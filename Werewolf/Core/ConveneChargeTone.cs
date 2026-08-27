using System;

namespace Werewolf.Core
{
    public static class ConveneChargeTone
    {
        public const float StartPitch = 0.75f;
        public const float PitchOctaves = 1f;
        public const float StartVolume = 0.40f;
        public const float EndVolume = 0.60f;

        public static float PitchFor(float ratio)
        {
            float t = MathUtil.Clamp01(ratio);
            return StartPitch * (float)Math.Pow(2d, PitchOctaves * t);
        }

        public static float VolumeFor(float ratio)
        {
            float t = MathUtil.Clamp01(ratio);
            return StartVolume + (EndVolume - StartVolume) * t;
        }
    }
}
