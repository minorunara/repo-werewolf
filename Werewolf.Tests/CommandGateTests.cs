using Werewolf.Debugging;
using Xunit;

namespace Werewolf.Tests
{
    public class CommandGateTests
    {

        [Theory]
        [InlineData("/ww start", "start", new string[0])]
        [InlineData("/ww bot 4", "bot", new[] { "4" })]
        [InlineData("/ww kill -2 vote", "kill", new[] { "-2", "vote" })]
        [InlineData("  /ww status  ", "status", new string[0])]
        [InlineData("/WW START", "start", new string[0])]
        public void TryParse_WwCommand_ParsesCommandAndArgs(string message, string command, string[] args)
        {
            bool ok = CommandGate.TryParse(message, out string cmd, out string[] parsed);

            Assert.True(ok);
            Assert.Equal(command, cmd);
            Assert.Equal(args, parsed);
        }

        [Fact]
        public void TryParse_WwAlone_ReturnsEmptyCommand()
        {
            bool ok = CommandGate.TryParse("/ww", out string cmd, out string[] args);

            Assert.True(ok);
            Assert.Equal("", cmd);
            Assert.Empty(args);
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("/w start")]
        [InlineData("/wwstart")]
        [InlineData("say /ww start")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParse_NonWwMessage_ReturnsFalse(string message)
        {
            bool ok = CommandGate.TryParse(message, out _, out _);

            Assert.False(ok);
        }

        [Theory]
        [InlineData("start")]
        [InlineData("bot")]
        [InlineData("role")]
        [InlineData("skiptimer")]
        [InlineData("reveal")]
        [InlineData("kill")]
        [InlineData("phase")]
        [InlineData("status")]
        [InlineData("selftest")]
        [InlineData("unknowncommand")]
        public void Decide_CheatCommand_AllowedOnlyForHostWithDebugMode(string command)
        {
            Assert.Equal(CommandGateVerdict.Allowed,
                CommandGate.Decide(command, isHost: true, debugMode: true));

            Assert.Equal(CommandGateVerdict.RejectedDebugModeDisabled,
                CommandGate.Decide(command, isHost: true, debugMode: false));

            Assert.Equal(CommandGateVerdict.RejectedNotHost,
                CommandGate.Decide(command, isHost: false, debugMode: true));

            Assert.NotEqual(CommandGateVerdict.Allowed,
                CommandGate.Decide(command, isHost: false, debugMode: false));
        }
    }
}
