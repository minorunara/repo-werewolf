using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class HueSpreadTests
    {
        [Fact]
        public void Order_StartsNearestToTheStartHueThenTakesTheFarthestEachTime()
        {
            int[] order = HueSpread.Order(new[] { 270f, 0f, 90f, 180f });
            Assert.Equal(new[] { 1, 3, 0, 2 }, order);
        }

        [Fact]
        public void Order_IgnoresThePaletteOrderAndAvoidsNeighbours()
        {
            int[] order = HueSpread.Order(new[] { 10f, 20f, 30f, 200f });
            Assert.Equal(0, order[0]);
            Assert.Equal(3, order[1]);
            Assert.Equal(2, order[2]);
        }

        [Fact]
        public void Order_UsesEveryCandidateExactlyOnce()
        {
            int[] order = HueSpread.Order(new[] { 5f, 355f, 120f, 240f, 60f, 300f, 180f });
            Assert.Equal(Enumerable.Range(0, 7), order.OrderBy(i => i));
        }

        [Fact]
        public void Order_IsEmptyWithoutCandidates()
        {
            Assert.Empty(HueSpread.Order(new float[0]));
            Assert.Empty(HueSpread.Order(null));
        }

        [Fact]
        public void Distance_WrapsAroundTheHueCircle()
        {
            Assert.Equal(20f, HueSpread.Distance(10f, 350f));
            Assert.Equal(180f, HueSpread.Distance(0f, 180f));
            Assert.Equal(0f, HueSpread.Distance(-90f, 270f));
        }
    }
}
