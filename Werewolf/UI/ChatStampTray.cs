using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal sealed class ChatStampTray
    {
        public const float BarHeight = 34f;

        private const float ButtonWidth = 130f;
        private const float ButtonHeight = 26f;
        private const float ButtonFontSize = 18f;

        private const float PalettePadding = 8f;
        private const float TabHeight = 28f;
        private const float TabGap = 4f;
        private const float TabFontSize = 14f;
        private const float TabIconSize = 22f;

        private const float PaletteGap = 2f;

        private const float ItemHeight = 80f;

        private const float ItemGap = 2f;
        private const float ItemFontSize = 19f;
        private const float ItemTextPadding = 8f;

        private const float ItemIconSize = 64f;
        private const float ItemIconGap = 8f;

        private const float ItemScrollSensitivity = 40f;

        private const float DisabledAlpha = 0.45f;

        private static readonly Color BarBgColor = new Color(0.14f, 0.14f, 0.19f, 0.92f);
        private static readonly Color ButtonColor = new Color(0.24f, 0.24f, 0.32f, 0.95f);
        private static readonly Color ButtonHoverColor = new Color(0.34f, 0.34f, 0.44f, 0.95f);
        private static readonly Color ButtonTextColor = new Color(0.92f, 0.93f, 0.97f, 1f);
        private static readonly Color PaletteBgColor = new Color(0.05f, 0.05f, 0.09f, 0.97f);
        private static readonly Color TabColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly Color TabHoverColor = new Color(0.26f, 0.26f, 0.34f, 0.95f);
        private static readonly Color TabActiveColor = new Color(0.32f, 0.29f, 0.18f, 0.98f);
        private static readonly Color TabTextColor = new Color(0.78f, 0.80f, 0.87f, 1f);
        private static readonly Color ItemColor = new Color(0.13f, 0.13f, 0.19f, 0.95f);
        private static readonly Color ItemHoverColor = new Color(0.24f, 0.24f, 0.33f, 0.95f);
        private static readonly Color ItemTextColor = new Color(0.92f, 0.93f, 0.97f, 1f);

        private struct TabEntry
        {
            public RectTransform Rect;
            public Image Bg;
            public ChatStampCategory Category;
        }

        private struct ItemEntry
        {
            public RectTransform Rect;
            public Image Bg;
            public byte Id;
        }

        private RectTransform _panelRect;
        private float _panelWidth;
        private float _panelHeight;
        private float _headerHeight;
        private float _footerHeight;
        private float _paletteBottomY;
        private float _paletteHeight;

        private RectTransform _bar;
        private CanvasGroup _barGroup;
        private RectTransform _buttonRect;
        private Image _buttonBg;

        private GameObject _palette;
        private RectTransform _paletteRect;
        private RectTransform _itemsViewport;
        private RectTransform _itemsRoot;
        private CanvasGroup _itemsGroup;
        private readonly List<TabEntry> _tabs = new List<TabEntry>();
        private readonly List<ItemEntry> _items = new List<ItemEntry>();

        private readonly ClickGate _click = new ClickGate();
        private ChatStampCategory _category;
        private bool _paletteOpen;
        private bool _windowOpen = true;
        private bool _paletteHover;
        private bool _itemsDimmed;

        public bool IsPaletteOpen => _paletteOpen;

        public void Build(RectTransform panelRect, float panelWidth, float panelHeight, float headerHeight)
        {
            if (_bar != null || panelRect == null) return;
            _panelRect = panelRect;
            _panelWidth = panelWidth;
            _panelHeight = panelHeight;
            _headerHeight = headerHeight;
            _footerHeight = 0f;
            ApplyGeometry();

            var barGo = new GameObject("StampBar", typeof(RectTransform));
            _bar = (RectTransform)barGo.transform;
            _bar.SetParent(panelRect, false);
            _bar.anchorMin = _bar.anchorMax = new Vector2(0.5f, 0f);
            _bar.pivot = new Vector2(0.5f, 0f);
            _bar.anchoredPosition = new Vector2(0f, _footerHeight);
            _bar.sizeDelta = new Vector2(panelWidth, BarHeight);
            _barGroup = barGo.AddComponent<CanvasGroup>();

            Image barBg = UiKit.CreateImage(_bar, "Bg", Vector2.zero, Vector2.zero, BarBgColor);
            UiKit.Stretch(barBg.rectTransform);

            _buttonBg = UiKit.CreateRoundedImage(_bar, "StampButton",
                new Vector2(-panelWidth * 0.5f + 8f + ButtonWidth * 0.5f, 0f),
                new Vector2(ButtonWidth, ButtonHeight), ButtonColor);
            _buttonRect = _buttonBg.rectTransform;
            TextMeshProUGUI label = UiKit.CreateText(_buttonRect, "Label", Vector2.zero,
                new Vector2(ButtonWidth, ButtonHeight), Texts.Get(TextId.ChatStampButtonLabel),
                ButtonFontSize, ButtonTextColor, TextAlignmentOptions.Center);
            label.enableWordWrapping = false;

            foreach (ChatStampCategoryDef cat in ChatStampCatalog.CategoryDefs)
            {
                AssetCatalog.GetSprite(cat.IconKey);
            }
        }

        public void SetFooterHeight(float footerHeight)
        {
            if (_bar == null || Mathf.Approximately(footerHeight, _footerHeight)) return;
            _footerHeight = footerHeight;
            ApplyGeometry();
            _bar.anchoredPosition = new Vector2(0f, _footerHeight);
            DestroyPalette();
        }

        private void ApplyGeometry()
        {
            _paletteBottomY = _footerHeight + BarHeight + PaletteGap;
            _paletteHeight = _panelHeight - _headerHeight - _paletteBottomY;
        }

        private void DestroyPalette()
        {
            ClosePalette();
            if (_palette != null) UnityEngine.Object.Destroy(_palette);
            ForgetPalette();
        }

        private void ForgetPalette()
        {
            _palette = null;
            _paletteRect = null;
            _itemsViewport = null;
            _itemsRoot = null;
            _itemsGroup = null;
            _tabs.Clear();
            _items.Clear();
            _paletteOpen = false;
            _paletteHover = false;
            _itemsDimmed = false;
        }

        public void Tick(bool windowOpen, bool cooldownReady, bool defaultChatter, Action<byte> send)
        {
            if (_bar == null) return;

            if (windowOpen != _windowOpen)
            {
                _windowOpen = windowOpen;
                if (_barGroup != null) _barGroup.alpha = windowOpen ? 1f : DisabledAlpha;
                if (!windowOpen) ClosePalette();
            }

            Vector2 mouse = Input.mousePosition;
            bool overButton = _windowOpen && _buttonRect != null
                && RectTransformUtility.RectangleContainsScreenPoint(_buttonRect, mouse, null);
            if (_buttonBg != null)
            {
                _buttonBg.color = overButton || _paletteOpen ? ButtonHoverColor : ButtonColor;
            }

            int hoverTab = -1;
            int hoverItem = -1;
            if (_paletteOpen)
            {
                SyncItemsDim(cooldownReady);
                UpdatePaletteHover(mouse, cooldownReady, out hoverTab, out hoverItem);
            }

            if (!_click.Tick(Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0), mouse.x, mouse.y)) return;
            HandleClick(mouse, overButton, hoverTab, hoverItem, cooldownReady, defaultChatter, send);
        }

        private void UpdatePaletteHover(Vector2 mouse, bool cooldownReady, out int hoverTab, out int hoverItem)
        {
            hoverTab = -1;
            hoverItem = -1;
            bool inPalette = _paletteRect != null
                && RectTransformUtility.RectangleContainsScreenPoint(_paletteRect, mouse, null);
            if (!inPalette)
            {
                if (_paletteHover)
                {
                    _paletteHover = false;
                    ResetHoverColors();
                }
                return;
            }

            _paletteHover = true;
            for (int i = 0; i < _tabs.Count; i++)
            {
                TabEntry tab = _tabs[i];
                bool hover = hoverTab < 0
                    && RectTransformUtility.RectangleContainsScreenPoint(tab.Rect, mouse, null);
                tab.Bg.color = tab.Category == _category ? TabActiveColor
                    : hover ? TabHoverColor : TabColor;
                if (hover) hoverTab = i;
            }
            bool inItemsWindow = _itemsViewport != null
                && RectTransformUtility.RectangleContainsScreenPoint(_itemsViewport, mouse, null);
            for (int i = 0; i < _items.Count; i++)
            {
                ItemEntry item = _items[i];
                bool hover = hoverItem < 0 && cooldownReady && inItemsWindow
                    && RectTransformUtility.RectangleContainsScreenPoint(item.Rect, mouse, null);
                item.Bg.color = hover ? ItemHoverColor : ItemColor;
                if (hover) hoverItem = i;
            }
        }

        private void HandleClick(Vector2 mouse, bool overButton, int hoverTab, int hoverItem,
                                 bool cooldownReady, bool defaultChatter, Action<byte> send)
        {
            if (overButton)
            {
                if (_paletteOpen)
                {
                    ClosePalette();
                }
                else
                {
                    OpenPalette(defaultChatter);
                    SyncItemsDim(cooldownReady);
                }
                return;
            }
            if (!_paletteOpen) return;

            if (hoverTab >= 0)
            {
                ChatStampCategory category = _tabs[hoverTab].Category;
                if (category != _category)
                {
                    _category = category;
                    RebuildItems();
                }
                return;
            }
            if (hoverItem >= 0)
            {
                send?.Invoke(_items[hoverItem].Id);
                ClosePalette();
                return;
            }
            if (_paletteRect == null
                || !RectTransformUtility.RectangleContainsScreenPoint(_paletteRect, mouse, null))
            {
                ClosePalette();
            }
        }

        public void ClosePalette()
        {
            _paletteOpen = false;
            if (_palette != null) _palette.SetActive(false);
        }

        private void OpenPalette(bool defaultChatter)
        {
            EnsurePalette();
            if (_palette == null) return;
            _category = defaultChatter ? ChatStampCategory.Chatter : ChatStampCategory.Guide;
            RebuildItems();
            ResetHoverColors();
            _paletteHover = false;
            _palette.SetActive(true);
            _paletteOpen = true;
            WLog.Line("chat_stamp_palette", secret: false, ("open", true), ("chatter", defaultChatter));
        }

        private void SyncItemsDim(bool cooldownReady)
        {
            bool dimmed = !cooldownReady;
            if (dimmed == _itemsDimmed) return;
            _itemsDimmed = dimmed;
            if (_itemsGroup != null) _itemsGroup.alpha = dimmed ? DisabledAlpha : 1f;
        }

        private void ResetHoverColors()
        {
            foreach (TabEntry tab in _tabs)
            {
                tab.Bg.color = tab.Category == _category ? TabActiveColor : TabColor;
            }
            foreach (ItemEntry item in _items) item.Bg.color = ItemColor;
        }

        private static Sprite ItemIconSprite(ChatStampDef def)
            => ChatEmoji.TryReadSpriteAt(def.Emoji, 0, out string key, out _) ? AssetCatalog.GetSprite(key) : null;

        private void EnsurePalette()
        {
            if (_palette != null || _panelRect == null) return;

            var go = new GameObject("StampPalette", typeof(RectTransform));
            _paletteRect = (RectTransform)go.transform;
            _paletteRect.SetParent(_panelRect, false);
            _paletteRect.anchorMin = _paletteRect.anchorMax = new Vector2(0.5f, 0f);
            _paletteRect.pivot = new Vector2(0.5f, 0f);
            _paletteRect.anchoredPosition = new Vector2(0f, _paletteBottomY);
            _paletteRect.sizeDelta = new Vector2(_panelWidth, _paletteHeight);
            _palette = go;

            Image bg = UiKit.CreateImage(_paletteRect, "Bg", Vector2.zero, Vector2.zero, PaletteBgColor);
            UiKit.Stretch(bg.rectTransform);
            bg.raycastTarget = true;

            IReadOnlyList<ChatStampCategoryDef> categories = ChatStampCatalog.CategoryDefs;
            float innerWidth = _panelWidth - PalettePadding * 2f;
            float tabWidth = (innerWidth - TabGap * (categories.Count - 1)) / categories.Count;
            float tabTopY = _paletteHeight * 0.5f - PalettePadding - TabHeight * 0.5f;
            for (int i = 0; i < categories.Count; i++)
            {
                ChatStampCategoryDef cat = categories[i];
                float x = -innerWidth * 0.5f + tabWidth * 0.5f + i * (tabWidth + TabGap);
                Image tabBg = UiKit.CreateRoundedImage(_paletteRect, "Tab" + i,
                    new Vector2(x, tabTopY), new Vector2(tabWidth, TabHeight), TabColor);

                Sprite tabSprite = AssetCatalog.GetSprite(cat.IconKey);
                if (tabSprite != null)
                {
                    Image tabIcon = UiKit.CreateImage(tabBg.rectTransform, "Icon", Vector2.zero,
                        new Vector2(TabIconSize, TabIconSize), Color.white);
                    tabIcon.sprite = tabSprite;
                    tabIcon.preserveAspect = true;
                    if (cat.IconRotationZ != 0f)
                    {
                        tabIcon.rectTransform.localEulerAngles = new Vector3(0f, 0f, cat.IconRotationZ);
                    }
                }
                else
                {
                    TextMeshProUGUI tabLabel = UiKit.CreateText(tabBg.rectTransform, "Label",
                        Vector2.zero, new Vector2(tabWidth, TabHeight), Texts.Get(cat.TextId),
                        TabFontSize, TabTextColor, TextAlignmentOptions.Center);
                    tabLabel.enableWordWrapping = false;
                }

                _tabs.Add(new TabEntry
                {
                    Rect = tabBg.rectTransform,
                    Bg = tabBg,
                    Category = cat.Category,
                });
            }

            UiKit.CreateScrollArea(_paletteRect, "ItemsScroll", ItemScrollSensitivity,
                out RectTransform scrollArea, out _itemsViewport, out _itemsRoot);
            scrollArea.offsetMin = new Vector2(PalettePadding, PalettePadding);
            scrollArea.offsetMax = new Vector2(-PalettePadding, -(PalettePadding + TabHeight + TabGap));
            _itemsGroup = scrollArea.gameObject.AddComponent<CanvasGroup>();
        }

        private void RebuildItems()
        {
            if (_itemsRoot == null) return;
            for (int i = _itemsRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_itemsRoot.GetChild(i).gameObject);
            }
            _items.Clear();

            IReadOnlyList<ChatStampDef> defs = ChatStampCatalog.ForCategory(_category);
            float innerWidth = _panelWidth - PalettePadding * 2f;
            float contentHeight = defs.Count * ItemHeight + Mathf.Max(0, defs.Count - 1) * ItemGap;
            _itemsRoot.sizeDelta = new Vector2(0f, contentHeight);
            _itemsRoot.anchoredPosition = Vector2.zero;
            float listTop = contentHeight * 0.5f;
            float textWidth = innerWidth - ItemTextPadding * 2f - ItemIconSize - ItemIconGap;
            for (int i = 0; i < defs.Count; i++)
            {
                float topY = listTop - ItemHeight * 0.5f - i * (ItemHeight + ItemGap);
                Image itemBg = UiKit.CreateRoundedImage(_itemsRoot, "Item" + defs[i].Id.ToString(),
                    new Vector2(0f, topY), new Vector2(innerWidth, ItemHeight), ItemColor);

                Sprite icon = ItemIconSprite(defs[i]);
                if (icon != null)
                {
                    Image img = UiKit.CreateImage(itemBg.rectTransform, "Icon",
                        new Vector2(-innerWidth * 0.5f + ItemTextPadding + ItemIconSize * 0.5f, 0f),
                        new Vector2(ItemIconSize, ItemIconSize), Color.white);
                    img.sprite = icon;
                    img.preserveAspect = true;
                }

                TextMeshProUGUI label = UiKit.CreateText(itemBg.rectTransform, "Label",
                    new Vector2((ItemIconSize + ItemIconGap) * 0.5f, 0f),
                    new Vector2(textWidth, ItemHeight),
                    Texts.Get(defs[i].TextId),
                    ItemFontSize, ItemTextColor, TextAlignmentOptions.MidlineLeft);
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Ellipsis;
                _items.Add(new ItemEntry
                {
                    Rect = itemBg.rectTransform,
                    Bg = itemBg,
                    Id = defs[i].Id,
                });
            }
        }

        public void Clear()
        {
            ForgetPalette();
            _panelRect = null;
            _bar = null;
            _barGroup = null;
            _buttonRect = null;
            _buttonBg = null;
            _click.Reset();
            _windowOpen = true;
        }
    }
}
