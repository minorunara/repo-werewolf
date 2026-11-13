#nullable enable
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Core.Replay;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed partial class ReplayViewerPanel : IClientPanel
    {
        public string LayerName => ResultScreen.Layer;

        private const float PanelWidth = HudGeometry.MapOverlay.Width;
        private const float MapHeight = HudGeometry.MapOverlay.Height;
        private const float MapCenterY = 10f;
        private const float BarHeight = 56f;
        private const float BarCenterY = MapCenterY - MapHeight * 0.5f - 6f - BarHeight * 0.5f;
        private const float StatusCenterY = BarCenterY - BarHeight * 0.5f - 14f;
        private const float ToggleButtonMargin = HudGeometry.ReplayEntry.Margin;

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
        private const float TrailUnderThickness = 4f;
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
        private const float StampEpigraphFontSize = StampFontSize * 0.4f;
        private const float StampTitleWithEpigraphOffsetY = StampFontSize * 0.22f;
        private const float StampEpigraphOffsetY = -StampFontSize * 0.4f;
        private const float DemoChatIntervalSec = 0.72f;

        private static readonly Color DanmakuMetaColor = new Color(0.85f, 0.85f, 0.88f, 0.75f);
        private static readonly Color DanmakuMetaWolfColor = new Color(1f, 0.5f, 0.5f, 0.85f);
        private static readonly Color StampExecutedColor = new Color(0.95f, 0.3f, 0.26f, 1f);
        private static readonly Color StampNoExecutionColor = new Color(0.92f, 0.92f, 0.95f, 1f);
        private static readonly Color StampVillagerWinColor = new Color(0.31f, 0.75f, 0.42f, 1f);

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
        private static readonly Color SeekCapColor = new Color(0.45f, 0.45f, 0.5f, 0.55f);
        private static readonly Color EnemyColor = new Color(0.92f, 0.22f, 0.2f, 1f);
        private static readonly Color ValuableColor = new Color(0.95f, 0.8f, 0.3f, 1f);
        private static readonly Color ItemColor = new Color(0.25f, 0.82f, 0.9f, 1f);
        private static readonly Color CartColor = new Color(0.62f, 0.42f, 0.22f, 1f);
        private static readonly Color FrameDefaultColor = new Color(0.06f, 0.06f, 0.1f, 0.95f);
        private static readonly Color FrameSelfColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color FrameWolfColor = new Color(0.95f, 0.2f, 0.18f, 1f);
        private static readonly Color TrailUnderColor = new Color(0.02f, 0.02f, 0.05f, 0.8f);
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
            iconSize: HudGeometry.ReplayEntry.IconSize,
            labelWidth: HudGeometry.ReplayEntry.LabelWidth,
            labelHeight: HudGeometry.ReplayEntry.LabelHeight,
            labelFontSize: 24f,
            labelColor: new Color(1f, 0.9f, 0.6f, 1f),
            fallbackText: "▶",
            fallbackPlateColor: new Color(0.08f, 0.08f, 0.10f, 0.55f),
            fallbackTextColor: new Color(0.95f, 0.95f, 0.95f, 0.95f));

        private sealed class MapView
        {
            public MapView(RawImage image, RectTransform imageRect, RectTransform markersRoot,
                RectTransform popupRoot, RectTransform stampRoot, RectTransform danmakuRoot,
                RectTransform trailUnderLayer, RectTransform trailLayer, RectTransform epLayer,
                RectTransform deathMarkLayer, RectTransform cartLayer, RectTransform valuableLayer,
                RectTransform itemLayer, RectTransform enemyLayer, RectTransform playerLayer,
                TextMeshProUGUI stampLabel, TextMeshProUGUI stampEpigraphLabel,
                TextMeshProUGUI danmakuMeasurer)
            {
                Image = image;
                ImageRect = imageRect;
                MarkersRoot = markersRoot;
                PopupRoot = popupRoot;
                StampRoot = stampRoot;
                DanmakuRoot = danmakuRoot;
                TrailUnderLayer = trailUnderLayer;
                TrailLayer = trailLayer;
                EpLayer = epLayer;
                DeathMarkLayer = deathMarkLayer;
                CartLayer = cartLayer;
                ValuableLayer = valuableLayer;
                ItemLayer = itemLayer;
                EnemyLayer = enemyLayer;
                PlayerLayer = playerLayer;
                StampLabel = stampLabel;
                StampEpigraphLabel = stampEpigraphLabel;
                DanmakuMeasurer = danmakuMeasurer;
            }

            public RawImage Image { get; }
            public RectTransform ImageRect { get; }
            public RectTransform MarkersRoot { get; }
            public RectTransform PopupRoot { get; }
            public RectTransform StampRoot { get; }
            public RectTransform DanmakuRoot { get; }
            public RectTransform TrailUnderLayer { get; }
            public RectTransform TrailLayer { get; }
            public RectTransform EpLayer { get; }
            public RectTransform DeathMarkLayer { get; }
            public RectTransform CartLayer { get; }
            public RectTransform ValuableLayer { get; }
            public RectTransform ItemLayer { get; }
            public RectTransform EnemyLayer { get; }
            public RectTransform PlayerLayer { get; }
            public TextMeshProUGUI StampLabel { get; }
            public TextMeshProUGUI StampEpigraphLabel { get; }
            public TextMeshProUGUI DanmakuMeasurer { get; }
        }

        private sealed class ControlsView
        {
            public ControlsView(Image playBg, Image playIcon, TextMeshProUGUI playLabel,
                Image speedBg, Image speedIcon, TextMeshProUGUI speedLabel,
                Image saveBg, Image saveIcon, TextMeshProUGUI saveLabel,
                TextMeshProUGUI timeLabel, TextMeshProUGUI statusLabel,
                RectTransform seekHitRect, RectTransform seekBarRect, Image seekFill,
                RectTransform seekHandle)
            {
                PlayBg = playBg; PlayIcon = playIcon; PlayLabel = playLabel;
                SpeedBg = speedBg; SpeedIcon = speedIcon; SpeedLabel = speedLabel;
                SaveBg = saveBg; SaveIcon = saveIcon; SaveLabel = saveLabel;
                TimeLabel = timeLabel; StatusLabel = statusLabel;
                SeekHitRect = seekHitRect; SeekBarRect = seekBarRect;
                SeekFill = seekFill; SeekHandle = seekHandle;
            }

            public Image PlayBg { get; }
            public Image PlayIcon { get; }
            public TextMeshProUGUI PlayLabel { get; }
            public RectTransform PlayRect => PlayBg.rectTransform;
            public Image SpeedBg { get; }
            public Image SpeedIcon { get; }
            public TextMeshProUGUI SpeedLabel { get; }
            public RectTransform SpeedRect => SpeedBg.rectTransform;
            public Image SaveBg { get; }
            public Image SaveIcon { get; }
            public TextMeshProUGUI SaveLabel { get; }
            public RectTransform SaveRect => SaveBg.rectTransform;
            public TextMeshProUGUI TimeLabel { get; }
            public TextMeshProUGUI StatusLabel { get; }
            public RectTransform SeekHitRect { get; }
            public RectTransform SeekBarRect { get; }
            public Image SeekFill { get; }
            public RectTransform SeekHandle { get; }
        }

        private sealed class GaugeView
        {
            public GaugeView(GameObject root, Image lossFill, Image deliveryFill,
                GameObject deliveryTrack, GameObject? deliveryIcon,
                TextMeshProUGUI lossText, TextMeshProUGUI deliveredText)
            {
                Root = root; LossFill = lossFill; DeliveryFill = deliveryFill;
                DeliveryTrack = deliveryTrack; DeliveryIcon = deliveryIcon;
                LossText = lossText; DeliveredText = deliveredText;
            }

            public GameObject Root { get; }
            public Image LossFill { get; }
            public Image DeliveryFill { get; }
            public GameObject DeliveryTrack { get; }
            public GameObject? DeliveryIcon { get; }
            public TextMeshProUGUI LossText { get; }
            public TextMeshProUGUI DeliveredText { get; }
        }

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, GameObject viewerRoot, MapView map,
                ControlsView controls, GaugeView gauge)
            {
                Root = root; ViewerRoot = viewerRoot; Map = map; Controls = controls; Gauge = gauge;
            }

            public GameObject Root { get; }
            public GameObject ViewerRoot { get; }
            public MapView Map { get; }
            public ControlsView Controls { get; }
            public GaugeView Gauge { get; }
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(ReplayViewerPanel));
        private GameObject? _root => _view?.Root;
        private GameObject? _viewerRoot => _view?.ViewerRoot;
        private RawImage? _mapImage => _view?.Map.Image;
        private RectTransform? _mapImageRect => _view?.Map.ImageRect;
        private RectTransform? _markersRoot => _view?.Map.MarkersRoot;
        private RectTransform? _popupRoot => _view?.Map.PopupRoot;
        private RectTransform? _stampRoot => _view?.Map.StampRoot;
        private RectTransform? _danmakuRoot => _view?.Map.DanmakuRoot;

        private RectTransform? _trailUnderLayer => _view?.Map.TrailUnderLayer;
        private RectTransform? _trailLayer => _view?.Map.TrailLayer;
        private RectTransform? _epLayer => _view?.Map.EpLayer;
        private RectTransform? _deathMarkLayer => _view?.Map.DeathMarkLayer;
        private RectTransform? _cartLayer => _view?.Map.CartLayer;
        private RectTransform? _valuableLayer => _view?.Map.ValuableLayer;
        private RectTransform? _itemLayer => _view?.Map.ItemLayer;
        private RectTransform? _enemyLayer => _view?.Map.EnemyLayer;
        private RectTransform? _playerLayer => _view?.Map.PlayerLayer;
        private readonly IconToggleButton _toggle = new IconToggleButton();

        private Image? _playButtonBg => _view?.Controls.PlayBg;
        private Image? _playButtonIcon => _view?.Controls.PlayIcon;
        private TextMeshProUGUI? _playButtonLabel => _view?.Controls.PlayLabel;
        private RectTransform? _playButtonRect => _view?.Controls.PlayRect;
        private Image? _speedButtonBg => _view?.Controls.SpeedBg;
        private Image? _speedButtonIcon => _view?.Controls.SpeedIcon;
        private TextMeshProUGUI? _speedButtonLabel => _view?.Controls.SpeedLabel;
        private RectTransform? _speedButtonRect => _view?.Controls.SpeedRect;
        private Image? _saveButtonBg => _view?.Controls.SaveBg;
        private Image? _saveButtonIcon => _view?.Controls.SaveIcon;
        private TextMeshProUGUI? _saveButtonLabel => _view?.Controls.SaveLabel;
        private RectTransform? _saveButtonRect => _view?.Controls.SaveRect;
        private Sprite? _playButtonSprite;
        private Sprite? _pauseButtonSprite;
        private Sprite? _speedButtonSprite;
        private Sprite? _saveButtonSprite;
        private TextMeshProUGUI? _timeLabel => _view?.Controls.TimeLabel;
        private TextMeshProUGUI? _statusLabel => _view?.Controls.StatusLabel;

        private RectTransform? _seekHitRect => _view?.Controls.SeekHitRect;
        private RectTransform? _seekBarRect => _view?.Controls.SeekBarRect;
        private Image? _seekFill => _view?.Controls.SeekFill;
        private RectTransform? _seekHandle => _view?.Controls.SeekHandle;
        private readonly List<Image> _seekMeetingShades = new List<Image>();
        private readonly List<Image> _seekDeathTicks = new List<Image>();
        private readonly List<Image> _seekCapBands = new List<Image>();

        private GameObject? _gaugeRoot => _view?.Gauge.Root;
        private Image? _gaugeLossFill => _view?.Gauge.LossFill;
        private Image? _gaugeDeliveryFill => _view?.Gauge.DeliveryFill;
        private GameObject? _gaugeDeliveryTrack => _view?.Gauge.DeliveryTrack;
        private GameObject? _gaugeDeliveryIcon => _view?.Gauge.DeliveryIcon;
        private TextMeshProUGUI? _gaugeLossText => _view?.Gauge.LossText;
        private TextMeshProUGUI? _gaugeDeliveredText => _view?.Gauge.DeliveredText;
        private int _gaugeShownLoss = -1;
        private int _gaugeShownDelivered = -1;
        private bool _gaugeHasDelivery;

        private Texture? _placeholderTex;
        private bool _placeholderResolved;

        private sealed class PlayerMarker
        {
            public PlayerMarker(GameObject root, RectTransform rect, Image frame, Image fill,
                TextMeshProUGUI id, TextMeshProUGUI name)
            {
                Root = root; Rect = rect; Frame = frame; Fill = fill; Id = id; Name = name;
            }
            public GameObject Root { get; }
            public RectTransform Rect { get; }
            public Image Frame { get; }
            public Image Fill { get; }
            public TextMeshProUGUI Id { get; }
            public TextMeshProUGUI Name { get; }
        }

        private sealed class DotMarker
        {
            public DotMarker(GameObject root, RectTransform rect, Image dot,
                TextMeshProUGUI? label = null, TextMeshProUGUI? sub = null)
            {
                Root = root; Rect = rect; Dot = dot; Label = label; Sub = sub;
            }
            public GameObject Root { get; }
            public RectTransform Rect { get; }
            public Image Dot { get; }
            public TextMeshProUGUI? Label { get; }
            public TextMeshProUGUI? Sub { get; }
        }

        private sealed class CommentView
        {
            public CommentView(GameObject root, RectTransform rect, CanvasGroup opacity,
                Image accent, TextMeshProUGUI meta, TextMeshProUGUI body1, TextMeshProUGUI body2)
            {
                Root = root; Rect = rect; Opacity = opacity; Accent = accent;
                Meta = meta; Body1 = body1; Body2 = body2;
            }
            public GameObject Root { get; }
            public RectTransform Rect { get; }
            public CanvasGroup Opacity { get; }
            public Image Accent { get; }
            public TextMeshProUGUI Meta { get; }
            public TextMeshProUGUI Body1 { get; }
            public TextMeshProUGUI Body2 { get; }
            public ReplayDanmakuComment? Bound { get; set; }
        }

        private readonly List<CommentView> _danmakuPool = new List<CommentView>();
        private TextMeshProUGUI? _stampLabel => _view?.Map.StampLabel;
        private TextMeshProUGUI? _stampEpigraphLabel => _view?.Map.StampEpigraphLabel;
        private TextMeshProUGUI? _danmakuMeasurer => _view?.Map.DanmakuMeasurer;
        private Material? _danmakuOutlineMaterial;
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
        private Sprite? _deathMarkSprite;
        private readonly List<Image> _trailPool = new List<Image>();
        private readonly List<Image> _trailUnderPool = new List<Image>();
        private readonly List<TextMeshProUGUI> _popupPool = new List<TextMeshProUGUI>();

        private readonly List<(int Actor, Vector2 Pos)> _playerHits = new List<(int, Vector2)>();
        private readonly List<ReplayTrailPoint> _trailScratch = new List<ReplayTrailPoint>();

        private sealed class ViewerSession
        {
            public ViewerSession(bool demo, ReplayPlayback? playback, ReplayClock clock,
                ReplayDanmaku danmaku, MapStillCapture.Still? still)
            {
                if (!demo && playback == null)
                {
                    throw new ArgumentException("Recorded replay sessions require playback.", nameof(playback));
                }
                Demo = demo;
                Playback = playback;
                Clock = clock;
                Danmaku = danmaku;
                Still = still;
            }

            public bool Demo { get; }
            public ReplayPlayback? Playback { get; }
            public ReplayClock Clock { get; }
            public ReplayDanmaku Danmaku { get; }
            public MapStillCapture.Still? Still { get; }
        }

        private ViewerSession? _session;
        private bool _viewerOpen => _session != null;
        private bool _openedReplayThisMatch;
        private KeyCode _labelKey = KeyCode.None;
        private MapStillCapture.Still? _still => _session?.Still;
        private ReplayPlayback? _playback => _session?.Playback;
        private ReplayClock? _clock => _session?.Clock;
        private ReplayDanmaku? _danmaku => _session?.Danmaku;
        private readonly HashSet<int> _selectedActors = new HashSet<int>();
        private bool _seeking;
        private bool _resumeAfterSeek;
        private double _seekRebuiltT;
        private bool _saveDone;
        private bool _capNoticeShown;
        private int _demoCount;
        private float _demoPhase;

        public bool Exists => GetLiveViewOrNull() != null;

        public bool IsOpen => _viewerOpen;

        public bool DemoActive => _demoCount > 0;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            GameObject? root = null;
            try
            {
                RectTransform rootRect = UiKit.CreateStretched(layerRoot, "WW_ReplayViewer");
                root = rootRect.gameObject;

            RectTransform toggleRoot = _toggle.Build(rootRect, "ToggleButton", ToggleStyle,
                "icon_replay_viewer", BuildToggleLabel(KeyCode.None), "icon_replay_viewer_hover");
            UiKit.PinToCorner(toggleRoot, UiKit.BottomLeft, ToggleButtonMargin);

                RectTransform viewerRect = UiKit.CreateStretched(rootRect, "Viewer");
                GameObject viewerRoot = viewerRect.gameObject;

            RectTransform mapPanel = UiKit.CreateRect(viewerRect, "MapPanel",
                new Vector2(0f, MapCenterY), new Vector2(PanelWidth, MapHeight));
            UiKit.CreateImage(mapPanel, "PanelBg", Vector2.zero,
                new Vector2(PanelWidth + 12f, MapHeight + 12f), PanelFrameColor);

            var mapGo = new GameObject("MapImage", typeof(RectTransform));
                RectTransform mapImageRect = (RectTransform)mapGo.transform;
                mapImageRect.SetParent(mapPanel, false);
                mapImageRect.anchorMin = mapImageRect.anchorMax = new Vector2(0.5f, 0.5f);
                mapImageRect.anchoredPosition = Vector2.zero;
                mapImageRect.sizeDelta = new Vector2(PanelWidth, MapHeight);
                RawImage mapImage = mapGo.AddComponent<RawImage>();
                mapImage.color = Color.white;
                mapImage.raycastTarget = false;

                RectTransform markersRoot = UiKit.CreateRect(mapPanel, "Markers", Vector2.zero, new Vector2(PanelWidth, MapHeight));
                markersRoot.gameObject.AddComponent<RectMask2D>();
                RectTransform trailUnderLayer = CreateMarkerLayer(markersRoot, "TrailUnders");
                RectTransform trailLayer = CreateMarkerLayer(markersRoot, "Trails");
                RectTransform epLayer = CreateMarkerLayer(markersRoot, "Eps");
                RectTransform deathMarkLayer = CreateMarkerLayer(markersRoot, "DeathMarks");
                RectTransform cartLayer = CreateMarkerLayer(markersRoot, "Carts");
                RectTransform valuableLayer = CreateMarkerLayer(markersRoot, "Valuables");
                RectTransform itemLayer = CreateMarkerLayer(markersRoot, "Items");
                RectTransform enemyLayer = CreateMarkerLayer(markersRoot, "Enemies");
                RectTransform playerLayer = CreateMarkerLayer(markersRoot, "Players");

                RectTransform popupRoot = UiKit.CreateRect(mapPanel, "Popups", Vector2.zero, new Vector2(PanelWidth, MapHeight));
                popupRoot.gameObject.AddComponent<RectMask2D>();

                RectTransform stampRoot = UiKit.CreateRect(mapPanel, "Stamp", Vector2.zero, new Vector2(PanelWidth, MapHeight));
                stampRoot.gameObject.AddComponent<RectMask2D>();
                TextMeshProUGUI stampLabel = UiKit.CreateText(stampRoot, "StampLabel", Vector2.zero,
                new Vector2(PanelWidth, 240f), "", StampFontSize, StampNoExecutionColor,
                TextAlignmentOptions.Center);
                stampLabel.enableAutoSizing = true;
                stampLabel.fontSizeMin = 46f;
                stampLabel.fontSizeMax = StampFontSize;
                stampLabel.gameObject.SetActive(false);
                TextMeshProUGUI stampEpigraphLabel = UiKit.CreateText(stampRoot, "StampEpigraphLabel",
                new Vector2(0f, StampEpigraphOffsetY), new Vector2(PanelWidth, 100f), "",
                StampEpigraphFontSize, StampNoExecutionColor, TextAlignmentOptions.Center);
                stampEpigraphLabel.fontStyle = FontStyles.Italic;
                stampEpigraphLabel.enableAutoSizing = true;
                stampEpigraphLabel.fontSizeMin = 24f;
                stampEpigraphLabel.fontSizeMax = StampEpigraphFontSize;
                stampEpigraphLabel.gameObject.SetActive(false);

                RectTransform danmakuRoot = UiKit.CreateRect(mapPanel, "Danmaku", Vector2.zero, new Vector2(PanelWidth, MapHeight));
                danmakuRoot.gameObject.AddComponent<RectMask2D>();
                TextMeshProUGUI danmakuMeasurer = UiKit.CreateText(danmakuRoot, "Measurer", new Vector2(0f, -10000f),
                new Vector2(10f, 10f), "", DanmakuBodyFontSize, Color.clear, TextAlignmentOptions.Center);
                BuildDanmakuOutlineMaterial(danmakuMeasurer);

                GaugeView gauge = BuildGauge(viewerRect);

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
                    out Image playButtonBg, out Image playButtonIcon, out TextMeshProUGUI playButtonLabel);

            x += PlayButtonWidth * 0.5f + 10f + SpeedButtonWidth * 0.5f;
                BuildButton(bar, "SpeedButton", new Vector2(x, 0f), new Vector2(SpeedButtonWidth, ButtonHeight),
                    out Image speedButtonBg, out Image speedButtonIcon, out TextMeshProUGUI speedButtonLabel);

            float seekCenter = (SeekLeft + SeekRight) * 0.5f;
            float seekWidth = SeekRight - SeekLeft;
                RectTransform seekHitRect = UiKit.CreateRect(bar, "SeekHit",
                new Vector2(seekCenter, 0f), new Vector2(seekWidth, SeekHitHeight));
                RectTransform seekBarRect = UiKit.CreateRect(seekHitRect, "SeekBar",
                Vector2.zero, new Vector2(seekWidth, SeekBarThickness));
                UiKit.CreateBackdrop(seekBarRect, "SeekBg", SeekBgColor);

                Image seekFill = UiKit.CreateImage(seekBarRect, "SeekFill", Vector2.zero,
                new Vector2(0f, SeekBarThickness), SeekFillColor);
                var fillRect = seekFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;

                var handleImg = UiKit.CreateImage(seekBarRect, "SeekHandle", Vector2.zero,
                new Vector2(SeekHandleSize, SeekHandleSize), Color.white);
            handleImg.sprite = UiKit.CircleSprite();
                RectTransform seekHandle = handleImg.rectTransform;
                seekHandle.anchorMin = seekHandle.anchorMax = new Vector2(0f, 0.5f);

                TextMeshProUGUI timeLabel = UiKit.CreateText(bar, "Time",
                new Vector2(SeekRight + 16f + TimeLabelWidth * 0.5f, 0f),
                new Vector2(TimeLabelWidth, ButtonHeight),
                "", TimeFontSize, new Color(1f, 0.95f, 0.8f, 1f), TextAlignmentOptions.Center);

                BuildButton(bar, "SaveButton",
                new Vector2(PanelWidth * 0.5f - SaveButtonWidth * 0.5f - 16f, 0f),
                new Vector2(SaveButtonWidth, ButtonHeight),
                    out Image saveButtonBg, out Image saveButtonIcon, out TextMeshProUGUI saveButtonLabel);

                TextMeshProUGUI statusLabel = UiKit.CreateText(viewerRect, "Status",
                new Vector2(0f, StatusCenterY), new Vector2(PanelWidth, 26f),
                "", StatusFontSize, StatusMutedColor, TextAlignmentOptions.Center);

                var map = new MapView(mapImage, mapImageRect, markersRoot, popupRoot, stampRoot,
                    danmakuRoot, trailUnderLayer, trailLayer, epLayer, deathMarkLayer, cartLayer,
                    valuableLayer, itemLayer, enemyLayer, playerLayer, stampLabel,
                    stampEpigraphLabel, danmakuMeasurer);
                var controls = new ControlsView(playButtonBg, playButtonIcon, playButtonLabel,
                    speedButtonBg, speedButtonIcon, speedButtonLabel, saveButtonBg, saveButtonIcon,
                    saveButtonLabel, timeLabel, statusLabel, seekHitRect, seekBarRect, seekFill, seekHandle);
                viewerRoot.SetActive(false);
                root.SetActive(false);
                _view = new ViewRefs(root, viewerRoot, map, controls, gauge);
                _textRefresh.Subscribe(() => Exists, ApplyTexts);
            }
            catch
            {
                _view = null;
                ReleaseViewOwnedState();
                if (root != null)
                {
                    root.SetActive(false);
                    UnityEngine.Object.Destroy(root);
                }
                throw;
            }
        }

        private static RectTransform CreateMarkerLayer(RectTransform markersRoot, string name)
            => UiKit.CreateRect(markersRoot, name, Vector2.zero, new Vector2(PanelWidth, MapHeight));

        private static GaugeView BuildGauge(RectTransform viewerRect)
        {
            RectTransform gauge = UiKit.CreateRect(viewerRect, "Gauge",
                new Vector2(0f, GaugeCenterY), new Vector2(PanelWidth, GaugeHeight));
            GameObject gaugeRoot = gauge.gameObject;
            UiKit.CreateBackdrop(gauge, "Bg", GaugeBgColor);

            var trackSize = new Vector2(GaugeTrackWidth, GaugeTrackHeight);
            Image lossTrack = UiKit.CreateImage(gauge, "LossTrack",
                new Vector2(0f, GaugeLossRowY), trackSize, GaugeTrackColor);
            Image gaugeLossFill = UiKit.CreateFilledImage(lossTrack.rectTransform, "LossFill",
                Vector2.zero, trackSize, GaugeLossColor);

            Image deliveryTrack = UiKit.CreateImage(gauge, "DeliveryTrack",
                new Vector2(0f, GaugeDeliveryRowY), trackSize, GaugeTrackColor);
            GameObject gaugeDeliveryTrack = deliveryTrack.gameObject;
            Image gaugeDeliveryFill = UiKit.CreateFilledImage(deliveryTrack.rectTransform, "DeliveryFill",
                Vector2.zero, trackSize, GaugeDeliveryColor);
            gaugeDeliveryFill.fillOrigin = (int)Image.OriginHorizontal.Right;

            TextMeshProUGUI gaugeLossText = UiKit.CreateText(gauge, "LossText",
                new Vector2(-GaugeTrackWidth * 0.25f, GaugeTextRowY),
                new Vector2(GaugeTrackWidth * 0.5f, 22f),
                "", GaugeTextFontSize, GaugeTextColor, TextAlignmentOptions.Center);
            TextMeshProUGUI gaugeDeliveredText = UiKit.CreateText(gauge, "DeliveredText",
                new Vector2(GaugeTrackWidth * 0.25f, GaugeTextRowY),
                new Vector2(GaugeTrackWidth * 0.5f, 22f),
                "", GaugeTextFontSize, GaugeDeliveryColor, TextAlignmentOptions.Center);

            Sprite? lossIcon = AssetCatalog.GetSprite("icon_gauge_valuable_loss");
            if (lossIcon != null)
            {
                Image img = UiKit.CreateImage(gauge, "LossIcon",
                    new Vector2(-GaugeTrackWidth * 0.5f, GaugeLossRowY),
                    new Vector2(GaugeIconSize, GaugeIconSize), Color.white);
                img.sprite = lossIcon;
                img.preserveAspect = true;
            }
            Sprite? deliveryIcon = AssetCatalog.GetSprite("icon_gauge_delivery");
            GameObject? gaugeDeliveryIcon = null;
            if (deliveryIcon != null)
            {
                Image img = UiKit.CreateImage(gauge, "DeliveryIcon",
                    new Vector2(GaugeTrackWidth * 0.5f, GaugeDeliveryRowY),
                    new Vector2(GaugeIconSize, GaugeIconSize), Color.white);
                img.sprite = deliveryIcon;
                img.preserveAspect = true;
                gaugeDeliveryIcon = img.gameObject;
            }

            gaugeRoot.SetActive(false);
            return new GaugeView(gaugeRoot, gaugeLossFill, gaugeDeliveryFill,
                gaugeDeliveryTrack, gaugeDeliveryIcon, gaugeLossText, gaugeDeliveredText);
        }

        public void Tick(bool resultVisible, ReplayRecorder? recorder,
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
                NoteCapCrossing();

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

        private void ApplyTexts()
        {
            _toggle.SetLabel(BuildToggleLabel(_labelKey));
            _gaugeShownLoss = -1;
            _gaugeShownDelivered = -1;
            if (_viewerOpen) UpdateControls();
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            if (_viewerOpen) CloseViewer();
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
            ReleaseViewOwnedState();
            _gaugeShownLoss = -1;
            _gaugeShownDelivered = -1;
            _gaugeHasDelivery = false;
            _openedReplayThisMatch = false;
            _labelKey = KeyCode.None;
            _saveDone = false;
            _demoCount = 0;
        }

        private void OpenViewer(ReplayRecorder? recorder, float orthoSize, int resolutionPreset)
        {
            if (!DemoActive && recorder == null) return;
            bool demo = DemoActive;
            ReplayPlayback? playback = demo ? null : ReplayPlayback.FromRecorder(recorder);
            if (!demo && playback == null)
            {
                SetStatus(Texts.Get(TextId.ReplayNoData), StatusMutedColor);
                return;
            }
            ReplayPace? pace = playback != null ? playback.BuildPace() : null;
            var clock = new ReplayClock(playback != null ? playback.Duration : 600.0, pace) { Fast = true };
            bool autoStarted = !demo && !_openedReplayThisMatch;
            if (autoStarted)
            {
                clock.SetPlaying(true);
            }
            var danmaku = new ReplayDanmaku(
                playback != null ? (IReadOnlyList<(double, int, string)>)playback.Chats : null,
                playback != null ? (IReadOnlyList<(double, int)>)playback.MeetingResults : null,
                pace,
                playback?.GameOver);
            MapStillCapture.Still? still = MapStillCapture.GetOrCapture(orthoSize, resolutionPreset);
            var session = new ViewerSession(demo, playback, clock, danmaku, still);
            _session = session;
            try
            {
                _danmakuPlayers.Clear();
                if (playback != null)
                {
                    foreach (ReplayPlayerEntry p in playback.Players) _danmakuPlayers[p.Actor] = p;
                }
                _demoChatTimer = 0f;
                _demoChatSeq = 0;
                _selectedActors.Clear();
                _seeking = false;
                _resumeAfterSeek = false;
                _saveDone = false;
                _capNoticeShown = false;
                _gaugeShownLoss = -1;
                _gaugeShownDelivered = -1;
                _gaugeHasDelivery = playback != null && playback.DeliveredDollarsAt(playback.Duration) > 0;
                if (_gaugeDeliveryTrack != null && _gaugeDeliveryTrack.activeSelf != _gaugeHasDelivery)
                {
                    _gaugeDeliveryTrack.SetActive(_gaugeHasDelivery);
                }
                if (_gaugeDeliveryIcon != null && _gaugeDeliveryIcon.activeSelf != _gaugeHasDelivery)
                {
                    _gaugeDeliveryIcon.SetActive(_gaugeHasDelivery);
                }
                SetStatus(string.Empty, StatusMutedColor);
                if (_mapImage != null)
                {
                    _mapImage.texture = still != null && still.Texture != null
                        ? (Texture)still.Texture
                        : ResolvePlaceholder();
                }
                BuildSeekStatics();
                if (_viewerRoot != null) _viewerRoot.SetActive(true);
                if (autoStarted) _openedReplayThisMatch = true;
            }
            catch
            {
                _session = null;
                if (_viewerRoot != null) _viewerRoot.SetActive(false);
                _danmakuPlayers.Clear();
                _selectedActors.Clear();
                throw;
            }
            WLog.Line("replay_viewer_open", secret: false,
                ("demo", _demoCount),
                ("autoStarted", autoStarted),
                ("duration", clock.Duration),
                ("players", playback != null ? playback.Players.Count : 0));
        }

        private void CloseViewer()
        {
            _seeking = false;
            _resumeAfterSeek = false;
            if (_viewerRoot != null) _viewerRoot.SetActive(false);
            _session = null;
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

            SetButtonHover(_playButtonBg!, _playButtonRect!, mouse);
            SetButtonHover(_speedButtonBg!, _speedButtonRect!, mouse);
            SetButtonHover(_saveButtonBg!, _saveButtonRect!, mouse);

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
            if (HitRect(_seekHitRect!, mouse))
            {
                BeginSeek(mouse);
                return;
            }
            if (HitRect(_mapImageRect!, mouse))
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

        private static string CapSuffix(ReplayExportReport report)
            => report.PositionCapped ? "  " + Texts.Get(TextId.ReplaySavedCapped) : "";

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
                            : TextId.ReplaySavedToModFolder) + "  " + (report.FileName ?? "")
                        + CapSuffix(report),
                        StatusOkColor);
                    break;
                case ReplayExportOutcome.AlreadyExists:
                    _saveDone = true;
                    SetStatus(Texts.Get(TextId.ReplayAlreadySaved) + "  " + (report.FileName ?? "")
                            + CapSuffix(report),
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

            SetButtonContent(_playButtonBg!, _playButtonIcon!, _playButtonLabel!,
                _clock!.Playing ? _pauseButtonSprite : _playButtonSprite,
                Texts.Get(_clock.Playing ? TextId.ReplayPause : TextId.ReplayPlay));

            int speed = (int)_clock.EffectiveSpeed();
            SetButtonContent(_speedButtonBg!, _speedButtonIcon!, _speedButtonLabel!,
                _speedButtonSprite!,
                _speedButtonSprite != null ? "×" + speed : Texts.Format(TextId.ReplaySpeedFormat, speed));

            SetButtonContent(_saveButtonBg!, _saveButtonIcon!, _saveButtonLabel!,
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

        private void NoteCapCrossing()
        {
            if (_capNoticeShown || _clock == null || _playback == null) return;
            if (_clock.T < _playback.PositionCapT) return;
            _capNoticeShown = true;
            SetStatus(Texts.Get(TextId.ReplayPositionCapped), StatusMutedColor);
        }

        private void BuildSeekStatics()
        {
            float width = SeekRight - SeekLeft;
            double duration = _playback != null ? _playback.Duration : 0;

            int capCount = 0;
            if (_playback != null && duration > 0 && _playback.PositionCapT < duration)
            {
                float capX = (float)(Math.Max(0.0, _playback.PositionCapT) / duration) * width;
                Image band = EnsureSeekOverlay(_seekCapBands, capCount++, "CapBand", SeekCapColor);
                var capRect = band.rectTransform;
                capRect.anchorMin = capRect.anchorMax = new Vector2(0f, 0.5f);
                capRect.pivot = new Vector2(0f, 0.5f);
                capRect.anchoredPosition = new Vector2(capX, 0f);
                capRect.sizeDelta = new Vector2(Mathf.Max(2f, width - capX), SeekBarThickness);
            }
            HideExtra(_seekCapBands, capCount);

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
            Sprite? sprite, string text)
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

        private static bool HitRect(RectTransform? rect, Vector2 screenPoint)
        {
            return rect != null && rect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            ReleaseViewOwnedState();
            return null;
        }

        private void ReleaseViewOwnedState()
        {
            _session = null;
            _toggle.Clear();
            _danmakuPool.Clear();
            _danmakuPlayers.Clear();
            if (_danmakuOutlineMaterial != null)
            {
                UnityEngine.Object.Destroy(_danmakuOutlineMaterial);
                _danmakuOutlineMaterial = null;
            }
            _demoChatTimer = 0f;
            _demoChatSeq = 0;
            _playButtonSprite = null;
            _pauseButtonSprite = null;
            _speedButtonSprite = null;
            _saveButtonSprite = null;
            _seekMeetingShades.Clear();
            _seekDeathTicks.Clear();
            _seekCapBands.Clear();
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
            _trailUnderPool.Clear();
            _playerHits.Clear();
            _placeholderTex = null;
            _placeholderResolved = false;
            _selectedActors.Clear();
            _seeking = false;
            _resumeAfterSeek = false;
            _labelKey = KeyCode.None;
            _gaugeShownLoss = -1;
            _gaugeShownDelivered = -1;
            _gaugeHasDelivery = false;
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

        private Texture? ResolvePlaceholder()
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
