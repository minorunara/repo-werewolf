using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioSelectionTests
    {
        [Fact]
        public void ConsumeOpenRequest_TakesTheRequestOnce()
        {
            ScenarioSelection.Clear();
            Assert.False(ScenarioSelection.OpenRequested);
            Assert.False(ScenarioSelection.ConsumeOpenRequest());

            ScenarioSelection.RequestOpen();
            Assert.True(ScenarioSelection.OpenRequested);

            Assert.True(ScenarioSelection.ConsumeOpenRequest());
            Assert.False(ScenarioSelection.OpenRequested);
            Assert.False(ScenarioSelection.ConsumeOpenRequest());
        }

        [Fact]
        public void RequestOpen_IsIdempotent()
        {
            ScenarioSelection.Clear();
            ScenarioSelection.RequestOpen();
            ScenarioSelection.RequestOpen();

            Assert.True(ScenarioSelection.ConsumeOpenRequest());
            Assert.False(ScenarioSelection.ConsumeOpenRequest());
        }

        [Fact]
        public void Clear_DropsPendingRequest()
        {
            ScenarioSelection.RequestOpen();
            ScenarioSelection.Clear();
            Assert.False(ScenarioSelection.ConsumeOpenRequest());
        }
    }
}
