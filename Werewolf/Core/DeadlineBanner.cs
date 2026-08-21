using System;

namespace Werewolf.Core
{
    public static class DeadlineBanner
    {

        public const float SlideInSec = 0.320f;

        public const float Line2StaggerSec = 0f;

        public const float GhostOpacity = 0.60f;

        public const float GhostFadeSec = 1.600f;

        public const float PopGhostFadeSec = 0.900f;

        public const float PopGhostScale = 3.5f;

        public const float PopDelaySec = 0.250f;

        public const float PopSec = 0.150f;

        public const float PopScale = 1.80f;

        public const float StartOpacity = 0.20f;

        public const float FadeInSec = 4.300f;

        public const float HoldSec = 3.900f;

        public const float ExitSec = 0.300f;

        public const float ExitScale = 3.0f;

        public const float Line1WidthPx = 1000f;

        public const float Line2WidthPx = 700f;

        public const float RowOffsetY = 97f;

        public const float ReferenceWidthPx = 1920f;

        public const float OffscreenMarginPx = 120f;

        public const int EmojiCount = 1;

        public const float EmojiSizePx = 320f;

        public const float EmojiPeakAlpha = 1.0f;

        public const float EmojiFadeInSec = 3.5f;

        public const float EmojiAppearPx = 2000f;

        public const float EmojiDelaySec = 1.0f;

        public const float EmojiStaggerSec = 0.8f;

        public const float EmojiSpeedPxPerSec = 480f;

        public const float EmojiBandHeightPx = 320f;

        private const float BackOutC1 = 1.70158f;

        public const float RowTotalSec = SlideInSec + HoldSec + ExitSec;

        public const float TotalSec = Line2StaggerSec + RowTotalSec;

        public static float StartOffsetX(float restWidthPx, bool fromLeft)
        {
            float distance = ReferenceWidthPx * 0.5f + restWidthPx * 0.5f + OffscreenMarginPx;
            return fromLeft ? -distance : distance;
        }

        public static BannerRowState Compute(float t, float startOffsetX)
        {
            if (t < 0f) return BannerRowState.AllHidden;

            float ramp = RampAlpha(t);
            float sinceStop = t - SlideInSec;

            BannerLayerState main;
            if (t < SlideInSec)
            {
                main = new BannerLayerState(true, startOffsetX * (1f - t / SlideInSec), 1f, ramp);
            }
            else
            {
                float scale = 1f;
                float alpha = ramp;
                bool visible = true;

                if (sinceStop >= PopDelaySec)
                {
                    float u = Clamp01((sinceStop - PopDelaySec) / PopSec);
                    scale = 1f + (PopScale - 1f) * EaseOutBack(u);
                }
                if (sinceStop >= HoldSec)
                {
                    float u = (sinceStop - HoldSec) / ExitSec;
                    if (u >= 1f)
                    {
                        visible = false;
                    }
                    else
                    {
                        scale = PopScale + (ExitScale - PopScale) * EaseOutCubic(u);
                        alpha = ramp * (1f - u);
                    }
                }
                main = visible ? new BannerLayerState(true, 0f, scale, alpha) : BannerLayerState.Hidden;
            }

            BannerLayerState slideGhost = BannerLayerState.Hidden;
            if (GhostOpacity > 0f && t < GhostFadeSec)
            {
                slideGhost = new BannerLayerState(
                    true,
                    startOffsetX * (1f - t / SlideInSec),
                    1f,
                    GhostOpacity * (1f - Pow15(t / GhostFadeSec)));
            }

            BannerLayerState popGhost = BannerLayerState.Hidden;
            float popGhostT = sinceStop - PopDelaySec;
            if (GhostOpacity > 0f && popGhostT >= 0f && popGhostT < PopGhostFadeSec && sinceStop < HoldSec)
            {
                float u = popGhostT / PopGhostFadeSec;
                popGhost = new BannerLayerState(
                    true, 0f,
                    1f + (PopGhostScale - 1f) * u,
                    GhostOpacity * (1f - Pow15(u)));
            }

            return new BannerRowState(main, slideGhost, popGhost);
        }

        public static BannerEmojiState ComputeEmoji(float t, int index)
        {
            float lt = t - (EmojiDelaySec + index * EmojiStaggerSec);
            float exitStart = Line2StaggerSec + SlideInSec + HoldSec;
            if (lt < 0f || t >= exitStart + ExitSec) return BannerEmojiState.Hidden;

            float travel = EmojiSpeedPxPerSec * lt;
            float span = ReferenceWidthPx + EmojiSizePx;
            if (travel > span) return BannerEmojiState.Hidden;

            float alpha = EmojiPeakAlpha;
            if (EmojiFadeInSec > 0f) alpha *= Clamp01(lt / EmojiFadeInSec);
            if (EmojiAppearPx > 0f) alpha *= Clamp01(travel / EmojiAppearPx);
            if (t >= exitStart) alpha *= 1f - (t - exitStart) / ExitSec;

            float dir = index % 2 == 0 ? 1f : -1f;
            float centerX = -dir * span * 0.5f + dir * travel;
            float lane = Frac(index * 0.61803f + 0.5f) - 0.5f;
            float centerY = -lane * EmojiBandHeightPx;
            float rotation = dir * travel / (EmojiSizePx * 0.5f);
            return new BannerEmojiState(true, centerX, centerY, rotation, alpha);
        }

        private static float Frac(float v) => v - (float)Math.Floor(v);

        public static float RampAlpha(float t)
        {
            if (t <= 0f) return StartOpacity;
            float u = Clamp01(t / FadeInSec);
            return StartOpacity + (1f - StartOpacity) * u;
        }

        private static float EaseOutBack(float t)
        {
            const float c3 = BackOutC1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + BackOutC1 * p * p;
        }

        private static float EaseOutCubic(float t)
        {
            float p = 1f - t;
            return 1f - p * p * p;
        }

        private static float Pow15(float u) => (float)Math.Pow(u, 1.5);

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    public readonly struct BannerLayerState
    {
        public bool Visible { get; }

        public float OffsetX { get; }

        public float Scale { get; }

        public float Alpha { get; }

        public BannerLayerState(bool visible, float offsetX, float scale, float alpha)
        {
            Visible = visible;
            OffsetX = offsetX;
            Scale = scale;
            Alpha = alpha;
        }

        public static readonly BannerLayerState Hidden = new BannerLayerState(false, 0f, 1f, 0f);
    }

    public readonly struct BannerEmojiState
    {
        public bool Visible { get; }

        public float CenterX { get; }

        public float CenterY { get; }

        public float RotationRad { get; }

        public float Alpha { get; }

        public BannerEmojiState(bool visible, float centerX, float centerY, float rotationRad, float alpha)
        {
            Visible = visible;
            CenterX = centerX;
            CenterY = centerY;
            RotationRad = rotationRad;
            Alpha = alpha;
        }

        public static readonly BannerEmojiState Hidden = new BannerEmojiState(false, 0f, 0f, 0f, 0f);
    }

    public readonly struct BannerRowState
    {
        public BannerLayerState Main { get; }

        public BannerLayerState SlideGhost { get; }

        public BannerLayerState PopGhost { get; }

        public BannerRowState(BannerLayerState main, BannerLayerState slideGhost, BannerLayerState popGhost)
        {
            Main = main;
            SlideGhost = slideGhost;
            PopGhost = popGhost;
        }

        public static readonly BannerRowState AllHidden = new BannerRowState(
            BannerLayerState.Hidden, BannerLayerState.Hidden, BannerLayerState.Hidden);
    }
}
