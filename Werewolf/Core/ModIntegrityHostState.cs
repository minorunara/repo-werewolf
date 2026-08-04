using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class ModIntegrityHostState
    {
        public const int ResponseTimeoutMs = 5000;
        public const int RetryIntervalMs = 1500;
        public const int MaxRequestAttempts = 3;
        public const int SnapshotCoalesceMs = 250;

        private sealed class ActorState
        {
            public int Actor;
            public ModIntegrityStatus Status;
            public ModUnavailableReason Reason;
            public ModDifferenceSummary Summary;
            public IReadOnlyList<ModDifference> Differences = ModIntegrityCollections.Freeze<ModDifference>(null);
            public string AcceptedFingerprint;
            public bool LockedInvalid;
            public long FirstRequestUnixMs;
            public long LastRequestUnixMs;
            public int RequestAttempts;
        }

        private readonly Dictionary<int, ActorState> _actors = new Dictionary<int, ActorState>();
        private long _lastSnapshotPublishedUnixMs;

        public int Epoch { get; private set; }
        public int Revision { get; private set; }
        public int BaselineActor { get; private set; }
        public ModManifest Baseline { get; private set; }
        public bool Dirty { get; private set; }
        public bool IsInitialized => Baseline != null;

        public void BeginEpoch(
            int epoch,
            int baselineActor,
            IReadOnlyList<int> rosterActors,
            ModManifest baseline,
            long nowUnixMs)
        {
            if (epoch <= 0) throw new ArgumentOutOfRangeException(nameof(epoch));
            if (baselineActor <= 0) throw new ArgumentOutOfRangeException(nameof(baselineActor));
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            ValidateRoster(rosterActors, baselineActor);

            _actors.Clear();
            Epoch = epoch;
            Revision = 1;
            BaselineActor = baselineActor;
            Baseline = baseline;
            Dirty = true;
            _lastSnapshotPublishedUnixMs = nowUnixMs;

            for (int i = 0; i < rosterActors.Count; i++)
            {
                int actor = rosterActors[i];
                _actors[actor] = actor == baselineActor
                    ? CreateBaseline(actor)
                    : CreatePending(actor, nowUnixMs);
            }
        }

        public void SyncRoster(IReadOnlyList<int> rosterActors, long nowUnixMs)
        {
            if (!IsInitialized) return;
            ValidateRoster(rosterActors, BaselineActor);

            var current = new HashSet<int>();
            for (int i = 0; i < rosterActors.Count; i++) current.Add(rosterActors[i]);

            bool changed = false;
            var removed = new List<int>();
            foreach (int actor in _actors.Keys)
            {
                if (!current.Contains(actor)) removed.Add(actor);
            }
            for (int i = 0; i < removed.Count; i++)
            {
                _actors.Remove(removed[i]);
                changed = true;
            }

            for (int i = 0; i < rosterActors.Count; i++)
            {
                int actor = rosterActors[i];
                if (_actors.ContainsKey(actor)) continue;
                _actors[actor] = actor == BaselineActor
                    ? CreateBaseline(actor)
                    : CreatePending(actor, nowUnixMs);
                changed = true;
            }

            if (changed) Touch();
        }

        public FingerprintReportOutcome ApplyFingerprintReport(
            int actor,
            string reportedFingerprint,
            long nowUnixMs)
        {
            if (!TryGetParticipant(actor, out ActorState state)) return FingerprintReportOutcome.RejectedInvalid;
            if (!ModManifestComparer.IsCanonicalFingerprint(reportedFingerprint))
            {
                SetUnavailable(state, ModUnavailableReason.InvalidPayload);
                return FingerprintReportOutcome.RejectedInvalid;
            }
            if (state.LockedInvalid) return FingerprintReportOutcome.RejectedInvalid;

            if (!string.IsNullOrEmpty(state.AcceptedFingerprint))
            {
                if (string.Equals(state.AcceptedFingerprint, reportedFingerprint, StringComparison.Ordinal))
                    return state.Status == ModIntegrityStatus.Match
                        ? FingerprintReportOutcome.Matched
                        : FingerprintReportOutcome.RejectedInvalid;
                LockConflictingReport(state);
                return FingerprintReportOutcome.RejectedInvalid;
            }

            if (!string.Equals(reportedFingerprint, Baseline.Fingerprint, StringComparison.Ordinal))
            {
                state.LastRequestUnixMs = nowUnixMs;
                return FingerprintReportOutcome.RetryCurrentRequest;
            }

            state.AcceptedFingerprint = reportedFingerprint;
            SetResolved(state, ModIntegrityStatus.Match, default, ModIntegrityCollections.Freeze<ModDifference>(null));
            return FingerprintReportOutcome.Matched;
        }

        public void ApplyManifest(int actor, ModManifest manifest, long nowUnixMs)
        {
            if (manifest == null || !TryGetParticipant(actor, out ActorState state)) return;
            if (state.LockedInvalid) return;

            if (!string.IsNullOrEmpty(state.AcceptedFingerprint))
            {
                if (string.Equals(state.AcceptedFingerprint, manifest.Fingerprint, StringComparison.Ordinal)) return;
                LockConflictingReport(state);
                return;
            }

            state.AcceptedFingerprint = manifest.Fingerprint;
            ModComparisonResult comparison = ModManifestComparer.Compare(Baseline, manifest);
            SetResolved(
                state,
                comparison.IsMatch ? ModIntegrityStatus.Match : ModIntegrityStatus.Difference,
                comparison.Summary,
                comparison.Differences);
        }

        public void MarkUnavailable(int actor, ModUnavailableReason reason)
        {
            if (reason == ModUnavailableReason.None || !TryGetParticipant(actor, out ActorState state)) return;
            SetUnavailable(state, reason);
        }

        public IReadOnlyList<int> GetRetryTargets(long nowUnixMs, bool force)
        {
            var result = new List<int>();
            foreach (ActorState state in _actors.Values)
            {
                if (state.Status != ModIntegrityStatus.Pending) continue;
                if (!force)
                {
                    if (state.RequestAttempts >= MaxRequestAttempts) continue;
                    if (nowUnixMs - state.LastRequestUnixMs < RetryIntervalMs) continue;
                    state.RequestAttempts++;
                }
                state.LastRequestUnixMs = nowUnixMs;
                result.Add(state.Actor);
            }
            result.Sort();
            return result.AsReadOnly();
        }

        public bool TickTimeouts(long nowUnixMs)
        {
            bool changed = false;
            foreach (ActorState state in _actors.Values)
            {
                if (state.Status != ModIntegrityStatus.Pending) continue;
                if (nowUnixMs - state.FirstRequestUnixMs < ResponseTimeoutMs) continue;
                state.Status = ModIntegrityStatus.Unavailable;
                state.Reason = ModUnavailableReason.NoResponse;
                state.Summary = default;
                state.Differences = ModIntegrityCollections.Freeze<ModDifference>(null);
                changed = true;
            }
            if (changed) Touch();
            return changed;
        }

        public bool ShouldPublishSnapshot(long nowUnixMs, bool force)
        {
            if (!Dirty || !IsInitialized) return false;
            return force || nowUnixMs - _lastSnapshotPublishedUnixMs >= SnapshotCoalesceMs;
        }

        public ModIntegritySnapshot BuildSnapshot()
        {
            if (!IsInitialized) return null;
            var records = new List<ModParticipantRecord>(_actors.Count);
            if (_actors.TryGetValue(BaselineActor, out ActorState baseline)) records.Add(ToRecord(baseline));
            var actorNumbers = new List<int>(_actors.Keys);
            actorNumbers.Sort();
            for (int i = 0; i < actorNumbers.Count; i++)
            {
                int actor = actorNumbers[i];
                if (actor == BaselineActor) continue;
                records.Add(ToRecord(_actors[actor]));
            }

            if (!ModIntegritySnapshot.TryCreate(
                Epoch, Revision, BaselineActor, records,
                out ModIntegritySnapshot snapshot, out string error))
                throw new InvalidOperationException($"snapshot:{error}");
            return snapshot;
        }

        public IReadOnlyList<ModDifference> GetDifferences(int actor)
        {
            return _actors.TryGetValue(actor, out ActorState state)
                ? state.Differences
                : ModIntegrityCollections.Freeze<ModDifference>(null);
        }

        public void MarkSnapshotPublished(long nowUnixMs)
        {
            if (!IsInitialized) return;
            Dirty = false;
            _lastSnapshotPublishedUnixMs = nowUnixMs;
        }

        public void Clear()
        {
            _actors.Clear();
            Epoch = 0;
            Revision = 0;
            BaselineActor = 0;
            Baseline = null;
            Dirty = false;
            _lastSnapshotPublishedUnixMs = 0;
        }

        private static ActorState CreateBaseline(int actor)
        {
            return new ActorState
            {
                Actor = actor,
                Status = ModIntegrityStatus.Baseline,
                Reason = ModUnavailableReason.None,
            };
        }

        private static ActorState CreatePending(int actor, long nowUnixMs)
        {
            return new ActorState
            {
                Actor = actor,
                Status = ModIntegrityStatus.Pending,
                Reason = ModUnavailableReason.None,
                FirstRequestUnixMs = nowUnixMs,
                LastRequestUnixMs = nowUnixMs,
                RequestAttempts = 1,
            };
        }

        private bool TryGetParticipant(int actor, out ActorState state)
        {
            if (actor == BaselineActor || !_actors.TryGetValue(actor, out state))
            {
                state = null;
                return false;
            }
            return true;
        }

        private void SetResolved(
            ActorState state,
            ModIntegrityStatus status,
            ModDifferenceSummary summary,
            IReadOnlyList<ModDifference> differences)
        {
            bool changed = state.Status != status ||
                state.Reason != ModUnavailableReason.None ||
                state.Summary != summary;
            state.Status = status;
            state.Reason = ModUnavailableReason.None;
            state.Summary = summary;
            state.Differences = ModIntegrityCollections.Freeze(differences);
            if (changed) Touch();
        }

        private void SetUnavailable(ActorState state, ModUnavailableReason reason)
        {
            if (state.Status == ModIntegrityStatus.Unavailable && state.Reason == reason) return;
            state.Status = ModIntegrityStatus.Unavailable;
            state.Reason = reason;
            state.Summary = default;
            state.Differences = ModIntegrityCollections.Freeze<ModDifference>(null);
            Touch();
        }

        private void LockConflictingReport(ActorState state)
        {
            state.LockedInvalid = true;
            SetUnavailable(state, ModUnavailableReason.InvalidPayload);
        }

        private void Touch()
        {
            Revision++;
            Dirty = true;
        }

        private static ModParticipantRecord ToRecord(ActorState state)
        {
            return new ModParticipantRecord(state.Actor, state.Status, state.Reason, state.Summary);
        }

        private static void ValidateRoster(IReadOnlyList<int> rosterActors, int baselineActor)
        {
            if (rosterActors == null) throw new ArgumentNullException(nameof(rosterActors));
            if (rosterActors.Count == 0 || rosterActors.Count > 100)
                throw new ArgumentOutOfRangeException(nameof(rosterActors));
            var actors = new HashSet<int>();
            bool hasBaseline = false;
            for (int i = 0; i < rosterActors.Count; i++)
            {
                int actor = rosterActors[i];
                if (actor <= 0 || !actors.Add(actor)) throw new ArgumentException("actor", nameof(rosterActors));
                if (actor == baselineActor) hasBaseline = true;
            }
            if (!hasBaseline) throw new ArgumentException("baseline", nameof(rosterActors));
        }
    }
}
