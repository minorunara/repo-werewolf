using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Core.Replay;

namespace Werewolf.UI
{
    public sealed partial class ReplayViewerPanel : IClientPanel
    {
        public string LayerName => ResultScreen.Layer;

        private const float PanelWidth = 1200f;
        private const float MapHeight = 720f;
        private const float MapCenterY = 10f;
        private const float BarHeight = 56f;
        private const float BarCenterY = MapCenterY - MapHeight * 0.5f - 6f - BarHeight * 0.5f;
        private const float StatusCenterY = BarCenterY - BarHeight * 0.5f - 14f;
        private const float ToggleButtonMargin = 24f;

        private const float ButtonHeight = 40f;
        private const float PlayButtonWidth = 96f;
        private const float SpeedButtonWidth = 96f;
        private const float SaveButtonWidth = 96f;
        private const float TimeLabelWidth = 140f;
        private const float SeekLeft = -370f;
        private const float SeekRight = 330f;
        private const float SeekHitHeight = 44f;
        private const float SeekBarThickness = 10f;
        private const float SeekHandleSize = 20f;
        private const float ButtonFontSize = 22f;
        private const float ButtonIconSize = 28f;
        private const float ButtonIconPadding = 8f;
        private const float ButtonIconLabelGap = 4f;
        private const float ButtonIconLabelFontSize = 18f;
        private const float TimeFontSize = 22f;
        private const float StatusFontSize = 18f;

        private const float PlayerFillSize = 14f;
        private const float PlayerFrameSize = 18f;
        private const float PlayerIdFontSize = 11f;
        private const float PlayerNameFontSize = 14f;
        private const float PlayerNameOffsetY = -17f;
        private const float EnemyDotSize = 12f;
        private const float EnemyNameFontSize = 12f;
        private const float EnemyNameOffsetY = -14f;
        private const float ValuableDotSize = 7f;
        private const float ItemDotSize = 7f;
        private const float CartSize = 13f;
        private const float EpSize = 18f;
        private const float EpFontSize = 12f;
        private const float EpStateFontSize = 13f;
        private const float EpStateOffsetY = 17f;
        private const float TrailThickness = 2f;
        private const double TrailSeconds = 15.0;
        private const float MarkerClickRadius = 16f;
        private const float DimFactor = 0.35f;
        private const float DeathMarkSize = 14f;
        private const float DeathMarkBarThickness = 3f;

        private const double SeekRebuildEpsilon = 1e-6;

        private const float PopupFontSize = 19f;
        private const float PopupRiseY = 26f;
        private const float PopupBaseOffsetY = 12f;
        private const double PopupVisibleRealSec = 1.2;
        private const double PopupMinLifeSec = 4.0;
        private const double PopupMaxLifeSec = 40.0;
        private const int PopupMaxVisible = 24;

        private const float DanmakuShotRootHeight = 210f;
        private const float DanmakuBodyFontSize = 44f;
        private const float DanmakuMetaFontSize = 24f;
        private const float DanmakuWidthPaddingPx = 24f;
        private const float DanmakuOutlineWidth = 0.22f;
        private const float DanmakuLandingShakePx = 3f;
        private const float DanmakuAccentExtraWidth = 90f;
        private const float StampFontSize = 138f;
        private const float DemoChatIntervalSec = 0.72f;

        private static readonly Color DanmakuMetaColor = new Color(0.85f, 0.85f, 0.88f, 0.75f);
        private static readonly Color DanmakuMetaWolfColor = new Color(1f, 0.5f, 0.5f, 0.85f);
        private static readonly Color StampExecutedColor = new Color(0.95f, 0.3f, 0.26f, 1f);
        private static readonly Color StampNoExecutionColor = new Color(0.92f, 0.92f, 0.95f, 1f);

        private static readonly string[] DemoChatTexts =
        {
            "あ",
            "こっちは金庫を回収したよ",
            "さっき3番が抽出ポイントの裏で誰かと一緒だった気がする",
            "いま考えると最初の会議で5番が黙っていたのはかなり怪しいよ",
            "最終盤まで生き残っている人の行動を全員でもう一度時系列に並べて検証してみましょうよ",
        };

        private const float GaugeHeight = 100f;
        private const float GaugeCenterY = 430f;
        private const float GaugeTrackWidth = 1080f;
        private const float GaugeTrackHeight = 14f;
        private const float GaugeLossRowY = 24f;
        private const float GaugeDeliveryRowY = 8f;
        private const float GaugeTextRowY = -18f;
        private const float GaugeIconSize = 44f;
        private const float GaugeTextFontSize = 18f;

        private static readonly Color PanelFrameColor = new Color(0.02f, 0.02f, 0.05f, 0.95f);
        private static readonly Color BarBgColor = new Color(0.06f, 0.06f, 0.1f, 0.95f);
        private static readonly Color ButtonBgColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly Color ButtonHoverColor = new Color(0.26f, 0.26f, 0.34f, 1f);
        private static readonly Color SeekBgColor = new Color(1f, 1f, 1f, 0.28f);
        private static readonly Color SeekFillColor = new Color(1f, 0.85f, 0.35f, 0.9f);
        private static readonly Color SeekMeetingColor = new Color(0.5f, 0.7f, 1f, 0.35f);
        private static readonly Color SeekDeathColor = new Color(0.95f, 0.25f, 0.2f, 0.95f);
        private static readonly Color EnemyColor = new Color(0.92f, 0.22f, 0.2f, 1f);
        private static readonly Color ValuableColor = new Color(0.95f, 0.8f, 0.3f, 1f);
        private static readonly Color ItemColor = new Color(0.25f, 0.82f, 0.9f, 1f);
        private static readonly Color CartColor = new Color(0.62f, 0.42f, 0.22f, 1f);
        private static readonly Color FrameDefaultColor = new Color(0.06f, 0.06f, 0.1f, 0.95f);
        private static readonly Color FrameSelfColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color FrameWolfColor = new Color(0.95f, 0.2f, 0.18f, 1f);
        private static readonly Color NameDefaultColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color NameWolfColor = new Color(1f, 0.45f, 0.45f, 1f);
        private static readonly Color DeathMarkFallbackColor = new Color(0.90f, 0.15f, 0.15f, 0.95f);
        private static readonly Color PopupLostColor = new Color(0.95f, 0.33f, 0.28f, 1f);
        private static readonly Color PopupDeliverColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        private static readonly Color GaugeBgColor = new Color(0.02f, 0.02f, 0.05f, 0.92f);
        private static readonly Color GaugeTrackColor = new Color(0.10f, 0.10f, 0.14f, 0.9f);
        private static readonly Color GaugeLossColor = new Color(0.85f, 0.65f, 0.15f, 0.95f);
        private static readonly Color GaugeDeliveryColor = new Color(0.30f, 0.80f, 0.95f, 0.95f);
        private static readonly Color GaugeTextColor = new Color(0.95f, 0.95f, 0.98f, 0.95f);
        private static readonly Color StatusOkColor = new Color(0.55f, 0.9f, 0.55f, 1f);
        private static readonly Color StatusMutedColor = new Color(0.85f, 0.85f, 0.88f, 0.95f);
        private static readonly Color StatusErrorColor = new Color(1f, 0.45f, 0.4f, 1f);

        private static readonly IconToggleStyle ToggleStyle = new IconToggleStyle(
            iconSize: 288f,
            labelWidth: 240f,
            labelHeight: 32f,
            labelFontSize: 24f,
            labelColor: new Color(1f, 0.9f, 0.6f, 1f),
            fallbackText: "▶",
            fallbackPlateColor: new Color(0.08f, 0.08f, 0.10f, 0.55f),
            fallbackTextColor: new Color(0.95f, 0.95f, 0.95f, 0.95f));

        private GameObject _root;
        private GameObject _viewerRoot;
        private RawImage _mapImage;
        private RectTransform _mapImageRect;
        private RectTransform _markersRoot;
        private RectTransform _popupRoot;
        private RectTransform _stampRoot;
        private RectTransform _danmakuRoot;

        private RectTransform _trailLayer;
        private RectTransform _epLayer;
        private RectTransform _deathMarkLayer;
        private RectTransform _cartLayer;
        private RectTransform _valuableLayer;
        private RectTransform _itemLayer;
        private RectTransform _enemyLayer;
        private RectTransform _playerLayer;
        private readonly IconToggleButton _toggle = new IconToggleButton();
        private MapStillCapture.Still _still;

        private Image _playButtonBg;
        private Image _playButtonIcon;
        private TextMeshProUGUI _playButtonLabel;
        private RectTransform _playButtonRect;
        private Image _speedButtonBg;
        private Image _speedButtonIcon;
        private TextMeshProUGUI _speedButtonLabel;
        private RectTransform _speedButtonRect;
        private Image _saveButtonBg;
        private Image _saveButtonIcon;
        private TextMeshProUGUI _saveButtonLabel;
        private RectTransform _saveButtonRect;
        private Sprite _playButtonSprite;
        private Sprite _pauseButtonSprite;
        private Sprite _speedButtonSprite;
        private Sprite _saveButtonSprite;
        private TextMeshProUGUI _timeLabel;
        private TextMeshProUGUI _statusLabel;

        private RectTransform _seekHitRect;
        private RectTransform _seekBarRect;
        private Image _seekFill;
        private RectTransform _seekHandle;
        private readonly List<Image> _seekMeetingShades = new List<Image>();
        private readonly List<Image> _seekDeathTicks = new List<Image>();

        private GameObject _gaugeRoot;
        private Image _gaugeLossFill;
        private Image _gaugeDeliveryFill;
        private GameObject _gaugeDeliveryTrack;
        private GameObject _gaugeDeliveryIcon;
        private TextMeshProUGUI _gaugeLossText;
        private TextMeshProUGUI _gaugeDeliveredText;
        private int _gaugeShownLoss = -1;
        private int _gaugeShownDelivered = -1;
        private bool _gaugeHasDelivery;

        private Texture _placeholderTex;
        private bool _placeholderResolved;

        private sealed class PlayerMarker
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Frame;
            public Image Fill;
            public TextMeshProUGUI Id;
            public TextMeshProUGUI Name;
        }

        private sealed class DotMarker
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Dot;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Sub;
        }

        private sealed class CommentView
        {
            public GameObject Root;
            public RectTransform Rect;
            public CanvasGroup Opacity;
            public Image Accent;
            public TextMeshProUGUI Meta;
            public TextMeshProUGUI Body1;
            public TextMeshProUGUI Body2;
            public ReplayDanmakuComment Bound;
        }

        private readonly List<CommentView> _danmakuPool = new List<CommentView>();
        private TextMeshProUGUI _stampLabel;
        private TextMeshProUGUI _danmakuMeasurer;
        private Material _danmakuOutlineMaterial;
        private ReplayDanmaku _danmaku;
        private readonly Dictionary<int, ReplayPlayerEntry> _danmakuPlayers
            = new Dictionary<int, ReplayPlayerEntry>();
        private float _demoChatTimer;
        private int _demoChatSeq;

        private readonly List<PlayerMarker> _playerPool = new List<PlayerMarker>();
        private readonly List<DotMarker> _enemyPool = new List<DotMarker>();
        private readonly List<DotMarker> _valuablePool = new List<DotMarker>();
        private readonly List<DotMarker> _itemPool = new List<DotMarker>();
        private readonly List<DotMarker> _cartPool = new List<DotMarker>();
        private readonly List<DotMarker> _epPool = new List<DotMarker>();
        private readonly List<RectTransform> _deathMarkPool = new List<RectTransform>();
        private Sprite _deathMarkSprite;
        private readonly List<Image> _trailPool = new List<Image>();
        private readonly List<TextMeshProUGUI> _popupPool = new List<TextMeshProUGUI>();

        private readonly List<(int Actor, Vector2 Pos)> _playerHits = new List<(int, Vector2)>();
        private readonly List<ReplayTrailPoint> _trailScratch = new List<ReplayTrailPoint>();

        private bool _viewerOpen;
        private bool _openedReplayThisMatch;
        private KeyCode _labelKey = KeyCode.None;
        private ReplayPlayback _playback;
        private ReplayClock _clock;
        private readonly HashSet<int> _selectedActors = new HashSet<int>();
        private bool _seeking;
        private bool _resumeAfterSeek;
        private double _seekRebuiltT;
        private bool _saveDone;
        private int _demoCount;
        private float _demoPhase;

        public bool Exists => _root != null;

        public bool IsOpen => _viewerOpen;

        public bool DemoActive => _demoCount > 0;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            RectTransform rootRect = UiKit.CreateRect(layerRoot, "WW_ReplayViewer",
                Vector2.zero, new Vector2(1920f, 1080f));
            _root = rootRect.gameObject;

            RectTransform toggleRoot = _toggle.Build(rootRect, "ToggleButton", ToggleStyle,
                "icon_replay_viewer", BuildToggleLabel(KeyCode.None), "icon_replay_viewer_hover");
            Vector2 toggleSize = toggleRoot.sizeDelta;
            toggleRoot.anchoredPosition = new Vector2(
                -(1920f * 0.5f) + toggleSize.x * 0.5f + ToggleButtonMargin,
                -(1080f * 0.5f) + toggleSize.y * 0.5f + ToggleButtonMargin);

            RectTransform viewerRect = UiKit.CreateRect(rootRect, "Viewer", Vector2.zero, new Vector2(1920f, 1080f));
            _viewerRoot = viewerRect.gameObject;

            RectTransform mapPanel = UiKit.CreateRect(viewerRect, "MapPanel",
                new Vector2(0f, MapCenterY), new Vector2(PanelWidth, MapHeight));
            UiKit.CreateImage(mapPanel, "PanelBg", Vector2.zero,
                new Vector2(PanelWidth + 12f, MapHeight + 12f), PanelFrameColor);

            var mapGo = new GameObject("MapImage", typeof(RectTransform));
            _mapImageRect = (RectTransform)mapGo.transform;
            _mapImageRect.SetParent(mapPanel, false);
            _mapImageRect.anchorMin = _mapImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            _mapImageRect.anchoredPosition = Vector2.zero;
            _mapImageRect.sizeDelta = new Vector2(PanelWidth, MapHeight);
            _mapImage = mapGo.AddComponent<RawImage>();
            _mapImage.color = Color.white;
            _mapImage.raycastTarget = false;

            _markersRoot = UiKit.CreateRect(mapPanel, "Markers", Vector2.zero, new Vector2(PanelWidth, MapHeight));
            _markersRoot.gameObject.AddComponent<RectMask2D>();
            _trailLayer = CreateMarkerLayer("Trails");
            _epLayer = CreateMarkerLayer("Eps");
            _deathMarkLayer = CreateMarkerLayer("DeathMarks");
            _cartLayer = CreateMarkerLayer("Carts");
            _valuableLayer = CreateMarkerLayer("Valuables");
            _itemLayer = CreateMarkerLayer("Items");
            _enemyLayer = CreateMarkerLayer("Enemies");
            _playerLayer = CreateMarkerLayer("Players");

            _popupRoot = UiKit.CreateRect(mapPanel, "Popups", Vector2.zero, new Vector2(PanelWidth, MapHeight));
            _popupRoot.gameObject.AddComponent<RectMask2D>();

            _stampRoot = UiKit.CreateRect(mapPanel, "Stamp", Vector2.zero, new Vector2(PanelWidth, MapHeight));
            _stampRoot.gameObject.AddComponent<RectMask2D>();
            _stampLabel = UiKit.CreateText(_stampRoot, "StampLabel", Vector2.zero,
                new Vector2(PanelWidth, 240f), "", StampFontSize, StampNoExecutionColor,
                TextAlignmentOptions.Center);
            _stampLabel.enableAutoSizing = true;
            _stampLabel.fontSizeMin = 46f;
            _stampLabel.fontSizeMax = StampFontSize;
            _stampLabel.gameObject.SetActive(false);

            _danmakuRoot = UiKit.CreateRect(mapPanel, "Danmaku", Vector2.zero, new Vector2(PanelWidth, MapHeight));
            _danmakuRoot.gameObject.AddComponent<RectMask2D>();
            _danmakuMeasurer = UiKit.CreateText(_danmakuRoot, "Measurer", new Vector2(0f, -10000f),
                new Vector2(10f, 10f), "", DanmakuBodyFontSize, Color.clear, TextAlignmentOptions.Center);
            BuildDanmakuOutlineMaterial();

            BuildGauge(viewerRect);

            RectTransform bar = UiKit.CreateRect(viewerRect, "ControlBar",
                new Vector2(0f, BarCenterY), new Vector2(PanelWidth, BarHeight));
            UiKit.CreateImage(bar, "BarBg", Vector2.zero,
                new Vector2(PanelWidth + 12f, BarHeight + 12f), BarBgColor);

            _playButtonSprite = AssetCatalog.GetSprite("emoji_play_button");
            _pauseButtonSprite = AssetCatalog.GetSprite("emoji_pause_button");
            _speedButtonSprite = AssetCatalog.GetSprite("emoji_fast_forward_button");
            _saveButtonSprite = AssetCatalog.GetSprite("emoji_floppy_disk");
            _deathMarkSprite = AssetCatalog.GetSprite("emoji_cross_mark");

            float x = -PanelWidth * 0.5f + PlayButtonWidth * 0.5f + 16f;
            BuildButton(bar, "PlayButton", new Vector2(x, 0f), new Vector2(PlayButtonWidth, ButtonHeight),
                out _playButtonBg, out _playButtonIcon, out _playButtonLabel);
            _playButtonRect = _playButtonBg.rectTransform;

            x += PlayButtonWidth * 0.5f + 10f + SpeedButtonWidth * 0.5f;
            BuildButton(bar, "SpeedButton", new Vector2(x, 0f), new Vector2(SpeedButtonWidth, ButtonHeight),
                out _speedButtonBg, out _speedButtonIcon, out _speedButtonLabel);
            _speedButtonRect = _speedButtonBg.rectTransform;

            float seekCenter = (SeekLeft + SeekRight) * 0.5f;
            float seekWidth = SeekRight - SeekLeft;
            _seekHitRect = UiKit.CreateRect(bar, "SeekHit",
                new Vector2(seekCenter, 0f), new Vector2(seekWidth, SeekHitHeight));
            _seekBarRect = UiKit.CreateRect(_seekHitRect, "SeekBar",
                Vector2.zero, new Vector2(seekWidth, SeekBarThickness));
            var seekBg = UiKit.CreateImage(_seekBarRect, "SeekBg", Vector2.zero,
                new Vector2(seekWidth, SeekBarThickness), SeekBgColor);
            UiKit.Stretch(seekBg.rectTransform);

            _seekFill = UiKit.CreateImage(_seekBarRect, "SeekFill", Vector2.zero,
                new Vector2(0f, SeekBarThickness), SeekFillColor);
            var fillRect = _seekFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;

            var handleImg = UiKit.CreateImage(_seekBarRect, "SeekHandle", Vector2.zero,
                new Vector2(SeekHandleSize, SeekHandleSize), Color.white);
            handleImg.sprite = UiKit.CircleSprite();
            _seekHandle = handleImg.rectTransform;
            _seekHandle.anchorMin = _seekHandle.anchorMax = new Vector2(0f, 0.5f);

            _timeLabel = UiKit.CreateText(bar, "Time",
                new Vector2(SeekRight + 16f + TimeLabelWidth * 0.5f, 0f),
                new Vector2(TimeLabelWidth, ButtonHeight),
                "", TimeFontSize, new Color(1f, 0.95f, 0.8f, 1f), TextAlignmentOptions.Center);

            BuildButton(bar, "SaveButton",
                new Vector2(PanelWidth * 0.5f - SaveButtonWidth * 0.5f - 16f, 0f),
                new Vector2(SaveButtonWidth, ButtonHeight),
                out _saveButtonBg, out _saveButtonIcon, out _saveButtonLabel);
            _saveButtonRect = _saveButtonBg.rectTransform;

            _statusLabel = UiKit.CreateText(viewerRect, "Status",
                new Vector2(0f, StatusCenterY), new Vector2(PanelWidth, 26f),
                "", StatusFontSize, StatusMutedColor, TextAlignmentOptions.Center);

            _viewerRoot.SetActive(false);
            _root.SetActive(false);
            WLog.Line("replay_viewer_built", secret: false, ("icon", _toggle.HasIcon ? 1 : 0));
        }

        private RectTransform CreateMarkerLayer(string name)
            => UiKit.CreateRect(_markersRoot, name, Vector2.zero, new Vector2(PanelWidth, MapHeight));

        private void BuildGauge(RectTransform viewerRect)
        {
            RectTransform gauge = UiKit.CreateRect(viewerRect, "Gauge",
                new Vector2(0f, GaugeCenterY), new Vector2(PanelWidth, GaugeHeight));
            _gaugeRoot = gauge.gameObject;
            Image bg = UiKit.CreateImage(gauge, "Bg", Vector2.zero,
                new Vector2(PanelWidth, GaugeHeight), GaugeBgColor);
            UiKit.Stretch(bg.rectTransform);

            var trackSize = new Vector2(GaugeTrackWidth, GaugeTrackHeight);
            Image lossTrack = UiKit.CreateImage(gauge, "LossTrack",
                new Vector2(0f, GaugeLossRowY), trackSize, GaugeTrackColor);
            _gaugeLossFill = UiKit.CreateFilledImage(lossTrack.rectTransform, "LossFill",
                Vector2.zero, trackSize, GaugeLossColor);

            Image deliveryTrack = UiKit.CreateImage(gauge, "DeliveryTrack",
                new Vector2(0f, GaugeDeliveryRowY), trackSize, GaugeTrackColor);
            _gaugeDeliveryTrack = deliveryTrack.gameObject;
            _gaugeDeliveryFill = UiKit.CreateFilledImage(deliveryTrack.rectTransform, "DeliveryFill",
                Vector2.zero, trackSize, GaugeDeliveryColor);
            _gaugeDeliveryFill.fillOrigin = (int)Image.OriginHorizontal.Right;

            _gaugeLossText = UiKit.CreateText(gauge, "LossText",
                new Vector2(-GaugeTrackWidth * 0.25f, GaugeTextRowY),
                new Vector2(GaugeTrackWidth * 0.5f, 22f),
                "", GaugeTextFontSize, GaugeTextColor, TextAlignmentOptions.Center);
            _gaugeDeliveredText = UiKit.CreateText(gauge, "DeliveredText",
                new Vector2(GaugeTrackWidth * 0.25f, GaugeTextRowY),
                new Vector2(GaugeTrackWidth * 0.5f, 22f),
                "", GaugeTextFontSize, GaugeDeliveryColor, TextAlignmentOptions.Center);

            Sprite lossIcon = AssetCatalog.GetSprite("icon_gauge_valuable_loss");
            if (lossIcon != null)
            {
                Image img = UiKit.CreateImage(gauge, "LossIcon",
                    new Vector2(-GaugeTrackWidth * 0.5f, GaugeLossRowY),
                    new Vector2(GaugeIconSize, GaugeIconSize), Color.white);
                img.sprite = lossIcon;
                img.preserveAspect = true;
            }
            Sprite deliveryIcon = AssetCatalog.GetSprite("icon_gauge_delivery");
            if (deliveryIcon != null)
            {
                Image img = UiKit.CreateImage(gauge, "DeliveryIcon",
                    new Vector2(GaugeTrackWidth * 0.5f, GaugeDeliveryRowY),
                    new Vector2(GaugeIconSize, GaugeIconSize), Color.white);
                img.sprite = deliveryIcon;
                img.preserveAspect = true;
                _gaugeDeliveryIcon = img.gameObject;
            }

            _gaugeRoot.SetActive(false);
        }

        public void Tick(bool resultVisible, ReplayRecorder recorder,
            float orthoSize, int resolutionPreset, KeyCode toggleKey, bool keysFree,
            Func<Vector2, bool> pointerOnChat, Func<ReplayExportReport> export)
        {
            if (_root == null) return;

            bool window = resultVisible || DemoActive;
            if (_root.activeSelf != window) _root.SetActive(window);
            if (!window)
            {
                if (_viewerOpen) CloseViewer();
                return;
            }

            bool hasData = DemoActive || (recorder != null && recorder.SegmentCount > 0);
            if (_toggle.Container != null && _toggle.Container.gameObject.activeSelf != hasData)
            {
                _toggle.Container.gameObject.SetActive(hasData);
            }
            if (!hasData)
            {
                if (_viewerOpen) CloseViewer();
                return;
            }

            UiKit.KeepCursorFree();

            if (_labelKey != toggleKey)
            {
                _labelKey = toggleKey;
                _toggle.SetLabel(BuildToggleLabel(toggleKey));
            }

            _toggle.SetHover(_toggle.IsPointerOverOpaqueIcon());
            bool keyPressed = resultVisible && keysFree
                && toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey);
            if (_toggle.WasClicked() || keyPressed)
            {
                if (_viewerOpen) CloseViewer();
                else OpenViewer(recorder, orthoSize, resolutionPreset);
            }
            if (DemoActive && !_viewerOpen)
            {
                OpenViewer(recorder, orthoSize, resolutionPreset);
            }
            if (!_viewerOpen) return;

            try
            {
                double prevT = _clock != null ? _clock.T : 0;
                bool wasPlaying = _clock != null && _clock.Playing;
                _clock?.Tick(Time.unscaledDeltaTime);

                HandleInput(pointerOnChat, export);
                UpdateControls();
                UpdateMarkers();
                UpdateDanmaku(prevT, wasPlaying);
                UpdateGauge();
            }
            catch (Exception e)
            {
                WLog.Line("replay_viewer_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void SetDemo(int count)
        {
            _demoCount = Math.Max(0, count);
            if (_demoCount == 0 && _viewerOpen && _playback == null)
            {
                CloseViewer();
            }
            WLog.Line("replay_viewer_demo", secret: false, ("count", _demoCount));
        }

        public void Destroy()
        {
            if (_viewerOpen) CloseViewer();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _viewerRoot = null;
            _mapImage = null;
            _mapImageRect = null;
            _markersRoot = null;
            _popupRoot = null;
            _stampRoot = null;
            _danmakuRoot = null;
            _stampLabel = null;
            _danmakuMeasurer = null;
            _danmakuPool.Clear();
            _danmaku = null;
            _danmakuPlayers.Clear();
            if (_danmakuOutlineMaterial != null)
            {
                UnityEngine.Object.Destroy(_danmakuOutlineMaterial);
                _danmakuOutlineMaterial = null;
            }
            _demoChatTimer = 0f;
            _demoChatSeq = 0;
            _trailLayer = null;
            _epLayer = null;
            _deathMarkLayer = null;
            _cartLayer = null;
            _valuableLayer = null;
            _itemLayer = null;
            _enemyLayer = null;
            _playerLayer = null;
            _toggle.Clear();
            _playButtonBg = null;
            _playButtonIcon = null;
            _playButtonLabel = null;
            _playButtonRect = null;
            _speedButtonBg = null;
            _speedButtonIcon = null;
            _speedButtonLabel = null;
            _speedButtonRect = null;
            _saveButtonBg = null;
            _saveButtonIcon = null;
            _saveButtonLabel = null;
            _saveButtonRect = null;
            _playButtonSprite = null;
            _pauseButtonSprite = null;
            _speedButtonSprite = null;
            _saveButtonSprite = null;
            _timeLabel = null;
            _statusLabel = null;
            _seekHitRect = null;
            _seekBarRect = null;
            _seekFill = null;
            _seekHandle = null;
            _seekMeetingShades.Clear();
            _seekDeathTicks.Clear();
            _gaugeRoot = null;
            _gaugeLossFill = null;
            _gaugeDeliveryFill = null;
            _gaugeDeliveryTrack = null;
            _gaugeDeliveryIcon = null;
            _gaugeLossText = null;
            _gaugeDeliveredText = null;
            _gaugeShownLoss = -1;
            _gaugeShownDelivered = -1;
            _gaugeHasDelivery = false;
            _popupPool.Clear();
            _playerPool.Clear();
            _enemyPool.Clear();
            _valuablePool.Clear();
            _itemPool.Clear();
            _cartPool.Clear();
            _epPool.Clear();
            _deathMarkPool.Clear();
            _deathMarkSprite = null;
            _trailPool.Clear();
            _playerHits.Clear();
            _placeholderTex = null;
            _placeholderResolved = false;
            _viewerOpen = false;
            _openedReplayThisMatch = false;
            _labelKey = KeyCode.None;
            _still = null;
            _playback = null;
            _clock = null;
            _selectedActors.Clear();
            _seeking = false;
            _resumeAfterSeek = false;
            _saveDone = false;
            _demoCount = 0;
        }

        private void OpenViewer(ReplayRecorder recorder, float orthoSize, int resolutionPreset)
        {
            _playback = DemoActive ? null : ReplayPlayback.FromRecorder(recorder);
            if (!DemoActive && _playback == null)
            {
                SetStatus(Texts.Get(TextId.ReplayNoData), StatusMutedColor);
                return;
            }
            ReplayPace pace = _playback != null ? _playback.BuildPace() : null;
            _clock = new ReplayClock(_playback != null ? _playback.Duration : 600.0, pace);
            bool autoStarted = !DemoActive && !_openedReplayThisMatch;
            if (autoStarted)
            {
                _openedReplayThisMatch = true;
                _clock.SetPlaying(true);
            }
            _danmaku = new ReplayDanmaku(
                _playback != null ? (IReadOnlyList<(double, int, string)>)_playback.Chats : null,
                _playback != null ? (IReadOnlyList<(double, int)>)_playback.MeetingResults : null,
                pace);
            _danmakuPlayers.Clear();
            if (_playback != null)
            {
                foreach (ReplayPlayerEntry p in _playback.Players) _danmakuPlayers[p.Actor] = p;
            }
            _demoChatTimer = 0f;
            _demoChatSeq = 0;
            _selectedActors.Clear();
            _seeking = false;
            _resumeAfterSeek = false;
            _saveDone = false;
            _gaugeShownLoss = -1;
            _gaugeShownDelivered = -1;
            _gaugeHasDelivery = _playback != null && _playback.DeliveredDollarsAt(_playback.Duration) > 0;
            if (_gaugeDeliveryTrack != null && _gaugeDeliveryTrack.activeSelf != _gaugeHasDelivery)
            {
                _gaugeDeliveryTrack.SetActive(_gaugeHasDelivery);
            }
            if (_gaugeDeliveryIcon != null && _gaugeDeliveryIcon.activeSelf != _gaugeHasDelivery)
            {
                _gaugeDeliveryIcon.SetActive(_gaugeHasDelivery);
            }
            SetStatus(string.Empty, StatusMutedColor);
            _still = MapStillCapture.GetOrCapture(orthoSize, resolutionPreset);
            if (_mapImage != null)
            {
                _mapImage.texture = _still != null && _still.Texture != null
                    ? (Texture)_still.Texture
                    : ResolvePlaceholder();
            }
            BuildSeekStatics();
            _viewerOpen = true;
            if (_viewerRoot != null) _viewerRoot.SetActive(true);
            WLog.Line("replay_viewer_open", secret: false,
                ("demo", _demoCount),
                ("autoStarted", autoStarted),
                ("duration", _clock.Duration),
                ("players", _playback != null ? _playback.Players.Count : 0));
        }

        private void CloseViewer()
        {
            _viewerOpen = false;
            _seeking = false;
            _resumeAfterSeek = false;
            if (_viewerRoot != null) _viewerRoot.SetActive(false);
            _still = null;
            _playback = null;
            _clock = null;
            _danmaku = null;
            _danmakuPlayers.Clear();
            HideDanmakuViews();
            _selectedActors.Clear();
            WLog.Line("replay_viewer_close", secret: false);
        }

        private static string BuildToggleLabel(KeyCode key)
            => Texts.Get(TextId.ReplayToggleLabel) + " [" + (key == KeyCode.None ? "-" : key.ToString()) + "]";

        private void HandleInput(Func<Vector2, bool> pointerOnChat, Func<ReplayExportReport> export)
        {
            Vector2 mouse = Input.mousePosition;

            if (_seeking)
            {
                if (Input.GetMouseButton(0))
                {
                    SeekFromPointer(mouse);
                    return;
                }
                EndSeek();
            }

            SetButtonHover(_playButtonBg, _playButtonRect, mouse);
            SetButtonHover(_speedButtonBg, _speedButtonRect, mouse);
            SetButtonHover(_saveButtonBg, _saveButtonRect, mouse);

            if (!Input.GetMouseButtonDown(0)) return;
            if (pointerOnChat != null && pointerOnChat(mouse)) return;
            if (_toggle.ContainsRect(mouse)) return;

            if (HitRect(_playButtonRect, mouse))
            {
                _clock?.TogglePlay();
                return;
            }
            if (HitRect(_speedButtonRect, mouse))
            {
                if (_clock != null) _clock.Fast = !_clock.Fast;
                return;
            }
            if (HitRect(_saveButtonRect, mouse))
            {
                DoSave(export);
                return;
            }
            if (HitRect(_seekHitRect, mouse))
            {
                BeginSeek(mouse);
                return;
            }
            if (HitRect(_mapImageRect, mouse))
            {
                HandleMapClick(mouse);
            }
        }

        private void BeginSeek(Vector2 mouse)
        {
            _seeking = true;
            _resumeAfterSeek = _clock != null && _clock.Playing;
            _clock?.SetPlaying(false);
            _seekRebuiltT = double.NegativeInfinity;
            SeekFromPointer(mouse);
            WLog.Line("replay_viewer_seek", secret: false,
                ("t", _clock != null ? _clock.T : 0.0), ("resume", _resumeAfterSeek));
        }

        private void EndSeek()
        {
            _seeking = false;
            _danmaku?.RebuildAtSeek(_clock != null ? _clock.T : 0,
                _clock != null && _clock.Fast, MeasureCommentWidth);
            if (_resumeAfterSeek && _clock != null && _clock.T < _clock.Duration)
            {
                _clock.SetPlaying(true);
            }
            _resumeAfterSeek = false;
        }

        private void SeekFromPointer(Vector2 screenPoint)
        {
            if (_clock == null || _seekBarRect == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _seekBarRect, screenPoint, null, out Vector2 local))
            {
                return;
            }
            float width = _seekBarRect.rect.width;
            if (width <= 0f) return;
            float ratio = Mathf.Clamp01((local.x - _seekBarRect.rect.xMin) / width);
            _clock.Seek(ratio * _clock.Duration);
            if (_danmaku != null && Math.Abs(_clock.T - _seekRebuiltT) > SeekRebuildEpsilon)
            {
                _danmaku.RebuildAtSeek(_clock.T, _clock.Fast, MeasureCommentWidth);
                _seekRebuiltT = _clock.T;
            }
        }

        private void HandleMapClick(Vector2 screenPoint)
        {
            if (_markersRoot == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _markersRoot, screenPoint, null, out Vector2 local))
            {
                return;
            }

            int bestActor = 0;
            float bestSq = MarkerClickRadius * MarkerClickRadius;
            bool found = false;
            for (int i = 0; i < _playerHits.Count; i++)
            {
                Vector2 d = _playerHits[i].Pos - local;
                float sq = d.sqrMagnitude;
                if (sq <= bestSq)
                {
                    bestSq = sq;
                    bestActor = _playerHits[i].Actor;
                    found = true;
                }
            }

            if (found)
            {
                if (!_selectedActors.Add(bestActor)) _selectedActors.Remove(bestActor);
            }
            else
            {
                _selectedActors.Clear();
            }
        }

        private void DoSave(Func<ReplayExportReport> export)
        {
            if (export == null || DemoActive) return;
            ReplayExportReport report;
            try
            {
                report = export();
            }
            catch (Exception e)
            {
                WLog.Line("replay_viewer_save_error", secret: false, ("err", e.Message));
                report = new ReplayExportReport { Outcome = ReplayExportOutcome.Failed };
            }

            switch (report.Outcome)
            {
                case ReplayExportOutcome.Saved:
                    _saveDone = true;
                    SetStatus(
                        Texts.Get(report.ToDownloads
                            ? TextId.ReplaySavedToDownloads
                            : TextId.ReplaySavedToModFolder) + "  " + (report.FileName ?? ""),
                        StatusOkColor);
                    break;
                case ReplayExportOutcome.AlreadyExists:
                    _saveDone = true;
                    SetStatus(Texts.Get(TextId.ReplayAlreadySaved) + "  " + (report.FileName ?? ""),
                        StatusMutedColor);
                    break;
                case ReplayExportOutcome.Empty:
                    SetStatus(Texts.Get(TextId.ReplayNoData), StatusMutedColor);
                    break;
                default:
                    SetStatus(Texts.Get(TextId.ReplaySaveFailed), StatusErrorColor);
                    break;
            }
        }

        private void UpdateControls()
        {
            if (_clock == null) return;

            SetButtonContent(_playButtonBg, _playButtonIcon, _playButtonLabel,
                _clock.Playing ? _pauseButtonSprite : _playButtonSprite,
                Texts.Get(_clock.Playing ? TextId.ReplayPause : TextId.ReplayPlay));

            int speed = (int)_clock.EffectiveSpeed();
            SetButtonContent(_speedButtonBg, _speedButtonIcon, _speedButtonLabel,
                _speedButtonSprite,
                _speedButtonSprite != null ? "×" + speed : Texts.Format(TextId.ReplaySpeedFormat, speed));

            SetButtonContent(_saveButtonBg, _saveButtonIcon, _saveButtonLabel,
                DemoActive ? null : _saveButtonSprite,
                DemoActive ? "-" : Texts.Get(_saveDone ? TextId.ReplaySavedButton : TextId.ReplaySaveButton));
            if (_timeLabel != null)
            {
                string want = FormatClock(_clock.T) + " / " + FormatClock(_clock.Duration);
                if (_timeLabel.text != want) _timeLabel.text = want;
            }

            float width = _seekBarRect != null ? _seekBarRect.rect.width : 0f;
            float ratio = _clock.Duration > 0 ? (float)(_clock.T / _clock.Duration) : 0f;
            if (_seekFill != null)
            {
                _seekFill.rectTransform.sizeDelta = new Vector2(width * ratio, SeekBarThickness);
            }
            if (_seekHandle != null)
            {
                _seekHandle.anchoredPosition = new Vector2(width * ratio, 0f);
            }
        }

        private void BuildSeekStatics()
        {
            float width = SeekRight - SeekLeft;
            double duration = _playback != null ? _playback.Duration : 0;

            int shadeCount = 0;
            if (_playback != null && duration > 0)
            {
                for (int i = 0; i < _playback.Meetings.Count; i++)
                {
                    (double start, double end) = _playback.Meetings[i];
                    float x0 = (float)(start / duration) * width;
                    float x1 = (float)(end / duration) * width;
                    Image shade = EnsureSeekOverlay(_seekMeetingShades, shadeCount++, "MeetingShade", SeekMeetingColor);
                    var rect = shade.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = new Vector2(x0, 0f);
                    rect.sizeDelta = new Vector2(Mathf.Max(2f, x1 - x0), SeekBarThickness);
                }
            }
            HideExtra(_seekMeetingShades, shadeCount);

            int tickCount = 0;
            if (_playback != null && duration > 0)
            {
                for (int i = 0; i < _playback.Deaths.Count; i++)
                {
                    float x = (float)(_playback.Deaths[i].T / duration) * width;
                    Image tick = EnsureSeekOverlay(_seekDeathTicks, tickCount++, "DeathTick", SeekDeathColor);
                    var rect = tick.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(x, 0f);
                    rect.sizeDelta = new Vector2(2f, 16f);
                }
            }
            HideExtra(_seekDeathTicks, tickCount);
        }

        private static void BuildButton(RectTransform parent, string name, Vector2 pos, Vector2 size,
            out Image bg, out Image icon, out TextMeshProUGUI label)
        {
            bg = UiKit.CreateRoundedImage(parent, name, pos, size, ButtonBgColor);
            icon = UiKit.CreateImage(bg.rectTransform, "Icon", Vector2.zero,
                new Vector2(ButtonIconSize, ButtonIconSize), Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.gameObject.SetActive(false);
            label = UiKit.CreateText(bg.rectTransform, "Label", Vector2.zero, size,
                "", ButtonFontSize, Color.white, TextAlignmentOptions.Center);
        }

        private static void SetButtonContent(Image bg, Image icon, TextMeshProUGUI label,
            Sprite sprite, string text)
        {
            if (bg == null || icon == null || label == null) return;

            float width = bg.rectTransform.sizeDelta.x;
            bool hasIcon = sprite != null;
            if (icon.gameObject.activeSelf != hasIcon) icon.gameObject.SetActive(hasIcon);

            if (hasIcon)
            {
                if (icon.sprite != sprite) icon.sprite = sprite;
                float iconX = -width * 0.5f + ButtonIconPadding + ButtonIconSize * 0.5f;
                icon.rectTransform.anchoredPosition = new Vector2(iconX, 0f);

                float labelLeft = iconX + ButtonIconSize * 0.5f + ButtonIconLabelGap;
                float labelRight = width * 0.5f - ButtonIconPadding;
                label.rectTransform.anchoredPosition = new Vector2((labelLeft + labelRight) * 0.5f, 0f);
                label.rectTransform.sizeDelta = new Vector2(labelRight - labelLeft, bg.rectTransform.sizeDelta.y);
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.fontSizeMax = ButtonIconLabelFontSize;
            }
            else
            {
                label.rectTransform.anchoredPosition = Vector2.zero;
                label.rectTransform.sizeDelta = bg.rectTransform.sizeDelta;
                label.enableAutoSizing = false;
                label.fontSize = ButtonFontSize;
            }

            if (label.text != text) label.text = text;
        }

        private static void SetButtonHover(Image bg, RectTransform rect, Vector2 mouse)
        {
            if (bg == null || rect == null) return;
            Color want = HitRect(rect, mouse) ? ButtonHoverColor : ButtonBgColor;
            if (bg.color != want) bg.color = want;
        }

        private static bool HitRect(RectTransform rect, Vector2 screenPoint)
        {
            return rect != null && rect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
        }

        private void SetStatus(string text, Color color)
        {
            if (_statusLabel == null) return;
            _statusLabel.text = text ?? string.Empty;
            _statusLabel.color = color;
        }

        private static string FormatClock(double seconds)
        {
            if (seconds < 0) seconds = 0;
            int total = (int)seconds;
            return (total / 60).ToString() + ":" + (total % 60).ToString("00");
        }

        private Texture ResolvePlaceholder()
        {
            if (_placeholderResolved) return _placeholderTex;
            _placeholderResolved = true;
            try
            {
                _placeholderTex = AssetCatalog.GetTexture("mapoverlay_placeholder");
            }
            catch (Exception e)
            {
                WLog.Line("replay_viewer_placeholder_error", secret: false, ("err", e.Message));
                _placeholderTex = null;
            }
            return _placeholderTex;
        }
    }
}
