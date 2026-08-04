using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Werewolf.Core
{
    public static class ModManifestComparer
    {
        public static bool TryCreateManifest(
            IEnumerable<ModManifestEntry> entries,
            out ModManifest manifest,
            out string error)
        {
            manifest = null;
            error = null;
            if (entries == null)
            {
                error = "entries";
                return false;
            }

            var canonical = new List<ModManifestEntry>();
            var guids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ModManifestEntry entry in entries)
            {
                if (entry == null)
                {
                    error = "null_entry";
                    return false;
                }

                string guid = (entry.Guid ?? string.Empty).Trim().ToLowerInvariant();
                if (guid.Length == 0)
                {
                    error = "empty_guid";
                    return false;
                }
                if (!guids.Add(guid))
                {
                    error = "duplicate_guid";
                    return false;
                }

                canonical.Add(new ModManifestEntry(
                    guid,
                    entry.Name ?? string.Empty,
                    entry.Version ?? string.Empty,
                    entry.ContentId ?? string.Empty));
            }

            canonical.Sort((a, b) => StringComparer.Ordinal.Compare(a.Guid, b.Guid));
            string fingerprint = ComputeFingerprint(canonical);
            manifest = new ModManifest(canonical, fingerprint);
            return true;
        }

        public static bool IsCanonicalFingerprint(string value)
        {
            if (value == null || value.Length != 64) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            }
            return true;
        }

        public static ModComparisonResult Compare(ModManifest baseline, ModManifest participant)
        {
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            if (participant == null) throw new ArgumentNullException(nameof(participant));

            var baselineByGuid = ToDictionary(baseline);
            var participantByGuid = ToDictionary(participant);
            var differences = new List<ModDifference>();

            for (int i = 0; i < baseline.Entries.Count; i++)
            {
                ModManifestEntry expected = baseline.Entries[i];
                if (!participantByGuid.TryGetValue(expected.Guid, out ModManifestEntry actual))
                {
                    differences.Add(new ModDifference(
                        ModDifferenceKind.Missing,
                        expected.Guid,
                        expected.Name,
                        expected.Version,
                        string.Empty));
                    continue;
                }

                if (!string.Equals(expected.Version, actual.Version, StringComparison.Ordinal))
                {
                    differences.Add(new ModDifference(
                        ModDifferenceKind.Version,
                        expected.Guid,
                        DisplayName(expected, actual),
                        expected.Version,
                        actual.Version));
                }

                if (!string.Equals(expected.ContentId, actual.ContentId, StringComparison.Ordinal))
                {
                    differences.Add(new ModDifference(
                        ModDifferenceKind.Content,
                        expected.Guid,
                        DisplayName(expected, actual),
                        expected.ContentId,
                        actual.ContentId));
                }
            }

            for (int i = 0; i < participant.Entries.Count; i++)
            {
                ModManifestEntry actual = participant.Entries[i];
                if (!baselineByGuid.ContainsKey(actual.Guid))
                {
                    differences.Add(new ModDifference(
                        ModDifferenceKind.Extra,
                        actual.Guid,
                        actual.Name,
                        string.Empty,
                        actual.Version));
                }
            }

            return new ModComparisonResult(differences);
        }

        private static Dictionary<string, ModManifestEntry> ToDictionary(ModManifest manifest)
        {
            var result = new Dictionary<string, ModManifestEntry>(StringComparer.Ordinal);
            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ModManifestEntry entry = manifest.Entries[i];
                result[entry.Guid] = entry;
            }
            return result;
        }

        private static string DisplayName(ModManifestEntry baseline, ModManifestEntry participant)
        {
            if (!string.IsNullOrEmpty(baseline.Name)) return baseline.Name;
            return participant.Name ?? string.Empty;
        }

        private static string ComputeFingerprint(IReadOnlyList<ModManifestEntry> entries)
        {
            var canonical = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                ModManifestEntry entry = entries[i];
                canonical.Append(entry.Guid).Append('\n')
                    .Append(entry.Version).Append('\n')
                    .Append(entry.ContentId).Append('\n');
            }

            byte[] bytes = Encoding.UTF8.GetBytes(canonical.ToString());
            byte[] hash;
            using (SHA256 sha = SHA256.Create()) hash = sha.ComputeHash(bytes);
            var hex = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) hex.Append(hash[i].ToString("x2"));
            return hex.ToString();
        }
    }
}
