using System;
using System.Collections;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.UI
{
    public static class UiTween
    {

        public static AnimationCurve Linear() => AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public static AnimationCurve EaseInOut() => AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public static AnimationCurve EaseOut()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, EaseOutValue(0.25f)),
                new Keyframe(0.5f, EaseOutValue(0.5f)),
                new Keyframe(0.75f, EaseOutValue(0.75f)),
                new Keyframe(1f, 1f));
            SmoothAll(curve);
            return curve;
        }

        public static AnimationCurve EaseIn()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.25f * 0.25f),
                new Keyframe(0.5f, 0.5f * 0.5f),
                new Keyframe(0.75f, 0.75f * 0.75f),
                new Keyframe(1f, 1f));
            SmoothAll(curve);
            return curve;
        }

        private static float EaseOutValue(float t) => 1f - (1f - t) * (1f - t);

        private static void SmoothAll(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        }

        public static IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float duration, AnimationCurve curve = null)
        {
            if (target == null)
            {
                WLog.Line("uitween_skip", secret: false, ("op", "scale"), ("reason", "null_target"));
                yield break;
            }
            curve ??= Linear();
            if (duration <= 0f)
            {
                target.localScale = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null)
                {
                    WLog.Line("uitween_skip", secret: false, ("op", "scale"), ("reason", "destroyed"));
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                target.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }
            if (target != null) target.localScale = to;
        }

        public static IEnumerator Scale(Transform target, float from, float to, float duration, AnimationCurve curve = null)
            => Scale(target, Vector3.one * from, Vector3.one * to, duration, curve);

        public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration, AnimationCurve curve = null)
        {
            if (group == null)
            {
                WLog.Line("uitween_skip", secret: false, ("op", "fade"), ("reason", "null_group"));
                yield break;
            }
            curve ??= Linear();
            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (group == null)
                {
                    WLog.Line("uitween_skip", secret: false, ("op", "fade"), ("reason", "destroyed"));
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                group.alpha = Mathf.LerpUnclamped(from, to, t);
                yield return null;
            }
            if (group != null) group.alpha = to;
        }

        public static IEnumerator Move(RectTransform target, Vector2 from, Vector2 to, float duration, AnimationCurve curve = null)
        {
            if (target == null)
            {
                WLog.Line("uitween_skip", secret: false, ("op", "move"), ("reason", "null_target"));
                yield break;
            }
            curve ??= Linear();
            if (duration <= 0f)
            {
                target.anchoredPosition = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null)
                {
                    WLog.Line("uitween_skip", secret: false, ("op", "move"), ("reason", "destroyed"));
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                yield return null;
            }
            if (target != null) target.anchoredPosition = to;
        }

        public static IEnumerator Hold(float duration)
        {
            if (duration <= 0f) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public static IEnumerator Sequence(params IEnumerator[] steps)
        {
            if (steps == null) yield break;
            foreach (IEnumerator step in steps)
            {
                if (step == null) continue;
                while (true)
                {
                    bool moved;
                    try
                    {
                        moved = step.MoveNext();
                    }
                    catch (Exception e)
                    {
                        WLog.Line("uitween_tick_error", secret: false, ("op", "sequence"), ("err", e.Message));
                        moved = false;
                    }
                    if (!moved) break;
                    yield return step.Current;
                }
            }
        }

        public static IEnumerator Parallel(params IEnumerator[] steps)
        {
            if (steps == null || steps.Length == 0) yield break;

            bool[] finished = new bool[steps.Length];
            int remaining = 0;
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] == null) finished[i] = true;
                else remaining++;
            }

            while (remaining > 0)
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    if (finished[i]) continue;

                    bool moved;
                    try
                    {
                        moved = steps[i].MoveNext();
                    }
                    catch (Exception e)
                    {
                        WLog.Line("uitween_tick_error", secret: false, ("op", "parallel"), ("index", i), ("err", e.Message));
                        moved = false;
                    }

                    if (!moved)
                    {
                        finished[i] = true;
                        remaining--;
                    }
                }
                if (remaining > 0) yield return null;
            }
        }
    }
}
