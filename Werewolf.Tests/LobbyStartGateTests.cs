using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class LobbyStartGateTests
    {
        private const int Enough = PlayerCountGate.MinimumPlayers;
        private const int TooFew = PlayerCountGate.MinimumPlayers - 1;

        [Fact]
        public void Evaluate_UsesSevereOverCautionAndNoneWhenClear()
        {
            Assert.Equal(LobbyStartWarningKind.None, Evaluate(ModIntegrityStatus.Match, true).WarningKind);
            Assert.Equal(LobbyStartWarningKind.Caution, Evaluate(ModIntegrityStatus.Difference, true).WarningKind);
            Assert.Equal(LobbyStartWarningKind.Severe, Evaluate(ModIntegrityStatus.Unavailable, true).WarningKind);
            Assert.Equal(LobbyStartWarningKind.Severe, Evaluate(ModIntegrityStatus.Pending, true).WarningKind);
            Assert.Equal(LobbyStartWarningKind.Severe, Evaluate(ModIntegrityStatus.Match, false).WarningKind);
        }

        [Fact]
        public void Evaluate_PlayerShortfall_RaisesCautionWithoutModIssue()
        {
            LobbyStartDecision decision = Evaluate(ModIntegrityStatus.Match, true, TooFew);

            Assert.True(decision.HasPlayerShortfall);
            Assert.Equal(TooFew, decision.PlayerCount);
            Assert.Equal(PlayerCountGate.MinimumPlayers, decision.RequiredPlayerCount);
            Assert.Equal(LobbyStartWarningKind.None, decision.ModWarningKind);
            Assert.Equal(LobbyStartWarningKind.Caution, decision.WarningKind);
        }

        [Fact]
        public void AllowsContinue_IsFalseForPlayerShortfallAndTeamOverflow()
        {
            Assert.False(Evaluate(ModIntegrityStatus.Match, true, TooFew).AllowsContinue);
            Assert.False(Evaluate(ModIntegrityStatus.Unavailable, true, TooFew).AllowsContinue);
            Assert.False(Evaluate(ModIntegrityStatus.Match, true, Enough, teamTotal: Enough).AllowsContinue);
            Assert.False(Evaluate(ModIntegrityStatus.Unavailable, true, Enough, teamTotal: Enough).AllowsContinue);
            Assert.True(Evaluate(ModIntegrityStatus.Unavailable, true, Enough).AllowsContinue);
            Assert.True(Evaluate(ModIntegrityStatus.Difference, true, Enough).AllowsContinue);
            Assert.True(Evaluate(ModIntegrityStatus.Match, true, 1, debugMode: true).AllowsContinue);
        }

        [Fact]
        public void Evaluate_TeamOverflow_RaisesCautionWithoutModIssue()
        {
            LobbyStartDecision decision = Evaluate(ModIntegrityStatus.Match, true, Enough, teamTotal: Enough);

            Assert.True(decision.HasTeamOverflow);
            Assert.Equal(Enough, decision.WerewolfCount);
            Assert.False(decision.HasPlayerShortfall);
            Assert.Equal(LobbyStartWarningKind.None, decision.ModWarningKind);
            Assert.Equal(LobbyStartWarningKind.Caution, decision.WarningKind);
            Assert.False(decision.AllowsContinue);
        }

        [Fact]
        public void Evaluate_TeamTotalBelowPlayerCount_ReportsNoOverflow()
        {
            LobbyStartDecision decision = Evaluate(ModIntegrityStatus.Match, true, Enough, teamTotal: Enough - 1);

            Assert.False(decision.HasTeamOverflow);
            Assert.Equal(LobbyStartWarningKind.None, decision.WarningKind);
            Assert.True(decision.AllowsContinue);
        }

        [Fact]
        public void Evaluate_TeamOverflow_DoesNotDowngradeSevereModWarning()
        {
            LobbyStartDecision decision = Evaluate(ModIntegrityStatus.Unavailable, true, Enough, teamTotal: Enough);

            Assert.True(decision.HasTeamOverflow);
            Assert.Equal(LobbyStartWarningKind.Severe, decision.ModWarningKind);
            Assert.Equal(LobbyStartWarningKind.Severe, decision.WarningKind);
        }

        [Fact]
        public void Evaluate_DebugMode_SuppressesTeamOverflow()
        {
            LobbyStartDecision decision = Evaluate(ModIntegrityStatus.Match, true, 1, teamTotal: 5, debugMode: true);

            Assert.False(decision.HasTeamOverflow);
            Assert.Equal(LobbyStartWarningKind.None, decision.WarningKind);
        }

        [Fact]
        public void Evaluate_ShortfallAndOverflow_ReportsBothIndependently()
        {
            LobbyStartDecision decision = Evaluate(ModIntegrityStatus.Match, true, TooFew, teamTotal: Enough);

            Assert.True(decision.HasPlayerShortfall);
            Assert.True(decision.HasTeamOverflow);
            Assert.Equal(LobbyStartWarningKind.Caution, decision.WarningKind);
            Assert.False(decision.AllowsContinue);
        }

        [Fact]
        public void Evaluate_PlayerShortfall_DoesNotDowngradeSevereModWarning()
        {
            LobbyStartDecision decision = Evaluate(ModIntegrityStatus.Unavailable, true, TooFew);

            Assert.True(decision.HasPlayerShortfall);
            Assert.Equal(LobbyStartWarningKind.Severe, decision.ModWarningKind);
            Assert.Equal(LobbyStartWarningKind.Severe, decision.WarningKind);
        }

        [Fact]
        public void Evaluate_EnoughPlayers_ReportsNoShortfall()
        {
            LobbyStartDecision decision = Evaluate(ModIntegrityStatus.Match, true, Enough);

            Assert.False(decision.HasPlayerShortfall);
            Assert.Equal(LobbyStartWarningKind.None, decision.WarningKind);
        }

        [Fact]
        public void Evaluate_DebugMode_SuppressesPlayerShortfall()
        {
            LobbyStartDecision decision = Evaluate(ModIntegrityStatus.Match, true, 1, debugMode: true);

            Assert.False(decision.HasPlayerShortfall);
            Assert.Equal(LobbyStartWarningKind.None, decision.WarningKind);
        }

        [Fact]
        public void PlayerCountGate_IsSatisfiedAtMinimumAndAboveOnly()
        {
            Assert.False(PlayerCountGate.IsSatisfied(TooFew, false));
            Assert.True(PlayerCountGate.IsSatisfied(Enough, false));
            Assert.True(PlayerCountGate.IsSatisfied(Enough + 1, false));
            Assert.True(PlayerCountGate.IsSatisfied(0, true));
        }

        [Fact]
        public void FinalCheck_HasFixedDeadlineAndRejectsDoubleBegin()
        {
            var attempt = new LobbyStartAttempt();
            Assert.True(attempt.TryBeginFinalCheck(true, 1000));
            Assert.Equal(2500, attempt.FinalCheckDeadlineUnixMs);
            Assert.False(attempt.TryBeginFinalCheck(true, 2000));
            Assert.False(attempt.ShouldCompleteFinalCheck(true, 2499));
            Assert.True(attempt.ShouldCompleteFinalCheck(true, 2500));
            Assert.False(attempt.IsFinalCheckPending);
        }

        [Fact]
        public void FinalCheck_CompletesEarlyWhenPendingResolves()
        {
            var attempt = new LobbyStartAttempt();
            attempt.TryBeginFinalCheck(true, 1000);
            Assert.True(attempt.ShouldCompleteFinalCheck(false, 1100));
        }

        [Fact]
        public void Bypass_IsOneShotRevisionIndependentAndBoundToLobbyHostAndLifetime()
        {
            var attempt = new LobbyStartAttempt();
            attempt.ArmOneShotBypass(123, 10, 1000);
            Assert.True(attempt.TryConsumeBypass(123, 10, 6000));
            Assert.False(attempt.TryConsumeBypass(123, 10, 6000));

            attempt.ArmOneShotBypass(123, 10, 1000);
            Assert.False(attempt.TryConsumeBypass(124, 10, 1001));
            attempt.ArmOneShotBypass(123, 10, 1000);
            Assert.False(attempt.TryConsumeBypass(123, 11, 1001));
            attempt.ArmOneShotBypass(123, 10, 1000);
            Assert.False(attempt.TryConsumeBypass(123, 10, 6001));
        }

        [Fact]
        public void Clear_RemovesHoldAndBypass()
        {
            var attempt = new LobbyStartAttempt();
            attempt.TryBeginFinalCheck(true, 1);
            attempt.ArmOneShotBypass(1, 2, 1);
            attempt.Clear();
            Assert.False(attempt.IsFinalCheckPending);
            Assert.False(attempt.TryConsumeBypass(1, 2, 2));
        }

        private static LobbyStartDecision Evaluate(
            ModIntegrityStatus participantStatus,
            bool baselineReady,
            int playerCount = Enough,
            int teamTotal = 1,
            bool debugMode = false)
        {
            return LobbyStartGate.Evaluate(
                Snapshot(participantStatus), baselineReady, playerCount, teamTotal, debugMode);
        }

        private static ModIntegritySnapshot Snapshot(ModIntegrityStatus participantStatus)
        {
            ModUnavailableReason reason = participantStatus == ModIntegrityStatus.Unavailable
                ? ModUnavailableReason.NoResponse : ModUnavailableReason.None;
            ModDifferenceSummary summary = participantStatus == ModIntegrityStatus.Difference
                ? new ModDifferenceSummary(1, 0, 0, 0) : default;
            var records = new[]
            {
                new ModParticipantRecord(10, ModIntegrityStatus.Baseline, ModUnavailableReason.None, default),
                new ModParticipantRecord(20, participantStatus, reason, summary),
            };
            Assert.True(ModIntegritySnapshot.TryCreate(1, 1, 10, records,
                out ModIntegritySnapshot snapshot, out string error), error);
            return snapshot;
        }
    }
}
