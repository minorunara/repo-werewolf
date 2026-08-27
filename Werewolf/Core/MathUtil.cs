namespace Werewolf.Core
{
    public static class MathUtil
    {
        public static float Clamp01(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);

        public static double Clamp01(double value) => value < 0d ? 0d : (value > 1d ? 1d : value);
    }
}
