using System;

namespace Werewolf.Core.Replay
{
    public static class ReplayMarkerPalette
    {
        public const double GoldenAngleDeg = 137.508;
        public const double Saturation = 0.62;
        public const double Lightness = 0.60;

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
