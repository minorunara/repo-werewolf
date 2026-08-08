using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class HudPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private const float LeftMargin = 40f;
        private const float TopMargin = 220f;
        private const float RootWidth = 400f;
        private const float RootHeight = 220f;
        private const float BadgeIconSize = 128f;
        private const float BadgeRowHeight = 132f;
        private const float BadgeGap = 10f;
        private const float BadgeFontSize = 48f;

        private const float IdRowHeight = 44f;
        private const float IdFontSize = 36f;

        private const float TimerFontSize = 66f;
        private const float TimerTopMargin = 16f;
        private static readonly Vector2 TimerSize = new Vector2(700f, 80f);

        private const float ScatterGuardFontSize = 38f;
        private const float ScatterGuardTopMargin = TimerTopMargin + 80f + 4f;
        private static readonly Vector2 ScatterGuardSize = new Vector2(1100f, 48f);
        private static readonly Color ScatterGuardColor = new Color(0.55f, 0.85f, 1f, 0.95f);

        private const float TestPlayFontSize = 34f;
        private const float TestPlayTopMargin = ScatterGuardTopMargin + 48f + 4f;
        private static readonly Vector2 TestPlaySize = new Vector2(1100f, 44f);
        private static readonly Color TestPlayColor = new Color(1f, 0.6f, 0.15f, 0.95f);

        private GameObject _root;

        private GameObject _badgeRow;
        private Image _badgeIcon;
        private TextMeshProUGUI _badgeText;

        private GameObject _idRow;
        private TextMeshProUGUI _idText;

        private GameObject _timerRow;
        private TextMeshProUGUI _timerText;

        private GameObject _scatterGuardRow;
        private GameObject _testPlayRow;

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

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_HudPanel", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(RootWidth, RootHeight);
            _root = go;
            ApplyPositionOffset(force: true);

            _badgeRow = BuildBadgeRow(rect);
            _idRow = BuildIdRow(rect);
            _timerRow = BuildTimerBanner(layerRoot, out _timerText);
            _scatterGuardRow = BuildStaticBanner(layerRoot, "WW_HudScatterGuard",
                TextId.HudScatterGuardBanner, ScatterGuardTopMargin, ScatterGuardSize,
                ScatterGuardFontSize, ScatterGuardColor);
            _testPlayRow = BuildStaticBanner(layerRoot, "WW_HudTestPlay",
                TextId.HudTestPlayBanner, TestPlayTopMargin, TestPlaySize,
                TestPlayFontSize, TestPlayColor);

            _badgeRow.SetActive(false);
            _idRow.SetActive(false);
            _timerRow.SetActive(false);
            _scatterGuardRow.SetActive(false);
            _testPlayRow.SetActive(false);
            WLog.Line("hud_panel_built", secret: false);
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
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            if (_timerRow != null)
            {
                UnityEngine.Object.Destroy(_timerRow);
            }
            if (_scatterGuardRow != null)
            {
                UnityEngine.Object.Destroy(_scatterGuardRow);
            }
            if (_testPlayRow != null)
            {
                UnityEngine.Object.Destroy(_testPlayRow);
            }
            _badgeRow = null; _badgeIcon = null; _badgeText = null;
            _idRow = null; _idText = null;
            _timerRow = null; _timerText = null;
            _scatterGuardRow = null;
            _testPlayRow = null;
            _lastBadgeRole = null;
            _lastTimerSec = -1;
            _lastSelfId = -1;
        }

        private void ApplyBadge(HudState s)
        {
            bool visible = s.ShowBadge && s.BadgeRole.HasValue;
            if (_badgeRow.activeSelf != visible) _badgeRow.SetActive(visible);
            if (!visible)
            {
                _lastBadgeRole = null;
                return;
            }
            if (_lastBadgeRole != s.BadgeRole)
            {
                _lastBadgeRole = s.BadgeRole;
                _badgeText.text = RoleLabel(s.BadgeRole.Value);
                Sprite icon = ResolveRoleIcon(s.BadgeRole.Value);
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
                _idText.text = Texts.Format(TextId.HudSelfIdFormat, s.SelfId);
            }
        }

        private void ApplyTimer(HudState s)
        {
            if (_timerRow.activeSelf != s.ShowTimer) _timerRow.SetActive(s.ShowTimer);
            if (!s.ShowTimer) { _lastTimerSec = -1; _lastTimerFrozen = false; return; }

            long remainingSec = (s.TimerRemainingMs + 999) / 1000;
            if (remainingSec < 0) remainingSec = 0;

            if (remainingSec != _lastTimerSec || s.TimerFrozen != _lastTimerFrozen)
            {
                _lastTimerSec = remainingSec;
                _lastTimerFrozen = s.TimerFrozen;
                _timerText.text = s.TimerFrozen
                    ? Texts.Format(TextId.HudTimerFrozenFormat, FormatTime(remainingSec))
                    : FormatTime(remainingSec);
            }

            if (s.TimerFrozen)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * TimerBlinkHz * 2f * Mathf.PI);
                _timerText.color = Color.Lerp(TimerFrozenColorB, TimerFrozenColorA, pulse);
            }
            else if (s.TimerAlert)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * TimerBlinkHz * 2f * Mathf.PI);
                _timerText.color = Color.Lerp(TimerAlertColorB, TimerAlertColorA, pulse);
            }
            else if (_timerText.color != TimerNormalColor)
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

        private GameObject BuildBadgeRow(RectTransform parent)
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
            _badgeIcon = iconGo.AddComponent<Image>();
            _badgeIcon.raycastTarget = false;
            _badgeIcon.enabled = false;

            _badgeText = UiKit.CreateText(rect, "Text",
                Vector2.zero, new Vector2(RootWidth - BadgeIconSize - BadgeGap, BadgeRowHeight),
                string.Empty, BadgeFontSize, new Color(1f, 1f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft);
            var tRect = _badgeText.rectTransform;
            tRect.anchorMin = new Vector2(0f, 0f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0f, 0.5f);
            tRect.offsetMin = new Vector2(BadgeIconSize + BadgeGap, 0f);
            tRect.offsetMax = new Vector2(0f, 0f);
            return go;
        }

        private GameObject BuildIdRow(RectTransform parent)
        {
            var go = new GameObject("IdRow", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -(BadgeRowHeight + 2f));
            rect.sizeDelta = new Vector2(RootWidth, IdRowHeight);

            _idText = UiKit.CreateText(rect, "Text", Vector2.zero,
                new Vector2(RootWidth, IdRowHeight), string.Empty, IdFontSize,
                new Color(1f, 1f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft);
            var tRect = _idText.rectTransform;
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
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -TimerTopMargin);
            rect.sizeDelta = TimerSize;

            text = UiKit.CreateText(rect, "Text", Vector2.zero, TimerSize,
                string.Empty, TimerFontSize, TimerNormalColor, TextAlignmentOptions.Center);
            var tRect = text.rectTransform;
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject BuildStaticBanner(Transform layerRoot, string name, TextId textId,
                                                    float topMargin, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topMargin);
            rect.sizeDelta = size;

            var text = UiKit.CreateText(rect, "Text", Vector2.zero, size,
                Texts.Get(textId), fontSize, color, TextAlignmentOptions.Center);
            var tRect = text.rectTransform;
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            return go;
        }

        private static string RoleLabel(Role role)
        {
            switch (role)
            {
                case Role.Werewolf: return Texts.Get(TextId.RoleNameWerewolf);
                case Role.BlackCat: return Texts.Get(TextId.RoleNameBlackCat);
                case Role.Villager: return Texts.Get(TextId.RoleNameVillager);
                case Role.Bomber: return Texts.Get(TextId.RoleNameBomber);
                case Role.Shaman: return Texts.Get(TextId.RoleNameShaman);
                default: return role.ToString();
            }
        }

        private static Sprite ResolveRoleIcon(Role role)
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
