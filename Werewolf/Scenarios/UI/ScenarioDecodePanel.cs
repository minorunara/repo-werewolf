using TMPro;
using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Scenarios.UI
{
    public sealed class ScenarioDecodePanel : IClientPanel
    {
        public string LayerName => "ScenarioDecode";

        private const float WindowWidth = HudGeometry.DecodeWindow.Width;
        private const float WindowHeight = HudGeometry.DecodeWindow.Height;
        private const float FrameThickness = 4f;
        private const float HeaderHeight = 48f;
        private const float FontSize = 38f;
        private const float BlockWidth = WindowWidth - 72f;
        private const float BlockHeight = 176f;

        private static readonly Color TextColor = new Color(0.32f, 1f, 0.36f, 0.95f);
        private static readonly Color HeaderColor = new Color(0.36f, 1f, 0.42f, 0.8f);
        private static readonly Color FrameColor = new Color(0.18f, 0.82f, 0.25f, 0.92f);
        private static readonly Color ScreenColor = new Color(0.005f, 0.025f, 0.012f, 0.94f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.62f);
        private static readonly Color ScanlineColor = new Color(0.18f, 1f, 0.26f, 0.035f);

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, TextMeshProUGUI text)
            {
                Root = root;
                Text = text;
            }

            public GameObject Root { get; }
            public TextMeshProUGUI Text { get; }
        }

        private ViewRefs? _view;

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            GameObject? partialRoot = null;
            try
            {
                RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_ScenarioDecode");
                GameObject go = rect.gameObject;
                partialRoot = go;

                var canvas = go.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = WerewolfUIManager.ScenarioDecodeSortingOrder;

                UiKit.CreateImage(rect, "Shadow", new Vector2(9f, -9f),
                    new Vector2(WindowWidth + 14f, WindowHeight + 14f), ShadowColor);
                UiKit.CreateImage(rect, "Frame", Vector2.zero,
                    new Vector2(WindowWidth, WindowHeight), FrameColor);
                RectTransform screen = UiKit.CreateRect(rect, "Screen", Vector2.zero,
                    new Vector2(WindowWidth - FrameThickness * 2f, WindowHeight - FrameThickness * 2f));
                var screenImage = screen.gameObject.AddComponent<UnityEngine.UI.Image>();
                screenImage.color = ScreenColor;
                screenImage.raycastTarget = false;

                float headerY = WindowHeight * 0.5f - HeaderHeight * 0.5f - FrameThickness;
                TextMeshProUGUI title = UiKit.CreateText(screen, "Header",
                    new Vector2(0f, headerY), new Vector2(BlockWidth, HeaderHeight),
                    "TAXMAN SIGNAL INTERFACE  //  SEMANTIC DECODER",
                    20f, HeaderColor, TextAlignmentOptions.MidlineLeft);
                title.characterSpacing = 2f;

                float dividerY = WindowHeight * 0.5f - HeaderHeight - FrameThickness;
                UiKit.CreateImage(screen, "HeaderDivider", new Vector2(0f, dividerY),
                    new Vector2(BlockWidth, 2f), FrameColor);

                for (int i = 0; i < 8; i++)
                {
                    float y = dividerY - 24f - i * 28f;
                    UiKit.CreateImage(screen, "Scanline" + i, new Vector2(0f, y),
                        new Vector2(BlockWidth, 1f), ScanlineColor);
                }

                TextMeshProUGUI text = UiKit.CreateText(screen, "Text", new Vector2(0f, -24f),
                    new Vector2(BlockWidth, BlockHeight),
                    string.Empty, FontSize, TextColor, TextAlignmentOptions.Left);

                go.SetActive(false);
                _view = new ViewRefs(go, text);
                partialRoot = null;
            }
            catch
            {
                if (partialRoot != null) Object.Destroy(partialRoot);
                _view = null;
                throw;
            }
        }

        public void Show(string? block)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            if (string.IsNullOrEmpty(block))
            {
                Hide();
                return;
            }
            view.Root.SetActive(true);
            if (view.Text.text != block) view.Text.text = block;
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) view.Root.SetActive(false);
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) Object.Destroy(view.Root);
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;
            _view = null;
            return null;
        }
    }
}
