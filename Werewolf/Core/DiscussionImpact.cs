using System;

namespace Werewolf.Core
{
    public static class DiscussionImpact
    {

        public const float SlideSec = 0.180f;

        public const float HoldSec = 0.975f;

        public const float FlySec = 0.900f;

        public const float TotalSec = SlideSec + HoldSec + FlySec;

        public const float ImpactSec = SlideSec;

        public const float StartOffsetXPx = 1180f;

        public const float SlidePow = 2.2f;

        public const float RecoilPx = 22f;

        public const float RecoilSec = 0.180f;

        public const float FontSizePx = 130f;

        public const float GapPx = 30f;

        public const float CharOffsetPx = 40f;

        public const float JitterHoldRatio = 0.5f;

        public const float JitterSettleSec = 0.250f;

        public static readonly float[] CharPattern =
            { 1f, -0.78f, 0.9f, -1f, 0.62f, -0.86f, 0.74f, -0.66f, 0.94f, -0.72f };

        public const int MaxCharUnitsPerWord = 8;

        public const float WordOffsetPx = 16f;

        public const float WordTiltDeg = 4f;

        public const float FlyDistPx = 920f;

        public const float StrokeWidthPx = 1.5f;

        public const float HaloInnerWidthPx = 10f;

        public const float HaloOuterWidthPx = 10f;

        public const float HaloInnerAlpha = 0.30f;

        public const float HaloOuterAlpha = 0.20f;

        public static ImpactState Compute(float t)
        {
            if (t < 0f || t >= TotalSec) return ImpactState.Hidden;

            float slideOffset = t < SlideSec
                ? StartOffsetXPx * (1f - (float)Math.Pow(t / SlideSec, SlidePow))
                : 0f;

            float since = t - SlideSec;

            float recoil = 0f;
            if (since >= 0f && since < RecoilSec)
            {
                recoil = RecoilPx * (float)Math.Sin(Math.PI * (since / RecoilSec));
            }

            float jitterK = 0f;
            if (since >= 0f)
            {
                float decay = (float)Math.Exp(-since / (JitterSettleSec / 3f));
                jitterK = JitterHoldRatio + (1f - JitterHoldRatio) * decay;
            }

            float flyOffset = 0f;
            float alpha = 1f;
            float flyStart = SlideSec + HoldSec;
            if (t >= flyStart)
            {
                float u = Clamp01((t - flyStart) / FlySec);
                float ease = 1f - (1f - u) * (1f - u);
                flyOffset = FlyDistPx * ease;
                alpha = Math.Max(0f, 1f - (float)Math.Pow(u, 1.5));
            }

            return new ImpactState(true, slideOffset, recoil, flyOffset, jitterK, alpha);
        }

        public static float CharOffsetY(int index, float jitterK)
        {
            if (index < 0) return 0f;
            return CharOffsetPx * CharPattern[index % CharPattern.Length] * jitterK;
        }

        public static bool IsSquareScript(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            bool any = false;
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c)) continue;
                any = true;
                if (!IsSquareScriptChar(c)) return false;
            }
            return any;
        }

        private static bool IsSquareScriptChar(char c)
        {
            return (c >= '\u3040' && c <= '\u30FF')
                || (c >= '\u31F0' && c <= '\u31FF')
                || (c >= '\u3130' && c <= '\u318F')
                || (c >= '\u3400' && c <= '\u4DBF')
                || (c >= '\u4E00' && c <= '\u9FFF')
                || (c >= '\uF900' && c <= '\uFAFF')
                || (c >= '\uAC00' && c <= '\uD7A3');
        }

        public static ImpactJitterUnit ResolveUnit(string left, string right)
        {
            if (!IsSquareScript(left) || !IsSquareScript(right)) return ImpactJitterUnit.Word;
            if (CountUnits(left) > MaxCharUnitsPerWord) return ImpactJitterUnit.Word;
            if (CountUnits(right) > MaxCharUnitsPerWord) return ImpactJitterUnit.Word;
            return ImpactJitterUnit.Char;
        }

        public static int CountUnits(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int n = 0;
            foreach (char c in text)
            {
                if (!char.IsWhiteSpace(c)) n++;
            }
            return n;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    public enum ImpactJitterUnit
    {
        Char = 0,

        Word = 1,
    }

    public readonly struct ImpactState
    {
        public bool Visible { get; }

        public float SlideOffsetX { get; }

        public float Recoil { get; }

        public float FlyOffset { get; }

        public float JitterK { get; }

        public float Alpha { get; }

        public ImpactState(bool visible, float slideOffsetX, float recoil,
            float flyOffset, float jitterK, float alpha)
        {
            Visible = visible;
            SlideOffsetX = slideOffsetX;
            Recoil = recoil;
            FlyOffset = flyOffset;
            JitterK = jitterK;
            Alpha = alpha;
        }

        public static readonly ImpactState Hidden = new ImpactState(false, 0f, 0f, 0f, 0f, 0f);
    }
}
