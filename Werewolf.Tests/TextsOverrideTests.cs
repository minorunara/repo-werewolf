using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class TextsOverrideTests : IDisposable
    {
        public void Dispose()
        {
            Texts.ClearOverride();
            Texts.FormatErrorLogger = null;
        }

        [Fact]
        public void Get_AfterSetOverride_ReturnsOverriddenValue()
        {
            Texts.SetOverride(new Dictionary<TextId, string>
            {
                [TextId.NoticeCatAwakened] = "The black cat stirs",
            });

            Assert.Equal("The black cat stirs", Texts.Get(TextId.NoticeCatAwakened));
        }

        [Fact]
        public void Get_KeyMissingFromOverride_FallsBackToJapanese()
        {
            Texts.SetOverride(new Dictionary<TextId, string>
            {
                [TextId.NoticeCatAwakened] = "override only for this key",
            });

            Assert.Equal("誰も処刑されませんでした", Texts.Get(TextId.NoticeNoExecution));
        }

        [Fact]
        public void ClearOverride_RestoresJapaneseForPreviouslyOverriddenKey()
        {
            Texts.SetOverride(new Dictionary<TextId, string>
            {
                [TextId.NoticeCatAwakened] = "override",
            });
            Texts.ClearOverride();

            Assert.Equal("もし黒猫がいるなら、目覚めている頃です…", Texts.Get(TextId.NoticeCatAwakened));
        }

        [Fact]
        public void Format_BrokenOverrideTemplate_FallsBackToJapaneseInsteadOfThrowing()
        {
            Texts.SetOverride(new Dictionary<TextId, string>
            {
                [TextId.NoticeExecutedFormat] = "{9} was executed",
            });

            string result = Texts.Format(TextId.NoticeExecutedFormat, "Alice");

            Assert.Equal("Aliceが処刑されました", result);
        }

        [Fact]
        public void Format_BrokenOverrideTemplate_LogsOnlyOncePerKey()
        {
            var calls = new List<string>();
            Texts.FormatErrorLogger = id => calls.Add(id);
            Texts.SetOverride(new Dictionary<TextId, string>
            {
                [TextId.NoticeExecutedFormat] = "{9} broken",
            });

            Texts.Format(TextId.NoticeExecutedFormat, "Alice");
            Texts.Format(TextId.NoticeExecutedFormat, "Bob");
            Texts.Format(TextId.NoticeExecutedFormat, "Carol");

            Assert.Single(calls);
            Assert.Equal(nameof(TextId.NoticeExecutedFormat), calls[0]);
        }

        [Fact]
        public void Format_UnsetLogger_DoesNotThrow()
        {
            Texts.FormatErrorLogger = null;
            Texts.SetOverride(new Dictionary<TextId, string>
            {
                [TextId.NoticeExecutedFormat] = "{9} broken",
            });

            string result = Texts.Format(TextId.NoticeExecutedFormat, "Alice");

            Assert.Equal("Aliceが処刑されました", result);
        }

        [Fact]
        public void Format_NonOverriddenKey_IsUnaffectedByOtherOverrides()
        {
            Texts.SetOverride(new Dictionary<TextId, string>
            {
                [TextId.NoticeExecutedFormat] = "{9} broken",
            });

            string result = Texts.Format(TextId.NoticeCurseVictimFormat, "Bob");
            Assert.Equal("Bobは道連れにされました", result);
        }

        [Fact]
        public void ExportTemplate_ContainsAllTextIds_AndRoundTripsThroughLangFileParse()
        {
            string template = Texts.ExportTemplate();
            var parsed = LangFile.Parse(template);

            foreach (TextId id in Enum.GetValues(typeof(TextId)))
            {
                Assert.True(parsed.ContainsKey(id), $"ExportTemplate に {id} のキー行が含まれていない");
                Assert.Equal(Texts.Get(id), parsed[id]);
            }
        }
    }
}
