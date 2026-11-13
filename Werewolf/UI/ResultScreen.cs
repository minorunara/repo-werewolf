#nullable enable
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

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, TextMeshProUGUI bannerText, TextMeshProUGUI epigraphText,
                Image bannerImage, RectTransform content, TextMeshProUGUI footerText,
                Image returnButtonBg, TextMeshProUGUI returnButtonLabel, CanvasGroup returnButtonGroup,
                TextMeshProUGUI digestHeader)
            {
                Root = root;
                BannerText = bannerText;
                EpigraphText = epigraphText;
                BannerImage = bannerImage;
                Content = content;
                FooterText = footerText;
                ReturnButtonBg = returnButtonBg;
                ReturnButtonLabel = returnButtonLabel;
                ReturnButtonGroup = returnButtonGroup;
                DigestHeader = digestHeader;
            }

            public GameObject Root { get; }
            public TextMeshProUGUI BannerText { get; }
            public TextMeshProUGUI EpigraphText { get; }
            public Image BannerImage { get; }
            public RectTransform Content { get; }
            public TextMeshProUGUI FooterText { get; }
            public Image ReturnButtonBg { get; }
            public TextMeshProUGUI ReturnButtonLabel { get; }
            public CanvasGroup ReturnButtonGroup { get; }
            public TextMeshProUGUI DigestHeader { get; }
            public List<Slot> Slots { get; } = new List<Slot>();
            public List<GameObject> DigestObjects { get; } = new List<GameObject>();
            public string? ReturnButtonLabelText { get; set; }
            public float ContentHeight { get; set; }
            public float ScrollY { get; set; }
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(ResultScreen));

        private byte? _shownTeam;
        private int _shownWinReason = ResultDigest.ReasonUnknown;

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

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            GameObject? root = null;
            try
            {
                RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_ResultScreen");
                root = rect.gameObject;

            UiKit.CreateBackdrop(rect, "Bg", new Color(0f, 0f, 0f, 0.82f));

            Image bannerImage = UiKit.CreateImage(rect, "Banner",
                new Vector2(0f, BannerY), new Vector2(PanelWidth, BannerHeight),
                new Color(0.2f, 0.2f, 0.25f, 0.9f));
            bannerImage.raycastTarget = false;
            TextMeshProUGUI bannerText = UiKit.CreateText(bannerImage.rectTransform, "BannerText",
                Vector2.zero, new Vector2(PanelWidth, BannerTitleHeight),
                string.Empty, BannerFontSize,
                BannerDefaultColor, TextAlignmentOptions.Center);
            bannerText.fontStyle = FontStyles.Bold;
            bannerText.outlineWidth = 0.25f;
            bannerText.outlineColor = Color.black;

            TextMeshProUGUI epigraphText = UiKit.CreateText(bannerImage.rectTransform, "BannerEpigraph",
                new Vector2(0f, EpigraphY), new Vector2(PanelWidth - 40f, EpigraphHeight),
                string.Empty, EpigraphFontSize,
                EpigraphColor, TextAlignmentOptions.Center);
            epigraphText.fontStyle = FontStyles.Italic;
            epigraphText.outlineWidth = 0.15f;
            epigraphText.outlineColor = Color.black;
            epigraphText.enableAutoSizing = true;
            epigraphText.fontSizeMin = 18f;
            epigraphText.fontSizeMax = EpigraphFontSize;
            epigraphText.gameObject.SetActive(false);

            RectTransform viewport = UiKit.CreateRect(rect, "Viewport",
                new Vector2(0f, ViewportCenterY), new Vector2(ContentWidth, ViewportHeight));
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = UiKit.CreateRect(viewport, "Content",
                Vector2.zero, new Vector2(ContentWidth, ViewportHeight));
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            TextMeshProUGUI digestHeader = UiKit.CreateText(content, "DigestHeader",
                Vector2.zero, new Vector2(ContentWidth - 40f, DigestHeaderHeight),
                Texts.Get(TextId.ResultDigestHeader), DigestFontSize,
                new Color(1f, 0.9f, 0.55f, 1f), TextAlignmentOptions.Center);
            digestHeader.gameObject.SetActive(false);

            Image returnButtonBg = UiKit.CreateImage(rect, "ReturnButton",
                new Vector2(0f, ReturnButtonY), new Vector2(ReturnButtonWidth, ReturnButtonHeight),
                ReturnButtonPalette.EnabledBg);
            returnButtonBg.raycastTarget = false;
            CanvasGroup returnButtonGroup = returnButtonBg.gameObject.AddComponent<CanvasGroup>();
            returnButtonGroup.alpha = 0f;
            TextMeshProUGUI returnButtonLabel = UiKit.CreateText(returnButtonBg.rectTransform, "Label",
                Vector2.zero, new Vector2(ReturnButtonWidth, ReturnButtonHeight),
                Texts.Get(TextId.ResultReturnButtonLabel), ReturnButtonFontSize,
                Color.white, TextAlignmentOptions.Center);
            returnButtonLabel.textWrappingMode = TextWrappingModes.NoWrap;
            returnButtonBg.gameObject.SetActive(false);

            TextMeshProUGUI footerText = UiKit.CreateText(rect, "Footer",
                new Vector2(0f, FooterY), new Vector2(ContentWidth, 40f),
                string.Empty, FooterFontSize,
                new Color(1f, 0.95f, 0.6f, 1f), TextAlignmentOptions.Center);

                root.SetActive(false);
                _view = new ViewRefs(root, bannerText, epigraphText, bannerImage, content,
                    footerText, returnButtonBg, returnButtonLabel, returnButtonGroup, digestHeader);
                _shownTeam = null;
                _shownWinReason = ResultDigest.ReasonUnknown;
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

        public void Show(byte winningTeam, IReadOnlyList<ResultRow>? rows,
            Func<int, PlayerHandle>? resolveAvatar = null,
            IReadOnlyList<string>? digestLines = null,
            string? footerText = null,
            Func<int, int>? resolveId = null,
            int winReason = ResultDigest.ReasonUnknown)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            _shownTeam = winningTeam;
            _shownWinReason = winReason;
            view.BannerText.text = ResultDigestText.OutcomeText(winningTeam);
            view.BannerText.color = BannerColor(winningTeam);
            ShowEpigraph(view, ResultDigestText.EpigraphText(winReason));
            view.FooterText.text = footerText ?? string.Empty;

            IReadOnlyList<ResultRow> renderedRows = rows ?? Array.Empty<ResultRow>();
            int count = renderedRows.Count;
            EnsureSlots(view, count);
            float y = ContentTopPad;
            for (int i = 0; i < view.Slots.Count; i++)
            {
                if (i < count)
                {
                    PlayerHandle avatar = PlayerHandle.None;
                    if (resolveAvatar != null && renderedRows[i] != null && renderedRows[i].ActorNumber > 0)
                    {
                        try { avatar = resolveAvatar(renderedRows[i].ActorNumber); }
                        catch (Exception e)
                        {
                            WLog.Line("result_row_avatar_error", secret: false,
                                ("actor", renderedRows[i].ActorNumber), ("err", e.Message));
                            avatar = PlayerHandle.None;
                        }
                    }
                    int participantId = 0;
                    if (resolveId != null && renderedRows[i] != null)
                    {
                        try { participantId = resolveId(renderedRows[i].ActorNumber); }
                        catch (Exception e)
                        {
                            WLog.Line("result_row_id_error", secret: false,
                                ("actor", renderedRows[i].ActorNumber), ("err", e.Message));
                            participantId = 0;
                        }
                    }
                    view.Slots[i].SetTop(y);
                    view.Slots[i].Show(renderedRows[i], avatar, participantId);
                    y += RowHeight + RowSpacing;
                }
                else
                {
                    view.Slots[i].Hide();
                }
            }

            ClearDigestObjects(view);
            IReadOnlyList<string> renderedDigest = digestLines ?? Array.Empty<string>();
            int digestCount = renderedDigest.Count;
            if (digestCount > 0)
            {
                y += SectionGap;
                TextMeshProUGUI header = view.DigestHeader;
                header.text = Texts.Get(TextId.ResultDigestHeader);
                PlaceTop(header.rectTransform, y, DigestHeaderHeight);
                header.gameObject.SetActive(true);
                y += DigestHeaderHeight;

                for (int i = 0; i < digestCount; i++)
                {
                    var line = UiKit.CreateText(view.Content, "DigestLine" + i,
                        Vector2.zero, new Vector2(ContentWidth - 120f, DigestLineHeight),
                        renderedDigest[i] ?? string.Empty, DigestFontSize,
                        new Color(0.9f, 0.92f, 0.95f, 1f), TextAlignmentOptions.MidlineLeft);
                    PlaceTop(line.rectTransform, y, DigestLineHeight);
                    view.DigestObjects.Add(line.gameObject);
                    y += DigestLineHeight;
                }
            }

            view.ContentHeight = y + ContentBottomPad;
            view.Content.sizeDelta = new Vector2(ContentWidth, Mathf.Max(view.ContentHeight, ViewportHeight));
            view.ScrollY = 0f;
            view.Content.anchoredPosition = Vector2.zero;

            if (!view.Root.activeSelf) view.Root.SetActive(true);
            WLog.Line("result_screen_show", secret: false,
                ("team", winningTeam), ("rows", count), ("digest", digestCount));
        }

        public void ShowPreplayAbort(string title, string reason, string note, string footerText)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            _shownTeam = null;
            view.BannerText.text = title ?? string.Empty;
            view.BannerText.color = VoidMatchColor;
            view.BannerImage.color = new Color(0.2f, 0.2f, 0.22f, 0.94f);
            ShowEpigraph(view, null);
            view.FooterText.text = footerText ?? string.Empty;

            foreach (Slot slot in view.Slots) slot.Hide();
            ClearDigestObjects(view);

            var reasonText = UiKit.CreateText(view.Content, "PreplayAbortReason",
                Vector2.zero, new Vector2(ContentWidth - 120f, 150f),
                reason ?? string.Empty, 34f,
                new Color(0.95f, 0.95f, 0.98f, 1f), TextAlignmentOptions.Center);
            reasonText.textWrappingMode = TextWrappingModes.Normal;
            PlaceTop(reasonText.rectTransform, 170f, 150f);
            view.DigestObjects.Add(reasonText.gameObject);

            var noteText = UiKit.CreateText(view.Content, "PreplayAbortNote",
                Vector2.zero, new Vector2(ContentWidth - 160f, 100f),
                note ?? string.Empty, 27f,
                new Color(0.74f, 0.76f, 0.8f, 1f), TextAlignmentOptions.Center);
            noteText.textWrappingMode = TextWrappingModes.Normal;
            PlaceTop(noteText.rectTransform, 350f, 100f);
            view.DigestObjects.Add(noteText.gameObject);

            view.ContentHeight = ViewportHeight;
            view.Content.sizeDelta = new Vector2(ContentWidth, ViewportHeight);
            view.ScrollY = 0f;
            view.Content.anchoredPosition = Vector2.zero;
            if (!view.Root.activeSelf) view.Root.SetActive(true);
        }

        public void Tick(bool wheelBlocked = false)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || !view.Root.activeSelf || wheelBlocked) return;
            float wheel = Input.mouseScrollDelta.y;
            if (wheel == 0f) return;

            float maxScroll = Mathf.Max(0f, view.ContentHeight - ViewportHeight);
            view.ScrollY = Mathf.Clamp(view.ScrollY - wheel * ScrollStep, 0f, maxScroll);
            view.Content.anchoredPosition = new Vector2(0f, view.ScrollY);
        }

        public void SetFooter(string footerText)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) view.FooterText.text = footerText ?? string.Empty;
        }

        public void SetReturnButton(bool visible, float alpha, bool armed, bool hover, string? keyName,
                                    TextId labelId = TextId.ResultReturnButtonLabel,
                                    TextId labelWithKeyFormatId = TextId.ResultReturnButtonWithKeyFormat)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            if (view.ReturnButtonBg.gameObject.activeSelf != visible)
            {
                view.ReturnButtonBg.gameObject.SetActive(visible);
            }
            if (!visible) return;

            view.ReturnButtonGroup.alpha = alpha;
            string labelText = armed
                ? Texts.Get(TextId.VoteConfirmLabel)
                : string.IsNullOrEmpty(keyName)
                    ? Texts.Get(labelId)
                    : Texts.Format(labelWithKeyFormatId, keyName);
            if (view.ReturnButtonLabelText != labelText)
            {
                view.ReturnButtonLabelText = labelText;
                view.ReturnButtonLabel.text = labelText;
            }
            ButtonVisual.Resolve(ReturnButtonPalette,
                armed: armed, hover: hover, selected: false, enabled: true,
                out Color bg, out Color label);
            view.ReturnButtonBg.color = bg;
            view.ReturnButtonLabel.color = label;
        }

        public bool IsPointerOverReturnButton(Vector2 screenPoint)
        {
            ViewRefs? view = GetLiveViewOrNull();
            return view != null && view.ReturnButtonBg.gameObject.activeSelf
                && RectTransformUtility.RectangleContainsScreenPoint(
                    view.ReturnButtonBg.rectTransform, screenPoint, null);
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null && view.Root.activeSelf) view.Root.SetActive(false);
        }

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view == null) return;
            view.ReturnButtonLabelText = null;
            view.ReturnButtonLabel.text = Texts.Get(TextId.ResultReturnButtonLabel);
            if (_shownTeam.HasValue)
            {
                view.BannerText.text = ResultDigestText.OutcomeText(_shownTeam.Value);
                ShowEpigraph(view, ResultDigestText.EpigraphText(_shownWinReason));
                view.DigestHeader.text = Texts.Get(TextId.ResultDigestHeader);
            }
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            _shownTeam = null;
            _shownWinReason = ResultDigest.ReasonUnknown;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private static void EnsureSlots(ViewRefs view, int count)
        {
            while (view.Slots.Count < count)
            {
                view.Slots.Add(BuildSlot(view.Content, view.Slots.Count));
            }
        }

        private static void ClearDigestObjects(ViewRefs view)
        {
            foreach (GameObject go in view.DigestObjects)
            {
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            view.DigestObjects.Clear();
            if (view.DigestHeader != null) view.DigestHeader.gameObject.SetActive(false);
        }

        private static void PlaceTop(RectTransform rt, float yFromTop, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(yFromTop + height * 0.5f));
        }

        private static void ShowEpigraph(ViewRefs view, string? epigraph)
        {
            bool has = !string.IsNullOrEmpty(epigraph);
            view.BannerText.rectTransform.anchoredPosition =
                new Vector2(0f, has ? BannerTitleWithEpigraphY : 0f);
            if (has) view.EpigraphText.text = epigraph;
            view.EpigraphText.gameObject.SetActive(has);
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;
            _view = null;
            return null;
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
            idLabel.textWrappingMode = TextWrappingModes.NoWrap;

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
            private ListedRow? _listed;

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
                string? key;
                switch (status)
                {
                    case ResultRowStatus.Dead: key = "icon_status_dead"; break;
                    case ResultRowStatus.Executed: key = AlternativeExecutionIcon.DefaultAssetKey; break;
                    case ResultRowStatus.Disconnected: key = "icon_status_disconnected"; break;
                    default: key = null; break;
                }
                Sprite? sprite = key != null ? AssetCatalog.GetSprite(key) : null;
                if (sprite == null)
                {
                    _statusIcon.gameObject.SetActive(false);
                    return;
                }
                _statusIcon.sprite = sprite;
                _statusIcon.gameObject.SetActive(true);
            }

            private void SetAvatar(PlayerHandle avatar, int actor, string? playerName)
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
