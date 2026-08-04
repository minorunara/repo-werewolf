using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public static class LangFile
    {
        public static Dictionary<TextId, string> Parse(string content)
        {
            var result = new Dictionary<TextId, string>();
            if (string.IsNullOrEmpty(content)) return result;

            var lines = content.Split('\n');
            foreach (var rawLine in lines)
            {
                string line = rawLine;
                if (line.Length > 0 && line[line.Length - 1] == '\r')
                {
                    line = line.Substring(0, line.Length - 1);
                }
                if (line.Length == 0) continue;
                if (line[0] == '#') continue;

                int eq = line.IndexOf('=');
                if (eq < 0) continue;

                string key = line.Substring(0, eq).Trim();
                if (key.Length == 0) continue;

                if (!Enum.TryParse<TextId>(key, out var id) || id.ToString() != key)
                {
                    continue;
                }

                string rawValue = line.Substring(eq + 1);
                result[id] = Unescape(rawValue);
            }
            return result;
        }

        private static string Unescape(string value)
        {
            if (value.IndexOf('\\') < 0) return value;

            var sb = new System.Text.StringBuilder(value.Length);
            int i = 0;
            while (i < value.Length)
            {
                char c = value[i];
                if (c == '\\' && i + 1 < value.Length)
                {
                    char next = value[i + 1];
                    if (next == 'n')
                    {
                        sb.Append('\n');
                        i += 2;
                        continue;
                    }
                    if (next == '\\')
                    {
                        sb.Append('\\');
                        i += 2;
                        continue;
                    }
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }
    }
}
