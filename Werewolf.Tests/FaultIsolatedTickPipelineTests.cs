using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public sealed class FaultIsolatedTickPipelineTests : IDisposable
    {
        private readonly List<string> _logs = new List<string>();

        public FaultIsolatedTickPipelineTests()
        {
            WLog.Sink = (line, _) => _logs.Add(line);
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Fact]
        public void Tick_WhenStageThrows_ContinuesWithFollowingStagesInOrder()
        {
            var calls = new List<string>();
            var pipeline = new FaultIsolatedTickPipeline(
                new FaultIsolatedTickPipeline.Stage("first", now => calls.Add($"first:{now}")),
                new FaultIsolatedTickPipeline.Stage("broken", _ => throw new InvalidOperationException("boom")),
                new FaultIsolatedTickPipeline.Stage("last", now => calls.Add($"last:{now}")));

            pipeline.Tick(1234);

            Assert.Equal(new[] { "first:1234", "last:1234" }, calls);
            string error = Assert.Single(_logs);
            Assert.Contains("tick_error stage=broken", error);
            Assert.Contains("type=System.InvalidOperationException", error);
            Assert.Contains("err=boom", error);
            Assert.Contains("stack=", error);
        }

        [Fact]
        public void Constructor_RejectsMissingStageContracts()
        {
            Assert.Throws<ArgumentException>(() =>
                new FaultIsolatedTickPipeline.Stage("", _ => { }));
            Assert.Throws<ArgumentNullException>(() =>
                new FaultIsolatedTickPipeline.Stage("stage", null));
            Assert.Throws<ArgumentException>(() =>
                new FaultIsolatedTickPipeline(new FaultIsolatedTickPipeline.Stage[] { null }));
        }
    }
}
