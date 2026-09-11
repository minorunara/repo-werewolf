using System;
using Werewolf.Core;
using Werewolf.Debugging;
using Xunit;

namespace Werewolf.Tests
{
    public class RepeatedLogLimiterTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void FirstOccurrence_IsAlwaysEmitted()
        {
            var limiter = new RepeatedLogLimiter();

            var decision = limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0);

            Assert.True(decision.EmitCurrent);
            Assert.False(decision.HasSummary);
        }

        [Fact]
        public void RepeatsInsideWindow_AreSuppressed()
        {
            var limiter = new RepeatedLogLimiter(TimeSpan.FromSeconds(10));
            limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0);

            var second = limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0.AddSeconds(1));
            var third = limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0.AddSeconds(9));

            Assert.False(second.EmitCurrent);
            Assert.False(third.EmitCurrent);
        }

        [Fact]
        public void FirstOccurrenceAfterWindow_EmitsSummaryThenCurrent()
        {
            var limiter = new RepeatedLogLimiter(TimeSpan.FromSeconds(10));
            limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0);
            limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0.AddSeconds(1));
            limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0.AddSeconds(2));

            var decision = limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0.AddSeconds(10));

            Assert.True(decision.EmitCurrent);
            Assert.True(decision.HasSummary);
            Assert.Equal(2, decision.Summary.SuppressedCount);
            Assert.Equal("tick_error", decision.Summary.Kind);
            Assert.Equal(WLogLevel.Error, decision.Summary.Level);
            Assert.False(decision.Summary.Secret);
        }

        [Fact]
        public void DifferentKindsLevelsAndSecrecy_AreIndependent()
        {
            var limiter = new RepeatedLogLimiter();
            limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0);

            Assert.True(limiter.Observe("[WW] ui_tick_error err=boom", WLogLevel.Error, false, T0).EmitCurrent);
            Assert.True(limiter.Observe("[WW] tick_error err=boom", WLogLevel.Warning, false, T0).EmitCurrent);
            Assert.True(limiter.Observe("[WW] tick_error err=boom secret=1", WLogLevel.Error, true, T0).EmitCurrent);
        }

        [Fact]
        public void SameKindWithDifferentDetails_IsNotSuppressed()
        {
            var limiter = new RepeatedLogLimiter();
            limiter.Observe("[WW] tick_error err=first", WLogLevel.Error, false, T0);

            var different = limiter.Observe(
                "[WW] tick_error err=second", WLogLevel.Error, false, T0.AddSeconds(1));

            Assert.True(different.EmitCurrent);
        }

        [Fact]
        public void DrainAndReset_ReturnsPendingSummaryAndRearmsFirstOccurrence()
        {
            var limiter = new RepeatedLogLimiter();
            limiter.Observe("[WW] tick_error err=boom secret=1", WLogLevel.Error, true, T0);
            limiter.Observe("[WW] tick_error err=boom secret=1", WLogLevel.Error, true, T0.AddSeconds(1));
            limiter.Observe("[WW] tick_error err=boom secret=1", WLogLevel.Error, true, T0.AddSeconds(2));

            var summary = Assert.Single(limiter.DrainAndReset());
            var afterReset = limiter.Observe(
                "[WW] tick_error err=boom secret=1", WLogLevel.Error, true, T0.AddSeconds(3));

            Assert.Equal(2, summary.SuppressedCount);
            Assert.True(summary.Secret);
            Assert.Contains("kind=tick_error count=2 secret=1", summary.ToLine());
            Assert.True(afterReset.EmitCurrent);
            Assert.Empty(limiter.DrainAndReset());
        }

        [Fact]
        public void ClockMovingBackwards_StartsNewWindowWithoutLosingSummary()
        {
            var limiter = new RepeatedLogLimiter();
            limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0);
            limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0.AddSeconds(1));

            var decision = limiter.Observe(
                "[WW] tick_error err=boom", WLogLevel.Error, false, T0.AddMinutes(-1));

            Assert.True(decision.EmitCurrent);
            Assert.Equal(1, decision.Summary.SuppressedCount);
        }

        [Fact]
        public void NonPositiveWindow_UsesDefaultWindow()
        {
            var limiter = new RepeatedLogLimiter(TimeSpan.Zero);
            limiter.Observe("[WW] tick_error err=boom", WLogLevel.Error, false, T0);

            Assert.False(limiter.Observe(
                "[WW] tick_error err=boom", WLogLevel.Error, false, T0.AddSeconds(1)).EmitCurrent);
        }
    }
}
