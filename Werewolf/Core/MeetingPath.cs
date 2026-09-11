using System;
using System.Collections.Generic;
using Werewolf.Core.Replay;

namespace Werewolf.Core
{
    public sealed class MeetingPath
    {
        public const float PlaybackSpeed = 32f;

        public const double TrailSeconds = 15.0;

        public const float WarpJumpMeters = 8f;

        public const float StillMeters = 0.1f;

        public const double BlinkPeriodSec = 0.9;

        public const double BlinkPeak = 1.2;

        private readonly List<ReplayTrailPoint> _points;

        private MeetingPath(List<ReplayTrailPoint> points)
        {
            _points = points;
        }

        public IReadOnlyList<ReplayTrailPoint> Points => _points;

        public int Count => _points.Count;
        public ReplayTrailPoint Start => _points[0];
        public ReplayTrailPoint Goal => _points[_points.Count - 1];
        public double StartT => Start.T;
        public double EndT => Goal.T;
        public double DurationSec => EndT - StartT;

        public double CycleSec => DurationSec / PlaybackSpeed;

        public static MeetingPath Build(
            IReadOnlyList<ReplayTrailPoint> track,
            IReadOnlyList<double> warpTimes,
            IReadOnlyList<double> resumeTimes)
        {
            if (track == null || warpTimes == null || warpTimes.Count == 0) return null;
            double warpT = warpTimes[warpTimes.Count - 1];
            double resumeT = double.NegativeInfinity;
            if (resumeTimes != null)
            {
                for (int i = 0; i < resumeTimes.Count; i++)
                {
                    if (resumeTimes[i] < warpT && resumeTimes[i] > resumeT) resumeT = resumeTimes[i];
                }
            }

            List<ReplayTrailPoint> best = null;
            List<ReplayTrailPoint> run = null;
            ReplayTrailPoint prev = default;
            for (int i = 0; i < track.Count; i++)
            {
                ReplayTrailPoint p = track[i];
                if (p.T < resumeT || p.T > warpT) continue;
                bool jump = run != null && IsJump(prev, p);
                if (run == null || jump)
                {
                    if (run != null && (best == null || run.Count >= best.Count)) best = run;
                    run = new List<ReplayTrailPoint>();
                }
                run.Add(p);
                prev = p;
            }
            if (run != null && (best == null || run.Count >= best.Count)) best = run;
            return best != null && best.Count > 0 ? new MeetingPath(TrimStill(best)) : null;
        }

        private static List<ReplayTrailPoint> TrimStill(List<ReplayTrailPoint> points)
        {
            int last = points.Count - 1;
            int head = 0;
            while (head < last && IsStill(points[0], points[head + 1])) head++;
            int tail = last;
            while (tail > head && IsStill(points[last], points[tail - 1])) tail--;
            if (head == 0 && tail == last) return points;
            return points.GetRange(head, tail - head + 1);
        }

        public static MeetingPath FromRecorder(ReplayRecorder recorder, int actor)
        {
            if (recorder == null) return null;
            var track = new List<ReplayTrailPoint>();
            var warps = new List<double>();
            var resumes = new List<double>();
            recorder.OpenSegmentPlayerTrackInto(actor, track);
            recorder.OpenSegmentEventTimesInto(ReplayEvents.MeetWarp, warps);
            recorder.OpenSegmentEventTimesInto(ReplayEvents.Phase, resumes,
                ReplayEvents.PhaseToKey, GamePhase.Play.ToString());
            return Build(track, warps, resumes);
        }

        public void DecimatedInto(int maxPoints, List<ReplayTrailPoint> into)
        {
            int stride = maxPoints <= 0 ? 1 : Math.Max(1, (_points.Count + maxPoints - 1) / maxPoints);
            for (int i = 0; i < _points.Count; i += stride) into.Add(_points[i]);
            if ((_points.Count - 1) % stride != 0) into.Add(Goal);
        }

        public void TrailInto(double pathT, int maxPoints, List<ReplayTrailPoint> into)
        {
            if (into == null) return;
            double toT = Math.Max(StartT, Math.Min(EndT, pathT));
            double fromT = Math.Max(StartT, toT - TrailSeconds);
            into.Add(PointAt(fromT));
            if (toT <= fromT) return;

            int first = FirstIndexAfter(fromT);
            int stop = FirstIndexAtOrAfter(toT);
            int interiorCount = stop - first;
            int budget = maxPoints <= 0 ? interiorCount : Math.Max(0, Math.Max(2, maxPoints) - 2);
            if (budget > 0)
            {
                int stride = Math.Max(1, (interiorCount + budget - 1) / budget);
                for (int i = first; i < stop; i += stride) into.Add(_points[i]);
            }
            into.Add(PointAt(toT));
        }

        public double PathTimeAt(double realElapsedSec)
        {
            if (DurationSec <= 0) return StartT;
            double cycle = CycleSec;
            double phase = realElapsedSec % cycle;
            if (phase < 0) phase += cycle;
            double t = StartT + phase * PlaybackSpeed;
            return t > EndT ? EndT : t;
        }

        public static float BlinkBrightness(double realElapsedSec)
        {
            double u = realElapsedSec % BlinkPeriodSec;
            if (u < 0) u += BlinkPeriodSec;
            u /= BlinkPeriodSec;
            double b = BlinkPeak * 4.0 * u * (1.0 - u);
            return b >= 1.0 ? 1f : (float)b;
        }

        public void PositionAt(double t, out float x, out float z)
        {
            if (t <= StartT)
            {
                x = Start.X;
                z = Start.Z;
                return;
            }
            if (t >= EndT)
            {
                x = Goal.X;
                z = Goal.Z;
                return;
            }
            int lo = 0, hi = _points.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (_points[mid].T <= t) lo = mid;
                else hi = mid;
            }
            ReplayTrailPoint a = _points[lo];
            ReplayTrailPoint b = _points[hi];
            double dt = b.T - a.T;
            float f = dt <= 0 ? 0f : (float)((t - a.T) / dt);
            x = a.X + (b.X - a.X) * f;
            z = a.Z + (b.Z - a.Z) * f;
        }

        private ReplayTrailPoint PointAt(double t)
        {
            PositionAt(t, out float x, out float z);
            return new ReplayTrailPoint { T = t, X = x, Z = z };
        }

        private int FirstIndexAfter(double t)
        {
            int lo = 0, hi = _points.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (_points[mid].T <= t) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        private int FirstIndexAtOrAfter(double t)
        {
            int lo = 0, hi = _points.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (_points[mid].T < t) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        private static bool IsJump(ReplayTrailPoint a, ReplayTrailPoint b)
        {
            float dx = b.X - a.X;
            float dz = b.Z - a.Z;
            return dx * dx + dz * dz > WarpJumpMeters * WarpJumpMeters;
        }

        private static bool IsStill(ReplayTrailPoint anchor, ReplayTrailPoint p)
        {
            float dx = p.X - anchor.X;
            float dz = p.Z - anchor.Z;
            return dx * dx + dz * dz < StillMeters * StillMeters;
        }
    }
}
