using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ChatEmojiTests
    {
        private static string Meta(string key)
        {
            return typeof(ChatEmojiTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == key).Value;
        }

        private static IEnumerable<int> CodePoints(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    yield return char.ConvertToUtf32(text[i], text[i + 1]);
                    i++;
                    continue;
                }
                yield return text[i];
            }
        }

        private static bool LooksLikeEmoji(int codePoint)
            => codePoint >= 0x10000 || (codePoint >= 0x2600 && codePoint <= 0x27BF);

        [Fact]
        public void Templates_ContainNoUnmappedEmoji()
        {
            var missing = new List<string>();
            foreach (KeyValuePair<TextId, string> pair in ChatEmoji.Templates)
            {
                foreach (int cp in CodePoints(pair.Value))
                {
                    if (!LooksLikeEmoji(cp)) continue;
                    if (ChatEmoji.SpriteKeys.ContainsKey(cp)) continue;
                    missing.Add($"{pair.Key}: U+{cp:X4}");
                }
            }

            Assert.True(missing.Count == 0,
                "素材キーの無い絵文字が文言テーブルにある: " + string.Join(", ", missing) +
                "。ChatEmoji.SpriteKeys へコードポイントを追加し、対応する PNG を Werewolf/Assets へ置く。");
        }

        [Fact]
        public void SpriteKeys_AreUniqueAssetKeys()
        {
            List<string> keys = ChatEmoji.SpriteKeys.Values.ToList();

            Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
            Assert.All(keys, key => Assert.StartsWith("emoji_", key, StringComparison.Ordinal));
        }

        [Fact]
        public void SpriteKeys_HaveEmbeddedPng()
        {
            string dllPath = Meta("Werewolf.ModDllPath");
            Assert.True(File.Exists(dllPath), $"MOD DLL が見つからない: {dllPath}");

            using var module = ModuleDefinition.ReadModule(dllPath);
            var embedded = new HashSet<string>(
                module.Resources.OfType<EmbeddedResource>().Select(r => r.Name), StringComparer.Ordinal);

            var missing = ChatEmoji.SpriteKeys.Values
                .Select(key => "Werewolf.Assets." + key + ".png")
                .Where(name => !embedded.Contains(name))
                .ToList();

            Assert.True(missing.Count == 0,
                "埋め込まれていない絵文字素材がある: " + string.Join(", ", missing) +
                "。PNG を Werewolf/Assets へ置く（csproj の Assets\\**\\*.png で自動埋め込み）。");
        }

        [Fact]
        public void Templates_MatchLanguagePlaceholders()
        {
            var mismatched = new List<string>();
            foreach (KeyValuePair<TextId, string> pair in ChatEmoji.Templates)
            {
                string emoji = Placeholders(pair.Value);
                string language = Placeholders(Texts.Get(pair.Key));
                if (emoji != language) mismatched.Add($"{pair.Key}: 絵文字={emoji} / 言語={language}");
            }

            Assert.True(mismatched.Count == 0,
                "絵文字表記と言語文言でプレースホルダが揃っていない: " + string.Join(", ", mismatched));
        }

        private static string Placeholders(string template)
            => string.Join(",", Regex.Matches(template ?? string.Empty, @"\{(\d+)\}")
                .Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(v => v));

        [Fact]
        public void Get_WithoutSprites_UsesLanguageText()
        {
            foreach (TextId id in ChatEmoji.Templates.Keys)
            {
                Assert.Equal(Texts.Get(id), ChatEmoji.Get(id, emoji: false));
                Assert.Equal(ChatEmoji.Template(id), ChatEmoji.Get(id, emoji: true));
            }
        }

        [Fact]
        public void Get_UnmappedKey_FallsBackToLanguageEvenWithSprites()
        {
            Assert.Null(ChatEmoji.Template(TextId.ChatLogSystemName));
            Assert.Equal(Texts.Get(TextId.ChatLogSystemName),
                ChatEmoji.Get(TextId.ChatLogSystemName, emoji: true));
        }

        [Fact]
        public void Format_FillsEmojiTemplate()
        {
            string line = ChatEmoji.Format(TextId.RecapHaulFormat, emoji: true, 500, 9000);

            Assert.Contains("500", line, StringComparison.Ordinal);
            Assert.Contains("9000", line, StringComparison.Ordinal);
            Assert.DoesNotContain("納品", line, StringComparison.Ordinal);
        }
    }
}
