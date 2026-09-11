using Werewolf.Core;
using Werewolf.Debugging;
using Xunit;

namespace Werewolf.Tests
{
    public class StructuredLogPolicyTests
    {
        [Theory]
        [InlineData(WLogLevel.Debug, false, StructuredLogRoute.Drop)]
        [InlineData(WLogLevel.Debug, true, StructuredLogRoute.Drop)]
        [InlineData(WLogLevel.Warning, false, StructuredLogRoute.Immediate)]
        [InlineData(WLogLevel.Error, false, StructuredLogRoute.Immediate)]
        [InlineData(WLogLevel.Warning, true, StructuredLogRoute.Deferred)]
        [InlineData(WLogLevel.Error, true, StructuredLogRoute.Deferred)]
        public void NormalMode_DropsNormalTraceButKeepsFailures(
            WLogLevel level, bool secret, StructuredLogRoute expected)
        {
            Assert.Equal(expected,
                StructuredLogPolicy.Decide(level, secret, debugLogging: false, debugMode: false));
        }

        [Theory]
        [InlineData(false, StructuredLogRoute.Immediate)]
        [InlineData(true, StructuredLogRoute.Deferred)]
        public void DebugLogging_EmitsPublicTraceAndDefersSecretTrace(
            bool secret, StructuredLogRoute expected)
        {
            Assert.Equal(expected,
                StructuredLogPolicy.Decide(
                    WLogLevel.Debug, secret, debugLogging: true, debugMode: false));
        }

        [Theory]
        [InlineData(WLogLevel.Debug, false)]
        [InlineData(WLogLevel.Debug, true)]
        [InlineData(WLogLevel.Warning, false)]
        [InlineData(WLogLevel.Warning, true)]
        [InlineData(WLogLevel.Error, false)]
        [InlineData(WLogLevel.Error, true)]
        public void DebugMode_EmitsEverythingImmediately(WLogLevel level, bool secret)
        {
            Assert.Equal(StructuredLogRoute.Immediate,
                StructuredLogPolicy.Decide(level, secret, debugLogging: false, debugMode: true));
        }
    }
}
