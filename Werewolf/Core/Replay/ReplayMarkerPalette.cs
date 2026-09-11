using System;
using System.Globalization;

namespace Werewolf.Core.Replay
{
    public static class ReplayMarkerPalette
    {
        public const double GoldenAngleDeg = 137.508;
        public const double Saturation = 0.62;
        public const double Lightness = 0.60;

        public const float DarkLuminance = 0.45f;

        public static void ColorFor(int participantId, string hex, out float r, out float g, out float b)
        {
            if (TryParseHex(hex, out r, out g, out b)) return;
            ColorFor(participantId, out r, out g, out b);
        }

        public static void ColorFor(int participantId, out float r, out float g, out float b)
        {
            if (participantId <= 0)
            {
                r = g = b = 0.62f;
                return;
            }
            double hue = participantId * GoldenAngleDeg % 360.0;
            HslToRgb(hue, Saturation, Lightness, out r, out g, out b);
        }

        public static bool IsDark(float r, float g, float b)
        {
            return 0.2126f * r + 0.7152f * g + 0.0722f * b < DarkLuminance;
        }

        public static string ToHex(float r, float g, float b)
        {
            return "#" + Byte(r).ToString("x2") + Byte(g).ToString("x2") + Byte(b).ToString("x2");
        }

        public static bool TryParseHex(string hex, out float r, out float g, out float b)
        {
            r = g = b = 0f;
            if (hex == null || hex.Length != 7 || hex[0] != '#') return false;
            if (!int.TryParse(hex.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb)) return false;
            r = ((rgb >> 16) & 0xFF) / 255f;
            g = ((rgb >> 8) & 0xFF) / 255f;
            b = (rgb & 0xFF) / 255f;
            return true;
        }

        private static int Byte(float v)
        {
            if (float.IsNaN(v)) return 0;
            return (int)Math.Round(Math.Max(0f, Math.Min(1f, v)) * 255f);
        }

        public static void HslToRgb(double hueDeg, double s, double l, out float r, out float g, out float b)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double hp = ((hueDeg % 360) + 360) % 360 / 60.0;
            double x = c * (1 - Math.Abs(hp % 2 - 1));
            double r1, g1, b1;
            if (hp < 1) { r1 = c; g1 = x; b1 = 0; }
            else if (hp < 2) { r1 = x; g1 = c; b1 = 0; }
            else if (hp < 3) { r1 = 0; g1 = c; b1 = x; }
            else if (hp < 4) { r1 = 0; g1 = x; b1 = c; }
            else if (hp < 5) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }
            double m = l - c / 2;
            r = (float)(r1 + m);
            g = (float)(g1 + m);
            b = (float)(b1 + m);
        }
    }
}
