using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class DiscussionImpactTests
    {

        [Theory]
        [InlineData(-0.01f)]
        [InlineData(DiscussionImpact.TotalSec)]
        [InlineData(DiscussionImpact.TotalSec + 1f)]
        public void Compute_OutsideDuration_IsHidden(float t)
        {
            Assert.False(DiscussionImpact.Compute(t).Visible);
        }

        [Fact]
        public void Compute_AtStart_IsFullyOffscreenAndOpaque()
        {
            ImpactState s = DiscussionImpact.Compute(0f);
            Assert.True(s.Visible);
            Assert.Equal(DiscussionImpact.StartOffsetXPx, s.SlideOffsetX, 3);
            Assert.Equal(1f, s.Alpha, 3);
            Assert.Equal(0f, s.FlyOffset, 3);
        }

        [Fact]
        public void Compute_SlideIn_ClosesTheDistanceMonotonically()
        {
            float prev = float.MaxValue;
            for (int i = 0; i <= 10; i++)
            {
                float t = DiscussionImpact.SlideSec * i / 10f;
                float offset = DiscussionImpact.Compute(t).SlideOffsetX;
                Assert.True(offset < prev, $"t={t} で走り込みが戻っている");
                prev = offset;
            }
            Assert.Equal(0f, prev, 3);
        }

        [Fact]
        public void Compute_SlideIn_AcceleratesTowardImpact()
        {
            float half = DiscussionImpact.StartOffsetXPx
                - DiscussionImpact.Compute(DiscussionImpact.SlideSec * 0.5f).SlideOffsetX;
            Assert.True(half < DiscussionImpact.StartOffsetXPx * 0.5f,
                "前半で半分以上進んでいる＝減速している");
        }

        [Fact]
        public void Compute_AtImpact_HasArrivedAndJitterIsMaximal()
        {
            ImpactState s = DiscussionImpact.Compute(DiscussionImpact.ImpactSec);
            Assert.Equal(0f, s.SlideOffsetX, 3);
            Assert.Equal(1f, s.JitterK, 3);
            Assert.Equal(0f, s.Recoil, 3);
        }

        [Fact]
        public void Compute_BeforeImpact_HasNoJitter()
        {
            Assert.Equal(0f, DiscussionImpact.Compute(DiscussionImpact.SlideSec * 0.5f).JitterK, 3);
        }

        [Fact]
        public void Compute_Recoil_PushesOutwardThenReturns()
        {
            float mid = DiscussionImpact.Compute(
                DiscussionImpact.ImpactSec + DiscussionImpact.RecoilSec * 0.5f).Recoil;
            float end = DiscussionImpact.Compute(
                DiscussionImpact.ImpactSec + DiscussionImpact.RecoilSec).Recoil;
            Assert.Equal(DiscussionImpact.RecoilPx, mid, 3);
            Assert.Equal(0f, end, 3);
        }

        [Fact]
        public void Compute_DuringHold_SettlesToHoldRatioAndStaysVisible()
        {
            float justBeforeFly = DiscussionImpact.SlideSec + DiscussionImpact.HoldSec - 0.001f;
            ImpactState s = DiscussionImpact.Compute(justBeforeFly);
            Assert.True(s.Visible);
            Assert.Equal(1f, s.Alpha, 3);
            Assert.Equal(0f, s.FlyOffset, 3);
            Assert.Equal(DiscussionImpact.JitterHoldRatio, s.JitterK, 2);
        }

        [Fact]
        public void Compute_Fly_MovesAwayAndFadesOut()
        {
            float flyStart = DiscussionImpact.SlideSec + DiscussionImpact.HoldSec;
            ImpactState begin = DiscussionImpact.Compute(flyStart);
            ImpactState mid = DiscussionImpact.Compute(flyStart + DiscussionImpact.FlySec * 0.5f);
            ImpactState last = DiscussionImpact.Compute(
                flyStart + DiscussionImpact.FlySec - 0.001f);

            Assert.Equal(0f, begin.FlyOffset, 3);
            Assert.True(mid.FlyOffset > begin.FlyOffset);
            Assert.True(last.FlyOffset > mid.FlyOffset);
            Assert.True(last.FlyOffset <= DiscussionImpact.FlyDistPx);

            Assert.True(mid.Alpha < begin.Alpha);
            Assert.True(last.Alpha < mid.Alpha);
            Assert.True(last.Alpha >= 0f);
        }

        [Fact]
        public void Compute_Fly_LeavesFasterThanItArrives()
        {
            float flyStart = DiscussionImpact.SlideSec + DiscussionImpact.HoldSec;
            float half = DiscussionImpact.Compute(flyStart + DiscussionImpact.FlySec * 0.5f).FlyOffset;
            Assert.True(half > DiscussionImpact.FlyDistPx * 0.5f);
        }

        [Fact]
        public void ImpactSec_IsTheEndOfSlideIn()
        {
            Assert.Equal(DiscussionImpact.SlideSec, DiscussionImpact.ImpactSec);
        }

        [Fact]
        public void TotalSec_IsTheSumOfThePhases()
        {
            Assert.Equal(
                DiscussionImpact.SlideSec + DiscussionImpact.HoldSec + DiscussionImpact.FlySec,
                DiscussionImpact.TotalSec);
        }

        [Fact]
        public void CharOffsetY_FirstCharGoesUpAndSecondGoesDown()
        {
            Assert.True(DiscussionImpact.CharOffsetY(0, 1f) > 0f);
            Assert.True(DiscussionImpact.CharOffsetY(1, 1f) < 0f);
        }

        [Fact]
        public void CharOffsetY_AlternatesDirectionAcrossThePattern()
        {
            for (int i = 0; i < DiscussionImpact.CharPattern.Length - 1; i++)
            {
                float a = DiscussionImpact.CharOffsetY(i, 1f);
                float b = DiscussionImpact.CharOffsetY(i + 1, 1f);
                Assert.True(a * b < 0f, $"{i} と {i + 1} が同じ向きにズレている");
            }
        }

        [Fact]
        public void CharOffsetY_WrapsAroundBeyondThePattern()
        {
            int len = DiscussionImpact.CharPattern.Length;
            Assert.Equal(DiscussionImpact.CharOffsetY(0, 1f), DiscussionImpact.CharOffsetY(len, 1f), 4);
        }

        [Fact]
        public void CharOffsetY_ScalesWithJitterStrength()
        {
            Assert.Equal(0f, DiscussionImpact.CharOffsetY(0, 0f), 4);
            Assert.Equal(
                DiscussionImpact.CharOffsetY(0, 1f) * 0.5f,
                DiscussionImpact.CharOffsetY(0, 0.5f), 4);
        }

        [Fact]
        public void CharOffsetY_StaysWithinTheConfiguredAmplitude()
        {
            for (int i = 0; i < DiscussionImpact.CharPattern.Length; i++)
            {
                Assert.True(System.Math.Abs(DiscussionImpact.CharOffsetY(i, 1f))
                    <= DiscussionImpact.CharOffsetPx + 0.001f);
            }
        }

        [Fact]
        public void CharOffsetY_NegativeIndex_IsFlat()
        {
            Assert.Equal(0f, DiscussionImpact.CharOffsetY(-1, 1f), 4);
        }

        [Theory]
        [InlineData("議論")]
        [InlineData("開始")]
        [InlineData("讨论")]
        [InlineData("토론")]
        [InlineData("시작")]
        [InlineData("はじめ")]
        [InlineData("カイギ")]
        public void IsSquareScript_SquareBlockScripts_AreTrue(string text)
        {
            Assert.True(DiscussionImpact.IsSquareScript(text));
        }

        [Theory]
        [InlineData("DISCUSS")]
        [InlineData("NOW!")]
        [InlineData("DEBATTE")]
        [InlineData("ДЕБАТЫ")]
        [InlineData("ΤΕΛΟΣ")]
        [InlineData("نقاش")]
        [InlineData("อภิปราย")]
        [InlineData("ﾄｰﾛﾝ")]
        [InlineData("議論 START")]
        public void IsSquareScript_OtherScripts_AreFalse(string text)
        {
            Assert.False(DiscussionImpact.IsSquareScript(text));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsSquareScript_EmptyOrBlank_IsFalse(string text)
        {
            Assert.False(DiscussionImpact.IsSquareScript(text));
        }

        [Fact]
        public void IsSquareScript_IgnoresWhitespaceBetweenSquareChars()
        {
            Assert.True(DiscussionImpact.IsSquareScript("議 論"));
        }

        [Theory]
        [InlineData("議論", "開始")]
        [InlineData("토론", "시작")]
        [InlineData("讨论", "开始")]
        public void ResolveUnit_SquareBlockPair_UsesPerChar(string left, string right)
        {
            Assert.Equal(ImpactJitterUnit.Char, DiscussionImpact.ResolveUnit(left, right));
        }

        [Theory]
        [InlineData("DISCUSS", "NOW!")]
        [InlineData("DEBATTE", "JETZT!")]
        [InlineData("ДЕБАТЫ", "СТАРТ")]
        [InlineData("議論", "START")]
        [InlineData("DISCUSS", "開始")]
        public void ResolveUnit_AnythingElse_UsesPerWord(string left, string right)
        {
            Assert.Equal(ImpactJitterUnit.Word, DiscussionImpact.ResolveUnit(left, right));
        }

        [Fact]
        public void ResolveUnit_OverlongSquareWord_FallsBackToPerWord()
        {
            var overlong = new string('議', DiscussionImpact.MaxCharUnitsPerWord + 1);
            Assert.Equal(ImpactJitterUnit.Word, DiscussionImpact.ResolveUnit(overlong, "開始"));
            Assert.Equal(ImpactJitterUnit.Word, DiscussionImpact.ResolveUnit("議論", overlong));
        }

        [Fact]
        public void ResolveUnit_AtTheCharUnitLimit_StillUsesPerChar()
        {
            var atLimit = new string('議', DiscussionImpact.MaxCharUnitsPerWord);
            Assert.Equal(ImpactJitterUnit.Char, DiscussionImpact.ResolveUnit(atLimit, atLimit));
        }

        [Theory]
        [InlineData(null, 0)]
        [InlineData("", 0)]
        [InlineData("議論", 2)]
        [InlineData("DISCUSS", 7)]
        [InlineData("A B", 2)]
        public void CountUnits_CountsNonWhitespaceChars(string text, int expected)
        {
            Assert.Equal(expected, DiscussionImpact.CountUnits(text));
        }

        [Fact]
        public void HoldSec_MatchesTheClapperInterval()
        {
            Assert.Equal(0.975f, DiscussionImpact.HoldSec, 3);
        }
    }
}
