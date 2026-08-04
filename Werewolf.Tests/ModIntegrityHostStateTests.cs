using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ModIntegrityHostStateTests
    {
        [Fact]
        public void BeginEpoch_CreatesBaselineAndPendingRoster()
        {
            var state = Begin();
            ModIntegritySnapshot snapshot = state.BuildSnapshot();

            Assert.Equal(10, snapshot.BaselineActor);
            Assert.Equal(ModIntegrityStatus.Baseline, Record(snapshot, 10).Status);
            Assert.Equal(ModIntegrityStatus.Pending, Record(snapshot, 20).Status);
            Assert.Equal(1, snapshot.Revision);
        }

        [Fact]
        public void FingerprintReport_OnlyCurrentBaselineBecomesMatch()
        {
            var state = Begin();
            int revision = state.Revision;
            string other = Manifest(new ModManifestEntry("other", "Other", "1", "x")).Fingerprint;

            Assert.Equal(FingerprintReportOutcome.RetryCurrentRequest,
                state.ApplyFingerprintReport(20, other, 100));
            Assert.Equal(revision, state.Revision);
            Assert.Equal(ModIntegrityStatus.Pending, Record(state.BuildSnapshot(), 20).Status);

            Assert.Equal(FingerprintReportOutcome.Matched,
                state.ApplyFingerprintReport(20, state.Baseline.Fingerprint, 200));
            Assert.Equal(revision + 1, state.Revision);
            Assert.Equal(ModIntegrityStatus.Match, Record(state.BuildSnapshot(), 20).Status);
        }

        [Fact]
        public void DuplicateSameReport_IsNoOp_ConflictingSecondReportIsLocalized()
        {
            var state = Begin();
            state.ApplyFingerprintReport(20, state.Baseline.Fingerprint, 100);
            int revision = state.Revision;

            state.ApplyFingerprintReport(20, state.Baseline.Fingerprint, 200);
            Assert.Equal(revision, state.Revision);

            state.ApplyManifest(20, Manifest(new ModManifestEntry("different", "D", "1", "x")), 300);
            Assert.Equal(revision + 1, state.Revision);
            Assert.Equal(ModIntegrityStatus.Unavailable, Record(state.BuildSnapshot(), 20).Status);
            Assert.Equal(ModUnavailableReason.InvalidPayload, Record(state.BuildSnapshot(), 20).UnavailableReason);
            Assert.Equal(ModIntegrityStatus.Pending, Record(state.BuildSnapshot(), 30).Status);
        }

        [Fact]
        public void Timeout_AllowsLateValidRecovery()
        {
            var state = Begin();
            Assert.True(state.TickTimeouts(5000));
            Assert.Equal(ModUnavailableReason.NoResponse, Record(state.BuildSnapshot(), 20).UnavailableReason);

            state.ApplyFingerprintReport(20, state.Baseline.Fingerprint, 6000);
            Assert.Equal(ModIntegrityStatus.Match, Record(state.BuildSnapshot(), 20).Status);
        }

        [Fact]
        public void ManifestComparison_PublishesSummaryAndDetail()
        {
            var state = Begin();
            ModManifest participant = Manifest(
                new ModManifestEntry("base", "Base", "2", "changed"),
                new ModManifestEntry("extra", "Extra", "1", "x"));

            state.ApplyManifest(20, participant, 100);

            ModParticipantRecord record = Record(state.BuildSnapshot(), 20);
            Assert.Equal(ModIntegrityStatus.Difference, record.Status);
            Assert.Equal(new ModDifferenceSummary(0, 1, 1, 1), record.Summary);
            Assert.Equal(3, state.GetDifferences(20).Count);
        }

        [Fact]
        public void SyncRoster_AddsPendingAndRemovesDeparted()
        {
            var state = Begin();
            state.SyncRoster(new[] { 10, 20, 40 }, 100);
            ModIntegritySnapshot snapshot = state.BuildSnapshot();

            Assert.False(snapshot.TryGetRecord(30, out _));
            Assert.Equal(ModIntegrityStatus.Pending, Record(snapshot, 40).Status);
        }

        [Fact]
        public void SnapshotPublish_IsCoalesced()
        {
            var state = Begin();
            Assert.False(state.ShouldPublishSnapshot(249, false));
            Assert.True(state.ShouldPublishSnapshot(250, false));
            state.MarkSnapshotPublished(250);
            Assert.False(state.ShouldPublishSnapshot(1000, false));
        }

        private static ModIntegrityHostState Begin()
        {
            var state = new ModIntegrityHostState();
            state.BeginEpoch(1, 10, new[] { 10, 20, 30 },
                Manifest(new ModManifestEntry("base", "Base", "1", "content")), 0);
            return state;
        }

        private static ModParticipantRecord Record(ModIntegritySnapshot snapshot, int actor)
        {
            Assert.True(snapshot.TryGetRecord(actor, out ModParticipantRecord record));
            return record;
        }

        private static ModManifest Manifest(params ModManifestEntry[] entries)
        {
            Assert.True(ModManifestComparer.TryCreateManifest(entries, out ModManifest manifest, out string error), error);
            return manifest;
        }
    }
}
