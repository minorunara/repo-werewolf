using System;
using System.Text;

namespace Werewolf.Core.Replay
{
    public static class ReplayExportNaming
    {
        public const string Prefix = "repo_werewolf_replay_";
        public const string Extension = ".jsonl";

        public static string FileName(ReplaySegmentHeader header)
        {
            string stamp = FormatStamp(header?.StartedAtIso);
            string level = SanitizeLevel(header?.LevelName);
            return Prefix + stamp + "_" + level + Extension;
        }

        public static string FormatStamp(string startedAtIso)
        {
            if (string.IsNullOrEmpty(startedAtIso)) return "unknown";
            var digits = new StringBuilder(14);
            foreach (char c in startedAtIso)
            {
                if (c < '0' || c > '9') continue;
                digits.Append(c);
                if (digits.Length == 14) break;
            }
            if (digits.Length < 14) return "unknown";
            digits.Insert(8, '_');
            return digits.ToString();
        }

        public static string SanitizeLevel(string levelName)
        {
            string name = levelName ?? "";
            const string vanillaPrefix = "Level - ";
            if (name.StartsWith(vanillaPrefix, StringComparison.Ordinal))
            {
                name = name.Substring(vanillaPrefix.Length);
            }

            var sb = new StringBuilder(name.Length);
            bool lastUnderscore = false;
            foreach (char c in name)
            {
                bool safe = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')
                    || (c >= '0' && c <= '9') || c == '-';
                if (safe)
                {
                    sb.Append(c);
                    lastUnderscore = false;
                }
                else if (!lastUnderscore)
                {
                    sb.Append('_');
                    lastUnderscore = true;
                }
            }
            string result = sb.ToString().Trim('_');
            return result.Length > 0 ? result : "level";
        }
    }
}
