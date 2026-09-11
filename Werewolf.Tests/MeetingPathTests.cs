using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingPathTests
    {
        private static ReplayTrailPoint P(double t, float x, float z = 0f)
            => new ReplayTrailPoint { T = t, X = x, Z = z };

        private static List<ReplayTrailPoint> Walk(double fromT, double toT, float fromX, float toX, double step = 0.25)
        {
            var list = new List<ReplayTrailPoint>();
            int n = (int)((toT - fromT) / step);
            for (int i = 0; i <= n; i++)
            {
                double t = fromT + i * step;
                float f = n == 0 ? 0f : (float)i / n;
                list.Add(P(t, fromX + (toX - fromX) * f));
            }
            return list;
        }

        [Fact]
        public void DecimatedInto_KeepsStrideAndAlwaysEndsAtGoal()
        {
            MeetingPath path = MeetingPath.Build(Walk(0, 10, 0f, 5f, 1.0), new[] { 10.0 }, null);
            Assert.NotNull(path);
            Assert.Equal(11, path.Count);
            var into = new List<ReplayTrailPoint>();
            path.DecimatedInto(4, into);
            Assert.Equal(new[] { 0.0, 3.0, 6.0, 9.0, 10.0 }, into.ConvertAll(p => p.T));

            path = MeetingPath.Build(Walk(0, 9, 0f, 5f, 1.0), new[] { 9.0 }, null);
            into.Clear();
            path.DecimatedInto(4, into);
            Assert.Equal(new[] { 0.0, 3.0, 6.0, 9.0 }, into.ConvertAll(p => p.T));

            into.Clear();
            path.DecimatedInto(100, into);
            Assert.Equal(10, into.Count);
        }

        [Fact]
        public void Build_WindowIsLastWarpAndLastResumeBeforeIt()
        {
            var track = new List<ReplayTrailPoint>();
            track.AddRange(Walk(0, 10, 0f, 5f));
            track.AddRange(Walk(20, 30, 100f, 105f));
            MeetingPath path = MeetingPath.Build(track, new[] { 10.0, 30.0 }, new[] { 20.0 });
            Assert.NotNull(path);
            Assert.Equal(20.0, path.StartT);
            Assert.Equal(30.0, path.EndT);
            Assert.Equal(100f, path.Start.X);
            Assert.Equal(105f, path.Goal.X);
        }

        [Fact]
        public void Build_NoResumeEvent_StartsFromFirstSample()
        {
            var track = Walk(0, 10, 0f, 5f);
            MeetingPath path = MeetingPath.Build(track, new[] { 10.0 }, new double[0]);
            Assert.NotNull(path);
            Assert.Equal(0.0, path.StartT);
            Assert.Equal(10.0, path.EndT);
        }

        [Fact]
        public void Build_DropsTeleportRuns_KeepsLongest()
        {
            var track = new List<ReplayTrailPoint>
            {
                P(20.0, 100f), P(20.25, 100f),
            };
            track.AddRange(Walk(20.5, 30.0, 0f, 5f));
            track.Add(P(30.0, 100f));
            MeetingPath path = MeetingPath.Build(track, new[] { 30.0 }, new[] { 20.0 });
            Assert.NotNull(path);
            Assert.Equal(20.5, path.StartT);
            Assert.Equal(30.0, path.EndT);
            Assert.Equal(0f, path.Start.X);
            Assert.Equal(5f, path.Goal.X);
            Assert.All(path.Points, p => Assert.True(p.X <= 5f));
        }

        [Fact]
        public void Build_NoWarp_ReturnsNull()
        {
            Assert.Null(MeetingPath.Build(Walk(0, 10, 0f, 5f), new double[0], new double[0]));
            Assert.Null(MeetingPath.Build(Walk(0, 10, 0f, 5f), null, null));
        }

        [Fact]
        public void Build_EmptyWindow_ReturnsNull()
        {
            var track = Walk(0, 10, 0f, 5f);
            Assert.Null(MeetingPath.Build(track, new[] { 30.0 }, new[] { 20.0 }));
        }

        [Fact]
        public void Build_TrimsStillSamplesAtBothEnds()
        {
            var track = new List<ReplayTrailPoint>();
            track.AddRange(Walk(20, 21, 0f, 0f));
            track.AddRange(Walk(21.25, 25, 0.5f, 8f));
            track.AddRange(Walk(25.25, 27, 8f, 8f));
            MeetingPath path = MeetingPath.Build(track, new[] { 27.0 }, new[] { 20.0 });
            Assert.NotNull(path);
            Assert.Equal(21.0, path.StartT);
            Assert.Equal(25.0, path.EndT);
            Assert.Equal(0f, path.Start.X);
            Assert.Equal(8f, path.Goal.X);
        }

        [Fact]
        public void Build_AllStill_KeepsLastPoint()
        {
            MeetingPath path = MeetingPath.Build(Walk(0, 10, 3f, 3f), new[] { 10.0 }, null);
            Assert.NotNull(path);
            Assert.Equal(1, path.Count);
            Assert.Equal(10.0, path.StartT);
            Assert.Equal(3f, path.Start.X);
        }

        [Fact]
        public void PathTimeAt_LoopsAtPlaybackSpeedWithoutHold()
        {
            double duration = MeetingPath.PlaybackSpeed * 4.0;
            MeetingPath path = MeetingPath.Build(Walk(0, duration, 0f, 100f, 1.0), new[] { duration }, null);
            Assert.NotNull(path);
            Assert.Equal(duration, path.DurationSec);
            Assert.Equal(4.0, path.CycleSec, 6);

            Assert.Equal(0.0, path.PathTimeAt(0.0), 6);
            Assert.Equal(MeetingPath.PlaybackSpeed, path.PathTimeAt(1.0), 6);
            Assert.Equal(duration - MeetingPath.PlaybackSpeed * 0.001, path.PathTimeAt(3.999), 6);
            Assert.Equal(0.0, path.PathTimeAt(4.0), 6);
            Assert.Equal(MeetingPath.PlaybackSpeed, path.PathTimeAt(5.0), 6);
        }

        [Fact]
        public void TrailInto_KeepsOnlyRecentWindowWithInterpolatedEnds()
        {
            MeetingPath path = MeetingPath.Build(
                new[] { P(0.0, 0f), P(10.0, 1f), P(20.0, 2f), P(30.0, 3f) },
                new[] { 30.0 }, null);
            Assert.NotNull(path);
            var into = new List<ReplayTrailPoint>();

            path.TrailInto(23.0, 100, into);

            Assert.Equal(new[] { 8.0, 10.0, 20.0, 23.0 }, into.ConvertAll(p => p.T));
            Assert.Equal(0.8f, into[0].X, 3);
            Assert.Equal(2.3f, into[3].X, 3);
        }

        [Fact]
        public void TrailInto_AtLoopStart_DoesNotCarryPreviousCycle()
        {
            MeetingPath path = MeetingPath.Build(Walk(0, 30, 0f, 3f, 1.0), new[] { 30.0 }, null);
            Assert.NotNull(path);
            var into = new List<ReplayTrailPoint>();

            path.TrailInto(path.PathTimeAt(path.CycleSec), 100, into);

            ReplayTrailPoint only = Assert.Single(into);
            Assert.Equal(path.StartT, only.T);
            Assert.Equal(path.Start.X, only.X);
        }

        [Fact]
        public void TrailInto_RespectsPointLimitAndKeepsWindowEnds()
        {
            MeetingPath path = MeetingPath.Build(Walk(0, 30, 0f, 3f, 0.25), new[] { 30.0 }, null);
            Assert.NotNull(path);
            var into = new List<ReplayTrailPoint>();

            path.TrailInto(30.0, 5, into);

            Assert.InRange(into.Count, 2, 5);
            Assert.Equal(path.EndT - MeetingPath.TrailSeconds, into[0].T);
            Assert.Equal(path.EndT, into[into.Count - 1].T);

            into.Clear();
            path.TrailInto(30.0, 2, into);
            Assert.Equal(2, into.Count);
            Assert.Equal(path.EndT - MeetingPath.TrailSeconds, into[0].T);
            Assert.Equal(path.EndT, into[1].T);
        }

        [Fact]
        public void BlinkBrightness_BouncesWithClampedBrightPlateau()
        {
            double period = MeetingPath.BlinkPeriodSec;
            Assert.Equal(0f, MeetingPath.BlinkBrightness(0.0), 6);
            Assert.Equal(0f, MeetingPath.BlinkBrightness(period), 6);
            Assert.Equal(1f, MeetingPath.BlinkBrightness(period * 0.5), 6);
            Assert.Equal(1f, MeetingPath.BlinkBrightness(period * 0.3), 6);
            Assert.Equal(1f, MeetingPath.BlinkBrightness(period * 0.7), 6);
            Assert.True(MeetingPath.BlinkBrightness(period * 0.1) > 0.4f);
            int bright = 0, dark = 0;
            for (int i = 0; i < 1000; i++)
            {
                float b = MeetingPath.BlinkBrightness(period * i / 1000.0);
                if (b >= 1f) bright++;
                else if (b < 0.5f) dark++;
            }
            Assert.True(bright > dark, $"bright={bright} dark={dark}");
            Assert.Equal(MeetingPath.BlinkBrightness(period * 0.2),
                MeetingPath.BlinkBrightness(period * 7.2), 5);
        }

        [Fact]
        public void PathTimeAt_SinglePoint_StaysAtStart()
        {
            MeetingPath path = MeetingPath.Build(new[] { P(5.0, 1f, 2f) }, new[] { 6.0 }, null);
            Assert.NotNull(path);
            Assert.Equal(5.0, path.PathTimeAt(123.0));
            path.PositionAt(99.0, out float x, out float z);
            Assert.Equal(1f, x);
            Assert.Equal(2f, z);
        }

        [Fact]
        public void PositionAt_InterpolatesAndClamps()
        {
            MeetingPath path = MeetingPath.Build(
                new[] { P(0.0, 0f, 0f), P(1.0, 4f, 0f), P(3.0, 4f, 6f) }, new[] { 3.0 }, null);
            Assert.NotNull(path);
            Assert.Equal(3, path.Count);
            path.PositionAt(0.5, out float x, out float z);
            Assert.Equal(2f, x, 3);
            Assert.Equal(0f, z, 3);
            path.PositionAt(2.0, out x, out z);
            Assert.Equal(4f, x, 3);
            Assert.Equal(3f, z, 3);
            path.PositionAt(-1.0, out x, out z);
            Assert.Equal(0f, x, 3);
            path.PositionAt(9.0, out x, out z);
            Assert.Equal(6f, z, 3);
        }

        [Fact]
        public void FromRecorder_ReadsOwnTrackAndMeetingEvents()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(new ReplaySegmentHeader
            {
                LevelName = "Level - Test",
                StartedAtIso = "2026-08-29T00:00:00+09:00",
                IsHost = true,
                LocalActor = 1,
            }, 100.0);
            rec.NoteEvent(100.0, "phase", ("to", "Play"));
            for (int i = 0; i <= 12; i++)
            {
                double now = 100.0 + i * 0.25;
                rec.Sample(now, new[]
                {
                    new ReplayEntitySample(ReplayEntityKind.Player, 2, 50f, 0f, 0f),
                    new ReplayEntitySample(ReplayEntityKind.Player, 1, i * 0.25f, 0f, 0f),
                });
            }
            rec.NoteEvent(103.0, "meet_warp");
            rec.Sample(103.25, new[] { new ReplayEntitySample(ReplayEntityKind.Player, 1, 100f, 0f, 0f) });

            MeetingPath path = MeetingPath.FromRecorder(rec, 1);
            Assert.NotNull(path);
            Assert.Equal(13, path.Count);
            Assert.Equal(0.0, path.StartT);
            Assert.Equal(3.0, path.EndT);
            Assert.Equal(0f, path.Start.X);
            Assert.Equal(3f, path.Goal.X, 3);
            Assert.All(path.Points, p => Assert.True(p.X < 50f));

            Assert.Null(MeetingPath.FromRecorder(rec, 3));
        }

        [Fact]
        public void FromRecorder_NoOpenSegment_ReturnsNull()
        {
            var rec = new ReplayRecorder();
            Assert.Null(MeetingPath.FromRecorder(rec, 1));
            Assert.Null(MeetingPath.FromRecorder(null, 1));
        }

        [Fact]
        public void Recorder_EventTimes_FilterByFieldValue()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(new ReplaySegmentHeader { LevelName = "L", StartedAtIso = "x", LocalActor = 1 }, 0.0);
            rec.NoteEvent(1.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(2.0, "phase", ("to", "Play"));
            rec.NoteEvent(3.0, "meet", ("a", 1), ("kind", "Emergency"));
            var times = new List<double>();
            rec.OpenSegmentEventTimesInto("phase", times, "to", "Play");
            Assert.Equal(new[] { 2.0 }, times);
            times.Clear();
            rec.OpenSegmentEventTimesInto("phase", times);
            Assert.Equal(new[] { 1.0, 2.0 }, times);
            times.Clear();
            rec.OpenSegmentEventTimesInto("meet", times, "missing", "x");
            Assert.Empty(times);
        }
    }
}
