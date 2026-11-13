#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class VoidMatchPanel : IClientPanel
    {
        public string LayerName => "VoidMatch";

        private const float BackdropAlpha = 0.78f;
        private const float BoxWidth = 980f;
        private const float BoxHeight = 300f;
        private const float RadialSize = 120f;

        private static readonly Color ChargingColor = new Color(1f, 0.85f, 0.15f, 0.65f);
        private static readonly Color HeaderColor = new Color(1f, 0.55f, 0.5f, 1f);
        private static readonly Color BodyColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.65f);

        private static readonly Color ChargingLabelColor = new Color(1f, 1f, 1f, 0.9f);

        private sealed class ViewRefs
        {
            public ViewRefs(
                GameObject root,
                GameObject confirmRoot,
                Image radial,
                TextMeshProUGUI chargingLabel,
                TextMeshProUGUI promptText,
                TextMeshProUGUI hintText,
                TextMeshProUGUI headerText,
                TextMeshProUGUI bodyText)
            {
                Root = root;
                ConfirmRoot = confirmRoot;
                Radial = radial;
                ChargingLabel = chargingLabel;
                PromptText = promptText;
                HintText = hintText;
                HeaderText = headerText;
                BodyText = bodyText;
            }

            public GameObject Root { get; }
            public GameObject ConfirmRoot { get; }
            public Image Radial { get; }
            public TextMeshProUGUI ChargingLabel { get; }
            public TextMeshProUGUI PromptText { get; }
            public TextMeshProUGUI HintText { get; }
            public TextMeshProUGUI HeaderText { get; }
            public TextMeshProUGUI BodyText { get; }
            public int LastRemaining { get; set; } = -1;
            public KeyCode LastKey { get; set; } = KeyCode.None;
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(VoidMatchPanel));

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

                RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_VoidMatch");
                root = rect.gameObject;

                var canvas = root.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = WerewolfUIManager.VoidMatchSortingOrder;

                RectTransform confirmRect = UiKit.CreateStretched(rect, "Confirm");
                GameObject confirmRoot = confirmRect.gameObject;

                UiKit.CreateBackdrop(confirmRect, "Backdrop", new Color(0f, 0f, 0f, BackdropAlpha));

                Image box = UiKit.CreateImage(confirmRect, "Box", new Vector2(0f, 60f),
                    new Vector2(BoxWidth, BoxHeight), new Color(0.08f, 0.08f, 0.1f, 0.92f));
                box.raycastTarget = false;

                TextMeshProUGUI headerText = UiKit.CreateText(box.rectTransform, "Header", new Vector2(0f, 100f),
                    new Vector2(BoxWidth - 60f, 60f),
                    Texts.Get(TextId.VoidMatchArmHeader), 42f, HeaderColor, TextAlignmentOptions.Center);

                TextMeshProUGUI bodyText = UiKit.CreateText(box.rectTransform, "Body", new Vector2(0f, 20f),
                    new Vector2(BoxWidth - 80f, 90f),
                    Texts.Get(TextId.VoidMatchArmBody), 26f, BodyColor, TextAlignmentOptions.Center);

                TextMeshProUGUI promptText = UiKit.CreateText(box.rectTransform, "Prompt",
                    new Vector2(0f, -60f), new Vector2(BoxWidth - 60f, 44f), string.Empty,
                    30f, Color.white, TextAlignmentOptions.Center);

                TextMeshProUGUI hintText = UiKit.CreateText(box.rectTransform, "Hint",
                    new Vector2(0f, -110f), new Vector2(BoxWidth - 60f, 36f), string.Empty,
                    22f, HintColor, TextAlignmentOptions.Center);

                confirmRoot.SetActive(false);

                Image radial = UiKit.CreateRadialImage(rect, "HoldRadial",
                    Vector2.zero, new Vector2(RadialSize, RadialSize), ChargingColor);
                radial.raycastTarget = false;

                TextMeshProUGUI chargingLabel = UiKit.CreateText(rect, "ChargingLabel",
                    new Vector2(0f, -(RadialSize * 0.5f + 34f)), new Vector2(560f, 52f),
                    Texts.Get(TextId.VoidMatchChargingLabel), 38f, ChargingLabelColor,
                    TextAlignmentOptions.Center);

                root.SetActive(false);
                _view = new ViewRefs(root, confirmRoot, radial, chargingLabel, promptText, hintText,
                    headerText, bodyText);
                _textRefresh.Subscribe(() => Exists, ApplyTexts);
            }
            catch
            {
                _view = null;
                if (root != null)
                {
                    root.SetActive(false);
                    Object.Destroy(root);
                }
                throw;
            }
        }

        public void Tick(bool charging, float ratio, bool armed, int remainingSeconds, KeyCode confirmKey)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            bool anyVisible = charging || armed;
            if (view.Root.activeSelf != anyVisible) view.Root.SetActive(anyVisible);
            if (!anyVisible)
            {
                view.LastRemaining = -1;
                return;
            }

            if (view.ConfirmRoot.activeSelf != armed) view.ConfirmRoot.SetActive(armed);
            if (view.Radial.gameObject.activeSelf != charging) view.Radial.gameObject.SetActive(charging);
            if (view.ChargingLabel.gameObject.activeSelf != charging)
            {
                view.ChargingLabel.gameObject.SetActive(charging);
            }

            if (charging) view.Radial.fillAmount = Mathf.Clamp01(ratio);

            if (!armed) return;

            if (view.LastKey != confirmKey)
            {
                view.LastKey = confirmKey;
                view.PromptText.text = Texts.Format(TextId.VoidMatchConfirmPromptFormat,
                    confirmKey.ToString(), (int)VoidMatchHold.ConfirmSeconds);
            }
            if (view.LastRemaining != remainingSeconds)
            {
                view.LastRemaining = remainingSeconds;
                view.HintText.text = Texts.Format(TextId.VoidMatchCancelHintFormat, remainingSeconds);
            }
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            return null;
        }

        private void ApplyTexts()
        {
            ViewRefs? view = _view;
            if (view == null) return;
            view.HeaderText.text = Texts.Get(TextId.VoidMatchArmHeader);
            view.BodyText.text = Texts.Get(TextId.VoidMatchArmBody);
            view.ChargingLabel.text = Texts.Get(TextId.VoidMatchChargingLabel);
            view.LastKey = KeyCode.None;
            view.LastRemaining = -1;
        }

        public void Destroy()
        {
            _textRefresh.Unsubscribe();
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) Object.Destroy(view.Root);
        }
    }
}
