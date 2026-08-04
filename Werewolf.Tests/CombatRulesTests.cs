using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CombatRulesTests
    {

        [Theory]
        [InlineData(GamePhase.Lobby, false)]
        [InlineData(GamePhase.Play, true)]
        [InlineData(GamePhase.Meeting, true)]
        [InlineData(GamePhase.GameOver, false)]
        public void IsMatchLive_MatrixOfAllPhases(GamePhase phase, bool expected)
        {
            Assert.Equal(expected, CombatRules.IsMatchLive(phase));
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void OverrideSavingGrace_MatchLiveWithTrueInput_ForcesFalse(GamePhase phase)
        {
            Assert.False(CombatRules.OverrideSavingGrace(phase, true));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.GameOver)]
        public void OverrideSavingGrace_NotMatchLiveWithTrueInput_KeepsTrue(GamePhase phase)
        {
            Assert.True(CombatRules.OverrideSavingGrace(phase, true));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        [InlineData(GamePhase.GameOver)]
        public void OverrideSavingGrace_FalseInput_StaysFalseInAllPhases(GamePhase phase)
        {
            Assert.False(CombatRules.OverrideSavingGrace(phase, false));
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void OverrideMeleePlayerDamage_MatchLive_ReturnsEnemyDamage(GamePhase phase)
        {
            Assert.Equal(30, CombatRules.OverrideMeleePlayerDamage(phase, 0, 30));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.GameOver)]
        public void OverrideMeleePlayerDamage_NotMatchLive_KeepsRunLevelZero(GamePhase phase)
        {
            Assert.Equal(0, CombatRules.OverrideMeleePlayerDamage(phase, 0, 30));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.GameOver)]
        public void OverrideMeleePlayerDamage_NotMatchLive_KeepsArenaShopValue(GamePhase phase)
        {
            Assert.Equal(30, CombatRules.OverrideMeleePlayerDamage(phase, 30, 30));
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void OverrideMeleePlayerDamage_MatchLiveAlreadyEqual_IsNoOp(GamePhase phase)
        {
            Assert.Equal(30, CombatRules.OverrideMeleePlayerDamage(phase, 30, 30));
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void OverrideMeleePvpDurabilityHit_MatchLiveOnLevel_FlipsToDrainingPath(GamePhase phase)
        {
            Assert.False(CombatRules.OverrideMeleePvpDurabilityHit(phase, true, arenaOrShop: false));
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void OverrideMeleePvpDurabilityHit_ArenaOrShop_KeepsInput(GamePhase phase)
        {
            Assert.True(CombatRules.OverrideMeleePvpDurabilityHit(phase, true, arenaOrShop: true));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        [InlineData(GamePhase.GameOver)]
        public void OverrideMeleePvpDurabilityHit_EnemyHit_IsPassthroughInAllPhases(GamePhase phase)
        {
            Assert.False(CombatRules.OverrideMeleePvpDurabilityHit(phase, false, arenaOrShop: false));
            Assert.False(CombatRules.OverrideMeleePvpDurabilityHit(phase, false, arenaOrShop: true));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.GameOver)]
        public void OverrideMeleePvpDurabilityHit_NotMatchLive_KeepsVanillaBehaviour(GamePhase phase)
        {
            Assert.True(CombatRules.OverrideMeleePvpDurabilityHit(phase, true, arenaOrShop: false));
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void ShouldDisarmGrabbedOnMeleeHit_MatchLiveNonMelee_IsTrue(GamePhase phase)
        {
            Assert.True(CombatRules.ShouldDisarmGrabbedOnMeleeHit(phase, grabbing: true, grabbedIsMelee: false));
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void ShouldDisarmGrabbedOnMeleeHit_MatchLiveMelee_IsFalse(GamePhase phase)
        {
            Assert.False(CombatRules.ShouldDisarmGrabbedOnMeleeHit(phase, grabbing: true, grabbedIsMelee: true));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        [InlineData(GamePhase.GameOver)]
        public void ShouldDisarmGrabbedOnMeleeHit_NotGrabbing_IsFalseInAllPhases(GamePhase phase)
        {
            Assert.False(CombatRules.ShouldDisarmGrabbedOnMeleeHit(phase, grabbing: false, grabbedIsMelee: false));
        }

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.GameOver)]
        public void ShouldDisarmGrabbedOnMeleeHit_NotMatchLive_IsFalse(GamePhase phase)
        {
            Assert.False(CombatRules.ShouldDisarmGrabbedOnMeleeHit(phase, grabbing: true, grabbedIsMelee: false));
        }

        [Theory]
        [InlineData(GamePhase.Lobby, false)]
        [InlineData(GamePhase.Play, true)]
        [InlineData(GamePhase.Meeting, true)]
        [InlineData(GamePhase.GameOver, false)]
        public void ShouldSpillInventoryOnMeleeHit_MatrixOfAllPhases(GamePhase phase, bool expected)
        {
            Assert.Equal(expected, CombatRules.ShouldSpillInventoryOnMeleeHit(phase));
        }

        [Theory]
        [InlineData(GamePhase.Lobby, true, false)]
        [InlineData(GamePhase.Play, true, true)]
        [InlineData(GamePhase.Meeting, true, true)]
        [InlineData(GamePhase.GameOver, true, false)]
        [InlineData(GamePhase.Lobby, false, false)]
        [InlineData(GamePhase.Play, false, false)]
        [InlineData(GamePhase.Meeting, false, false)]
        [InlineData(GamePhase.GameOver, false, false)]
        public void ShouldBlockEquipContestedItem_MatrixOfPhasesAndContest(
            GamePhase phase, bool grabbedByOtherPlayer, bool expected)
        {
            Assert.Equal(expected, CombatRules.ShouldBlockEquipContestedItem(phase, grabbedByOtherPlayer));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PickSpillSlotIndex_NoOccupiedSlots_ReturnsMinusOne(int occupiedCount)
        {
            Assert.Equal(-1, CombatRules.PickSpillSlotIndex(occupiedCount, 0.5f));
        }

        [Theory]
        [InlineData(0.0f, 0)]
        [InlineData(0.32f, 0)]
        [InlineData(0.34f, 1)]
        [InlineData(0.66f, 1)]
        [InlineData(0.67f, 2)]
        [InlineData(0.99f, 2)]
        public void PickSpillSlotIndex_ThreeOccupied_SplitsRollEvenly(float roll, int expected)
        {
            Assert.Equal(expected, CombatRules.PickSpillSlotIndex(3, roll));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void PickSpillSlotIndex_RollAtUpperBound_ClampsToLastSlot(int occupiedCount)
        {
            Assert.Equal(occupiedCount - 1, CombatRules.PickSpillSlotIndex(occupiedCount, 1.0f));
        }

        [Fact]
        public void PickSpillSlotIndex_AnyRoll_StaysInRange()
        {
            for (int occupiedCount = 1; occupiedCount <= 3; occupiedCount++)
            {
                for (int step = 0; step <= 100; step++)
                {
                    int index = CombatRules.PickSpillSlotIndex(occupiedCount, step / 100f);
                    Assert.InRange(index, 0, occupiedCount - 1);
                }
            }
        }
    }
}
