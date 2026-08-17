using System;
using System.Collections.Generic;

namespace Werewolf.Core.Replay
{
    public sealed class ReplayEntityTrack
    {
        public ReplayEntityKind Kind;
        public int Id;
        public string Name = "";

        public int Vid;

        internal readonly List<double> T = new List<double>();
        internal readonly List<float> X = new List<float>();
        internal readonly List<float> Y = new List<float>();
        internal readonly List<float> Z = new List<float>();

        internal readonly List<double> ValueT = new List<double>();
        internal readonly List<int> ValueDollars = new List<int>();

        internal readonly List<(double From, double To)> HaulRanges = new List<(double, double)>();

        public double GoneAtT = double.PositiveInfinity;

        public bool HiddenDuplicate;

        public int Count => T.Count;
        public double FirstT => T.Count > 0 ? T[0] : double.PositiveInfinity;
        public double LastT => T.Count > 0 ? T[T.Count - 1] : double.NegativeInfinity;

        internal void Add(double t, float x, float y, float z)
        {
            T.Add(t);
            X.Add(x);
            Y.Add(y);
            Z.Add(z);
        }

        internal void AddValue(double t, int dollars)
        {
            ValueT.Add(t);
            ValueDollars.Add(dollars);
        }

        public int ValueAt(double t)
        {
            int idx = ReplayPlayback.LastIndexAtOrBefore(ValueT, t);
            return idx < 0 ? 0 : ValueDollars[idx];
        }

        public bool InHaulNear(double t, double lookbackSec)
        {
            for (int i = 0; i < HaulRanges.Count; i++)
            {
                (double from, double to) = HaulRanges[i];
                if (from <= t && t - to <= lookbackSec) return true;
            }
            return false;
        }
    }

    public enum ReplayValueEventKind : byte
    {
        Damage = 0,

        Deliver = 1,

        Lost = 2,
    }

    public struct ReplayValuePopup
    {
        public double T;
        public float X;
        public float Z;
        public int Amount;
        public ReplayValueEventKind Kind;
    }

    public struct ReplayDeathMark
    {
        public double T;

        public double HideT;

        public float X;
        public float Z;
        public int Actor;

        public bool IsVisibleAt(double t) => t >= T && t < HideT;
    }

    public sealed class ReplayPlayerEntry
    {
        public const byte RoleUnknown = 255;

        public int Actor;
        public int ParticipantId;
        public string Name = "";
        public ReplayEntityTrack Track;

        public ReplayEntityTrack CorpseTrack;

        public double DeathT = double.PositiveInfinity;

        public double AnnouncedT = double.PositiveInfinity;

        public bool IsDepartedAt(double t) => t >= AnnouncedT;

        public byte Role = RoleUnknown;

        public bool IsWerewolfSide;

        public bool IsAliveAt(double t) => t < DeathT;
    }

    public sealed class ReplayEpEntry
    {
        public int Id;

        public int Number;

        public float X;
        public float Y;
        public float Z;

        internal readonly List<double> StateT = new List<double>();
        internal readonly List<byte> States = new List<byte>();
        internal readonly List<string> StateNames = new List<string>();

        public (byte State, string Name) StateAt(double t)
        {
            int idx = ReplayPlayback.LastIndexAtOrBefore(StateT, t);
            if (idx < 0) idx = 0;
            return (States[idx], StateNames[idx]);
        }
    }

    public struct ReplayTrailPoint
    {
        public double T;
        public float X;
        public float Z;
    }

    public sealed class ReplayPlayback
    {
        public const double PresenceGapSec = 2.0;

        public const float CartDuplicateMeters = 1.0f;

        public static readonly string[] PressStateNames = { "Warning", "Extracting", "TaxReturn", "Complete" };

        public const double HaulLookbackSec = 3.0;

        public const double DeathMarkPosLookaheadSec = 0.5;

        public const float CorpseSpawnNearMeters = 10f;

        public ReplaySegmentHeader Header { get; private set; }
        public double Duration { get; private set; }

        public List<ReplayPlayerEntry> Players { get; } = new List<ReplayPlayerEntry>();
        public List<ReplayEntityTrack> Enemies { get; } = new List<ReplayEntityTrack>();
        public List<ReplayEntityTrack> Carts { get; } = new List<ReplayEntityTrack>();
        public List<ReplayEntityTrack> Valuables { get; } = new List<ReplayEntityTrack>();
        public List<ReplayEntityTrack> Items { get; } = new List<ReplayEntityTrack>();
        public List<ReplayEpEntry> Eps { get; } = new List<ReplayEpEntry>();

        public List<(double T, int Actor)> Deaths { get; } = new List<(double, int)>();

        public List<ReplayDeathMark> DeathMarks { get; } = new List<ReplayDeathMark>();

        public List<(double Start, double End)> Meetings { get; } = new List<(double, double)>();

        public List<(double T, int Actor, string Text)> Chats { get; }
            = new List<(double, int, string)>();

        public List<(double T, int ExecutedActor)> MeetingResults { get; }
            = new List<(double, int)>();

        public byte WinnerTeam { get; private set; } = 255;

        public int BaseDollars { get; private set; }

        public List<ReplayValuePopup> Popups { get; } = new List<ReplayValuePopup>();

        private readonly List<double> _lossCumT = new List<double>();
        private readonly List<int> _lossCumDollars = new List<int>();

        private readonly List<double> _popupT = new List<double>();
        private readonly List<int> _popupLostCum = new List<int>();
        private readonly List<int> _popupDeliverCum = new List<int>();

        private readonly Dictionary<long, ReplayEntityTrack> _tracks =
            new Dictionary<long, ReplayEntityTrack>();
        private readonly Dictionary<int, ReplayPlayerEntry> _playersByActor =
            new Dictionary<int, ReplayPlayerEntry>();
        private readonly Dictionary<int, ReplayEpEntry> _epsById = new Dictionary<int, ReplayEpEntry>();
        private readonly Dictionary<int, int> _destroyedByVid = new Dictionary<int, int>();

        private ReplayPlayback() { }

        public static ReplayPlayback FromRecorder(ReplayRecorder recorder)
        {
            if (recorder == null) return null;
            IReadOnlyList<ReplayRecorder.Segment> segments = recorder.SegmentsForPlayback;
            if (segments.Count == 0) return null;
            ReplayRecorder.Segment seg = segments[segments.Count - 1];

            var pb = new ReplayPlayback { Header = seg.Header };
            pb.SeedFromHeader(seg.Header);

            double lastT = 0;
            var meetings = new MeetingRangeBuilder();
            foreach (object record in seg.Records)
            {
                if (record is ReplayRecorder.PosRecord pos)
                {
                    lastT = Math.Max(lastT, pos.T);
                    for (int i = 0; i < pos.Entries.Count; i++)
                    {
                        ReplayEntitySample s = pos.Entries[i];
                        pb.ResolveTrack(s.Kind, s.Id).Add(pos.T, s.X, s.Y, s.Z);
                    }
                }
                else if (record is ReplayRecorder.EntityRecord ent)
                {
                    lastT = Math.Max(lastT, ent.T);
                    ReplayEntityTrack track = pb.ResolveTrack(ent.Kind, ent.Id);
                    if (string.IsNullOrEmpty(track.Name)) track.Name = ent.Name ?? "";
                    if (track.Vid == 0) track.Vid = ent.Vid;
                }
                else if (record is ReplayRecorder.EventRecord ev)
                {
                    lastT = Math.Max(lastT, ev.T);
                    pb.ApplyEvent(ev, meetings);
                }
            }

            pb.Duration = seg.EndTick >= 0 ? seg.EndTick : lastT;
            meetings.Close(pb.Duration, pb.Meetings);
            pb.MarkCartDuplicateItems();
            pb.PruneCorpseSpawnGlitch();
            pb.BuildDeathMarks();

            if (recorder.TryGetPlaybackLedger(
                segments.Count - 1, out IReadOnlyList<ReplayLossEntry> ledger))
            {
                foreach (ReplayLossEntry entry in ledger)
                {
                    if (entry.Destroyed && !pb._destroyedByVid.ContainsKey(entry.Vid))
                    {
                        pb._destroyedByVid[entry.Vid] = entry.Dollars;
                    }
                }
            }
            pb.BuildValuePopups();
            return pb;
        }

        public int LostDollarsAt(double t)
        {
            if (_lossCumT.Count > 0)
            {
                int idx = LastIndexAtOrBefore(_lossCumT, t);
                return idx < 0 ? 0 : _lossCumDollars[idx];
            }
            int p = LastIndexAtOrBefore(_popupT, t);
            return p < 0 ? 0 : _popupLostCum[p];
        }

        public int DeliveredDollarsAt(double t)
        {
            int p = LastIndexAtOrBefore(_popupT, t);
            return p < 0 ? 0 : _popupDeliverCum[p];
        }

        public int LastPopupIndexAtOrBefore(double t) => LastIndexAtOrBefore(_popupT, t);

        public bool TryGetPos(ReplayEntityTrack tr, double t, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            if (tr == null || tr.Count == 0) return false;

            bool hold = tr.Kind == ReplayEntityKind.Valuable || tr.Kind == ReplayEntityKind.Item
                || tr.Kind == ReplayEntityKind.Corpse;
            if (hold)
            {
                if (t >= tr.GoneAtT) return false;
                int idx = LastIndexAtOrBefore(tr.T, t);
                if (idx < 0) return false;
                x = tr.X[idx];
                y = tr.Y[idx];
                z = tr.Z[idx];
                return true;
            }

            if (t <= tr.FirstT)
            {
                if (tr.FirstT - t > PresenceGapSec) return false;
                x = tr.X[0];
                y = tr.Y[0];
                z = tr.Z[0];
                return true;
            }
            if (t >= tr.LastT)
            {
                if (t - tr.LastT > PresenceGapSec) return false;
                int last = tr.Count - 1;
                x = tr.X[last];
                y = tr.Y[last];
                z = tr.Z[last];
                return true;
            }

            int prev = LastIndexAtOrBefore(tr.T, t);
            int next = prev + 1;
            double dt = tr.T[next] - tr.T[prev];
            if (dt > PresenceGapSec) return false;
            float f = dt <= 0 ? 0f : (float)((t - tr.T[prev]) / dt);
            x = tr.X[prev] + (tr.X[next] - tr.X[prev]) * f;
            y = tr.Y[prev] + (tr.Y[next] - tr.Y[prev]) * f;
            z = tr.Z[prev] + (tr.Z[next] - tr.Z[prev]) * f;
            return true;
        }

        public ReplayPace BuildPace()
        {
            var times = new List<double>(Chats.Count);
            for (int i = 0; i < Chats.Count; i++) times.Add(Chats[i].T);
            return new ReplayPace(Meetings, times);
        }

        public void TrailInto(ReplayEntityTrack tr, double fromT, double toT, List<ReplayTrailPoint> into)
        {
            if (tr == null || into == null) return;
            int start = LastIndexAtOrBefore(tr.T, fromT);
            if (start < 0) start = 0;
            for (int i = start; i < tr.Count; i++)
            {
                if (tr.T[i] > toT) break;
                if (tr.T[i] < fromT) continue;
                into.Add(new ReplayTrailPoint { T = tr.T[i], X = tr.X[i], Z = tr.Z[i] });
            }
        }

        internal static int LastIndexAtOrBefore(List<double> sorted, double value)
        {
            int lo = 0, hi = sorted.Count - 1, ans = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (sorted[mid] <= value)
                {
                    ans = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return ans;
        }

        private void SeedFromHeader(ReplaySegmentHeader header)
        {
            if (header == null) return;
            foreach (ReplayPlayerInfo p in header.Players)
            {
                ResolvePlayer(p.Actor, p.ParticipantId, p.Name);
            }
            foreach (ReplayValuableInfo v in header.Valuables)
            {
                ReplayEntityTrack track = ResolveTrack(ReplayEntityKind.Valuable, v.Id);
                track.Name = v.Name ?? "";
                track.Vid = v.Vid;
                track.Add(0.0, v.X, v.Y, v.Z);
                track.AddValue(0.0, v.Dollars);
                BaseDollars += v.Dollars;
            }
            for (int i = 0; i < header.ExtractionPoints.Count; i++)
            {
                ReplayEpInfo ep = header.ExtractionPoints[i];
                var entry = new ReplayEpEntry
                {
                    Id = ep.Id,
                    Number = i + 1,
                    X = ep.X,
                    Y = ep.Y,
                    Z = ep.Z,
                };
                entry.StateT.Add(0.0);
                entry.States.Add(ep.State);
                entry.StateNames.Add(ep.StateName ?? "");
                Eps.Add(entry);
                _epsById[ep.Id] = entry;
            }
        }

        private ReplayPlayerEntry ResolvePlayer(int actor, int participantId, string name)
        {
            if (_playersByActor.TryGetValue(actor, out ReplayPlayerEntry existing)) return existing;
            var entry = new ReplayPlayerEntry
            {
                Actor = actor,
                ParticipantId = participantId,
                Name = name ?? "",
            };
            Players.Add(entry);
            _playersByActor[actor] = entry;
            entry.Track = ResolveTrack(ReplayEntityKind.Player, actor);
            return entry;
        }

        private ReplayEntityTrack ResolveTrack(ReplayEntityKind kind, int id)
        {
            long key = ((long)(byte)kind << 32) | (uint)id;
            if (_tracks.TryGetValue(key, out ReplayEntityTrack track)) return track;
            track = new ReplayEntityTrack { Kind = kind, Id = id };
            _tracks[key] = track;
            switch (kind)
            {
                case ReplayEntityKind.Player:
                    if (!_playersByActor.ContainsKey(id))
                    {
                        var entry = new ReplayPlayerEntry { Actor = id, Track = track };
                        Players.Add(entry);
                        _playersByActor[id] = entry;
                    }
                    break;
                case ReplayEntityKind.Enemy: Enemies.Add(track); break;
                case ReplayEntityKind.Cart: Carts.Add(track); break;
                case ReplayEntityKind.Valuable: Valuables.Add(track); break;
                case ReplayEntityKind.Item: Items.Add(track); break;
                case ReplayEntityKind.Corpse: ResolvePlayer(id, 0, null).CorpseTrack = track; break;
            }
            return track;
        }

        private sealed class MeetingRangeBuilder
        {
            private double _warpT = -1;

            public void NoteWarp(double t)
            {
                if (_warpT < 0) _warpT = t;
            }

            public void Close(double endT, List<(double, double)> into)
            {
                if (_warpT >= 0 && _warpT < endT) into.Add((_warpT, endT));
                _warpT = -1;
            }
        }

        private void ApplyEvent(ReplayRecorder.EventRecord ev, MeetingRangeBuilder meetings)
        {
            switch (ev.Name)
            {
                case "death":
                    if (TryField(ev.Fields, "a", out int deadActor))
                    {
                        ReplayPlayerEntry p = ResolvePlayer(deadActor, 0, null);
                        if (ev.T < p.DeathT) p.DeathT = ev.T;
                        Deaths.Add((ev.T, deadActor));
                    }
                    break;

                case "announced":
                    if (FieldValue(ev.Fields, "actors") is int[] announcedActors)
                    {
                        foreach (int actor in announcedActors)
                        {
                            ReplayPlayerEntry p = ResolvePlayer(actor, 0, null);
                            if (ev.T < p.AnnouncedT) p.AnnouncedT = ev.T;
                        }
                    }
                    break;

                case "val_gone":
                    if (TryField(ev.Fields, "v", out int goneId))
                    {
                        ReplayEntityTrack tr = ResolveTrack(ReplayEntityKind.Valuable, goneId);
                        if (ev.T < tr.GoneAtT) tr.GoneAtT = ev.T;
                    }
                    break;

                case "val_value":
                    if (TryField(ev.Fields, "v", out int valueId)
                        && TryField(ev.Fields, "$", out int dollars))
                    {
                        ResolveTrack(ReplayEntityKind.Valuable, valueId).AddValue(ev.T, dollars);
                    }
                    break;

                case "haul":
                    ApplyHaul(ev);
                    break;

                case "loss":
                    if (TryField(ev.Fields, "$", out int lostDollars))
                    {
                        int cum = (_lossCumDollars.Count > 0
                            ? _lossCumDollars[_lossCumDollars.Count - 1] : 0) + lostDollars;
                        _lossCumT.Add(ev.T);
                        _lossCumDollars.Add(cum);
                        if (TryFieldBool(ev.Fields, "d", out bool destroyed) && destroyed
                            && TryField(ev.Fields, "vid", out int lossVid)
                            && !_destroyedByVid.ContainsKey(lossVid))
                        {
                            _destroyedByVid[lossVid] = lostDollars;
                        }
                    }
                    break;

                case "item_gone":
                    if (TryField(ev.Fields, "i", out int goneItemId))
                    {
                        ReplayEntityTrack tr = ResolveTrack(ReplayEntityKind.Item, goneItemId);
                        if (ev.T < tr.GoneAtT) tr.GoneAtT = ev.T;
                    }
                    break;

                case "phase":
                    if (TryFieldString(ev.Fields, "to", out string to)
                        && !string.Equals(to, "Meeting", StringComparison.Ordinal))
                    {
                        meetings.Close(ev.T, Meetings);
                    }
                    break;

                case "meet_warp":
                    meetings.NoteWarp(ev.T);
                    break;

                case "ep_state":
                    if (TryField(ev.Fields, "ep", out int epId)
                        && _epsById.TryGetValue(epId, out ReplayEpEntry ep)
                        && TryField(ev.Fields, "s", out int state))
                    {
                        TryFieldString(ev.Fields, "n", out string stateName);
                        ep.StateT.Add(ev.T);
                        ep.States.Add((byte)state);
                        ep.StateNames.Add(stateName ?? "");
                    }
                    break;

                case "chat":
                    if (TryField(ev.Fields, "a", out int chatActor)
                        && TryFieldString(ev.Fields, "text", out string chatText))
                    {
                        string clean = ReplayChatText.SanitizeForRecord(chatText);
                        if (clean.Length > 0) Chats.Add((ev.T, chatActor, clean));
                    }
                    break;

                case "meeting_result":
                    if (TryField(ev.Fields, "a", out int executedActor))
                    {
                        MeetingResults.Add((ev.T, executedActor));
                    }
                    break;

                case "gameover":
                    if (TryField(ev.Fields, "team", out int team)) WinnerTeam = (byte)team;
                    ApplyGameOverRoles(ev.Fields);
                    break;
            }
        }

        private void ApplyGameOverRoles((string Key, object Value)[] fields)
        {
            int[] actors = FieldValue(fields, "actors") as int[];
            int[] roles = FieldValue(fields, "roles") as int[];
            if (actors == null || roles == null) return;
            for (int i = 0; i < actors.Length && i < roles.Length; i++)
            {
                ReplayPlayerEntry p = ResolvePlayer(actors[i], 0, null);
                p.Role = (byte)roles[i];
                p.IsWerewolfSide = RoleDistribution.TeamOf((Role)p.Role) == Team.Werewolves;
            }
        }

        private void ApplyHaul(ReplayRecorder.EventRecord ev)
        {
            if (FieldValue(ev.Fields, "add") is List<int> added)
            {
                foreach (int id in added)
                {
                    ReplayEntityTrack tr = ResolveTrack(ReplayEntityKind.Valuable, id);
                    tr.HaulRanges.Add((ev.T, double.PositiveInfinity));
                }
            }
            if (FieldValue(ev.Fields, "del") is List<int> removed)
            {
                foreach (int id in removed)
                {
                    ReplayEntityTrack tr = ResolveTrack(ReplayEntityKind.Valuable, id);
                    int last = tr.HaulRanges.Count - 1;
                    if (last >= 0 && double.IsPositiveInfinity(tr.HaulRanges[last].To))
                    {
                        tr.HaulRanges[last] = (tr.HaulRanges[last].From, ev.T);
                    }
                }
            }
        }

        private void BuildValuePopups()
        {
            foreach (ReplayEntityTrack tr in Valuables)
            {
                for (int i = 1; i < tr.ValueT.Count; i++)
                {
                    int delta = tr.ValueDollars[i - 1] - tr.ValueDollars[i];
                    if (delta <= 0) continue;
                    if (!TryGetLastPos(tr, tr.ValueT[i], out float dx, out float dz)) continue;
                    Popups.Add(new ReplayValuePopup
                    {
                        T = tr.ValueT[i],
                        X = dx,
                        Z = dz,
                        Amount = delta,
                        Kind = ReplayValueEventKind.Damage,
                    });
                }

                if (double.IsPositiveInfinity(tr.GoneAtT)) continue;
                double goneT = tr.GoneAtT;
                if (!TryGetLastPos(tr, goneT, out float gx, out float gz)) continue;
                int lastSeen = tr.ValueAt(goneT);

                ReplayValueEventKind kind;
                int amount;
                if (tr.Vid != 0 && _destroyedByVid.TryGetValue(tr.Vid, out int destroyedAmount))
                {
                    kind = ReplayValueEventKind.Lost;
                    amount = destroyedAmount > 0 ? destroyedAmount : lastSeen;
                }
                else if (tr.InHaulNear(goneT, HaulLookbackSec) && AnyEpPressingAt(goneT))
                {
                    kind = ReplayValueEventKind.Deliver;
                    amount = lastSeen;
                }
                else
                {
                    continue;
                }
                if (amount <= 0) continue;
                Popups.Add(new ReplayValuePopup
                {
                    T = goneT,
                    X = gx,
                    Z = gz,
                    Amount = amount,
                    Kind = kind,
                });
            }

            Popups.Sort((a, b) => a.T.CompareTo(b.T));
            int lost = 0, delivered = 0;
            for (int i = 0; i < Popups.Count; i++)
            {
                ReplayValuePopup p = Popups[i];
                if (p.Kind == ReplayValueEventKind.Deliver) delivered += p.Amount;
                else lost += p.Amount;
                _popupT.Add(p.T);
                _popupLostCum.Add(lost);
                _popupDeliverCum.Add(delivered);
            }
        }

        private void BuildDeathMarks()
        {
            foreach ((double t, int actor) in Deaths)
            {
                bool insideMeeting = false;
                for (int i = 0; i < Meetings.Count; i++)
                {
                    if (Meetings[i].Start <= t && t < Meetings[i].End)
                    {
                        insideMeeting = true;
                        break;
                    }
                }
                if (insideMeeting) continue;
                if (!TryGetDeathPos(actor, t, out float x, out float z)) continue;

                double hideT = double.PositiveInfinity;
                for (int i = 0; i < Meetings.Count; i++)
                {
                    if (Meetings[i].Start >= t)
                    {
                        hideT = Meetings[i].End;
                        break;
                    }
                }
                DeathMarks.Add(new ReplayDeathMark
                {
                    T = t,
                    HideT = hideT,
                    X = x,
                    Z = z,
                    Actor = actor,
                });
            }
        }

        private void PruneCorpseSpawnGlitch()
        {
            foreach (ReplayPlayerEntry p in Players)
            {
                ReplayEntityTrack corpse = p.CorpseTrack;
                ReplayEntityTrack track = p.Track;
                if (corpse == null || corpse.Count == 0) continue;
                if (track == null || track.Count == 0 || double.IsPositiveInfinity(p.DeathT)) continue;

                int idx = LastIndexAtOrBefore(track.T, p.DeathT + DeathMarkPosLookaheadSec);
                if (idx < 0) idx = 0;
                float dx = track.X[idx];
                float dy = track.Y[idx];
                float dz = track.Z[idx];

                int keep = 0;
                while (keep < corpse.Count)
                {
                    float ox = corpse.X[keep] - dx;
                    float oy = corpse.Y[keep] - dy;
                    float oz = corpse.Z[keep] - dz;
                    if (ox * ox + oy * oy + oz * oz
                        <= CorpseSpawnNearMeters * CorpseSpawnNearMeters)
                    {
                        break;
                    }
                    keep++;
                }
                if (keep == 0 || keep >= corpse.Count) continue;
                corpse.T.RemoveRange(0, keep);
                corpse.X.RemoveRange(0, keep);
                corpse.Y.RemoveRange(0, keep);
                corpse.Z.RemoveRange(0, keep);
            }
        }

        private bool TryGetDeathPos(int actor, double deathT, out float x, out float z)
        {
            x = z = 0f;
            if (!_playersByActor.TryGetValue(actor, out ReplayPlayerEntry p)) return false;
            ReplayEntityTrack track = p.Track;
            if (track == null || track.Count == 0) return false;
            int idx = LastIndexAtOrBefore(track.T, deathT + DeathMarkPosLookaheadSec);
            if (idx < 0) idx = 0;
            x = track.X[idx];
            z = track.Z[idx];
            return true;
        }

        public bool AnyEpPressingAt(double t)
        {
            for (int i = 0; i < Eps.Count; i++)
            {
                if (IsPressState(Eps[i].StateAt(t).Name)) return true;
            }
            return false;
        }

        private static bool IsPressState(string stateName)
        {
            for (int i = 0; i < PressStateNames.Length; i++)
            {
                if (string.Equals(PressStateNames[i], stateName, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static bool TryGetLastPos(ReplayEntityTrack tr, double t, out float x, out float z)
        {
            x = z = 0f;
            if (tr == null || tr.Count == 0) return false;
            int idx = LastIndexAtOrBefore(tr.T, t);
            if (idx < 0) idx = 0;
            x = tr.X[idx];
            z = tr.Z[idx];
            return true;
        }

        private void MarkCartDuplicateItems()
        {
            foreach (ReplayEntityTrack cart in Carts)
            {
                if (cart.Count == 0) continue;
                foreach (ReplayEntityTrack item in Items)
                {
                    if (item.HiddenDuplicate || item.Count == 0) continue;
                    if (!string.Equals(cart.Name, item.Name, StringComparison.Ordinal)) continue;
                    float dx = cart.X[0] - item.X[0];
                    float dy = cart.Y[0] - item.Y[0];
                    float dz = cart.Z[0] - item.Z[0];
                    if (dx * dx + dy * dy + dz * dz
                        <= CartDuplicateMeters * CartDuplicateMeters)
                    {
                        item.HiddenDuplicate = true;
                        break;
                    }
                }
            }
        }

        private static bool TryField((string Key, object Value)[] fields, string key, out int value)
        {
            object raw = FieldValue(fields, key);
            if (raw is int i)
            {
                value = i;
                return true;
            }
            if (raw is byte b)
            {
                value = b;
                return true;
            }
            value = 0;
            return false;
        }

        private static bool TryFieldBool((string Key, object Value)[] fields, string key, out bool value)
        {
            if (FieldValue(fields, key) is bool b)
            {
                value = b;
                return true;
            }
            value = false;
            return false;
        }

        private static bool TryFieldString((string Key, object Value)[] fields, string key, out string value)
        {
            value = FieldValue(fields, key) as string;
            return value != null;
        }

        private static object FieldValue((string Key, object Value)[] fields, string key)
        {
            if (fields == null) return null;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Key == key) return fields[i].Value;
            }
            return null;
        }
    }
}
