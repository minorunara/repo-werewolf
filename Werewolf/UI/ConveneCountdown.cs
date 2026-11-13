#nullable enable
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

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

        private ViewRefs? _view;

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, CanvasGroup group, TextMeshProUGUI header,
                TextMeshProUGUI number, CanvasGroup numberGroup)
            {
                Root = root;
                Group = group;
                Header = header;
                Number = number;
                NumberGroup = numberGroup;
            }

            public GameObject Root { get; }
            public CanvasGroup Group { get; }
            public TextMeshProUGUI Header { get; }
            public TextMeshProUGUI Number { get; }
            public CanvasGroup NumberGroup { get; }

            public int CurrentSecond { get; set; } = -1;
            public int TotalSeconds { get; set; }
            public int AnimationGeneration { get; set; }
            public int StandaloneGeneration { get; set; }
        }

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

                root = new GameObject("WW_ConveneCountdown", typeof(RectTransform));
                var rect = (RectTransform)root.transform;
                rect.SetParent(layerRoot, false);
                UiKit.Stretch(rect);

                CanvasGroup group = root.AddComponent<CanvasGroup>();
                group.alpha = 1f;
                group.blocksRaycasts = false;
                group.interactable = false;

                TextMeshProUGUI header = UiKit.CreateText(rect, "Header",
                    new Vector2(0f, HeaderY), HeaderSize,
                    string.Empty, HeaderFontSize,
                    new Color(1f, 0.95f, 0.7f, 1f), TextAlignmentOptions.Center);
                header.textWrappingMode = TextWrappingModes.NoWrap;
                header.raycastTarget = false;

                TextMeshProUGUI number = UiKit.CreateText(rect, "Number",
                    new Vector2(0f, NumberY), NumberSize,
                    string.Empty, NumberFontSize,
                    new Color(1f, 0.98f, 0.85f, 1f), TextAlignmentOptions.Center);
                number.textWrappingMode = TextWrappingModes.NoWrap;
                number.raycastTarget = false;
                CanvasGroup numberGroup = number.gameObject.AddComponent<CanvasGroup>();
                numberGroup.alpha = 0f;
                numberGroup.blocksRaycasts = false;
                numberGroup.interactable = false;

                root.SetActive(false);
                _view = new ViewRefs(root, group, header, number, numberGroup);
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

        public IEnumerator? Tick(string callerLabel, int remainingSeconds,
                                TextId headerFormatId = TextId.ConveneCountdownHeaderFormat)
        {
            ViewRefs? view = GetLiveViewOrNull();
            return view == null ? null : Tick(view, callerLabel, remainingSeconds, headerFormatId);
        }

        private IEnumerator? Tick(ViewRefs view, string callerLabel, int remainingSeconds, TextId headerFormatId)
        {
            if (!IsCurrent(view)) return null;
            if (remainingSeconds < 0) remainingSeconds = 0;

            ApplyHeader(view, callerLabel, headerFormatId);
            view.Group.alpha = 1f;
            if (!view.Root.activeSelf) view.Root.SetActive(true);

            if (remainingSeconds == view.CurrentSecond) return null;

            if (remainingSeconds > view.TotalSeconds) view.TotalSeconds = remainingSeconds;

            int generation = NextAnimation(view);
            view.CurrentSecond = remainingSeconds;
            view.Number.text = remainingSeconds.ToString();
            float baseFactor = ComputeBaseScaleFactor(view);
            view.Number.transform.localScale = Vector3.one * (ScaleEnterFrom * baseFactor);
            view.NumberGroup.alpha = 0f;
            return RunSecondTweenGuarded(view, generation, BuildSecondTween(view, baseFactor));
        }

        private static float ComputeBaseScaleFactor(ViewRefs view)
        {
            if (view.TotalSeconds <= 0) return BaseScaleAtStart;
            float progress = 1f - (float)view.CurrentSecond / view.TotalSeconds;
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;
            return BaseScaleAtStart + progress * (BaseScaleAtEnd - BaseScaleAtStart);
        }

        private static IEnumerator BuildSecondTween(ViewRefs view, float baseFactor)
        {
            Transform number = view.Number.transform;
            float sEnter = ScaleEnterFrom * baseFactor;
            float sHold = ScaleHold * baseFactor;
            float sExit = ScaleExitTo * baseFactor;
            return UiTween.Sequence(
                UiTween.Parallel(
                    UiTween.Scale(number, sEnter, sHold, PhaseEnterSec, UiTween.EaseIn()),
                    UiTween.Fade(view.NumberGroup, 0f, 1f, PhaseEnterSec)),
                UiTween.Hold(PhaseHoldSec),
                UiTween.Parallel(
                    UiTween.Scale(number, sHold, sExit, PhaseExitSec),
                    UiTween.Fade(view.NumberGroup, 1f, 0f, PhaseExitSec)));
        }

        private IEnumerator RunSecondTweenGuarded(ViewRefs view, int generation, IEnumerator routine)
        {
            while (IsCurrentAnimation(view, generation))
            {
                bool moved;
                bool failed = false;
                try
                {
                    moved = routine.MoveNext();
                }
                catch (System.Exception e)
                {
                    WLog.Line("convene_countdown_tween_error", secret: false, ("err", e.Message));
                    if (IsCurrentAnimation(view, generation)) InvalidateAnimation(view);
                    moved = false;
                    failed = true;
                }

                if (failed || !moved || !IsCurrentAnimation(view, generation)) yield break;
                yield return routine.Current;
            }
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) Hide(view);
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view == null) return;

            Invalidate(view);
            if (view.Root != null)
            {
                view.Root.SetActive(false);
                UnityEngine.Object.Destroy(view.Root);
            }
        }

        public IEnumerator PlayStandalone(string callerLabel, int totalSeconds)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null)
            {
                WLog.Line("convene_countdown_skip", secret: false, ("reason", "not_built"));
                yield break;
            }
            if (totalSeconds < 0) totalSeconds = 0;

            int standaloneGeneration = NextStandalone(view);
            InvalidateAnimation(view);
            view.CurrentSecond = -1;
            view.TotalSeconds = 0;

            for (int s = totalSeconds; s >= 0; s--)
            {
                if (!IsCurrentStandalone(view, standaloneGeneration)) yield break;

                IEnumerator? secondTween = Tick(view, callerLabel, s, TextId.ConveneCountdownHeaderFormat);
                if (secondTween != null)
                    yield return RunStandaloneStepGuarded(view, standaloneGeneration, secondTween);
                else yield return null;
            }
            if (IsCurrentStandalone(view, standaloneGeneration)) Hide(view);
        }

        private IEnumerator RunStandaloneStepGuarded(ViewRefs view, int generation, IEnumerator routine)
        {
            while (IsCurrentStandalone(view, generation))
            {
                bool moved;
                bool failed = false;
                try
                {
                    moved = routine.MoveNext();
                }
                catch (System.Exception e)
                {
                    WLog.Line("convene_countdown_standalone_error", secret: false, ("err", e.Message));
                    if (IsCurrentStandalone(view, generation)) InvalidateStandalone(view);
                    moved = false;
                    failed = true;
                }

                if (failed || !moved || !IsCurrentStandalone(view, generation)) yield break;
                yield return routine.Current;
            }
        }

        private static void ApplyHeader(ViewRefs view, string callerLabel, TextId headerFormatId)
        {
            string label = string.IsNullOrEmpty(callerLabel) ? Texts.Get(TextId.ConveneCountdownDefaultCallerName) : callerLabel;
            string next = Texts.Format(headerFormatId, label);
            if (view.Header.text != next) view.Header.text = next;
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null || view.Root != null) return view;

            _view = null;
            Invalidate(view);
            return null;
        }

        private bool IsCurrent(ViewRefs view)
        {
            return ReferenceEquals(_view, view) && view.Root != null;
        }

        private int NextAnimation(ViewRefs view)
        {
            InvalidateAnimation(view);
            return view.AnimationGeneration;
        }

        private int NextStandalone(ViewRefs view)
        {
            InvalidateStandalone(view);
            return view.StandaloneGeneration;
        }

        private static void InvalidateAnimation(ViewRefs view)
        {
            unchecked { view.AnimationGeneration++; }
        }

        private static void InvalidateStandalone(ViewRefs view)
        {
            unchecked { view.StandaloneGeneration++; }
        }

        private bool IsCurrentAnimation(ViewRefs view, int generation)
        {
            return IsCurrent(view) && view.AnimationGeneration == generation;
        }

        private bool IsCurrentStandalone(ViewRefs view, int generation)
        {
            return IsCurrent(view) && view.StandaloneGeneration == generation;
        }

        private static void ResetDisplay(ViewRefs view)
        {
            view.CurrentSecond = -1;
            view.TotalSeconds = 0;
            if (view.Number != null) view.Number.text = string.Empty;
            if (view.NumberGroup != null) view.NumberGroup.alpha = 0f;
            if (view.Header != null) view.Header.text = string.Empty;
        }

        private static void Invalidate(ViewRefs view)
        {
            InvalidateAnimation(view);
            InvalidateStandalone(view);
            view.CurrentSecond = -1;
            view.TotalSeconds = 0;
        }

        private static void Hide(ViewRefs view)
        {
            Invalidate(view);
            ResetDisplay(view);
            if (view.Root != null && view.Root.activeSelf) view.Root.SetActive(false);
        }
    }
}
