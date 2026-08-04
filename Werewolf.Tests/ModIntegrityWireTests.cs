using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class ModIntegrityWireTests
    {
        [Fact]
        public void ManifestRequest_RoundTrips()
        {
            ModManifest baseline = Manifest(1);
            object[] payload = ModIntegrityWire.BuildManifestRequest(7, baseline.Fingerprint);

            Assert.True(ModIntegrityWire.TryReadManifestRequest(payload, out ModManifestRequestMessage request, out _));
            Assert.Equal(7, request.Epoch);
            Assert.Equal(ModIntegrityWire.ProtocolVersion, request.ProtocolVersion);
            Assert.Equal(baseline.Fingerprint, request.BaselineFingerprint);
        }

        [Fact]
        public void MatchingManifest_UsesFingerprintOnlyReport()
        {
            ModManifest manifest = Manifest(2);
            IReadOnlyList<object[]> payloads = ModIntegrityWire.BuildManifestReport(1, manifest.Fingerprint, manifest);

            Assert.Single(payloads);
            Assert.True(ModIntegrityWire.TryReadManifestReport(payloads[0], out ModManifestReportChunk report, out _));
            Assert.True(report.IsFingerprintOnly);
            Assert.Empty(report.Entries);
        }

        [Fact]
        public void MismatchingManifest_ChunksAndAssemblesOutOfOrder()
        {
            ModManifest baseline = Manifest(1);
            ModManifest participant = Manifest(33);
            IReadOnlyList<object[]> payloads = ModIntegrityWire.BuildManifestReport(5, baseline.Fingerprint, participant);
            Assert.Equal(2, payloads.Count);
            Assert.True(ModIntegrityWire.TryReadManifestReport(payloads[1], out ModManifestReportChunk second, out _));
            Assert.True(ModIntegrityWire.TryReadManifestReport(payloads[0], out ModManifestReportChunk first, out _));

            var assembler = new ModManifestChunkAssembler(second);
            Assert.Equal(ModManifestChunkAddOutcome.Accepted, assembler.Add(second));
            Assert.Equal(ModManifestChunkAddOutcome.Duplicate, assembler.Add(second));
            Assert.Equal(ModManifestChunkAddOutcome.Completed, assembler.Add(first));
            Assert.Equal(participant.Fingerprint, assembler.Manifest.Fingerprint);
            Assert.Equal(33, assembler.Manifest.Entries.Count);
        }

        [Fact]
        public void ManifestReport_RejectsBadParallelArraysAndFingerprint()
        {
            ModManifest manifest = Manifest(1);
            object[] badArrays =
            {
                1, ModIntegrityWire.ProtocolVersion, 0, 1, manifest.Fingerprint,
                new[] { "a" }, Array.Empty<string>(), new[] { "1" }, new[] { "x" },
            };
            Assert.False(ModIntegrityWire.TryReadManifestReport(badArrays, out _, out _));

            object[] badFingerprint =
            {
                1, ModIntegrityWire.ProtocolVersion, 0, 0, manifest.Fingerprint.ToUpperInvariant(),
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
            };
            Assert.False(ModIntegrityWire.TryReadManifestReport(badFingerprint, out _, out _));
        }

        [Fact]
        public void Snapshot_RoundTripsHundredActors()
        {
            var records = new List<ModParticipantRecord>
            {
                new ModParticipantRecord(1, ModIntegrityStatus.Baseline, ModUnavailableReason.None, default),
            };
            for (int actor = 2; actor <= 100; actor++)
                records.Add(new ModParticipantRecord(actor, ModIntegrityStatus.Match, ModUnavailableReason.None, default));
            Assert.True(ModIntegritySnapshot.TryCreate(2, 9, 1, records, out ModIntegritySnapshot source, out _));

            Assert.True(ModIntegrityWire.TryReadSnapshot(ModIntegrityWire.BuildSnapshot(source),
                out ModIntegritySnapshot decoded, out string error), error);
            Assert.Equal(100, decoded.Records.Count);
            Assert.Equal(9, decoded.Revision);
        }

        [Fact]
        public void DetailResponse_UsesAtMostThirtyTwoBoundedChunks()
        {
            var differences = new List<ModDifference>();
            for (int i = 0; i < ModIntegrityWire.MaxDifferences; i++)
                differences.Add(new ModDifference(ModDifferenceKind.Missing, $"g{i}", $"n{i}", "1", ""));

            IReadOnlyList<object[]> payloads = ModIntegrityWire.BuildDetailResponse(1, 2, 3, differences);
            Assert.Equal(ModIntegrityWire.MaxDetailChunks, payloads.Count);
            int total = 0;
            for (int i = payloads.Count - 1; i >= 0; i--)
            {
                Assert.True(ModIntegrityWire.TryReadDetailResponse(payloads[i], out ModIntegrityDetailChunk chunk, out _));
                total += chunk.Differences.Count;
            }
            Assert.Equal(ModIntegrityWire.MaxDifferences, total);
        }

        [Fact]
        public void BuildRejectsManifestAndDetailOverMaximum()
        {
            ModManifest baseline = Manifest(1);
            ModManifest tooMany = Manifest(ModIntegrityWire.MaxPlugins + 1);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ModIntegrityWire.BuildManifestReport(1, baseline.Fingerprint, tooMany));

            var differences = new List<ModDifference>();
            for (int i = 0; i <= ModIntegrityWire.MaxDifferences; i++)
                differences.Add(new ModDifference(ModDifferenceKind.Extra, $"g{i}", "n", "", "1"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ModIntegrityWire.BuildDetailResponse(1, 1, 2, differences));
        }

        private static ModManifest Manifest(int count)
        {
            var entries = new List<ModManifestEntry>();
            for (int i = 0; i < count; i++)
                entries.Add(new ModManifestEntry($"plugin.{i:000}", $"Plugin {i}", "1.0", $"sha256:{i:000}"));
            Assert.True(ModManifestComparer.TryCreateManifest(entries, out ModManifest manifest, out string error), error);
            return manifest;
        }
    }
}
