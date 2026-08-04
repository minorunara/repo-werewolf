using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class ResultScreen : IClientPanel
    {
        public string LayerName => "Result";

        private const float ContentWidth = 1100f;
        private const float PanelWidth = 900f;
        private const float BannerHeight = 100f;
        private const float BannerY = 430f;
        private const float ViewportHeight = 760f;
        private const float ViewportCenterY = -20f;
        private const float FooterY = -470f;
        private const float RowHeight = 56f;
        private const float RowSpacing = 4f;
        private const float RowInnerMargin = 24f;
        private const float BannerFontSize = 52f;
        private const float RowFontSize = 26f;
        private const float FooterFontSize = 30f;
        private const float DigestHeaderHeight = 44f;
        private const float DigestLineHeight = 32f;
        private const float DigestFontSize = 24f;
        private const float SectionGap = 24f;
        private const float ContentTopPad = 8f;
        private const float ContentBottomPad = 16f;
        private const float ScrollStep = 90f;

        private const float AvatarScale = 1.8f;
        private const float AvatarHolderX = -PanelWidth * 0.5f + 120f;
        private const float AvatarHolderY = -29f;

        private GameObject _root;
        private TextMeshProUGUI _bannerText;
        private Image _bannerImage;
        private RectTransform _content;
        private TextMeshProUGUI _footerText;
        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<GameObject> _digestObjects = new List<GameObject>();
        private float _contentHeight;
        private float _scrollY;

        public bool Exists => _root != null;

        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_ResultScreen", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _root = go;

            var bg = UiKit.CreateImage(rect, "Bg", Vector2.zero,
                new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.82f));
            var bgRect = bg.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bg.raycastTarget = false;

            _bannerImage = UiKit.CreateImage(rect, "Banner",
                new Vector2(0f, BannerY), new Vector2(PanelWidth, BannerHeight),
                new Color(0.2f, 0.2f, 0.25f, 0.9f));
            _bannerImage.raycastTarget = false;
            _bannerText = UiKit.CreateText(_bannerImage.rectTransform, "BannerText",
                Vector2.zero, new Vector2(PanelWidth, BannerHeight),
                string.Empty, BannerFontSize,
                new Color(1f, 0.95f, 0.6f, 1f), TextAlignmentOptions.Center);
            var btRect = _bannerText.rectTransform;
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = Vector2.zero;
            btRect.offsetMax = Vector2.zero;

            RectTransform viewport = UiKit.CreateRect(rect, "Viewport",
                new Vector2(0f, ViewportCenterY), new Vector2(ContentWidth, ViewportHeight));
            viewport.gameObject.AddComponent<RectMask2D>();

            _content = UiKit.CreateRect(viewport, "Content",
                Vector2.zero, new Vector2(ContentWidth, ViewportHeight));
            _content.anchorMin = new Vector2(0.5f, 1f);
            _content.anchorMax = new Vector2(0.5f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            _footerText = UiKit.CreateText(rect, "Footer",
                new Vector2(0f, FooterY), new Vector2(ContentWidth, 48f),
                string.Empty, FooterFontSize,
                new Color(1f, 0.95f, 0.6f, 1f), TextAlignmentOptions.Center);

            _root.SetActive(false);
            WLog.Line("result_screen_built", secret: false);
        }

        public void Show(Team winningTeam, IReadOnlyList<ResultRow> rows,
            Func<int, PlayerAvatar> resolveAvatar = null,
            IReadOnlyList<string> digestLines = null,
            string footerText = null)
        {
            if (_root == null) return;

            _bannerText.text = FormatBanner(winningTeam);
            _footerText.text = footerText ?? string.Empty;

            int count = rows != null ? rows.Count : 0;
            EnsureSlots(count);
            float y = ContentTopPad;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < count)
                {
                    PlayerAvatar avatar = null;
                    if (resolveAvatar != null && rows[i] != null && rows[i].ActorNumber > 0)
                    {
                        try { avatar = resolveAvatar(rows[i].ActorNumber); }
                        catch { avatar = null; }
                    }
                    _slots[i].SetTop(y);
                    _slots[i].Show(rows[i], avatar);
                    y += RowHeight + RowSpacing;
                }
                else
                {
                    _slots[i].Hide();
                }
            }

            ClearDigestObjects();
            int digestCount = digestLines != null ? digestLines.Count : 0;
            if (digestCount > 0)
            {
                y += SectionGap;
                var header = UiKit.CreateText(_content, "DigestHeader",
                    Vector2.zero, new Vector2(ContentWidth - 40f, DigestHeaderHeight),
                    Texts.Get(TextId.ResultDigestHeader), DigestFontSize,
                    new Color(1f, 0.9f, 0.55f, 1f), TextAlignmentOptions.Center);
                PlaceTop(header.rectTransform, y, DigestHeaderHeight);
                _digestObjects.Add(header.gameObject);
                y += DigestHeaderHeight;

                for (int i = 0; i < digestCount; i++)
                {
                    var line = UiKit.CreateText(_content, "DigestLine" + i,
                        Vector2.zero, new Vector2(ContentWidth - 120f, DigestLineHeight),
                        digestLines[i] ?? string.Empty, DigestFontSize,
                        new Color(0.9f, 0.92f, 0.95f, 1f), TextAlignmentOptions.MidlineLeft);
                    PlaceTop(line.rectTransform, y, DigestLineHeight);
                    _digestObjects.Add(line.gameObject);
                    y += DigestLineHeight;
                }
            }

            _contentHeight = y + ContentBottomPad;
            _content.sizeDelta = new Vector2(ContentWidth, Mathf.Max(_contentHeight, ViewportHeight));
            _scrollY = 0f;
            _content.anchoredPosition = Vector2.zero;

            if (!_root.activeSelf) _root.SetActive(true);
            WLog.Line("result_screen_show", secret: false,
                ("team", winningTeam), ("rows", count), ("digest", digestCount));
        }

        public void Tick()
        {
            if (!Visible || _content == null) return;
            float wheel = Input.mouseScrollDelta.y;
            if (wheel == 0f) return;

            float maxScroll = Mathf.Max(0f, _contentHeight - ViewportHeight);
            _scrollY = Mathf.Clamp(_scrollY - wheel * ScrollStep, 0f, maxScroll);
            _content.anchoredPosition = new Vector2(0f, _scrollY);
        }

        public void SetFooter(string footerText)
        {
            if (_footerText != null) _footerText.text = footerText ?? string.Empty;
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf) _root.SetActive(false);
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _slots.Clear();
            _digestObjects.Clear();
            _bannerText = null;
            _bannerImage = null;
            _content = null;
            _footerText = null;
        }

        private void EnsureSlots(int count)
        {
            while (_slots.Count < count)
            {
                _slots.Add(BuildSlot(_content, _slots.Count));
            }
        }

        private void ClearDigestObjects()
        {
            foreach (GameObject go in _digestObjects)
            {
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            _digestObjects.Clear();
        }

        private static void PlaceTop(RectTransform rt, float yFromTop, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(yFromTop + height * 0.5f));
        }

        private static string FormatBanner(Team team)
        {
            switch (team)
            {
                case Team.Villagers: return Texts.Get(TextId.ResultBannerVillagerWin);
                case Team.Werewolves: return Texts.Get(TextId.ResultBannerWerewolfWin);
                default: return Texts.Get(TextId.ResultBannerDefault);
            }
        }

        private static string StatusLabel(ResultRowStatus status)
        {
            switch (status)
            {
                case ResultRowStatus.Alive: return Texts.Get(TextId.ResultStatusAlive);
                case ResultRowStatus.Executed: return Texts.Get(TextId.ResultStatusExecuted);
                case ResultRowStatus.Disconnected: return Texts.Get(TextId.ResultStatusDisconnected);
                case ResultRowStatus.Dead:
                default: return Texts.Get(TextId.ResultStatusDead);
            }
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

        private static Color RoleColor(Role role, bool alive)
        {
            Color c = RoleDistribution.TeamOf(role) == Team.Werewolves
                ? new Color(1f, 0.45f, 0.45f, 1f)
                : new Color(0.55f, 0.85f, 1f, 1f);
            if (!alive) { c.r *= 0.75f; c.g *= 0.75f; c.b *= 0.75f; c.a = 0.9f; }
            return c;
        }

        private static Slot BuildSlot(RectTransform parent, int index)
        {
            var bg = UiKit.CreateImage(parent, $"Row{index}",
                Vector2.zero, new Vector2(PanelWidth, RowHeight),
                new Color(1f, 1f, 1f, 0.06f));
            bg.raycastTarget = false;
            var bgRect = bg.rectTransform;
            bgRect.anchorMin = new Vector2(0.5f, 1f);
            bgRect.anchorMax = new Vector2(0.5f, 1f);

            var mark = UiKit.CreateText(bgRect, "Mark",
                new Vector2(-PanelWidth * 0.5f + RowInnerMargin + 12f, 0f),
                new Vector2(32f, RowHeight),
                string.Empty, RowFontSize,
                new Color(1f, 0.9f, 0.4f, 1f), TextAlignmentOptions.Center);

            RectTransform avatarHolder = UiKit.CreateRect(bgRect, "AvatarHolder",
                new Vector2(AvatarHolderX, AvatarHolderY), new Vector2(220f, RowHeight));
            avatarHolder.localScale = new Vector3(AvatarScale, AvatarScale, 1f);

            var name = UiKit.CreateText(bgRect, "Name",
                new Vector2(-PanelWidth * 0.25f, 0f),
                new Vector2(PanelWidth * 0.45f, RowHeight),
                string.Empty, RowFontSize,
                new Color(1f, 1f, 1f, 1f), TextAlignmentOptions.MidlineLeft);

            var role = UiKit.CreateText(bgRect, "Role",
                new Vector2(PanelWidth * 0.15f, 0f),
                new Vector2(PanelWidth * 0.2f, RowHeight),
                string.Empty, RowFontSize,
                new Color(1f, 0.95f, 0.75f, 1f), TextAlignmentOptions.Center);

            var status = UiKit.CreateText(bgRect, "Status",
                new Vector2(PanelWidth * 0.5f - RowInnerMargin - 40f, 0f),
                new Vector2(80f, RowHeight),
                string.Empty, RowFontSize,
                new Color(0.85f, 0.85f, 0.9f, 1f), TextAlignmentOptions.MidlineRight);

            bg.gameObject.SetActive(false);
            return new Slot(bg.gameObject, mark, avatarHolder, name, role, status);
        }

        private sealed class Slot
        {
            private readonly GameObject _go;
            private readonly TextMeshProUGUI _mark;
            private readonly RectTransform _avatarHolder;
            private readonly TextMeshProUGUI _name;
            private readonly TextMeshProUGUI _role;
            private readonly TextMeshProUGUI _status;
            private MenuPlayerListed _listed;

            internal Slot(
                GameObject go,
                TextMeshProUGUI mark,
                RectTransform avatarHolder,
                TextMeshProUGUI name,
                TextMeshProUGUI role,
                TextMeshProUGUI status)
            {
                _go = go;
                _mark = mark;
                _avatarHolder = avatarHolder;
                _name = name;
                _role = role;
                _status = status;
            }

            internal void SetTop(float yFromTop)
            {
                PlaceTop((RectTransform)_go.transform, yFromTop, RowHeight);
            }

            internal void Show(ResultRow row, PlayerAvatar avatar)
            {
                if (row == null) { Hide(); return; }
                _mark.text = row.IsWinningSide ? "★" : string.Empty;
                SetAvatar(avatar, row.Name);
                _name.gameObject.SetActive(_listed == null);
                _name.text = row.Name ?? string.Empty;
                _role.text = RoleLabel(row.Role);
                _role.color = RoleColor(row.Role, row.Alive);
                _status.text = StatusLabel(row.Status);
                Color nameColor = row.Alive
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(0.7f, 0.7f, 0.7f, 0.9f);
                _name.color = nameColor;
                if (!_go.activeSelf) _go.SetActive(true);
            }

            internal void Hide()
            {
                SetAvatar(null, null);
                if (_go.activeSelf) _go.SetActive(false);
            }

            private void SetAvatar(PlayerAvatar avatar, string playerName)
            {
                if (_listed != null)
                {
                    UnityEngine.Object.Destroy(_listed.gameObject);
                    _listed = null;
                }
                if (avatar != null)
                {
                    _listed = ListedAvatar.TryCreate(_avatarHolder, avatar, playerName);
                }
            }
        }
    }
}
