using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ModManifestComparerTests
    {
        [Fact]
        public void TryCreateManifest_NormalizesGuidAndIgnoresOrderAndName()
        {
            Assert.True(ModManifestComparer.TryCreateManifest(new[]
            {
                new ModManifestEntry(" B.Mod ", "B", "2", "sha256:b"),
                new ModManifestEntry("A.MOD", "A", "1", "sha256:a"),
            }, out ModManifest first, out _));
            Assert.True(ModManifestComparer.TryCreateManifest(new[]
            {
                new ModManifestEntry("a.mod", "renamed", "1", "sha256:a"),
                new ModManifestEntry("b.mod", "other", "2", "sha256:b"),
            }, out ModManifest second, out _));

            Assert.Equal(first.Fingerprint, second.Fingerprint);
            Assert.Equal("a.mod", first.Entries[0].Guid);
            Assert.Equal("b.mod", first.Entries[1].Guid);
        }

        [Fact]
        public void TryCreateManifest_RejectsEmptyAndDuplicateGuid()
        {
            Assert.False(ModManifestComparer.TryCreateManifest(new[]
            {
                new ModManifestEntry(" ", "A", "1", "x"),
            }, out _, out _));
            Assert.False(ModManifestComparer.TryCreateManifest(new[]
            {
                new ModManifestEntry("A", "A", "1", "x"),
                new ModManifestEntry("a", "A2", "1", "x"),
            }, out _, out _));
        }

        [Fact]
        public void Fingerprint_IsCanonicalLowerHex()
        {
            ModManifest manifest = Manifest(new ModManifestEntry("a", "A", "1", "content"));
            Assert.True(ModManifestComparer.IsCanonicalFingerprint(manifest.Fingerprint));
            Assert.False(ModManifestComparer.IsCanonicalFingerprint(manifest.Fingerprint.ToUpperInvariant()));
            Assert.False(ModManifestComparer.IsCanonicalFingerprint("abc"));
        }

        [Fact]
        public void Compare_ClassifiesFourKindsIndependently()
        {
            ModManifest baseline = Manifest(
                new ModManifestEntry("missing", "Missing", "1", "m"),
                new ModManifestEntry("changed", "Changed", "1", "old"));
            ModManifest participant = Manifest(
                new ModManifestEntry("changed", "Changed local", "2", "new"),
                new ModManifestEntry("extra", "Extra", "1", "e"));

            ModComparisonResult result = ModManifestComparer.Compare(baseline, participant);

            Assert.False(result.IsMatch);
            Assert.Equal(new ModDifferenceSummary(1, 1, 1, 1), result.Summary);
            Assert.Equal(4, result.Differences.Count);
        }

        private static ModManifest Manifest(params ModManifestEntry[] entries)
        {
            Assert.True(ModManifestComparer.TryCreateManifest(entries, out ModManifest manifest, out string error), error);
            return manifest;
        }
    }
}
