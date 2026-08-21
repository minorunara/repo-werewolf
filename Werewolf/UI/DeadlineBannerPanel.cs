using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class DeadlineBannerPanel : IClientPanel
    {
        public string LayerName => "Hud";

        private const float MeasureFontSize = 100f;

        private const float FallbackFontSize = 90f;

        private static readonly Vector2 LabelSize = new Vector2(2600f, 200f);

        private static readonly Color OutlineColor = new Color(0.10f, 0.05f, 0.04f, 1f);

        private const float OutlineTintTowardHalo = 0.95f;

        private const float BenchToTmpOutline = 0.03f;
        private const float OutlineWidth = 2.5f * BenchToTmpOutline;

        private const float HaloInnerAlpha = 0.55f;
        private const float HaloInnerOutlineWidth = 9f * BenchToTmpOutline;
        private const float HaloOuterAlpha = 0.25f;
        private const float HaloOuterOutlineWidth = 15f * BenchToTmpOutline;

        private static readonly Color Line1FaceColor = Color.white;
        private static readonly Color Line1GhostColor = new Color(1f, 0.541f, 0.439f, 1f);
        private static readonly Color Line1HaloColor = new Color(1f, 0.275f, 0.157f, 1f);
        private static readonly Color Line2FaceColor = new Color(1f, 0.706f, 0.157f, 1f);
        private static readonly Color Line2GhostColor = new Color(1f, 0.816f, 0.439f, 1f);
        private static readonly Color Line2HaloColor = new Color(1f, 0.510f, 0.078f, 1f);

        private GameObject _root;
        private Image[] _emojis;
        private Row _row1;
        private Row _row2;
        private float _elapsedSec;
        private bool _playing;

        private sealed class Row
        {
            public TextMeshProUGUI Main;
            public TextMeshProUGUI SlideGhost;
            public TextMeshProUGUI PopGhost;
            public TextMeshProUGUI HaloInner;
            public TextMeshProUGUI HaloOuter;
            public float StartOffsetX;
            public float CenterY;
            public float TargetWidthPx;
            public bool FromLeft;
        }

        public bool Exists => _root != null;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_DeadlineBanner", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            UiKit.Stretch(rect);
            _root = go;

            BuildEmojis(rect);

            _row1 = BuildRow(rect, "Line1", DeadlineBanner.RowOffsetY,
                DeadlineBanner.Line1WidthPx, fromLeft: true,
                faceColor: Line1FaceColor, ghostColor: Line1GhostColor, haloColor: Line1HaloColor, italic: false);
            _row2 = BuildRow(rect, "Line2", -DeadlineBanner.RowOffsetY,
                DeadlineBanner.Line2WidthPx, fromLeft: false,
                faceColor: Line2FaceColor, ghostColor: Line2GhostColor, haloColor: Line2HaloColor, italic: true);

            _root.SetActive(false);
        }

        private void BuildEmojis(RectTransform parent)
        {
            _emojis = new Image[DeadlineBanner.EmojiCount];
            Sprite sprite = AssetCatalog.GetSprite("emoji_zany_face");
            if (sprite == null)
            {
                WLog.Line("deadline_banner_emoji_missing", secret: false);
                return;
            }
            for (int i = 0; i < _emojis.Length; i++)
            {
                var go = new GameObject("Emoji" + i, typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                rect.sizeDelta = new Vector2(DeadlineBanner.EmojiSizePx, DeadlineBanner.EmojiSizePx);
                var img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.raycastTarget = false;
                img.enabled = false;
                _emojis[i] = img;
            }
        }

        private Row BuildRow(RectTransform parent, string name, float centerY, float targetWidthPx,
            bool fromLeft, Color faceColor, Color ghostColor, Color haloColor, bool italic)
        {
            var row = new Row
            {
                CenterY = centerY,
                TargetWidthPx = targetWidthPx,
                FromLeft = fromLeft,
            };
            var pos = new Vector2(0f, centerY);
            FontStyles style = italic ? (FontStyles.Bold | FontStyles.Italic) : FontStyles.Bold;

            row.SlideGhost = CreateLayer(parent, name + "_SlideGhost", pos, ghostColor, style);
            row.HaloOuter = CreateLayer(parent, name + "_HaloOuter", pos, haloColor, style,
                haloColor, HaloOuterOutlineWidth);
            row.HaloInner = CreateLayer(parent, name + "_HaloInner", pos, haloColor, style,
                haloColor, HaloInnerOutlineWidth);
            row.Main = CreateLayer(parent, name + "_Main", pos, faceColor, style,
                Color.Lerp(OutlineColor, haloColor, OutlineTintTowardHalo), OutlineWidth);
            row.PopGhost = CreateLayer(parent, name + "_PopGhost", pos, ghostColor, style);
            return row;
        }

        private static TextMeshProUGUI CreateLayer(RectTransform parent, string name, Vector2 pos,
            Color color, FontStyles style, Color? outlineColor = null, float outlineWidth = 0f)
        {
            TextMeshProUGUI label = UiKit.CreateText(parent, name, pos, LabelSize,
                string.Empty, MeasureFontSize, color, TextAlignmentOptions.Center);
            label.fontStyle = style;
            if (outlineColor.HasValue && outlineWidth > 0f)
            {
                label.outlineColor = outlineColor.Value;
                label.outlineWidth = outlineWidth;
            }
            label.enabled = false;
            return label;
        }

        public void Show(string line1, string line2)
        {
            if (_root == null) return;
            try
            {
                ApplyText(_row1, line1 ?? string.Empty);
                ApplyText(_row2, line2 ?? string.Empty);
                _elapsedSec = 0f;
                _playing = true;
                if (!_root.activeSelf) _root.SetActive(true);
                Apply();
            }
            catch (Exception e)
            {
                WLog.Line("deadline_banner_show_error", secret: false, ("err", e.Message));
            }
        }

        public void Tick()
        {
            if (!_playing || _root == null) return;
            try
            {
                _elapsedSec += Time.unscaledDeltaTime;
                if (_elapsedSec > DeadlineBanner.TotalSec)
                {
                    Hide();
                    return;
                }
                Apply();
            }
            catch (Exception e)
            {
                WLog.Line("deadline_banner_tick_error", secret: false, ("err", e.Message));
                Hide();
            }
        }

        public void Hide()
        {
            _playing = false;
            _elapsedSec = 0f;
            if (_root != null && _root.activeSelf) _root.SetActive(false);
        }

        public void Destroy()
        {
            _playing = false;
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _row1 = null;
            _row2 = null;
            _emojis = null;
        }

        private void ApplyText(Row row, string text)
        {
            if (row == null) return;
            row.Main.text = row.SlideGhost.text = row.PopGhost.text
                = row.HaloInner.text = row.HaloOuter.text = text;

            float restWidth = row.TargetWidthPx / DeadlineBanner.PopScale;
            row.Main.fontSize = MeasureFontSize;
            float measured = row.Main.GetPreferredValues(text).x;
            float fontSize;
            if (measured > 0f)
            {
                fontSize = MeasureFontSize * restWidth / measured;
            }
            else
            {
                fontSize = FallbackFontSize;
                WLog.Line("deadline_banner_measure_failed", secret: false, ("len", text.Length));
            }
            row.Main.fontSize = row.SlideGhost.fontSize = row.PopGhost.fontSize
                = row.HaloInner.fontSize = row.HaloOuter.fontSize = fontSize;
            row.StartOffsetX = DeadlineBanner.StartOffsetX(restWidth, row.FromLeft);
        }

        private void Apply()
        {
            ApplyEmojis(_elapsedSec);
            ApplyRow(_row1, _elapsedSec);
            ApplyRow(_row2, _elapsedSec - DeadlineBanner.Line2StaggerSec);
        }

        private void ApplyEmojis(float t)
        {
            if (_emojis == null) return;
            for (int i = 0; i < _emojis.Length; i++)
            {
                Image img = _emojis[i];
                if (img == null) continue;
                BannerEmojiState state = DeadlineBanner.ComputeEmoji(t, i);
                if (!state.Visible)
                {
                    if (img.enabled) img.enabled = false;
                    continue;
                }
                if (!img.enabled) img.enabled = true;
                img.rectTransform.anchoredPosition = new Vector2(state.CenterX, state.CenterY);
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -state.RotationRad * Mathf.Rad2Deg);
                Color c = img.color;
                c.a = state.Alpha;
                img.color = c;
            }
        }

        private static void ApplyRow(Row row, float t)
        {
            if (row == null) return;
            BannerRowState state = DeadlineBanner.Compute(t, row.StartOffsetX);
            ApplyLayer(row.Main, state.Main, row.CenterY);
            ApplyLayer(row.HaloOuter, Dim(state.Main, HaloOuterAlpha), row.CenterY);
            ApplyLayer(row.HaloInner, Dim(state.Main, HaloInnerAlpha), row.CenterY);
            ApplyLayer(row.SlideGhost, state.SlideGhost, row.CenterY);
            ApplyLayer(row.PopGhost, state.PopGhost, row.CenterY);
        }

        private static BannerLayerState Dim(BannerLayerState main, float alphaFactor)
        {
            return main.Visible
                ? new BannerLayerState(true, main.OffsetX, main.Scale, main.Alpha * alphaFactor)
                : BannerLayerState.Hidden;
        }

        private static void ApplyLayer(TextMeshProUGUI label, BannerLayerState layer, float centerY)
        {
            if (label == null) return;
            if (!layer.Visible)
            {
                if (label.enabled) label.enabled = false;
                return;
            }
            if (!label.enabled) label.enabled = true;
            label.rectTransform.anchoredPosition = new Vector2(layer.OffsetX, centerY);
            label.rectTransform.localScale = Vector3.one * layer.Scale;
            Color c = label.color;
            c.a = layer.Alpha;
            label.color = c;
        }
    }
}
