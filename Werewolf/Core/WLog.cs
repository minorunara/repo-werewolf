using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Werewolf.Core
{
    public static class WLog
    {
        public const string Prefix = "[WW]";

        public static Action<string, bool> Sink;

        public static void Event(string dir, int code, string target, object[] payload,
            bool secret = false, int[] targetActors = null)
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
