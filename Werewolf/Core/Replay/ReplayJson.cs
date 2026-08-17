using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Werewolf.Core.Replay
{
    public static class ReplayJson
    {
        public static string F2(double value)
            => value.ToString("0.##", CultureInfo.InvariantCulture);

        public static void AppendEscaped(StringBuilder sb, string value)
        {
            sb.Append('"');
            if (value != null)
            {
                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20)
                            {
                                sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                sb.Append(c);
                            }
                            break;
                    }
                }
            }
            sb.Append('"');
        }

        public static void AppendValue(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    break;
                case string s:
                    AppendEscaped(sb, s);
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case float f:
                    sb.Append(F2(f));
                    break;
                case double d:
                    sb.Append(F2(d));
                    break;
                case byte or sbyte or short or ushort or int or uint or long:
                    sb.Append(((System.IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
                    break;
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
                    break;
                default:
                    AppendEscaped(sb, value.ToString());
                    break;
            }
        }

        internal static string HeaderLine(ReplaySegmentHeader header)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"k\":\"seg\",\"fv\":").Append(ReplayRecorder.FormatVersion);
            sb.Append(",\"lvl\":");
            AppendEscaped(sb, header.LevelName);
            sb.Append(",\"t0\":");
            AppendEscaped(sb, header.StartedAtIso);
            sb.Append(",\"host\":").Append(header.IsHost ? "true" : "false");
            sb.Append(",\"me\":").Append(header.LocalActor);

            sb.Append(",\"players\":[");
            for (int i = 0; i < header.Players.Count; i++)
            {
                ReplayPlayerInfo p = header.Players[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"a\":").Append(p.Actor).Append(",\"id\":").Append(p.ParticipantId).Append(",\"n\":");
                AppendEscaped(sb, p.Name);
                sb.Append('}');
            }
            sb.Append(']');

            sb.Append(",\"eps\":[");
            for (int i = 0; i < header.ExtractionPoints.Count; i++)
            {
                ReplayEpInfo ep = header.ExtractionPoints[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"e\":").Append(ep.Id).Append(",\"s\":").Append(ep.State).Append(",\"n\":");
                AppendEscaped(sb, ep.StateName);
                AppendXyz(sb, ep.X, ep.Y, ep.Z);
                sb.Append('}');
            }
            sb.Append(']');

            sb.Append(",\"vals\":[");
            for (int i = 0; i < header.Valuables.Count; i++)
            {
                ReplayValuableInfo v = header.Valuables[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"v\":").Append(v.Id).Append(",\"n\":");
                AppendEscaped(sb, v.Name);
                sb.Append(",\"$\":").Append(v.Dollars);
                if (v.Vid != 0) sb.Append(",\"vid\":").Append(v.Vid);
                AppendXyz(sb, v.X, v.Y, v.Z);
                sb.Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        internal static string PosLine(double t, List<ReplayEntitySample> entries)
        {
            var sb = new StringBuilder(64 + entries.Count * 24);
            sb.Append("{\"k\":\"p\",\"t\":").Append(F2(t));
            AppendKindArray(sb, entries, ReplayEntityKind.Player, "P");
            AppendKindArray(sb, entries, ReplayEntityKind.Enemy, "E");
            AppendKindArray(sb, entries, ReplayEntityKind.Cart, "C");
            AppendKindArray(sb, entries, ReplayEntityKind.Valuable, "V");
            AppendKindArray(sb, entries, ReplayEntityKind.Item, "I");
            AppendKindArray(sb, entries, ReplayEntityKind.Corpse, "D");
            sb.Append('}');
            return sb.ToString();
        }

        internal static string EventLine(double t, string name, (string Key, object Value)[] fields)
        {
            var sb = new StringBuilder(64);
            sb.Append("{\"k\":\"ev\",\"t\":").Append(F2(t)).Append(",\"e\":");
            AppendEscaped(sb, name);
            if (fields != null)
            {
                foreach ((string key, object value) in fields)
                {
                    sb.Append(',');
                    AppendEscaped(sb, key);
                    sb.Append(':');
                    AppendValue(sb, value);
                }
            }
            sb.Append('}');
            return sb.ToString();
        }

        internal static string EntityLine(double t, ReplayEntityKind kind, int id, string name, int vid = 0)
        {
            var sb = new StringBuilder(64);
            sb.Append("{\"k\":\"ent\",\"t\":").Append(F2(t)).Append(",\"kind\":\"").Append(KindLetter(kind));
            sb.Append("\",\"id\":").Append(id).Append(",\"n\":");
            AppendEscaped(sb, name);
            if (vid != 0) sb.Append(",\"vid\":").Append(vid);
            sb.Append('}');
            return sb.ToString();
        }

        internal static string SegEndLine(double t)
            => "{\"k\":\"segend\",\"t\":" + F2(t) + "}";

        internal static string LedgerLine(IReadOnlyList<ReplayLossEntry> entries)
        {
            var sb = new StringBuilder(24 + entries.Count * 16);
            sb.Append("{\"k\":\"ledger\",\"n\":[");
            for (int i = 0; i < entries.Count; i++)
            {
                ReplayLossEntry e = entries[i];
                if (i > 0) sb.Append(',');
                sb.Append('[').Append(e.Vid).Append(',').Append(e.Dollars).Append(',')
                  .Append(e.IsOrb ? 1 : 0).Append(',').Append(e.Destroyed ? 1 : 0).Append(']');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        internal static string MapImageLine(ReplayMapImage image)
        {
            var sb = new StringBuilder(96 + (image.Png.Length / 3 + 1) * 4);
            sb.Append("{\"k\":\"mapimg\",\"w\":").Append(image.Width.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"h\":").Append(image.Height.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"x0\":").Append(F2(image.MinX));
            sb.Append(",\"x1\":").Append(F2(image.MaxX));
            sb.Append(",\"z0\":").Append(F2(image.MinZ));
            sb.Append(",\"z1\":").Append(F2(image.MaxZ));
            sb.Append(",\"png\":\"").Append(System.Convert.ToBase64String(image.Png)).Append("\"}");
            return sb.ToString();
        }

        internal static string MapLine(ReplayMapMesh map)
        {
            var sb = new StringBuilder(32 + map.Vertices.Length * 7 + map.Triangles.Length * 5);
            sb.Append("{\"k\":\"map\",\"verts\":[");
            for (int i = 0; i < map.Vertices.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(F2(map.Vertices[i]));
            }
            sb.Append("],\"tris\":[");
            for (int i = 0; i < map.Triangles.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(map.Triangles[i].ToString(CultureInfo.InvariantCulture));
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendKindArray(
            StringBuilder sb, List<ReplayEntitySample> entries, ReplayEntityKind kind, string key)
        {
            bool any = false;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Kind != kind) continue;
                if (!any)
                {
                    sb.Append(",\"").Append(key).Append("\":[");
                    any = true;
                }
                else
                {
                    sb.Append(',');
                }
                ReplayEntitySample s = entries[i];
                sb.Append('[').Append(s.Id).Append(',').Append(F2(s.X)).Append(',')
                  .Append(F2(s.Y)).Append(',').Append(F2(s.Z)).Append(']');
            }
            if (any) sb.Append(']');
        }

        private static void AppendXyz(StringBuilder sb, float x, float y, float z)
        {
            sb.Append(",\"x\":").Append(F2(x));
            sb.Append(",\"y\":").Append(F2(y));
            sb.Append(",\"z\":").Append(F2(z));
        }

        private static string KindLetter(ReplayEntityKind kind)
        {
            switch (kind)
            {
                case ReplayEntityKind.Player: return "P";
                case ReplayEntityKind.Enemy: return "E";
                case ReplayEntityKind.Cart: return "C";
                case ReplayEntityKind.Valuable: return "V";
                case ReplayEntityKind.Corpse: return "D";
                default: return "I";
            }
        }
    }
}
