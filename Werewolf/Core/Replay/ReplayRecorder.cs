using System;
using System.Collections.Generic;

namespace Werewolf.Core.Replay
{
    public enum ReplayEntityKind : byte
    {
        Player = 0,
        Enemy = 1,
        Cart = 2,
        Valuable = 3,
        Item = 4,

        Corpse = 5,
    }

    public struct ReplayEntitySample
    {
        public ReplayEntityKind Kind;
        public int Id;
        public float X;
        public float Y;
        public float Z;

        public ReplayEntitySample(ReplayEntityKind kind, int id, float x, float y, float z)
        {
            Kind = kind;
            Id = id;
            X = x;
            Y = y;
            Z = z;
        }
    }

    public sealed class ReplayPlayerInfo
    {
        public int Actor;
        public int ParticipantId;
        public string Name;
    }

    public sealed class ReplayEpInfo
    {
        public int Id;
        public byte State;
        public string StateName;
        public float X;
        public float Y;
        public float Z;
    }

    public sealed class ReplayValuableInfo
    {
        public int Id;
        public string Name;
        public int Dollars;
        public int Vid;
        public float X;
        public float Y;
        public float Z;
    }

    public struct ReplayLossEntry
    {
        public int Vid;
        public int Dollars;
        public bool IsOrb;
        public bool Destroyed;
    }

    public sealed class ReplayMapMesh
    {
        public float[] Vertices = Array.Empty<float>();
        public int[] Triangles = Array.Empty<int>();
    }

    public sealed class ReplayMapImage
    {
        public int Width;
        public int Height;
        public float MinX;
        public float MaxX;
        public float MinZ;
        public float MaxZ;
        public byte[] Png = Array.Empty<byte>();
    }

    public sealed class ReplaySegmentHeader
    {
        public string LevelName;
        public string StartedAtIso;
        public bool IsHost;
        public int LocalActor;
        public List<ReplayPlayerInfo> Players = new List<ReplayPlayerInfo>();
        public List<ReplayEpInfo> ExtractionPoints = new List<ReplayEpInfo>();
        public List<ReplayValuableInfo> Valuables = new List<ReplayValuableInfo>();
        public ReplayMapMesh Map;

        public ReplayMapImage MapImage;
    }

    public sealed class ReplayRecorder
    {
        public const int FormatVersion = 1;
        public const double BaseSampleIntervalSec = 0.25;
        public const float MoveThresholdMeters = 0.1f;
        public const double LateEventGraceSec = 5.0;

        private readonly int _degradeEntries1;
        private readonly int _degradeEntries2;
        private readonly int _hardCapEntries;

        public ReplayRecorder() : this(500_000, 750_000, 1_000_000) { }

        internal ReplayRecorder(int degradeEntries1, int degradeEntries2, int hardCapEntries)
        {
            _degradeEntries1 = degradeEntries1;
            _degradeEntries2 = degradeEntries2;
            _hardCapEntries = hardCapEntries;
        }

        internal sealed class Segment
        {
            public ReplaySegmentHeader Header;
            public readonly List<object> Records = new List<object>();
            public readonly List<ReplayLossEntry> Losses = new List<ReplayLossEntry>();
            public double StartClock;
            public double EndClock = -1;
            public double EndTick = -1;
        }

        internal sealed class PosRecord
        {
            public double T;
            public List<ReplayEntitySample> Entries;
        }

        internal sealed class EventRecord
        {
            public double T;
            public string Name;
            public (string Key, object Value)[] Fields;
        }

        internal sealed class EntityRecord
        {
            public double T;
            public ReplayEntityKind Kind;
            public int Id;
            public string Name;
            public int Vid;
        }

        private readonly List<Segment> _segments = new List<Segment>();
        private Segment _open;
        private double _lastSampleClock;
        private int _positionEntries;
        private int _eventCount;

        private readonly Dictionary<long, ReplayEntitySample> _lastOnChangePos =
            new Dictionary<long, ReplayEntitySample>();
        private readonly HashSet<long> _knownEntities = new HashSet<long>();
        private readonly HashSet<int> _presentValuables = new HashSet<int>();
        private readonly HashSet<int> _seenValuablesScratch = new HashSet<int>();
        private readonly HashSet<int> _presentItems = new HashSet<int>();
        private readonly HashSet<int> _seenItemsScratch = new HashSet<int>();
        private readonly Dictionary<int, int> _valueById = new Dictionary<int, int>();
        private readonly Dictionary<int, byte> _epStateById = new Dictionary<int, byte>();
        private readonly HashSet<int> _haulIds = new HashSet<int>();
        private readonly List<int> _haulDiffScratch = new List<int>();

        private readonly Dictionary<int, List<ReplayLossEntry>> _hostLedger =
            new Dictionary<int, List<ReplayLossEntry>>();
        private int _hostLedgerSegmentCount;

        public bool SegmentOpen => _open != null;
        public int SegmentCount => _segments.Count;
        public int PositionEntryCount => _positionEntries;
        public int EventCount => _eventCount;

        public double CurrentSampleIntervalSec
        {
            get
            {
                if (_positionEntries >= _degradeEntries2) return BaseSampleIntervalSec * 4.0;
                if (_positionEntries >= _degradeEntries1) return BaseSampleIntervalSec * 2.0;
                return BaseSampleIntervalSec;
            }
        }

        public void BeginSegment(ReplaySegmentHeader header, double nowSec)
        {
            if (header == null) return;
            if (_open != null) EndSegment(nowSec);

            _lastOnChangePos.Clear();
            _knownEntities.Clear();
            _presentValuables.Clear();
            _presentItems.Clear();
            _valueById.Clear();
            _epStateById.Clear();
            _haulIds.Clear();

            foreach (ReplayValuableInfo v in header.Valuables)
            {
                _knownEntities.Add(EntityKey(ReplayEntityKind.Valuable, v.Id));
                _presentValuables.Add(v.Id);
                _valueById[v.Id] = v.Dollars;
            }
            foreach (ReplayEpInfo ep in header.ExtractionPoints)
            {
                _epStateById[ep.Id] = ep.State;
            }

            _open = new Segment { Header = header, StartClock = nowSec };
            _segments.Add(_open);
            _lastSampleClock = double.NegativeInfinity;
        }

        public void EndSegment(double nowSec)
        {
            if (_open == null) return;
            _open.EndClock = nowSec;
            _open.EndTick = Tick(_open, nowSec);
            _open = null;
        }

        public bool ShouldSample(double nowSec)
        {
            if (_open == null) return false;
            if (_positionEntries >= _hardCapEntries) return false;
            return nowSec - _lastSampleClock >= CurrentSampleIntervalSec;
        }

        public void Sample(double nowSec, IReadOnlyList<ReplayEntitySample> states)
        {
            if (_open == null || states == null) return;
            _lastSampleClock = nowSec;
            double t = Tick(_open, nowSec);

            _seenValuablesScratch.Clear();
            _seenItemsScratch.Clear();
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].Kind == ReplayEntityKind.Valuable) _seenValuablesScratch.Add(states[i].Id);
                else if (states[i].Kind == ReplayEntityKind.Item) _seenItemsScratch.Add(states[i].Id);
            }
            foreach (int id in _presentValuables)
            {
                if (_seenValuablesScratch.Contains(id)) continue;
                AppendEvent(_open, t, "val_gone", new (string, object)[] { ("v", id) });
            }
            _presentValuables.Clear();
            foreach (int id in _seenValuablesScratch) _presentValuables.Add(id);

            foreach (int id in _presentItems)
            {
                if (_seenItemsScratch.Contains(id)) continue;
                AppendEvent(_open, t, "item_gone", new (string, object)[] { ("i", id) });
            }
            _presentItems.Clear();
            foreach (int id in _seenItemsScratch) _presentItems.Add(id);

            List<ReplayEntitySample> entries = null;
            for (int i = 0; i < states.Count; i++)
            {
                ReplayEntitySample s = states[i];
                bool include;
                if (s.Kind == ReplayEntityKind.Valuable || s.Kind == ReplayEntityKind.Item
                    || s.Kind == ReplayEntityKind.Corpse)
                {
                    long key = EntityKey(s.Kind, s.Id);
                    if (!_lastOnChangePos.TryGetValue(key, out ReplayEntitySample last))
                    {
                        include = true;
                        _lastOnChangePos[key] = s;
                    }
                    else
                    {
                        float dx = s.X - last.X;
                        float dy = s.Y - last.Y;
                        float dz = s.Z - last.Z;
                        include = dx * dx + dy * dy + dz * dz
                            > MoveThresholdMeters * MoveThresholdMeters;
                        if (include) _lastOnChangePos[key] = s;
                    }
                }
                else
                {
                    include = true;
                }
                if (!include) continue;
                if (entries == null) entries = new List<ReplayEntitySample>();
                entries.Add(s);
            }

            if (entries != null)
            {
                _open.Records.Add(new PosRecord { T = t, Entries = entries });
                _positionEntries += entries.Count;
            }
        }

        public void NoteEntity(double nowSec, ReplayEntityKind kind, int id, string name, int vid = 0)
        {
            if (_open == null) return;
            if (!_knownEntities.Add(EntityKey(kind, id))) return;
            _open.Records.Add(new EntityRecord
            {
                T = Tick(_open, nowSec),
                Kind = kind,
                Id = id,
                Name = name ?? "",
                Vid = vid,
            });
        }

        public void NoteValuableValue(double nowSec, int id, int dollars)
        {
            if (_open == null) return;
            if (_valueById.TryGetValue(id, out int prev) && prev == dollars) return;
            _valueById[id] = dollars;
            AppendEvent(_open, Tick(_open, nowSec), "val_value",
                new (string, object)[] { ("v", id), ("$", dollars) });
        }

        public void NoteEpState(double nowSec, int epId, byte state, string stateName)
        {
            if (_open == null) return;
            if (_epStateById.TryGetValue(epId, out byte prev) && prev == state) return;
            _epStateById[epId] = state;
            AppendEvent(_open, Tick(_open, nowSec), "ep_state",
                new (string, object)[] { ("ep", epId), ("s", (int)state), ("n", stateName ?? "") });
        }

        public void NoteEvent(double nowSec, string name, params (string Key, object Value)[] fields)
        {
            Segment target = ResolveEventTarget(nowSec);
            if (target == null) return;
            AppendEvent(target, Tick(target, nowSec), name, fields);
        }

        public void NoteLoss(double nowSec, int vid, int dollars, bool isOrb, bool destroyed)
        {
            Segment target = ResolveEventTarget(nowSec);
            if (target == null) return;
            AppendEvent(target, Tick(target, nowSec), "loss",
                new (string, object)[] { ("$", dollars), ("orb", isOrb), ("vid", vid), ("d", destroyed) });
            target.Losses.Add(new ReplayLossEntry
            {
                Vid = vid,
                Dollars = dollars,
                IsOrb = isOrb,
                Destroyed = destroyed,
            });
        }

        public void NoteHaulIds(double nowSec, IReadOnlyList<int> ids)
        {
            if (_open == null || ids == null) return;

            List<int> added = null;
            for (int i = 0; i < ids.Count; i++)
            {
                if (_haulIds.Contains(ids[i])) continue;
                (added ?? (added = new List<int>())).Add(ids[i]);
            }
            _haulDiffScratch.Clear();
            foreach (int prev in _haulIds)
            {
                bool still = false;
                for (int i = 0; i < ids.Count; i++)
                {
                    if (ids[i] == prev) { still = true; break; }
                }
                if (!still) _haulDiffScratch.Add(prev);
            }
            if (added == null && _haulDiffScratch.Count == 0) return;

            _haulDiffScratch.Sort();
            foreach (int gone in _haulDiffScratch) _haulIds.Remove(gone);
            if (added != null) foreach (int id in added) _haulIds.Add(id);

            AppendEvent(_open, Tick(_open, nowSec), "haul", new (string, object)[]
            {
                ("add", added ?? new List<int>()),
                ("del", new List<int>(_haulDiffScratch)),
            });
        }

        public object[] BuildLossLedgerWire()
        {
            if (_segments.Count == 0) return null;
            var segIdx = new List<int>();
            var vids = new List<int>();
            var dollars = new List<int>();
            var flags = new List<byte>();
            for (int i = 0; i < _segments.Count; i++)
            {
                foreach (ReplayLossEntry e in _segments[i].Losses)
                {
                    segIdx.Add(i);
                    vids.Add(e.Vid);
                    dollars.Add(e.Dollars);
                    flags.Add((byte)((e.IsOrb ? 1 : 0) | (e.Destroyed ? 2 : 0)));
                }
            }
            return new object[]
            {
                _segments.Count, segIdx.ToArray(), vids.ToArray(), dollars.ToArray(), flags.ToArray(),
            };
        }

        public bool ApplyLossLedgerWire(object[] payload)
        {
            if (payload == null || payload.Length < 5) return false;
            if (!(payload[0] is int segCount) || segCount <= 0) return false;
            if (!(payload[1] is int[] segIdx) || !(payload[2] is int[] vids)
                || !(payload[3] is int[] dollars) || !(payload[4] is byte[] flags)) return false;
            if (segIdx.Length != vids.Length || segIdx.Length != dollars.Length
                || segIdx.Length != flags.Length) return false;
            for (int i = 0; i < segIdx.Length; i++)
            {
                if (segIdx[i] < 0 || segIdx[i] >= segCount) return false;
            }

            _hostLedger.Clear();
            _hostLedgerSegmentCount = segCount;
            for (int i = 0; i < segIdx.Length; i++)
            {
                if (!_hostLedger.TryGetValue(segIdx[i], out List<ReplayLossEntry> list))
                {
                    list = new List<ReplayLossEntry>();
                    _hostLedger[segIdx[i]] = list;
                }
                list.Add(new ReplayLossEntry
                {
                    Vid = vids[i],
                    Dollars = dollars[i],
                    IsOrb = (flags[i] & 1) != 0,
                    Destroyed = (flags[i] & 2) != 0,
                });
            }
            return true;
        }

        public int HostLedgerSegmentCount => _hostLedgerSegmentCount;

        internal IReadOnlyList<Segment> SegmentsForPlayback => _segments;

        internal bool TryGetPlaybackLedger(int segmentIndex, out IReadOnlyList<ReplayLossEntry> entries)
        {
            entries = EmptyLosses;
            if (segmentIndex < 0 || segmentIndex >= _segments.Count) return false;
            Segment seg = _segments[segmentIndex];
            if (seg.Header != null && seg.Header.IsHost)
            {
                if (seg.EndTick < 0) return false;
                entries = seg.Losses;
                return true;
            }
            if (segmentIndex >= _hostLedgerSegmentCount) return false;
            entries = _hostLedger.TryGetValue(segmentIndex, out List<ReplayLossEntry> list)
                ? (IReadOnlyList<ReplayLossEntry>)list
                : EmptyLosses;
            return true;
        }

        public ReplaySegmentHeader FirstSegmentHeader
            => _segments.Count > 0 ? _segments[0].Header : null;

        public void AttachMapImage(ReplayMapImage image)
        {
            if (image == null || _segments.Count == 0) return;
            _segments[_segments.Count - 1].Header.MapImage = image;
        }

        public void Reset()
        {
            _segments.Clear();
            _open = null;
            _lastSampleClock = double.NegativeInfinity;
            _positionEntries = 0;
            _eventCount = 0;
            _lastOnChangePos.Clear();
            _knownEntities.Clear();
            _presentValuables.Clear();
            _presentItems.Clear();
            _valueById.Clear();
            _epStateById.Clear();
            _haulIds.Clear();
            _hostLedger.Clear();
            _hostLedgerSegmentCount = 0;
        }

        public IEnumerable<string> ToJsonLines()
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                Segment seg = _segments[i];
                yield return ReplayJson.HeaderLine(seg.Header);
                if (seg.Header.Map != null && seg.Header.Map.Triangles.Length > 0)
                {
                    yield return ReplayJson.MapLine(seg.Header.Map);
                }
                if (seg.Header.MapImage != null && seg.Header.MapImage.Png.Length > 0)
                {
                    yield return ReplayJson.MapImageLine(seg.Header.MapImage);
                }
                if (seg.Header.IsHost)
                {
                    if (seg.EndTick >= 0) yield return ReplayJson.LedgerLine(seg.Losses);
                }
                else if (i < _hostLedgerSegmentCount)
                {
                    _hostLedger.TryGetValue(i, out List<ReplayLossEntry> entries);
                    yield return ReplayJson.LedgerLine(entries ?? EmptyLosses);
                }
                foreach (object record in seg.Records)
                {
                    if (record is PosRecord pos) yield return ReplayJson.PosLine(pos.T, pos.Entries);
                    else if (record is EventRecord ev) yield return ReplayJson.EventLine(ev.T, ev.Name, ev.Fields);
                    else if (record is EntityRecord ent) yield return ReplayJson.EntityLine(ent.T, ent.Kind, ent.Id, ent.Name, ent.Vid);
                }
                if (seg.EndTick >= 0) yield return ReplayJson.SegEndLine(seg.EndTick);
            }
        }

        private static readonly List<ReplayLossEntry> EmptyLosses = new List<ReplayLossEntry>();

        private Segment ResolveEventTarget(double nowSec)
        {
            if (_open != null) return _open;
            Segment last = _segments.Count > 0 ? _segments[_segments.Count - 1] : null;
            if (last == null || last.EndClock < 0) return null;
            if (nowSec - last.EndClock > LateEventGraceSec) return null;
            return last;
        }

        private void AppendEvent(Segment target, double t, string name, (string Key, object Value)[] fields)
        {
            target.Records.Add(new EventRecord { T = t, Name = name, Fields = fields });
            _eventCount++;
        }

        private static double Tick(Segment seg, double nowSec)
        {
            double t = nowSec - seg.StartClock;
            return t < 0 ? 0 : Math.Round(t, 2, MidpointRounding.AwayFromZero);
        }

        private static long EntityKey(ReplayEntityKind kind, int id)
            => ((long)(byte)kind << 32) | (uint)id;
    }
}
