using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

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

        private GameObject _root;
        private GameObject _confirmRoot;
        private Image _radial;
        private TextMeshProUGUI _chargingLabel;
        private TextMeshProUGUI _promptText;
        private TextMeshProUGUI _hintText;

        private int _lastRemaining = -1;
        private KeyCode _lastKey = KeyCode.None;

        public bool Exists => _root != null;

        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_VoidMatch", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _root = go;

            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = WerewolfUIManager.VoidMatchSortingOrder;

            var confirm = new GameObject("Confirm", typeof(RectTransform));
            var confirmRect = (RectTransform)confirm.transform;
            confirmRect.SetParent(rect, false);
            UiKit.Stretch(confirmRect);
            _confirmRoot = confirm;

            Image backdrop = UiKit.CreateImage(confirmRect, "Backdrop", Vector2.zero,
                new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, BackdropAlpha));
            backdrop.raycastTarget = false;
            var bgRect = backdrop.rectTransform;
            UiKit.Stretch(bgRect);

            Image box = UiKit.CreateImage(confirmRect, "Box", new Vector2(0f, 60f),
                new Vector2(BoxWidth, BoxHeight), new Color(0.08f, 0.08f, 0.1f, 0.92f));
            box.raycastTarget = false;

            UiKit.CreateText(box.rectTransform, "Header", new Vector2(0f, 100f),
                new Vector2(BoxWidth - 60f, 60f),
                Texts.Get(TextId.VoidMatchArmHeader), 42f, HeaderColor, TextAlignmentOptions.Center);

            UiKit.CreateText(box.rectTransform, "Body", new Vector2(0f, 20f),
                new Vector2(BoxWidth - 80f, 90f),
                Texts.Get(TextId.VoidMatchArmBody), 26f, BodyColor, TextAlignmentOptions.Center);

            _promptText = UiKit.CreateText(box.rectTransform, "Prompt", new Vector2(0f, -60f),
                new Vector2(BoxWidth - 60f, 44f),
                string.Empty, 30f, Color.white, TextAlignmentOptions.Center);

            _hintText = UiKit.CreateText(box.rectTransform, "Hint", new Vector2(0f, -110f),
                new Vector2(BoxWidth - 60f, 36f),
                string.Empty, 22f, HintColor, TextAlignmentOptions.Center);

            _confirmRoot.SetActive(false);

            _radial = UiKit.CreateRadialImage(rect, "HoldRadial",
                Vector2.zero, new Vector2(RadialSize, RadialSize), ChargingColor);
            _radial.raycastTarget = false;

            _chargingLabel = UiKit.CreateText(rect, "ChargingLabel",
                new Vector2(0f, -(RadialSize * 0.5f + 34f)), new Vector2(560f, 52f),
                Texts.Get(TextId.VoidMatchChargingLabel), 38f, ChargingLabelColor,
                TextAlignmentOptions.Center);

            _root.SetActive(false);
        }

        public void Tick(bool charging, float ratio, bool armed, int remainingSeconds, KeyCode confirmKey)
        {
            if (_root == null) return;

            bool anyVisible = charging || armed;
            if (_root.activeSelf != anyVisible) _root.SetActive(anyVisible);
            if (!anyVisible)
            {
                _lastRemaining = -1;
                return;
            }

            if (_confirmRoot.activeSelf != armed) _confirmRoot.SetActive(armed);
            if (_radial.gameObject.activeSelf != charging) _radial.gameObject.SetActive(charging);
            if (_chargingLabel.gameObject.activeSelf != charging)
            {
                _chargingLabel.gameObject.SetActive(charging);
            }

            if (charging) _radial.fillAmount = Mathf.Clamp01(ratio);

            if (!armed) return;

            if (_lastKey != confirmKey)
            {
                _lastKey = confirmKey;
                _promptText.text = Texts.Format(TextId.VoidMatchConfirmPromptFormat,
                    confirmKey.ToString(), (int)VoidMatchHold.ConfirmSeconds);
            }
            if (_lastRemaining != remainingSeconds)
            {
                _lastRemaining = remainingSeconds;
                _hintText.text = Texts.Format(TextId.VoidMatchCancelHintFormat, remainingSeconds);
            }
        }

        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
            _confirmRoot = null;
            _radial = null;
            _chargingLabel = null;
            _promptText = null;
            _hintText = null;
            _lastRemaining = -1;
            _lastKey = KeyCode.None;
        }
    }
}
