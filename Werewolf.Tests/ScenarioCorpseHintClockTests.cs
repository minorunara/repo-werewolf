using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioCorpseHintClockTests
    {
        private const long Delay = 1000L;

        [Fact]
        public void FiresOnceAfterTheDelayFromTheFirstStart()
        {
            var clock = new ScenarioCorpseHintClock();

            Assert.False(clock.Tick(100, started: false, ended: false, Delay));
            Assert.False(clock.Tick(200, started: true, ended: false, Delay));
            Assert.False(clock.MarkerShown);
            Assert.False(clock.Tick(200 + Delay - 1, started: true, ended: false, Delay));

            Assert.True(clock.Tick(200 + Delay, started: true, ended: false, Delay));
            Assert.True(clock.MarkerShown);

            Assert.False(clock.Tick(200 + Delay + 1, started: true, ended: false, Delay));
            Assert.True(clock.MarkerShown);
        }

        [Fact]
        public void KeepsCountingFromTheFirstStartEvenIfTheStartDrops()
        {
            var clock = new ScenarioCorpseHintClock();
            clock.Tick(0, started: true, ended: false, Delay);
            clock.Tick(500, started: false, ended: false, Delay);
            clock.Tick(900, started: true, ended: false, Delay);

            Assert.True(clock.Tick(Delay, started: true, ended: false, Delay));
        }

        [Fact]
        public void EndingBeforeTheDelayCancelsTheHintForGood()
        {
            var clock = new ScenarioCorpseHintClock();
            clock.Tick(0, started: true, ended: false, Delay);
            Assert.False(clock.Tick(500, started: true, ended: true, Delay));
            Assert.True(clock.Done);

            Assert.False(clock.Tick(Delay * 10, started: true, ended: false, Delay));
            Assert.False(clock.MarkerShown);
        }

        [Fact]
        public void EndingAfterFiringHidesTheMarker()
        {
            var clock = new ScenarioCorpseHintClock();
            clock.Tick(0, started: true, ended: false, Delay);
            Assert.True(clock.Tick(Delay, started: true, ended: false, Delay));
            Assert.True(clock.MarkerShown);

            Assert.False(clock.Tick(Delay + 1, started: true, ended: true, Delay));
            Assert.False(clock.MarkerShown);
            Assert.True(clock.Done);
        }

        [Fact]
        public void StartAndEndOnTheSameFrameEndsWithoutFiring()
        {
            var clock = new ScenarioCorpseHintClock();
            Assert.False(clock.Tick(0, started: true, ended: true, 0L));
            Assert.True(clock.Done);
            Assert.False(clock.MarkerShown);
        }

        [Fact]
        public void NeverFiresWithoutTheStart()
        {
            var clock = new ScenarioCorpseHintClock();
            Assert.False(clock.Tick(0, started: false, ended: false, Delay));
            Assert.False(clock.Tick(Delay * 10, started: false, ended: false, Delay));
            Assert.False(clock.MarkerShown);
            Assert.False(clock.Done);
        }
    }
}
