using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioDefinitionContractTests
    {
        public static IEnumerable<object[]> Implemented
            => ScenarioCatalog.Entries.Where(e => e.Implemented).Select(e => new object[] { e.Id });

        private static ScenarioDefinition Def(ScenarioId id)
        {
            Assert.True(ScenarioCatalog.TryGetDefinition(id, out var definition),
                $"{id} は実装済みなのに定義が無い");
            return definition;
        }

        [Fact]
        public void Catalog_ListsEveryScenarioIdExactlyOnce()
        {
            var entries = ScenarioCatalog.Entries;

            Assert.Equal(
                System.Enum.GetValues(typeof(ScenarioId)).Cast<ScenarioId>().OrderBy(id => id),
                entries.Select(e => e.Id).OrderBy(id => id));
        }

        [Fact]
        public void Catalog_EveryEntryHasNameAndIcon()
        {
            foreach (var entry in ScenarioCatalog.Entries)
            {
                Assert.False(string.IsNullOrEmpty(Texts.Get(entry.NameId)));
                Assert.False(string.IsNullOrEmpty(entry.IconKey));
                if (entry.Implemented) Assert.True(entry.DescriptionId.HasValue);
            }
        }

        [Fact]
        public void Catalog_OnlyImplementedEntriesCanStartAndHaveDefinitions()
        {
            foreach (var entry in ScenarioCatalog.Entries)
            {
                Assert.Equal(entry.Implemented, ScenarioCatalog.CanStart(entry.Id));
                Assert.Equal(entry.Implemented, ScenarioCatalog.TryGetDefinition(entry.Id, out _));
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Definition_KnowsItsOwnId(ScenarioId id) => Assert.Equal(id, Def(id).Id);

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Cast_BotIdsAndActorsAreDistinctAndDoNotClashWithThePlayer(ScenarioId id)
        {
            ScenarioCast cast = Def(id).Cast;
            var bots = cast.Bots;

            Assert.NotEmpty(bots);
            Assert.Equal(bots.Count, bots.Select(b => b.ParticipantId).Distinct().Count());
            Assert.Equal(bots.Count, bots.Select(b => b.ActorNumber).Distinct().Count());
            Assert.DoesNotContain(cast.PlayerParticipantId, bots.Select(b => b.ParticipantId));

            foreach (var bot in bots)
            {
                Assert.True(bot.ActorNumber < -1, $"{bot.Name}: {bot.ActorNumber}");
                Assert.True(cast.TryGetBot(bot.ParticipantId, out var found));
                Assert.Equal(bot.ActorNumber, found.ActorNumber);
                Assert.Equal(bot.ActorNumber, cast.ActorOf(bot.ParticipantId, 1));
            }

            Assert.Equal(7, cast.ActorOf(cast.PlayerParticipantId, 7));
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Cast_BotNamesContainNoDigits(ScenarioId id)
        {
            Assert.All(Def(id).Cast.Bots,
                b => Assert.All(b.Name ?? "", ch => Assert.False(char.IsDigit(ch))));
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Cast_RoleCountsAreConsistentWithTheBotRoster(ScenarioId id)
        {
            ScenarioCast cast = Def(id).Cast;
            int wolves = cast.Bots.Count(b => b.Role == Role.Werewolf || b.Role == Role.Bomber);
            if (cast.PlayerRole == Role.Werewolf || cast.PlayerRole == Role.Bomber) wolves++;

            Assert.True(wolves <= cast.WerewolfCount,
                $"人狼枠 {cast.WerewolfCount} に対して強制指定が {wolves} 人");
            Assert.InRange(cast.BlackCatChancePercent, 0, 100);
            Assert.InRange(cast.BomberChancePercent, 0, 100);
            Assert.InRange(cast.ShamanChancePercent, 0, 100);
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Cast_IsAppliedToTheSessionConfig(ScenarioId id)
        {
            ScenarioDefinition definition = Def(id);
            var config = ScenarioGameConfig.Build(new GameConfig(), definition);

            Assert.Equal(definition.Cast.WerewolfCount, config.WerewolfCount);
            Assert.Equal(definition.Cast.BlackCatChancePercent, config.BlackCatChancePercent);
            Assert.Equal(definition.Cast.BomberChancePercent, config.BomberChancePercent);
            Assert.Equal(definition.Cast.ShamanChancePercent, config.ShamanChancePercent);
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Roster_BuildsBotsAndForcedRolesFromTheSameDefinition(ScenarioId id)
        {
            ScenarioCast cast = Def(id).Cast;
            var bots = ScenarioRoster.BuildBots(cast.Bots);
            var forced = ScenarioRoster.BuildForcedRoles(cast.Bots);

            Assert.Equal(cast.Bots.Count, bots.Count);
            Assert.All(bots, b => Assert.True(b.IsBot));
            Assert.Equal(cast.Bots.Count, forced.Count);
            foreach (var participant in cast.Bots)
            {
                Assert.Equal(participant.Role, forced[participant.ActorNumber]);
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Environment_FixesAMapAndStartsAboveTheFirstLevel(ScenarioId id)
        {
            ScenarioEnvironmentSpec spec = Def(id).Environment;

            Assert.False(string.IsNullOrWhiteSpace(spec.ForcedLevelName));
            Assert.True(spec.LevelsCompleted >= 1);
            Assert.True(spec.TruckLeashMeters >= 0f);
            Assert.Equal(spec.TruckLeashMeters > 0f, spec.TruckLeashEnabled);
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Environment_NamesOnlyVanillaAssetsListedInTheRegistry(ScenarioId id)
        {
            ScenarioEnvironmentSpec spec = Def(id).Environment;

            Assert.True(
                ScenarioVanillaNames.Contains(ScenarioVanillaNameKind.Level, spec.ForcedLevelName),
                $"{id} の固定マップ \"{spec.ForcedLevelName}\" が ScenarioVanillaNames に無い");

            foreach (var upgrade in spec.StartUpgrades)
            {
                Assert.True(
                    ScenarioVanillaNames.Contains(ScenarioVanillaNameKind.Upgrade, upgrade.Key),
                    $"{id} の開始強化 \"{upgrade.Key}\" が ScenarioVanillaNames に無い");
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Environment_EnemySetsStayWithinTheAcceptedRange(ScenarioId id)
        {
            EnemySetCounts sets = Def(id).Environment.EnemySets;
            EnemySetCounts clamped = EnemySetRules.Clamp(sets);

            Assert.Equal(clamped.Danger1, sets.Danger1);
            Assert.Equal(clamped.Danger2, sets.Danger2);
            Assert.Equal(clamped.Danger3, sets.Danger3);
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Environment_SeededLossStaysBelowEveryPerkThreshold(ScenarioId id)
        {
            var config = new GameConfig();
            int pct = Def(id).Environment.SeededLossPct;

            Assert.InRange(pct, 0, 100);
            if (pct == 0) return;

            Assert.True(pct < config.StaminaUnlockPct);
            Assert.True(pct < config.JumpUnlockPct);
            Assert.True(pct < config.EnemyIgnoreUnlockPct);
            Assert.True(pct < config.HealUnlockPct);
            Assert.True(pct < config.BeaconChargePct);
            Assert.True(pct < config.InformantThresholdPct);
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void ChatScript_SpeakersAreBotsAndLinesFitTheChatLog(ScenarioId id)
        {
            ScenarioDefinition definition = Def(id);

            Assert.NotEmpty(definition.Meetings);
            foreach (var line in definition.Meetings.SelectMany(m => m.ChatScript))
            {
                Assert.True(definition.Cast.TryGetBot(line.ParticipantId, out _),
                    $"話者 {line.ParticipantId} が名簿に無い");
                foreach (Language language in new[] { Language.Japanese, Language.English })
                {
                    string text = Texts.TableFor(language)[line.Text];
                    Assert.False(string.IsNullOrWhiteSpace(text), $"{language}: {line.Text}");
                    Assert.True(text.Length <= MeetingChatLog.MaxTextLength,
                        $"{language}: {line.Text} は {text.Length} 文字");
                }
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void BotVotes_ComeFromBotsAndNeverFromTheSameBotTwice(ScenarioId id)
        {
            ScenarioDefinition definition = Def(id);

            foreach (var meeting in definition.Meetings)
            {
                var voters = meeting.BotVotes.Select(v => v.VoterParticipantId).ToArray();

                Assert.Equal(voters.Length, voters.Distinct().Count());
                foreach (var vote in meeting.BotVotes)
                {
                    Assert.True(definition.Cast.TryGetBot(vote.VoterParticipantId, out _),
                        $"投票者 {vote.VoterParticipantId} が名簿に無い");
                    Assert.NotEqual(vote.VoterParticipantId, vote.TargetParticipantId);
                    Assert.True(vote.IsSkip
                                || vote.TargetParticipantId == definition.Cast.PlayerParticipantId
                                || definition.Cast.TryGetBot(vote.TargetParticipantId, out _),
                        $"投票先 {vote.TargetParticipantId} が名簿に無い");
                }
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_EveryLineResolvesInEveryLanguage(ScenarioId id)
        {
            foreach (var page in Def(id).Pages)
            {
                foreach (var line in page.Lines)
                {
                    foreach (Language language in new[] { Language.Japanese, Language.English })
                    {
                        Assert.False(string.IsNullOrWhiteSpace(Texts.TableFor(language)[line]),
                            $"{language}: {line}");
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_NoLineIsShownTwice(ScenarioId id)
        {
            var lines = Def(id).Pages.SelectMany(p => p.Lines).ToArray();

            Assert.Equal(lines.Length, lines.Distinct().Count());
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_NameABotForEveryBombPremise(ScenarioId id)
        {
            ScenarioDefinition definition = Def(id);

            foreach (var page in definition.Pages)
            {
                bool bombPremise = page.Need == ScenarioNeed.BombTargetNear
                                   || page.Need == ScenarioNeed.BombOnTarget;
                if (bombPremise)
                {
                    Assert.NotEqual(0, page.BombTargetParticipantId);
                }
                if (page.MarkBombTarget) Assert.NotEqual(0, page.BombTargetParticipantId);
                if (page.BombTargetParticipantId == 0) continue;
                Assert.True(definition.Cast.TryGetBot(page.BombTargetParticipantId, out _),
                    $"プラント対象 {page.BombTargetParticipantId} が名簿に無い");
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_EachCueAppearsAtMostOnceAndOnlyWhenItHasWorkToDo(ScenarioId id)
        {
            ScenarioDefinition definition = Def(id);
            var pages = definition.Pages;

            foreach (ScenarioCue cue in System.Enum.GetValues(typeof(ScenarioCue)))
            {
                if (cue == ScenarioCue.None || cue == ScenarioCue.ActionNag) continue;
                for (int m = 0; m < definition.Meetings.Count; m++)
                {
                    Assert.True(pages.Count(p => p.Cue == cue && p.MeetingIndex == m) <= 1,
                        $"{cue} を持つページが会議 {m} に複数ある");
                }
            }
            Assert.All(pages, p => Assert.InRange(p.MeetingIndex, 0, definition.Meetings.Count - 1));

            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Cue == ScenarioCue.ActionNag)
                {
                    Assert.True(i + 1 < pages.Count && pages[i + 1].Gate == ScenarioSignal.ActionNag,
                        $"page {i} の再案内を受けるページが無い");
                }

                if (pages[i].Gate == ScenarioSignal.ActionNag)
                {
                    Assert.True(i > 0 && pages[i - 1].Cue == ScenarioCue.ActionNag,
                        $"page {i} が再案内タイマーを回すページの直後にない");
                }
            }

            for (int m = 0; m < definition.Meetings.Count; m++)
            {
                ScenarioMeetingScript meeting = definition.Meetings[m];
                Assert.Equal(meeting.ChatScript.Count > 0,
                    pages.Any(p => p.Cue == ScenarioCue.ChatScript && p.MeetingIndex == m));
                Assert.Equal(meeting.BotVotes.Count > 0,
                    pages.Any(p => p.Cue == ScenarioCue.BotVotes && p.MeetingIndex == m));
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_ForgetAReusedEventGateBeforeItsSecondUse(ScenarioId id)
        {
            var pages = Def(id).Pages;

            for (int first = 0; first < pages.Count; first++)
            {
                ScenarioSignal gate = pages[first].Gate;
                if (gate == ScenarioSignal.Advance || gate == ScenarioSignal.ActionNag) continue;

                for (int second = first + 1; second < pages.Count; second++)
                {
                    if (pages[second].Gate != gate) continue;
                    bool forgotten = false;
                    for (int k = first + 1; k <= second && !forgotten; k++)
                    {
                        forgotten = pages[k].ForgetsOnEntry.Contains(gate);
                    }
                    Assert.True(forgotten, $"{gate} で入るページ {first} と {second} の間に保持を解くページが無い");
                    first = second - 1;
                    break;
                }
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_MarkEachMilestoneAtMostOnce(ScenarioId id)
        {
            var pages = Def(id).Pages;

            foreach (ScenarioMilestone milestone in System.Enum.GetValues(typeof(ScenarioMilestone)))
            {
                if (milestone == ScenarioMilestone.None) continue;
                Assert.True(pages.Count(p => p.Milestone == milestone) <= 1,
                    $"{milestone} の印を持つページが複数ある");
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_ArmCheckmateOnlyInTheScenarioThatEndsOnIt(ScenarioId id)
        {
            bool armed = Def(id).Pages.Any(p => p.Milestone == ScenarioMilestone.CheckmateArmed);

            Assert.Equal(id == ScenarioId.WerewolfPerks, armed);
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_ReachTheEndWhenEveryGateSignalArrives(ScenarioId id)
        {
            ScenarioDefinition definition = Def(id);
            ScenarioFlow flow = definition.CreateFlow();

            for (int guard = 0; guard < 500 && !flow.Finished; guard++)
            {
                if (flow.Feed(ScenarioSignal.Advance)) continue;

                int before = flow.Index;
                ScenarioSignal gate = definition.Pages[before + 1].Gate;
                flow.Feed(gate);
                if (flow.Index == before) flow.Feed(ScenarioSignal.Advance);
                Assert.True(flow.Index > before, $"page {before} が {gate} で進まない");
            }

            Assert.True(flow.Finished);
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void NagPages_RepeatEveryKeyTheirPromptNamed(ScenarioId id)
        {
            IReadOnlyList<ScenarioPage> pages = Def(id).Pages;
            foreach (Language language in new[] { Language.Japanese, Language.English })
            {
                var table = Texts.TableFor(language);
                for (int i = 1; i < pages.Count; i++)
                {
                    if (pages[i].Gate != ScenarioSignal.ActionNag) continue;

                    string prompt = JoinLines(table, pages[i - 1].Lines);
                    string nag = JoinLines(table, pages[i].Lines);
                    foreach (string tag in ScenarioKeyTags.All)
                    {
                        if (!prompt.Contains(tag)) continue;
                        Assert.True(nag.Contains(tag), $"{language} page {i} が {tag} を言い直さない");
                    }
                }
            }
        }

        private static string JoinLines(IReadOnlyDictionary<TextId, string> table, IReadOnlyList<TextId> ids)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < ids.Count; i++) sb.Append(table[ids[i]]).Append(' ');
            return sb.ToString();
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_DoNotPromptAnythingWhileTheMeetingStillHoldsThePlayer(ScenarioId id)
        {
            IReadOnlyList<ScenarioPage> pages = Def(id).Pages;
            for (int i = 0; i < pages.Count - 1; i++)
            {
                if (pages[i].Gate != ScenarioSignal.TallyRevealed) continue;

                ScenarioSignal next = pages[i + 1].Gate;
                Assert.True(
                    next == ScenarioSignal.MeetingReleased || next == ScenarioSignal.WinCeremonyDone,
                    $"page {i} の次が {next} で開く（拘束中に先へ進む）");
            }
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Pages_DoNotFinishOnAdvanceInputAlone(ScenarioId id)
        {
            ScenarioFlow flow = Def(id).CreateFlow();
            for (int i = 0; i < 200; i++) flow.Feed(ScenarioSignal.Advance);

            Assert.False(flow.Finished);
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void Notices_AreCompleteForWhatTheScenarioTurnsOn(ScenarioId id)
        {
            ScenarioDefinition definition = Def(id);
            ScenarioNotices notices = definition.Notices;

            Assert.Equal(definition.Environment.TruckLeashEnabled, notices.LeashNag.Count > 0);
            Assert.Equal(notices.EarlyStartSignal.HasValue, notices.EarlyStartLines.Count > 0);
            Assert.Equal(definition.Pages.Any(p => p.BombTargetParticipantId != 0),
                notices.WrongTargetLines.Count > 0);
            Assert.Equal(notices.WrongTargetLines.Count > 0, notices.AlreadyOnTargetLines.Count > 0);
            if (notices.EarlyStartSignal.HasValue)
            {
                Assert.NotEqual(ScenarioMilestone.None, notices.EarlyStartAfter);
                Assert.Single(definition.Pages, p => p.Milestone == notices.EarlyStartAfter);
            }

            foreach (ScenarioBreakKind kind in System.Enum.GetValues(typeof(ScenarioBreakKind)))
            {
                bool named = notices.BreakLines(kind).Count > 0;
                if (!definition.CanBreak(kind))
                {
                    Assert.False(named, $"{kind} は起こらないのに文言がある");
                }
                else if (MustNameCause(kind))
                {
                    Assert.True(named, $"{kind} の原因を名指しする文言が無い");
                }
            }
            if (definition.Watch.ExtractionBreaksBefore != ScenarioMilestone.None)
            {
                Assert.Single(definition.Pages, p => p.Milestone == definition.Watch.ExtractionBreaksBefore);
            }
            if (definition.Watch.MaxQuotaBagReissues > 0)
            {
                Assert.Contains(definition.Pages, p => p.Has(ScenarioEntryAction.SpawnQuotaBag));
                Assert.True(definition.Watch.QuotaBagUntil.HasValue,
                    "袋の面倒を見る窓を閉じる信号が無い（次の抽出地点のぶんまで出し続ける）");
                Assert.True(IndexOfGate(definition, definition.Watch.QuotaBagUntil.Value) >= 0,
                    $"{definition.Watch.QuotaBagUntil} で入るページが無い");
            }
            if (definition.Environment.TruckLeashAfter != ScenarioMilestone.None)
            {
                Assert.True(definition.Environment.TruckLeashEnabled);
                Assert.Single(definition.Pages, p => p.Milestone == definition.Environment.TruckLeashAfter);
            }
            if (definition.Environment.TruckLeashBefore != ScenarioMilestone.None)
            {
                Assert.True(definition.Environment.TruckLeashEnabled);
                Assert.Single(definition.Pages, p => p.Milestone == definition.Environment.TruckLeashBefore);
                if (definition.Environment.TruckLeashAfter != ScenarioMilestone.None)
                {
                    Assert.True(IndexOfMilestone(definition, definition.Environment.TruckLeashBefore)
                        < IndexOfMilestone(definition, definition.Environment.TruckLeashAfter));
                }
            }

            if (notices.MapAside.Count > 0)
            {
                Assert.Contains(definition.Pages, p => p.Cue == ScenarioCue.ChatScript);
            }

            IEnumerable<TextId> breakLines = System.Enum.GetValues(typeof(ScenarioBreakKind))
                .Cast<ScenarioBreakKind>()
                .SelectMany(kind => notices.BreakLines(kind));
            foreach (var line in notices.LeashNag
                         .Concat(notices.EarlyStartLines)
                         .Concat(notices.ExtractionNag)
                         .Concat(breakLines)
                         .Concat(notices.WrongTargetLines)
                         .Concat(notices.AlreadyOnTargetLines)
                         .Concat(notices.MapAside))
            {
                foreach (Language language in new[] { Language.Japanese, Language.English })
                {
                    Assert.False(string.IsNullOrWhiteSpace(Texts.TableFor(language)[line]),
                        $"{language}: {line}");
                }
            }

            if (!notices.KeyHint.HasValue) return;
            Assert.Contains("{0}", Texts.TableFor(Language.Japanese)[notices.KeyHint.FormatId]);
            Assert.Contains("{0}", Texts.TableFor(Language.English)[notices.KeyHint.FormatId]);
            Assert.False(string.IsNullOrWhiteSpace(Texts.Get(notices.KeyHint.GrabNoteId)));
            Assert.False(string.IsNullOrWhiteSpace(Texts.Get(notices.KeyHint.InteractNoteId)));
            Assert.Contains(definition.Pages, p => p.Lines.Contains(notices.KeyHint.GrabNoteId));
            Assert.DoesNotContain(definition.Pages, p => p.Lines.Contains(notices.KeyHint.InteractNoteId));
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void BreakWatch_MatchesTheOrderOfTheScript(ScenarioId id)
        {
            ScenarioDefinition definition = Def(id);
            ScenarioBreakWatch watch = definition.Watch;

            if (watch.RequireTallyBeforeResult)
            {
                int tally = IndexOfGate(definition, ScenarioSignal.TallyRevealed);
                int result = IndexOfGate(definition, ScenarioSignal.ResultScreenShown);
                Assert.True(tally >= 0, "開票演出で入るページが無い");
                Assert.True(result > tally, $"結果画面のページ({result})が開票({tally})より前にある");
            }

            if (!watch.MeetingRequiredFrom.HasValue) return;

            int from = IndexOfGate(definition, watch.MeetingRequiredFrom.Value);
            Assert.True(from >= 0, $"{watch.MeetingRequiredFrom} で入るページが無い");
            if (!watch.MeetingRequiredUntil.HasValue) return;

            int until = IndexOfGate(definition, watch.MeetingRequiredUntil.Value);
            Assert.True(until > from, "会議を前提にする区間の終わりが始まりより前にある");
        }

        [Theory]
        [MemberData(nameof(Implemented))]
        public void CorpseHint_NamesABotAndSignalsTheScriptUses(ScenarioId id)
        {
            ScenarioDefinition definition = Def(id);
            ScenarioCorpseHint hint = definition.CorpseHint;
            if (!hint.Enabled) return;

            Assert.True(definition.Cast.TryGetBot(hint.ParticipantId, out _), $"{hint.ParticipantId}");
            Assert.NotEmpty(hint.Lines);
            foreach (TextId line in hint.Lines)
            {
                foreach (Language language in new[] { Language.Japanese, Language.English })
                {
                    Assert.False(string.IsNullOrWhiteSpace(Texts.TableFor(language)[line]),
                        $"{language}: {line}");
                }
            }

            Assert.NotEqual(hint.After, hint.Until);
            Assert.NotEqual(ScenarioSignal.Advance, hint.After);
            Assert.NotEqual(ScenarioSignal.Advance, hint.Until);
            Assert.Contains(definition.Pages, p => p.Gate == hint.After || p.SkipIf.Contains(hint.After));
            Assert.Contains(definition.Pages, p => p.Gate == hint.Until);
            Assert.True(hint.DelayMs > 0);
        }

        [Fact]
        public void Notices_DefaultHasNoBreakLinesForAnyKind()
        {
            ScenarioNotices notices = default;
            foreach (ScenarioBreakKind kind in System.Enum.GetValues(typeof(ScenarioBreakKind)))
            {
                Assert.Empty(notices.BreakLines(kind));
            }
        }

        private static bool MustNameCause(ScenarioBreakKind kind)
        {
            switch (kind)
            {
                case ScenarioBreakKind.AllExtractionsCompleted:
                case ScenarioBreakKind.EarlyExtraction:
                case ScenarioBreakKind.QuotaBagLost:
                    return true;
                default:
                    return false;
            }
        }

        private static int IndexOfGate(ScenarioDefinition definition, ScenarioSignal gate)
        {
            var pages = definition.Pages;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Gate == gate) return i;
            }
            return -1;
        }

        private static int IndexOfMilestone(ScenarioDefinition definition, ScenarioMilestone milestone)
        {
            var pages = definition.Pages;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Milestone == milestone) return i;
            }
            return -1;
        }
    }
}
