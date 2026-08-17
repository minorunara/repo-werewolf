using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayDanmakuTests
    {
        private static readonly Func<ReplayDanmakuComment, double> FixedWidth = _ => 0.2;

        private static readonly List<(double, double)> Meetings
            = new List<(double, double)> { (100.0, 200.0) };

        private static ReplayDanmaku Build(
            List<(double, int, string)> chats,
            List<(double, int)> results = null)
        {
            var times = new List<double>();
            if (chats != null) foreach ((double t, int _, string _) in chats) times.Add(t);
            return new ReplayDanmaku(
                chats ?? new List<(double, int, string)>(),
                results ?? new List<(double, int)>(),
                new ReplayPace(Meetings, times));
        }

        [Fact]
        public void Step_SpawnsOnForwardPass_Once()
        {
            ReplayDanmaku d = Build(new List<(double, int, string)> { (110.0, 1, "こんにちは") });
            d.Step(109.0, 110.5, 0.1, FixedWidth);
            Assert.Single(d.Active);
            Assert.Equal(1, d.Active[0].Actor);
            Assert.Equal(0.0, d.Active[0].Elapsed, 6);

            d.Step(110.5, 110.6, 0.1, FixedWidth);
            Assert.Single(d.Active);
        }

        [Fact]
        public void Step_NoSpawnWithoutForwardMotion()
        {
            ReplayDanmaku d = Build(new List<(double, int, string)> { (110.0, 1, "a") });
            d.Step(110.0, 110.0, 0.1, FixedWidth);
            Assert.Empty(d.Active);
        }

        [Fact]
        public void Step_RetiresAfterTotalSeconds()
        {
            ReplayDanmaku d = Build(new List<(double, int, string)> { (110.0, 1, "やあ") });
            d.Step(109.0, 110.1, 0.1, FixedWidth);
            Assert.Single(d.Active);

            d.Step(110.1, 110.2, 1.79, FixedWidth);
            Assert.Single(d.Active);
            d.Step(110.2, 110.3, 0.02, FixedWidth);
            Assert.Empty(d.Active);
        }

        [Fact]
        public void Step_PausedPresentationClock_FreezesPosition()
        {
            ReplayDanmaku d = Build(new List<(double, int, string)> { (110.0, 1, "テスト") });
            d.Step(109.0, 110.1, 0.1, FixedWidth);
            d.Step(110.1, 110.1, 0.0, FixedWidth);
            Assert.Equal(0.0, d.Active[0].Elapsed, 6);
        }

        [Fact]
        public void CenterXAt_EntersFromSpeakerSide_ThenLocksInPlace()
        {
            var right = new ReplayDanmakuComment
            {
                Actor = 1, WidthRatio = 0.2, DwellSec = 1.5,
                HandoffElapsed = ReplayDanmaku.HandoffSec,
            };
            var left = new ReplayDanmakuComment
            {
                Actor = 2, WidthRatio = 0.2, DwellSec = 1.5,
                HandoffElapsed = ReplayDanmaku.HandoffSec,
            };

            Assert.Equal(0.65, ReplayDanmaku.CenterXAt(right, 0), 6);
            Assert.Equal(0.10, ReplayDanmaku.CenterXAt(right, ReplayChatText.SlideInSec), 6);
            Assert.Equal(ReplayDanmaku.CenterXAt(right, 0.8),
                ReplayDanmaku.CenterXAt(right, 1.2), 6);
            Assert.Equal(-0.65, ReplayDanmaku.CenterXAt(left, 0), 6);
            Assert.Equal(-0.10, ReplayDanmaku.CenterXAt(left, ReplayChatText.SlideInSec), 6);
        }

        [Fact]
        public void CenterXAt_EntryDecelerates_NaturalExitRecedesSameSide()
        {
            var c = new ReplayDanmakuComment
            {
                Actor = 1, WidthRatio = 0.2, DwellSec = 1.5,
                HandoffElapsed = ReplayDanmaku.HandoffSec,
            };
            double half = ReplayChatText.SlideInSec * 0.5;
            double d1 = ReplayDanmaku.CenterXAt(c, 0) - ReplayDanmaku.CenterXAt(c, half);
            double d2 = ReplayDanmaku.CenterXAt(c, half)
                - ReplayDanmaku.CenterXAt(c, ReplayChatText.SlideInSec);
            Assert.True(d1 > d2);
            double outStart = ReplayChatText.SlideInSec + c.DwellSec;
            Assert.True(ReplayDanmaku.CenterXAt(c, c.TotalSec)
                > ReplayDanmaku.CenterXAt(c, outStart));
        }

        [Fact]
        public void OpacityAt_FadesIn_Holds_ThenFadesOut()
        {
            var c = new ReplayDanmakuComment
            {
                Actor = 1, DwellSec = 1.5, HandoffElapsed = ReplayDanmaku.HandoffSec,
            };
            double inMid = ReplayChatText.SlideInSec * 0.5;
            double outStart = ReplayChatText.SlideInSec + c.DwellSec;
            double outMid = outStart + ReplayChatText.SlideOutSec * 0.5;

            Assert.Equal(0.0, ReplayDanmaku.OpacityAt(c, 0.0), 6);
            Assert.Equal(0.5, ReplayDanmaku.OpacityAt(c, inMid), 6);
            Assert.Equal(1.0, ReplayDanmaku.OpacityAt(c, ReplayChatText.SlideInSec), 6);
            Assert.Equal(1.0, ReplayDanmaku.OpacityAt(c, 1.0), 6);
            Assert.Equal(0.5, ReplayDanmaku.OpacityAt(c, outMid), 6);
            Assert.Equal(0.0, ReplayDanmaku.OpacityAt(c, c.TotalSec), 6);
        }

        [Fact]
        public void VisualProfile_IsDeterministicAcrossSeekRebuild()
        {
            var chats = new List<(double, int, string)> { (110.125, 3, "同じ発言") };
            ReplayDanmaku d = Build(chats);
            d.Step(109.0, 110.2, 0.01, FixedWidth);
            ReplayDanmakuComment first = d.Active.Single();
            uint seed = first.VisualSeed;
            ReplayDanmakuProfile profile = first.Profile;
            Assert.Equal(841917769u, seed);
            Assert.Equal(ReplayDanmakuProfile.Impact, profile);

            d.RebuildAtSeek(110.2, fast: false, FixedWidth);
            ReplayDanmakuComment rebuilt = d.Active.Single();
            Assert.Equal(seed, rebuilt.VisualSeed);
            Assert.Equal(profile, rebuilt.Profile);
            Assert.Equal(ReplayDanmaku.TiltSign(first), ReplayDanmaku.TiltSign(rebuilt));
            Assert.Equal(ReplayDanmaku.TrajectoryTiltSign(first),
                ReplayDanmaku.TrajectoryTiltSign(rebuilt));
            Assert.Equal(ReplayDanmaku.RestingTiltDegrees(first),
                ReplayDanmaku.RestingTiltDegrees(rebuilt), 6);
        }

        [Fact]
        public void ClaimProfile_GivesMotionAReasonFromTextShape()
        {
            Assert.Equal(ReplayDanmakuProfile.Impact,
                ReplayDanmaku.ProfileForClaim("違う", 2));
            Assert.Equal(ReplayDanmakuProfile.Impact,
                ReplayDanmaku.ProfileForClaim("その証言で本当に合っていますか？", 16));
            Assert.Equal(ReplayDanmakuProfile.Slash,
                ReplayDanmaku.ProfileForClaim("こっちは金庫を回収したよ", 13));
            Assert.Equal(ReplayDanmakuProfile.Cool,
                ReplayDanmaku.ProfileForClaim(new string('あ', 31) + "！", 31));
        }

        [Fact]
        public void SpeakerTrajectory_KeepsHomeAndDirectionAcrossDifferentUtterances()
        {
            var first = new ReplayDanmakuComment
            {
                Actor = 6,
                VisualSeed = ReplayDanmaku.VisualSeedFor(6, 110.125, 2),
            };
            var later = new ReplayDanmakuComment
            {
                Actor = 6,
                VisualSeed = ReplayDanmaku.VisualSeedFor(6, 145.750, 37),
            };

            Assert.NotEqual(first.VisualSeed, later.VisualSeed);
            Assert.Equal(ReplayDanmaku.HomeSlotFor(first.Actor), ReplayDanmaku.HomeSlotFor(later.Actor));
            Assert.Equal(ReplayDanmaku.TiltSign(first), ReplayDanmaku.TiltSign(later));
            Assert.Equal(ReplayDanmaku.TrajectoryTiltSign(first),
                ReplayDanmaku.TrajectoryTiltSign(later));
            Assert.Equal(4, Enumerable.Range(1, 8)
                .Select(ReplayDanmaku.HomeSlotFor).Distinct().Count());
        }

        [Fact]
        public void SpeakerHomeSlot_IsPreferred_AndCollisionFallsBackNearby()
        {
            ReplayDanmaku d = Build(new List<(double, int, string)>());
            ReplayDanmakuComment first = d.SpawnAdHoc(5, "最初", FixedWidth);
            Assert.Equal(ReplayDanmaku.HomeSlotFor(5), first.Slot);

            int[] sameHomeActors = Enumerable.Range(1, 100)
                .Where(actor => ReplayDanmaku.HomeSlotFor(actor) == 1)
                .Take(4).ToArray();
            d.ClearActive();
            foreach (int actor in sameHomeActors) d.SpawnAdHoc(actor, "応答", FixedWidth);
            Assert.Equal(new[] { 1, 0, 2, 3 }, d.Active.Select(c => c.Slot).ToArray());

            d.ClearActive();
            ReplayDanmakuComment later = d.SpawnAdHoc(5, "別の発言", FixedWidth);
            Assert.Equal(first.Slot, later.Slot);
        }

        [Fact]
        public void RestingTilt_FollowsEntryTrajectory_FromBothSides()
        {
            foreach (int actor in new[] { 1, 2 })
            {
                var c = new ReplayDanmakuComment
                {
                    Actor = actor,
                    Slot = 1,
                    WidthRatio = 0.2,
                    DwellSec = 1.5,
                    VisualSeed = 456u,
                    Profile = ReplayDanmakuProfile.Slash,
                    HandoffElapsed = ReplayDanmaku.HandoffSec,
                };
                double dx = ReplayDanmaku.CenterXAt(c, ReplayChatText.SlideInSec)
                    - ReplayDanmaku.CenterXAt(c, 0);
                double dy = ReplayDanmaku.CenterYRatioAt(c, ReplayChatText.SlideInSec)
                    - ReplayDanmaku.CenterYRatioAt(c, 0);

                Assert.Equal(Math.Sign(dy / dx), Math.Sign(ReplayDanmaku.RestingTiltDegrees(c)));
                Assert.InRange(Math.Abs(ReplayDanmaku.RestingTiltDegrees(c)), 2.5, 3.5);
            }
        }

        [Fact]
        public void FontHierarchyAndScale_EnlargeShortClaims_ThenHold()
        {
            var c = new ReplayDanmakuComment
            {
                DisplayChars = 8,
                DwellSec = 1.5,
                Profile = ReplayDanmakuProfile.Impact,
                HandoffElapsed = ReplayDanmaku.HandoffSec,
            };

            Assert.Equal(1.55, ReplayDanmaku.FontScaleFor(c), 6);
            Assert.Equal(1.27, ReplayDanmaku.FontScaleFor(
                new ReplayDanmakuComment { DisplayChars = 20 }), 6);
            Assert.Equal(1.09, ReplayDanmaku.FontScaleFor(
                new ReplayDanmakuComment { DisplayChars = 34 }), 6);
            Assert.Equal(0.95, ReplayDanmaku.FontScaleFor(
                new ReplayDanmakuComment { DisplayChars = 50 }), 6);
            Assert.Equal(0.70, ReplayDanmaku.ScaleAt(c, 0.0), 6);
            Assert.Equal(1.18, ReplayDanmaku.ScaleAt(c,
                ReplayChatText.SlideInSec * 0.70), 6);
            Assert.Equal(1.0, ReplayDanmaku.ScaleAt(c, ReplayChatText.SlideInSec), 6);
            Assert.Equal(1.0, ReplayDanmaku.ScaleAt(c, 1.0), 6);
            Assert.True(ReplayDanmaku.ScaleAt(c, c.TotalSec - 0.01) < 1.0);
        }

        [Fact]
        public void ReadableFocus_HasNoHorizontalDriftOrShake()
        {
            var c = new ReplayDanmakuComment
            {
                Actor = 1,
                Slot = 1,
                WidthRatio = 0.2,
                DwellSec = 1.5,
                VisualSeed = 123u,
                Profile = ReplayDanmakuProfile.Slash,
                HandoffElapsed = ReplayDanmaku.HandoffSec,
            };

            ReplayDanmaku.LandingShakeAt(c, 1.0, out double x, out double y);
            Assert.Equal(ReplayDanmaku.RestingTiltDegrees(c),
                ReplayDanmaku.RotationDegreesAt(c, 1.0), 6);
            Assert.Equal(ReplayDanmaku.CenterXAt(c, 0.8), ReplayDanmaku.CenterXAt(c, 1.0), 6);
            Assert.Equal(ReplayDanmaku.CenterYRatioAt(c, 0.8), ReplayDanmaku.CenterYRatioAt(c, 1.0), 6);
            Assert.Equal(0.0, x, 6);
            Assert.Equal(0.0, y, 6);
            Assert.Equal(0.0, ReplayDanmaku.AccentOpacityAt(c, 1.0), 6);
        }

        [Fact]
        public void LandingShakeAndAccent_AreLimitedToArrivalWindow()
        {
            var c = new ReplayDanmakuComment
            {
                Actor = 1,
                DwellSec = 1.5,
                VisualSeed = 456u,
                Profile = ReplayDanmakuProfile.Impact,
            };

            double initialWidth = ReplayDanmaku.AccentWidthScaleAt(c, 0);
            Assert.InRange(initialWidth, 0.01, 0.99);
            Assert.True(ReplayDanmaku.AccentWidthScaleAt(c, 0.035) > initialWidth);
            Assert.Equal(1.0, ReplayDanmaku.AccentWidthScaleAt(c, 0.1), 6);
            Assert.True(ReplayDanmaku.AccentCenterOffsetFactorAt(c, 0) > 0);
            var fromLeft = new ReplayDanmakuComment
            {
                Actor = 2,
                Profile = ReplayDanmakuProfile.Impact,
            };
            Assert.True(ReplayDanmaku.AccentCenterOffsetFactorAt(fromLeft, 0) < 0);

            Assert.True(ReplayDanmaku.AccentOpacityAt(c, 0.1) > 0);
            ReplayDanmaku.LandingShakeAt(c, ReplayChatText.SlideInSec + 0.05,
                out double x, out double y);
            Assert.True(Math.Abs(x) + Math.Abs(y) > 0);
            ReplayDanmaku.LandingShakeAt(c,
                ReplayChatText.SlideInSec + ReplayDanmaku.LandingShakeSec,
                out x, out y);
            Assert.Equal(0.0, x, 6);
            Assert.Equal(0.0, y, 6);
            Assert.Equal(0.0, ReplayDanmaku.AccentOpacityAt(c, 0.6), 6);
        }

        [Fact]
        public void IdleMotion_UsesThreeNeutralStyles_WithSharedViewerKnownValues()
        {
            ReplayDanmakuComment BuildIdle(uint seed) => new ReplayDanmakuComment
            {
                Actor = 3,
                DwellSec = 2.5,
                VisualSeed = seed,
                Depth = 0,
                DepthFrom = 0,
                HandoffElapsed = ReplayDanmaku.HandoffSec,
            };

            ReplayDanmakuComment still = BuildIdle(841917768u);
            ReplayDanmakuComment glide = BuildIdle(841917769u);
            ReplayDanmakuComment floating = BuildIdle(841917770u);
            Assert.Equal(ReplayDanmakuIdleStyle.Still, ReplayDanmaku.IdleStyleFor(still));
            Assert.Equal(ReplayDanmakuIdleStyle.Glide, ReplayDanmaku.IdleStyleFor(glide));
            Assert.Equal(ReplayDanmakuIdleStyle.Float, ReplayDanmaku.IdleStyleFor(floating));

            ReplayDanmaku.IdleMotionAt(still, 1.0, out double stillX, out double stillY);
            Assert.Equal(0.0, stillX, 9);
            Assert.Equal(0.0, stillY, 9);

            ReplayDanmaku.IdleMotionAt(glide, 1.0, out double glideX, out double glideY);
            Assert.Equal(-9.931487180724458, glideX, 9);
            Assert.Equal(-0.48421914607893757, glideY, 9);
            ReplayDanmaku.IdleMotionAt(floating, 1.0, out double floatX, out double floatY);
            Assert.Equal(-6.999999880406848, floatX, 9);
            Assert.Equal(-3.999471395910293, floatY, 9);
        }

        [Fact]
        public void IdleMotion_WeakensContinuouslyWithDepth_AndConnectsAtLifetimeEdges()
        {
            var focus = new ReplayDanmakuComment
            {
                Actor = 3,
                DwellSec = 2.5,
                VisualSeed = 841917770u,
                HandoffElapsed = ReplayDanmaku.HandoffSec,
            };
            ReplayDanmaku.IdleMotionAt(focus, ReplayChatText.SlideInSec,
                out double enterX, out double enterY);
            Assert.Equal(0.0, enterX, 9);
            Assert.Equal(0.0, enterY, 9);
            ReplayDanmaku.IdleMotionAt(focus, focus.TotalSec,
                out double exitX, out double exitY);
            Assert.Equal(0.0, exitX, 9);
            Assert.Equal(0.0, exitY, 9);

            ReplayDanmaku.IdleMotionAt(focus, 1.0, out double focusX, out _);
            var depthThree = new ReplayDanmakuComment
            {
                Actor = focus.Actor,
                DwellSec = focus.DwellSec,
                VisualSeed = focus.VisualSeed,
                Depth = 3,
                DepthFrom = 3,
                HandoffElapsed = ReplayDanmaku.HandoffSec,
            };
            ReplayDanmaku.IdleMotionAt(depthThree, 1.0, out double echoX, out _);
            Assert.Equal(0.25, ReplayDanmaku.IdleDepthFactor(depthThree), 9);
            Assert.Equal(focusX * 0.25, echoX, 9);

            depthThree.Depth = 4;
            depthThree.DepthFrom = 3;
            depthThree.HandoffElapsed = ReplayDanmaku.HandoffSec * 0.5;
            Assert.Equal(0.125, ReplayDanmaku.IdleDepthFactor(depthThree), 9);
        }

        [Fact]
        public void Rotation_StaysSlightlyTiltedAfterAccentSettles()
        {
            var c = new ReplayDanmakuComment
            {
                DwellSec = 1.5,
                VisualSeed = 456u,
                Profile = ReplayDanmakuProfile.Slash,
            };

            Assert.NotEqual(0.0, ReplayDanmaku.RotationDegreesAt(c, ReplayChatText.SlideInSec));
            Assert.True(ReplayDanmaku.AccentOpacityAt(c, ReplayChatText.SlideInSec) > 0);
            double settledAt = ReplayChatText.SlideInSec + ReplayDanmaku.ArrivalTiltSettleSec;
            Assert.Equal(ReplayDanmaku.RestingTiltDegrees(c),
                ReplayDanmaku.RotationDegreesAt(c, settledAt), 6);
            Assert.InRange(Math.Abs(ReplayDanmaku.RotationDegreesAt(c, settledAt)), 2.5, 3.5);
            Assert.Equal(0.0, ReplayDanmaku.AccentOpacityAt(c, settledAt), 6);

            var other = new ReplayDanmakuComment
            {
                DwellSec = 1.5,
                VisualSeed = 789u,
                Profile = ReplayDanmakuProfile.Slash,
            };
            Assert.NotEqual(ReplayDanmaku.RestingTiltDegrees(c),
                ReplayDanmaku.RestingTiltDegrees(other));
        }

        [Fact]
        public void NewClaim_DemotesPreviousWithContinuousDepthTransition()
        {
            ReplayDanmaku d = Build(new List<(double, int, string)>());
            ReplayDanmakuComment first = d.SpawnAdHoc(1, "最初の主張", FixedWidth);
            ReplayDanmakuComment second = d.SpawnAdHoc(2, "反論", FixedWidth);

            Assert.Equal(1, first.Depth);
            Assert.Equal(0, second.Depth);
            Assert.Equal(0.0, ReplayDanmaku.VisualDepthAt(first), 6);
            d.Step(0, 0, ReplayDanmaku.HandoffSec * 0.5, FixedWidth);
            Assert.Equal(0.5, ReplayDanmaku.VisualDepthAt(first), 6);
            d.Step(0, 0, ReplayDanmaku.HandoffSec * 0.5, FixedWidth);
            Assert.Equal(1.0, ReplayDanmaku.VisualDepthAt(first), 6);
            Assert.True(ReplayDanmaku.ScaleAt(first, first.Elapsed)
                < ReplayDanmaku.ScaleAt(second, second.Elapsed));
            Assert.Equal(ReplayDanmaku.OpacityAt(first, first.Elapsed),
                ReplayDanmaku.OpacityAt(second, second.Elapsed), 6);
        }

        [Fact]
        public void FocusNaturalExit_ClearsOlderEchoesInsteadOfResurfacingThem()
        {
            ReplayDanmaku d = Build(new List<(double, int, string)>());
            d.SpawnAdHoc(1, new string('長', 50), FixedWidth);
            ReplayDanmakuComment focus = d.SpawnAdHoc(2, "短", FixedWidth);

            d.Step(0, 0, focus.TotalSec + 0.01, FixedWidth);
            Assert.Empty(d.Active);
        }

        [Fact]
        public void NearSimultaneousClaims_GetDistinctSlotsAndDepths()
        {
            var chats = new List<(double, int, string)>
            {
                (110.0, 1, "ひとつめのコメント"),
                (110.5, 2, "ふたつめのコメント"),
                (111.0, 3, "みっつめのコメント"),
                (111.5, 4, "よっつめのコメント"),
            };
            ReplayDanmaku d = Build(chats);
            d.Step(109.0, 112.0, 0.1, FixedWidth);
            Assert.Equal(4, d.Active.Count);
            Assert.Equal(4, d.Active.Select(c => c.Slot).Distinct().Count());
            Assert.Equal(new[] { 3, 2, 1, 0 }, d.Active.Select(c => c.Depth).ToArray());
            d.Step(112.0, 112.0, ReplayDanmaku.HandoffSec, FixedWidth);
            Assert.Equal(new[] { 0.56, 0.68, 0.82, 1.0 },
                d.Active.Select(c => ReplayDanmaku.ScaleAt(c, 1.0)).ToArray());
            Assert.All(d.Active, c => Assert.Equal(1.0, ReplayDanmaku.OpacityAt(c, 1.0), 6));
        }

        [Fact]
        public void FifthClaim_FadesOldestEcho_ThenReturnsIt()
        {
            ReplayDanmaku d = Build(new List<(double, int, string)>());
            for (int actor = 1; actor <= 4; actor++)
                d.SpawnAdHoc(actor, "コメント" + actor, FixedWidth);
            d.Step(0, 0, ReplayDanmaku.HandoffSec, FixedWidth);

            ReplayDanmakuComment oldest = d.Active.Single(c => c.Actor == 1);
            int recycledSlot = oldest.Slot;
            d.SpawnAdHoc(5, "コメント5", FixedWidth);

            Assert.Equal(ReplayDanmaku.MaxVisibleShots + 1, d.Active.Count);
            Assert.Equal(4, oldest.Depth);
            Assert.Equal(1.0, ReplayDanmaku.OpacityAt(oldest, oldest.Elapsed), 6);
            Assert.Contains(d.Active, c => c.Actor == 5 && c.Depth == 0);
            Assert.Equal(recycledSlot, d.Active.Single(c => c.Actor == 5).Slot);

            d.Step(0, 0, ReplayDanmaku.HandoffSec * 0.5, FixedWidth);
            Assert.Contains(d.Active, c => c.Actor == 1);
            Assert.Equal(0.5, ReplayDanmaku.OpacityAt(oldest, oldest.Elapsed), 6);

            d.Step(0, 0, ReplayDanmaku.HandoffSec * 0.5, FixedWidth);
            Assert.Equal(ReplayDanmaku.MaxVisibleShots, d.Active.Count);
            Assert.DoesNotContain(d.Active, c => c.Actor == 1);
            Assert.Equal(new[] { 3, 2, 1, 0 }, d.Active.Select(c => c.Depth).ToArray());
        }

        [Fact]
        public void Stamp_ShowsOnForwardPass_AndFadesOut()
        {
            ReplayDanmaku d = Build(
                new List<(double, int, string)>(),
                new List<(double, int)> { (150.0, 3) });

            Assert.False(d.TryGetStamp(out _, out _));
            d.Step(149.0, 150.5, 0.1, FixedWidth);
            Assert.True(d.TryGetStamp(out int actor, out double progress));
            Assert.Equal(3, actor);
            Assert.Equal(0.0, progress, 6);

            d.Step(150.5, 150.6, 1.0, FixedWidth);
            Assert.True(d.TryGetStamp(out _, out progress));
            Assert.Equal(1.0 / ReplayDanmaku.StampLifeSec, progress, 6);

            d.Step(150.6, 150.7, 0.6, FixedWidth);
            Assert.False(d.TryGetStamp(out _, out _));
        }

        [Fact]
        public void Stamp_NotRestoredBySeek_ButFutureOnesStillFire()
        {
            ReplayDanmaku d = Build(
                new List<(double, int, string)>(),
                new List<(double, int)> { (150.0, -1) });

            d.RebuildAtSeek(151.0, fast: false, FixedWidth);
            Assert.False(d.TryGetStamp(out _, out _));
            d.Step(151.0, 152.0, 0.1, FixedWidth);
            Assert.False(d.TryGetStamp(out _, out _));

            d.RebuildAtSeek(149.0, fast: false, FixedWidth);
            d.Step(149.0, 150.5, 0.1, FixedWidth);
            Assert.True(d.TryGetStamp(out int actor, out _));
            Assert.Equal(-1, actor);
        }

        [Fact]
        public void RebuildAtSeek_RestoresElapsedFromPaceIntegral()
        {
            var chats = new List<(double, int, string)>
            {
                (110.0, 1, "こんにちは"),
                (111.0, 2, "やあどうも"),
            };
            ReplayDanmaku d = Build(chats);
            d.RebuildAtSeek(112.0, fast: false, FixedWidth);

            Assert.Equal(2, d.Active.Count);
            ReplayDanmakuComment c1 = d.Active.First(c => c.Actor == 1);
            ReplayDanmakuComment c2 = d.Active.First(c => c.Actor == 2);
            Assert.Equal(0.5, c1.Elapsed, 6);
            Assert.Equal(0.25, c2.Elapsed, 6);
            Assert.Equal(1, c1.Depth);
            Assert.Equal(0, c2.Depth);
            Assert.Equal(0.25, c1.HandoffElapsed, 6);
        }

        [Fact]
        public void RebuildAtSeek_SkipsCommentsPastTheirOwnTotal()
        {
            var chats = new List<(double, int, string)>
            {
                (110.0, 1, "あ"),
                (110.5, 2, new string('い', 50)),
            };
            ReplayDanmaku d = Build(chats);
            d.RebuildAtSeek(120.0, fast: false, FixedWidth);
            Assert.Equal(2, d.Active.Count);

            d.RebuildAtSeek(129.0, fast: false, FixedWidth);
            Assert.Single(d.Active);
            Assert.Equal(2, d.Active[0].Actor);

            d.RebuildAtSeek(145.0, fast: false, FixedWidth);
            Assert.Empty(d.Active);
        }

        [Fact]
        public void RebuildAtSeek_DoesNotResurfaceEchoBehindExpiredLatestClaim()
        {
            var chats = new List<(double, int, string)>
            {
                (110.0, 1, new string('長', 50)),
                (110.5, 2, "短"),
            };
            ReplayDanmaku d = Build(chats);

            d.RebuildAtSeek(129.0, fast: false, FixedWidth);
            Assert.Empty(d.Active);
        }

        [Fact]
        public void RebuildAtSeek_ClearsActive_AndResetsForwardCursor()
        {
            var chats = new List<(double, int, string)> { (110.0, 1, "巻き戻しテスト") };
            ReplayDanmaku d = Build(chats);
            d.Step(109.0, 110.5, 0.1, FixedWidth);
            Assert.Single(d.Active);

            d.RebuildAtSeek(100.0, fast: false, FixedWidth);
            Assert.Empty(d.Active);

            d.Step(100.0, 111.0, 0.1, FixedWidth);
            Assert.Single(d.Active);
        }

        [Fact]
        public void Step_RewindToStart_ClearsActive_AndRespawnsOnSecondPass()
        {
            var chats = new List<(double, int, string)> { (110.0, 1, "二周目テスト") };
            var results = new List<(double, int)> { (150.0, 2) };
            ReplayDanmaku d = Build(chats, results);
            d.Step(109.0, 151.0, 0.1, FixedWidth);
            Assert.Single(d.Active);
            Assert.True(d.TryGetStamp(out _, out _));

            d.Step(151.0, 0.0, 0.0, FixedWidth);
            Assert.Empty(d.Active);
            Assert.False(d.TryGetStamp(out _, out _));

            d.Step(0.0, 110.5, 0.1, FixedWidth);
            Assert.Single(d.Active);
            Assert.Equal(1, d.Active[0].Actor);
            d.Step(110.5, 150.5, 0.1, FixedWidth);
            Assert.True(d.TryGetStamp(out int actor, out _));
            Assert.Equal(2, actor);
        }

        [Fact]
        public void Playback_CollectsChatsAndResults_AndBuildsPace()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(new ReplaySegmentHeader
            {
                LevelName = "L",
                StartedAtIso = "2026-08-11T00:00:00+09:00",
                IsHost = true,
                LocalActor = 1,
            }, 1000.0);
            rec.NoteEvent(1100.0, "phase", ("to", "Meeting"));
            rec.NoteEvent(1100.0, "meet_warp");
            rec.NoteEvent(1110.0, "chat", ("a", 2), ("text", "怪しいのは3だ"));
            rec.NoteEvent(1120.0, "meeting_result", ("a", 3));
            rec.NoteEvent(1130.0, "phase", ("to", "Play"));
            rec.EndSegment(1200.0);

            ReplayPlayback pb = ReplayPlayback.FromRecorder(rec);
            Assert.Single(pb.Chats);
            Assert.Equal((110.0, 2, "怪しいのは3だ"), pb.Chats[0]);
            Assert.Single(pb.MeetingResults);
            Assert.Equal((120.0, 3), pb.MeetingResults[0]);

            ReplayPace pace = pb.BuildPace();
            Assert.Equal(ReplayPaceZone.MeetingTalk, pace.ZoneAt(101.0));
            Assert.Equal(ReplayPaceZone.MeetingTalk, pace.ZoneAt(112.0));
            Assert.Equal(ReplayPaceZone.MeetingSilent, pace.ZoneAt(116.0));
            Assert.Equal(ReplayPaceZone.Explore, pace.ZoneAt(140.0));

            var d = new ReplayDanmaku(pb.Chats, pb.MeetingResults, pace);
            d.Step(99.0, 111.0, 0.1, FixedWidth);
            Assert.Single(d.Active);
            Assert.Equal("怪しいのは3だ", d.Active[0].Text);
        }
    }
}
