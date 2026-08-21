using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal sealed class ChatScrollView
    {
        private readonly float _overscan;
        private readonly float _padding;
        private readonly float _bottomSlack;

        private ScrollRect _scroll;
        private RectTransform _viewport;
        private RectTransform _content;

        private readonly Dictionary<int, GameObject> _live = new Dictionary<int, GameObject>();
        private readonly List<int> _evictScratch = new List<int>();
        private int _liveEpoch = int.MinValue;

        public ChatScrollView(float overscan, float padding, float bottomSlack)
        {
            _overscan = overscan;
            _padding = padding;
            _bottomSlack = bottomSlack;
        }

        public RectTransform Content => _content;

        public bool Attached => _content != null && _viewport != null;

        public int LiveCount => _live.Count;

        public void Attach(ScrollRect scroll, RectTransform viewport, RectTransform content)
        {
            _scroll = scroll;
            _viewport = viewport;
            _content = content;
        }

        public void Sync(ChatLayout layout, Func<int, GameObject> createBlock)
        {
            if (!Attached || layout == null || createBlock == null) return;

            if (layout.Epoch != _liveEpoch)
            {
                Clear();
                _liveEpoch = layout.Epoch;
            }

            float windowTop = _content.anchoredPosition.y - _padding;
            float from = windowTop - _overscan;
            float to = windowTop + _viewport.rect.height + _overscan;
            layout.GetVisibleRange(from, to, out int first, out int end);

            _evictScratch.Clear();
            foreach (KeyValuePair<int, GameObject> live in _live)
            {
                if (live.Key < first || live.Key >= end) _evictScratch.Add(live.Key);
            }
            for (int i = 0; i < _evictScratch.Count; i++)
            {
                int index = _evictScratch[i];
                if (_live[index] != null) UnityEngine.Object.Destroy(_live[index]);
                _live.Remove(index);
            }

            for (int i = first; i < end; i++)
            {
                if (_live.ContainsKey(i)) continue;
                GameObject go = createBlock(i);
                if (go != null) _live[i] = go;
            }
        }

        public void Clear()
        {
            foreach (KeyValuePair<int, GameObject> live in _live)
            {
                if (live.Value != null) UnityEngine.Object.Destroy(live.Value);
            }
            _live.Clear();
        }

        public void SetContentHeight(float height)
        {
            if (_content != null) _content.sizeDelta = new Vector2(0f, height);
        }

        public float WindowTop => _content != null ? _content.anchoredPosition.y - _padding : 0f;

        public void ScrollToContentTop(float contentTop)
        {
            if (_scroll == null || _content == null || _viewport == null) return;
            Canvas.ForceUpdateCanvases();
            float scrollable = Mathf.Max(0f, _content.rect.height - _viewport.rect.height);
            _content.anchoredPosition = new Vector2(
                _content.anchoredPosition.x,
                Mathf.Clamp(contentTop + _padding, 0f, scrollable));
        }

        public bool IsAtBottom()
        {
            if (_scroll == null || _content == null || _viewport == null) return true;
            float scrollable = _content.rect.height - _viewport.rect.height;
            if (scrollable <= 0f) return true;
            return _content.anchoredPosition.y >= scrollable - _bottomSlack;
        }

        public void ScrollToBottom()
        {
            if (_scroll == null) return;
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }

        public void ResetScrollPosition()
        {
            if (_scroll != null) _scroll.verticalNormalizedPosition = 0f;
        }

        public void Detach()
        {
            _scroll = null;
            _viewport = null;
            _content = null;
        }
    }
}
