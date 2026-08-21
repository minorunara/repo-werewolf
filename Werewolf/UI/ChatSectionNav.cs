using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal sealed class ChatSectionNav
    {
        private const float ButtonSize = 28f;
        private const float ButtonGap = 4f;
        private const float ButtonMarginRight = 8f;
        private const float TriangleWidth = 14f;
        private const float TriangleHeight = 9f;

        private const float TickWidth = 8f;
        private const float TickHeight = 3f;

        private const float JumpTopMargin = 6f;

        private const float JumpEpsilon = 4f;

        private static readonly Color PlateIdleColor = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color PlateHoverColor = new Color(1f, 0.9f, 0.6f, 0.22f);
        private static readonly Color ArrowIdleColor = new Color(1f, 0.9f, 0.6f, 0.75f);
        private static readonly Color ArrowHoverColor = new Color(1f, 0.95f, 0.75f, 1f);
        private static readonly Color TickColor = new Color(1f, 0.85f, 0.5f, 0.85f);

        private RectTransform _prevRect;
        private RectTransform _nextRect;
        private Image _prevPlate;
        private Image _nextPlate;
        private Image _prevArrow;
        private Image _nextArrow;

        private RectTransform _tickParent;
        private readonly List<RectTransform> _ticks = new List<RectTransform>();

        private int _marksLayoutVersion = int.MinValue;
        private int _marksSectionCount = -1;

        public void Build(RectTransform panelRect, float headerY, float panelWidth, RectTransform scrollbarRect)
        {
            float nextX = panelWidth * 0.5f - ButtonMarginRight - ButtonSize * 0.5f;
            float prevX = nextX - ButtonSize - ButtonGap;
            _prevRect = BuildButton(panelRect, "JumpPrev", new Vector2(prevX, headerY),
                pointDown: false, out _prevPlate, out _prevArrow);
            _nextRect = BuildButton(panelRect, "JumpNext", new Vector2(nextX, headerY),
                pointDown: true, out _nextPlate, out _nextArrow);
            _tickParent = scrollbarRect;
            SetAvailable(false);
        }

        private static RectTransform BuildButton(RectTransform parent, string name, Vector2 pos,
                                                 bool pointDown, out Image plate, out Image arrow)
        {
            plate = UiKit.CreateRoundedImage(parent, name, pos, new Vector2(ButtonSize, ButtonSize), PlateIdleColor);
            plate.raycastTarget = false;
            arrow = UiKit.CreateImage(plate.rectTransform, "Arrow", Vector2.zero,
                new Vector2(TriangleWidth, TriangleHeight), ArrowIdleColor);
            arrow.sprite = UiKit.TriangleUpSprite();
            arrow.raycastTarget = false;
            if (pointDown) arrow.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            return plate.rectTransform;
        }

        public void Tick(ChatLayout layout, IReadOnlyList<long> sections, ChatScrollView view)
        {
            if (_prevRect == null || !_prevRect.gameObject.activeSelf) return;
            if (layout == null || sections == null || view == null) return;

            Vector2 mouse = Input.mousePosition;
            bool hoverPrev = RectTransformUtility.RectangleContainsScreenPoint(_prevRect, mouse, null);
            bool hoverNext = RectTransformUtility.RectangleContainsScreenPoint(_nextRect, mouse, null);
            ApplyHover(_prevPlate, _prevArrow, hoverPrev);
            ApplyHover(_nextPlate, _nextArrow, hoverNext);

            if (!Input.GetMouseButtonDown(0)) return;
            if (hoverPrev) JumpPrev(layout, sections, view);
            else if (hoverNext) JumpNext(layout, sections, view);
        }

        private static void ApplyHover(Image plate, Image arrow, bool hover)
        {
            if (plate != null) plate.color = hover ? PlateHoverColor : PlateIdleColor;
            if (arrow != null) arrow.color = hover ? ArrowHoverColor : ArrowIdleColor;
        }

        private static void JumpPrev(ChatLayout layout, IReadOnlyList<long> sections, ChatScrollView view)
        {
            float window = view.WindowTop;
            float best = float.NaN;
            for (int i = 0; i < sections.Count; i++)
            {
                if (!layout.TryGetContentTop(sections[i], out float top)) continue;
                float target = top - JumpTopMargin;
                if (target >= window - JumpEpsilon) break;
                best = target;
            }
            if (!float.IsNaN(best)) view.ScrollToContentTop(best);
        }

        private static void JumpNext(ChatLayout layout, IReadOnlyList<long> sections, ChatScrollView view)
        {
            float window = view.WindowTop;
            for (int i = 0; i < sections.Count; i++)
            {
                if (!layout.TryGetContentTop(sections[i], out float top)) continue;
                float target = top - JumpTopMargin;
                if (target <= window + JumpEpsilon) continue;
                view.ScrollToContentTop(target);
                return;
            }
            view.ScrollToBottom();
        }

        public void SyncMarks(ChatLayout layout, IReadOnlyList<long> sections,
                              float sidePadding, float contentHeight)
        {
            if (_tickParent == null || layout == null || sections == null) return;
            if (layout.Version == _marksLayoutVersion && sections.Count == _marksSectionCount) return;
            _marksLayoutVersion = layout.Version;
            _marksSectionCount = sections.Count;

            SetAvailable(sections.Count > 0);

            int used = 0;
            if (contentHeight > 0f)
            {
                for (int i = 0; i < sections.Count; i++)
                {
                    if (!layout.TryGetContentTop(sections[i], out float top)) continue;
                    float frac = Mathf.Clamp((sidePadding + top) / contentHeight, 0.01f, 0.99f);
                    RectTransform tick = used < _ticks.Count ? _ticks[used] : CreateTick();
                    tick.gameObject.SetActive(true);
                    tick.anchorMin = tick.anchorMax = new Vector2(0.5f, 1f - frac);
                    tick.anchoredPosition = Vector2.zero;
                    used++;
                }
            }
            for (int i = used; i < _ticks.Count; i++) _ticks[i].gameObject.SetActive(false);
        }

        private RectTransform CreateTick()
        {
            Image image = UiKit.CreateImage(_tickParent, "SectionTick", Vector2.zero,
                new Vector2(TickWidth, TickHeight), TickColor);
            image.raycastTarget = false;
            _ticks.Add(image.rectTransform);
            return image.rectTransform;
        }

        private void SetAvailable(bool available)
        {
            if (_prevRect != null) _prevRect.gameObject.SetActive(available);
            if (_nextRect != null) _nextRect.gameObject.SetActive(available);
        }

        public void Clear()
        {
            _prevRect = null;
            _nextRect = null;
            _prevPlate = null;
            _nextPlate = null;
            _prevArrow = null;
            _nextArrow = null;
            _tickParent = null;
            _ticks.Clear();
            _marksLayoutVersion = int.MinValue;
            _marksSectionCount = -1;
        }
    }
}
