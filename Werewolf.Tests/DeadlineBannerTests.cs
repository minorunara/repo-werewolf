using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class DeadlineBannerTests
    {
        private const float RestWidth = 667f;
        private static readonly float StartX = DeadlineBanner.StartOffsetX(RestWidth, fromLeft: true);

        private static BannerRowState At(float t) => DeadlineBanner.Compute(t, StartX);

        private static readonly float Stop = DeadlineBanner.SlideInSec;
        private static readonly float PopStart = Stop + DeadlineBanner.PopDelaySec;
        private static readonly float PopEnd = PopStart + DeadlineBanner.PopSec;
        private static readonly float ExitStart = Stop + DeadlineBanner.HoldSec;
        private static readonly float End = ExitStart + DeadlineBanner.ExitSec;

        [Fact]
        public void StartOffsetX_PutsTextFullyOffscreenOnTheCorrectSide()
        {
            float left = DeadlineBanner.StartOffsetX(RestWidth, fromLeft: true);
            float right = DeadlineBanner.StartOffsetX(RestWidth, fromLeft: false);

            Assert.True(left < 0f, "上段は左（負）から走り出す");
            Assert.True(right > 0f, "下段は右（正）から走り出す");
            Assert.Equal(-left, right, 3);
            Assert.True(left + RestWidth * 0.5f < -DeadlineBanner.ReferenceWidthPx * 0.5f);
        }

        [Fact]
        public void SlideIn_IsLinear_AndStopsDeadCenter()
        {
            Assert.Equal(StartX, At(0f).Main.OffsetX, 2);
            Assert.Equal(StartX * 0.5f, At(Stop * 0.5f).Main.OffsetX, 2);
            Assert.Equal(StartX * 0.25f, At(Stop * 0.75f).Main.OffsetX, 2);
            Assert.Equal(0f, At(Stop).Main.OffsetX, 3);
        }

        [Fact]
        public void SlideIn_StillAtFullSpeedOneFrameBeforeStopping()
        {
            const float frame = 1f / 60f;
            float expected = StartX * (frame / DeadlineBanner.SlideInSec);
            Assert.Equal(expected, At(Stop - frame).Main.OffsetX, 2);
        }

        [Fact]
        public void Main_StaysCenteredAndUnscaledUntilPopStarts()
        {
            BannerLayerState atStop = At(Stop).Main;
            Assert.True(atStop.Visible);
            Assert.Equal(1f, atStop.Scale, 4);
            Assert.Equal(1f, At(PopStart - 0.01f).Main.Scale, 4);
        }

        [Fact]
        public void SlideGhost_TracksTheMainExactlyWhileMoving()
        {
            Assert.Equal(At(0f).Main.OffsetX, At(0f).SlideGhost.OffsetX, 3);
            Assert.Equal(At(Stop * 0.5f).Main.OffsetX, At(Stop * 0.5f).SlideGhost.OffsetX, 3);
        }

        [Fact]
        public void SlideGhost_OvertakesTheMainAndLeavesTheOppositeSide()
        {
            BannerLayerState after = At(Stop + 0.1f).SlideGhost;
            Assert.True(after.Visible);
            Assert.True(after.OffsetX > 0f, "上段の残像は停止後に右へ抜ける");

            float later = At(Stop + 0.3f).SlideGhost.OffsetX;
            Assert.True(later > after.OffsetX, "抜ける方向は単調");
        }

        [Fact]
        public void SlideGhost_PeaksAtGhostOpacityAndFadesOutOnSchedule()
        {
            Assert.Equal(DeadlineBanner.GhostOpacity, At(0f).SlideGhost.Alpha, 3);
            Assert.True(At(DeadlineBanner.GhostFadeSec - 0.01f).SlideGhost.Alpha < 0.02f);
            Assert.False(At(DeadlineBanner.GhostFadeSec + 0.001f).SlideGhost.Visible);
            Assert.False(At(DeadlineBanner.GhostFadeSec + 0.5f).SlideGhost.Visible);
        }

        [Fact]
        public void Pop_ReachesPopScaleAndHoldsIt()
        {
            Assert.Equal(DeadlineBanner.PopScale, At(PopEnd).Main.Scale, 3);
            Assert.Equal(DeadlineBanner.PopScale, At(PopEnd + 1f).Main.Scale, 3);
        }

        [Fact]
        public void Pop_OvershootsPastPopScale_BackOut()
        {
            bool overshot = false;
            for (float t = PopStart; t < PopEnd; t += 0.005f)
            {
                if (At(t).Main.Scale > DeadlineBanner.PopScale) { overshot = true; break; }
            }
            Assert.True(overshot, "BackOut の行き過ぎが出ていない");
        }

        [Fact]
        public void PopGhost_AppearsWithThePop_GrowsBeyondTheMain_AndStaysCentered()
        {
            Assert.False(At(PopStart - 0.01f).PopGhost.Visible);

            float mid = PopStart + DeadlineBanner.PopGhostFadeSec * 0.5f;
            BannerRowState state = At(mid);
            Assert.True(state.PopGhost.Visible);
            Assert.True(state.PopGhost.Scale > state.Main.Scale, "手前の残像は本体より前へ出る");
            Assert.Equal(0f, state.PopGhost.OffsetX, 4);
        }

        [Fact]
        public void PopGhost_FadesOutOnSchedule()
        {
            float lastDrawn = PopStart + DeadlineBanner.PopGhostFadeSec - (1f / 60f);
            Assert.True(At(lastDrawn).PopGhost.Alpha < 0.1f,
                $"消滅直前の残像が濃すぎる: {At(lastDrawn).PopGhost.Alpha}");
            Assert.False(At(PopStart + DeadlineBanner.PopGhostFadeSec + 0.001f).PopGhost.Visible);
        }

        [Fact]
        public void Alpha_StartsFaint_AndKeepsRampingUntilTheExitTakesOver()
        {
            Assert.Equal(DeadlineBanner.StartOpacity, At(0f).Main.Alpha, 3);
            Assert.Equal(1f, DeadlineBanner.RampAlpha(DeadlineBanner.FadeInSec), 3);
            float atExit = At(ExitStart - 0.01f).Main.Alpha;
            Assert.True(atExit > 0.9f, $"退場直前の濃さが薄すぎる: {atExit}");
            Assert.Equal(DeadlineBanner.RampAlpha(ExitStart - 0.01f), atExit, 3);
        }

        [Fact]
        public void Alpha_IncreasesMonotonicallyUntilTheExit()
        {
            float prev = -1f;
            for (float t = 0f; t < ExitStart; t += 0.01f)
            {
                float a = At(t).Main.Alpha;
                Assert.True(a >= prev - 1e-5f, $"t={t} で濃さが下がった ({prev} → {a})");
                prev = a;
            }
        }

        [Fact]
        public void Exit_GrowsTowardTheViewerWhileFadingOut()
        {
            float mid = ExitStart + DeadlineBanner.ExitSec * 0.5f;
            BannerLayerState state = At(mid).Main;
            Assert.True(state.Visible);
            Assert.True(state.Scale > DeadlineBanner.PopScale, "退場では手前へ拡大する");
            Assert.True(state.Alpha < 1f && state.Alpha > 0f);

            Assert.Equal(DeadlineBanner.ExitScale, At(End - 0.001f).Main.Scale, 2);
            Assert.True(At(End - 0.001f).Main.Alpha < 0.02f);
        }

        [Fact]
        public void Main_DisappearsWhenTheExitCompletes()
        {
            Assert.False(At(End).Main.Visible);
            Assert.False(At(End + 1f).Main.Visible);
        }

        [Fact]
        public void PopGhost_DoesNotReappearDuringTheExit()
        {
            for (float t = ExitStart; t <= End; t += 0.01f)
            {
                Assert.False(At(t).PopGhost.Visible);
            }
        }

        private static BannerEmojiState Emoji(float t) => DeadlineBanner.ComputeEmoji(t, 0);

        private static readonly float EmojiExitStart =
            DeadlineBanner.Line2StaggerSec + DeadlineBanner.SlideInSec + DeadlineBanner.HoldSec;

        [Fact]
        public void Emoji_HiddenBeforeItsDelay()
        {
            Assert.False(Emoji(-0.5f).Visible);
            Assert.False(Emoji(DeadlineBanner.EmojiDelaySec - 0.01f).Visible);
        }

        [Fact]
        public void Emoji_InvisibleAtTheEdge_AndMaterializesWhileRolling()
        {
            BannerEmojiState atEdge = Emoji(DeadlineBanner.EmojiDelaySec + 0.01f);
            Assert.True(atEdge.Visible);
            Assert.True(atEdge.Alpha < 0.01f, $"画面端で既に見えている: {atEdge.Alpha}");

            float prev = -1f;
            for (float t = DeadlineBanner.EmojiDelaySec; t < EmojiExitStart; t += 0.05f)
            {
                float a = Emoji(t).Alpha;
                Assert.True(a >= prev - 1e-5f, $"t={t} で濃さが下がった ({prev} → {a})");
                prev = a;
            }
            Assert.True(prev > 0.3f, $"退場直前でもまだ薄すぎる: {prev}");
        }

        [Fact]
        public void Emoji_FirstOneRollsLeftToRightThroughTheCenterLane()
        {
            BannerEmojiState early = Emoji(DeadlineBanner.EmojiDelaySec + 0.2f);
            BannerEmojiState later = Emoji(DeadlineBanner.EmojiDelaySec + 1.2f);
            Assert.True(early.CenterX < 0f, "1個目は左の画面外から入る");
            Assert.True(later.CenterX > early.CenterX, "右へ転がり続ける");
            Assert.Equal(0f, early.CenterY, 3);
            Assert.Equal(0f, later.CenterY, 3);
        }

        [Fact]
        public void Emoji_RotationIsTravelOverRadius()
        {
            const float lt = 1.5f;
            BannerEmojiState state = Emoji(DeadlineBanner.EmojiDelaySec + lt);
            float travel = DeadlineBanner.EmojiSpeedPxPerSec * lt;
            Assert.Equal(travel / (DeadlineBanner.EmojiSizePx * 0.5f), state.RotationRad, 3);
        }

        [Fact]
        public void Emoji_FadesWithTheTextExit_AndNeverComesBack()
        {
            Assert.True(Emoji(EmojiExitStart + DeadlineBanner.ExitSec - 0.01f).Alpha < 0.05f);
            for (float t = EmojiExitStart + DeadlineBanner.ExitSec; t < 12f; t += 0.25f)
            {
                Assert.False(DeadlineBanner.ComputeEmoji(t, 0).Visible);
            }
        }

        [Fact]
        public void Emoji_AllValuesStayFiniteAndAlphaInRange()
        {
            for (float t = -0.2f; t <= DeadlineBanner.TotalSec + 0.5f; t += 0.01f)
            {
                BannerEmojiState state = Emoji(t);
                Assert.False(float.IsNaN(state.CenterX) || float.IsInfinity(state.CenterX));
                Assert.False(float.IsNaN(state.RotationRad) || float.IsInfinity(state.RotationRad));
                if (state.Visible) Assert.InRange(state.Alpha, 0f, 1f);
            }
        }

        [Fact]
        public void TotalSec_CoversEveryLayer()
        {
            Assert.Equal(DeadlineBanner.RowTotalSec + DeadlineBanner.Line2StaggerSec, DeadlineBanner.TotalSec, 4);
            BannerRowState last = At(DeadlineBanner.TotalSec);
            Assert.False(last.Main.Visible);
            Assert.False(last.SlideGhost.Visible);
            Assert.False(last.PopGhost.Visible);
        }

        [Fact]
        public void NegativeTime_IsFullyHidden()
        {
            BannerRowState before = At(-0.5f);
            Assert.False(before.Main.Visible);
            Assert.False(before.SlideGhost.Visible);
            Assert.False(before.PopGhost.Visible);
        }

        [Fact]
        public void AllValuesStayFiniteAndAlphaInRangeAcrossTheWholeTimeline()
        {
            for (float t = -0.2f; t <= DeadlineBanner.TotalSec + 0.5f; t += 0.005f)
            {
                BannerRowState state = At(t);
                foreach (BannerLayerState layer in new[] { state.Main, state.SlideGhost, state.PopGhost })
                {
                    Assert.False(float.IsNaN(layer.OffsetX) || float.IsInfinity(layer.OffsetX));
                    Assert.False(float.IsNaN(layer.Scale) || float.IsInfinity(layer.Scale));
                    Assert.False(float.IsNaN(layer.Alpha) || float.IsInfinity(layer.Alpha));
                    if (layer.Visible)
                    {
                        Assert.InRange(layer.Alpha, 0f, 1f);
                        Assert.True(layer.Scale > 0f);
                    }
                }
            }
        }
    }
}
