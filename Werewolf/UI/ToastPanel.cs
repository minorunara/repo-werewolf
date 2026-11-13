#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class ToastPanel : IClientPanel
    {
        public string LayerName => "Toast";

        public const int SlotCount = 5;

        private const float RightMargin = 24f;
        private const float TopMargin = 24f;
        private const float SlotWidth = 960f;
        private const float SlotHeight = 88f;
        private const float SlotSpacing = 16f;
        private const float FontSize = 44f;

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, List<Slot> slots)
            {
                Root = root;
                Slots = slots;
            }

            public GameObject Root { get; }
            public List<Slot> Slots { get; }
        }

        private ViewRefs? _view;

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                RectTransform rect = UiKit.CreateAnchoredRect(layerRoot, "WW_ToastPanel",
                    UiKit.TopRight, new Vector2(1f, 1f),
                    new Vector2(-RightMargin, -TopMargin),
                    new Vector2(SlotWidth, SlotCount * SlotHeight + (SlotCount - 1) * SlotSpacing));
                root = rect.gameObject;
                var slots = new List<Slot>(SlotCount);
                for (int i = 0; i < SlotCount; i++)
                {
                    slots.Add(BuildSlot(rect, i));
                }

                _view = new ViewRefs(root, slots);
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

        public void Tick(ToastQueue queue, long nowUnixMs)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || queue == null) return;

            IReadOnlyList<ToastEntry> visible = queue.Visible(nowUnixMs);
            int count = visible.Count;
            for (int i = 0; i < view.Slots.Count; i++)
            {
                if (i < count)
                {
                    view.Slots[i].Show(visible[i].Message);
                }
                else
                {
                    view.Slots[i].Hide();
                }
            }
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            for (int i = 0; i < view.Slots.Count; i++) view.Slots[i].Hide();
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            return null;
        }

        private static Slot BuildSlot(Transform parent, int index)
        {
            float y = -(index * (SlotHeight + SlotSpacing));

            var bg = UiKit.CreateImage(parent, $"Slot{index}",
                new Vector2(0f, y), new Vector2(SlotWidth, SlotHeight),
                new Color(0f, 0f, 0f, 0.55f));
            var bgRect = bg.rectTransform;
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.pivot = new Vector2(1f, 1f);
            bgRect.anchoredPosition = new Vector2(0f, y);
            bg.raycastTarget = false;

            var text = UiKit.CreateText(bgRect, "Text",
                new Vector2(0f, 0f), new Vector2(SlotWidth - 20f, SlotHeight - 6f),
                string.Empty, FontSize,
                new Color(1f, 0.95f, 0.8f, 0.98f),
                TextAlignmentOptions.MidlineRight);
            var tRect = text.rectTransform;
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(10f, 3f);
            tRect.offsetMax = new Vector2(-10f, -3f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;

            bg.gameObject.SetActive(false);
            return new Slot(bg.gameObject, text);
        }

        private sealed class Slot
        {
            private readonly GameObject _go;
            private readonly TextMeshProUGUI _text;

            internal Slot(GameObject go, TextMeshProUGUI text)
            {
                _go = go;
                _text = text;
            }

            internal void Show(string message)
            {
                if (_text.text != message) _text.text = message ?? string.Empty;
                if (!_go.activeSelf) _go.SetActive(true);
            }

            internal void Hide()
            {
                if (_go.activeSelf) _go.SetActive(false);
            }
        }
    }
}
