using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class ModIntegrityClientState
    {
        public const int DetailRequestTimeoutMs = 3000;
        public const int MaxDetailChunks = 32;
        public const int MaxDetailEntries = 1024;
        public const int DetailEntriesPerChunk = 32;

        private sealed class DetailAssembly
        {
            public int ChunkCount;
            public IReadOnlyList<ModDifference>[] Chunks;
            public int Received;
        }

        private readonly Dictionary<int, ModIntegrityDetail> _details = new Dictionary<int, ModIntegrityDetail>();
        private int _detailRequestActor = -1;
        private long _detailRequestStartedUnixMs;
        private DetailAssembly _detailAssembly;

        public ModIntegritySnapshot Current { get; private set; }
        public int PendingDetailActor => _detailRequestActor;

        public bool TryApplySnapshot(ModIntegritySnapshot snapshot, int expectedMasterActor)
        {
            if (snapshot == null || expectedMasterActor <= 0 || snapshot.BaselineActor != expectedMasterActor)
                return false;

            if (Current != null)
            {
                if (snapshot.Epoch < Current.Epoch) return false;
                if (snapshot.Epoch == Current.Epoch && snapshot.Revision <= Current.Revision) return false;
            }

            Current = snapshot;
            ClearDetails();
            return true;
        }

        public bool BeginDetailRequest(int actor, long nowUnixMs)
        {
            if (Current == null || actor <= 0) return false;
            if (!Current.TryGetRecord(actor, out ModParticipantRecord record) ||
                record.Status != ModIntegrityStatus.Difference)
                return false;
            if (_details.ContainsKey(actor)) return false;
            if (_detailRequestActor == actor) return false;

            _detailRequestActor = actor;
            _detailRequestStartedUnixMs = nowUnixMs;
            _detailAssembly = null;
            return true;
        }

        public bool TryApplyDetailChunk(ModIntegrityDetailChunk chunk, out bool completed)
        {
            completed = false;
            if (Current == null || chunk == null) return false;
            if (chunk.Epoch != Current.Epoch || chunk.Revision != Current.Revision) return false;
            if (chunk.Actor != _detailRequestActor) return false;
            if (chunk.ChunkCount < 1 || chunk.ChunkCount > MaxDetailChunks ||
                chunk.ChunkIndex < 0 || chunk.ChunkIndex >= chunk.ChunkCount ||
                chunk.Differences.Count > DetailEntriesPerChunk)
            {
                AbandonDetailRequest();
                return false;
            }

            if (_detailAssembly == null)
            {
                _detailAssembly = new DetailAssembly
                {
                    ChunkCount = chunk.ChunkCount,
                    Chunks = new IReadOnlyList<ModDifference>[chunk.ChunkCount],
                };
            }
            else if (_detailAssembly.ChunkCount != chunk.ChunkCount)
            {
                AbandonDetailRequest();
                return false;
            }

            IReadOnlyList<ModDifference> existing = _detailAssembly.Chunks[chunk.ChunkIndex];
            if (existing != null)
            {
                if (SameDifferences(existing, chunk.Differences)) return true;
                AbandonDetailRequest();
                return false;
            }

            _detailAssembly.Chunks[chunk.ChunkIndex] = ModIntegrityCollections.Freeze(chunk.Differences);
            _detailAssembly.Received++;
            if (_detailAssembly.Received != _detailAssembly.ChunkCount) return true;

            var all = new List<ModDifference>();
            for (int i = 0; i < _detailAssembly.Chunks.Length; i++)
            {
                IReadOnlyList<ModDifference> part = _detailAssembly.Chunks[i];
                if (part == null)
                {
                    AbandonDetailRequest();
                    return false;
                }
                for (int j = 0; j < part.Count; j++)
                {
                    all.Add(part[j]);
                    if (all.Count > MaxDetailEntries)
                    {
                        AbandonDetailRequest();
                        return false;
                    }
                }
            }

            _details[chunk.Actor] = new ModIntegrityDetail(
                chunk.Epoch, chunk.Revision, chunk.Actor, all);
            _detailRequestActor = -1;
            _detailRequestStartedUnixMs = 0;
            _detailAssembly = null;
            completed = true;
            return true;
        }

        public bool TickDetailTimeout(long nowUnixMs, out int timedOutActor)
        {
            timedOutActor = -1;
            if (_detailRequestActor < 0 || nowUnixMs - _detailRequestStartedUnixMs < DetailRequestTimeoutMs)
                return false;
            timedOutActor = _detailRequestActor;
            AbandonDetailRequest();
            return true;
        }

        public bool TryGetDetail(int actor, out ModIntegrityDetail detail)
        {
            return _details.TryGetValue(actor, out detail);
        }

        public void Clear()
        {
            Current = null;
            ClearDetails();
        }

        private void ClearDetails()
        {
            _details.Clear();
            _detailRequestActor = -1;
            _detailRequestStartedUnixMs = 0;
            _detailAssembly = null;
        }

        private void AbandonDetailRequest()
        {
            _detailRequestActor = -1;
            _detailRequestStartedUnixMs = 0;
            _detailAssembly = null;
        }

        private static bool SameDifferences(
            IReadOnlyList<ModDifference> left,
            IReadOnlyList<ModDifference> right)
        {
            if (left == null || right == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                ModDifference a = left[i];
                ModDifference b = right[i];
                if (a == null || b == null ||
                    a.Kind != b.Kind ||
                    !string.Equals(a.Guid, b.Guid, StringComparison.Ordinal) ||
                    !string.Equals(a.Name, b.Name, StringComparison.Ordinal) ||
                    !string.Equals(a.BaselineValue, b.BaselineValue, StringComparison.Ordinal) ||
                    !string.Equals(a.ParticipantValue, b.ParticipantValue, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }
}
