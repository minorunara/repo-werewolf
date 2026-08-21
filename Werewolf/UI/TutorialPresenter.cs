using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class TutorialPresenter
    {
        private const float DisplaySeconds = 15f;

        private const float TextMaxWidth = 400f;
        private const float TextMinHeight = 42f;

        private const float FontRestoreDelaySeconds = 2f;

        public float FontScale { get; set; } = 1f;

        public TutorialBubblePanel Bubble { get; set; }

        private readonly List<(TutorialId Id, string Message)> _queue = new List<(TutorialId, string)>();
        private float _remaining;
        private float _fontRestoreRemaining;
        private float _originalFontSizeMax = -1f;
        private TutorialUI _elevatedUi;
        private bool _onBubble;
        private string _currentMessage;

        public void Enqueue(TutorialId id, string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _queue.Add((id, message));
        }

        public bool IsPending(TutorialId id)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].Id == id) return true;
            }
            return false;
        }

        public TutorialId? Tick(bool meetingUiVisible)
        {
            TutorialUI ui = TutorialUI.instance;
            TutorialBubblePanel bubble = Bubble;
            bool bubbleReady = bubble != null && bubble.Exists;
            bool bubbleSkip = bubbleReady && bubble.Tick();

            if (_remaining > 0f)
            {
                if (_onBubble)
                {
                    if (!bubbleReady || !bubble.Visible)
                    {
                        EndDisplay();
                        return null;
                    }
                    if (bubbleSkip)
                    {
                        EndDisplay();
                        return null;
                    }
                }
                else
                {
                    if (ui == null)
                    {
                        _remaining = 0f;
                        _fontRestoreRemaining = 0f;
                        _currentMessage = null;
                        return null;
                    }
                    if (meetingUiVisible && bubbleReady && _currentMessage != null)
                    {
                        bubble.ShowMessage(_currentMessage, FontScale);
                        _onBubble = true;
                        _fontRestoreRemaining = FontRestoreDelaySeconds;
                    }
                    else
                    {
                        ui.Show();
                    }
                }
                _remaining -= Time.deltaTime;
                if (_remaining <= 0f) EndDisplay();
                return null;
            }

            if (_queue.Count > 0)
            {
                bool useBubble = meetingUiVisible && bubbleReady;
                if (useBubble && !bubble.Idle) return null;
                if (!useBubble && ui == null) return null;
                (TutorialId id, string message) = _queue[0];
                _queue.RemoveAt(0);
                _currentMessage = message;
                _onBubble = useBubble;
                if (useBubble) bubble.ShowMessage(message, FontScale);
                else Display(ui, message);
                _remaining = DisplaySeconds;
                return id;
            }

            if (_fontRestoreRemaining > 0f)
            {
                _fontRestoreRemaining -= Time.deltaTime;
                if (_fontRestoreRemaining <= 0f && ui != null && _originalFontSizeMax > 0f)
                {
                    ui.Text.fontSizeMax = _originalFontSizeMax;
                }
            }
            return null;
        }

        public void Cancel()
        {
            _queue.Clear();
            if (_remaining > 0f) EndDisplay();
        }

        private void EndDisplay()
        {
            _remaining = 0f;
            if (_onBubble) Bubble?.Hide();
            else _fontRestoreRemaining = FontRestoreDelaySeconds;
            _onBubble = false;
            _currentMessage = null;
        }

        private void Display(TutorialUI ui, string message)
        {
            EnsureOnTop(ui);

            TextMeshProUGUI text = ui.Text;
            if (_originalFontSizeMax < 0f) _originalFontSizeMax = text.fontSizeMax;
            text.fontSizeMax = _originalFontSizeMax * Mathf.Clamp(FontScale, 0.1f, 2f);
            _fontRestoreRemaining = 0f;

            text.text = message;
            Vector2 size = SemiFunc.PreferredAutoscaledTextSize(text, TextMaxWidth, float.PositiveInfinity, 5);
            ui.mainTextContainer.sizeDelta = new Vector2(ui.mainTextContainer.sizeDelta.x, Mathf.Max(size.y, TextMinHeight));
            ui.SemiUISpringShakeY(20f, 10f, 0.3f);
            ui.SemiUISpringScale(0.4f, 5f, 0.2f);
        }

        private void EnsureOnTop(TutorialUI ui)
        {
            if (ui == null || _elevatedUi == ui) return;
            GameObject go = ui.gameObject;
            Canvas nested = go.GetComponent<Canvas>();
            if (nested == null) nested = go.AddComponent<Canvas>();
            nested.overrideSorting = true;
            nested.sortingOrder = WerewolfUIManager.TutorialSortingOrder;
            _elevatedUi = ui;
        }
    }
}
