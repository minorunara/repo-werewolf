using System;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class TextsTests
    {
        [Fact]
        public void AllTextIds_HaveNonEmptyJapaneseEntry()
        {
            foreach (TextId id in Enum.GetValues(typeof(TextId)))
            {
                string value = Texts.Get(id);
                Assert.False(string.IsNullOrEmpty(value), $"TextId.{id} に日本語文言が登録されていない");
            }
        }

        [Fact]
        public void AllTextIds_HaveNonEmptyEnglishEntry()
        {
            var english = Texts.TableFor(Language.English);
            foreach (TextId id in Enum.GetValues(typeof(TextId)))
            {
                Assert.True(english.TryGetValue(id, out var value) && !string.IsNullOrEmpty(value),
                    $"TextId.{id} に英語文言が登録されていない");
            }
        }

        [Fact]
        public void EnglishTable_ContainsNoJapaneseCharacters()
        {
            var english = Texts.TableFor(Language.English);
            foreach (TextId id in Enum.GetValues(typeof(TextId)))
            {
                string value = english[id];
                var hit = System.Text.RegularExpressions.Regex.Match(value, @"[ぁ-ゖァ-ヺ一-鿿]+");
                Assert.False(hit.Success,
                    $"TextId.{id} の英語文言に日本語が残っている（\"{hit.Value}\"）");
            }
        }

        [Fact]
        public void EnglishAndJapanese_LineCountsMatch()
        {
            var japanese = Texts.TableFor(Language.Japanese);
            var english = Texts.TableFor(Language.English);
            foreach (TextId id in Enum.GetValues(typeof(TextId)))
            {
                int ja = japanese[id].Split('\n').Length;
                int en = english[id].Split('\n').Length;
                Assert.True(ja == en,
                    $"TextId.{id} の行数が日英で不一致（ja: {ja} / en: {en}）");
            }
        }

        [Fact]
        public void EnglishAndJapanese_PlaceholderSetsMatch()
        {
            var japanese = Texts.TableFor(Language.Japanese);
            var english = Texts.TableFor(Language.English);
            foreach (TextId id in Enum.GetValues(typeof(TextId)))
            {
                var ja = Placeholders(japanese[id]);
                var en = Placeholders(english[id]);
                Assert.True(ja.SetEquals(en),
                    $"TextId.{id} のプレースホルダが日英で不一致（ja: {string.Join(",", ja)} / en: {string.Join(",", en)}）");
            }
        }

        private static System.Collections.Generic.HashSet<string> Placeholders(string template)
        {
            var result = new System.Collections.Generic.HashSet<string>();
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(template, @"\{\d+\}"))
            {
                result.Add(m.Value);
            }
            return result;
        }

        [Fact]
        public void Current_DefaultsToJapanese()
        {
            Assert.Equal(Language.Japanese, Texts.Current);
        }

        [Fact]
        public void Format_SubstitutesPlaceholders()
        {
            string result = Texts.Format(TextId.NoticeExecutedFormat, "Alice");
            Assert.Equal("Aliceが処刑されました", result);
        }

        [Fact]
        public void Format_NoArgs_ReturnsTemplateUnchanged()
        {
            string result = Texts.Format(TextId.NoticeNoExecution);
            Assert.Equal(Texts.Get(TextId.NoticeNoExecution), result);
        }
    }
}
