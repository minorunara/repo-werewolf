using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class LobbyStartWarning : IClientPanel
    {
        private const float ButtonRowY = -160f;
        private GameObject _root;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private Image _panelBackground;
        private RectTransform _backRect;
        private RectTransform _detailsRect;
        private RectTransform _continueRect;
        private Action _onBack;
        private Action _onDetails;
        private Action _onContinue;

        public string LayerName => WerewolfUIManager.ModIntegrityModalLayer;
        public bool Exists => _root != null;
        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var rootGo = new GameObject("WW_LobbyStartWarning", typeof(RectTransform));
            var rootRect = (RectTransform)rootGo.transform;
            rootRect.SetParent(layerRoot, false);
            UiKit.Stretch(rootRect);
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = WerewolfUIManager.PanelSortingOrder;
            rootGo.AddComponent<GraphicRaycaster>();
            Image dim = rootGo.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;
            _root = rootGo;

            RectTransform panel = UiKit.CreateRect(rootRect, "Panel", Vector2.zero, new Vector2(980f, 460f));
            _panelBackground = UiKit.CreateImage(panel, "Bg", Vector2.zero, panel.sizeDelta, new Color(0.26f, 0.16f, 0.03f, 0.99f));
            _title = UiKit.CreateText(panel, "Title", new Vector2(0f, 150f), new Vector2(900f, 70f), string.Empty, 38f, Color.white, TextAlignmentOptions.Center);
            _body = UiKit.CreateText(panel, "Body", new Vector2(0f, -5f), new Vector2(880f, 220f), string.Empty, 27f, Color.white, TextAlignmentOptions.Center);
            _title.richText = false;
            _body.richText = false;
            _body.enableWordWrapping = true;
            _body.enableAutoSizing = true;
            _body.fontSizeMax = 27f;
            _body.fontSizeMin = 18f;

            _backRect = BuildButton(panel, "Back", new Vector2(-320f, ButtonRowY), new Vector2(220f, 58f), Texts.Get(TextId.ModIntegrityButtonBack));
            _detailsRect = BuildButton(panel, "Details", new Vector2(0f, ButtonRowY), new Vector2(260f, 58f), Texts.Get(TextId.ModIntegrityButtonDetails));
            _continueRect = BuildButton(panel, "Continue", new Vector2(320f, ButtonRowY), new Vector2(280f, 58f), Texts.Get(TextId.ModIntegrityButtonContinue));
            _root.SetActive(false);
        }

        public void Show(LobbyStartDecision decision, Action onBack, Action onDetails, Action onContinue)
        {
            if (_root == null) throw new InvalidOperationException("warning_not_built");
            _title.text = ResolveTitle(decision);
            _body.text = ResolveBody(decision);
            _panelBackground.color = decision.WarningKind == LobbyStartWarningKind.Caution
                ? new Color(0.35f, 0.24f, 0.03f, 0.99f)
                : new Color(0.37f, 0.045f, 0.055f, 0.99f);
            LayoutButtons(
                showDetails: decision.ModWarningKind != LobbyStartWarningKind.None,
                showContinue: decision.AllowsContinue);
            _onBack = onBack;
            _onDetails = onDetails;
            _onContinue = onContinue;
            _root.SetActive(true);
        }

        private void LayoutButtons(bool showDetails, bool showContinue)
        {
            _detailsRect.gameObject.SetActive(showDetails);
            _continueRect.gameObject.SetActive(showContinue);

            int count = 1 + (showDetails ? 1 : 0) + (showContinue ? 1 : 0);
            float spacing = count == 3 ? 320f : 200f;
            float left = -spacing * (count - 1) * 0.5f;
            int index = 0;
            PlaceButton(_backRect, left, ref index, spacing);
            if (showDetails) PlaceButton(_detailsRect, left, ref index, spacing);
            if (showContinue) PlaceButton(_continueRect, left, ref index, spacing);
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
            if (!Visible) return;
            UiKit.KeepCursorFree();
            if (!Input.GetMouseButtonDown(0)) return;
            Vector2 mouse = Input.mousePosition;
            if (Hit(_backRect, mouse)) InvokeAndHide(_onBack);
            else if (Hit(_detailsRect, mouse)) InvokeAndHide(_onDetails);
            else if (Hit(_continueRect, mouse)) InvokeAndHide(_onContinue);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
            _onBack = null;
            _onDetails = null;
            _onContinue = null;
        }

        public void Destroy()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _title = null;
            _body = null;
            _panelBackground = null;
            _backRect = null;
            _detailsRect = null;
            _continueRect = null;
            _onBack = null;
            _onDetails = null;
            _onContinue = null;
        }

        private static RectTransform BuildButton(Transform parent, string name, Vector2 position, Vector2 size, string text)
        {
            Image bg = UiKit.CreateImage(parent, name, position, size, new Color(0.16f, 0.18f, 0.24f, 1f));
            TextMeshProUGUI label = UiKit.CreateText(bg.rectTransform, "Label", Vector2.zero, size, text, 23f, Color.white, TextAlignmentOptions.Center);
            label.richText = false;
            return bg.rectTransform;
        }

        private void InvokeAndHide(Action callback)
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
