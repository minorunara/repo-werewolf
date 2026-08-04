using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class BellScheduleTests
    {

        [Theory]
        [InlineData(600, 0)]
        [InlineData(301, 0)]
        [InlineData(300, 30)]
        [InlineData(181, 30)]
        [InlineData(180, 10)]
        [InlineData(61, 10)]
        [InlineData(60, 5)]
        [InlineData(31, 5)]
        [InlineData(30, 3)]
        [InlineData(11, 3)]
        [InlineData(10, 1)]
        [InlineData(1, 1)]
        [InlineData(0, 0)]
        public void IntervalSecFor_MatchesTierTable(long remainingSec, int expectedInterval)
        {
            Assert.Equal(expectedInterval, BellSchedule.IntervalSecFor(remainingSec));
        }

        [Theory]
        [InlineData(300_001, false)]
        [InlineData(300_000, true)]
        [InlineData(1_000, true)]
        [InlineData(0, true)]
        public void AlertActive_ThresholdIsFiveMinutes(long remainingMs, bool expected)
        {
            Assert.Equal(expected, BellSchedule.AlertActive(remainingMs));
        }

        [Fact]
        public void Tick_CrossingFiveMinuteMark_RingsOnce()
        {
            var bell = new BellSchedule();
            Assert.Equal(0, bell.Tick(400_000));

            Assert.Equal(0, bell.Tick(300_001));
            Assert.Equal(300, bell.Tick(300_000));
            Assert.Equal(0, bell.Tick(299_999));
            Assert.Equal(0, bell.Tick(270_001));
            Assert.Equal(270, bell.Tick(269_500));
        }

        [Fact]
        public void Tick_MultipleMarksInOneFrame_RingsOnceWithLowestMark()
        {
            var bell = new BellSchedule();
            bell.Tick(310_000);

            Assert.Equal(270, bell.Tick(265_000));
            Assert.Equal(0, bell.Tick(264_000));
        }

        [Fact]
        public void Tick_ArmedInsideAlertZone_DoesNotRingRetroactively()
        {
            var bell = new BellSchedule();

            Assert.Equal(0, bell.Tick(200_000));
            Assert.Equal(0, bell.Tick(199_000));
            Assert.Equal(180, bell.Tick(180_000));
        }

        [Fact]
        public void Tick_RemainingJumpsUp_RearmsWithoutRinging()
        {
            var bell = new BellSchedule();
            Assert.Equal(0, bell.Tick(0));

            Assert.Equal(0, bell.Tick(400_000));
            Assert.Equal(300, bell.Tick(300_000));
        }

        [Fact]
        public void Tick_SmallJitterIncrease_DoesNotRearmAndDoesNotDoubleRing()
        {
            var bell = new BellSchedule();
            bell.Tick(310_000);
            Assert.Equal(300, bell.Tick(299_900));

            Assert.Equal(0, bell.Tick(300_100));
            Assert.Equal(0, bell.Tick(299_800));
        }

        [Fact]
        public void Tick_ThirtySecondTier_RingsEveryThreeSeconds()
        {
            var bell = new BellSchedule();
            bell.Tick(32_000);

            Assert.Equal(0, bell.Tick(31_000));
            Assert.Equal(30, bell.Tick(30_000));
            Assert.Equal(0, bell.Tick(28_000));
            Assert.Equal(27, bell.Tick(27_000));
            Assert.Equal(0, bell.Tick(25_000));
            Assert.Equal(24, bell.Tick(24_000));
        }

        [Fact]
        public void Tick_ThirtySecondTier_HandsOffToOneSecondTierAtTen()
        {
            var bell = new BellSchedule();
            bell.Tick(14_000);

            Assert.Equal(0, bell.Tick(13_000));
            Assert.Equal(12, bell.Tick(12_000));
            Assert.Equal(0, bell.Tick(11_000));
            Assert.Equal(10, bell.Tick(10_000));
            Assert.Equal(9, bell.Tick(9_000));
        }

        [Fact]
        public void Tick_FinalTenSeconds_RingsEverySecond()
        {
            var bell = new BellSchedule();
            bell.Tick(12_000);

            Assert.Equal(0, bell.Tick(10_500));
            Assert.Equal(10, bell.Tick(10_000));
            Assert.Equal(9, bell.Tick(9_000));
            Assert.Equal(8, bell.Tick(8_000));
            Assert.Equal(0, bell.Tick(7_500));
            Assert.Equal(7, bell.Tick(6_800));
            Assert.Equal(1, bell.Tick(500));
            Assert.Equal(0, bell.Tick(0));
        }

        [Fact]
        public void Reset_AllowsNewRoundToRearm()
        {
            var bell = new BellSchedule();
            bell.Tick(310_000);
            Assert.Equal(300, bell.Tick(299_000));

            bell.Reset();

            Assert.Equal(0, bell.Tick(400_000));
            Assert.Equal(300, bell.Tick(300_000));
        }

        [Fact]
        public void VolumeScaleFor_AboveTenSeconds_IsBaseVolume()
        {
            Assert.Equal(BellSchedule.BaseVolumeScale, BellSchedule.VolumeScaleFor(300));
            Assert.Equal(BellSchedule.BaseVolumeScale, BellSchedule.VolumeScaleFor(60));
            Assert.Equal(BellSchedule.BaseVolumeScale, BellSchedule.VolumeScaleFor(15));
        }

        [Fact]
        public void VolumeScaleFor_FinalTier_RampsLinearlyToDouble()
        {
            Assert.Equal(0.5f, BellSchedule.VolumeScaleFor(10), precision: 5);
            Assert.Equal(1.0f, BellSchedule.VolumeScaleFor(1), precision: 5);

            float prev = BellSchedule.VolumeScaleFor(10);
            for (int mark = 9; mark >= 1; mark--)
            {
                float v = BellSchedule.VolumeScaleFor(mark);
                Assert.True(v > prev, $"mark={mark} の音量 {v} が直前 {prev} より大きくない");
                prev = v;
            }
        }
    }
}
