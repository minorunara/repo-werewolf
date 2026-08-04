using System;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GazeConeLockTests
    {
        private static bool UpdateAtDegrees(GazeConeLock lockObj, double degrees)
        {
            double rad = degrees * Math.PI / 180.0;
            return lockObj.Update((float)Math.Sin(rad), 0f, (float)Math.Cos(rad));
        }

        [Fact]
        public void FirstUpdate_LocksReferenceAndReturnsNotHeld()
        {
            var gaze = new GazeConeLock(coneDegrees: 15f);
            Assert.False(gaze.Update(0f, 0f, 1f));
        }

        [Fact]
        public void WithinCone_ReturnsHeld()
        {
            var gaze = new GazeConeLock(coneDegrees: 15f);
            UpdateAtDegrees(gaze, 0);
            Assert.True(UpdateAtDegrees(gaze, 0));
            Assert.True(UpdateAtDegrees(gaze, 5));
            Assert.True(UpdateAtDegrees(gaze, -5));
            Assert.True(UpdateAtDegrees(gaze, 14.9));
        }

        [Fact]
        public void BeyondCone_ResetsAndRelocksToNewDirection()
        {
            var gaze = new GazeConeLock(coneDegrees: 15f);
            UpdateAtDegrees(gaze, 0);
            Assert.False(UpdateAtDegrees(gaze, 30));
            Assert.True(UpdateAtDegrees(gaze, 30));
            Assert.True(UpdateAtDegrees(gaze, 40));
            Assert.False(UpdateAtDegrees(gaze, 0));
        }

        [Fact]
        public void SlowPan_EventuallyExitsConeAndResets()
        {
            var gaze = new GazeConeLock(coneDegrees: 15f);
            UpdateAtDegrees(gaze, 0);
            bool dropped = false;
            for (int deg = 2; deg <= 30; deg += 2)
            {
                if (!UpdateAtDegrees(gaze, deg)) dropped = true;
            }
            Assert.True(dropped);
        }

        [Fact]
        public void UnnormalizedInput_SameResultAsNormalized()
        {
            var gaze = new GazeConeLock(coneDegrees: 15f);
            gaze.Update(0f, 0f, 100f);
            Assert.True(gaze.Update(0f, 0f, 0.001f));
        }

        [Fact]
        public void ZeroVector_DropsHoldAndReference()
        {
            var gaze = new GazeConeLock(coneDegrees: 15f);
            UpdateAtDegrees(gaze, 0);
            Assert.True(UpdateAtDegrees(gaze, 0));
            Assert.False(gaze.Update(0f, 0f, 0f));
            Assert.False(UpdateAtDegrees(gaze, 0));
        }

        [Fact]
        public void Reset_RequiresRelock()
        {
            var gaze = new GazeConeLock(coneDegrees: 15f);
            UpdateAtDegrees(gaze, 0);
            Assert.True(UpdateAtDegrees(gaze, 0));
            gaze.Reset();
            Assert.False(UpdateAtDegrees(gaze, 0));
            Assert.True(UpdateAtDegrees(gaze, 0));
        }

        [Fact]
        public void NonPositiveConeDegrees_FallsBackToDefault()
        {
            var gaze = new GazeConeLock(coneDegrees: 0f);
            UpdateAtDegrees(gaze, 0);
            Assert.True(UpdateAtDegrees(gaze, GazeConeLock.ConeDegrees - 0.1));
            Assert.False(UpdateAtDegrees(gaze, GazeConeLock.ConeDegrees + 5.0));
        }
    }
}
