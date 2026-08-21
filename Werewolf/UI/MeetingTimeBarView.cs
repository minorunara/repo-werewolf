using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class MeetingTimeBarView
    {
        public const float BarY = 300f;

        private const float TimeBarWidth = 800f;
        private const float TimeBarHeight = 28f;
        private const float TimeBarCenterX = -70f;
        private static readonly Color TimeBarEdgeColor = new Color(0.92f, 0.94f, 0.98f, 0.22f);
        private static readonly Color TimeBarTrackColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color TimeBarZoneMarkColor = new Color(0.84f, 0.22f, 0.18f, 0.18f);
        private static readonly Color TimeBarNormalColor = new Color(0.275f, 0.710f, 0.318f, 1f);
        private static readonly Color TimeBarRedColor = new Color(0.878f, 0.243f, 0.184f, 1f);
        private static readonly Color TimeBarLagColor = new Color(1f, 0.753f, 0.180f, 1f);
        private static readonly Color TimeBarZoneLineColor = new Color(0.92f, 0.94f, 0.98f, 0.25f);

        private readonly MeetingTimeBar _timeBar = new MeetingTimeBar();
        private GameObject _timeBarRoot;
        private Image _timeBarZoneMark;
        private Image _timeBarLag;
        private Image _timeBarGreen;
        private Image _timeBarRed;
        private RectTransform _timeBarZoneLine;
        private bool _timeBarZoneLayoutDone;

        public void Build(RectTransform parent)
        {
            RectTransform bar = UiKit.CreateRect(parent, "TimeBar", new Vector2(0f, BarY),
                new Vector2(parent.rect.width, TimeBarHeight));
            _timeBarRoot = bar.gameObject;
            float left = TimeBarCenterX - TimeBarWidth / 2f;
            UiKit.CreateImage(bar, "Edge", new Vector2(TimeBarCenterX, 0f),
                new Vector2(TimeBarWidth + 4f, TimeBarHeight + 4f), TimeBarEdgeColor);
            UiKit.CreateImage(bar, "Track", new Vector2(TimeBarCenterX, 0f),
                new Vector2(TimeBarWidth, TimeBarHeight), TimeBarTrackColor);
            _timeBarZoneMark = UiKit.CreateImage(bar, "ZoneMark", new Vector2(left, 0f),
                new Vector2(0f, TimeBarHeight), TimeBarZoneMarkColor);
            _timeBarLag = UiKit.CreateFilledImage(bar, "LagFill", new Vector2(TimeBarCenterX, 0f),
                new Vector2(TimeBarWidth, TimeBarHeight), TimeBarLagColor);
            _timeBarRed = UiKit.CreateFilledImage(bar, "RedFill", new Vector2(TimeBarCenterX, 0f),
                new Vector2(TimeBarWidth, TimeBarHeight), TimeBarRedColor);
            _timeBarGreen = UiKit.CreateFilledImage(bar, "GreenFill", new Vector2(TimeBarCenterX, 0f),
                new Vector2(TimeBarWidth, TimeBarHeight), TimeBarNormalColor);
            _timeBarZoneLine = UiKit.CreateImage(bar, "ZoneLine", new Vector2(left, 0f),
                new Vector2(2f, TimeBarHeight), TimeBarZoneLineColor).rectTransform;
            _timeBarRoot.SetActive(false);
        }

        public void Tick(MeetingClientState state, long nowUnixMs)
        {
            if (!_timeBar.Started) _timeBar.Begin(state.MeetingTotalMs);
            _timeBar.Tick(state.RemainingMs(nowUnixMs), nowUnixMs);
            EnsureZoneLayout();

            float zoneFrac = (float)_timeBar.RedZoneFraction;
            float fillFrac = (float)_timeBar.FillFraction;
            _timeBarLag.fillAmount = (float)_timeBar.LagFraction;
            _timeBarRed.fillAmount = Mathf.Min(fillFrac, zoneFrac);
            float greenSpan = 1f - zoneFrac;
            _timeBarGreen.fillAmount = greenSpan <= 0f ? 0f
                : Mathf.Clamp01((fillFrac - zoneFrac) / greenSpan);
        }

        public void SetVisible(bool visible)
        {
            if (_timeBarRoot == null) return;
            if (_timeBarRoot.activeSelf != visible) _timeBarRoot.SetActive(visible);
        }

        public void Reset()
        {
            _timeBar.Reset();
            _timeBarZoneLayoutDone = false;
            _timeBarRoot?.SetActive(false);
        }

        public void OnPanelDestroy()
        {
            _timeBarRoot = null;
            _timeBarZoneMark = null;
            _timeBarLag = null;
            _timeBarGreen = null;
            _timeBarRed = null;
            _timeBarZoneLine = null;
            _timeBar.Reset();
            _timeBarZoneLayoutDone = false;
        }

        private void EnsureZoneLayout()
        {
            if (_timeBarZoneLayoutDone || _timeBar.TotalMs <= 0) return;
            _timeBarZoneLayoutDone = true;
            float zoneW = TimeBarWidth * (float)_timeBar.RedZoneFraction;
            float left = TimeBarCenterX - TimeBarWidth / 2f;
            _timeBarZoneMark.rectTransform.sizeDelta = new Vector2(zoneW, TimeBarHeight);
            _timeBarZoneMark.rectTransform.anchoredPosition = new Vector2(left + zoneW / 2f, 0f);
            _timeBarZoneLine.anchoredPosition = new Vector2(left + zoneW, 0f);
            float greenW = TimeBarWidth - zoneW;
            _timeBarGreen.rectTransform.sizeDelta = new Vector2(greenW, TimeBarHeight);
            _timeBarGreen.rectTransform.anchoredPosition = new Vector2(left + zoneW + greenW / 2f, 0f);
        }
    }
}
