using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum ModIntegrityStatus : byte
    {
        Baseline = 0,
        Pending = 1,
        Match = 2,
        Difference = 3,
        Unavailable = 4,
    }

    public enum ModUnavailableReason : byte
    {
        None = 0,
        NoResponse = 1,
        UnsupportedProtocol = 2,
        InvalidPayload = 3,
        TooLarge = 4,
        ManifestCollectionFailed = 5,
    }

    public enum ModDifferenceKind : byte
    {
        Missing = 0,
        Extra = 1,
        Version = 2,
        Content = 3,
    }

    public enum FingerprintReportOutcome : byte
    {
        Matched = 0,
        RetryCurrentRequest = 1,
        RejectedInvalid = 2,
    }

    internal static class ModIntegrityCollections
    {
        public static IReadOnlyList<T> Freeze<T>(IEnumerable<T> items)
        {
            return items == null
                ? new List<T>().AsReadOnly()
                : new List<T>(items).AsReadOnly();
        }
    }

    public sealed class ModManifestEntry
    {
        public ModManifestEntry(string guid, string name, string version, string contentId)
        {
            Guid = guid;
            Name = name;
            Version = version;
            ContentId = contentId;
        }

        public string Guid { get; }
        public string Name { get; }
        public string Version { get; }
        public string ContentId { get; }
    }

    public sealed class ModManifest
    {
        internal ModManifest(IEnumerable<ModManifestEntry> entries, string fingerprint)
        {
            Entries = ModIntegrityCollections.Freeze(entries);
            Fingerprint = fingerprint;
        }

        public IReadOnlyList<ModManifestEntry> Entries { get; }
        public string Fingerprint { get; }
    }

    public sealed class ModDifference
    {
        public ModDifference(
            ModDifferenceKind kind,
            string guid,
            string name,
            string baselineValue,
            string participantValue)
        {
            Kind = kind;
            Guid = guid ?? string.Empty;
            Name = name ?? string.Empty;
            BaselineValue = baselineValue ?? string.Empty;
            ParticipantValue = participantValue ?? string.Empty;
        }

        public ModDifferenceKind Kind { get; }
        public string Guid { get; }
        public string Name { get; }
        public string BaselineValue { get; }
        public string ParticipantValue { get; }
    }

    public readonly struct ModDifferenceSummary : IEquatable<ModDifferenceSummary>
    {
        public ModDifferenceSummary(int missing, int extra, int version, int content)
        {
            if (missing < 0 || extra < 0 || version < 0 || content < 0)
                throw new ArgumentOutOfRangeException(nameof(missing));
            Missing = missing;
            Extra = extra;
            Version = version;
            Content = content;
        }

        public int Missing { get; }
        public int Extra { get; }
        public int Version { get; }
        public int Content { get; }
        public int Total => Missing + Extra + Version + Content;

        public bool Equals(ModDifferenceSummary other) =>
            Missing == other.Missing && Extra == other.Extra &&
            Version == other.Version && Content == other.Content;

        public override bool Equals(object obj) => obj is ModDifferenceSummary other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Missing, Extra, Version, Content);
        public static bool operator ==(ModDifferenceSummary left, ModDifferenceSummary right) => left.Equals(right);
        public static bool operator !=(ModDifferenceSummary left, ModDifferenceSummary right) => !left.Equals(right);
    }

    public sealed class ModComparisonResult
    {
        internal ModComparisonResult(IEnumerable<ModDifference> differences)
        {
            Differences = ModIntegrityCollections.Freeze(differences);
            int missing = 0;
            int extra = 0;
            int version = 0;
            int content = 0;
            for (int i = 0; i < Differences.Count; i++)
            {
                switch (Differences[i].Kind)
                {
                    case ModDifferenceKind.Missing: missing++; break;
                    case ModDifferenceKind.Extra: extra++; break;
                    case ModDifferenceKind.Version: version++; break;
                    case ModDifferenceKind.Content: content++; break;
                }
            }
            Summary = new ModDifferenceSummary(missing, extra, version, content);
        }

        public IReadOnlyList<ModDifference> Differences { get; }
        public ModDifferenceSummary Summary { get; }
        public bool IsMatch => Summary.Total == 0;
    }

    public sealed class ModParticipantRecord
    {
        internal ModParticipantRecord(
            int actor,
            ModIntegrityStatus status,
            ModUnavailableReason unavailableReason,
            ModDifferenceSummary summary)
        {
            Actor = actor;
            Status = status;
            UnavailableReason = unavailableReason;
            Summary = summary;
        }

        public int Actor { get; }
        public ModIntegrityStatus Status { get; }
        public ModUnavailableReason UnavailableReason { get; }
        public ModDifferenceSummary Summary { get; }
    }

    public sealed class ModIntegritySnapshot
    {
        private ModIntegritySnapshot(
            int epoch,
            int revision,
            int baselineActor,
            IEnumerable<ModParticipantRecord> records)
        {
            Epoch = epoch;
            Revision = revision;
            BaselineActor = baselineActor;
            Records = ModIntegrityCollections.Freeze(records);
        }

        public int Epoch { get; }
        public int Revision { get; }
        public int BaselineActor { get; }
        public IReadOnlyList<ModParticipantRecord> Records { get; }

        public static bool TryCreate(
            int epoch,
            int revision,
            int baselineActor,
            IEnumerable<ModParticipantRecord> records,
            out ModIntegritySnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = null;
            if (epoch <= 0) return Fail("epoch", out error);
            if (revision < 0) return Fail("revision", out error);
            if (baselineActor <= 0) return Fail("baseline_actor", out error);
            if (records == null) return Fail("records", out error);

            var list = new List<ModParticipantRecord>();
            var actors = new HashSet<int>();
            int baselineCount = 0;
            foreach (ModParticipantRecord record in records)
            {
                if (record == null) return Fail("null_record", out error);
                if (record.Actor <= 0 || !actors.Add(record.Actor)) return Fail("actor", out error);
                if (!Enum.IsDefined(typeof(ModIntegrityStatus), record.Status) ||
                    !Enum.IsDefined(typeof(ModUnavailableReason), record.UnavailableReason))
                    return Fail("enum", out error);
                if (record.Actor == baselineActor)
                {
                    if (record.Status != ModIntegrityStatus.Baseline) return Fail("baseline_status", out error);
                    baselineCount++;
                }
                else if (record.Status == ModIntegrityStatus.Baseline)
                {
                    return Fail("foreign_baseline", out error);
                }
                if (record.Status == ModIntegrityStatus.Unavailable)
                {
                    if (record.UnavailableReason == ModUnavailableReason.None) return Fail("missing_reason", out error);
                }
                else if (record.UnavailableReason != ModUnavailableReason.None)
                {
                    return Fail("unexpected_reason", out error);
                }
                if (record.Status == ModIntegrityStatus.Difference)
                {
                    if (record.Summary.Total <= 0 || record.Summary.Total > 1024) return Fail("difference_summary", out error);
                }
                else if (record.Summary.Total != 0)
                {
                    return Fail("unexpected_summary", out error);
                }
                list.Add(record);
                if (list.Count > 100) return Fail("too_many_records", out error);
            }

            if (baselineCount != 1) return Fail("baseline_count", out error);
            snapshot = new ModIntegritySnapshot(epoch, revision, baselineActor, list);
            return true;
        }

        public bool TryGetRecord(int actor, out ModParticipantRecord record)
        {
            for (int i = 0; i < Records.Count; i++)
            {
                if (Records[i].Actor == actor)
                {
                    record = Records[i];
                    return true;
                }
            }
            record = null;
            return false;
        }

        private static bool Fail(string value, out string error)
        {
            error = value;
            return false;
        }
    }

    public sealed class ModIntegrityDetailChunk
    {
        public ModIntegrityDetailChunk(
            int epoch,
            int revision,
            int actor,
            int chunkIndex,
            int chunkCount,
            IEnumerable<ModDifference> differences)
        {
            Epoch = epoch;
            Revision = revision;
            Actor = actor;
            ChunkIndex = chunkIndex;
            ChunkCount = chunkCount;
            Differences = ModIntegrityCollections.Freeze(differences);
        }

        public int Epoch { get; }
        public int Revision { get; }
        public int Actor { get; }
        public int ChunkIndex { get; }
        public int ChunkCount { get; }
        public IReadOnlyList<ModDifference> Differences { get; }
    }

    public sealed class ModIntegrityDetail
    {
        internal ModIntegrityDetail(int epoch, int revision, int actor, IEnumerable<ModDifference> differences)
        {
            Epoch = epoch;
            Revision = revision;
            Actor = actor;
            Differences = ModIntegrityCollections.Freeze(differences);
        }

        public int Epoch { get; }
        public int Revision { get; }
        public int Actor { get; }
        public IReadOnlyList<ModDifference> Differences { get; }
    }

    public sealed class ModParticipantView
    {
        public ModParticipantView(ModParticipantRecord record, string displayName, bool isLocal, bool isBaseline)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            DisplayName = string.IsNullOrEmpty(displayName) ? $"Actor {record.Actor}" : displayName;
            IsLocal = isLocal;
            IsBaseline = isBaseline;
        }

        public ModParticipantRecord Record { get; }
        public string DisplayName { get; }
        public bool IsLocal { get; }
        public bool IsBaseline { get; }
    }
}
