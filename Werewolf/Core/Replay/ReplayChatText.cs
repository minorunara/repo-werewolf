using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Werewolf.Core.Replay
{
    public static class ReplayChatText
    {
        public const int MaxDisplayChars = 50;
        public const int SingleLineMax = 30;
        public const int WrapTarget = 25;

        public const double SlideInSec = 0.22;
        public const double SlideOutSec = 0.28;
        public const double DwellBaseSec = 1.25;
        public const double DwellPerCharSec = 0.025;
        public const double DwellMaxSec = 2.5;

        private const char Zwj = (char)0x200D;

        private const string LineHeadForbidden = "、。，．！？：；）」』】〉》・ーぁぃぅぇぉっゃゅょゎ々ゝゞ…‥!?,.:;)";

        private const string LineTailForbidden = "（「『【〈《(";

        public static string SanitizeForRecord(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (c == '<') { sb.Append('＜'); continue; }
                if (c == '\n' || c == '\r' || c == '\t') { sb.Append(' '); continue; }
                if (char.IsControl(c)) continue;
                sb.Append(c);
            }
            return TruncateDisplay(sb.ToString().Trim(), MaxDisplayChars);
        }

        public static int DisplayLength(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int count = 0;
            for (int i = 0; i < s.Length; i += DisplayCharLength(s, i)) count++;
            return count;
        }

        public static string TruncateDisplay(string s, int maxDisplayChars)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            int count = 0;
            int i = 0;
            while (i < s.Length)
            {
                if (count == maxDisplayChars) return s.Substring(0, i);
                i += DisplayCharLength(s, i);
                count++;
            }
            return s;
        }

        public static (string Line1, string Line2) Wrap(string text)
        {
            if (string.IsNullOrEmpty(text)) return (string.Empty, null);

            var starts = new List<int>(text.Length + 1);
            for (int i = 0; i < text.Length; i += DisplayCharLength(text, i)) starts.Add(i);
            starts.Add(text.Length);
            int n = starts.Count - 1;
            if (n <= SingleLineMax) return (text, null);

            int split = ChooseSplit(text, starts);
            return (text.Substring(0, starts[split]), text.Substring(starts[split]));
        }

        public static double DwellSeconds(int displayChars)
        {
            double d = DwellBaseSec + displayChars * DwellPerCharSec;
            if (d < DwellBaseSec) d = DwellBaseSec;
            return d > DwellMaxSec ? DwellMaxSec : d;
        }

        public static double TotalSeconds(int displayChars)
            => SlideInSec + DwellSeconds(displayChars) + SlideOutSec;

        private static int ChooseSplit(string text, List<int> starts)
        {
            int[] candidates = { WrapTarget, WrapTarget + 1, WrapTarget - 1 };
            foreach (int cand in candidates)
            {
                if (ViolatesKinsoku(text, starts, cand)) continue;
                return cand;
            }
            return WrapTarget;
        }

        private static bool ViolatesKinsoku(string text, List<int> starts, int split)
        {
            char head = text[starts[split]];
            if (LineHeadForbidden.IndexOf(head) >= 0) return true;
            char tail = text[starts[split - 1]];
            if (LineTailForbidden.IndexOf(tail) >= 0) return true;
            return false;
        }

        private static int DisplayCharLength(string s, int index)
        {
            int i = index + CodePointLength(s, index);
            while (i < s.Length)
            {
                if (IsExtender(s, i))
                {
                    i += CodePointLength(s, i);
                    continue;
                }
                if (s[i] == Zwj && i + 1 < s.Length)
                {
                    i += 1 + CodePointLength(s, i + 1);
                    continue;
                }
                break;
            }
            return i - index;
        }

        private static int CodePointLength(string s, int index)
            => char.IsHighSurrogate(s[index]) && index + 1 < s.Length && char.IsLowSurrogate(s[index + 1])
                ? 2
                : 1;

        private static bool IsExtender(string s, int index)
        {
            char c = s[index];
            if (c >= (char)0xFE00 && c <= (char)0xFE0F) return true;
            if (char.IsHighSurrogate(c) && index + 1 < s.Length && char.IsLowSurrogate(s[index + 1]))
            {
                int cp = char.ConvertToUtf32(c, s[index + 1]);
                if (cp >= 0xE0100 && cp <= 0xE01EF) return true;
                if (cp >= 0x1F3FB && cp <= 0x1F3FF) return true;
            }
            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(s, index);
            return cat == UnicodeCategory.NonSpacingMark
                || cat == UnicodeCategory.SpacingCombiningMark
                || cat == UnicodeCategory.EnclosingMark;
        }
    }
}
