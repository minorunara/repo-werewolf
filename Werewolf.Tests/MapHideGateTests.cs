using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MapHideGateTests
    {
        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void ShouldSuppress_MatrixOfAllInputs(bool roundActive, bool minimapHideEnabled, bool expected)
        {
            Assert.Equal(expected, MapHideGate.ShouldSuppress(roundActive, minimapHideEnabled));
        }
    }
}
