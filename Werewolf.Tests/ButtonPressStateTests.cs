using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ButtonPressStateTests
    {
        private const float Step = ButtonPressState.ReleaseSeconds / 4f;

        [Fact]
        public void Sync_ReportsDownEdgeOnce()
        {
            var s = new ButtonPressState();
            Assert.Equal(ButtonPressState.Phase.Up, s.Current);
            Assert.True(s.Sync(true));
            Assert.Equal(ButtonPressState.Phase.Down, s.Current);
            Assert.False(s.Sync(true));
            Assert.Equal(ButtonPressState.Phase.Down, s.Current);
        }

        [Fact]
        public void Sync_WithoutCharging_StaysUp()
        {
            var s = new ButtonPressState();
            Assert.False(s.Sync(false));
            Assert.Equal(ButtonPressState.Phase.Up, s.Current);
            Assert.False(s.Tick(Step));
        }

        [Fact]
        public void Tick_DoesNotAdvanceWhileDown()
        {
            var s = new ButtonPressState();
            s.Sync(true);
            for (int i = 0; i < 10; i++) Assert.False(s.Tick(Step));
            Assert.Equal(ButtonPressState.Phase.Down, s.Current);
            Assert.Equal(0f, s.ReleaseEval);
        }

        [Fact]
        public void Release_ReturnsToUpAfterReleaseSeconds()
        {
            var s = new ButtonPressState();
            s.Sync(true);
            Assert.False(s.Sync(false));
            Assert.Equal(ButtonPressState.Phase.Releasing, s.Current);

            for (int i = 0; i < 3; i++)
            {
                Assert.True(s.Tick(Step));
                Assert.Equal(ButtonPressState.Phase.Releasing, s.Current);
                Assert.True(s.ReleaseEval < 1f);
            }
            Assert.True(s.Tick(Step));
            Assert.Equal(ButtonPressState.Phase.Up, s.Current);
            Assert.Equal(1f, s.ReleaseEval);
            Assert.False(s.Tick(Step));
        }

        [Fact]
        public void Sync_DuringRelease_GoesBackDown()
        {
            var s = new ButtonPressState();
            s.Sync(true);
            s.Sync(false);
            s.Tick(Step);
            Assert.True(s.Sync(true));
            Assert.Equal(ButtonPressState.Phase.Down, s.Current);
            Assert.Equal(0f, s.ReleaseEval);
        }

        [Fact]
        public void Reset_ReturnsToUp()
        {
            var s = new ButtonPressState();
            s.Sync(true);
            s.Reset();
            Assert.Equal(ButtonPressState.Phase.Up, s.Current);
            Assert.False(s.Sync(false));
        }
    }
}
