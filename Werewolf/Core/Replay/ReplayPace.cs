using System;
using System.Collections.Generic;

namespace Werewolf.Core.Replay
{
    public enum ReplayPaceZone : byte
    {
        Explore = 0,

        MeetingTalk = 1,

        MeetingSilent = 2,
    }

    public sealed class ReplayPace
    {
        public const float ExploreNormal = 8f;
        public const float ExploreFast = 32f;
        public const float TalkNormal = 4f;
        public const float TalkFast = 16f;
        public const float SilentNormal = 16f;
        public const float SilentFast = 32f;

        public const double TalkWindowSec = 5.0;

        private readonly List<(double Start, double End)> _meetings;
        private readonly List<double> _chats;

        public ReplayPace(
            IReadOnlyList<(double Start, double End)> meetings, IReadOnlyList<double> chatTimes)
        {
            _meetings = meetings != null
                ? new List<(double, double)>(meetings)
                : new List<(double, double)>();
            _chats = chatTimes != null ? new List<double>(chatTimes) : new List<double>();
            _meetings.Sort((a, b) => a.Start.CompareTo(b.Start));
            _chats.Sort();
        }

        public ReplayPaceZone ZoneAt(double t)
        {
            if (!TryGetMeeting(t, out (double Start, double End) m)) return ReplayPaceZone.Explore;
            return t - AnchorAt(t, m.Start) < TalkWindowSec
                ? ReplayPaceZone.MeetingTalk
                : ReplayPaceZone.MeetingSilent;
        }

        public float SpeedAt(double t, bool fast) => SpeedOf(ZoneAt(t), fast);

        public static float SpeedOf(ReplayPaceZone zone, bool fast)
        {
            switch (zone)
            {
                case ReplayPaceZone.MeetingTalk: return fast ? TalkFast : TalkNormal;
                case ReplayPaceZone.MeetingSilent: return fast ? SilentFast : SilentNormal;
                default: return fast ? ExploreFast : ExploreNormal;
            }
        }

        public double Advance(double t, double dtRealSec, bool fast)
        {
            if (dtRealSec <= 0) return t;
            double remaining = dtRealSec;
            for (int guard = 0; guard < 10000 && remaining > 0; guard++)
            {
                double speed = SpeedAt(t, fast);
                double boundary = NextBoundaryAfter(t, out bool isChat);
                double target = t + remaining * speed;
                if (target < boundary) return target;
                remaining -= (boundary - t) / speed;
                t = boundary;
                if (isChat) return t;
            }
            return t;
        }

        public double RealSecondsBetween(double t0, double t1, bool fast)
        {
            if (t1 <= t0) return 0;
            double total = 0;
            double t = t0;
            for (int guard = 0; guard < 10000 && t < t1; guard++)
            {
                double speed = SpeedAt(t, fast);
                double boundary = NextBoundaryAfter(t, out _);
                double end = boundary < t1 ? boundary : t1;
                total += (end - t) / speed;
                t = end;
            }
            return total;
        }

        private double NextBoundaryAfter(double t, out bool isChat)
        {
            isChat = false;
            double next = double.PositiveInfinity;
            int ci = ReplayPlayback.LastIndexAtOrBefore(_chats, t) + 1;
            if (ci < _chats.Count)
            {
                next = _chats[ci];
                isChat = true;
            }

            if (TryGetMeeting(t, out (double Start, double End) m))
            {
                if (m.End < next)
                {
                    next = m.End;
                    isChat = false;
                }
                double onset = AnchorAt(t, m.Start) + TalkWindowSec;
                if (onset > t && onset < next)
                {
                    next = onset;
                    isChat = false;
                }
            }
            else
            {
                for (int i = 0; i < _meetings.Count; i++)
                {
                    if (_meetings[i].Start <= t) continue;
                    if (_meetings[i].Start < next)
                    {
                        next = _meetings[i].Start;
                        isChat = false;
                    }
                    break;
                }
            }
            return next;
        }

        private double AnchorAt(double t, double meetingStart)
        {
            int idx = ReplayPlayback.LastIndexAtOrBefore(_chats, t);
            double lastChat = idx >= 0 ? _chats[idx] : double.NegativeInfinity;
            return lastChat > meetingStart ? lastChat : meetingStart;
        }

        private bool TryGetMeeting(double t, out (double Start, double End) meeting)
        {
            for (int i = 0; i < _meetings.Count; i++)
            {
                if (t >= _meetings[i].Start && t < _meetings[i].End)
                {
                    meeting = _meetings[i];
                    return true;
                }
            }
            meeting = default;
            return false;
        }
    }
}
