#nullable enable
using System;

namespace Werewolf.Core
{
    public static class LanguagePolicy
    {
        public const string EnglishLocaleCode = "en-US";

        public readonly struct Choice
        {
            public Choice(Language language, string fileCode)
            {
                Language = language;
                FileCode = fileCode;
            }

            public Language Language { get; }

            public string FileCode { get; }
        }

        public static Choice Resolve(string? gameLocaleCode)
        {
            string code = NormalizeLocaleCode(gameLocaleCode);
            bool japanese = code.StartsWith("ja", StringComparison.OrdinalIgnoreCase)
                            && (code.Length == 2 || code[2] == '-');
            return new Choice(japanese ? Language.Japanese : Language.English, code);
        }

        public static string NormalizeLocaleCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return EnglishLocaleCode;
            var sb = new System.Text.StringBuilder(code!.Length);
            foreach (char c in code.Trim())
            {
                if (c == '_' || c == '-') sb.Append('-');
                else if (char.IsLetterOrDigit(c) && c < 128) sb.Append(c);
            }
            return sb.Length == 0 ? EnglishLocaleCode : sb.ToString();
        }
    }
}
