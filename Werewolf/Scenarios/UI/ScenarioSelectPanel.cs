using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.UI;
using Werewolf.Scenarios.Vanilla;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.UI
{
    public sealed class ScenarioSelectPanel : IClientPanel
    {
        public string LayerName => WerewolfUIManager.ScenarioSelectLayer;

        private const float TitleY = 340f;
        private const float TitleFontSize = 54f;

        private const float TileSpacing = 350f;
        private const float TileHitWidth = 340f;
        private const float TileHitHeight = 380f;
        private const float IconSize = 270f;
        private const float IconY = 60f;
        private const float NameY = -110f;
        private const float StatusY = -150f;
        private const float NameFontSize = 30f;
        private const float NameFontSizeMin = 20f;
        private const float StatusFontSize = 22f;
        private const float ArrowSize = 36f;

        private const float DescY = -230f;
        private const float DescFontSize = 28f;
        private const float BackY = -330f;
        private static readonly Vector2 BackSize = new Vector2(260f, 64f);

        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.93f);
        private static readonly Color TitleColor = new Color(1f, 0.95f, 0.7f, 1f);
        private static readonly Color NameColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color LockedTint = new Color(1f, 1f, 1f, 0.28f);
        private static readonly Color LockedLabelColor = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color ClearedColor = new Color(0.55f, 0.9f, 0.6f, 1f);
        private static readonly Color DescColor = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color ArrowColor = new Color(1f, 1f, 1f, 0.3f);
        private static readonly Color HoverFrameColor = new Color(1f, 0.82f, 0.3f, 0.22f);
        private static readonly ButtonPalette BackPalette = new ButtonPalette(
            enabledBg: new Color(0.16f, 0.18f, 0.24f, 1f),
            hoverBg: new Color(0.3f, 0.34f, 0.44f, 1f),
            disabledBg: new Color(0.12f, 0.12f, 0.14f, 1f),
            disabledLabel: new Color(1f, 1f, 1f, 0.4f));

        private static int _suppressUntilFrame = -1;

        internal static bool SuppressVanillaMenuHover => Time.frameCount <= _suppressUntilFrame;

        private sealed class Tile
        {
            public ScenarioId Id;
            public bool Implemented;
            public RectTransform Hit;
            public Image HoverFrame;
            public Image Icon;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Status;
        }

        private GameObject _root;
        private readonly List<Tile> _tiles = new List<Tile>();
        private TextMeshProUGUI _descText;
        private RectTransform _backRect;
        private Image _backImage;
        private TextMeshProUGUI _backLabel;

        private bool _open;
        private int _hoverIndex = -1;
        private Action<ScenarioId> _onStart;

        public bool Exists => _root != null;

        public bool IsOpen => _open;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var rootGo = new GameObject("WW_ScenarioSelect", typeof(RectTransform));
            var rootRect = (RectTransform)rootGo.transform;
            rootRect.SetParent(layerRoot, false);
            UiKit.Stretch(rootRect);
            Canvas canvas = rootGo.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = WerewolfUIManager.ScenarioSelectSortingOrder;
            rootGo.AddComponent<GraphicRaycaster>();
            Image dim = rootGo.AddComponent<Image>();
            dim.color = DimColor;
            dim.raycastTarget = true;
            _root = rootGo;

            UiKit.CreateText(rootRect, "Title", new Vector2(0f, TitleY), new Vector2(1400f, 80f),
                Texts.Get(TextId.ScenarioSelectTitle), TitleFontSize, TitleColor, TextAlignmentOptions.Center);

            BuildTiles(rootRect);

            _descText = UiKit.CreateText(rootRect, "Description", new Vector2(0f, DescY),
                new Vector2(1400f, 60f), string.Empty, DescFontSize, DescColor, TextAlignmentOptions.Center);
            _descText.richText = false;

            _backImage = UiKit.CreateImage(rootRect, "Back", new Vector2(0f, BackY), BackSize, BackPalette.EnabledBg);
            _backRect = _backImage.rectTransform;
            _backLabel = UiKit.CreateText(_backRect, "Label", Vector2.zero, BackSize,
                Texts.Get(TextId.ScenarioBack), 26f, Color.white, TextAlignmentOptions.Center);
            _backLabel.richText = false;

            _root.SetActive(false);
        }

        private void BuildTiles(Transform parent)
        {
            IReadOnlyList<ScenarioEntry> entries = ScenarioCatalog.Entries;
            float left = -TileSpacing * (entries.Count - 1) * 0.5f;

            for (int i = 0; i < entries.Count; i++)
            {
                ScenarioEntry entry = entries[i];
                float x = left + TileSpacing * i;

                RectTransform hit = UiKit.CreateRect(parent, "Tile_" + entry.Id,
                    new Vector2(x, 0f), new Vector2(TileHitWidth, TileHitHeight));

                Image frame = UiKit.CreateImage(hit, "HoverFrame", new Vector2(0f, IconY),
                    new Vector2(IconSize + 40f, IconSize + 40f), HoverFrameColor);
                frame.sprite = UiKit.RoundedRectSprite();
                frame.type = Image.Type.Sliced;
                frame.enabled = false;

                Image icon = UiKit.CreateImage(hit, "Icon", new Vector2(0f, IconY),
                    new Vector2(IconSize, IconSize), Color.white);
                icon.sprite = AssetCatalog.GetSprite(entry.IconKey);
                icon.preserveAspect = true;
                icon.enabled = icon.sprite != null;

                TextMeshProUGUI name = UiKit.CreateText(hit, "Name", new Vector2(0f, NameY),
                    new Vector2(TileHitWidth, 40f), Texts.Get(entry.NameId), NameFontSize, NameColor,
                    TextAlignmentOptions.Center);
                name.richText = false;
                name.enableAutoSizing = true;
                name.fontSizeMax = NameFontSize;
                name.fontSizeMin = NameFontSizeMin;

                TextMeshProUGUI status = UiKit.CreateText(hit, "Status", new Vector2(0f, StatusY),
                    new Vector2(TileHitWidth, 32f), string.Empty, StatusFontSize, ClearedColor,
                    TextAlignmentOptions.Center);
                status.richText = false;

                if (!entry.Implemented)
                {
                    icon.color = LockedTint;
                    name.color = LockedLabelColor;
                }

                _tiles.Add(new Tile
                {
                    Id = entry.Id,
                    Implemented = entry.Implemented,
                    Hit = hit,
                    HoverFrame = frame,
                    Icon = icon,
                    Name = name,
                    Status = status,
                });

                if (i == 0) continue;

                Image arrow = UiKit.CreateImage(parent, "Arrow_" + i,
                    new Vector2(x - TileSpacing * 0.5f, IconY), new Vector2(ArrowSize, ArrowSize), ArrowColor);
                arrow.sprite = UiKit.TriangleUpSprite();
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            }
        }

        public void Open(Func<ScenarioId, bool> isCleared, Action<ScenarioId> onStart)
        {
            if (_root == null) return;
            _onStart = onStart;
            _hoverIndex = -1;

            bool recommendationTaken = false;
            for (int i = 0; i < _tiles.Count; i++)
            {
                Tile tile = _tiles[i];
                bool cleared = isCleared != null && isCleared(tile.Id);
                tile.Status.text = tile.Implemented
                    ? (cleared ? Texts.Get(TextId.ScenarioCompleted) : string.Empty)
                    : Texts.Get(TextId.ScenarioLocked);
                tile.Status.color = tile.Implemented ? ClearedColor : LockedLabelColor;
                tile.HoverFrame.enabled = false;

                if (!tile.Implemented) continue;
                bool recommended = !cleared && !recommendationTaken;
                recommendationTaken |= recommended;
                tile.Name.color = recommended ? TitleColor : NameColor;
            }

            _descText.text = string.Empty;
            _open = true;
            PumpHoverSuppression();
            _root.SetActive(true);
            WLog.Line("scenario_select_open", secret: false);
        }

        public void Close(string reason)
        {
            _suppressUntilFrame = -1;
            if (!_open) return;
            _open = false;
            _hoverIndex = -1;
            _onStart = null;
            if (_root != null) _root.SetActive(false);
            WLog.Line("scenario_select_close", secret: false, ("reason", reason));
        }

        public void Tick()
        {
            if (!_open || _root == null) return;

            try
            {
                UiKit.KeepCursorFree();
                PumpHoverSuppression();

                Vector2 mouse = Input.mousePosition;
                _hoverIndex = HitTest(mouse);
                UpdateHoverVisuals(mouse);

                if (NoTextInputsActive() && MenuKeyDown())
                {
                    Close("menu_key");
                    return;
                }

                if (!Input.GetMouseButtonDown(0)) return;

                if (Hit(_backRect, mouse))
                {
                    Close("back_button");
                    return;
                }

                if (_hoverIndex < 0) return;
                Tile picked = _tiles[_hoverIndex];
                if (!picked.Implemented)
                {
                    WLog.Line("scenario_select_locked", secret: false, ("scenario", picked.Id));
                    return;
                }

                Action<ScenarioId> start = _onStart;
                ScenarioId id = picked.Id;
                Close("scenario_start");
                WLog.Line("scenario_select_pick", secret: false, ("scenario", id));
                start?.Invoke(id);
            }
            catch (Exception e)
            {
                WLog.Line("scenario_select_error", secret: false, ("err", e.Message));
            }
        }

        private void UpdateHoverVisuals(Vector2 mouse)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].HoverFrame.enabled = i == _hoverIndex && _tiles[i].Implemented;
            }

            if (_hoverIndex >= 0
                && ScenarioCatalog.TryGet(_tiles[_hoverIndex].Id, out ScenarioEntry entry))
            {
                _descText.text = entry.DescriptionId.HasValue
                    ? Texts.Get(entry.DescriptionId.Value)
                    : Texts.Get(TextId.ScenarioLocked);
            }
            else
            {
                _descText.text = string.Empty;
            }

            bool backHover = Hit(_backRect, mouse);
            ButtonVisual.Resolve(BackPalette, armed: false, hover: backHover, selected: false, enabled: true,
                out Color bg, out Color label);
            _backImage.color = bg;
            _backLabel.color = label;
        }

        private int HitTest(Vector2 point)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (Hit(_tiles[i].Hit, point)) return i;
            }
            return -1;
        }

        private static bool Hit(RectTransform rect, Vector2 point)
            => rect != null && rect.gameObject.activeInHierarchy
               && RectTransformUtility.RectangleContainsScreenPoint(rect, point, null);

        private static void PumpHoverSuppression() => _suppressUntilFrame = Time.frameCount + 1;

        private static bool MenuKeyDown()
            => ScenarioVanillaProbe.InputDown(GameKey.Menu, "select_menu_key");

        private static bool NoTextInputsActive()
            => ScenarioVanillaProbe.NoTextInputsActive("select_text_inputs");

        public void Destroy()
        {
            Close("destroy");
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _tiles.Clear();
            _descText = null;
            _backRect = null;
            _backImage = null;
            _backLabel = null;
        }
    }
}
