using System;

namespace Werewolf.Core
{
    public enum LobbyStartWarningKind
    {
        None = 0,
        Caution = 1,
        Severe = 2,
    }

    public readonly struct LobbyStartDecision
    {
        public LobbyStartDecision(
            LobbyStartWarningKind modWarningKind,
            int differenceCount,
            int unavailableCount,
            int pendingCount,
            bool baselineReady,
            int playerCount,
            int requiredPlayerCount,
            bool playerCountSatisfied,
            int werewolfCount,
            bool teamTotalSatisfied)
        {
            ModWarningKind = modWarningKind;
            DifferenceCount = differenceCount;
            UnavailableCount = unavailableCount;
            PendingCount = pendingCount;
            BaselineReady = baselineReady;
            PlayerCount = playerCount;
            RequiredPlayerCount = requiredPlayerCount;
            PlayerCountSatisfied = playerCountSatisfied;
            WerewolfCount = werewolfCount;
            TeamTotalSatisfied = teamTotalSatisfied;
        }

        public LobbyStartWarningKind ModWarningKind { get; }
        public int DifferenceCount { get; }
        public int UnavailableCount { get; }
        public int PendingCount { get; }
        public bool BaselineReady { get; }
        public int PlayerCount { get; }
        public int RequiredPlayerCount { get; }
        public bool PlayerCountSatisfied { get; }
        public int WerewolfCount { get; }
        public bool TeamTotalSatisfied { get; }

        public bool HasPending => PendingCount > 0;
        public bool HasPlayerShortfall => !PlayerCountSatisfied;

        public bool HasTeamOverflow => !TeamTotalSatisfied;

        public bool AllowsContinue => !HasPlayerShortfall && !HasTeamOverflow;

        public LobbyStartWarningKind WarningKind =>
            (HasPlayerShortfall || HasTeamOverflow) && ModWarningKind == LobbyStartWarningKind.None
                ? LobbyStartWarningKind.Caution
                : ModWarningKind;
    }

    public static class LobbyStartGate
    {
        public const int FinalCheckWaitMs = 1500;
        public const int AttemptTokenLifetimeMs = 5000;

        public static LobbyStartDecision Evaluate(
            ModIntegritySnapshot snapshot,
            bool baselineReady,
            int playerCount,
            int werewolfCount,
            bool debugMode)
        {
            int difference = 0;
            int unavailable = 0;
            int pending = 0;

            if (snapshot != null)
            {
                for (int i = 0; i < snapshot.Records.Count; i++)
                {
                    switch (snapshot.Records[i].Status)
                    {
                        case ModIntegrityStatus.Difference: difference++; break;
                        case ModIntegrityStatus.Unavailable: unavailable++; break;
                        case ModIntegrityStatus.Pending: pending++; break;
                    }
                }
            }

            LobbyStartWarningKind modKind;
            if (!baselineReady || snapshot == null || unavailable > 0 || pending > 0)
                modKind = LobbyStartWarningKind.Severe;
            else if (difference > 0)
                modKind = LobbyStartWarningKind.Caution;
            else
                modKind = LobbyStartWarningKind.None;

            bool teamTotalSatisfied = debugMode ||
                !ConfigValidator.WerewolfCountExceedsPlayers(werewolfCount, playerCount);

            return new LobbyStartDecision(
                modKind, difference, unavailable, pending, baselineReady,
                playerCount, PlayerCountGate.MinimumPlayers,
                PlayerCountGate.IsSatisfied(playerCount, debugMode),
                werewolfCount, teamTotalSatisfied);
        }
    }

    public sealed class LobbyStartAttempt
    {
        private bool _bypassArmed;
        private int _lobbyInstanceId;
        private int _authorizingHostActor;
        private long _bypassExpiresUnixMs;

        public bool IsFinalCheckPending { get; private set; }
        public long FinalCheckDeadlineUnixMs { get; private set; }
        public bool IsBypassArmed => _bypassArmed;

        public bool TryBeginFinalCheck(bool hasPending, long nowUnixMs)
        {
            if (!hasPending || IsFinalCheckPending) return false;
            IsFinalCheckPending = true;
            FinalCheckDeadlineUnixMs = nowUnixMs + LobbyStartGate.FinalCheckWaitMs;
            return true;
        }

        public bool ShouldCompleteFinalCheck(bool hasPending, long nowUnixMs)
        {
            if (!IsFinalCheckPending) return false;
            if (hasPending && nowUnixMs < FinalCheckDeadlineUnixMs) return false;
            IsFinalCheckPending = false;
            FinalCheckDeadlineUnixMs = 0;
            return true;
        }

        public void CancelFinalCheck()
        {
            IsFinalCheckPending = false;
            FinalCheckDeadlineUnixMs = 0;
        }

        public void ArmOneShotBypass(
            int lobbyInstanceId,
            int authorizingHostActor,
            long nowUnixMs)
        {
            if (lobbyInstanceId == 0) throw new ArgumentOutOfRangeException(nameof(lobbyInstanceId));
            if (authorizingHostActor <= 0) throw new ArgumentOutOfRangeException(nameof(authorizingHostActor));
            CancelFinalCheck();
            _bypassArmed = true;
            _lobbyInstanceId = lobbyInstanceId;
            _authorizingHostActor = authorizingHostActor;
            _bypassExpiresUnixMs = nowUnixMs + LobbyStartGate.AttemptTokenLifetimeMs;
        }

        public bool TryConsumeBypass(
            int lobbyInstanceId,
            int currentMasterActor,
            long nowUnixMs)
        {
            if (!_bypassArmed) return false;
            bool valid = lobbyInstanceId == _lobbyInstanceId &&
                currentMasterActor == _authorizingHostActor &&
                nowUnixMs <= _bypassExpiresUnixMs;
            ClearBypass();
            return valid;
        }

        public void Clear()
        {
            CancelFinalCheck();
            ClearBypass();
        }

        private void ClearBypass()
        {
            _bypassArmed = false;
            _lobbyInstanceId = 0;
            _authorizingHostActor = 0;
            _bypassExpiresUnixMs = 0;
        }
    }
}
