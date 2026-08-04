using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Net
{
    public sealed class ModManifestRequestMessage
    {
        public ModManifestRequestMessage(int epoch, byte protocolVersion, string baselineFingerprint)
        {
            Epoch = epoch;
            ProtocolVersion = protocolVersion;
            BaselineFingerprint = baselineFingerprint;
        }

        public int Epoch { get; }
        public byte ProtocolVersion { get; }
        public string BaselineFingerprint { get; }
    }

    public sealed class ModManifestReportChunk
    {
        public ModManifestReportChunk(
            int epoch,
            byte protocolVersion,
            int chunkIndex,
            int chunkCount,
            string manifestFingerprint,
            IReadOnlyList<ModManifestEntry> entries)
        {
            Epoch = epoch;
            ProtocolVersion = protocolVersion;
            ChunkIndex = chunkIndex;
            ChunkCount = chunkCount;
            ManifestFingerprint = manifestFingerprint;
            Entries = ModIntegrityCollections.Freeze(entries);
        }

        public int Epoch { get; }
        public byte ProtocolVersion { get; }
        public int ChunkIndex { get; }
        public int ChunkCount { get; }
        public string ManifestFingerprint { get; }
        public IReadOnlyList<ModManifestEntry> Entries { get; }
        public bool IsFingerprintOnly => ChunkCount == 0;
    }

    public sealed class ModIntegrityDetailRequestMessage
    {
        public ModIntegrityDetailRequestMessage(int epoch, int actor)
        {
            Epoch = epoch;
            Actor = actor;
        }

        public int Epoch { get; }
        public int Actor { get; }
    }

    public enum ModManifestChunkAddOutcome
    {
        Accepted,
        Duplicate,
        Completed,
        Conflict,
        Invalid,
    }

    public sealed class ModManifestChunkAssembler
    {
        private readonly ModManifestReportChunk[] _chunks;
        private int _received;

        public ModManifestChunkAssembler(ModManifestReportChunk first)
        {
            if (first == null || first.IsFingerprintOnly) throw new ArgumentException(nameof(first));
            Epoch = first.Epoch;
            ManifestFingerprint = first.ManifestFingerprint;
            _chunks = new ModManifestReportChunk[first.ChunkCount];
        }

        public int Epoch { get; }
        public string ManifestFingerprint { get; }
        public ModManifest Manifest { get; private set; }

        public ModManifestChunkAddOutcome Add(ModManifestReportChunk chunk)
        {
            if (chunk == null || chunk.IsFingerprintOnly ||
                chunk.Epoch != Epoch || chunk.ChunkCount != _chunks.Length ||
                !string.Equals(chunk.ManifestFingerprint, ManifestFingerprint, StringComparison.Ordinal))
                return ModManifestChunkAddOutcome.Invalid;

            ModManifestReportChunk existing = _chunks[chunk.ChunkIndex];
            if (existing != null)
            {
                return SameEntries(existing.Entries, chunk.Entries)
                    ? ModManifestChunkAddOutcome.Duplicate
                    : ModManifestChunkAddOutcome.Conflict;
            }

            _chunks[chunk.ChunkIndex] = chunk;
            _received++;
            if (_received != _chunks.Length) return ModManifestChunkAddOutcome.Accepted;

            var entries = new List<ModManifestEntry>();
            for (int i = 0; i < _chunks.Length; i++)
            {
                ModManifestReportChunk part = _chunks[i];
                if (part == null) return ModManifestChunkAddOutcome.Invalid;
                for (int j = 0; j < part.Entries.Count; j++)
                {
                    entries.Add(part.Entries[j]);
                    if (entries.Count > ModIntegrityWire.MaxPlugins)
                        return ModManifestChunkAddOutcome.Invalid;
                }
            }

            if (!ModManifestComparer.TryCreateManifest(entries, out ModManifest manifest, out _) ||
                !string.Equals(manifest.Fingerprint, ManifestFingerprint, StringComparison.Ordinal))
                return ModManifestChunkAddOutcome.Invalid;

            Manifest = manifest;
            return ModManifestChunkAddOutcome.Completed;
        }

        private static bool SameEntries(
            IReadOnlyList<ModManifestEntry> left,
            IReadOnlyList<ModManifestEntry> right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                ModManifestEntry a = left[i];
                ModManifestEntry b = right[i];
                if (!string.Equals(a.Guid, b.Guid, StringComparison.Ordinal) ||
                    !string.Equals(a.Name, b.Name, StringComparison.Ordinal) ||
                    !string.Equals(a.Version, b.Version, StringComparison.Ordinal) ||
                    !string.Equals(a.ContentId, b.ContentId, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }

    public static class ModIntegrityWire
    {
        public const byte ProtocolVersion = 1;
        public const int MaxParticipants = 100;
        public const int MaxPlugins = 512;
        public const int ManifestEntriesPerChunk = 32;
        public const int MaxManifestChunks = 16;
        public const int MaxDifferences = 1024;
        public const int DetailEntriesPerChunk = 32;
        public const int MaxDetailChunks = 32;
        public const int MaxTextCharsPerChunk = 16384;
        public const int FingerprintLength = 64;
        public const int MaxGuidLength = 128;
        public const int MaxNameLength = 128;
        public const int MaxVersionLength = 64;
        public const int MaxContentIdLength = 96;
        public const int DetailRequestTimeoutMs = 3000;

        public static object[] BuildManifestRequest(int epoch, string baselineFingerprint)
        {
            return NetPayload.Build(new object[] { epoch, ProtocolVersion, baselineFingerprint });
        }

        public static bool TryReadManifestRequest(
            object[] payload,
            out ModManifestRequestMessage request,
            out string error)
        {
            request = null;
            error = null;
            if (payload == null || payload.Length != 3) return Fail("arity", out error);
            int epoch = (int)payload[0];
            byte protocol = (byte)payload[1];
            string fingerprint = (string)payload[2];
            if (epoch <= 0) return Fail("epoch", out error);
            if (protocol != ProtocolVersion) return Fail("protocol", out error);
            if (!ModManifestComparer.IsCanonicalFingerprint(fingerprint)) return Fail("fingerprint", out error);
            request = new ModManifestRequestMessage(epoch, protocol, fingerprint);
            return true;
        }

        public static IReadOnlyList<object[]> BuildManifestReport(
            int epoch,
            string baselineFingerprint,
            ModManifest manifest)
        {
            if (epoch <= 0) throw new ArgumentOutOfRangeException(nameof(epoch));
            if (!ModManifestComparer.IsCanonicalFingerprint(baselineFingerprint))
                throw new ArgumentException(nameof(baselineFingerprint));
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (manifest.Entries.Count > MaxPlugins) throw new ArgumentOutOfRangeException(nameof(manifest));

            var payloads = new List<object[]>();
            if (string.Equals(manifest.Fingerprint, baselineFingerprint, StringComparison.Ordinal))
            {
                payloads.Add(NetPayload.Build(new object[]
                {
                    epoch, ProtocolVersion, 0, 0, manifest.Fingerprint,
                    Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
                }));
                return payloads.AsReadOnly();
            }

            int chunkCount = Math.Max(1, (manifest.Entries.Count + ManifestEntriesPerChunk - 1) / ManifestEntriesPerChunk);
            if (chunkCount > MaxManifestChunks) throw new ArgumentOutOfRangeException(nameof(manifest));
            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int start = chunkIndex * ManifestEntriesPerChunk;
                int count = Math.Min(ManifestEntriesPerChunk, manifest.Entries.Count - start);
                var guids = new string[count];
                var names = new string[count];
                var versions = new string[count];
                var contentIds = new string[count];
                for (int i = 0; i < count; i++)
                {
                    ModManifestEntry entry = manifest.Entries[start + i];
                    guids[i] = entry.Guid;
                    names[i] = entry.Name;
                    versions[i] = entry.Version;
                    contentIds[i] = entry.ContentId;
                }
                ValidateManifestArrays(guids, names, versions, contentIds, true);
                payloads.Add(NetPayload.Build(new object[]
                {
                    epoch, ProtocolVersion, chunkIndex, chunkCount, manifest.Fingerprint,
                    guids, names, versions, contentIds,
                }));
            }
            return payloads.AsReadOnly();
        }

        public static bool TryReadManifestReport(
            object[] payload,
            out ModManifestReportChunk report,
            out string error)
        {
            report = null;
            error = null;
            if (payload == null || payload.Length != 9) return Fail("arity", out error);
            int epoch = (int)payload[0];
            byte protocol = (byte)payload[1];
            int chunkIndex = (int)payload[2];
            int chunkCount = (int)payload[3];
            string fingerprint = (string)payload[4];
            string[] guids = (string[])payload[5];
            string[] names = (string[])payload[6];
            string[] versions = (string[])payload[7];
            string[] contentIds = (string[])payload[8];

            if (epoch <= 0) return Fail("epoch", out error);
            if (protocol != ProtocolVersion) return Fail("protocol", out error);
            if (!ModManifestComparer.IsCanonicalFingerprint(fingerprint)) return Fail("fingerprint", out error);

            if (chunkCount == 0)
            {
                if (chunkIndex != 0 || !AllEmpty(guids, names, versions, contentIds))
                    return Fail("fingerprint_shape", out error);
                report = new ModManifestReportChunk(
                    epoch, protocol, 0, 0, fingerprint, Array.Empty<ModManifestEntry>());
                return true;
            }

            if (chunkCount < 1 || chunkCount > MaxManifestChunks ||
                chunkIndex < 0 || chunkIndex >= chunkCount)
                return Fail("chunk", out error);
            if (!ValidateManifestArrays(guids, names, versions, contentIds, false))
                return Fail("entries", out error);

            var entries = new List<ModManifestEntry>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                entries.Add(new ModManifestEntry(guids[i], names[i], versions[i], contentIds[i]));
            }
            report = new ModManifestReportChunk(
                epoch, protocol, chunkIndex, chunkCount, fingerprint, entries);
            return true;
        }

        public static object[] BuildSnapshot(ModIntegritySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Records.Count > MaxParticipants) throw new ArgumentOutOfRangeException(nameof(snapshot));
            int count = snapshot.Records.Count;
            var actors = new int[count];
            var statuses = new byte[count];
            var reasons = new byte[count];
            var diffCounts = new int[count * 4];
            for (int i = 0; i < count; i++)
            {
                ModParticipantRecord record = snapshot.Records[i];
                actors[i] = record.Actor;
                statuses[i] = (byte)record.Status;
                reasons[i] = (byte)record.UnavailableReason;
                int offset = i * 4;
                diffCounts[offset] = record.Summary.Missing;
                diffCounts[offset + 1] = record.Summary.Extra;
                diffCounts[offset + 2] = record.Summary.Version;
                diffCounts[offset + 3] = record.Summary.Content;
            }
            return NetPayload.Build(new object[]
            {
                snapshot.Epoch, snapshot.Revision, snapshot.BaselineActor,
                actors, statuses, reasons, diffCounts,
            });
        }

        public static bool TryReadSnapshot(
            object[] payload,
            out ModIntegritySnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = null;
            if (payload == null || payload.Length != 7) return Fail("arity", out error);
            int epoch = (int)payload[0];
            int revision = (int)payload[1];
            int baselineActor = (int)payload[2];
            int[] actors = (int[])payload[3];
            byte[] statuses = (byte[])payload[4];
            byte[] reasons = (byte[])payload[5];
            int[] diffCounts = (int[])payload[6];
            if (actors == null || statuses == null || reasons == null || diffCounts == null ||
                actors.Length == 0 || actors.Length > MaxParticipants ||
                statuses.Length != actors.Length || reasons.Length != actors.Length ||
                diffCounts.Length != actors.Length * 4)
                return Fail("arrays", out error);

            var records = new List<ModParticipantRecord>(actors.Length);
            for (int i = 0; i < actors.Length; i++)
            {
                if (!Enum.IsDefined(typeof(ModIntegrityStatus), statuses[i]) ||
                    !Enum.IsDefined(typeof(ModUnavailableReason), reasons[i]))
                    return Fail("enum", out error);
                int offset = i * 4;
                if (diffCounts[offset] < 0 || diffCounts[offset + 1] < 0 ||
                    diffCounts[offset + 2] < 0 || diffCounts[offset + 3] < 0)
                    return Fail("diff", out error);
                records.Add(new ModParticipantRecord(
                    actors[i],
                    (ModIntegrityStatus)statuses[i],
                    (ModUnavailableReason)reasons[i],
                    new ModDifferenceSummary(
                        diffCounts[offset], diffCounts[offset + 1],
                        diffCounts[offset + 2], diffCounts[offset + 3])));
            }

            return ModIntegritySnapshot.TryCreate(
                epoch, revision, baselineActor, records, out snapshot, out error);
        }

        public static object[] BuildDetailRequest(int epoch, int actor)
        {
            return NetPayload.Build(new object[] { epoch, actor });
        }

        public static bool TryReadDetailRequest(
            object[] payload,
            out ModIntegrityDetailRequestMessage request,
            out string error)
        {
            request = null;
            error = null;
            if (payload == null || payload.Length != 2) return Fail("arity", out error);
            int epoch = (int)payload[0];
            int actor = (int)payload[1];
            if (epoch <= 0 || actor <= 0) return Fail("value", out error);
            request = new ModIntegrityDetailRequestMessage(epoch, actor);
            return true;
        }

        public static IReadOnlyList<object[]> BuildDetailResponse(
            int epoch,
            int revision,
            int actor,
            IReadOnlyList<ModDifference> differences)
        {
            if (epoch <= 0 || revision < 0 || actor <= 0) throw new ArgumentOutOfRangeException(nameof(epoch));
            int total = differences?.Count ?? 0;
            if (total > MaxDifferences) throw new ArgumentOutOfRangeException(nameof(differences));
            int chunkCount = Math.Max(1, (total + DetailEntriesPerChunk - 1) / DetailEntriesPerChunk);
            if (chunkCount > MaxDetailChunks) throw new ArgumentOutOfRangeException(nameof(differences));

            var payloads = new List<object[]>(chunkCount);
            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int start = chunkIndex * DetailEntriesPerChunk;
                int count = Math.Min(DetailEntriesPerChunk, total - start);
                var kinds = new byte[count];
                var guids = new string[count];
                var names = new string[count];
                var baselineValues = new string[count];
                var participantValues = new string[count];
                for (int i = 0; i < count; i++)
                {
                    ModDifference difference = differences[start + i];
                    kinds[i] = (byte)difference.Kind;
                    guids[i] = difference.Guid;
                    names[i] = difference.Name;
                    baselineValues[i] = difference.BaselineValue;
                    participantValues[i] = difference.ParticipantValue;
                }
                ValidateDetailArrays(kinds, guids, names, baselineValues, participantValues, true);
                payloads.Add(NetPayload.Build(new object[]
                {
                    epoch, revision, actor, chunkIndex, chunkCount,
                    kinds, guids, names, baselineValues, participantValues,
                }));
            }
            return payloads.AsReadOnly();
        }

        public static bool TryReadDetailResponse(
            object[] payload,
            out ModIntegrityDetailChunk chunk,
            out string error)
        {
            chunk = null;
            error = null;
            if (payload == null || payload.Length != 10) return Fail("arity", out error);
            int epoch = (int)payload[0];
            int revision = (int)payload[1];
            int actor = (int)payload[2];
            int chunkIndex = (int)payload[3];
            int chunkCount = (int)payload[4];
            byte[] kinds = (byte[])payload[5];
            string[] guids = (string[])payload[6];
            string[] names = (string[])payload[7];
            string[] baselineValues = (string[])payload[8];
            string[] participantValues = (string[])payload[9];
            if (epoch <= 0 || revision < 0 || actor <= 0 ||
                chunkCount < 1 || chunkCount > MaxDetailChunks ||
                chunkIndex < 0 || chunkIndex >= chunkCount)
                return Fail("value", out error);
            if (!ValidateDetailArrays(kinds, guids, names, baselineValues, participantValues, false))
                return Fail("entries", out error);

            var differences = new List<ModDifference>(kinds.Length);
            for (int i = 0; i < kinds.Length; i++)
            {
                differences.Add(new ModDifference(
                    (ModDifferenceKind)kinds[i], guids[i], names[i],
                    baselineValues[i], participantValues[i]));
            }
            chunk = new ModIntegrityDetailChunk(
                epoch, revision, actor, chunkIndex, chunkCount, differences);
            return true;
        }

        private static bool ValidateManifestArrays(
            string[] guids,
            string[] names,
            string[] versions,
            string[] contentIds,
            bool throwOnFailure)
        {
            bool valid = guids != null && names != null && versions != null && contentIds != null &&
                guids.Length == names.Length && guids.Length == versions.Length && guids.Length == contentIds.Length &&
                guids.Length <= ManifestEntriesPerChunk;
            int chars = 0;
            if (valid)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    valid = ValidText(guids[i], 1, MaxGuidLength) &&
                        ValidText(names[i], 0, MaxNameLength) &&
                        ValidText(versions[i], 0, MaxVersionLength) &&
                        ValidText(contentIds[i], 0, MaxContentIdLength);
                    if (!valid) break;
                    chars += guids[i].Length + names[i].Length + versions[i].Length + contentIds[i].Length;
                    if (chars > MaxTextCharsPerChunk) { valid = false; break; }
                }
            }
            if (!valid && throwOnFailure) throw new ArgumentException("manifest_entries");
            return valid;
        }

        private static bool ValidateDetailArrays(
            byte[] kinds,
            string[] guids,
            string[] names,
            string[] baselineValues,
            string[] participantValues,
            bool throwOnFailure)
        {
            bool valid = kinds != null && guids != null && names != null &&
                baselineValues != null && participantValues != null &&
                kinds.Length == guids.Length && kinds.Length == names.Length &&
                kinds.Length == baselineValues.Length && kinds.Length == participantValues.Length &&
                kinds.Length <= DetailEntriesPerChunk;
            int chars = 0;
            if (valid)
            {
                for (int i = 0; i < kinds.Length; i++)
                {
                    valid = Enum.IsDefined(typeof(ModDifferenceKind), kinds[i]) &&
                        ValidText(guids[i], 1, MaxGuidLength) &&
                        ValidText(names[i], 0, MaxNameLength) &&
                        ValidText(baselineValues[i], 0, MaxContentIdLength) &&
                        ValidText(participantValues[i], 0, MaxContentIdLength);
                    if (!valid) break;
                    chars += guids[i].Length + names[i].Length + baselineValues[i].Length + participantValues[i].Length;
                    if (chars > MaxTextCharsPerChunk) { valid = false; break; }
                }
            }
            if (!valid && throwOnFailure) throw new ArgumentException("detail_entries");
            return valid;
        }

        private static bool AllEmpty(params Array[] arrays)
        {
            if (arrays == null) return false;
            for (int i = 0; i < arrays.Length; i++)
            {
                if (arrays[i] == null || arrays[i].Length != 0) return false;
            }
            return true;
        }

        private static bool ValidText(string value, int min, int max)
        {
            return value != null && value.Length >= min && value.Length <= max;
        }

        private static bool Fail(string value, out string error)
        {
            error = value;
            return false;
        }
    }
}
