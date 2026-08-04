using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class CheckmateRevealPanel : IClientPanel
    {
        public string LayerName => "CheckmateReveal";

        private static readonly Vector2 TrackSize = new Vector2(1500f, 20f);
        private const float GaugeScale = 0.8f;
        private static readonly Vector2 GaugePos = new Vector2(0f, 220f);
        private static readonly Color TrackBgColor = new Color(0.10f, 0.10f, 0.14f, 0.9f);
        private static readonly Color FillColor = new Color(0.85f, 0.65f, 0.15f, 0.95f);
        private static readonly Color DeliveryFillColor = new Color(0.30f, 0.80f, 0.95f, 0.95f);
        private static readonly Color QuotaLineColor = new Color(0.25f, 0.55f, 1f, 0.95f);
        private static readonly Color CheckmateLineColor = new Color(0.95f, 0.12f, 0.12f, 0.95f);
        private const float QuotaLineWidth = 4f;

        private const float EntranceStartScale = 2.6f;
        private static readonly Vector2 IconSize = new Vector2(300f, 300f);
        private static readonly Vector2 IconPos = new Vector2(0f, -60f);
        private static readonly Vector2 TitlePos = new Vector2(0f, -290f);

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color TitleColor = new Color(0.85f, 0.08f, 0.08f, 1f);

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _gaugeRect;
        private Image _fillImage;
        private Image _deliveryFill;
        private GameObject _deliveryTrack;
        private Image _quotaLine;
        private Image _checkmateLine;
        private TextMeshProUGUI _lossText;
        private RectTransform _stampRect;
        private CanvasGroup _stampGroup;
        private Image _iconImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _titleTextBack;

        private readonly GaugeReveal _reveal = new GaugeReveal();
        private MeetingGaugeSnapshot _snapshot;
        private int _finalPermille;
        private int _finalLoss;

        public bool Exists => _root != null;

        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_CheckmateReveal", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _root = go;

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            Image backdrop = UiKit.CreateImage(rect, "Backdrop", Vector2.zero,
                new Vector2(1920f, 1080f), BackdropColor);
            var bgRect = backdrop.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            _gaugeRect = UiKit.CreateRect(rect, "Gauge", GaugePos, new Vector2(1560f, 104f));
            _gaugeRect.localScale = Vector3.one * GaugeScale;

            Image track = UiKit.CreateImage(_gaugeRect, "Track", Vector2.zero, TrackSize, TrackBgColor);
            _fillImage = UiKit.CreateFilledImage(track.rectTransform, "Fill", Vector2.zero, TrackSize, FillColor);

            Image deliveryTrack = UiKit.CreateImage(_gaugeRect, "DeliveryTrack",
                new Vector2(0f, -TrackSize.y), TrackSize, TrackBgColor);
            _deliveryTrack = deliveryTrack.gameObject;
            _deliveryFill = UiKit.CreateFilledImage(deliveryTrack.rectTransform, "DeliveryFill",
                Vector2.zero, TrackSize, DeliveryFillColor);
            _deliveryFill.fillOrigin = (int)Image.OriginHorizontal.Right;

            _quotaLine = UiKit.CreateImage(_gaugeRect, "QuotaLine",
                new Vector2(0f, -TrackSize.y / 2f),
                new Vector2(QuotaLineWidth, TrackSize.y * 2f + 8f), QuotaLineColor);
            _checkmateLine = UiKit.CreateImage(_gaugeRect, "CheckmateLine",
                new Vector2(0f, -TrackSize.y / 2f),
                new Vector2(QuotaLineWidth, TrackSize.y * 2f + 8f), CheckmateLineColor);

            Sprite gaugeIcon = AssetCatalog.GetSprite("icon_gauge_valuable_loss");
            if (gaugeIcon != null)
            {
                Image iconImg = UiKit.CreateImage(_gaugeRect, "GaugeIcon",
                    new Vector2(-TrackSize.x / 2f, 0f), new Vector2(128f, 128f), Color.white);
                iconImg.sprite = gaugeIcon;
                iconImg.preserveAspect = true;
            }

            _lossText = UiKit.CreateText(_gaugeRect, "LossText", new Vector2(0f, -TrackSize.y - 36f),
                new Vector2(1500f, 44f), "", 44f, Color.white, TextAlignmentOptions.Center);

            _stampRect = UiKit.CreateRect(rect, "Stamp", Vector2.zero, new Vector2(1400f, 800f));
            _stampGroup = _stampRect.gameObject.AddComponent<CanvasGroup>();
            _stampGroup.alpha = 0f;
            _stampGroup.blocksRaycasts = false;
            _stampGroup.interactable = false;

            _iconImage = UiKit.CreateImage(_stampRect, "Icon", IconPos, IconSize, Color.white);
            _iconImage.enabled = false;

            string title = Texts.Get(TextId.CheckmateTitle);
            _titleTextBack = UiKit.CreateText(_stampRect, "TitleBack", TitlePos,
                new Vector2(1400f, 130f), title, 96f, Color.black, TextAlignmentOptions.Center);
            _titleTextBack.outlineColor = Color.white;
            _titleTextBack.outlineWidth = 0.30f;
            _titleTextBack.fontStyle = FontStyles.Bold;
            _titleText = UiKit.CreateText(_stampRect, "Title", TitlePos,
                new Vector2(1400f, 130f), title, 96f, TitleColor, TextAlignmentOptions.Center);
            _titleText.outlineColor = Color.black;
            _titleText.outlineWidth = 0.12f;
            _titleText.fontStyle = FontStyles.Bold;

            _root.SetActive(false);
            WLog.Line("checkmate_reveal_built", secret: false);
        }

        public void Show(MeetingGaugeSnapshot snapshot, int fromPermille, int fromLoss)
        {
            if (_root == null || snapshot == null) return;
            _snapshot = snapshot;

            _finalPermille = Clamp(snapshot.RatioPermille);
            _finalLoss = snapshot.LostDollars >= 0
                ? snapshot.LostDollars
                : (int)((long)snapshot.BaseDollars * _finalPermille + 500) / 1000;
            if (_finalLoss > snapshot.BaseDollars) _finalLoss = snapshot.BaseDollars;
            if (fromPermille > _finalPermille) fromPermille = _finalPermille;
            if (fromLoss > _finalLoss) fromLoss = _finalLoss;

            RenderLoss(fromPermille, fromLoss);

            int delivery = snapshot.DeliveryPermille();
            bool showDelivery = delivery >= 0;
            if (_deliveryTrack.activeSelf != showDelivery) _deliveryTrack.SetActive(showDelivery);
            if (showDelivery && _deliveryFill != null) _deliveryFill.fillAmount = delivery / 1000f;

            int quota = snapshot.QuotaPermille();
            bool showQuota = showDelivery && quota >= 0;
            if (_quotaLine != null)
            {
                _quotaLine.gameObject.SetActive(showQuota);
                if (showQuota)
                {
                    _quotaLine.rectTransform.anchoredPosition = new Vector2(
                        TrackSize.x / 2f - TrackSize.x * quota / 1000f, -TrackSize.y / 2f);
                }
            }

            int checkmate = snapshot.CheckmateLinePermille();
            if (_checkmateLine != null)
            {
                _checkmateLine.gameObject.SetActive(checkmate >= 0);
                if (checkmate >= 0)
                {
                    _checkmateLine.rectTransform.anchoredPosition = new Vector2(
                        -TrackSize.x / 2f + TrackSize.x * checkmate / 1000f, -TrackSize.y / 2f);
                }
            }

            Sprite icon = AssetCatalog.GetSprite("img_taxman_foreclosure")
                ?? AssetCatalog.GetSprite("img_taxman_death");
            if (icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.preserveAspect = true;
                _iconImage.enabled = true;
            }
            else
            {
                _iconImage.sprite = null;
                _iconImage.enabled = false;
            }

            _stampGroup.alpha = 0f;
            _stampRect.localScale = Vector3.one;
            _reveal.Stop();

            _pendingFromPermille = fromPermille;
            _pendingFromLoss = fromLoss;
        }

        private int _pendingFromPermille;
        private int _pendingFromLoss;

        public IEnumerator Play(Action onBreak, Action onStamp)
        {
            if (_root == null || _snapshot == null)
            {
                WLog.Line("checkmate_reveal_skip", secret: false, ("reason", "not_built"));
                yield break;
            }

            _group.alpha = 0f;
            _root.SetActive(true);
            WLog.Line("checkmate_reveal_show", secret: false,
                ("from", _pendingFromPermille), ("to", _finalPermille));

            yield return UiTween.Fade(_group, 0f, 1f, CheckmateCeremony.BackdropFadeSec, UiTween.EaseIn());

            _reveal.Start(_pendingFromPermille, _finalPermille, _pendingFromLoss, _finalLoss, NowMs());
            bool breakPlayed = false;
            while (!_reveal.Done(NowMs()))
            {
                long now = NowMs();
                if (!breakPlayed && _reveal.GrowthStarted(now))
                {
                    breakPlayed = true;
                    try { onBreak?.Invoke(); }
                    catch (Exception e)
                    {
                        WLog.Line("checkmate_break_sfx_error", secret: false, ("err", e.Message));
                    }
                }
                RenderLoss(_reveal.CurrentPermille(now), _reveal.CurrentLoss(now));
                yield return null;
            }
            _reveal.Stop();
            RenderLoss(_finalPermille, _finalLoss);

            yield return UiTween.Hold(CheckmateCeremony.PostRevealPauseSec);

            _stampRect.localScale = Vector3.one * EntranceStartScale;
            yield return UiTween.Parallel(
                UiTween.Scale(_stampRect, EntranceStartScale, 1f, CheckmateCeremony.StampEntranceSec, UiTween.EaseIn()),
                UiTween.Fade(_stampGroup, 0f, 1f, CheckmateCeremony.StampEntranceSec, UiTween.EaseIn()));

            try { onStamp?.Invoke(); }
            catch (Exception e)
            {
                WLog.Line("checkmate_stamp_sfx_error", secret: false, ("err", e.Message));
            }

            yield return UiTween.Hold(CheckmateCeremony.StampHoldSec);
            WLog.Line("checkmate_reveal_hold_end", secret: false);
        }

        public void HideNow()
        {
            _reveal.Stop();
            _snapshot = null;
            if (_root == null) return;
            _group.alpha = 0f;
            _stampGroup.alpha = 0f;
            _stampRect.localScale = Vector3.one;
            _root.SetActive(false);
        }

        public void Destroy()
        {
            _reveal.Stop();
            _snapshot = null;
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _group = null;
            _gaugeRect = null;
            _fillImage = null;
            _deliveryFill = null;
            _deliveryTrack = null;
            _quotaLine = null;
            _checkmateLine = null;
            _lossText = null;
            _stampRect = null;
            _stampGroup = null;
            _iconImage = null;
            _titleText = null;
            _titleTextBack = null;
        }

        private void RenderLoss(int permille, int lossDollars)
        {
            if (_fillImage != null) _fillImage.fillAmount = permille / 1000f;
            if (_lossText != null && _snapshot != null)
            {
                _lossText.text = Texts.Format(TextId.GaugeLossOverBaseFormat, lossDollars, _snapshot.BaseDollars);
            }
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
