using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Werewolf.Core;
using Werewolf.Game;
using Xunit;

namespace Werewolf.Tests
{
    public sealed class ModManifestCollectorTests
    {
        private static readonly IReadOnlyList<(string Guid, string Error)> NoContentIdErrors =
            Array.Empty<(string Guid, string Error)>();

        [Fact]
        public void InitialAndCollectingStates_ArePending_AndStartOnlyOnce()
        {
            var completion = new TaskCompletionSource<ModManifestCollectionOutcome>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            int captures = 0;
            int starts = 0;
            var collector = new ModManifestCollector(
                () =>
                {
                    captures++;
                    return Array.Empty<ModManifestPluginSnapshot>();
                },
                _ =>
                {
                    starts++;
                    return completion.Task;
                });

            Assert.True(collector.IsPending);
            Assert.False(collector.IsFailed);
            Assert.False(collector.TryGetResult(out _));

            collector.EnsureStarted();
            collector.EnsureStarted();

            Assert.True(collector.IsPending);
            Assert.False(collector.IsFailed);
            Assert.False(collector.TryGetResult(out _));
            Assert.Equal(1, captures);
            Assert.Equal(1, starts);
        }

        [Fact]
        public void CompletedSuccess_AtomicallyPublishesNonNullManifest_AndIsTerminal()
        {
            ModManifest manifest = Manifest();
            int captures = 0;
            int starts = 0;
            var collector = new ModManifestCollector(
                () =>
                {
                    captures++;
                    return Array.Empty<ModManifestPluginSnapshot>();
                },
                _ =>
                {
                    starts++;
                    return Task.FromResult(ModManifestCollectionOutcome.Succeeded(
                        manifest, NoContentIdErrors));
                });

            collector.EnsureStarted();
            Assert.True(collector.IsPending);
            Assert.False(collector.TryGetResult(out _));

            collector.EnsureStarted();

            Assert.False(collector.IsPending);
            Assert.False(collector.IsFailed);
            Assert.True(collector.TryGetResult(out ModManifest result));
            Assert.Same(manifest, result);

            collector.EnsureStarted();
            Assert.Equal(1, captures);
            Assert.Equal(1, starts);
            Assert.True(collector.TryGetResult(out result));
            Assert.Same(manifest, result);
        }

        [Fact]
        public void CompletedFailure_PublishesNoManifest_AndIsTerminal()
        {
            int starts = 0;
            var collector = new ModManifestCollector(
                () => Array.Empty<ModManifestPluginSnapshot>(),
                _ =>
                {
                    starts++;
                    return Task.FromResult(ModManifestCollectionOutcome.Failed(
                        "duplicate_guid", NoContentIdErrors));
                });

            collector.EnsureStarted();
            collector.EnsureStarted();

            Assert.False(collector.IsPending);
            Assert.True(collector.IsFailed);
            Assert.False(collector.TryGetResult(out _));

            collector.EnsureStarted();
            Assert.Equal(1, starts);
            Assert.True(collector.IsFailed);
        }

        [Fact]
        public void FaultedTask_BecomesFailedWithoutPublishingManifest()
        {
            var collector = new ModManifestCollector(
                () => Array.Empty<ModManifestPluginSnapshot>(),
                _ => Task.FromException<ModManifestCollectionOutcome>(new IOException("boom")));

            collector.EnsureStarted();
            collector.EnsureStarted();

            Assert.False(collector.IsPending);
            Assert.True(collector.IsFailed);
            Assert.False(collector.TryGetResult(out _));
        }

        [Fact]
        public void SnapshotFailure_BecomesFailedWithoutStartingWorker()
        {
            int starts = 0;
            var collector = new ModManifestCollector(
                () => throw new InvalidOperationException("snapshot"),
                _ =>
                {
                    starts++;
                    return Task.FromResult(ModManifestCollectionOutcome.Succeeded(
                        Manifest(), NoContentIdErrors));
                });

            collector.EnsureStarted();

            Assert.False(collector.IsPending);
            Assert.True(collector.IsFailed);
            Assert.False(collector.TryGetResult(out _));
            Assert.Equal(0, starts);
        }

        [Fact]
        public void Constructor_RejectsMissingSnapshotSource()
        {
            Assert.Throws<ArgumentNullException>(() => new ModManifestCollector(null));
        }

        private static ModManifest Manifest()
        {
            Assert.True(ModManifestComparer.TryCreateManifest(
                Array.Empty<ModManifestEntry>(), out ModManifest manifest, out string error), error);
            return manifest;
        }
    }
}
