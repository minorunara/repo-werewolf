using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

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

        private GameObject _root;
        private readonly List<Slot> _slots = new List<Slot>(SlotCount);

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_ToastPanel", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-RightMargin, -TopMargin);
            rect.sizeDelta = new Vector2(
                SlotWidth,
                SlotCount * SlotHeight + (SlotCount - 1) * SlotSpacing);
            _root = go;

            for (int i = 0; i < SlotCount; i++)
            {
                _slots.Add(BuildSlot(rect, i));
            }
            WLog.Line("toast_panel_built", secret: false, ("slots", SlotCount));
        }

        public void Tick(ToastQueue queue, long nowUnixMs)
        {
            if (_root == null || queue == null) return;

            IReadOnlyList<ToastEntry> visible = queue.Visible(nowUnixMs);
            int count = visible != null ? visible.Count : 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < count)
                {
                    _slots[i].Show(visible[i].Message);
                }
                else
                {
                    _slots[i].Hide();
                }
            }
        }

        public void Hide()
        {
            for (int i = 0; i < _slots.Count; i++) _slots[i].Hide();
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _slots.Clear();
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
            text.enableWordWrapping = false;
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
