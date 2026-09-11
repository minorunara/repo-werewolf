using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Werewolf.Core
{
    public enum WLogLevel
    {
        Debug = 0,
        Warning = 1,
        Error = 2,
    }

    public static class WLog
    {
        public const string Prefix = "[WW]";

        public static Action<string, bool> Sink;

        public static void Event(string dir, int code, string target, object[] payload,
            bool secret = false, int[] targetActors = null, int? transitMs = null)
        {
            if (Sink == null) return;

            var sb = new StringBuilder(Prefix)
                .Append(" dir=").Append(dir)
                .Append(" code=").Append(code.ToString(CultureInfo.InvariantCulture))
                .Append(" target=").Append(target);

            if (targetActors != null)
            {
                sb.Append(" targetActors=");
                AppendValue(sb, targetActors);
            }

            if (transitMs.HasValue)
            {
                sb.Append(" transitMs=").Append(transitMs.Value.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append(" payload=");
            AppendValue(sb, payload ?? Array.Empty<object>());

            Emit(sb, secret);
        }

        public static void Phase(GamePhase from, GamePhase to, string reason)
        {
            if (Sink == null) return;

            var sb = new StringBuilder(Prefix)
                .Append(" phase ").Append(from).Append("->").Append(to)
                .Append(" reason=").Append(reason);

            Emit(sb, secret: false);
        }

        public static void Line(string kind, bool secret, params (string Key, object Value)[] fields)
        {
            if (Sink == null) return;

            var sb = new StringBuilder(Prefix).Append(' ').Append(kind);

            if (fields != null)
            {
                foreach (var (key, value) in fields)
                {
                    sb.Append(' ').Append(key).Append('=');
                    AppendValue(sb, value);
                }
            }

            Emit(sb, secret);
        }

        internal static WLogLevel LevelOf(string line)
        {
            string kind = KindOf(line);

            if (kind == "warn" || kind.EndsWith("_warning", StringComparison.Ordinal))
                return WLogLevel.Warning;

            if (kind.EndsWith("_error", StringComparison.Ordinal)
                || kind.EndsWith("_failed", StringComparison.Ordinal)
                || kind.EndsWith("_fail", StringComparison.Ordinal)
                || kind.EndsWith("_invalid", StringComparison.Ordinal)
                || kind == "game_refs_disabled"
                || kind == "harmony_disabled")
                return WLogLevel.Error;

            return WLogLevel.Debug;
        }

        internal static string KindOf(string line)
        {
            if (string.IsNullOrEmpty(line) || !line.StartsWith(Prefix, StringComparison.Ordinal))
                return "unknown";

            int start = Prefix.Length;
            while (start < line.Length && line[start] == ' ') start++;
            if (start >= line.Length) return "unknown";

            int end = line.IndexOf(' ', start);
            return end < 0 ? line.Substring(start) : line.Substring(start, end - start);
        }

        private static void Emit(StringBuilder sb, bool secret)
        {
            if (secret) sb.Append(" secret=1");

            try
            {
                Sink?.Invoke(sb.ToString(), secret);
            }
            catch
            {
            }
        }

        private static void AppendValue(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    return;

                case bool b:
                    sb.Append(b ? "true" : "false");
                    return;

                case string s:
                    if (s.Length == 0 || s.IndexOf(' ') >= 0 || s.IndexOf('=') >= 0 || s.IndexOf('"') >= 0)
                    {
                        sb.Append('"').Append(s.Replace("\"", "\\\"")).Append('"');
                    }
                    else
                    {
                        sb.Append(s);
                    }
                    return;

                case IEnumerable items:
                    sb.Append('[');
                    bool first = true;
                    foreach (object item in items)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        AppendValue(sb, item);
                    }
                    sb.Append(']');
                    return;

                case IFormattable f:
                    sb.Append(f.ToString(null, CultureInfo.InvariantCulture));
                    return;

                default:
                    sb.Append(value);
                    return;
            }
        }
    }
}
