using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal sealed class ChatSpeakerFilter
    {
        private static readonly Color RowHoverColor = new Color(1f, 0.9f, 0.6f, 0.16f);
        private static readonly Color TitleHoverColor = new Color(1f, 0.97f, 0.85f, 1f);

        private const float TitleHitPadding = 8f;

        private struct Target
        {
            public RectTransform Rect;
            public int Actor;
        }

        private TextMeshProUGUI _title;
        private RectTransform _titleRect;
        private RectTransform _viewport;
        private string _normalTitle;
        private Color _titleBaseColor;

        private readonly List<Target> _targets = new List<Target>();
        private Image _hoverPlate;

        private int _actor;
        private bool _active;

        public Func<ChatLogEntry, bool> Predicate { get; private set; }

        public bool IsActive => _active;

        public void Attach(TextMeshProUGUI title, RectTransform viewport)
        {
            _title = title;
            _titleRect = title != null ? title.rectTransform : null;
            _viewport = viewport;
            _normalTitle = title != null ? title.text : string.Empty;
            _titleBaseColor = title != null ? title.color : Color.white;
            if (title != null) title.spriteAsset = EmojiSprites.Asset;
        }

        public void RegisterTarget(RectTransform rect, int actor)
        {
            if (rect == null) return;
            _targets.Add(new Target { Rect = rect, Actor = actor });
        }

        public void ClearTargets()
        {
            _targets.Clear();
        }

        public bool Tick()
        {
            if (_title == null) return false;

            Vector2 mouse = Input.mousePosition;

            RectTransform hoverRect = null;
            int hoverActor = 0;
            bool inViewport = _viewport != null
                && RectTransformUtility.RectangleContainsScreenPoint(_viewport, mouse, null);
            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                Target target = _targets[i];
                if (target.Rect == null)
                {
                    _targets.RemoveAt(i);
                    continue;
                }
                if (!inViewport || hoverRect != null) continue;
                if (!target.Rect.gameObject.activeInHierarchy) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(target.Rect, mouse, null)) continue;
                hoverRect = target.Rect;
                hoverActor = target.Actor;
            }

            ApplyRowHover(hoverRect);

            bool hoverTitle = IsOverTitle(mouse);
            _title.color = hoverTitle ? TitleHoverColor : _titleBaseColor;

            if (!Input.GetMouseButtonDown(0)) return false;
            if (hoverTitle)
            {
                Deactivate();
                return true;
            }
            if (hoverRect != null)
            {
                if (_active && hoverActor == _actor) Deactivate();
                else Activate(hoverActor);
                return true;
            }
            return false;
        }

        private bool IsOverTitle(Vector2 mouse)
        {
            if (!_active || _title == null || _titleRect == null) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_titleRect, mouse, null,
                    out Vector2 local))
            {
                return false;
            }
            float halfWidth = Mathf.Min(_title.preferredWidth, _titleRect.rect.width) * 0.5f
                + TitleHitPadding;
            float halfHeight = _titleRect.rect.height * 0.5f;
            return Mathf.Abs(local.x) <= halfWidth && Mathf.Abs(local.y) <= halfHeight;
        }

        public void Deactivate()
        {
            _active = false;
            Predicate = null;
            ApplyTitle();
        }

        private void Activate(int actor)
        {
            _active = true;
            _actor = actor;
            Predicate = entry => ChatFilter.Allows(entry, actor);
            ApplyTitle();
            WLog.Line("chat_filter_on", secret: false, ("actor", actor));
        }

        private void ApplyTitle()
        {
            if (_title == null) return;
            if (!_active)
            {
                _title.text = _normalTitle;
                _title.color = _titleBaseColor;
                return;
            }
            string label = Texts.Get(TextId.ChatLogFilteringTitle);
            _title.text = EmojiSprites.Ready ? EmojiSprites.Substitute(label + " ❌") : label;
        }

        private void ApplyRowHover(RectTransform hoverRect)
        {
            if (hoverRect == null)
            {
                if (_hoverPlate != null) _hoverPlate.gameObject.SetActive(false);
                return;
            }
            if (_hoverPlate == null)
            {
                _hoverPlate = UiKit.CreateRoundedImage(hoverRect, "FilterHover",
                    Vector2.zero, Vector2.zero, RowHoverColor);
                _hoverPlate.raycastTarget = false;
            }
            RectTransform plateRect = _hoverPlate.rectTransform;
            if (plateRect.parent != hoverRect)
            {
                plateRect.SetParent(hoverRect, false);
            }
            UiKit.Stretch(plateRect);
            plateRect.SetAsFirstSibling();
            _hoverPlate.gameObject.SetActive(true);
        }

        public void Clear()
        {
            _title = null;
            _titleRect = null;
            _viewport = null;
            _normalTitle = string.Empty;
            _targets.Clear();
            _hoverPlate = null;
            _active = false;
            _actor = 0;
            Predicate = null;
        }
    }
}
