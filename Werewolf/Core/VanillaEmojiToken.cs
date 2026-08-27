using System;
using System.Collections.Generic;
using System.Text;

namespace Werewolf.Core
{
    public static class VanillaEmojiToken
    {
        public const string Prefix = "[[";
        public const string Suffix = "]]";

        public static string Wrap(string name) => Prefix + (name ?? string.Empty) + Suffix;

        public static bool TryReadAt(string text, int index, out string name, out int next)
        {
            name = null;
            next = index;
            if (text == null || index < 0 || index + Prefix.Length > text.Length) return false;
            if (string.CompareOrdinal(text, index, Prefix, 0, Prefix.Length) != 0) return false;

            int nameStart = index + Prefix.Length;
            int close = text.IndexOf(Suffix, nameStart, StringComparison.Ordinal);
            if (close < 0) return false;

            name = text.Substring(nameStart, close - nameStart);
            next = close + Suffix.Length;
            return true;
        }

        public static bool TryParseWhole(string text, out string name)
        {
            if (TryReadAt(text, 0, out name, out int next) && next == text.Length) return true;
            name = null;
            return false;
        }

        public static IEnumerable<string> Names(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            for (int i = 0; i + Prefix.Length <= text.Length; i++)
            {
                if (string.CompareOrdinal(text, i, Prefix, 0, Prefix.Length) != 0) continue;
                if (!TryReadAt(text, i, out string name, out int next))
                {
                    yield return null;
                    yield break;
                }
                yield return name;
                i = next - 1;
            }
        }

        public static string Strip(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

            StringBuilder sb = null;
            int copied = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (!TryReadAt(text, i, out _, out int next)) continue;
                if (sb == null) sb = new StringBuilder(text.Length);
                sb.Append(text, copied, i - copied);
                copied = next;
                i = next - 1;
            }
            if (sb == null) return text;
            sb.Append(text, copied, text.Length - copied);
            return sb.ToString();
        }
    }
}
