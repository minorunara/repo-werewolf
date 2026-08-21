using System;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal sealed class TallyChipStrip
    {
        private const float ChipSize = 30f;
        private const float BandWidth = 84f;
        private const float LooseStepPx = 26f;
        private const float SlideInPx = 34f;
        private const float FadeInMs = 160f;
        private const float EaseTauMs = 70f;
        private const long MaxDtMs = 100;

        private readonly Image[] _chips;
        private readonly Color _baseColor;
        private readonly float[] _x;
        private readonly long[] _shownAtMs;
        private readonly float _bandLeftX;
        private readonly float _y;
        private long _lastTickMs;

        private TallyChipStrip(Image[] chips, Color baseColor, float bandLeftX, float y)
        {
            _chips = chips;
            _baseColor = baseColor;
            _bandLeftX = bandLeftX;
            _y = y;
            _x = new float[chips.Length];
            _shownAtMs = new long[chips.Length];
        }

        public static TallyChipStrip Create(RectTransform parent, Vector2 bandLeft)
        {
            Sprite sprite = AssetCatalog.GetSprite("icon_vote_slip");
            Color baseColor = sprite != null ? Color.white : new Color(1f, 0.9f, 0.6f, 0.9f);
            var chips = new Image[VoteTallyTimeline.MaxChips];
            for (int i = 0; i < chips.Length; i++)
            {
                Image chip = UiKit.CreateImage(parent, $"TallyChip{i}",
                    new Vector2(bandLeft.x + ChipSize / 2f, bandLeft.y),
                    new Vector2(ChipSize, ChipSize), baseColor);
                if (sprite != null)
                {
                    chip.sprite = sprite;
                    chip.preserveAspect = true;
                }
                chip.gameObject.SetActive(false);
                chips[i] = chip;
            }
            return new TallyChipStrip(chips, baseColor, bandLeft.x, bandLeft.y);
        }

        public void Apply(int visibleCount, bool topVisible, long nowMs)
        {
            long dt = _lastTickMs > 0 ? Math.Min(Math.Max(nowMs - _lastTickMs, 0), MaxDtMs) : 0;
            _lastTickMs = nowMs;
            float ease = 1f - Mathf.Exp(-dt / EaseTauMs);

            float step = visibleCount <= 1
                ? 0f
                : Mathf.Min(LooseStepPx, (BandWidth - ChipSize) / (visibleCount - 1));

            for (int i = 0; i < _chips.Length; i++)
            {
                Image chip = _chips[i];
                if (chip == null) continue;
                bool on = i < visibleCount && (topVisible || i < visibleCount - 1);
                if (chip.gameObject.activeSelf != on) chip.gameObject.SetActive(on);
                if (!on)
                {
                    _shownAtMs[i] = 0;
                    continue;
                }

                float target = _bandLeftX + ChipSize / 2f + i * step;
                if (_shownAtMs[i] == 0)
                {
                    _shownAtMs[i] = nowMs;
                    _x[i] = target + SlideInPx;
                }
                _x[i] += (target - _x[i]) * ease;
                chip.rectTransform.anchoredPosition = new Vector2(_x[i], _y);

                float alpha = Mathf.Clamp01((nowMs - _shownAtMs[i]) / FadeInMs);
                Color c = _baseColor;
                c.a *= alpha;
                chip.color = c;
            }
        }

        public void HideAll()
        {
            for (int i = 0; i < _chips.Length; i++)
            {
                Image chip = _chips[i];
                if (chip == null) continue;
                if (chip.gameObject.activeSelf) chip.gameObject.SetActive(false);
                _shownAtMs[i] = 0;
            }
            _lastTickMs = 0;
        }
    }
}
