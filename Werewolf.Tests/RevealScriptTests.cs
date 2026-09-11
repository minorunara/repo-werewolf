using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class RevealScriptTests
    {
        private static string[] AllLines(RevealContent content) =>
            content.Pages.SelectMany(p => p.BodyLines).ToArray();

        private static RevealContent Build(Role role, string[] teammates, bool blackCatPossible,
            ValuableMapMode valuableMapMode = ValuableMapMode.MeetingSync) =>
            RevealScript.Build(role, teammates, blackCatPossible, valuableMapMode);

        private static string T(TextId id) => Texts.Get(id);

        private static string B(TextId id) => "・" + Texts.Get(id);

        private static string TeammatePrefix => Texts.Get(TextId.RevealTeammatePrefix);

        [Fact]
        public void Villager_WithoutCatPossible_ShowsWinConditionsThenTips()
        {
            var content = Build(Role.Villager, new string[0], blackCatPossible: false);

            Assert.Equal(T(TextId.RevealVillagerTitle), content.Title);
            Assert.Equal(RoleIcon.Villager, content.Icon);
            Assert.Equal(2, content.Pages.Length);

            var page1 = content.Pages[0].BodyLines;
            Assert.Contains(T(TextId.RevealHeadingWinCondition), page1);
            Assert.Contains(B(TextId.RevealVillagerWinCondition1), page1);
            Assert.Contains(B(TextId.RevealVillagerWinCondition2), page1);
            Assert.DoesNotContain(T(TextId.RevealHeadingTips), page1);

            var page2 = content.Pages[1].BodyLines;
            Assert.Contains(T(TextId.RevealHeadingTips), page2);
            Assert.Contains(B(TextId.RevealVillagerTipConvene), page2);
            Assert.Contains(B(TextId.RevealVillagerTipReport), page2);
            Assert.Contains(T(TextId.RevealVillagerFlavor), page2);
        }

        [Fact]
        public void Villager_TipThree_FollowsValuableMapMode()
        {
            var meetingSync = Build(Role.Villager, new string[0], false, ValuableMapMode.MeetingSync);
            Assert.Contains(B(TextId.RevealVillagerTipValuableMap), AllLines(meetingSync));
            Assert.DoesNotContain(B(TextId.RevealVillagerTipAliveCheck), AllLines(meetingSync));

            foreach (var mode in new[] { ValuableMapMode.Realtime, ValuableMapMode.Hidden })
            {
                var content = Build(Role.Villager, new string[0], false, mode);
                Assert.DoesNotContain(B(TextId.RevealVillagerTipValuableMap), AllLines(content));
                Assert.Contains(B(TextId.RevealVillagerTipAliveCheck), AllLines(content));
            }
        }

        [Fact]
        public void Villager_WithCatPossible_ShowsMaybeVillagerText()
        {
            var content = Build(Role.Villager, new string[0], blackCatPossible: true);

            Assert.Equal(T(TextId.RevealVillagerTitleMaybeCat), content.Title);
            Assert.NotEqual(T(TextId.RevealVillagerTitle), content.Title);
            Assert.Equal(RoleIcon.Villager, content.Icon);
        }

        [Fact]
        public void Villager_CatPossibleFlag_DoesNotAffectWerewolfOrBlackCat()
        {
            var wolf = Build(Role.Werewolf, new[] { "A" }, blackCatPossible: true);
            var cat = Build(Role.BlackCat, new string[0], blackCatPossible: true);

            Assert.Equal(T(TextId.RevealWerewolfTitle), wolf.Title);
            Assert.Equal(T(TextId.RevealBlackCatTitle), cat.Title);
        }

        [Fact]
        public void Werewolf_ShowsTwoPages_TeamThenAbilities()
        {
            var content = Build(Role.Werewolf, new[] { "Alice", "Bob" }, blackCatPossible: false);

            Assert.Equal(T(TextId.RevealWerewolfTitle), content.Title);
            Assert.Equal(RoleIcon.Werewolf, content.Icon);
            Assert.Equal(2, content.Pages.Length);

            var page1 = content.Pages[0].BodyLines;
            Assert.Contains(page1, l => l.StartsWith(TeammatePrefix));
            Assert.Contains(T(TextId.RevealHeadingWinCondition), page1);
            Assert.Contains(B(TextId.RevealWolfTeamWinCondition1), page1);
            Assert.Contains(B(TextId.RevealWolfTeamWinCondition2), page1);
            Assert.Contains(B(TextId.RevealWolfTeamWinCondition3), page1);

            var page2 = content.Pages[1].BodyLines;
            Assert.Contains(T(TextId.RevealHeadingAbility), page2);
            Assert.Contains(B(TextId.RevealWolfAbility1), page2);
            Assert.Contains(B(TextId.RevealWolfAbility2), page2);
            Assert.Contains(B(TextId.RevealWolfAbility3), page2);
            Assert.DoesNotContain(page2, l => l.Contains("Alice") || l.Contains("Bob"));
        }

        [Fact]
        public void Werewolf_TeammateLine_ListsAllNamesWithPrefixAndSeparator()
        {
            var content = Build(Role.Werewolf, new[] { "Alice", "Bob" }, blackCatPossible: false);

            var teammateLines = content.Pages[0].BodyLines.Where(l => l.StartsWith(TeammatePrefix)).ToArray();
            Assert.Single(teammateLines);
            Assert.Equal(TeammatePrefix + "Alice,Bob", teammateLines[0]);
        }

        [Fact]
        public void Werewolf_WithNoTeammates_StillShowsPrefixLine()
        {
            var content = Build(Role.Werewolf, new string[0], blackCatPossible: false);

            var teammateLines = AllLines(content).Where(l => l.StartsWith(TeammatePrefix)).ToArray();
            Assert.Single(teammateLines);
            Assert.Equal(TeammatePrefix, teammateLines[0]);
        }

        [Fact]
        public void Werewolf_ManyTeammates_WrapsAcrossMultipleLines()
        {
            var teammates = new[] { "AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "EEEEEEEE" };
            var content = Build(Role.Werewolf, teammates, blackCatPossible: false);

            var teammateLines = content.Pages[0].BodyLines
                .Where(l => l.StartsWith(TeammatePrefix) || IsWrappedContinuationLine(l, teammates))
                .ToArray();

            Assert.True(teammateLines.Length > 1, "1行に収まらない仲間列挙は複数行に折返されるべき");

            string joined = string.Join("", teammateLines);
            foreach (var name in teammates)
            {
                Assert.Contains(name, joined);
            }
        }

        [Fact]
        public void Werewolf_WrappedLine_NeverExceedsSoftLimit()
        {
            var teammates = new[] { "AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "EEEEEEEE" };
            var content = Build(Role.Werewolf, teammates, blackCatPossible: false);

            foreach (var line in AllLines(content))
            {
                if (line.StartsWith(TeammatePrefix) || teammates.Any(line.Contains))
                {
                    Assert.True(line.Length <= 30, $"行が長すぎる: '{line}' ({line.Length}文字)");
                }
            }
        }

        [Fact]
        public void Werewolf_IdPrefixedTeammates_KeepEveryNameAcrossWrap()
        {
            var teammates = new[]
            {
                ParticipantLabel.Format(3, "Alice"),
                ParticipantLabel.Format(12, "Bob"),
                ParticipantLabel.Format(7, "Charlie"),
            };
            var content = Build(Role.Werewolf, teammates, blackCatPossible: false);

            string joined = string.Join("", content.Pages[0].BodyLines);
            Assert.Contains("3. Alice", joined);
            Assert.Contains("12. Bob", joined);
            Assert.Contains("7. Charlie", joined);
        }

        [Fact]
        public void SelfId_WhenRosterKnown_BuildsNameplateLineForEveryRole()
        {
            foreach (var role in new[] { Role.Villager, Role.Werewolf, Role.Bomber, Role.BlackCat, Role.Shaman })
            {
                var content = RevealScript.Build(role, new[] { "Alice" }, blackCatPossible: false,
                    ValuableMapMode.MeetingSync, blackCatCurseEnabled: true, selfParticipantId: 7);

                Assert.Equal(Texts.Format(TextId.RevealSelfIdFormat, 7), content.SelfIdLine);

                Assert.DoesNotContain(content.SelfIdLine, AllLines(content));
            }
        }

        [Fact]
        public void SelfId_WhenRosterMissing_LeavesNameplateEmpty()
        {
            var content = Build(Role.Werewolf, new[] { "Alice" }, blackCatPossible: false);
            Assert.True(string.IsNullOrEmpty(content.SelfIdLine));
        }

        [Fact]
        public void BlackCatAwakening_HasNoSelfIdLine()
        {
            var awakening = RevealScript.BuildBlackCatAwakening();
            Assert.True(string.IsNullOrEmpty(awakening.SelfIdLine));
        }

        [Fact]
        public void Bomber_ShowsTwoPages_WithBomberAbilities()
        {
            var content = Build(Role.Bomber, new[] { "Alice" }, blackCatPossible: false);

            Assert.Equal(RoleIcon.Bomber, content.Icon);
            Assert.Equal(2, content.Pages.Length);

            var page1 = content.Pages[0].BodyLines;
            Assert.Contains(page1, l => l.StartsWith(TeammatePrefix));
            Assert.Contains(B(TextId.RevealWolfTeamWinCondition3), page1);

            var page2 = content.Pages[1].BodyLines;
            Assert.Contains(T(TextId.RevealHeadingAbility), page2);
            Assert.Contains(B(TextId.RevealBomberAbility1), page2);
            Assert.Contains(B(TextId.RevealBomberAbility2), page2);
            Assert.Contains(B(TextId.RevealBomberAbility3), page2);
        }

        [Fact]
        public void BlackCat_CurseDisabled_ReplacesAbilityTextWithHiddenAlliance()
        {
            var content = RevealScript.Build(Role.BlackCat, new string[0], blackCatPossible: true,
                ValuableMapMode.MeetingSync, blackCatCurseEnabled: false);

            var lines = AllLines(content);
            Assert.Contains(B(TextId.RevealBlackCatNoCurse), lines);
            Assert.DoesNotContain(B(TextId.RevealBlackCatAbility), lines);

            var awakening = RevealScript.BuildBlackCatAwakening(blackCatCurseEnabled: false);
            Assert.Equal(T(TextId.RevealBlackCatNoCurse), awakening.Pages[0].BodyLines[0]);
        }

        [Fact]
        public void BlackCat_ShowsTwoPages_WinConditionsThenAbility_WithoutTeammateList()
        {
            var content = Build(Role.BlackCat, new[] { "Alice", "Bob" }, blackCatPossible: false);

            Assert.Equal(T(TextId.RevealBlackCatTitle), content.Title);
            Assert.Equal(RoleIcon.BlackCat, content.Icon);
            Assert.Equal(2, content.Pages.Length);

            var page1 = content.Pages[0].BodyLines;
            Assert.Contains(T(TextId.RevealHeadingWinCondition), page1);
            Assert.Contains(B(TextId.RevealWolfTeamWinCondition2), page1);
            Assert.Contains(B(TextId.RevealWolfTeamWinCondition3), page1);
            Assert.DoesNotContain(T(TextId.RevealHeadingAbility), page1);

            var page2 = content.Pages[1].BodyLines;
            Assert.Contains(T(TextId.RevealHeadingAbility), page2);
            Assert.Contains(B(TextId.RevealBlackCatAbility), page2);

            var lines = AllLines(content);
            Assert.DoesNotContain(lines, l => l.StartsWith(TeammatePrefix));
            Assert.DoesNotContain(lines, l => l.Contains("Alice"));
            Assert.DoesNotContain(lines, l => l.Contains("Bob"));
        }

        [Fact]
        public void BlackCatAwakening_ShowsPastTenseTitle_WithAbilityLineOnly()
        {
            var awakening = RevealScript.BuildBlackCatAwakening();

            Assert.Equal(T(TextId.RevealBlackCatAwakeningTitle), awakening.Title);
            Assert.NotEqual(T(TextId.RevealBlackCatTitle), awakening.Title);
            Assert.Equal(RoleIcon.BlackCat, awakening.Icon);

            Assert.Single(awakening.Pages);
            Assert.Equal(new[] { T(TextId.RevealBlackCatAbility) }, AllLines(awakening));
        }

        private static bool IsWrappedContinuationLine(string line, string[] teammates)
        {
            return teammates.Any(line.Contains) && !line.StartsWith(TeammatePrefix);
        }
    }
}
