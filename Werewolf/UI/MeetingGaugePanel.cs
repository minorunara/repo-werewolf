using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class MeetingGaugePanel : IClientPanel
    {
        private static readonly Vector2 FullPanelSize = new Vector2(1560f, 104f);
        private static readonly Vector2 FullPanelPos = new Vector2(0f, 400f);
        private static readonly Vector2 CenterPanelPos = new Vector2(0f, 0f);

        private static readonly Vector2 TrackSize = new Vector2(1500f, 20f);
        private static readonly Vector2 TrackPos = new Vector2(0f, -6f);
        private static readonly Color TrackBgColor = new Color(0.10f, 0.10f, 0.14f, 0.9f);
        private static readonly Color FillColor = new Color(0.85f, 0.65f, 0.15f, 0.95f);

        private static readonly Color DeliveryFillColor = new Color(0.30f, 0.80f, 0.95f, 0.95f);
        private static readonly Color QuotaLineColor = new Color(0.25f, 0.55f, 1f, 0.95f);
        private const float QuotaLineWidth = 4f;
        private static readonly Color CheckmateLineColor = new Color(0.95f, 0.12f, 0.12f, 0.95f);

        private static readonly Vector2 PctTextPos = new Vector2(0f, -36f);

        private const float TickWidth = 3f;
        private const float TickHeight = 14f;
        private const float PerkIconSize = 36f;
        private const float TierGap = 2f;
        private const float ScaleTickWidth = 2f;
        private const float ScaleTickHeight = 8f;
        private const float BalloonWidth = 96f;
        private const float BalloonHeight = 26f;
        private static readonly Color LockedTickColor = new Color(0.55f, 0.55f, 0.55f, 0.95f);
        private static readonly Color UnlockedTickColor = new Color(0.35f, 0.95f, 0.35f, 0.95f);
        private static readonly Color LockedIconTint = new Color(0.5f, 0.5f, 0.5f, 0.85f);
        private static readonly Color LockedBalloonBg = new Color(0.18f, 0.18f, 0.20f, 0.92f);
        private static readonly Color UnlockedBalloonBg = new Color(0.16f, 0.32f, 0.16f, 0.92f);
        private static readonly Color LockedTextColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        private static readonly Color UnlockedTextColor = new Color(0.65f, 1f, 0.65f, 1f);

        private GameObject _root;
        private RectTransform _rootRect;
        private RectTransform _trackRect;
        private Image _fillImage;
        private GameObject _deliveryTrack;
        private RectTransform _deliveryTrackRect;
        private Image _deliveryFillImage;
        private Image _quotaLine;
        private Image _checkmateLine;
        private GameObject _deliveryIcon;
        private TextMeshProUGUI _pctText;
        private TextMeshProUGUI _nextUpdateText;
        private readonly List<GameObject> _markerObjects = new List<GameObject>();

        private MeetingGaugeSnapshot _renderedSnapshot;
        private bool _wasMeetingActive;
        private int _lastNextUpdateSec = -1;

        private readonly GaugeReveal _reveal = new GaugeReveal();
        private int _lastRevealedPermille;
        private int _lastRevealedLoss;
        private int _revealUnlockCount;
        private bool _revealBreakPlayed;

        private readonly GaugeReveal _deliveryReveal = new GaugeReveal();
        private int _lastRevealedDeliveryPermille;
        private bool _deliverySfxPlayed;

        public Action OnRevealBreak;

        public Action OnDeliveryReveal;

        public int LastRevealedPermille => _lastRevealedPermille;

        public int LastRevealedLoss => _lastRevealedLoss;

        private readonly string _rootName;
        private readonly Vector2 _panelSize;
        private readonly Vector2 _rootPos;
        private readonly Vector2 _rootAnchor;
        private readonly Vector2 _rootPivot;
        private readonly float _scale;
        private readonly float _markerScale;

        public MeetingGaugePanel()
            : this("WW_MeetingGaugePanel", FullPanelSize, FullPanelPos,
                   new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                   scale: 0.8f, markerScale: 2f) { }

        public MeetingGaugePanel(string rootName, Vector2 panelSize, Vector2 pos, Vector2 anchor, Vector2 pivot,
                                 float scale, float markerScale)
        {
            _rootName = rootName;
            _panelSize = panelSize;
            _rootPos = pos;
            _rootAnchor = anchor;
            _rootPivot = pivot;
            _scale = scale;
            _markerScale = markerScale;
        }

        public bool Exists => _root != null;

        public string LayerName => WerewolfUIManager.RoleGaugeLayer;

        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            Vector2 pctSize = new Vector2(_panelSize.x - 24f, 22f);

            RectTransform rect = UiKit.CreateRect(layerRoot, _rootName, _rootPos, _panelSize);
            rect.anchorMin = rect.anchorMax = _rootAnchor;
            rect.pivot = _rootPivot;
            rect.localScale = Vector3.one * _scale;
            _root = rect.gameObject;
            _rootRect = rect;

            UiKit.CreateImage(rect, "Bg", Vector2.zero, _panelSize, new Color(0.02f, 0.02f, 0.05f, 0.75f));

            Image track = UiKit.CreateImage(rect, "Track", TrackPos, TrackSize, TrackBgColor);
            _trackRect = track.rectTransform;
            _fillImage = UiKit.CreateFilledImage(_trackRect, "Fill", Vector2.zero, TrackSize, FillColor);

            const float nextUpdateLeftPad = 80f;
            Image deliveryTrack = UiKit.CreateImage(rect, "DeliveryTrack",
                TrackPos + new Vector2(0f, -TrackSize.y), TrackSize, TrackBgColor);
            _deliveryTrackRect = deliveryTrack.rectTransform;
            _deliveryTrack = deliveryTrack.gameObject;
            _deliveryFillImage = UiKit.CreateFilledImage(_deliveryTrackRect, "DeliveryFill",
                Vector2.zero, TrackSize, DeliveryFillColor);
            _deliveryFillImage.fillOrigin = (int)Image.OriginHorizontal.Right;
            _quotaLine = UiKit.CreateImage(rect, "QuotaLine",
                new Vector2(0f, TrackPos.y - TrackSize.y / 2f),
                new Vector2(QuotaLineWidth, TrackSize.y * 2f + 8f), QuotaLineColor);
            _quotaLine.gameObject.SetActive(false);
            _checkmateLine = UiKit.CreateImage(rect, "CheckmateLine",
                new Vector2(0f, TrackPos.y - TrackSize.y / 2f),
                new Vector2(QuotaLineWidth, TrackSize.y * 2f + 8f), CheckmateLineColor);
            _checkmateLine.gameObject.SetActive(false);
            _deliveryTrack.SetActive(false);

            _pctText = UiKit.CreateText(rect, "PctText", PctTextPos, pctSize,
                "", 22f * _markerScale, Color.white, TextAlignmentOptions.Center);

            Vector2 nextUpdateSize = new Vector2(pctSize.x - nextUpdateLeftPad * 2f, 28f);
            _nextUpdateText = UiKit.CreateText(rect, "NextUpdate",
                new Vector2(PctTextPos.x + nextUpdateLeftPad, PctTextPos.y), nextUpdateSize,
                "", 24f * _markerScale, new Color(0.9f, 0.9f, 0.9f, 0.95f), TextAlignmentOptions.MidlineLeft);

            Sprite gaugeIcon = AssetCatalog.GetSprite("icon_gauge_valuable_loss");
            if (gaugeIcon != null)
            {
                const float iconSize = 128f;
                float iconX = TrackPos.x - TrackSize.x / 2f;
                Image iconImg = UiKit.CreateImage(rect, "GaugeIcon",
                    new Vector2(iconX, TrackPos.y),
                    new Vector2(iconSize, iconSize), Color.white);
                iconImg.sprite = gaugeIcon;
                iconImg.preserveAspect = true;
            }

            Sprite deliveryIcon = AssetCatalog.GetSprite("icon_gauge_delivery");
            if (deliveryIcon != null)
            {
                const float iconSize = 128f;
                float iconX = TrackPos.x + TrackSize.x / 2f;
                Image iconImg = UiKit.CreateImage(rect, "DeliveryIcon",
                    new Vector2(iconX, TrackPos.y - TrackSize.y),
                    new Vector2(iconSize, iconSize), Color.white);
                iconImg.sprite = deliveryIcon;
                iconImg.preserveAspect = true;
                _deliveryIcon = iconImg.gameObject;
                _deliveryIcon.SetActive(false);
            }

            _root.SetActive(false);
            WLog.Line("gauge_panel_built", secret: false);
        }

        public void Tick(RolesClientState roles, MeetingClientState meeting, long nowUnixMs)
        {
            if (_root == null) return;
            try
            {
                bool meetingActive = meeting != null && meeting.MeetingActive;

                if (_wasMeetingActive && !meetingActive && roles != null && roles.MeetingGauge != null)
                {
                    roles.ClearMeetingGauge();
                    WLog.Line("gauge_panel_cleared", secret: false);
                }
                _wasMeetingActive = meetingActive;

                MeetingGaugeSnapshot snapshot = roles != null ? roles.MeetingGauge : null;
                bool visible = meetingActive && snapshot != null && meeting.GaugeIntroReady(nowUnixMs);
                if (_root.activeSelf != visible)
                {
                    _root.SetActive(visible);
                    WLog.Line("gauge_panel_visible", secret: false, ("visible", visible));
                }
                if (!visible) return;

                if (_rootRect != null)
                {
                    _rootRect.anchoredPosition = Vector2.Lerp(
                        CenterPanelPos, FullPanelPos, (float)meeting.GaugeMoveProgress(nowUnixMs));
                }

                if (!ReferenceEquals(snapshot, _renderedSnapshot))
                {
                    BeginReveal(snapshot, nowUnixMs);
                    _renderedSnapshot = snapshot;
                }
                else if (_reveal.Active || _deliveryReveal.Active)
                {
                    TickReveal(snapshot, nowUnixMs);
                }
            }
            catch (Exception e)
            {
                WLog.Line("gauge_panel_tick_error", secret: false, ("err", e.Message));
            }
        }

        public void TickPlay(RolesClientState roles, MeetingClientState meeting, GamePhase phase,
                             Role? localRole, long nowUnixMs)
        {
            if (_root == null) return;
            try
            {
                bool warped = meeting != null && meeting.MeetingActive && meeting.WarpDone(nowUnixMs);
                MeetingGaugeSnapshot snapshot = roles != null ? roles.PlayGauge : null;
                bool visible = (phase == GamePhase.Play || phase == GamePhase.Meeting) && !warped
                    && snapshot != null && roles.CanShowHud(localRole);
                if (_root.activeSelf != visible)
                {
                    _root.SetActive(visible);
                    WLog.Line("play_gauge_visible", secret: false, ("visible", visible));
                }
                if (!visible) return;

                if (!ReferenceEquals(snapshot, _renderedSnapshot))
                {
                    Render(snapshot);
                    _renderedSnapshot = snapshot;
                }

                UpdateNextUpdateText(roles, nowUnixMs);
            }
            catch (Exception e)
            {
                WLog.Line("play_gauge_tick_error", secret: false, ("err", e.Message));
            }
        }

        private void UpdateNextUpdateText(RolesClientState roles, long nowUnixMs)
        {
            if (_nextUpdateText == null) return;

            long next = roles != null ? roles.GaugeNextUpdateUnixMs : 0;
            int sec;
            if (next <= 0) sec = -1;
            else
            {
                long remainMs = next - nowUnixMs;
                sec = remainMs > 0 ? (int)((remainMs + 999) / 1000) : 0;
            }
            if (sec == _lastNextUpdateSec) return;

            _lastNextUpdateSec = sec;
            _nextUpdateText.text = sec < 0 ? "" : Texts.Format(TextId.GaugeNextUpdateFormat, sec);
        }

        public void Destroy()
        {
            ClearMarkers();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _rootRect = null;
            _trackRect = null;
            _fillImage = null;
            _deliveryTrack = null;
            _deliveryTrackRect = null;
            _deliveryFillImage = null;
            _quotaLine = null;
            _checkmateLine = null;
            _deliveryIcon = null;
            _pctText = null;
            _nextUpdateText = null;
            _renderedSnapshot = null;
            _wasMeetingActive = false;
            _lastNextUpdateSec = -1;
            _reveal.Stop();
            _lastRevealedPermille = 0;
            _lastRevealedLoss = 0;
            _revealUnlockCount = 0;
            _revealBreakPlayed = false;
            _deliveryReveal.Stop();
            _lastRevealedDeliveryPermille = 0;
            _deliverySfxPlayed = false;
        }

        private void BeginReveal(MeetingGaugeSnapshot s, long nowUnixMs)
        {
            int toPermille = ClampPermille(s.RatioPermille);
            int toLoss = LossOf(s, toPermille);
            int fromPermille = _lastRevealedPermille;
            int fromLoss = _lastRevealedLoss;
            _lastRevealedPermille = toPermille;
            _lastRevealedLoss = toLoss;

            int toDelivery = s.DeliveryPermille();
            int fromDelivery = _lastRevealedDeliveryPermille;
            _lastRevealedDeliveryPermille = toDelivery < 0 ? 0 : toDelivery;

            bool lossAnim = GaugeReveal.ShouldAnimate(fromPermille, toPermille);
            bool deliveryAnim = toDelivery >= 0 && GaugeReveal.ShouldAnimate(fromDelivery, toDelivery);

            if (!lossAnim && !deliveryAnim)
            {
                _reveal.Stop();
                _deliveryReveal.Stop();
                Render(s, toPermille, toLoss, toDelivery);
                return;
            }

            if (lossAnim) _reveal.Start(fromPermille, toPermille, fromLoss, toLoss, nowUnixMs);
            else _reveal.Stop();
            _revealUnlockCount = GaugeMarkerLayout.UnlockedCount(s, lossAnim ? fromPermille : toPermille);
            _revealBreakPlayed = !lossAnim;

            if (deliveryAnim)
            {
                _deliveryReveal.Start(fromDelivery, toDelivery, 0, 0,
                    nowUnixMs + (lossAnim ? GaugeReveal.DurationMs : 0));
                _deliverySfxPlayed = false;
            }
            else
            {
                _deliveryReveal.Stop();
            }

            Render(s, lossAnim ? fromPermille : toPermille, lossAnim ? fromLoss : toLoss,
                deliveryAnim ? fromDelivery : toDelivery);
            WLog.Line("gauge_reveal_start", secret: false,
                ("from", fromPermille), ("to", toPermille),
                ("dFrom", fromDelivery), ("dTo", toDelivery));
        }

        private void TickReveal(MeetingGaugeSnapshot s, long nowUnixMs)
        {
            if (_reveal.Active && !_revealBreakPlayed && _reveal.GrowthStarted(nowUnixMs))
            {
                _revealBreakPlayed = true;
                OnRevealBreak?.Invoke();
            }
            if (_deliveryReveal.Active && !_deliverySfxPlayed && _deliveryReveal.GrowthStarted(nowUnixMs))
            {
                _deliverySfxPlayed = true;
                OnDeliveryReveal?.Invoke();
            }

            int permille = _reveal.Active ? _reveal.CurrentPermille(nowUnixMs) : ClampPermille(s.RatioPermille);
            int loss = _reveal.Active ? _reveal.CurrentLoss(nowUnixMs) : LossOf(s, permille);
            RenderFill(s, permille, loss);

            if (_deliveryReveal.Active)
            {
                RenderDelivery(s, _deliveryReveal.CurrentPermille(nowUnixMs));
            }

            int unlocked = GaugeMarkerLayout.UnlockedCount(s, permille);
            if (unlocked != _revealUnlockCount)
            {
                _revealUnlockCount = unlocked;
                RenderMarkers(s, permille);
            }

            if (_reveal.Done(nowUnixMs) && _deliveryReveal.Done(nowUnixMs))
            {
                _reveal.Stop();
                _deliveryReveal.Stop();
                Render(s);
            }
        }

        private void Render(MeetingGaugeSnapshot s)
        {
            int permille = ClampPermille(s.RatioPermille);
            Render(s, permille, LossOf(s, permille), s.DeliveryPermille());
        }

        private void Render(MeetingGaugeSnapshot s, int permille, int lossDollars, int deliveryPermille)
        {
            RenderFill(s, permille, lossDollars);
            RenderDelivery(s, deliveryPermille);
            RenderMarkers(s, permille);
        }

        private void RenderFill(MeetingGaugeSnapshot s, int permille, int lossDollars)
        {
            if (_fillImage != null) _fillImage.fillAmount = permille / 1000f;
            if (_pctText != null)
            {
                _pctText.text = Texts.Format(TextId.GaugeLossOverBaseFormat, lossDollars, s.BaseDollars);
            }
        }

        private void RenderDelivery(MeetingGaugeSnapshot s, int deliveryPermille)
        {
            if (_deliveryTrack == null) return;

            bool show = deliveryPermille >= 0;
            if (_deliveryTrack.activeSelf != show) _deliveryTrack.SetActive(show);
            if (_deliveryIcon != null && _deliveryIcon.activeSelf != show) _deliveryIcon.SetActive(show);
            if (_pctText != null)
            {
                _pctText.rectTransform.anchoredPosition =
                    show ? PctTextPos + new Vector2(0f, -TrackSize.y) : PctTextPos;
            }

            int quota = s.QuotaPermille();
            if (_quotaLine != null)
            {
                bool lineShow = show && quota >= 0;
                if (_quotaLine.gameObject.activeSelf != lineShow) _quotaLine.gameObject.SetActive(lineShow);
                if (lineShow)
                {
                    _quotaLine.rectTransform.anchoredPosition = new Vector2(
                        TrackSize.x / 2f - TrackSize.x * quota / 1000f,
                        TrackPos.y - TrackSize.y / 2f);
                }
            }

            int checkmate = s.CheckmateLinePermille();
            if (_checkmateLine != null)
            {
                bool cmShow = checkmate >= 0;
                if (_checkmateLine.gameObject.activeSelf != cmShow) _checkmateLine.gameObject.SetActive(cmShow);
                if (cmShow)
                {
                    _checkmateLine.rectTransform.anchoredPosition = new Vector2(
                        -TrackSize.x / 2f + TrackSize.x * checkmate / 1000f,
                        show ? TrackPos.y - TrackSize.y / 2f : TrackPos.y);
                    _checkmateLine.rectTransform.sizeDelta = new Vector2(
                        QuotaLineWidth, show ? TrackSize.y * 2f + 8f : TrackSize.y + 8f);
                }
            }
            if (!show) return;

            if (_deliveryFillImage != null) _deliveryFillImage.fillAmount = deliveryPermille / 1000f;
        }

        private static int ClampPermille(int permille)
        {
            if (permille < 0) return 0;
            if (permille > 1000) return 1000;
            return permille;
        }

        private static int LossOf(MeetingGaugeSnapshot s, int permille)
        {
            int loss = s.LostDollars >= 0
                ? s.LostDollars
                : (int)((long)s.BaseDollars * permille + 500) / 1000;
            if (loss > s.BaseDollars) loss = s.BaseDollars;
            return loss;
        }

        private void RenderMarkers(MeetingGaugeSnapshot s, int permille)
        {
            ClearMarkers();
            if (_trackRect == null) return;

            int minGapPct = Mathf.CeilToInt(PerkIconSize * _markerScale * 100f / TrackSize.x);
            List<GaugeMarker> markers = GaugeMarkerLayout.Build(s, permille, minGapPct);
            float trackWidth = TrackSize.x;

            foreach (GaugeMarker m in markers)
            {
                float x = -trackWidth / 2f + trackWidth * m.Pct / 100f;

                if (m.Kind == GaugeMarkerKind.Scale)
                {
                    float tickY = -(TrackSize.y / 2f + ScaleTickHeight / 2f)
                        - (s.DeliveryPermille() >= 0 ? TrackSize.y : 0f);
                    Image scaleTick = UiKit.CreateImage(_trackRect, "ScaleTick",
                        new Vector2(x, tickY),
                        new Vector2(ScaleTickWidth, ScaleTickHeight),
                        LockedTickColor);
                    _markerObjects.Add(scaleTick.gameObject);
                    continue;
                }

                Image tick = UiKit.CreateImage(_trackRect, "Tick",
                    new Vector2(x, TrackSize.y / 2f + TickHeight / 2f),
                    new Vector2(TickWidth, TickHeight),
                    m.Unlocked ? UnlockedTickColor : LockedTickColor);
                _markerObjects.Add(tick.gameObject);

                Sprite sprite = AssetCatalog.GetSprite(m.IconKey);
                if (sprite != null)
                {
                    float size = PerkIconSize * _markerScale;
                    float y = TrackSize.y / 2f + TickHeight + size / 2f + m.Tier * (size + TierGap);
                    Image iconImg = UiKit.CreateImage(_trackRect, "PerkIcon", new Vector2(x, y),
                        new Vector2(size, size), m.Unlocked ? Color.white : LockedIconTint);
                    iconImg.sprite = sprite;
                    iconImg.preserveAspect = true;
                    _markerObjects.Add(iconImg.gameObject);
                }
                else
                {
                    float bw = BalloonWidth * _markerScale;
                    float bh = BalloonHeight * _markerScale;
                    float y = TrackSize.y / 2f + TickHeight + bh / 2f + m.Tier * (bh + TierGap);
                    Image balloon = UiKit.CreateImage(_trackRect, "Balloon", new Vector2(x, y),
                        new Vector2(bw, bh), m.Unlocked ? UnlockedBalloonBg : LockedBalloonBg);
                    _markerObjects.Add(balloon.gameObject);
                    UiKit.CreateText(balloon.rectTransform, "Label", Vector2.zero,
                        new Vector2(bw - 8f, bh - 4f), m.Label, 14f * _markerScale,
                        m.Unlocked ? UnlockedTextColor : LockedTextColor, TextAlignmentOptions.Center);
                }
            }

            RenderRuleTexts(s);
        }

        private void RenderRuleTexts(MeetingGaugeSnapshot s)
        {
            bool left = s.DeliveryPermille() >= 0;
            float rowY = -30f - (left ? TrackSize.y : 0f);

            if (!left)
            {
                RenderBeaconRuleRight(s, rowY);
                return;
            }

            float x = -TrackSize.x / 2f + 80f;
            if (s.BeaconChargePct >= 1)
            {
                x = RenderRuleUnit(x, rowY, "perk_beacon",
                    "+1/" + s.BeaconChargePct + "%",
                    Texts.Format(TextId.GaugeBeaconRuleFormat, s.BeaconChargePct));
            }
            if (s.BombRefillPct >= 1)
            {
                RenderRuleUnit(x, rowY, "perk_bomb_plant",
                    "+1/" + s.BombRefillPct + "%",
                    Texts.Format(TextId.GaugeBombRuleFormat, s.BombRefillPct));
            }
        }

        private float RenderRuleUnit(float x, float rowY, string iconKey, string valueText, string fallbackText)
        {
            Color ruleColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
            Sprite icon = AssetCatalog.GetSprite(iconKey);
            if (icon != null)
            {
                float size = 20f * _markerScale;
                float textW = 90f * _markerScale;
                Image iconImg = UiKit.CreateImage(_trackRect, "RuleIcon",
                    new Vector2(x + size / 2f, rowY), new Vector2(size, size), Color.white);
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                _markerObjects.Add(iconImg.gameObject);

                var text = UiKit.CreateText(_trackRect, "RuleText",
                    new Vector2(x + size + 4f + textW / 2f, rowY),
                    new Vector2(textW, 24f * _markerScale), valueText, 16f * _markerScale,
                    ruleColor, TextAlignmentOptions.MidlineLeft);
                _markerObjects.Add(text.gameObject);
                return x + size + 4f + textW + 8f;
            }

            float fw = 200f * _markerScale;
            var fallback = UiKit.CreateText(_trackRect, "RuleText",
                new Vector2(x + fw / 2f, rowY), new Vector2(fw, 24f * _markerScale),
                fallbackText, 16f * _markerScale, ruleColor, TextAlignmentOptions.MidlineLeft);
            _markerObjects.Add(fallback.gameObject);
            return x + fw + 8f;
        }

        private void RenderBeaconRuleRight(MeetingGaugeSnapshot s, float rowY)
        {
            if (s.BeaconChargePct < 1) return;

            Color ruleColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
            Sprite icon = AssetCatalog.GetSprite("perk_beacon");
            if (icon != null)
            {
                float textW = 90f * _markerScale;
                float size = 20f * _markerScale;
                var text = UiKit.CreateText(_trackRect, "BeaconRule",
                    new Vector2(TrackSize.x / 2f - textW / 2f, rowY),
                    new Vector2(textW, 24f * _markerScale),
                    "+1/" + s.BeaconChargePct + "%", 16f * _markerScale, ruleColor,
                    TextAlignmentOptions.MidlineRight);
                _markerObjects.Add(text.gameObject);

                Image iconImg = UiKit.CreateImage(_trackRect, "BeaconRuleIcon",
                    new Vector2(TrackSize.x / 2f - textW - size / 2f - 4f, rowY),
                    new Vector2(size, size), Color.white);
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                _markerObjects.Add(iconImg.gameObject);
            }
            else
            {
                float textW = 200f * _markerScale;
                var text = UiKit.CreateText(_trackRect, "BeaconRule",
                    new Vector2(TrackSize.x / 2f - textW / 2f, rowY),
                    new Vector2(textW, 24f * _markerScale),
                    Texts.Format(TextId.GaugeBeaconRuleFormat, s.BeaconChargePct), 16f * _markerScale, ruleColor,
                    TextAlignmentOptions.MidlineRight);
                _markerObjects.Add(text.gameObject);
            }
        }

        private void ClearMarkers()
        {
            for (int i = 0; i < _markerObjects.Count; i++)
            {
                if (_markerObjects[i] != null) UnityEngine.Object.Destroy(_markerObjects[i]);
            }
            _markerObjects.Clear();
        }

    }
}
