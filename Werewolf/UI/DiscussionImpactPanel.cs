using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Werewolf.Core;

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

        private GameObject _root;
        private Word _left;
        private Word _right;
        private ImpactJitterUnit _unit;
        private float _elapsedSec;
        private bool _playing;
        private bool _impactFired;
        private Action _onImpact;

        private sealed class Word
        {
            public RectTransform Root;

            public readonly List<Unit> Units = new List<Unit>();

            public float WidthPx;

            public float Dir;
        }

        private sealed class Unit
        {
            public TextMeshProUGUI Main;
            public TextMeshProUGUI HaloInner;
            public TextMeshProUGUI HaloOuter;
            public float RestX;
        }

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_DiscussionImpact", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _root = go;
            _root.SetActive(false);
        }

        public void Show(string left, string right, Action onImpact)
        {
            if (_root == null) return;
            try
            {
                _root.transform.parent.SetAsLastSibling();

                if (!_root.activeSelf) _root.SetActive(true);

                left ??= string.Empty;
                right ??= string.Empty;
                _unit = DiscussionImpact.ResolveUnit(left, right);

                DestroyWords();
                _left = BuildWord("Left", left, -1f, LeftFaceColor, LeftHaloColor, LeftStrokeColor);
                _right = BuildWord("Right", right, +1f, RightFaceColor, RightHaloColor, RightStrokeColor);

                _onImpact = onImpact;
                _impactFired = false;
                _elapsedSec = 0f;
                _playing = true;
                Apply();

                WLog.Line("discussion_impact_shown", secret: false,
                    ("unit", _unit.ToString()), ("units", _left.Units.Count + _right.Units.Count));
            }
            catch (Exception e)
            {
                WLog.Line("discussion_impact_show_error", secret: false, ("err", e.Message));
                Hide();
            }
        }

        public void Tick()
        {
            if (!_playing || _root == null) return;
            try
            {
                float prev = _elapsedSec;
                _elapsedSec += Time.unscaledDeltaTime;

                if (!_impactFired && prev < DiscussionImpact.ImpactSec
                    && _elapsedSec >= DiscussionImpact.ImpactSec)
                {
                    _impactFired = true;
                    _onImpact?.Invoke();
                }

                if (_elapsedSec >= DiscussionImpact.TotalSec)
                {
                    Hide();
                    return;
                }
                Apply();
            }
            catch (Exception e)
            {
                WLog.Line("discussion_impact_tick_error", secret: false, ("err", e.Message));
                Hide();
            }
        }

        public void Hide()
        {
            _playing = false;
            _impactFired = false;
            _elapsedSec = 0f;
            _onImpact = null;
            if (_root != null && _root.activeSelf) _root.SetActive(false);
        }

        public void Destroy()
        {
            _playing = false;
            _onImpact = null;
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _left = null;
            _right = null;
        }

        private void DestroyWords()
        {
            DestroyWord(_left);
            DestroyWord(_right);
            _left = null;
            _right = null;
        }

        private static void DestroyWord(Word word)
        {
            if (word?.Root != null) UnityEngine.Object.Destroy(word.Root.gameObject);
        }

        private Word BuildWord(string name, string text, float dir,
            Color faceColor, Color haloColor, Color strokeColor)
        {
            var word = new Word { Dir = dir };

            var go = new GameObject("WW_Impact" + name, typeof(RectTransform));
            word.Root = (RectTransform)go.transform;
            word.Root.SetParent(_root.transform, false);
            word.Root.anchorMin = word.Root.anchorMax = new Vector2(0.5f, 0.5f);
            word.Root.pivot = new Vector2(0.5f, 0.5f);
            word.Root.sizeDelta = Vector2.zero;

            string[] pieces = SplitUnits(text);
            var widths = new float[pieces.Length];
            float total = 0f;
            for (int i = 0; i < pieces.Length; i++)
            {
                Unit unit = CreateUnit(word.Root, name + i, pieces[i], faceColor, haloColor, strokeColor);
                word.Units.Add(unit);
                widths[i] = Mathf.Max(0f, unit.Main.GetPreferredValues(pieces[i]).x);
                total += widths[i];
            }
            word.WidthPx = total;

            float cursor = -total * 0.5f;
            for (int i = 0; i < word.Units.Count; i++)
            {
                word.Units[i].RestX = cursor + widths[i] * 0.5f;
                cursor += widths[i];
            }
            return word;
        }

        private string[] SplitUnits(string text)
        {
            if (_unit == ImpactJitterUnit.Word) return new[] { text };

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
            return new Unit
            {
                HaloOuter = CreateLayer(parent, name + "_HaloOuter", text, haloColor,
                    haloColor, DiscussionImpact.HaloOuterWidthPx),
                HaloInner = CreateLayer(parent, name + "_HaloInner", text, haloColor,
                    haloColor, DiscussionImpact.HaloInnerWidthPx),
                Main = CreateLayer(parent, name + "_Main", text, faceColor,
                    strokeColor, DiscussionImpact.StrokeWidthPx),
            };
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

        private void Apply()
        {
            ImpactState state = DiscussionImpact.Compute(_elapsedSec);
            ApplyWord(_left, state);
            ApplyWord(_right, state);
        }

        private void ApplyWord(Word word, ImpactState state)
        {
            if (word?.Root == null) return;
            if (!state.Visible)
            {
                if (word.Root.gameObject.activeSelf) word.Root.gameObject.SetActive(false);
                return;
            }
            if (!word.Root.gameObject.activeSelf) word.Root.gameObject.SetActive(true);

            bool perWord = _unit == ImpactJitterUnit.Word;

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
