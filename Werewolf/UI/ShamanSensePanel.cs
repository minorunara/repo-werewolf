#nullable enable
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class ShamanSensePanel : IClientPanel
    {
        public string LayerName => "ShamanSense";

        private const float StormAlphaWeak = 0.04f;
        private const float StormAlphaMedium = 0.07f;
        private const float StormAlphaStrong = 0.10f;

        private const float GhostBurstAlpha = 0.45f;

        private const int NoiseFrames = 4;
        private const int NoiseSize = 96;
        private const float NoiseFrameSec = 0.10f;
        private const float NoiseTilesX = 2.5f;
        private const float NoiseTilesY = 1.40625f;
        private static readonly Vector2[] NoiseFrameOffsets =
        {
            new Vector2(0f, 0f),
            new Vector2(0.37f, 0.61f),
            new Vector2(0.73f, 0.19f),
            new Vector2(0.11f, 0.83f),
        };

        private const float TranceSaturation = -80;
        private const float PostProcessRefreshSec = 0.1f;
        private const float VignetteMaxAlpha = 0.45f;
        private const float TranceFadeInSec = 1.2f;
        private const float TranceFadeOutSec = 0.25f;
        private const int VignetteSize = 256;

        private const int RippleTextureSize = 256;
        private const float RippleDurationSec = 0.9f;
        private const float RippleStartSize = 80f;
        private const float RippleEndSize = 1400f;
        private const float RippleMaxAlpha = 0.38f;
        private static readonly Color RippleColor = new Color(0.72f, 0.86f, 1f);

        private sealed class ViewRefs
        {
            public ViewRefs(
                GameObject root,
                RawImage stormImage,
                RawImage vignetteImage,
                RawImage rippleImage,
                Texture2D vignetteTexture,
                Texture2D rippleTexture,
                Texture2D[] noiseTextures)
            {
                Root = root;
                StormImage = stormImage;
                VignetteImage = vignetteImage;
                RippleImage = rippleImage;
                VignetteTexture = vignetteTexture;
                RippleTexture = rippleTexture;
                NoiseTextures = noiseTextures;
            }

            public GameObject Root { get; }
            public RawImage StormImage { get; }
            public RawImage VignetteImage { get; }
            public RawImage RippleImage { get; }
            public Texture2D VignetteTexture { get; }
            public Texture2D RippleTexture { get; }
            public Texture2D[] NoiseTextures { get; }
            public float RippleRemainingSec { get; set; }
            public float TranceFade { get; set; }
            public int NoiseFrame { get; set; }
            public float NoiseClock { get; set; }
        }

        private ViewRefs? _view;

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            Texture2D? vignetteTexture = null;
            Texture2D? rippleTexture = null;
            Texture2D[]? noiseTextures = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                root = new GameObject("WW_ShamanSense", typeof(RectTransform));
                var rect = (RectTransform)root.transform;
                rect.SetParent(layerRoot, false);
                UiKit.Stretch(rect);

                var group = root.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;

                var vignetteGo = new GameObject("Vignette", typeof(RectTransform));
                var vignetteRect = (RectTransform)vignetteGo.transform;
                vignetteRect.SetParent(rect, false);
                UiKit.Stretch(vignetteRect);
                RawImage vignetteImage = vignetteGo.AddComponent<RawImage>();
                vignetteImage.raycastTarget = false;
                vignetteTexture = CreateVignetteTexture();
                vignetteImage.texture = vignetteTexture;
                vignetteImage.color = new Color(1f, 1f, 1f, 0f);
                vignetteImage.enabled = false;

                var rippleGo = new GameObject("DripRipple", typeof(RectTransform));
                var rippleRect = (RectTransform)rippleGo.transform;
                rippleRect.SetParent(rect, false);
                rippleRect.anchorMin = new Vector2(0.5f, 0.5f);
                rippleRect.anchorMax = new Vector2(0.5f, 0.5f);
                rippleRect.pivot = new Vector2(0.5f, 0.5f);
                rippleRect.anchoredPosition = Vector2.zero;
                rippleRect.sizeDelta = new Vector2(RippleStartSize, RippleStartSize);
                RawImage rippleImage = rippleGo.AddComponent<RawImage>();
                rippleImage.raycastTarget = false;
                rippleTexture = CreateRippleTexture();
                rippleImage.texture = rippleTexture;
                rippleImage.color = new Color(RippleColor.r, RippleColor.g, RippleColor.b, 0f);
                rippleImage.enabled = false;

                var stormGo = new GameObject("Storm", typeof(RectTransform));
                var stormRect = (RectTransform)stormGo.transform;
                stormRect.SetParent(rect, false);
                UiKit.Stretch(stormRect);
                RawImage stormImage = stormGo.AddComponent<RawImage>();
                stormImage.raycastTarget = false;
                noiseTextures = CreateNoiseTextures();
                stormImage.texture = noiseTextures[0];
                stormImage.uvRect = new Rect(0f, 0f, NoiseTilesX, NoiseTilesY);
                stormImage.color = new Color(1f, 1f, 1f, 0f);
                stormImage.enabled = false;

                root.SetActive(false);
                _view = new ViewRefs(root, stormImage, vignetteImage, rippleImage,
                    vignetteTexture, rippleTexture, noiseTextures);
            }
            catch
            {
                _view = null;
                if (root != null)
                {
                    root.SetActive(false);
                    Object.Destroy(root);
                }
                DestroyTexture(vignetteTexture);
                DestroyTexture(rippleTexture);
                DestroyTextures(noiseTextures);
                throw;
            }
        }

        public void PlayDripRipple()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            view.RippleRemainingSec = RippleDurationSec;
            view.RippleImage.rectTransform.sizeDelta = new Vector2(RippleStartSize, RippleStartSize);
            view.RippleImage.color = new Color(RippleColor.r, RippleColor.g, RippleColor.b, 0f);
            view.RippleImage.enabled = true;
            if (!view.Root.activeSelf) view.Root.SetActive(true);
        }

        public void Tick(ShamanStormTier tier, bool ghostBurst, bool gazeArmed, float deltaSeconds)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            float alpha = ghostBurst ? GhostBurstAlpha : StormAlpha(tier);
            TickDripRipple(view, deltaSeconds);

            if (gazeArmed)
            {
                VisualEffects.OverrideSaturation(
                    TranceSaturation,
                    1f / TranceFadeInSec,
                    1f / TranceFadeOutSec,
                    PostProcessRefreshSec,
                    view.Root);
            }

            float fadeRate = gazeArmed ? 1f / TranceFadeInSec : 1f / TranceFadeOutSec;
            view.TranceFade = Mathf.MoveTowards(view.TranceFade, gazeArmed ? 1f : 0f,
                fadeRate * deltaSeconds);

            if (alpha <= 0f && view.TranceFade <= 0f && view.RippleRemainingSec <= 0f)
            {
                if (view.Root.activeSelf) view.Root.SetActive(false);
                return;
            }
            if (!view.Root.activeSelf) view.Root.SetActive(true);

            float vignetteAlpha = view.TranceFade * VignetteMaxAlpha;
            view.VignetteImage.enabled = vignetteAlpha > 0f;
            view.VignetteImage.color = new Color(1f, 1f, 1f, vignetteAlpha);

            view.StormImage.enabled = alpha > 0f;
            if (alpha > 0f)
            {
                view.StormImage.color = new Color(1f, 1f, 1f, alpha);
                view.NoiseClock += deltaSeconds;
                if (view.NoiseClock >= NoiseFrameSec)
                {
                    view.NoiseClock = 0f;
                    view.NoiseFrame = (view.NoiseFrame + 1) % NoiseFrames;
                    view.StormImage.texture = view.NoiseTextures[view.NoiseFrame];
                    Vector2 offset = NoiseFrameOffsets[view.NoiseFrame];
                    view.StormImage.uvRect = new Rect(
                        offset.x, offset.y, NoiseTilesX, NoiseTilesY);
                }
            }
        }

        private static void TickDripRipple(ViewRefs view, float deltaSeconds)
        {
            if (view.RippleRemainingSec <= 0f) return;

            view.RippleRemainingSec = Mathf.Max(0f, view.RippleRemainingSec - deltaSeconds);
            float t = 1f - view.RippleRemainingSec / RippleDurationSec;
            float eased = 1f - (1f - t) * (1f - t);
            float size = Mathf.Lerp(RippleStartSize, RippleEndSize, eased);
            view.RippleImage.rectTransform.sizeDelta = new Vector2(size, size);

            float fadeIn = Mathf.Clamp01(t / 0.08f);
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, t);
            float rippleAlpha = RippleMaxAlpha * fadeIn * fadeOut;
            view.RippleImage.color = new Color(
                RippleColor.r, RippleColor.g, RippleColor.b, rippleAlpha);

            if (view.RippleRemainingSec <= 0f)
            {
                view.RippleImage.enabled = false;
            }
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            if (view.Root.activeSelf) view.Root.SetActive(false);
            ResetAnimation(view);
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view == null) return;
            if (view.Root != null) Object.Destroy(view.Root);
            DestroyOwnedTextures(view);
        }

        private static float StormAlpha(ShamanStormTier tier)
        {
            switch (tier)
            {
                case ShamanStormTier.Strong: return StormAlphaStrong;
                case ShamanStormTier.Medium: return StormAlphaMedium;
                case ShamanStormTier.Weak: return StormAlphaWeak;
                default: return 0f;
            }
        }

        private static Texture2D CreateVignetteTexture()
        {
            Texture2D? tex = null;
            try
            {
                tex = new Texture2D(VignetteSize, VignetteSize, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                var pixels = new Color32[VignetteSize * VignetteSize];
                const float inner = 0.55f;
                const float outer = 1.25f;
                for (int y = 0; y < VignetteSize; y++)
                {
                    float dy = (y / (float)(VignetteSize - 1)) * 2f - 1f;
                    for (int x = 0; x < VignetteSize; x++)
                    {
                        float dx = (x / (float)(VignetteSize - 1)) * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float t = Mathf.Clamp01((d - inner) / (outer - inner));
                        t = t * t * (3f - 2f * t);
                        pixels[y * VignetteSize + x] =
                            new Color32(0, 0, 0, (byte)(t * 255f));
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                tex.name = "WW_ShamanVignette";
                return tex;
            }
            catch
            {
                DestroyTexture(tex);
                throw;
            }
        }

        private static Texture2D CreateRippleTexture()
        {
            Texture2D? tex = null;
            try
            {
                tex = new Texture2D(
                    RippleTextureSize, RippleTextureSize, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                var pixels = new Color32[RippleTextureSize * RippleTextureSize];

                for (int y = 0; y < RippleTextureSize; y++)
                {
                    float dy = y / (float)(RippleTextureSize - 1) * 2f - 1f;
                    for (int x = 0; x < RippleTextureSize; x++)
                    {
                        float dx = x / (float)(RippleTextureSize - 1) * 2f - 1f;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float outer = Mathf.Clamp01(
                            1f - Mathf.Abs(distance - 0.78f) / 0.025f);
                        float inner = Mathf.Clamp01(
                            1f - Mathf.Abs(distance - 0.61f) / 0.018f) * 0.45f;
                        byte ringAlpha = (byte)(Mathf.Clamp01(outer + inner) * 255f);
                        pixels[y * RippleTextureSize + x] =
                            new Color32(255, 255, 255, ringAlpha);
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                tex.name = "WW_ShamanDripRipple";
                return tex;
            }
            catch
            {
                DestroyTexture(tex);
                throw;
            }
        }

        private static Texture2D[] CreateNoiseTextures()
        {
            var textures = new Texture2D[NoiseFrames];
            try
            {
                for (int f = 0; f < NoiseFrames; f++)
                {
                    Texture2D? tex = null;
                    try
                    {
                        tex = new Texture2D(
                            NoiseSize, NoiseSize, TextureFormat.RGBA32, false);
                        tex.wrapMode = TextureWrapMode.Repeat;
                        tex.filterMode = FilterMode.Point;
                        var pixels = new Color32[NoiseSize * NoiseSize];
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            byte v = (byte)Random.Range(0, 256);
                            pixels[i] = new Color32(v, v, v, 255);
                        }
                        tex.SetPixels32(pixels);
                        tex.Apply(false, false);
                        tex.name = "WW_ShamanNoise_" + f;
                        textures[f] = tex;
                        tex = null;
                    }
                    finally
                    {
                        DestroyTexture(tex);
                    }
                }
                return textures;
            }
            catch
            {
                DestroyTextures(textures);
                throw;
            }
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            DestroyOwnedTextures(view);
            return null;
        }

        private static void ResetAnimation(ViewRefs view)
        {
            view.TranceFade = 0f;
            view.RippleRemainingSec = 0f;
            view.RippleImage.color = new Color(RippleColor.r, RippleColor.g, RippleColor.b, 0f);
            view.RippleImage.enabled = false;
            view.VignetteImage.color = new Color(1f, 1f, 1f, 0f);
            view.VignetteImage.enabled = false;
        }

        private static void DestroyOwnedTextures(ViewRefs view)
        {
            DestroyTexture(view.VignetteTexture);
            DestroyTexture(view.RippleTexture);
            DestroyTextures(view.NoiseTextures);
        }

        private static void DestroyTextures(Texture2D[]? textures)
        {
            if (textures == null) return;
            foreach (Texture2D tex in textures) DestroyTexture(tex);
        }

        private static void DestroyTexture(Texture2D? texture)
        {
            if (texture != null) Object.Destroy(texture);
        }
    }
}
