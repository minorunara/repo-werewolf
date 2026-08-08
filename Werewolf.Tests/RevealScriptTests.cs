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

        [Fact]
        public void Villager_WithoutCatPossible_ShowsWinConditionsThenTips()
        {
            var content = Build(Role.Villager, new string[0], blackCatPossible: false);

            Assert.Equal("あなたは村人です", content.Title);
            Assert.Equal(RoleIcon.Villager, content.Icon);
            Assert.Equal(2, content.Pages.Length);

            var page1 = content.Pages[0].BodyLines;
            Assert.Contains("◆ 勝利条件（いずれか）", page1);
            Assert.Contains("・全ての抽出を完了し、トラックを発車させる", page1);
            Assert.Contains("・人狼陣営を全滅させる", page1);
            Assert.DoesNotContain(page1, l => l.Contains("ヒント"));

            var page2 = content.Pages[1].BodyLines;
            Assert.Contains("◆ ヒント", page2);
            Assert.Contains("・トラック最奥の赤いボタン長押しで会議を開ける", page2);
            Assert.Contains("・死体の近くで通報キーを押すと会議を開ける", page2);
            Assert.Contains("特別な能力はない。貴重品の回収と会議で人狼に立ち向かえ。", page2);
        }

        [Fact]
        public void Villager_TipThree_FollowsValuableMapMode()
        {
            var meetingSync = Build(Role.Villager, new string[0], false, ValuableMapMode.MeetingSync);
            Assert.Contains("・会議のたびにマップの貴重品情報が更新される", AllLines(meetingSync));
            Assert.DoesNotContain("・会議では全プレイヤーの生死を確認できる", AllLines(meetingSync));

            foreach (var mode in new[] { ValuableMapMode.Realtime, ValuableMapMode.Hidden })
            {
                var content = Build(Role.Villager, new string[0], false, mode);
                Assert.DoesNotContain("・会議のたびにマップの貴重品情報が更新される", AllLines(content));
                Assert.Contains("・会議では全プレイヤーの生死を確認できる", AllLines(content));
            }
        }

        [Fact]
        public void Villager_WithCatPossible_ShowsMaybeVillagerText()
        {
            var content = Build(Role.Villager, new string[0], blackCatPossible: true);

            Assert.Equal("あなたは村人…かもしれません。", content.Title);
            Assert.Equal(RoleIcon.Villager, content.Icon);
        }

        [Fact]
        public void Villager_CatPossibleFlag_DoesNotAffectWerewolfOrBlackCat()
        {
            var wolf = Build(Role.Werewolf, new[] { "A" }, blackCatPossible: true);
            var cat = Build(Role.BlackCat, new string[0], blackCatPossible: true);

            Assert.Equal("あなたは人狼です", wolf.Title);
            Assert.Equal("あなたは黒猫です", cat.Title);
        }

        [Fact]
        public void Werewolf_ShowsTwoPages_TeamThenAbilities()
        {
            var content = Build(Role.Werewolf, new[] { "Alice", "Bob" }, blackCatPossible: false);

            Assert.Equal("あなたは人狼です", content.Title);
            Assert.Equal(RoleIcon.Werewolf, content.Icon);
            Assert.Equal(2, content.Pages.Length);

            var page1 = content.Pages[0].BodyLines;
            Assert.Contains("◆ 勝利条件（いずれか）", page1);
            Assert.Contains("・その時点で存在する貴重品を全て集めても、最後の抽出を完了できない状態にする", page1);
            Assert.Contains("・時間切れまでトラックを発車させない", page1);
            Assert.Contains("・村人陣営を全滅させる", page1);

            var page2 = content.Pages[1].BodyLines;
            Assert.Contains("◆ あなたの能力", page2);
            Assert.Contains("・貴重品を壊すほどバフがかかる", page2);
            Assert.Contains("・マップで敵の位置が分かる", page2);
            Assert.Contains("・ビーコンで敵を今いる場所におびき寄せる", page2);
            Assert.DoesNotContain(page2, l => l.Contains("Alice") || l.Contains("Bob"));
        }

        [Fact]
        public void Werewolf_TeammateLine_ListsAllNamesWithPrefixAndSeparator()
        {
            var content = Build(Role.Werewolf, new[] { "Alice", "Bob" }, blackCatPossible: false);

            var teammateLines = content.Pages[0].BodyLines.Where(l => l.StartsWith("人狼仲間：")).ToArray();
            Assert.Single(teammateLines);
            Assert.Equal("人狼仲間：Alice,Bob", teammateLines[0]);
        }

        [Fact]
        public void Werewolf_WithNoTeammates_StillShowsPrefixLine()
        {
            var content = Build(Role.Werewolf, new string[0], blackCatPossible: false);

            var teammateLines = AllLines(content).Where(l => l.StartsWith("人狼仲間：")).ToArray();
            Assert.Single(teammateLines);
            Assert.Equal("人狼仲間：", teammateLines[0]);
        }

        [Fact]
        public void Werewolf_ManyTeammates_WrapsAcrossMultipleLines()
        {
            var teammates = new[] { "AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "EEEEEEEE" };
            var content = Build(Role.Werewolf, teammates, blackCatPossible: false);

            var teammateLines = content.Pages[0].BodyLines
                .Where(l => l.StartsWith("人狼仲間：") || IsWrappedContinuationLine(l, teammates))
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
                if (line.StartsWith("人狼仲間：") || teammates.Any(line.Contains))
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

                Assert.Equal("あなたの識別番号はNo.7", content.SelfIdLine);

                Assert.DoesNotContain(AllLines(content), l => l.Contains("識別番号"));
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
            Assert.Contains(page1, l => l.StartsWith("人狼仲間："));
            Assert.Contains("・村人陣営を全滅させる", page1);

            var page2 = content.Pages[1].BodyLines;
            Assert.Contains("◆ あなたの能力", page2);
            Assert.Contains("・十分な時間近くで過ごした他のプレイヤーを爆弾に変えられる", page2);
            Assert.Contains("・好きなタイミングで起爆し、周囲を破壊する。爆弾にされた本人もダメージを受けるが、HP1で耐える", page2);
            Assert.Contains("・その爆発に巻き込まれると自分が即死する", page2);
        }

        [Fact]
        public void BlackCat_CurseDisabled_ReplacesAbilityTextWithHiddenAlliance()
        {
            var content = RevealScript.Build(Role.BlackCat, new string[0], blackCatPossible: true,
                ValuableMapMode.MeetingSync, blackCatCurseEnabled: false);

            string body = string.Join("\n", content.Pages.SelectMany(p => p.BodyLines));
            Assert.Contains("互いに正体を知らされない", body);
            Assert.DoesNotContain("道連れ", body);

            var awakening = RevealScript.BuildBlackCatAwakening(blackCatCurseEnabled: false);
            Assert.Contains("互いに正体を知らされない", awakening.Pages[0].BodyLines[0]);
            Assert.DoesNotContain("道連れ", awakening.Pages[0].BodyLines[0]);
        }

        [Fact]
        public void BlackCat_ShowsTwoPages_WinConditionsThenAbility_WithoutTeammateList()
        {
            var content = Build(Role.BlackCat, new[] { "Alice", "Bob" }, blackCatPossible: false);

            Assert.Equal("あなたは黒猫です", content.Title);
            Assert.Equal(RoleIcon.BlackCat, content.Icon);
            Assert.Equal(2, content.Pages.Length);

            var page1 = content.Pages[0].BodyLines;
            Assert.Contains("・村人陣営を全滅させる", page1);
            Assert.Contains("・時間切れまでトラックを発車させない", page1);
            Assert.DoesNotContain(page1, l => l.Contains("あなたの能力"));

            var page2 = content.Pages[1].BodyLines;
            Assert.Contains("◆ あなたの能力", page2);
            Assert.Contains("・処刑対象に選ばれると、自分に投票したプレイヤーのうち1人を道連れに死亡させる", page2);

            var lines = AllLines(content);
            Assert.DoesNotContain(lines, l => l.Contains("人狼仲間"));
            Assert.DoesNotContain(lines, l => l.Contains("Alice"));
            Assert.DoesNotContain(lines, l => l.Contains("Bob"));
        }

        [Fact]
        public void BlackCatAwakening_ShowsPastTenseTitle_WithAbilityLineOnly()
        {
            var awakening = RevealScript.BuildBlackCatAwakening();

            Assert.Equal("あなたは黒猫でした。", awakening.Title);
            Assert.Equal(RoleIcon.BlackCat, awakening.Icon);

            Assert.Single(awakening.Pages);
            var lines = AllLines(awakening);
            Assert.Equal(
                new[]
                {
                    "処刑対象に選ばれると、自分に投票したプレイヤーのうち1人を道連れに死亡させる",
                },
                lines);
            Assert.DoesNotContain(lines, l => l.Contains("人狼陣営"));
            Assert.DoesNotContain(lines, l => l.Contains("人狼仲間"));
            Assert.DoesNotContain(lines, l => l.Contains("勝利条件"));
        }

        [Fact]
        public void Output_NeverContainsOtherRoleLabels()
        {
            var villager = Build(Role.Villager, new string[0], blackCatPossible: false);
            var werewolf = Build(Role.Werewolf, new[] { "Alice" }, blackCatPossible: false);
            var blackCat = Build(Role.BlackCat, new[] { "Alice" }, blackCatPossible: false);

            foreach (var content in new[] { villager, werewolf, blackCat })
            {
                foreach (var line in AllLines(content))
                {
                    Assert.DoesNotContain("村人です", line);
                    Assert.DoesNotContain("黒猫です", line);
                }
                Assert.DoesNotContain("人狼です", content.Title == "あなたは人狼です" ? "" : content.Title);
            }
        }

        private static bool IsWrappedContinuationLine(string line, string[] teammates)
        {
            return teammates.Any(line.Contains) && !line.StartsWith("人狼仲間：");
        }
    }
}
