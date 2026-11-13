#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class StartHoldOverlay : IClientPanel
    {
        public string LayerName => "StartHold";

        private const float BackdropAlpha = 0.85f;

        private const float FadeOutPerSec = 1.25f;

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, CanvasGroup group, TextMeshProUGUI messageText)
            {
                Root = root;
                Group = group;
                MessageText = messageText;
            }

            public GameObject Root { get; }
            public CanvasGroup Group { get; }
            public TextMeshProUGUI MessageText { get; }
        }

        private ViewRefs? _view;
        private readonly TextRefresh _textRefresh = new TextRefresh(nameof(StartHoldOverlay));

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

                RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_StartHold");
                root = rect.gameObject;

                CanvasGroup group = root.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;

                UiKit.CreateBackdrop(rect, "Backdrop", new Color(0f, 0f, 0f, BackdropAlpha));

                TextMeshProUGUI messageText = UiKit.CreateText(rect, "Message", new Vector2(0f, 0f), new Vector2(1600f, 100f),
                    Texts.Get(TextId.StartHoldWaitingOthers), 44f, Color.white, TextAlignmentOptions.Center);

                root.SetActive(false);
                _view = new ViewRefs(root, group, messageText);
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

        public void Tick(bool holding)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            if (holding)
            {
                if (!view.Root.activeSelf) view.Root.SetActive(true);
                view.Group.alpha = 1f;
                return;
            }

            if (!view.Root.activeSelf) return;
            view.Group.alpha -= FadeOutPerSec * Time.unscaledDeltaTime;
            if (view.Group.alpha <= 0f)
            {
                view.Group.alpha = 0f;
                view.Root.SetActive(false);
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
            if (view != null && view.MessageText != null)
                view.MessageText.text = Texts.Get(TextId.StartHoldWaitingOthers);
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
