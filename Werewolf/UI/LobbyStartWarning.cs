#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class LobbyStartWarning : IClientPanel
    {
        private const float ButtonRowY = -160f;

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, TextMeshProUGUI title, TextMeshProUGUI body,
                Image panelBackground, RectTransform backRect, RectTransform detailsRect,
                RectTransform continueRect, TextMeshProUGUI backLabel, TextMeshProUGUI detailsLabel,
                TextMeshProUGUI continueLabel)
            {
                Root = root;
                Title = title;
                Body = body;
                PanelBackground = panelBackground;
                BackRect = backRect;
                DetailsRect = detailsRect;
                ContinueRect = continueRect;
                BackLabel = backLabel;
                DetailsLabel = detailsLabel;
                ContinueLabel = continueLabel;
            }

            public GameObject Root { get; }
            public TextMeshProUGUI Title { get; }
            public TextMeshProUGUI Body { get; }
            public Image PanelBackground { get; }
            public RectTransform BackRect { get; }
            public RectTransform DetailsRect { get; }
            public RectTransform ContinueRect { get; }
            public TextMeshProUGUI BackLabel { get; }
            public TextMeshProUGUI DetailsLabel { get; }
            public TextMeshProUGUI ContinueLabel { get; }
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(LobbyStartWarning));
        private Action? _onBack;
        private Action? _onDetails;
        private Action? _onContinue;
        private LobbyStartDecision? _shownDecision;

        public string LayerName => WerewolfUIManager.ModIntegrityModalLayer;
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
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                root = new GameObject("WW_LobbyStartWarning", typeof(RectTransform));
                var rootRect = (RectTransform)root.transform;
                rootRect.SetParent(layerRoot, false);
                UiKit.Stretch(rootRect);
                var canvas = root.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = WerewolfUIManager.PanelSortingOrder;
                root.AddComponent<GraphicRaycaster>();
                Image dim = root.AddComponent<Image>();
                dim.color = new Color(0f, 0f, 0f, 0.72f);
                dim.raycastTarget = true;

                RectTransform panel = UiKit.CreateRect(rootRect, "Panel", Vector2.zero, new Vector2(980f, 460f));
                Image panelBackground = UiKit.CreateImage(panel, "Bg", Vector2.zero, panel.sizeDelta, new Color(0.26f, 0.16f, 0.03f, 0.99f));
                TextMeshProUGUI title = UiKit.CreateText(panel, "Title", new Vector2(0f, 150f), new Vector2(900f, 70f), string.Empty, 38f, Color.white, TextAlignmentOptions.Center);
                TextMeshProUGUI body = UiKit.CreateText(panel, "Body", new Vector2(0f, -5f), new Vector2(880f, 220f), string.Empty, 27f, Color.white, TextAlignmentOptions.Center);
                title.richText = false;
                body.richText = false;
                body.textWrappingMode = TextWrappingModes.Normal;
                body.enableAutoSizing = true;
                body.fontSizeMax = 27f;
                body.fontSizeMin = 18f;

                RectTransform backRect = BuildButton(panel, "Back", new Vector2(-320f, ButtonRowY), new Vector2(220f, 58f), Texts.Get(TextId.ModIntegrityButtonBack), out TextMeshProUGUI backLabel);
                RectTransform detailsRect = BuildButton(panel, "Details", new Vector2(0f, ButtonRowY), new Vector2(260f, 58f), Texts.Get(TextId.ModIntegrityButtonDetails), out TextMeshProUGUI detailsLabel);
                RectTransform continueRect = BuildButton(panel, "Continue", new Vector2(320f, ButtonRowY), new Vector2(280f, 58f), Texts.Get(TextId.ModIntegrityButtonContinue), out TextMeshProUGUI continueLabel);
                root.SetActive(false);
                _view = new ViewRefs(root, title, body, panelBackground,
                    backRect, detailsRect, continueRect, backLabel, detailsLabel, continueLabel);
                _textRefresh.Subscribe(() => Exists, ApplyTexts);
            }
            catch
            {
                _view = null;
                ClearCallbacks();
                if (root != null)
                {
                    root.SetActive(false);
                    UnityEngine.Object.Destroy(root);
                }
                throw;
            }
        }

        public void Show(LobbyStartDecision decision, Action onBack, Action onDetails, Action onContinue)
        {
            ViewRefs view = GetLiveViewOrNull()
                ?? throw new InvalidOperationException("warning_not_built");
            _shownDecision = decision;
            view.Title.text = ResolveTitle(decision);
            view.Body.text = ResolveBody(decision);
            view.PanelBackground.color = decision.WarningKind == LobbyStartWarningKind.Caution
                ? new Color(0.35f, 0.24f, 0.03f, 0.99f)
                : new Color(0.37f, 0.045f, 0.055f, 0.99f);
            LayoutButtons(view,
                showDetails: decision.ModWarningKind != LobbyStartWarningKind.None,
                showContinue: decision.AllowsContinue);
            _onBack = onBack;
            _onDetails = onDetails;
            _onContinue = onContinue;
            view.Root.SetActive(true);
        }

        private static void LayoutButtons(ViewRefs view, bool showDetails, bool showContinue)
        {
            view.DetailsRect.gameObject.SetActive(showDetails);
            view.ContinueRect.gameObject.SetActive(showContinue);

            int count = 1 + (showDetails ? 1 : 0) + (showContinue ? 1 : 0);
            float spacing = count == 3 ? 320f : 200f;
            float left = -spacing * (count - 1) * 0.5f;
            int index = 0;
            PlaceButton(view.BackRect, left, ref index, spacing);
            if (showDetails) PlaceButton(view.DetailsRect, left, ref index, spacing);
            if (showContinue) PlaceButton(view.ContinueRect, left, ref index, spacing);
        }

        private static void PlaceButton(RectTransform rect, float left, ref int index, float spacing)
        {
            rect.anchoredPosition = new Vector2(left + spacing * index, ButtonRowY);
            index++;
        }

        private static string ResolveTitle(LobbyStartDecision decision)
        {
            if (decision.HasPlayerShortfall) return Texts.Get(TextId.LobbyStartTooFewPlayersTitle);
            if (decision.HasTeamOverflow) return Texts.Get(TextId.LobbyStartTeamOverflowTitle);
            return decision.WarningKind == LobbyStartWarningKind.Caution
                ? Texts.Get(TextId.ModIntegrityStartCautionTitle)
                : Texts.Get(TextId.ModIntegrityStartSevereTitle);
        }

        private static string ResolveBody(LobbyStartDecision decision)
        {
            string body = string.Empty;
            if (decision.HasPlayerShortfall)
            {
                body = Texts.Format(TextId.LobbyStartTooFewPlayersBodyFormat,
                    decision.PlayerCount, decision.RequiredPlayerCount);
            }
            if (decision.HasTeamOverflow)
            {
                if (body.Length > 0) body += "\n";
                body += Texts.Format(TextId.LobbyStartTeamOverflowBodyFormat,
                    decision.WerewolfCount, decision.PlayerCount);
            }
            if (decision.ModWarningKind != LobbyStartWarningKind.None)
            {
                if (body.Length > 0) body += "\n";
                body += Texts.Format(TextId.ModIntegrityStartBodyFormat,
                    decision.DifferenceCount, decision.UnavailableCount, decision.PendingCount);
            }
            if (!decision.AllowsContinue)
            {
                if (body.Length > 0) body += "\n";
                body += Texts.Get(TextId.LobbyStartBlockedRemedy);
            }
            return body;
        }

        public void Tick()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || !view.Root.activeSelf) return;
            UiKit.KeepCursorFree();
            if (!Input.GetMouseButtonDown(0)) return;
            Vector2 mouse = Input.mousePosition;
            if (Hit(view.BackRect, mouse)) InvokeAndHide(_onBack);
            else if (Hit(view.DetailsRect, mouse)) InvokeAndHide(_onDetails);
            else if (Hit(view.ContinueRect, mouse)) InvokeAndHide(_onContinue);
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) view.Root.SetActive(false);
            ClearCallbacks();
        }

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view == null) return;
            view.BackLabel.text = Texts.Get(TextId.ModIntegrityButtonBack);
            view.DetailsLabel.text = Texts.Get(TextId.ModIntegrityButtonDetails);
            view.ContinueLabel.text = Texts.Get(TextId.ModIntegrityButtonContinue);
            if (_shownDecision.HasValue && view.Root.activeSelf)
            {
                view.Title.text = ResolveTitle(_shownDecision.Value);
                view.Body.text = ResolveBody(_shownDecision.Value);
            }
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            ClearCallbacks();
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            ClearCallbacks();
            return null;
        }

        private void ClearCallbacks()
        {
            _shownDecision = null;
            _onBack = null;
            _onDetails = null;
            _onContinue = null;
        }

        private static RectTransform BuildButton(Transform parent, string name, Vector2 position, Vector2 size, string text,
            out TextMeshProUGUI label)
        {
            Image bg = UiKit.CreateImage(parent, name, position, size, new Color(0.16f, 0.18f, 0.24f, 1f));
            label = UiKit.CreateText(bg.rectTransform, "Label", Vector2.zero, size, text, 23f, Color.white, TextAlignmentOptions.Center);
            label.richText = false;
            return bg.rectTransform;
        }

        private void InvokeAndHide(Action? callback)
        {
            Hide();
            callback?.Invoke();
        }

        private static bool Hit(RectTransform rect, Vector2 point)
        {
            return rect != null && rect.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(rect, point, null);
        }
    }
}
