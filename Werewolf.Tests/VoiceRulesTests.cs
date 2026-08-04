using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class VoiceRulesTests
    {

        [Fact]
        public void VoicePlanKind_ByteAssignments_MatchSpec()
        {
            Assert.Equal((byte)0, (byte)VoicePlanKind.None);
            Assert.Equal((byte)1, (byte)VoicePlanKind.ResultAll);
            Assert.Equal((byte)2, (byte)VoicePlanKind.Eavesdrop);
        }

        public static IEnumerable<object[]> AllDecideKindCombinations()
        {
            var phases = new[] { GamePhase.Lobby, GamePhase.Play, GamePhase.Meeting, GamePhase.GameOver };
            var roles = new Role?[] { Role.Werewolf, Role.Bomber, Role.Villager, Role.BlackCat, null };
            var alives = new[] { true, false };
            var modes = new[] { NecroVoiceMode.Off, NecroVoiceMode.NonWerewolfDead, NecroVoiceMode.AllDead };

            foreach (var phase in phases)
            foreach (var role in roles)
            foreach (var alive in alives)
            foreach (var mode in modes)
            {
                yield return new object[] { phase, role, alive, mode, ExpectedKind(phase, role, alive, mode) };
            }
        }

        private static VoicePlanKind ExpectedKind(GamePhase phase, Role? role, bool alive, NecroVoiceMode mode)
        {
            if (phase == GamePhase.GameOver)
            {
                return VoicePlanKind.ResultAll;
            }

            var isPlay = phase == GamePhase.Play;
            var isEavesdropRole = role == Role.Werewolf || role == Role.Bomber;
            var modeOn = mode != NecroVoiceMode.Off;

            if (isPlay && isEavesdropRole && alive && modeOn)
            {
                return VoicePlanKind.Eavesdrop;
            }

            return VoicePlanKind.None;
        }

        [Theory]
        [MemberData(nameof(AllDecideKindCombinations))]
        public void DecideKind_AllCombinations_MatchInvariants(
            GamePhase phase, Role? localRole, bool localAlive, NecroVoiceMode mode, VoicePlanKind expected)
        {
            Assert.Equal(expected, VoiceRules.DecideKind(phase, localRole, localAlive, mode));
        }

        [Fact]
        public void DecideKind_CoversAllCombinations_120Cases()
        {
            var count = 0;
            foreach (var _ in AllDecideKindCombinations())
            {
                count++;
            }
            Assert.Equal(4 * 5 * 2 * 3, count);
        }

        [Theory]
        [InlineData(NecroVoiceMode.Off)]
        [InlineData(NecroVoiceMode.NonWerewolfDead)]
        [InlineData(NecroVoiceMode.AllDead)]
        public void DecideKind_GameOver_IsResultAll_RegardlessOfMode(NecroVoiceMode mode)
        {
            foreach (var role in new Role?[] { Role.Werewolf, Role.Bomber, Role.Villager, Role.BlackCat, null })
            foreach (var alive in new[] { true, false })
            {
                Assert.Equal(
                    VoicePlanKind.ResultAll,
                    VoiceRules.DecideKind(GamePhase.GameOver, role, alive, mode));
            }
        }

        [Theory]
        [InlineData(NecroVoiceMode.NonWerewolfDead)]
        [InlineData(NecroVoiceMode.AllDead)]
        public void DecideKind_Play_AliveWerewolf_ModeOn_IsEavesdrop(NecroVoiceMode mode)
        {
            Assert.Equal(
                VoicePlanKind.Eavesdrop,
                VoiceRules.DecideKind(GamePhase.Play, Role.Werewolf, true, mode));
        }

        [Theory]
        [InlineData(NecroVoiceMode.NonWerewolfDead)]
        [InlineData(NecroVoiceMode.AllDead)]
        public void DecideKind_Play_AliveBomber_ModeOn_IsEavesdrop(NecroVoiceMode mode)
        {
            Assert.Equal(
                VoicePlanKind.Eavesdrop,
                VoiceRules.DecideKind(GamePhase.Play, Role.Bomber, true, mode));
        }

        [Fact]
        public void DecideKind_Play_AliveWerewolf_ModeOff_IsNone()
        {
            Assert.Equal(
                VoicePlanKind.None,
                VoiceRules.DecideKind(GamePhase.Play, Role.Werewolf, true, NecroVoiceMode.Off));
        }

        [Theory]
        [InlineData(NecroVoiceMode.Off)]
        [InlineData(NecroVoiceMode.NonWerewolfDead)]
        [InlineData(NecroVoiceMode.AllDead)]
        public void DecideKind_Meeting_IsNone_RegardlessOfOther(NecroVoiceMode mode)
        {
            foreach (var role in new Role?[] { Role.Werewolf, Role.Bomber, Role.Villager, Role.BlackCat, null })
            foreach (var alive in new[] { true, false })
            {
                Assert.Equal(
                    VoicePlanKind.None,
                    VoiceRules.DecideKind(GamePhase.Meeting, role, alive, mode));
            }
        }

        [Theory]
        [InlineData(NecroVoiceMode.Off)]
        [InlineData(NecroVoiceMode.NonWerewolfDead)]
        [InlineData(NecroVoiceMode.AllDead)]
        public void DecideKind_Lobby_IsNone_RegardlessOfOther(NecroVoiceMode mode)
        {
            foreach (var role in new Role?[] { Role.Werewolf, Role.Villager, Role.BlackCat, null })
            foreach (var alive in new[] { true, false })
            {
                Assert.Equal(
                    VoicePlanKind.None,
                    VoiceRules.DecideKind(GamePhase.Lobby, role, alive, mode));
            }
        }

        [Theory]
        [InlineData(Role.Villager)]
        [InlineData(Role.BlackCat)]
        [InlineData(null)]
        public void DecideKind_Play_NonWerewolfRoles_AreAlwaysNone(Role? role)
        {
            foreach (var alive in new[] { true, false })
            foreach (var mode in new[] { NecroVoiceMode.Off, NecroVoiceMode.NonWerewolfDead, NecroVoiceMode.AllDead })
            {
                Assert.Equal(
                    VoicePlanKind.None,
                    VoiceRules.DecideKind(GamePhase.Play, role, alive, mode));
            }
        }

        [Theory]
        [InlineData(NecroVoiceMode.Off)]
        [InlineData(NecroVoiceMode.NonWerewolfDead)]
        [InlineData(NecroVoiceMode.AllDead)]
        public void DecideKind_Play_DeadWerewolf_IsNone(NecroVoiceMode mode)
        {
            Assert.Equal(
                VoicePlanKind.None,
                VoiceRules.DecideKind(GamePhase.Play, Role.Werewolf, false, mode));
        }

        [Theory]
        [InlineData(NecroVoiceMode.Off, false)]
        [InlineData(NecroVoiceMode.Off, true)]
        [InlineData(NecroVoiceMode.NonWerewolfDead, false)]
        [InlineData(NecroVoiceMode.NonWerewolfDead, true)]
        [InlineData(NecroVoiceMode.AllDead, false)]
        [InlineData(NecroVoiceMode.AllDead, true)]
        public void IsEavesdropTarget_AliveTarget_IsAlwaysFalse(NecroVoiceMode mode, bool isKnownWerewolf)
        {
            Assert.False(VoiceRules.IsEavesdropTarget(
                targetDead: false, targetIsKnownWerewolf: isKnownWerewolf, mode: mode));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsEavesdropTarget_AllDead_DeadTarget_IsAlwaysTrue(bool isKnownWerewolf)
        {
            Assert.True(VoiceRules.IsEavesdropTarget(
                targetDead: true, targetIsKnownWerewolf: isKnownWerewolf, mode: NecroVoiceMode.AllDead));
        }

        [Fact]
        public void IsEavesdropTarget_NonWerewolfDead_KnownWerewolf_IsExcluded()
        {
            Assert.False(VoiceRules.IsEavesdropTarget(
                targetDead: true, targetIsKnownWerewolf: true, mode: NecroVoiceMode.NonWerewolfDead));
        }

        [Fact]
        public void IsEavesdropTarget_NonWerewolfDead_UnknownWerewolf_IsTarget_BlackCatSemantics()
        {
            Assert.True(VoiceRules.IsEavesdropTarget(
                targetDead: true, targetIsKnownWerewolf: false, mode: NecroVoiceMode.NonWerewolfDead));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsEavesdropTarget_Off_DeadTarget_IsFalse(bool isKnownWerewolf)
        {
            Assert.False(VoiceRules.IsEavesdropTarget(
                targetDead: true, targetIsKnownWerewolf: isKnownWerewolf, mode: NecroVoiceMode.Off));
        }

        [Theory]
        [InlineData(GamePhase.Lobby, true, false)]
        [InlineData(GamePhase.Lobby, false, false)]
        [InlineData(GamePhase.Play, true, true)]
        [InlineData(GamePhase.Play, false, false)]
        [InlineData(GamePhase.Meeting, true, true)]
        [InlineData(GamePhase.Meeting, false, false)]
        [InlineData(GamePhase.GameOver, true, false)]
        [InlineData(GamePhase.GameOver, false, false)]
        public void IsDeadCueMuteActive_OnlyWhileLocalAliveInPlayOrMeeting(
            GamePhase phase, bool localAlive, bool expected)
        {
            Assert.Equal(expected, VoiceRules.IsDeadCueMuteActive(phase, localAlive));
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void IsDeadCueMuteTarget_DeadAndNotAudibleOnly(
            bool targetDead, bool eavesdropAudible, bool expected)
        {
            Assert.Equal(expected, VoiceRules.IsDeadCueMuteTarget(targetDead, eavesdropAudible));
        }

        [Fact]
        public void IsEavesdropTarget_LoneWerewolf_AllDeadPlayersAreTargets_NonWerewolfDead()
        {
            var deadRoles = new Role?[] { Role.Villager, Role.BlackCat, Role.Werewolf, null };
            foreach (var _ in deadRoles)
            {
                Assert.True(VoiceRules.IsEavesdropTarget(
                    targetDead: true, targetIsKnownWerewolf: false, mode: NecroVoiceMode.NonWerewolfDead));
            }
        }

        [Theory]
        [InlineData(GamePhase.Play, true)]
        [InlineData(GamePhase.Play, false)]
        public void Matrix_Play_AliveSpeaker_CuesAndTextVisibleToEveryone(GamePhase phase, bool localAlive)
        {
            Assert.True(VoiceRules.ShouldShowDeadCues(
                phase, localAlive, speakerDead: false, speakerEavesdropAudible: false));
            Assert.True(VoiceRules.ShouldShowDeadText(
                phase, localAlive, speakerDead: false));
        }

        [Fact]
        public void Matrix_Play_DeadSpeaker_ObserverAliveVillager_HidesCuesAndText()
        {
            Assert.False(VoiceRules.ShouldShowDeadCues(
                GamePhase.Play, localAlive: true, speakerDead: true, speakerEavesdropAudible: false));
            Assert.False(VoiceRules.ShouldShowDeadText(
                GamePhase.Play, localAlive: true, speakerDead: true));
        }

        [Fact]
        public void Matrix_Play_DeadSpeaker_ObserverAliveWerewolf_NonWerewolfDeadMode_AudibleTarget_ShowsCuesAndFilter()
        {
            bool audible = VoiceRules.IsEavesdropTarget(
                targetDead: true, targetIsKnownWerewolf: false, mode: NecroVoiceMode.NonWerewolfDead);
            Assert.True(audible);
            Assert.True(VoiceRules.ShouldShowDeadCues(
                GamePhase.Play, localAlive: true, speakerDead: true, speakerEavesdropAudible: audible));
            Assert.True(VoiceRules.ShouldApplyNecroFilter(VoicePlanKind.Eavesdrop, audible));
            Assert.False(VoiceRules.ShouldShowDeadText(
                GamePhase.Play, localAlive: true, speakerDead: true));
        }

        [Fact]
        public void Matrix_Play_DeadSpeaker_ObserverAliveWerewolf_NonWerewolfDeadMode_ExcludedTarget_HidesCues()
        {
            bool audible = VoiceRules.IsEavesdropTarget(
                targetDead: true, targetIsKnownWerewolf: true, mode: NecroVoiceMode.NonWerewolfDead);
            Assert.False(audible);
            Assert.False(VoiceRules.ShouldShowDeadCues(
                GamePhase.Play, localAlive: true, speakerDead: true, speakerEavesdropAudible: audible));
            Assert.False(VoiceRules.ShouldApplyNecroFilter(VoicePlanKind.Eavesdrop, audible));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Matrix_Play_DeadSpeaker_ObserverAliveWerewolf_AllDeadMode_AudibleTarget_ShowsCuesAndFilter(
            bool isKnownWerewolf)
        {
            bool audible = VoiceRules.IsEavesdropTarget(
                targetDead: true, targetIsKnownWerewolf: isKnownWerewolf, mode: NecroVoiceMode.AllDead);
            Assert.True(audible);
            Assert.True(VoiceRules.ShouldShowDeadCues(
                GamePhase.Play, localAlive: true, speakerDead: true, speakerEavesdropAudible: audible));
            Assert.True(VoiceRules.ShouldApplyNecroFilter(VoicePlanKind.Eavesdrop, audible));
        }

        [Fact]
        public void Matrix_Play_DeadSpeaker_ObserverAliveWerewolf_OffMode_HidesCues()
        {
            bool audible = VoiceRules.IsEavesdropTarget(
                targetDead: true, targetIsKnownWerewolf: false, mode: NecroVoiceMode.Off);
            Assert.False(audible);
            Assert.False(VoiceRules.ShouldShowDeadCues(
                GamePhase.Play, localAlive: true, speakerDead: true, speakerEavesdropAudible: audible));
        }

        [Fact]
        public void Matrix_Play_DeadSpeaker_ObserverDead_ShowsCuesAndText_NoFilter()
        {
            Assert.True(VoiceRules.ShouldShowDeadCues(
                GamePhase.Play, localAlive: false, speakerDead: true, speakerEavesdropAudible: false));
            Assert.True(VoiceRules.ShouldShowDeadText(
                GamePhase.Play, localAlive: false, speakerDead: true));
            Assert.False(VoiceRules.ShouldApplyNecroFilter(VoicePlanKind.None, targetEavesdropAudible: false));
        }

        [Fact]
        public void Matrix_Meeting_DeadSpeaker_ObserverAliveVillager_HidesCuesAndText()
        {
            Assert.False(VoiceRules.ShouldShowDeadCues(
                GamePhase.Meeting, localAlive: true, speakerDead: true, speakerEavesdropAudible: false));
            Assert.False(VoiceRules.ShouldShowDeadText(
                GamePhase.Meeting, localAlive: true, speakerDead: true));
        }

        [Theory]
        [InlineData(NecroVoiceMode.NonWerewolfDead)]
        [InlineData(NecroVoiceMode.AllDead)]
        public void Matrix_Meeting_DeadSpeaker_ObserverAliveWerewolf_HidesCues_EavesdropSuspended(NecroVoiceMode mode)
        {
            var plan = VoiceRules.DecideKind(GamePhase.Meeting, Role.Werewolf, localAlive: true, mode);
            Assert.Equal(VoicePlanKind.None, plan);
            Assert.False(VoiceRules.ShouldShowDeadCues(
                GamePhase.Meeting, localAlive: true, speakerDead: true, speakerEavesdropAudible: false));
            Assert.False(VoiceRules.ShouldShowDeadText(
                GamePhase.Meeting, localAlive: true, speakerDead: true));
            Assert.False(VoiceRules.ShouldApplyNecroFilter(plan, targetEavesdropAudible: false));
        }

        [Fact]
        public void Matrix_Meeting_DeadSpeaker_ObserverDead_ShowsCuesAndText()
        {
            Assert.True(VoiceRules.ShouldShowDeadCues(
                GamePhase.Meeting, localAlive: false, speakerDead: true, speakerEavesdropAudible: false));
            Assert.True(VoiceRules.ShouldShowDeadText(
                GamePhase.Meeting, localAlive: false, speakerDead: true));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void Matrix_GameOver_AllVisibleAndAudible_NoFilter(bool localAlive, bool speakerDead)
        {
            Assert.True(VoiceRules.ShouldShowDeadCues(
                GamePhase.GameOver, localAlive, speakerDead, speakerEavesdropAudible: false));
            Assert.True(VoiceRules.ShouldShowDeadText(
                GamePhase.GameOver, localAlive, speakerDead));
            Assert.False(VoiceRules.ShouldApplyNecroFilter(VoicePlanKind.ResultAll, targetEavesdropAudible: false));
            Assert.False(VoiceRules.ShouldApplyNecroFilter(VoicePlanKind.ResultAll, targetEavesdropAudible: true));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Matrix_Lobby_AliveSpeaker_CuesAndTextVisible(bool localAlive)
        {
            Assert.True(VoiceRules.ShouldShowDeadCues(
                GamePhase.Lobby, localAlive, speakerDead: false, speakerEavesdropAudible: false));
            Assert.True(VoiceRules.ShouldShowDeadText(
                GamePhase.Lobby, localAlive, speakerDead: false));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Matrix_Lobby_DeadSpeaker_TextVisible_RoundOutside(bool localAlive)
        {
            Assert.True(VoiceRules.ShouldShowDeadText(
                GamePhase.Lobby, localAlive, speakerDead: true));
        }

        [Theory]
        [InlineData(VoicePlanKind.None, false, false)]
        [InlineData(VoicePlanKind.None, true, false)]
        [InlineData(VoicePlanKind.ResultAll, false, false)]
        [InlineData(VoicePlanKind.ResultAll, true, false)]
        [InlineData(VoicePlanKind.Eavesdrop, false, false)]
        [InlineData(VoicePlanKind.Eavesdrop, true, true)]
        public void ShouldApplyNecroFilter_OnlyEavesdropAndAudible(
            VoicePlanKind plan, bool audible, bool expected)
        {
            Assert.Equal(expected, VoiceRules.ShouldApplyNecroFilter(plan, audible));
        }

        [Theory]
        [InlineData(GamePhase.Lobby, true)]
        [InlineData(GamePhase.Lobby, false)]
        [InlineData(GamePhase.Play, true)]
        [InlineData(GamePhase.Play, false)]
        [InlineData(GamePhase.Meeting, true)]
        [InlineData(GamePhase.Meeting, false)]
        [InlineData(GamePhase.GameOver, true)]
        [InlineData(GamePhase.GameOver, false)]
        public void ShouldShowDeadCues_AliveSpeaker_AlwaysTrue(GamePhase phase, bool localAlive)
        {
            Assert.True(VoiceRules.ShouldShowDeadCues(
                phase, localAlive, speakerDead: false, speakerEavesdropAudible: false));
            Assert.True(VoiceRules.ShouldShowDeadCues(
                phase, localAlive, speakerDead: false, speakerEavesdropAudible: true));
            Assert.True(VoiceRules.ShouldShowDeadText(
                phase, localAlive, speakerDead: false));
        }
    }
}
