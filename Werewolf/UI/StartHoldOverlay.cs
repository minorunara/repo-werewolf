using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class StartHoldOverlay : IClientPanel
    {
        public string LayerName => "StartHold";

        private const float BackdropAlpha = 0.85f;

        private const float FadeOutPerSec = 1.25f;

        private GameObject _root;
        private CanvasGroup _group;

        public bool Exists => _root != null;

        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_StartHold", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _root = go;

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            Image backdrop = UiKit.CreateImage(rect, "Backdrop", Vector2.zero,
                new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, BackdropAlpha));
            var bgRect = backdrop.rectTransform;
            UiKit.Stretch(bgRect);

            UiKit.CreateText(rect, "Message", new Vector2(0f, 0f), new Vector2(1600f, 100f),
                Texts.Get(TextId.StartHoldWaitingOthers), 44f, Color.white, TextAlignmentOptions.Center);

            _root.SetActive(false);
        }

        public void Tick(bool holding)
        {
            if (_root == null) return;

            if (holding)
            {
                if (!_root.activeSelf) _root.SetActive(true);
                _group.alpha = 1f;
                return;
            }

            if (!_root.activeSelf) return;
            _group.alpha -= FadeOutPerSec * Time.unscaledDeltaTime;
            if (_group.alpha <= 0f)
            {
                _group.alpha = 0f;
                _root.SetActive(false);
            }
        }

        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
            _group = null;
        }
    }
}
