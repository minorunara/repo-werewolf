using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Werewolf.UI
{
    internal enum ToggleHit : byte
    {
        None = 0,

        Icon = 1,

        Label = 2,
    }

    internal readonly struct IconToggleStyle
    {
        public IconToggleStyle(float iconSize, float labelWidth, float labelHeight,
                               float labelFontSize, Color labelColor,
                               string fallbackText, Color fallbackPlateColor, Color fallbackTextColor)
        {
            IconSize = iconSize;
            LabelWidth = labelWidth;
            LabelHeight = labelHeight;
            LabelFontSize = labelFontSize;
            LabelColor = labelColor;
            FallbackText = fallbackText;
            FallbackPlateColor = fallbackPlateColor;
            FallbackTextColor = fallbackTextColor;
        }

        public float IconSize { get; }

        public float LabelWidth { get; }

        public float LabelHeight { get; }
        public float LabelFontSize { get; }
        public Color LabelColor { get; }

        public string FallbackText { get; }

        public Color FallbackPlateColor { get; }
        public Color FallbackTextColor { get; }
    }

    internal sealed class IconToggleButton
    {
        private const float AlphaHitThreshold = 0.5f;

        private const float IconLabelGap = 4f;
        private const float FallbackFontSize = 32f;

        private const float HoverOutlineThickness = 4f;
        private static readonly Color HoverOutlineColor = new Color(1f, 0.85f, 0.1f, 1f);

        private const float BadgeSize = 22f;
        private const float BadgeInset = 14f;
        private static readonly Color BadgeColor = new Color(0.86f, 0.16f, 0.12f, 1f);

        private RectTransform _container;
        private Image _icon;
        private RectTransform _iconRect;
        private TextMeshProUGUI _label;
        private RectTransform _labelRect;
        private Sprite _sprite;
        private Sprite _hoverSprite;
        private Outline _hoverOutline;
        private bool _hovering;
        private Image _badge;

        public RectTransform Container => _container;

        public bool HasIcon => _sprite != null;

        public RectTransform Build(Transform parent, string name, IconToggleStyle style,
                                   string spriteKey, string initialLabel, string hoverSpriteKey = null)
        {
            float width = Mathf.Max(style.IconSize, style.LabelWidth);
            float height = style.IconSize + IconLabelGap + style.LabelHeight;
            _container = UiKit.CreateRect(parent, name, Vector2.zero, new Vector2(width, height));

            float iconCenterY = height * 0.5f - style.IconSize * 0.5f;
            float labelCenterY = -(height * 0.5f - style.LabelHeight * 0.5f);

            _sprite = AssetCatalog.GetSprite(spriteKey);
            _hoverSprite = _sprite != null && !string.IsNullOrEmpty(hoverSpriteKey)
                ? AssetCatalog.GetSprite(hoverSpriteKey)
                : null;
            _icon = UiKit.CreateImage(_container, "Icon", new Vector2(0f, iconCenterY),
                new Vector2(style.IconSize, style.IconSize), Color.white);
            _iconRect = _icon.rectTransform;
            _icon.raycastTarget = true;

            if (_sprite != null)
            {
                _icon.sprite = _sprite;
                _icon.preserveAspect = true;
                try { _icon.alphaHitTestMinimumThreshold = AlphaHitThreshold; }
                catch {  }

                _hoverOutline = _icon.gameObject.AddComponent<Outline>();
                _hoverOutline.effectColor = HoverOutlineColor;
                _hoverOutline.effectDistance = new Vector2(HoverOutlineThickness, HoverOutlineThickness);
                _hoverOutline.useGraphicAlpha = true;
                _hoverOutline.enabled = false;
            }
            else
            {
                _icon.color = style.FallbackPlateColor;
                UiKit.CreateText(_iconRect, "IconFallback", Vector2.zero,
                    new Vector2(style.IconSize, style.IconSize),
                    style.FallbackText, FallbackFontSize, style.FallbackTextColor, TextAlignmentOptions.Center);
            }

            _label = UiKit.CreateText(_container, "Label", new Vector2(0f, labelCenterY),
                new Vector2(style.LabelWidth, style.LabelHeight),
                initialLabel, style.LabelFontSize, style.LabelColor, TextAlignmentOptions.Center);
            _labelRect = _label.rectTransform;

            return _container;
        }

        public void SetLabel(string text)
        {
            if (_label != null) _label.text = text;
        }

        public void SetBadgeVisible(bool visible)
        {
            if (_iconRect == null) return;
            if (_badge == null)
            {
                if (!visible) return;
                float offset = _iconRect.sizeDelta.x * 0.5f - BadgeInset;
                _badge = UiKit.CreateImage(_iconRect, "UnreadBadge",
                    new Vector2(offset, offset), new Vector2(BadgeSize, BadgeSize), BadgeColor);
                _badge.sprite = UiKit.CircleSprite();
            }
            if (_badge.gameObject.activeSelf != visible) _badge.gameObject.SetActive(visible);
        }

        public ToggleHit HitTest(Vector2 screenPoint)
        {
            if (_container == null || !_container.gameObject.activeSelf) return ToggleHit.None;

            if (_iconRect != null
                && RectTransformUtility.RectangleContainsScreenPoint(_iconRect, screenPoint, null))
            {
                if (_sprite == null) return ToggleHit.Icon;
                return SampleIconAlpha(screenPoint) >= AlphaHitThreshold ? ToggleHit.Icon : ToggleHit.None;
            }

            if (_labelRect != null
                && RectTransformUtility.RectangleContainsScreenPoint(_labelRect, screenPoint, null))
            {
                return ToggleHit.Label;
            }

            return ToggleHit.None;
        }

        public bool ContainsRect(Vector2 screenPoint)
        {
            if (_iconRect != null
                && RectTransformUtility.RectangleContainsScreenPoint(_iconRect, screenPoint, null))
            {
                return true;
            }
            return _labelRect != null
                && RectTransformUtility.RectangleContainsScreenPoint(_labelRect, screenPoint, null);
        }

        public bool IsPointerOverOpaqueIcon()
            => _sprite != null && HitTest(Input.mousePosition) == ToggleHit.Icon;

        public bool IsPointerOverHitArea()
            => HitTest(Input.mousePosition) != ToggleHit.None;

        public bool WasClicked()
            => Input.GetMouseButtonDown(0) && HitTest(Input.mousePosition) != ToggleHit.None;

        public void SetHover(bool over)
        {
            if (_sprite == null) return;
            if (_hovering == over) return;
            _hovering = over;
            if (_icon != null)
            {
                _icon.sprite = over && _hoverSprite != null ? _hoverSprite : _sprite;
            }
            if (_hoverOutline != null) _hoverOutline.enabled = over;
        }

        private float SampleIconAlpha(Vector2 screenPoint)
        {
            try
            {
                if (_iconRect == null || _sprite == null) return 1f;
                Texture2D tex = _sprite.texture as Texture2D;
                if (tex == null) return 1f;

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _iconRect, screenPoint, null, out Vector2 local))
                {
                    return 1f;
                }
                Rect r = _iconRect.rect;
                if (r.width <= 0f || r.height <= 0f) return 1f;
                float u = (local.x - r.xMin) / r.width;
                float v = (local.y - r.yMin) / r.height;
                if (u < 0f || u > 1f || v < 0f || v > 1f) return 1f;
                return tex.GetPixelBilinear(u, v).a;
            }
            catch
            {
                return 1f;
            }
        }

        public void Clear()
        {
            _container = null;
            _icon = null;
            _iconRect = null;
            _label = null;
            _labelRect = null;
            _sprite = null;
            _hoverSprite = null;
            _hoverOutline = null;
            _hovering = false;
            _badge = null;
        }
    }
}
