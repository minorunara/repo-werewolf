using System;
using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioNeedRulesTests
    {
        private static ScenarioNeedSample Sample(bool meetingUi = true, bool chatLog = false,
                                                 bool meetingMap = false, bool mapTool = false,
                                                 bool trance = false, bool coolingDown = false,
                                                 bool storm = false, bool corpse = false)
            => new ScenarioNeedSample(meetingUi, chatLog, meetingMap, mapTool, trance, coolingDown,
                storm, corpse);

        public static IEnumerable<object[]> AllNeeds()
        {
            foreach (ScenarioNeed need in Enum.GetValues(typeof(ScenarioNeed)))
            {
                if (need != ScenarioNeed.None) yield return new object[] { need };
            }
        }

        [Theory]
        [MemberData(nameof(AllNeeds))]
        public void EveryNeed_HasANagThatResolvesInEveryLanguage(ScenarioNeed need)
        {
            Assert.True(ScenarioNeedRules.TryGetNag(need, out ScenarioNeedNag nag), need.ToString());
            foreach (Language language in new[] { Language.Japanese, Language.English })
            {
                Assert.False(string.IsNullOrWhiteSpace(Texts.TableFor(language)[nag.Text]),
                    $"{language}: {need}");
            }
        }

        [Fact]
        public void None_HasNoNagAndIsNeverUnmet()
        {
            Assert.False(ScenarioNeedRules.TryGetNag(ScenarioNeed.None, out _));
            Assert.False(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.None));
            Assert.Equal(ScenarioNeed.None, ScenarioNeedRules.Unmet(ScenarioNeed.None, Sample()));
        }

        [Theory]
        [InlineData(ScenarioNeed.ChatLogOpen)]
        [InlineData(ScenarioNeed.MapClosed)]
        [InlineData(ScenarioNeed.MapOpen)]
        public void MeetingUiNeeds_AreNotJudgedWhileTheMeetingUiIsHidden(ScenarioNeed need)
        {
            Assert.Equal(ScenarioNeed.None,
                ScenarioNeedRules.Unmet(need, Sample(meetingUi: false)));
            Assert.Equal(ScenarioNeed.None,
                ScenarioNeedRules.Unmet(need, Sample(meetingUi: false, chatLog: true, meetingMap: true)));
        }

        [Fact]
        public void ChatLogOpen_IsUnmetUntilTheLogIsOpen()
        {
            Assert.Equal(ScenarioNeed.ChatLogOpen,
                ScenarioNeedRules.Unmet(ScenarioNeed.ChatLogOpen, Sample(chatLog: false)));
            Assert.Equal(ScenarioNeed.None,
                ScenarioNeedRules.Unmet(ScenarioNeed.ChatLogOpen, Sample(chatLog: true)));
        }

        [Fact]
        public void MapClosed_IsUnmetWhileTheMeetingMapIsOpen()
        {
            Assert.Equal(ScenarioNeed.MapClosed,
                ScenarioNeedRules.Unmet(ScenarioNeed.MapClosed, Sample(meetingMap: true)));
            Assert.Equal(ScenarioNeed.None,
                ScenarioNeedRules.Unmet(ScenarioNeed.MapClosed, Sample(meetingMap: false)));
        }

        [Fact]
        public void MapOpen_AsksToCloseTheMapToolWhenItBlocksTheMeetingMap()
        {
            Assert.Equal(ScenarioNeed.MapToolClosed,
                ScenarioNeedRules.Unmet(ScenarioNeed.MapOpen, Sample(meetingMap: false, mapTool: true)));
            Assert.Equal(ScenarioNeed.MapOpen,
                ScenarioNeedRules.Unmet(ScenarioNeed.MapOpen, Sample(meetingMap: false, mapTool: false)));
            Assert.Equal(ScenarioNeed.None,
                ScenarioNeedRules.Unmet(ScenarioNeed.MapOpen, Sample(meetingMap: true, mapTool: true)));
        }

        [Theory]
        [InlineData(ScenarioNeed.MapToolOpen)]
        [InlineData(ScenarioNeed.ShamanTranceActive)]
        [InlineData(ScenarioNeed.ShamanStormActive)]
        [InlineData(ScenarioNeed.CorpseNear)]
        public void PlayNeeds_AreJudgedRegardlessOfTheMeetingUi(ScenarioNeed need)
        {
            Assert.Equal(need, ScenarioNeedRules.Unmet(need, Sample(meetingUi: true)));
            Assert.Equal(need, ScenarioNeedRules.Unmet(need, Sample(meetingUi: false)));

            ScenarioNeedSample satisfied = Sample(meetingUi: false,
                mapTool: need == ScenarioNeed.MapToolOpen,
                trance: need == ScenarioNeed.ShamanTranceActive,
                storm: need == ScenarioNeed.ShamanStormActive,
                corpse: need == ScenarioNeed.CorpseNear);
            Assert.Equal(ScenarioNeed.None, ScenarioNeedRules.Unmet(need, satisfied));
        }

        [Fact]
        public void ShamanTrance_AsksToWaitWhileTheSenseIsCoolingDown()
        {
            Assert.Equal(ScenarioNeed.ShamanSenseReady,
                ScenarioNeedRules.Unmet(ScenarioNeed.ShamanTranceActive,
                    Sample(trance: false, coolingDown: true)));
            Assert.Equal(ScenarioNeed.ShamanSenseReady,
                ScenarioNeedRules.Unmet(ScenarioNeed.ShamanTranceActive,
                    Sample(trance: true, coolingDown: true)));

            Assert.Equal(ScenarioNeed.ShamanTranceActive,
                ScenarioNeedRules.Unmet(ScenarioNeed.ShamanTranceActive,
                    Sample(trance: false, coolingDown: false)));
            Assert.Equal(ScenarioNeed.None,
                ScenarioNeedRules.Unmet(ScenarioNeed.ShamanTranceActive,
                    Sample(trance: true, coolingDown: false)));
        }

        [Fact]
        public void ShamanSenseReady_IsUnmetOnlyWhileCoolingDown()
        {
            Assert.Equal(ScenarioNeed.ShamanSenseReady,
                ScenarioNeedRules.Unmet(ScenarioNeed.ShamanSenseReady, Sample(coolingDown: true)));
            Assert.Equal(ScenarioNeed.None,
                ScenarioNeedRules.Unmet(ScenarioNeed.ShamanSenseReady, Sample(coolingDown: false)));
            Assert.Equal(ScenarioNeed.ShamanSenseReady,
                ScenarioNeedRules.Unmet(ScenarioNeed.ShamanSenseReady,
                    Sample(meetingUi: false, coolingDown: true)));
        }

        [Fact]
        public void ShamanStorm_IsJudgedIndependentlyOfTheCooldown()
        {
            Assert.Equal(ScenarioNeed.None,
                ScenarioNeedRules.Unmet(ScenarioNeed.ShamanStormActive,
                    Sample(storm: true, coolingDown: true)));
            Assert.Equal(ScenarioNeed.ShamanStormActive,
                ScenarioNeedRules.Unmet(ScenarioNeed.ShamanStormActive,
                    Sample(storm: false, coolingDown: true)));
        }

        [Fact]
        public void OnlyScreenOperationsCountAsClearedByUi()
        {
            Assert.True(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.ChatLogOpen));
            Assert.True(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.MapClosed));
            Assert.True(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.MapOpen));
            Assert.True(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.MapToolOpen));
            Assert.True(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.MapToolClosed));
            Assert.False(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.ShamanTranceActive));
            Assert.False(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.ShamanSenseReady));
            Assert.False(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.ShamanStormActive));
            Assert.False(ScenarioNeedRules.ClearedByUiOperation(ScenarioNeed.CorpseNear));
        }

        [Fact]
        public void NagsWithoutAScreenButton_ShowNoHighlight()
        {
            foreach (ScenarioNeed need in new[]
                     {
                         ScenarioNeed.MapToolOpen, ScenarioNeed.MapToolClosed,
                         ScenarioNeed.ShamanTranceActive, ScenarioNeed.ShamanSenseReady,
                         ScenarioNeed.ShamanStormActive, ScenarioNeed.CorpseNear,
                     })
            {
                Assert.True(ScenarioNeedRules.TryGetNag(need, out ScenarioNeedNag nag));
                Assert.Equal(ScenarioHighlight.None, nag.Highlight);
            }

            Assert.True(ScenarioNeedRules.TryGetNag(ScenarioNeed.ChatLogOpen, out ScenarioNeedNag chat));
            Assert.Equal(ScenarioHighlight.ChatLogButton, chat.Highlight);
            Assert.Equal(ScenarioAnchor.ChatLogButton, chat.Anchor);
            Assert.True(ScenarioNeedRules.TryGetNag(ScenarioNeed.MapOpen, out ScenarioNeedNag map));
            Assert.Equal(ScenarioHighlight.MapButton, map.Highlight);
            Assert.Equal(ScenarioAnchor.MapButton, map.Anchor);
        }
    }
}
