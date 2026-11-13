#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class HudPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private const float LeftMargin = HudGeometry.RoleBadge.LeftMargin;
        private const float TopMargin = HudGeometry.RoleBadge.TopMargin;
        private const float RootWidth = 400f;
        private const float RootHeight = 220f;
        private const float BadgeIconSize = HudGeometry.RoleBadge.IconSize;
        private const float BadgeRowHeight = HudGeometry.RoleBadge.RowHeight;
        private const float BadgeGap = 10f;
        private const float BadgeFontSize = 48f;

        private const float IdRowHeight = 44f;
        private const float IdFontSize = 36f;

        private const float TimerFontSize = 66f;
        private const float TimerTopMargin = 16f;
        private static readonly Vector2 TimerSize = new Vector2(700f, 80f);

        private const float ScenarioBannerFontSize = 44f;
        private const float ScenarioBannerTopMargin = TimerTopMargin + 18f;
        private static readonly Vector2 ScenarioBannerSize = new Vector2(900f, 56f);
        private static readonly Color ScenarioBannerColor = new Color(1f, 0.95f, 0.7f, 0.9f);

        private const float ScatterGuardFontSize = 38f;
        private const float ScatterGuardTopMargin = TimerTopMargin + 80f + 4f;
        private static readonly Vector2 ScatterGuardSize = new Vector2(1100f, 48f);
        private static readonly Color ScatterGuardColor = new Color(0.55f, 0.85f, 1f, 0.95f);

        private const float TestPlayFontSize = 34f;
        private const float TestPlayTopMargin = ScatterGuardTopMargin + 48f + 4f;
        private static readonly Vector2 TestPlaySize = new Vector2(1100f, 44f);
        private static readonly Color TestPlayColor = new Color(1f, 0.6f, 0.15f, 0.95f);

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, GameObject badgeRow, Image badgeIcon,
                TextMeshProUGUI badgeText, GameObject idRow, TextMeshProUGUI idText,
                GameObject timerRow, TextMeshProUGUI timerText,
                GameObject scatterGuardRow, TextMeshProUGUI scatterGuardText,
                GameObject testPlayRow, TextMeshProUGUI testPlayText,
                GameObject scenarioBannerRow, TextMeshProUGUI scenarioBannerText)
            {
                Root = root; BadgeRow = badgeRow; BadgeIcon = badgeIcon; BadgeText = badgeText;
                IdRow = idRow; IdText = idText; TimerRow = timerRow; TimerText = timerText;
                ScatterGuardRow = scatterGuardRow; ScatterGuardText = scatterGuardText;
                TestPlayRow = testPlayRow; TestPlayText = testPlayText;
                ScenarioBannerRow = scenarioBannerRow; ScenarioBannerText = scenarioBannerText;
            }
            public GameObject Root { get; }
            public GameObject BadgeRow { get; }
            public Image BadgeIcon { get; }
            public TextMeshProUGUI BadgeText { get; }
            public GameObject IdRow { get; }
            public TextMeshProUGUI IdText { get; }
            public GameObject TimerRow { get; }
            public TextMeshProUGUI TimerText { get; }
            public GameObject ScatterGuardRow { get; }
            public TextMeshProUGUI ScatterGuardText { get; }
            public GameObject TestPlayRow { get; }
            public TextMeshProUGUI TestPlayText { get; }
            public GameObject ScenarioBannerRow { get; }
            public TextMeshProUGUI ScenarioBannerText { get; }
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(HudPanel));
        private GameObject? _root => _view?.Root;
        private GameObject? _badgeRow => _view?.BadgeRow;
        private Image? _badgeIcon => _view?.BadgeIcon;
        private TextMeshProUGUI? _badgeText => _view?.BadgeText;
        private GameObject? _idRow => _view?.IdRow;
        private TextMeshProUGUI? _idText => _view?.IdText;
        private GameObject? _timerRow => _view?.TimerRow;
        private TextMeshProUGUI? _timerText => _view?.TimerText;
        private GameObject? _scatterGuardRow => _view?.ScatterGuardRow;
        private GameObject? _testPlayRow => _view?.TestPlayRow;
        private GameObject? _scenarioBannerRow => _view?.ScenarioBannerRow;

        private Role? _lastBadgeRole;
        private long _lastTimerSec = -1;
        private bool _lastTimerFrozen;
        private int _lastSelfId = -1;

        private Vector2 _positionOffset;
        private Vector2 _appliedOffset;

        public Vector2 PositionOffset
        {
            get => _positionOffset;
            set => _positionOffset = value;
        }

        private static readonly Color TimerNormalColor = new Color(0.95f, 0.95f, 1f, 0.95f);
        private static readonly Color TimerFrozenColorA = new Color(1f, 0.95f, 0.35f, 1f);
        private static readonly Color TimerFrozenColorB = new Color(1f, 0.75f, 0.15f, 0.55f);
        private static readonly Color TimerAlertColorA = new Color(1f, 0.3f, 0.25f, 1f);
        private static readonly Color TimerAlertColorB = new Color(1f, 0.15f, 0.1f, 0.5f);
        private const float TimerBlinkHz = 1.5f;

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            GameObject? root = null;
            GameObject? timerRow = null;
            GameObject? scatterGuardRow = null;
            GameObject? testPlayRow = null;
            GameObject? scenarioBannerRow = null;
            try
            {
                RectTransform rect = UiKit.CreateAnchoredRect(layerRoot, "WW_HudPanel",
                    UiKit.TopLeft, new Vector2(0f, 1f), Vector2.zero, new Vector2(RootWidth, RootHeight));
                root = rect.gameObject;
                rect.anchoredPosition = new Vector2(
                    LeftMargin + _positionOffset.x, -(TopMargin + _positionOffset.y));
                _appliedOffset = _positionOffset;

                GameObject badgeRow = BuildBadgeRow(rect, out Image badgeIcon, out TextMeshProUGUI badgeText);
                GameObject idRow = BuildIdRow(rect, out TextMeshProUGUI idText);
                timerRow = BuildTimerBanner(layerRoot, out TextMeshProUGUI timerText);
                scatterGuardRow = BuildStaticBanner(layerRoot, "WW_HudScatterGuard",
                TextId.HudScatterGuardBanner, ScatterGuardTopMargin, ScatterGuardSize,
                ScatterGuardFontSize, ScatterGuardColor, out TextMeshProUGUI scatterGuardText);
                testPlayRow = BuildStaticBanner(layerRoot, "WW_HudTestPlay",
                TextId.HudTestPlayBanner, TestPlayTopMargin, TestPlaySize,
                TestPlayFontSize, TestPlayColor, out TextMeshProUGUI testPlayText);
                scenarioBannerRow = BuildStaticBanner(layerRoot, "WW_HudScenarioBanner",
                TextId.ScenarioHudBadge, ScenarioBannerTopMargin, ScenarioBannerSize,
                ScenarioBannerFontSize, ScenarioBannerColor, out TextMeshProUGUI scenarioBannerText);

                badgeRow.SetActive(false);
                idRow.SetActive(false);
                timerRow.SetActive(false);
                scatterGuardRow.SetActive(false);
                testPlayRow.SetActive(false);
                scenarioBannerRow.SetActive(false);
                _view = new ViewRefs(root, badgeRow, badgeIcon, badgeText, idRow, idText,
                    timerRow, timerText, scatterGuardRow, scatterGuardText,
                    testPlayRow, testPlayText, scenarioBannerRow, scenarioBannerText);
                _textRefresh.Subscribe(() => Exists, ApplyTexts);
            }
            catch
            {
                if (root != null) UnityEngine.Object.Destroy(root);
                if (timerRow != null) UnityEngine.Object.Destroy(timerRow);
                if (scatterGuardRow != null) UnityEngine.Object.Destroy(scatterGuardRow);
                if (testPlayRow != null) UnityEngine.Object.Destroy(testPlayRow);
                if (scenarioBannerRow != null) UnityEngine.Object.Destroy(scenarioBannerRow);
                throw;
            }
        }

        public HudState Tick(HudModel model, HudInput input)
        {
            if (_root == null || model == null) return HudState.Hidden;
            try
            {
                ApplyPositionOffset(force: false);
                HudState s = model.Compute(input);

                ApplyBadge(s);
                ApplySelfId(s);
                ApplyTimer(s);
                ApplyScatterGuard(s);
                ApplyTestPlay(s);
                ApplyScenarioBanner(s);
                return s;
            }
            catch (Exception e)
            {
                WLog.Line("hud_panel_tick_error", secret: false, ("err", e.Message));
                return HudState.Hidden;
            }
        }

        private void ApplyPositionOffset(bool force)
        {
            if (_root == null) return;
            if (!force && _appliedOffset == _positionOffset) return;
            var rect = (RectTransform)_root.transform;
            rect.anchoredPosition = new Vector2(
                LeftMargin + _positionOffset.x,
                -(TopMargin + _positionOffset.y));
            _appliedOffset = _positionOffset;
        }

        public void Hide()
        {
            if (_root == null) return;
            if (_badgeRow != null && _badgeRow.activeSelf) _badgeRow.SetActive(false);
            if (_idRow != null && _idRow.activeSelf) _idRow.SetActive(false);
            if (_timerRow != null && _timerRow.activeSelf) _timerRow.SetActive(false);
            if (_scatterGuardRow != null && _scatterGuardRow.activeSelf) _scatterGuardRow.SetActive(false);
            if (_testPlayRow != null && _testPlayRow.activeSelf) _testPlayRow.SetActive(false);
            if (_scenarioBannerRow != null && _scenarioBannerRow.activeSelf) _scenarioBannerRow.SetActive(false);
        }

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view == null) return;
            view.ScatterGuardText.text = Texts.Get(TextId.HudScatterGuardBanner);
            view.TestPlayText.text = Texts.Get(TextId.HudTestPlayBanner);
            view.ScenarioBannerText.text = Texts.Get(TextId.ScenarioHudBadge);
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
            if (view != null && view.TimerRow != null)
            {
                UnityEngine.Object.Destroy(view.TimerRow);
            }
            if (view != null && view.ScatterGuardRow != null)
            {
                UnityEngine.Object.Destroy(view.ScatterGuardRow);
            }
            if (view != null && view.TestPlayRow != null)
            {
                UnityEngine.Object.Destroy(view.TestPlayRow);
            }
            if (view != null && view.ScenarioBannerRow != null)
            {
                UnityEngine.Object.Destroy(view.ScenarioBannerRow);
            }
            _lastBadgeRole = null;
            _lastTimerSec = -1;
            _lastSelfId = -1;
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null && view.TimerRow != null && view.ScatterGuardRow != null
                && view.TestPlayRow != null && view.ScenarioBannerRow != null) return view;

            _view = null;
            if (view.Root != null) UnityEngine.Object.Destroy(view.Root);
            if (view.TimerRow != null) UnityEngine.Object.Destroy(view.TimerRow);
            if (view.ScatterGuardRow != null) UnityEngine.Object.Destroy(view.ScatterGuardRow);
            if (view.TestPlayRow != null) UnityEngine.Object.Destroy(view.TestPlayRow);
            if (view.ScenarioBannerRow != null) UnityEngine.Object.Destroy(view.ScenarioBannerRow);
            _lastBadgeRole = null;
            _lastTimerSec = -1;
            _lastSelfId = -1;
            return null;
        }

        private void ApplyBadge(HudState s)
        {
            Role? badgeRole = s.BadgeRole;
            bool visible = s.ShowBadge && badgeRole.HasValue;
            if (_badgeRow!.activeSelf != visible) _badgeRow.SetActive(visible);
            if (!visible)
            {
                _lastBadgeRole = null;
                return;
            }
            if (_lastBadgeRole != s.BadgeRole)
            {
                Role role = badgeRole.GetValueOrDefault();
                _lastBadgeRole = s.BadgeRole;
                _badgeText!.text = RoleLabel(role);
                Sprite? icon = ResolveRoleIcon(role);
                if (_badgeIcon != null)
                {
                    if (icon != null)
                    {
                        _badgeIcon.sprite = icon;
                        _badgeIcon.enabled = true;
                    }
                    else
                    {
                        _badgeIcon.enabled = false;
                    }
                }
            }
        }

        private void ApplySelfId(HudState s)
        {
            if (_idRow == null) return;
            if (_idRow.activeSelf != s.ShowSelfId) _idRow.SetActive(s.ShowSelfId);
            if (!s.ShowSelfId)
            {
                _lastSelfId = -1;
                return;
            }
            if (_lastSelfId != s.SelfId)
            {
                _lastSelfId = s.SelfId;
                _idText!.text = Texts.Format(TextId.HudSelfIdFormat, s.SelfId);
            }
        }

        private void ApplyTimer(HudState s)
        {
            if (_timerRow!.activeSelf != s.ShowTimer) _timerRow.SetActive(s.ShowTimer);
            if (!s.ShowTimer) { _lastTimerSec = -1; _lastTimerFrozen = false; return; }

            long remainingSec = (s.TimerRemainingMs + 999) / 1000;
            if (remainingSec < 0) remainingSec = 0;

            if (remainingSec != _lastTimerSec || s.TimerFrozen != _lastTimerFrozen)
            {
                _lastTimerSec = remainingSec;
                _lastTimerFrozen = s.TimerFrozen;
                _timerText!.text = s.TimerFrozen
                    ? Texts.Format(TextId.HudTimerFrozenFormat, FormatTime(remainingSec))
                    : FormatTime(remainingSec);
            }

            if (s.TimerFrozen)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * TimerBlinkHz * 2f * Mathf.PI);
                _timerText!.color = Color.Lerp(TimerFrozenColorB, TimerFrozenColorA, pulse);
            }
            else if (s.TimerAlert)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * TimerBlinkHz * 2f * Mathf.PI);
                _timerText!.color = Color.Lerp(TimerAlertColorB, TimerAlertColorA, pulse);
            }
            else if (_timerText!.color != TimerNormalColor)
            {
                _timerText.color = TimerNormalColor;
            }
        }

        private void ApplyScatterGuard(HudState s)
        {
            if (_scatterGuardRow != null && _scatterGuardRow.activeSelf != s.ShowScatterGuard)
                _scatterGuardRow.SetActive(s.ShowScatterGuard);
        }

        private void ApplyTestPlay(HudState s)
        {
            if (_testPlayRow != null && _testPlayRow.activeSelf != s.ShowTestPlay)
                _testPlayRow.SetActive(s.ShowTestPlay);
        }

        private void ApplyScenarioBanner(HudState s)
        {
            if (_scenarioBannerRow != null && _scenarioBannerRow.activeSelf != s.ShowScenarioBanner)
                _scenarioBannerRow.SetActive(s.ShowScenarioBanner);
        }

        private static GameObject BuildBadgeRow(RectTransform parent, out Image badgeIcon,
            out TextMeshProUGUI badgeText)
        {
            var go = new GameObject("BadgeRow", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(RootWidth, BadgeRowHeight);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 0f);
            iconRect.sizeDelta = new Vector2(BadgeIconSize, BadgeIconSize);
            badgeIcon = iconGo.AddComponent<Image>();
            badgeIcon.raycastTarget = false;
            badgeIcon.enabled = false;

            badgeText = UiKit.CreateText(rect, "Text",
                Vector2.zero, new Vector2(RootWidth - BadgeIconSize - BadgeGap, BadgeRowHeight),
                string.Empty, BadgeFontSize, new Color(1f, 1f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft);
            var tRect = badgeText.rectTransform;
            tRect.anchorMin = new Vector2(0f, 0f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0f, 0.5f);
            tRect.offsetMin = new Vector2(BadgeIconSize + BadgeGap, 0f);
            tRect.offsetMax = new Vector2(0f, 0f);
            return go;
        }

        private static GameObject BuildIdRow(RectTransform parent, out TextMeshProUGUI idText)
        {
            var go = new GameObject("IdRow", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -(BadgeRowHeight + 2f));
            rect.sizeDelta = new Vector2(RootWidth, IdRowHeight);

            idText = UiKit.CreateText(rect, "Text", Vector2.zero,
                new Vector2(RootWidth, IdRowHeight), string.Empty, IdFontSize,
                new Color(1f, 1f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft);
            var tRect = idText.rectTransform;
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.pivot = new Vector2(0f, 0.5f);
            tRect.offsetMin = new Vector2(BadgeIconSize + BadgeGap, 0f);
            tRect.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject BuildTimerBanner(Transform layerRoot, out TextMeshProUGUI text)
        {
            var go = new GameObject("WW_HudTimer", typeof(RectTransform));
            try
            {
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -TimerTopMargin);
            rect.sizeDelta = TimerSize;

            text = UiKit.CreateText(rect, "Text", Vector2.zero, TimerSize,
                string.Empty, TimerFontSize, TimerNormalColor, TextAlignmentOptions.Center);
            var tRect = text.rectTransform;
            UiKit.Stretch(tRect);
            return go;
            }
            catch
            {
                UnityEngine.Object.Destroy(go);
                throw;
            }
        }

        private static GameObject BuildStaticBanner(Transform layerRoot, string name, TextId textId,
                                                    float topMargin, Vector2 size, float fontSize, Color color,
                                                    out TextMeshProUGUI text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            try
            {
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topMargin);
            rect.sizeDelta = size;

            text = UiKit.CreateText(rect, "Text", Vector2.zero, size,
                Texts.Get(textId), fontSize, color, TextAlignmentOptions.Center);
            UiKit.Stretch(text.rectTransform);
            return go;
            }
            catch
            {
                UnityEngine.Object.Destroy(go);
                throw;
            }
        }

        private static string RoleLabel(Role role) => RoleText.Label(role);

        private static Sprite? ResolveRoleIcon(Role role)
        {
            switch (role)
            {
                case Role.Werewolf: return AssetCatalog.GetSprite("role_werewolf");
                case Role.BlackCat: return AssetCatalog.GetSprite("role_blackcat");
                case Role.Villager: return AssetCatalog.GetSprite("role_villager");
                case Role.Bomber: return AssetCatalog.GetSprite("role_bomber");
                case Role.Shaman: return AssetCatalog.GetSprite("role_shaman");
                default: return null;
            }
        }

        private static string FormatTime(long totalSec)
        {
            long m = totalSec / 60;
            long s = totalSec % 60;
            return Texts.Format(TextId.HudTimeRemainingFormat, m, s.ToString("00"));
        }
    }
}
