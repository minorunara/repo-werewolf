#nullable enable
using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class LanguagePolicyTests
    {
        [Theory]
        [InlineData("ja-JP", Language.Japanese, "ja-JP")]
        [InlineData("ja", Language.Japanese, "ja")]
        [InlineData("JA-jp", Language.Japanese, "JA-jp")]
        [InlineData("en-US", Language.English, "en-US")]
        [InlineData("zh-CN", Language.English, "zh-CN")]
        [InlineData("ko-KR", Language.English, "ko-KR")]
        [InlineData("", Language.English, "en-US")]
        [InlineData(null, Language.English, "en-US")]
        public void Resolve_FollowsGameLocale(string? locale, Language expected, string expectedFile)
        {
            var choice = LanguagePolicy.Resolve(locale);
            Assert.Equal(expected, choice.Language);
            Assert.Equal(expectedFile, choice.FileCode);
        }

        [Fact]
        public void Resolve_DoesNotTreatOtherJaPrefixedCodesAsJapanese()
        {
            Assert.Equal(Language.English, LanguagePolicy.Resolve("jav-ID").Language);
        }

        [Theory]
        [InlineData(" ja_JP ", "ja-JP")]
        [InlineData("zh-Hans-CN", "zh-Hans-CN")]
        [InlineData("../evil", "evil")]
        [InlineData("", "en-US")]
        [InlineData(null, "en-US")]
        public void NormalizeLocaleCode_KeepsOnlyFileSafeCharacters(string? code, string expected)
        {
            Assert.Equal(expected, LanguagePolicy.NormalizeLocaleCode(code));
        }

        [Fact]
        public void Apply_SwitchesLanguageAndOverride_AndRaisesChanged()
        {
            Language before = Texts.Current;
            int raised = 0;
            Action handler = () => raised++;
            Texts.Changed += handler;
            try
            {
                var table = new Dictionary<TextId, string> { [TextId.NoticeNoExecution] = "override" };
                Texts.Apply(Language.English, table);
                Assert.Equal(Language.English, Texts.Current);
                Assert.Equal("override", Texts.Get(TextId.NoticeNoExecution));
                Assert.Equal(1, raised);

                Texts.Apply(before, null);
                Assert.Equal(before, Texts.Current);
                Assert.Equal(Texts.TableFor(before)[TextId.NoticeNoExecution], Texts.Get(TextId.NoticeNoExecution));
                Assert.Equal(2, raised);
            }
            finally
            {
                Texts.Changed -= handler;
                Texts.Apply(before, null);
            }
        }
    }
}
