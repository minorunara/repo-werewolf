using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ModIntegrityClientStateTests
    {
        [Fact]
        public void Snapshot_RejectsWrongMasterAndStaleRevision()
        {
            var state = new ModIntegrityClientState();
            Assert.False(state.TryApplySnapshot(Snapshot(1, 1), 99));
            Assert.True(state.TryApplySnapshot(Snapshot(1, 8), 10));
            Assert.False(state.TryApplySnapshot(Snapshot(1, 7), 10));
            Assert.False(state.TryApplySnapshot(Snapshot(1, 8), 10));
            Assert.True(state.TryApplySnapshot(Snapshot(1, 9), 10));
            Assert.Equal(9, state.Current.Revision);
        }

        [Fact]
        public void NewEpoch_ClearsDetailCache()
        {
            var state = new ModIntegrityClientState();
            state.TryApplySnapshot(Snapshot(1, 1), 10);
            Assert.True(state.BeginDetailRequest(20, 0));
            Assert.True(state.TryApplyDetailChunk(Chunk(1, 1, 20, 0, 1, "a"), out bool completed));
            Assert.True(completed);
            Assert.True(state.TryGetDetail(20, out _));

            Assert.True(state.TryApplySnapshot(Snapshot(2, 1), 10));
            Assert.False(state.TryGetDetail(20, out _));
        }

        [Fact]
        public void DetailChunks_AssembleOutOfOrderAndDuplicateAtomically()
        {
            var state = new ModIntegrityClientState();
            state.TryApplySnapshot(Snapshot(1, 3), 10);
            Assert.True(state.BeginDetailRequest(20, 100));

            ModIntegrityDetailChunk second = Chunk(1, 3, 20, 1, 2, "b");
            ModIntegrityDetailChunk first = Chunk(1, 3, 20, 0, 2, "a");
            Assert.True(state.TryApplyDetailChunk(second, out bool done));
            Assert.False(done);
            Assert.True(state.TryApplyDetailChunk(second, out done));
            Assert.False(done);
            Assert.False(state.TryGetDetail(20, out _));
            Assert.True(state.TryApplyDetailChunk(first, out done));
            Assert.True(done);
            Assert.True(state.TryGetDetail(20, out ModIntegrityDetail detail));
            Assert.Equal("a", detail.Differences[0].Guid);
            Assert.Equal("b", detail.Differences[1].Guid);
        }

        [Fact]
        public void ConflictingDuplicate_DiscardsOnlyPartialAssembly()
        {
            var state = new ModIntegrityClientState();
            state.TryApplySnapshot(Snapshot(1, 3), 10);
            state.BeginDetailRequest(20, 0);
            state.TryApplyDetailChunk(Chunk(1, 3, 20, 0, 2, "a"), out _);

            Assert.False(state.TryApplyDetailChunk(Chunk(1, 3, 20, 0, 2, "other"), out _));
            Assert.Equal(-1, state.PendingDetailActor);
            Assert.NotNull(state.Current);
            Assert.False(state.TryGetDetail(20, out _));
        }

        [Fact]
        public void DetailTimeout_DiscardsPartialAndReturnsActor()
        {
            var state = new ModIntegrityClientState();
            state.TryApplySnapshot(Snapshot(1, 3), 10);
            state.BeginDetailRequest(20, 100);

            Assert.False(state.TickDetailTimeout(3099, out _));
            Assert.True(state.TickDetailTimeout(3100, out int actor));
            Assert.Equal(20, actor);
            Assert.NotNull(state.Current);
        }

        private static ModIntegritySnapshot Snapshot(int epoch, int revision)
        {
            var records = new[]
            {
                new ModParticipantRecord(10, ModIntegrityStatus.Baseline, ModUnavailableReason.None, default),
                new ModParticipantRecord(20, ModIntegrityStatus.Difference, ModUnavailableReason.None,
                    new ModDifferenceSummary(1, 0, 0, 0)),
            };
            Assert.True(ModIntegritySnapshot.TryCreate(epoch, revision, 10, records,
                out ModIntegritySnapshot snapshot, out string error), error);
            return snapshot;
        }

        private static ModIntegrityDetailChunk Chunk(
            int epoch, int revision, int actor, int index, int count, string guid)
        {
            return new ModIntegrityDetailChunk(epoch, revision, actor, index, count,
                new[] { new ModDifference(ModDifferenceKind.Missing, guid, guid, "1", "") });
        }
    }
}
