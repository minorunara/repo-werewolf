using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioDecodeIntroTests
    {
        private const int EmojiCount = 10;

        private static readonly float AllEmojiAt =
            EmojiCount * ScenarioDecodeIntro.EmojiIntervalSec;

        private static ScenarioDecodeIntro.State At(float t)
            => ScenarioDecodeIntro.At(t, EmojiCount);

        [Fact]
        public void Start_ShowsNothing()
        {
            var s = At(0f);

            Assert.Equal(0, s.EmojiShown);
            Assert.False(s.Line1Visible);
            Assert.False(s.Glitching);
            Assert.False(s.Translated);
            Assert.False(s.Returning);
            Assert.False(s.Finished);
        }

        [Fact]
        public void Emoji_ArriveOnePerInterval()
        {
            Assert.Equal(1, At(ScenarioDecodeIntro.EmojiIntervalSec).EmojiShown);
            Assert.Equal(1, At(ScenarioDecodeIntro.EmojiIntervalSec * 1.5f).EmojiShown);
            Assert.Equal(EmojiCount - 1,
                At(AllEmojiAt - ScenarioDecodeIntro.EmojiIntervalSec * 0.5f).EmojiShown);
            Assert.Equal(EmojiCount, At(AllEmojiAt).EmojiShown);
        }

        [Fact]
        public void Emoji_NeverExceedCount()
        {
            Assert.Equal(EmojiCount, At(AllEmojiAt * 3f).EmojiShown);
        }

        [Fact]
        public void Line1_AppearsWhenAllEmojiArrived_ThenGetsDone()
        {
            var justArrived = At(AllEmojiAt);
            Assert.True(justArrived.Line1Visible);
            Assert.False(justArrived.Line1Done);
            Assert.False(justArrived.Line2Visible);

            var done = At(AllEmojiAt + ScenarioDecodeIntro.DoneDelaySec);
            Assert.True(done.Line1Done);
            Assert.False(done.Line2Visible);
        }

        [Fact]
        public void Line2_AppearsAfterGap_ThenGetsDone()
        {
            float line2At = AllEmojiAt + ScenarioDecodeIntro.DoneDelaySec
                            + ScenarioDecodeIntro.LineGapSec;

            var appeared = At(line2At);
            Assert.True(appeared.Line2Visible);
            Assert.False(appeared.Line2Done);
            Assert.False(appeared.Glitching);

            var done = At(line2At + ScenarioDecodeIntro.DoneDelaySec);
            Assert.True(done.Line2Done);
            Assert.False(done.Glitching);
        }

        [Fact]
        public void Glitch_StartsAfterHold_ThenShowsTranslation()
        {
            float glitchAt = AllEmojiAt + ScenarioDecodeIntro.DoneDelaySec
                             + ScenarioDecodeIntro.LineGapSec + ScenarioDecodeIntro.DoneDelaySec
                             + ScenarioDecodeIntro.HoldSec;

            var holding = At(glitchAt - ScenarioDecodeIntro.HoldSec * 0.5f);
            Assert.False(holding.Glitching);
            Assert.False(holding.Finished);

            var glitching = At(glitchAt);
            Assert.True(glitching.Glitching);
            Assert.False(glitching.Finished);

            var translated = At(glitchAt + ScenarioDecodeIntro.GlitchSec);
            Assert.False(translated.Glitching);
            Assert.True(translated.Translated);
            Assert.False(translated.Returning);
            Assert.False(translated.Finished);
        }

        [Fact]
        public void Translation_HoldsThenReturnsBeforeFinishing()
        {
            float translatedAt = AllEmojiAt + ScenarioDecodeIntro.DoneDelaySec
                                 + ScenarioDecodeIntro.LineGapSec + ScenarioDecodeIntro.DoneDelaySec
                                 + ScenarioDecodeIntro.HoldSec + ScenarioDecodeIntro.GlitchSec;

            var holding = At(translatedAt + ScenarioDecodeIntro.TranslationHoldSec * 0.5f);
            Assert.True(holding.Translated);
            Assert.False(holding.Returning);
            Assert.False(holding.Finished);

            var returning = At(translatedAt + ScenarioDecodeIntro.TranslationHoldSec);
            Assert.True(returning.Translated);
            Assert.True(returning.Returning);
            Assert.False(returning.Finished);

            var finished = At(translatedAt + ScenarioDecodeIntro.TranslationHoldSec
                               + ScenarioDecodeIntro.ReturnSec);
            Assert.False(finished.Translated);
            Assert.False(finished.Returning);
            Assert.True(finished.Finished);
        }

        [Fact]
        public void ZeroEmoji_StillRunsLinesAndFinishes()
        {
            var s = ScenarioDecodeIntro.At(0f, 0);
            Assert.Equal(0, s.EmojiShown);
            Assert.True(s.Line1Visible);

            Assert.True(ScenarioDecodeIntro.At(100f, 0).Finished);
        }

        [Fact]
        public void EmojiUnits_SplitScriptLineWithoutLoss()
        {
            var units = ScenarioDecodeIntro.EmojiUnits(MeetingScenarioScript.IntroDecodeEmoji);

            Assert.Equal(MeetingScenarioScript.IntroDecodeEmoji, string.Concat(units));
            Assert.DoesNotContain(units, unit => unit.Trim().Length == 0);
        }

        [Fact]
        public void EmojiUnits_AttachVariationSelectorToPrecedingUnit()
        {
            var units = ScenarioDecodeIntro.EmojiUnits("⚔️🗳️");

            Assert.Equal(2, units.Count);
            Assert.Equal("⚔️", units[0]);
            Assert.Equal("🗳️", units[1]);
        }

        [Fact]
        public void EmojiUnits_KeepVanillaTokenAsSingleUnit()
        {
            var units = ScenarioDecodeIntro.EmojiUnits("[[extraction]]🐺[[semibots]]");

            Assert.Equal(3, units.Count);
            Assert.Equal("[[extraction]]", units[0]);
            Assert.Equal("🐺", units[1]);
            Assert.Equal("[[semibots]]", units[2]);
        }

        [Fact]
        public void EmojiUnits_UnclosedVanillaToken_FallsBackToCharUnits()
        {
            var units = ScenarioDecodeIntro.EmojiUnits("[[extraction");

            Assert.Equal("[[extraction".Length, units.Count);
            Assert.Equal("[[extraction", string.Concat(units));
        }

        [Fact]
        public void EmojiUnits_EmptyOrNull_ReturnEmpty()
        {
            Assert.Empty(ScenarioDecodeIntro.EmojiUnits(null));
            Assert.Empty(ScenarioDecodeIntro.EmojiUnits(string.Empty));
        }

        [Fact]
        public void Garble_KeepsLengthAndNewlines_AndAvoidsMarkupChars()
        {
            const string source = "ここでは趣向を変えて人狼ゲームを演じてもらう。\nabc DEF 123";
            var rng = new Random(1234);

            string garbled = ScenarioDecodeIntro.Garble(source, rng);

            Assert.Equal(source.Length, garbled.Length);
            Assert.Equal(
                source.Select((c, i) => (c, i)).Where(p => p.c == '\n').Select(p => p.i),
                garbled.Select((c, i) => (c, i)).Where(p => p.c == '\n').Select(p => p.i));
            Assert.DoesNotContain(garbled, c => c == '<' || c == '>' || c == '[' || c == ']');
        }

        [Fact]
        public void Garble_KeepsWidthClassPerCharacter()
        {
            const string source = "会議abc";
            var rng = new Random(42);

            string garbled = ScenarioDecodeIntro.Garble(source, rng);

            for (int i = 0; i < source.Length; i++)
            {
                Assert.Equal(source[i] < 0x80, garbled[i] < 0x80);
            }
        }

        [Fact]
        public void Garble_ChangesBetweenRolls()
        {
            const string source = "ここでは趣向を変えて人狼ゲームを演じてもらう。";
            var rng = new Random(7);

            string first = ScenarioDecodeIntro.Garble(source, rng);
            string second = ScenarioDecodeIntro.Garble(source, rng);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void MeetingScript_WiresIntroDecodeEmojiIntoDefinition()
        {
            Assert.Equal(MeetingScenarioScript.IntroDecodeEmoji,
                MeetingScenarioScript.Definition.IntroDecodeEmoji);
        }

        [Fact]
        public void IntroDecodeEmoji_EveryUnitIsRenderable()
        {
            var units = ScenarioDecodeIntro.EmojiUnits(MeetingScenarioScript.IntroDecodeEmoji);
            var broken = new List<string>();
            foreach (string unit in units)
            {
                string core = unit.TrimEnd(' ');
                if (VanillaEmojiToken.TryParseWhole(core, out string name))
                {
                    if (!VanillaEmojiNames.All.Contains(name)) broken.Add($"本体に無いスプライト名 [[{name}]]");
                    continue;
                }
                if (core.IndexOf('[') >= 0 || core.IndexOf(']') >= 0)
                {
                    broken.Add($"括弧の壊れたトークン: {core}");
                    continue;
                }
                int cp = char.ConvertToUtf32(core, 0);
                if (!ChatEmoji.SpriteKeys.ContainsKey(cp)) broken.Add($"素材キーの無い絵文字 U+{cp:X4}");
            }

            Assert.True(broken.Count == 0,
                "冒頭演出の絵文字列に描けない単位がある: " + string.Join(", ", broken) +
                "。自前絵文字なら ChatEmoji.SpriteKeys と Werewolf/Assets の PNG を、" +
                "本体絵文字なら VanillaEmojiNames のスプライト名を確認する。");
        }
    }
}
