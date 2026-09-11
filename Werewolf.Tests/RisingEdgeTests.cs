using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class RisingEdgeTests
    {
        [Fact]
        public void Observe_FirstTrue_IsRisingEdge()
        {
            var edge = new RisingEdge();

            Assert.True(edge.Observe(true));
        }

        [Fact]
        public void Observe_HeldTrue_FiresOnlyOnce()
        {
            var edge = new RisingEdge();
            edge.Observe(true);

            Assert.False(edge.Observe(true));
            Assert.False(edge.Observe(true));
        }

        [Fact]
        public void Observe_FallThenRise_FiresAgain()
        {
            var edge = new RisingEdge();
            edge.Observe(true);

            Assert.False(edge.Observe(false));
            Assert.False(edge.Observe(false));
            Assert.True(edge.Observe(true));
        }

        [Fact]
        public void Observe_FalseOnly_NeverFires()
        {
            var edge = new RisingEdge();

            Assert.False(edge.Observe(false));
            Assert.False(edge.Observe(false));
        }
    }
}
