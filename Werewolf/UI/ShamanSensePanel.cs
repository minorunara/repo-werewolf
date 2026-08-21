using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class ShamanSensePanel : IClientPanel
    {
        public string LayerName => "ShamanSense";

        private const float StormAlphaWeak = 0.08f;
        private const float StormAlphaMedium = 0.11f;
        private const float StormAlphaStrong = 0.15f;

        private const float GhostBurstAlpha = 0.45f;

        private const int NoiseFrames = 4;
        private const int NoiseSize = 192;
        private const float NoiseFrameSec = 0.06f;
        private const float NoiseTilesX = 2.5f;
        private const float NoiseTilesY = 1.40625f;

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

        private GameObject _root;
        private RawImage _stormImage;
        private RawImage _vignetteImage;
        private Texture2D _vignetteTexture;
        private RawImage _rippleImage;
        private Texture2D _rippleTexture;
        private float _rippleRemainingSec;
        private float _tranceFade;
        private Texture2D[] _noiseTextures;
        private int _noiseFrame;
        private float _noiseClock;

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_ShamanSense", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _root = go;

            var group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var vignetteGo = new GameObject("Vignette", typeof(RectTransform));
            var vignetteRect = (RectTransform)vignetteGo.transform;
            vignetteRect.SetParent(rect, false);
            UiKit.Stretch(vignetteRect);
            _vignetteImage = vignetteGo.AddComponent<RawImage>();
            _vignetteImage.raycastTarget = false;
            _vignetteImage.texture = EnsureVignetteTexture();
            _vignetteImage.color = new Color(1f, 1f, 1f, 0f);
            _vignetteImage.enabled = false;

            var rippleGo = new GameObject("DripRipple", typeof(RectTransform));
            var rippleRect = (RectTransform)rippleGo.transform;
            rippleRect.SetParent(rect, false);
            rippleRect.anchorMin = new Vector2(0.5f, 0.5f);
            rippleRect.anchorMax = new Vector2(0.5f, 0.5f);
            rippleRect.pivot = new Vector2(0.5f, 0.5f);
            rippleRect.anchoredPosition = Vector2.zero;
            rippleRect.sizeDelta = new Vector2(RippleStartSize, RippleStartSize);
            _rippleImage = rippleGo.AddComponent<RawImage>();
            _rippleImage.raycastTarget = false;
            _rippleImage.texture = EnsureRippleTexture();
            _rippleImage.color = new Color(RippleColor.r, RippleColor.g, RippleColor.b, 0f);
            _rippleImage.enabled = false;

            var stormGo = new GameObject("Storm", typeof(RectTransform));
            var stormRect = (RectTransform)stormGo.transform;
            stormRect.SetParent(rect, false);
            UiKit.Stretch(stormRect);
            _stormImage = stormGo.AddComponent<RawImage>();
            _stormImage.raycastTarget = false;
            _stormImage.texture = EnsureNoiseTextures()[0];
            _stormImage.uvRect = new Rect(0f, 0f, NoiseTilesX, NoiseTilesY);
            _stormImage.color = new Color(1f, 1f, 1f, 0f);
            _stormImage.enabled = false;

            _root.SetActive(false);
            WLog.Line("shaman_sense_built", secret: false);
        }

        public void PlayDripRipple()
        {
            if (_root == null || _rippleImage == null) return;

            _rippleRemainingSec = RippleDurationSec;
            _rippleImage.rectTransform.sizeDelta = new Vector2(RippleStartSize, RippleStartSize);
            _rippleImage.color = new Color(RippleColor.r, RippleColor.g, RippleColor.b, 0f);
            _rippleImage.enabled = true;
            if (!_root.activeSelf) _root.SetActive(true);
        }

        public void Tick(ShamanStormTier tier, bool ghostBurst, bool gazeArmed, float deltaSeconds)
        {
            if (_root == null) return;

            float alpha = ghostBurst ? GhostBurstAlpha : StormAlpha(tier);
            TickDripRipple(deltaSeconds);

            if (gazeArmed && PostProcessing.Instance != null)
            {
                PostProcessing.Instance.SaturationOverride(
                    TranceSaturation,
                    1f / TranceFadeInSec,
                    1f / TranceFadeOutSec,
                    PostProcessRefreshSec,
                    _root);
            }

            float fadeRate = gazeArmed ? 1f / TranceFadeInSec : 1f / TranceFadeOutSec;
            _tranceFade = Mathf.MoveTowards(_tranceFade, gazeArmed ? 1f : 0f,
                fadeRate * deltaSeconds);

            if (alpha <= 0f && _tranceFade <= 0f && _rippleRemainingSec <= 0f)
            {
                if (_root.activeSelf) _root.SetActive(false);
                return;
            }
            if (!_root.activeSelf) _root.SetActive(true);

            float vignetteAlpha = _tranceFade * VignetteMaxAlpha;
            _vignetteImage.enabled = vignetteAlpha > 0f;
            _vignetteImage.color = new Color(1f, 1f, 1f, vignetteAlpha);

            _stormImage.enabled = alpha > 0f;
            if (alpha > 0f)
            {
                _stormImage.color = new Color(1f, 1f, 1f, alpha);
                _noiseClock += deltaSeconds;
                if (_noiseClock >= NoiseFrameSec)
                {
                    _noiseClock = 0f;
                    _noiseFrame = (_noiseFrame + 1) % NoiseFrames;
                    _stormImage.texture = EnsureNoiseTextures()[_noiseFrame];
                    _stormImage.uvRect = new Rect(
                        Random.value, Random.value, NoiseTilesX, NoiseTilesY);
                }
            }
        }

        private void TickDripRipple(float deltaSeconds)
        {
            if (_rippleImage == null || _rippleRemainingSec <= 0f) return;

            _rippleRemainingSec = Mathf.Max(0f, _rippleRemainingSec - deltaSeconds);
            float t = 1f - _rippleRemainingSec / RippleDurationSec;
            float eased = 1f - (1f - t) * (1f - t);
            float size = Mathf.Lerp(RippleStartSize, RippleEndSize, eased);
            _rippleImage.rectTransform.sizeDelta = new Vector2(size, size);

            float fadeIn = Mathf.Clamp01(t / 0.08f);
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, t);
            float rippleAlpha = RippleMaxAlpha * fadeIn * fadeOut;
            _rippleImage.color = new Color(
                RippleColor.r, RippleColor.g, RippleColor.b, rippleAlpha);

            if (_rippleRemainingSec <= 0f)
            {
                _rippleImage.enabled = false;
            }
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf) _root.SetActive(false);
            _tranceFade = 0f;
            _rippleRemainingSec = 0f;
            if (_rippleImage != null)
            {
                _rippleImage.color = new Color(RippleColor.r, RippleColor.g, RippleColor.b, 0f);
                _rippleImage.enabled = false;
            }
            if (_vignetteImage != null)
            {
                _vignetteImage.color = new Color(1f, 1f, 1f, 0f);
                _vignetteImage.enabled = false;
            }
        }

        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
            _stormImage = null;
            _vignetteImage = null;
            _rippleImage = null;
            _tranceFade = 0f;
            _rippleRemainingSec = 0f;
            if (_vignetteTexture != null)
            {
                Object.Destroy(_vignetteTexture);
                _vignetteTexture = null;
            }
            if (_rippleTexture != null)
            {
                Object.Destroy(_rippleTexture);
                _rippleTexture = null;
            }
            if (_noiseTextures != null)
            {
                foreach (var tex in _noiseTextures)
                {
                    if (tex != null) Object.Destroy(tex);
                }
                _noiseTextures = null;
            }
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

        private Texture2D EnsureVignetteTexture()
        {
            if (_vignetteTexture != null) return _vignetteTexture;
            var tex = new Texture2D(VignetteSize, VignetteSize, TextureFormat.RGBA32, false);
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
                    pixels[y * VignetteSize + x] = new Color32(0, 0, 0, (byte)(t * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            tex.name = "WW_ShamanVignette";
            _vignetteTexture = tex;
            return tex;
        }

        private Texture2D EnsureRippleTexture()
        {
            if (_rippleTexture != null) return _rippleTexture;

            var tex = new Texture2D(
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
            _rippleTexture = tex;
            return tex;
        }

        private Texture2D[] EnsureNoiseTextures()
        {
            if (_noiseTextures != null) return _noiseTextures;
            _noiseTextures = new Texture2D[NoiseFrames];
            for (int f = 0; f < NoiseFrames; f++)
            {
                var tex = new Texture2D(NoiseSize, NoiseSize, TextureFormat.RGBA32, false);
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
                _noiseTextures[f] = tex;
            }
            return _noiseTextures;
        }
    }
}
