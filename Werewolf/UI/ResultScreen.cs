using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class ResultScreen : IClientPanel
    {
        public const string Layer = "Result";

        public string LayerName => Layer;

        private const float ContentWidth = 1100f;
        private const float PanelWidth = 900f;
        private const float BannerHeight = 130f;
        private const float BannerY = 445f;
        private const float ViewportHeight = 760f;
        private const float ViewportCenterY = -20f;
        private const float FooterY = -518f;
        private const float ReturnButtonY = -468f;
        private const float ReturnButtonWidth = 300f;
        private const float ReturnButtonHeight = 48f;
        private const float ReturnButtonFontSize = 24f;
        private const float RowHeight = 56f;
        private const float RowSpacing = 4f;
        private const float RowInnerMargin = 24f;
        private const float BannerFontSize = 60f;
        private const float BannerTitleHeight = 76f;
        private const float BannerTitleWithEpigraphY = 20f;
        private const float EpigraphY = -38f;
        private const float EpigraphHeight = 40f;
        private const float EpigraphFontSize = 28f;
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
        private const float StatusIconSize = 50f;
        private const float IdLabelX = -PanelWidth * 0.5f + 62f;
        private const float IdFontSize = 22f;
        private static readonly Color IdColor = new Color(1f, 0.9f, 0.6f, 1f);

        private static readonly Color WerewolfTeamColor = new Color(1f, 0.45f, 0.45f, 1f);
        private static readonly Color VillagerTeamColor = new Color(0.55f, 0.85f, 1f, 1f);
        private static readonly Color BannerDefaultColor = new Color(1f, 0.95f, 0.6f, 1f);
        private static readonly Color EpigraphColor = new Color(0.88f, 0.86f, 0.78f, 1f);

        private static readonly Color VoidMatchColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        private static readonly Color PeacefulCollapsedColor = new Color(0.75f, 0.32f, 0.32f, 1f);

        private static readonly ButtonPalette ReturnButtonPalette = new ButtonPalette(
            new Color(0.26f, 0.26f, 0.3f, 0.9f), new Color(0.42f, 0.42f, 0.48f, 1f),
            new Color(0.15f, 0.15f, 0.17f, 0.6f), new Color(0.55f, 0.55f, 0.55f));

        private GameObject _root;
        private TextMeshProUGUI _bannerText;
        private TextMeshProUGUI _epigraphText;
        private Image _bannerImage;
        private RectTransform _content;
        private TextMeshProUGUI _footerText;
        private Image _returnButtonBg;
        private TextMeshProUGUI _returnButtonLabel;
        private CanvasGroup _returnButtonGroup;
        private string _returnButtonLabelText;
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

            RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_ResultScreen");
            _root = rect.gameObject;

            UiKit.CreateBackdrop(rect, "Bg", new Color(0f, 0f, 0f, 0.82f));

            _bannerImage = UiKit.CreateImage(rect, "Banner",
                new Vector2(0f, BannerY), new Vector2(PanelWidth, BannerHeight),
                new Color(0.2f, 0.2f, 0.25f, 0.9f));
            _bannerImage.raycastTarget = false;
            _bannerText = UiKit.CreateText(_bannerImage.rectTransform, "BannerText",
                Vector2.zero, new Vector2(PanelWidth, BannerTitleHeight),
                string.Empty, BannerFontSize,
                BannerDefaultColor, TextAlignmentOptions.Center);
            _bannerText.fontStyle = FontStyles.Bold;
            _bannerText.outlineWidth = 0.25f;
            _bannerText.outlineColor = Color.black;

            _epigraphText = UiKit.CreateText(_bannerImage.rectTransform, "BannerEpigraph",
                new Vector2(0f, EpigraphY), new Vector2(PanelWidth - 40f, EpigraphHeight),
                string.Empty, EpigraphFontSize,
                EpigraphColor, TextAlignmentOptions.Center);
            _epigraphText.fontStyle = FontStyles.Italic;
            _epigraphText.outlineWidth = 0.15f;
            _epigraphText.outlineColor = Color.black;
            _epigraphText.enableAutoSizing = true;
            _epigraphText.fontSizeMin = 18f;
            _epigraphText.fontSizeMax = EpigraphFontSize;
            _epigraphText.gameObject.SetActive(false);

            RectTransform viewport = UiKit.CreateRect(rect, "Viewport",
                new Vector2(0f, ViewportCenterY), new Vector2(ContentWidth, ViewportHeight));
            viewport.gameObject.AddComponent<RectMask2D>();

            _content = UiKit.CreateRect(viewport, "Content",
                Vector2.zero, new Vector2(ContentWidth, ViewportHeight));
            _content.anchorMin = new Vector2(0.5f, 1f);
            _content.anchorMax = new Vector2(0.5f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            _returnButtonBg = UiKit.CreateImage(rect, "ReturnButton",
                new Vector2(0f, ReturnButtonY), new Vector2(ReturnButtonWidth, ReturnButtonHeight),
                ReturnButtonPalette.EnabledBg);
            _returnButtonBg.raycastTarget = false;
            _returnButtonGroup = _returnButtonBg.gameObject.AddComponent<CanvasGroup>();
            _returnButtonGroup.alpha = 0f;
            _returnButtonLabel = UiKit.CreateText(_returnButtonBg.rectTransform, "Label",
                Vector2.zero, new Vector2(ReturnButtonWidth, ReturnButtonHeight),
                Texts.Get(TextId.ResultReturnButtonLabel), ReturnButtonFontSize,
                Color.white, TextAlignmentOptions.Center);
            _returnButtonLabel.enableWordWrapping = false;
            _returnButtonBg.gameObject.SetActive(false);
            _returnButtonLabelText = null;

            _footerText = UiKit.CreateText(rect, "Footer",
                new Vector2(0f, FooterY), new Vector2(ContentWidth, 40f),
                string.Empty, FooterFontSize,
                new Color(1f, 0.95f, 0.6f, 1f), TextAlignmentOptions.Center);

            _root.SetActive(false);
        }

        public void Show(byte winningTeam, IReadOnlyList<ResultRow> rows,
            Func<int, PlayerHandle> resolveAvatar = null,
            IReadOnlyList<string> digestLines = null,
            string footerText = null,
            Func<int, int> resolveId = null,
            int winReason = ResultDigest.ReasonUnknown)
        {
            if (_root == null) return;

            _bannerText.text = ResultDigestText.OutcomeText(winningTeam);
            _bannerText.color = BannerColor(winningTeam);
            ShowEpigraph(ResultDigestText.EpigraphText(winReason));
            _footerText.text = footerText ?? string.Empty;

            int count = rows != null ? rows.Count : 0;
            EnsureSlots(count);
            float y = ContentTopPad;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < count)
                {
                    PlayerHandle avatar = PlayerHandle.None;
                    if (resolveAvatar != null && rows[i] != null && rows[i].ActorNumber > 0)
                    {
                        try { avatar = resolveAvatar(rows[i].ActorNumber); }
                        catch { avatar = PlayerHandle.None; }
                    }
                    int participantId = 0;
                    if (resolveId != null && rows[i] != null)
                    {
                        try { participantId = resolveId(rows[i].ActorNumber); }
                        catch { participantId = 0; }
                    }
                    _slots[i].SetTop(y);
                    _slots[i].Show(rows[i], avatar, participantId);
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

        public void Tick(bool wheelBlocked = false)
        {
            if (!Visible || _content == null || wheelBlocked) return;
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

        public void SetReturnButton(bool visible, float alpha, bool armed, bool hover, string keyName,
                                    TextId labelId = TextId.ResultReturnButtonLabel,
                                    TextId labelWithKeyFormatId = TextId.ResultReturnButtonWithKeyFormat)
        {
            if (_returnButtonBg == null) return;
            if (_returnButtonBg.gameObject.activeSelf != visible)
            {
                _returnButtonBg.gameObject.SetActive(visible);
            }
            if (!visible) return;

            _returnButtonGroup.alpha = alpha;
            string labelText = armed
                ? Texts.Get(TextId.VoteConfirmLabel)
                : string.IsNullOrEmpty(keyName)
                    ? Texts.Get(labelId)
                    : Texts.Format(labelWithKeyFormatId, keyName);
            if (_returnButtonLabelText != labelText)
            {
                _returnButtonLabelText = labelText;
                _returnButtonLabel.text = labelText;
            }
            ButtonVisual.Resolve(ReturnButtonPalette,
                armed: armed, hover: hover, selected: false, enabled: true,
                out Color bg, out Color label);
            _returnButtonBg.color = bg;
            _returnButtonLabel.color = label;
        }

        public bool IsPointerOverReturnButton(Vector2 screenPoint)
            => _returnButtonBg != null && _returnButtonBg.gameObject.activeSelf
               && RectTransformUtility.RectangleContainsScreenPoint(
                   _returnButtonBg.rectTransform, screenPoint, null);

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
            _returnButtonBg = null;
            _returnButtonLabel = null;
            _returnButtonGroup = null;
            _returnButtonLabelText = null;
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

        private void ShowEpigraph(string epigraph)
        {
            bool has = !string.IsNullOrEmpty(epigraph);
            _bannerText.rectTransform.anchoredPosition =
                new Vector2(0f, has ? BannerTitleWithEpigraphY : 0f);
            if (_epigraphText == null) return;
            if (has) _epigraphText.text = epigraph;
            _epigraphText.gameObject.SetActive(has);
        }

        private static Color BannerColor(byte outcome)
        {
            switch (outcome)
            {
                case (byte)Team.Villagers: return VillagerTeamColor;
                case (byte)Team.Werewolves: return WerewolfTeamColor;
                case TeamCodes.VoidMatch: return VoidMatchColor;
                case TeamCodes.PeacefulSurvived: return VillagerTeamColor;
                case TeamCodes.PeacefulCollapsed: return PeacefulCollapsedColor;
                default: return BannerDefaultColor;
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

        private static string RoleLabel(Role role) => RoleText.Label(role);

        private static Color RoleColor(Role role, bool alive)
        {
            Color c = RoleDistribution.TeamOf(role) == Team.Werewolves
                ? WerewolfTeamColor
                : VillagerTeamColor;
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
                new Vector2(-PanelWidth * 0.5f + RowInnerMargin + 8f, 0f),
                new Vector2(28f, RowHeight),
                string.Empty, RowFontSize,
                new Color(1f, 0.9f, 0.4f, 1f), TextAlignmentOptions.Center);

            RectTransform avatarHolder = UiKit.CreateRect(bgRect, "AvatarHolder",
                new Vector2(AvatarHolderX, AvatarHolderY), new Vector2(220f, RowHeight));
            avatarHolder.localScale = new Vector3(AvatarScale, AvatarScale, 1f);

            var idLabel = UiKit.CreateText(bgRect, "Id",
                new Vector2(IdLabelX, 0f), new Vector2(30f, RowHeight),
                string.Empty, IdFontSize, IdColor, TextAlignmentOptions.Center);
            idLabel.enableWordWrapping = false;

            float nameStartX = AvatarHolderX + ListedAvatar.NameStartFromHolder(AvatarScale);
            float nameWidth = PanelWidth * 0.05f - nameStartX;
            var name = UiKit.CreateText(bgRect, "Name",
                new Vector2(nameStartX + nameWidth * 0.5f, 0f),
                new Vector2(nameWidth, RowHeight),
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

            var statusIcon = UiKit.CreateImage(bgRect, "StatusIcon",
                new Vector2(AvatarHolderX, 0f), new Vector2(StatusIconSize, StatusIconSize), Color.white);
            statusIcon.gameObject.SetActive(false);

            bg.gameObject.SetActive(false);
            return new Slot(bg.gameObject, mark, avatarHolder, name, role, status, statusIcon, idLabel);
        }

        private sealed class Slot
        {
            private readonly GameObject _go;
            private readonly TextMeshProUGUI _mark;
            private readonly RectTransform _avatarHolder;
            private readonly TextMeshProUGUI _name;
            private readonly TextMeshProUGUI _role;
            private readonly TextMeshProUGUI _status;
            private readonly Image _statusIcon;
            private readonly TextMeshProUGUI _id;
            private ListedRow _listed;

            internal Slot(
                GameObject go,
                TextMeshProUGUI mark,
                RectTransform avatarHolder,
                TextMeshProUGUI name,
                TextMeshProUGUI role,
                TextMeshProUGUI status,
                Image statusIcon,
                TextMeshProUGUI idLabel)
            {
                _go = go;
                _mark = mark;
                _avatarHolder = avatarHolder;
                _name = name;
                _role = role;
                _status = status;
                _statusIcon = statusIcon;
                _id = idLabel;
            }

            internal void SetTop(float yFromTop)
            {
                PlaceTop((RectTransform)_go.transform, yFromTop, RowHeight);
            }

            internal void Show(ResultRow row, PlayerHandle avatar, int participantId)
            {
                if (row == null) { Hide(); return; }
                _mark.text = row.IsWinningSide ? "★" : string.Empty;
                _id.text = participantId > 0 ? participantId.ToString() : string.Empty;
                SetAvatar(avatar, row.ActorNumber, row.Name);
                _name.gameObject.SetActive(_listed == null);
                _name.text = row.Name ?? string.Empty;
                _role.text = RoleLabel(row.Role);
                _role.color = RoleColor(row.Role, row.Alive);
                _status.text = StatusLabel(row.Status);
                SetStatusIcon(row.Status);
                Color nameColor = row.Alive
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(0.7f, 0.7f, 0.7f, 0.9f);
                _name.color = nameColor;
                ListedAvatar.SetStaticShade(_listed, dead: !row.Alive, nameColor);
                if (!_go.activeSelf) _go.SetActive(true);
            }

            internal void Hide()
            {
                SetAvatar(PlayerHandle.None, 0, null);
                if (_go.activeSelf) _go.SetActive(false);
            }

            private void SetStatusIcon(ResultRowStatus status)
            {
                if (_statusIcon == null) return;
                string key;
                switch (status)
                {
                    case ResultRowStatus.Dead: key = "icon_status_dead"; break;
                    case ResultRowStatus.Executed: key = "icon_status_executed"; break;
                    case ResultRowStatus.Disconnected: key = "icon_status_disconnected"; break;
                    default: key = null; break;
                }
                Sprite sprite = key != null ? AssetCatalog.GetSprite(key) : null;
                if (sprite == null)
                {
                    _statusIcon.gameObject.SetActive(false);
                    return;
                }
                _statusIcon.sprite = sprite;
                _statusIcon.gameObject.SetActive(true);
            }

            private void SetAvatar(PlayerHandle avatar, int actor, string playerName)
            {
                if (_listed != null)
                {
                    UnityEngine.Object.Destroy(_listed.GameObject);
                    _listed = null;
                }
                if (avatar.IsValid)
                {
                    _listed = ListedAvatar.TryCreate(_avatarHolder, avatar, playerName);
                }
                else if (actor < 0)
                {
                    _listed = ListedAvatar.TryCreateStatic(_avatarHolder, playerName, BotColors.IndexFor(actor));
                }
            }
        }
    }
}
