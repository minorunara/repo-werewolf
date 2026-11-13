#nullable enable
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class CheckmateRevealPanel : IClientPanel
    {
        public string LayerName => "CheckmateReveal";

        private static readonly Vector2 TrackSize =
            new Vector2(HudGeometry.MeetingGauge.TrackWidth, HudGeometry.MeetingGauge.TrackHeight);
        private const float GaugeScale = HudGeometry.CheckmateReveal.GaugeScale;
        private static readonly Vector2 GaugePos = new Vector2(0f, HudGeometry.CheckmateReveal.GaugeCenterY);
        private static readonly Color TrackBgColor = new Color(0.10f, 0.10f, 0.14f, 0.9f);
        private static readonly Color FillColor = new Color(0.85f, 0.65f, 0.15f, 0.95f);
        private static readonly Color DeliveryFillColor = new Color(0.30f, 0.80f, 0.95f, 0.95f);
        private static readonly Color QuotaLineColor = new Color(0.25f, 0.55f, 1f, 0.95f);
        private static readonly Color CheckmateLineColor = new Color(0.95f, 0.12f, 0.12f, 0.95f);
        private const float QuotaLineWidth = 4f;

        private static readonly Vector2 IconSize = new Vector2(300f, 300f);
        private static readonly Vector2 IconPos = new Vector2(0f, -60f);
        private static readonly Vector2 TitlePos = new Vector2(0f, HudGeometry.CheckmateReveal.TitleCenterY);

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color TitleColor = new Color(0.85f, 0.08f, 0.08f, 1f);

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, CanvasGroup group, Image fillImage,
                Image deliveryFill, GameObject deliveryTrack, Image quotaLine,
                Image checkmateLine, TextMeshProUGUI lossText, RectTransform stampRect,
                CanvasGroup stampGroup, Image iconImage,
                TextMeshProUGUI titleFront, TextMeshProUGUI titleBack)
            {
                Root = root;
                Group = group;
                FillImage = fillImage;
                DeliveryFill = deliveryFill;
                DeliveryTrack = deliveryTrack;
                QuotaLine = quotaLine;
                CheckmateLine = checkmateLine;
                LossText = lossText;
                StampRect = stampRect;
                StampGroup = stampGroup;
                IconImage = iconImage;
                TitleFront = titleFront;
                TitleBack = titleBack;
            }

            public GameObject Root { get; }
            public CanvasGroup Group { get; }
            public Image FillImage { get; }
            public Image DeliveryFill { get; }
            public GameObject DeliveryTrack { get; }
            public Image QuotaLine { get; }
            public Image CheckmateLine { get; }
            public TextMeshProUGUI LossText { get; }
            public RectTransform StampRect { get; }
            public CanvasGroup StampGroup { get; }
            public Image IconImage { get; }
            public TextMeshProUGUI TitleFront { get; }
            public TextMeshProUGUI TitleBack { get; }
            public int RunGeneration { get; set; }
        }

        private sealed class PreparedContent
        {
            public PreparedContent(MeetingGaugeSnapshot snapshot, int fromPermille,
                int fromLoss, int finalPermille, int finalLoss)
            {
                Snapshot = snapshot;
                FromPermille = fromPermille;
                FromLoss = fromLoss;
                FinalPermille = finalPermille;
                FinalLoss = finalLoss;
                Reveal = new GaugeReveal();
            }

            public MeetingGaugeSnapshot Snapshot { get; }
            public int FromPermille { get; }
            public int FromLoss { get; }
            public int FinalPermille { get; }
            public int FinalLoss { get; }
            public GaugeReveal Reveal { get; }
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(CheckmateRevealPanel));
        private PreparedContent? _prepared;

        public bool Exists => GetLiveViewOrNull() != null;

        public bool Visible
        {
            get
            {
                ViewRefs? view = GetLiveViewOrNull();
                return view != null && view.Root.activeSelf;
            }
        }

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                RevealPanelKit.FullscreenRoot shell = RevealPanelKit.BuildFullscreenRoot(
                    layerRoot, "WW_CheckmateReveal", BackdropColor);
                RectTransform rect = shell.Rect;
                root = rect.gameObject;

                RectTransform gaugeRect = UiKit.CreateRect(rect, "Gauge", GaugePos,
                    new Vector2(HudGeometry.MeetingGauge.Width, HudGeometry.MeetingGauge.Height));
                gaugeRect.localScale = Vector3.one * GaugeScale;
                Image track = UiKit.CreateImage(
                    gaugeRect, "Track", Vector2.zero, TrackSize, TrackBgColor);
                Image fillImage = UiKit.CreateFilledImage(
                    track.rectTransform, "Fill", Vector2.zero, TrackSize, FillColor);

                Image deliveryTrackImage = UiKit.CreateImage(gaugeRect, "DeliveryTrack",
                    new Vector2(0f, -TrackSize.y), TrackSize, TrackBgColor);
                GameObject deliveryTrack = deliveryTrackImage.gameObject;
                Image deliveryFill = UiKit.CreateFilledImage(
                    deliveryTrackImage.rectTransform, "DeliveryFill",
                    Vector2.zero, TrackSize, DeliveryFillColor);
                deliveryFill.fillOrigin = (int)Image.OriginHorizontal.Right;

                Image quotaLine = UiKit.CreateImage(gaugeRect, "QuotaLine",
                    new Vector2(0f, -TrackSize.y / 2f),
                    new Vector2(QuotaLineWidth, TrackSize.y * 2f + 8f), QuotaLineColor);
                Image checkmateLine = UiKit.CreateImage(gaugeRect, "CheckmateLine",
                    new Vector2(0f, -TrackSize.y / 2f),
                    new Vector2(QuotaLineWidth, TrackSize.y * 2f + 8f), CheckmateLineColor);

                Sprite? gaugeIcon = AssetCatalog.GetSprite("icon_gauge_valuable_loss");
                if (gaugeIcon != null)
                {
                    Image iconImg = UiKit.CreateImage(gaugeRect, "GaugeIcon",
                        new Vector2(-TrackSize.x / 2f, 0f), new Vector2(128f, 128f), Color.white);
                    iconImg.sprite = gaugeIcon;
                    iconImg.preserveAspect = true;
                }

                TextMeshProUGUI lossText = UiKit.CreateText(gaugeRect, "LossText",
                    new Vector2(0f, HudGeometry.CheckmateReveal.LossTextOffsetY),
                    new Vector2(TrackSize.x, HudGeometry.CheckmateReveal.LossTextHeight),
                    "", 44f, Color.white, TextAlignmentOptions.Center);

                RectTransform stampRect = UiKit.CreateRect(
                    rect, "Stamp", Vector2.zero, new Vector2(1400f, 800f));
                CanvasGroup stampGroup = stampRect.gameObject.AddComponent<CanvasGroup>();
                stampGroup.alpha = 0f;
                stampGroup.blocksRaycasts = false;
                stampGroup.interactable = false;
                Image iconImage = UiKit.CreateImage(stampRect, "Icon", IconPos, IconSize, Color.white);
                iconImage.enabled = false;
                TextMeshProUGUI titleFront = RevealPanelKit.CreateStampTitle(stampRect, TitlePos,
                    new Vector2(1400f, HudGeometry.CheckmateReveal.TitleHeight),
                    Texts.Get(TextId.CheckmateTitle), 96f, TitleColor, Color.black, 0.12f,
                    out TextMeshProUGUI titleBack);

                root.SetActive(false);
                _view = new ViewRefs(root, shell.Group, fillImage, deliveryFill,
                    deliveryTrack, quotaLine, checkmateLine, lossText,
                    stampRect, stampGroup, iconImage, titleFront, titleBack);
                _textRefresh.Subscribe(() => Exists, ApplyTexts);
            }
            catch
            {
                _view = null;
                if (root != null)
                {
                    root.SetActive(false);
                    UnityEngine.Object.Destroy(root);
                }
                throw;
            }
        }

        public void Show(MeetingGaugeSnapshot snapshot, int fromPermille, int fromLoss)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || snapshot == null) return;

            InvalidateRun(view);
            ClearPrepared();
            try
            {
                int finalPermille = Clamp(snapshot.RatioPermille);
                int finalLoss = snapshot.LostDollars >= 0
                    ? snapshot.LostDollars
                    : (int)((long)snapshot.BaseDollars * finalPermille + 500) / 1000;
                if (finalLoss > snapshot.BaseDollars) finalLoss = snapshot.BaseDollars;
                if (fromPermille > finalPermille) fromPermille = finalPermille;
                if (fromLoss > finalLoss) fromLoss = finalLoss;

                RenderLoss(view, snapshot, fromPermille, fromLoss);

                int delivery = snapshot.DeliveryPermille();
                bool showDelivery = delivery >= 0;
                if (view.DeliveryTrack.activeSelf != showDelivery)
                    view.DeliveryTrack.SetActive(showDelivery);
                if (showDelivery) view.DeliveryFill.fillAmount = delivery / 1000f;

                int quota = snapshot.QuotaPermille();
                bool showQuota = showDelivery && quota >= 0;
                view.QuotaLine.gameObject.SetActive(showQuota);
                if (showQuota)
                {
                    view.QuotaLine.rectTransform.anchoredPosition = new Vector2(
                        TrackSize.x / 2f - TrackSize.x * quota / 1000f, -TrackSize.y / 2f);
                }

                int checkmate = snapshot.CheckmateLinePermille();
                view.CheckmateLine.gameObject.SetActive(checkmate >= 0);
                if (checkmate >= 0)
                {
                    view.CheckmateLine.rectTransform.anchoredPosition = new Vector2(
                        -TrackSize.x / 2f + TrackSize.x * checkmate / 1000f, -TrackSize.y / 2f);
                }

                RevealPanelKit.SetIcon(view.IconImage,
                    AssetCatalog.GetSprite("img_taxman_foreclosure")
                    ?? AssetCatalog.GetSprite("img_taxman_death"));
                view.StampGroup.alpha = 0f;
                view.StampRect.localScale = Vector3.one;
                view.Root.SetActive(false);

                _prepared = new PreparedContent(
                    snapshot, fromPermille, fromLoss, finalPermille, finalLoss);
            }
            catch (Exception e)
            {
                _prepared = null;
                view.Root.SetActive(false);
                WLog.Line("checkmate_reveal_prepare_error", secret: false, ("err", e.Message));
            }
        }

        public IEnumerator Play(Action? onBreak, Action? onStamp, Action? onDone = null)

        {
            ViewRefs? view = GetLiveViewOrNull();
            PreparedContent? prepared = _prepared;
            if (view == null || prepared == null)
            {
                WLog.Line("checkmate_reveal_skip", secret: false,
                    ("reason", view == null ? "not_built" : "not_prepared"));
                yield break;
            }

            int generation = NextRun(view);
            view.Group.alpha = 0f;
            view.Root.SetActive(true);
            WLog.Line("checkmate_reveal_show", secret: false,
                ("from", prepared.FromPermille), ("to", prepared.FinalPermille));

            yield return RunGuarded(view, prepared, generation,
                UiTween.Fade(view.Group, 0f, 1f,
                    CheckmateCeremony.BackdropFadeSec, UiTween.EaseIn()));
            if (!IsCurrentRun(view, prepared, generation)) yield break;

            prepared.Reveal.Start(prepared.FromPermille, prepared.FinalPermille,
                prepared.FromLoss, prepared.FinalLoss, NowMs());
            bool breakPlayed = false;
            while (IsCurrentRun(view, prepared, generation) && !prepared.Reveal.Done(NowMs()))
            {
                long now = NowMs();
                if (!breakPlayed && prepared.Reveal.GrowthStarted(now))
                {
                    breakPlayed = true;
                    RevealPanelKit.InvokeCallback(onBreak, "checkmate_break_sfx_error");
                    if (!IsCurrentRun(view, prepared, generation)) yield break;
                }
                RenderLoss(view, prepared.Snapshot,
                    prepared.Reveal.CurrentPermille(now), prepared.Reveal.CurrentLoss(now));
                yield return null;
            }
            if (!IsCurrentRun(view, prepared, generation)) yield break;
            prepared.Reveal.Stop();
            RenderLoss(view, prepared.Snapshot, prepared.FinalPermille, prepared.FinalLoss);

            yield return RunGuarded(view, prepared, generation,
                UiTween.Hold(CheckmateCeremony.PostRevealPauseSec));
            if (!IsCurrentRun(view, prepared, generation)) yield break;

            yield return RunGuarded(view, prepared, generation,
                RevealPanelKit.StampEntrance(
                    view.StampRect, view.StampGroup, CheckmateCeremony.StampEntranceSec));
            if (!IsCurrentRun(view, prepared, generation)) yield break;

            RevealPanelKit.InvokeCallback(onStamp, "checkmate_stamp_sfx_error");
            if (!IsCurrentRun(view, prepared, generation)) yield break;

            yield return RunGuarded(
                view, prepared, generation, UiTween.Hold(CheckmateCeremony.StampHoldSec));
            if (!IsCurrentRun(view, prepared, generation)) yield break;
            RevealPanelKit.InvokeCallback(onDone, "checkmate_done_callback_error");
            WLog.Line("checkmate_reveal_hold_end", secret: false);
        }

        public void HideNow()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) HideCurrent(view);
            else ClearPrepared();
        }

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view == null) return;
            string title = Texts.Get(TextId.CheckmateTitle);
            view.TitleFront.text = title;
            view.TitleBack.text = title;
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            if (view != null) InvalidateRun(view);
            ClearPrepared();
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private IEnumerator RunGuarded(
            ViewRefs view, PreparedContent prepared, int generation, IEnumerator routine)
        {
            while (IsCurrentRun(view, prepared, generation))
            {
                bool moved;
                try { moved = routine.MoveNext(); }
                catch (Exception e)
                {
                    WLog.Line("checkmate_reveal_tween_error", secret: false, ("err", e.Message));
                    if (IsCurrentRun(view, prepared, generation)) HideCurrent(view);
                    yield break;
                }
                if (!moved || !IsCurrentRun(view, prepared, generation)) yield break;
                yield return routine.Current;
            }
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            InvalidateRun(view);
            ClearPrepared();
            return null;
        }

        private int NextRun(ViewRefs view)
        {
            InvalidateRun(view);
            return view.RunGeneration;
        }

        private static void InvalidateRun(ViewRefs view)
        {
            unchecked { view.RunGeneration++; }
        }

        private bool IsCurrentRun(ViewRefs view, PreparedContent prepared, int generation)
        {
            return ReferenceEquals(_view, view)
                && ReferenceEquals(_prepared, prepared)
                && view.Root != null
                && view.RunGeneration == generation;
        }

        private void HideCurrent(ViewRefs view)
        {
            InvalidateRun(view);
            ClearPrepared();
            if (view.Root == null) return;
            view.Group.alpha = 0f;
            view.StampGroup.alpha = 0f;
            view.StampRect.localScale = Vector3.one;
            view.Root.SetActive(false);
        }

        private void ClearPrepared()
        {
            PreparedContent? prepared = _prepared;
            _prepared = null;
            prepared?.Reveal.Stop();
        }

        private static void RenderLoss(
            ViewRefs view, MeetingGaugeSnapshot snapshot, int permille, int lossDollars)
        {
            view.FillImage.fillAmount = permille / 1000f;
            view.LossText.text = Texts.Format(
                TextId.GaugeLossOverBaseFormat, lossDollars, snapshot.BaseDollars);
        }

        private static int Clamp(int permille)
        {
            if (permille < 0) return 0;
            if (permille > 1000) return 1000;
            return permille;
        }

        private static long NowMs() => (long)(Time.unscaledTimeAsDouble * 1000.0);
    }
}
