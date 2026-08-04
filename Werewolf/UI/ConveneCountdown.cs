using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class ConveneCountdown : IClientPanel
    {
        public string LayerName => "ConveneCountdown";

        private const float PhaseEnterSec = 0.18f;
        private const float PhaseHoldSec = 0.14f;
        private const float PhaseExitSec = 0.68f;

        private const float ScaleEnterFrom = 0.35f;
        private const float ScaleHold = 1.35f;
        private const float ScaleExitTo = 2.4f;

        private const float BaseScaleAtStart = 0.7f;
        private const float BaseScaleAtEnd = 3.0f;

        private const float HeaderY = 140f;
        private const float NumberY = -40f;
        private const float HeaderFontSize = 44f;
        private const float NumberFontSize = 220f;

        private static readonly Vector2 HeaderSize = new Vector2(1400f, 240f);
        private static readonly Vector2 NumberSize = new Vector2(600f, 400f);

        private GameObject _root;
        private CanvasGroup _group;
        private TextMeshProUGUI _headerText;
        private TextMeshProUGUI _numberText;
        private CanvasGroup _numberGroup;

        private int _currentSecond = -1;

        private int _totalSeconds;

        public bool Exists => _root != null;

        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_ConveneCountdown", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _root = go;

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _headerText = UiKit.CreateText(rect, "Header",
                new Vector2(0f, HeaderY), HeaderSize,
                string.Empty, HeaderFontSize,
                new Color(1f, 0.95f, 0.7f, 1f), TextAlignmentOptions.Center);
            _headerText.enableWordWrapping = false;
            _headerText.raycastTarget = false;

            _numberText = UiKit.CreateText(rect, "Number",
                new Vector2(0f, NumberY), NumberSize,
                string.Empty, NumberFontSize,
                new Color(1f, 0.98f, 0.85f, 1f), TextAlignmentOptions.Center);
            _numberText.enableWordWrapping = false;
            _numberText.raycastTarget = false;
            _numberGroup = _numberText.gameObject.AddComponent<CanvasGroup>();
            _numberGroup.alpha = 0f;
            _numberGroup.blocksRaycasts = false;
            _numberGroup.interactable = false;

            _root.SetActive(false);
            WLog.Line("convene_countdown_built", secret: false);
        }

        public IEnumerator Tick(string callerName, int remainingSeconds,
                                TextId headerFormatId = TextId.ConveneCountdownHeaderFormat)
        {
            if (_root == null) return null;
            if (remainingSeconds < 0) remainingSeconds = 0;

            ApplyHeader(callerName, headerFormatId);
            if (_group != null) _group.alpha = 1f;
            if (!_root.activeSelf) _root.SetActive(true);

            if (remainingSeconds == _currentSecond) return null;

            if (remainingSeconds > _totalSeconds) _totalSeconds = remainingSeconds;

            _currentSecond = remainingSeconds;
            _numberText.text = remainingSeconds.ToString();
            float baseFactor = ComputeBaseScaleFactor();
            _numberText.transform.localScale = Vector3.one * (ScaleEnterFrom * baseFactor);
            _numberGroup.alpha = 0f;
            return BuildSecondTween(baseFactor);
        }

        private float ComputeBaseScaleFactor()
        {
            if (_totalSeconds <= 0) return BaseScaleAtStart;
            float progress = 1f - (float)_currentSecond / _totalSeconds;
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;
            return BaseScaleAtStart + progress * (BaseScaleAtEnd - BaseScaleAtStart);
        }

        private IEnumerator BuildSecondTween(float baseFactor)
        {
            Transform number = _numberText.transform;
            float sEnter = ScaleEnterFrom * baseFactor;
            float sHold = ScaleHold * baseFactor;
            float sExit = ScaleExitTo * baseFactor;
            return UiTween.Sequence(
                UiTween.Parallel(
                    UiTween.Scale(number, sEnter, sHold, PhaseEnterSec, UiTween.EaseIn()),
                    UiTween.Fade(_numberGroup, 0f, 1f, PhaseEnterSec)),
                UiTween.Hold(PhaseHoldSec),
                UiTween.Parallel(
                    UiTween.Scale(number, sHold, sExit, PhaseExitSec),
                    UiTween.Fade(_numberGroup, 1f, 0f, PhaseExitSec)));
        }

        public void Hide()
        {
            _currentSecond = -1;
            _totalSeconds = 0;
            if (_numberText != null) _numberText.text = string.Empty;
            if (_numberGroup != null) _numberGroup.alpha = 0f;
            if (_headerText != null) _headerText.text = string.Empty;
            if (_root != null && _root.activeSelf) _root.SetActive(false);
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _group = null;
            _headerText = null;
            _numberText = null;
            _numberGroup = null;
            _currentSecond = -1;
            _totalSeconds = 0;
        }

        public IEnumerator PlayStandalone(string callerName, int totalSeconds)
        {
            if (_root == null)
            {
                WLog.Line("convene_countdown_skip", secret: false, ("reason", "not_built"));
                yield break;
            }
            if (totalSeconds < 0) totalSeconds = 0;

            _currentSecond = -1;
            _totalSeconds = 0;

            for (int s = totalSeconds; s >= 0; s--)
            {
                IEnumerator secondTween = Tick(callerName, s);
                if (secondTween != null) yield return secondTween;
                else yield return null;
            }
            Hide();
        }

        private void ApplyHeader(string callerName, TextId headerFormatId)
        {
            if (_headerText == null) return;
            string name = string.IsNullOrEmpty(callerName) ? Texts.Get(TextId.ConveneCountdownDefaultCallerName) : callerName;
            string next = Texts.Format(headerFormatId, name);
            if (_headerText.text != next) _headerText.text = next;
        }
    }
}
