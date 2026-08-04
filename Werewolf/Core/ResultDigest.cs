using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum DigestKind : byte
    {
        MatchStart = 0,

        MeetingConvened = 1,

        Executed = 2,

        NoExecution = 3,

        CurseStarted = 4,

        CurseFollow = 5,

        Death = 6,

        BombDetonated = 7,

        Checkmate = 8,

        MatchEnd = 9,

        ExtractionDone = 10,

        PerkUnlocked = 11,

        InformantEstablished = 12,

        FinalBalance = 13,
    }

    public sealed class DigestEntry
    {
        public DigestEntry(DigestKind kind, int atSec, int actor, int argA, int argB)
        {
            Kind = kind;
            AtSec = atSec;
            Actor = actor;
            ArgA = argA;
            ArgB = argB;
        }

        public DigestKind Kind { get; }

        public int AtSec { get; }

        public int Actor { get; }

        public int ArgA { get; }

        public int ArgB { get; }
    }

    public sealed class ResultDigest
    {
        public const int MaxEntries = 400;

        public const int ReasonUnknown = 255;

        private const byte BombDetonationCode = 180;

        private readonly List<DigestEntry> _entries = new List<DigestEntry>();
        private long _startUnixMs;
        private bool _started;

        public IReadOnlyList<DigestEntry> Entries => _entries;

        public bool Started => _started;

        public void Observe(byte code, object[] payload, long nowUnixMs, WinResult winner = null)
        {
            if (code == WWEventCodes.GameStart)
            {
                _entries.Clear();
                _startUnixMs = nowUnixMs;
                _started = true;
                Add(DigestKind.MatchStart, nowUnixMs, 0, 0, 0);
                return;
            }
            if (!_started || payload == null) return;

            switch (code)
            {
                case WWMeetingCodes.StartMeeting:
                    if (payload.Length >= 4 && payload[0] is int caller && payload[3] is byte kind)
                    {
                        Add(DigestKind.MeetingConvened, nowUnixMs, caller, kind, 0);
                    }
                    break;

                case WWMeetingCodes.MeetingResult:
                    if (payload.Length >= 1 && payload[0] is int executed)
                    {
                        if (executed != -1)
                        {
                            Add(DigestKind.Executed, nowUnixMs, executed, 0, 0);
                        }
                        else
                        {
                            Add(DigestKind.NoExecution, nowUnixMs, 0, 0, 0);
                        }
                    }
                    break;

                case WWEventCodes.PlayerDied:
                    if (payload.Length >= 2 && payload[0] is int dead && payload[1] is byte cause
                        && (DeathCause)cause == DeathCause.Other)
                    {
                        Add(DigestKind.Death, nowUnixMs, dead, 0, 0);
                    }
                    break;

                case WWRolesCodes.RoleState:
                    if (payload.Length >= 2 && payload[0] is byte subtype && payload[1] is int[] data
                        && data != null && data.Length >= 1)
                    {
                        if (subtype == 0) Add(DigestKind.CurseStarted, nowUnixMs, data[0], 0, 0);
                        else if (subtype == 1) Add(DigestKind.CurseFollow, nowUnixMs, data[0], 0, 0);
                    }
                    break;

                case BombDetonationCode:
                    if (payload.Length >= 1 && payload[0] is int target)
                    {
                        Add(DigestKind.BombDetonated, nowUnixMs, target, 0, 0);
                    }
                    break;

                case WWCheckmateCodes.CheckmateReveal:
                    Add(DigestKind.Checkmate, nowUnixMs, 0, 0, 0);
                    break;

                case WWEventCodes.GameOver:
                    if (payload.Length >= 1 && payload[0] is byte team)
                    {
                        int reason = winner != null ? (int)winner.Reason : ReasonUnknown;
                        Add(DigestKind.MatchEnd, nowUnixMs, 0, team, reason, force: true);
                    }
                    break;
            }
        }

        public void RecordExtractionDone(int completed, int total, long nowUnixMs)
        {
            if (!_started) return;
            Add(DigestKind.ExtractionDone, nowUnixMs, 0, completed, total);
        }

        public void RecordPerkUnlocked(byte perkId, long nowUnixMs)
        {
            if (!_started) return;
            Add(DigestKind.PerkUnlocked, nowUnixMs, 0, perkId, 0);
        }

        public void RecordInformant(long nowUnixMs)
        {
            if (!_started) return;
            Add(DigestKind.InformantEstablished, nowUnixMs, 0, 0, 0);
        }

        public void RecordFinalBalance(int deliveredDollars, int remainingQuotaDollars,
            int obtainableDollars, long nowUnixMs)
        {
            if (!_started) return;
            Add(DigestKind.FinalBalance, nowUnixMs, obtainableDollars,
                deliveredDollars, remainingQuotaDollars, force: true);
        }

        public object[] ToWire()
        {
            int n = _entries.Count;
            var kinds = new byte[n];
            var atSec = new int[n];
            var actors = new int[n];
            var argA = new int[n];
            var argB = new int[n];
            for (int i = 0; i < n; i++)
            {
                DigestEntry e = _entries[i];
                kinds[i] = (byte)e.Kind;
                atSec[i] = e.AtSec;
                actors[i] = e.Actor;
                argA[i] = e.ArgA;
                argB[i] = e.ArgB;
            }
            return new object[] { kinds, atSec, actors, argA, argB };
        }

        public static IReadOnlyList<DigestEntry> FromWire(object[] payload)
        {
            if (payload == null || payload.Length < 5) return null;
            if (!(payload[0] is byte[] kinds) || !(payload[1] is int[] atSec)
                || !(payload[2] is int[] actors) || !(payload[3] is int[] argA)
                || !(payload[4] is int[] argB))
            {
                return null;
            }
            int n = kinds.Length;
            if (atSec.Length != n || actors.Length != n || argA.Length != n || argB.Length != n)
            {
                return null;
            }
            var list = new List<DigestEntry>(n);
            for (int i = 0; i < n; i++)
            {
                list.Add(new DigestEntry((DigestKind)kinds[i], atSec[i], actors[i], argA[i], argB[i]));
            }
            return list;
        }

        private void Add(DigestKind kind, long nowUnixMs, int actor, int argA, int argB, bool force = false)
        {
            if (!force && _entries.Count >= MaxEntries) return;
            long elapsed = (nowUnixMs - _startUnixMs) / 1000L;
            if (elapsed < 0) elapsed = 0;
            _entries.Add(new DigestEntry(kind, (int)elapsed, actor, argA, argB));
        }
    }
}
