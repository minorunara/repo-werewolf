#nullable enable
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class DiscussionImpactPanel : IClientPanel
    {
        public string LayerName => "DiscussionImpact";

        private static readonly Vector2 UnitSize = new Vector2(1600f, 320f);

        private const float PxToTmpOutline = 0.03f * 100f / DiscussionImpact.FontSizePx;

        private static readonly Color LeftFaceColor = Color.white;
        private static readonly Color LeftHaloColor = new Color(1f, 0.275f, 0.157f, 1f);
        private static readonly Color LeftStrokeColor = new Color(0.478f, 0.102f, 0.047f, 1f);
        private static readonly Color RightFaceColor = new Color(1f, 0.706f, 0.157f, 1f);
        private static readonly Color RightHaloColor = new Color(1f, 0.510f, 0.118f, 1f);
        private static readonly Color RightStrokeColor = new Color(0.478f, 0.227f, 0.020f, 1f);

        private ViewRefs? _view;

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root)
            {
                Root = root;
            }

            public GameObject Root { get; }
            public WordPair? Pair { get; set; }
            public ImpactJitterUnit Unit { get; set; }
            public float ElapsedSec { get; set; }
            public bool Playing { get; set; }
            public bool ImpactFired { get; set; }
            public Action? OnImpact { get; set; }
        }

        private sealed class WordPair
        {
            public WordPair(Word left, Word right)
            {
                Left = left;
                Right = right;
            }

            public Word Left { get; }
            public Word Right { get; }
        }

        private sealed class Word
        {
            public Word(RectTransform root, List<Unit> units, float widthPx, float dir)
            {
                Root = root;
                Units = units;
                WidthPx = widthPx;
                Dir = dir;
            }

            public RectTransform Root { get; }

            public List<Unit> Units { get; }

            public float WidthPx { get; }

            public float Dir { get; }
        }

        private sealed class Unit
        {
            public Unit(TextMeshProUGUI main, TextMeshProUGUI haloInner, TextMeshProUGUI haloOuter)
            {
                Main = main;
                HaloInner = haloInner;
                HaloOuter = haloOuter;
            }

            public TextMeshProUGUI Main { get; }
            public TextMeshProUGUI HaloInner { get; }
            public TextMeshProUGUI HaloOuter { get; }
            public float RestX { get; set; }
        }

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                root = new GameObject("WW_DiscussionImpact", typeof(RectTransform));
                var rect = (RectTransform)root.transform;
                rect.SetParent(layerRoot, false);
                UiKit.Stretch(rect);
                root.SetActive(false);
                _view = new ViewRefs(root);
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

        public void Show(string? left, string? right, Action? onImpact)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            Word? newLeft = null;
            Word? newRight = null;
            try
            {
                DestroyWords(view);
                ResetPlayback(view, hideRoot: false);

                view.Root.transform.parent.SetAsLastSibling();

                if (!view.Root.activeSelf) view.Root.SetActive(true);

                left ??= string.Empty;
                right ??= string.Empty;
                ImpactJitterUnit unit = DiscussionImpact.ResolveUnit(left, right);

                newLeft = BuildWord(view.Root.transform, "Left", left, -1f,
                    LeftFaceColor, LeftHaloColor, LeftStrokeColor, unit);
                newRight = BuildWord(view.Root.transform, "Right", right, +1f,
                    RightFaceColor, RightHaloColor, RightStrokeColor, unit);
                var pair = new WordPair(newLeft, newRight);
                view.Pair = pair;
                newLeft = null;
                newRight = null;

                view.Unit = unit;
                view.OnImpact = onImpact;
                view.ImpactFired = false;
                view.ElapsedSec = 0f;
                view.Playing = true;
                Apply(view);

                WLog.Line("discussion_impact_shown", secret: false,
                    ("unit", unit.ToString()), ("units", pair.Left.Units.Count + pair.Right.Units.Count));
            }
            catch (Exception e)
            {
                DestroyWord(newLeft);
                DestroyWord(newRight);
                DestroyWords(view);
                ResetPlayback(view, hideRoot: true);
                WLog.Line("discussion_impact_show_error", secret: false, ("err", e.Message));
            }
        }

        public void Tick()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || !view.Playing) return;
            try
            {
                float prev = view.ElapsedSec;
                view.ElapsedSec += Time.unscaledDeltaTime;

                if (!view.ImpactFired && prev < DiscussionImpact.ImpactSec
                    && view.ElapsedSec >= DiscussionImpact.ImpactSec)
                {
                    view.ImpactFired = true;
                    Action? onImpact = view.OnImpact;
                    onImpact?.Invoke();
                    if (!IsCurrent(view) || !view.Playing) return;
                }

                if (view.ElapsedSec >= DiscussionImpact.TotalSec)
                {
                    ResetPlayback(view, hideRoot: true);
                    return;
                }
                Apply(view);
            }
            catch (Exception e)
            {
                WLog.Line("discussion_impact_tick_error", secret: false, ("err", e.Message));
                ResetPlayback(view, hideRoot: true);
            }
        }

        public void Hide()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) ResetPlayback(view, hideRoot: true);
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

        private static void ResetPlayback(ViewRefs view, bool hideRoot)
        {
            view.Playing = false;
            view.ImpactFired = false;
            view.ElapsedSec = 0f;
            view.OnImpact = null;
            if (hideRoot && view.Root != null && view.Root.activeSelf) view.Root.SetActive(false);
        }

        private static void Invalidate(ViewRefs view)
        {
            ResetPlayback(view, hideRoot: false);
            view.Pair = null;
        }

        private static void DestroyWords(ViewRefs view)
        {
            WordPair? pair = view.Pair;
            view.Pair = null;
            if (pair == null) return;

            DestroyWord(pair.Left);
            DestroyWord(pair.Right);
        }

        private static void DestroyWord(Word? word)
        {
            if (word != null && word.Root != null) UnityEngine.Object.Destroy(word.Root.gameObject);
        }

        private static Word BuildWord(Transform parent, string name, string text, float dir,
            Color faceColor, Color haloColor, Color strokeColor, ImpactJitterUnit jitterUnit)
        {
            GameObject? rootObject = null;
            try
            {
                rootObject = new GameObject("WW_Impact" + name, typeof(RectTransform));
                var root = (RectTransform)rootObject.transform;
                root.SetParent(parent, false);
                root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = Vector2.zero;

                string[] pieces = SplitUnits(text, jitterUnit);
                var units = new List<Unit>(pieces.Length);
                var widths = new float[pieces.Length];
                float total = 0f;
                for (int i = 0; i < pieces.Length; i++)
                {
                    Unit unit = CreateUnit(root, name + i, pieces[i], faceColor, haloColor, strokeColor);
                    units.Add(unit);
                    widths[i] = Mathf.Max(0f, unit.Main.GetPreferredValues(pieces[i]).x);
                    total += widths[i];
                }

                float cursor = -total * 0.5f;
                for (int i = 0; i < units.Count; i++)
                {
                    units[i].RestX = cursor + widths[i] * 0.5f;
                    cursor += widths[i];
                }
                return new Word(root, units, total, dir);
            }
            catch
            {
                if (rootObject != null) UnityEngine.Object.Destroy(rootObject);
                throw;
            }
        }

        private static string[] SplitUnits(string text, ImpactJitterUnit jitterUnit)
        {
            if (jitterUnit == ImpactJitterUnit.Word) return new[] { text };

            var pieces = new List<string>(text.Length);
            foreach (char c in text)
            {
                if (!char.IsWhiteSpace(c)) pieces.Add(c.ToString());
            }
            return pieces.Count > 0 ? pieces.ToArray() : new[] { text };
        }

        private static Unit CreateUnit(Transform parent, string name, string text,
            Color faceColor, Color haloColor, Color strokeColor)
        {
            TextMeshProUGUI haloOuter = CreateLayer(parent, name + "_HaloOuter", text, haloColor,
                haloColor, DiscussionImpact.HaloOuterWidthPx);
            TextMeshProUGUI haloInner = CreateLayer(parent, name + "_HaloInner", text, haloColor,
                haloColor, DiscussionImpact.HaloInnerWidthPx);
            TextMeshProUGUI main = CreateLayer(parent, name + "_Main", text, faceColor,
                strokeColor, DiscussionImpact.StrokeWidthPx);
            return new Unit(main, haloInner, haloOuter);
        }

        private static TextMeshProUGUI CreateLayer(Transform parent, string name, string text,
            Color color, Color outlineColor, float outlineWidthPx)
        {
            TextMeshProUGUI label = UiKit.CreateText(parent, name, Vector2.zero, UnitSize,
                text, DiscussionImpact.FontSizePx, color, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Normal;
            if (outlineWidthPx > 0f)
            {
                label.outlineColor = outlineColor;
                label.outlineWidth = outlineWidthPx * PxToTmpOutline;
            }
            return label;
        }

        private static void Apply(ViewRefs view)
        {
            WordPair? pair = view.Pair;
            if (pair == null) return;

            ImpactState state = DiscussionImpact.Compute(view.ElapsedSec);
            ApplyWord(pair.Left, state, view.Unit);
            ApplyWord(pair.Right, state, view.Unit);
        }

        private static void ApplyWord(Word word, ImpactState state, ImpactJitterUnit jitterUnit)
        {
            if (word.Root == null) return;
            if (!state.Visible)
            {
                if (word.Root.gameObject.activeSelf) word.Root.gameObject.SetActive(false);
                return;
            }
            if (!word.Root.gameObject.activeSelf) word.Root.gameObject.SetActive(true);

            bool perWord = jitterUnit == ImpactJitterUnit.Word;

            float x = word.Dir * (DiscussionImpact.GapPx * 0.5f + word.WidthPx * 0.5f
                + state.SlideOffsetX + state.Recoil);

            float y = -word.Dir * state.FlyOffset;
            if (perWord) y += -word.Dir * DiscussionImpact.WordOffsetPx * state.JitterK;

            word.Root.anchoredPosition = new Vector2(x, y);

            float tilt = perWord
                ? -word.Dir * DiscussionImpact.WordTiltDeg * state.JitterK
                : 0f;
            word.Root.localRotation = Quaternion.Euler(0f, 0f, tilt);

            for (int i = 0; i < word.Units.Count; i++)
            {
                Unit unit = word.Units[i];
                float dy = perWord ? 0f : DiscussionImpact.CharOffsetY(i, state.JitterK);
                var pos = new Vector2(unit.RestX, dy);
                ApplyLayer(unit.Main, pos, state.Alpha);
                ApplyLayer(unit.HaloInner, pos, state.Alpha * DiscussionImpact.HaloInnerAlpha);
                ApplyLayer(unit.HaloOuter, pos, state.Alpha * DiscussionImpact.HaloOuterAlpha);
            }
        }

        private static void ApplyLayer(TextMeshProUGUI label, Vector2 pos, float alpha)
        {
            if (label == null) return;
            label.rectTransform.anchoredPosition = pos;
            Color c = label.color;
            c.a = alpha;
            label.color = c;
        }
    }
}
