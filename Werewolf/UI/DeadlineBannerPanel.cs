#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

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

        private ViewRefs? _view;

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, Image[] emojis, Row row1, Row row2)
            {
                Root = root;
                Emojis = emojis;
                Row1 = row1;
                Row2 = row2;
            }

            public GameObject Root { get; }
            public Image[] Emojis { get; }
            public Row Row1 { get; }
            public Row Row2 { get; }
            public float ElapsedSec { get; set; }
            public bool Playing { get; set; }
        }

        private sealed class Row
        {
            public Row(TextMeshProUGUI main, TextMeshProUGUI slideGhost,
                TextMeshProUGUI popGhost, TextMeshProUGUI haloInner,
                TextMeshProUGUI haloOuter, float centerY, float targetWidthPx, bool fromLeft)
            {
                Main = main;
                SlideGhost = slideGhost;
                PopGhost = popGhost;
                HaloInner = haloInner;
                HaloOuter = haloOuter;
                CenterY = centerY;
                TargetWidthPx = targetWidthPx;
                FromLeft = fromLeft;
            }

            public TextMeshProUGUI Main { get; }
            public TextMeshProUGUI SlideGhost { get; }
            public TextMeshProUGUI PopGhost { get; }
            public TextMeshProUGUI HaloInner { get; }
            public TextMeshProUGUI HaloOuter { get; }
            public float RestWidthPx { get; set; }
            public float CenterY { get; }
            public float TargetWidthPx { get; }
            public bool FromLeft { get; }
        }

        public bool Exists => GetLiveViewOrNull() != null;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                RectTransform rect = UiKit.CreateStretched(layerRoot, "WW_DeadlineBanner");
                root = rect.gameObject;

                Image[] emojis = BuildEmojis(rect);
                Row row1 = BuildRow(rect, "Line1", DeadlineBanner.RowOffsetY,
                    DeadlineBanner.Line1WidthPx, fromLeft: true,
                    faceColor: Line1FaceColor, ghostColor: Line1GhostColor, haloColor: Line1HaloColor, italic: false);
                Row row2 = BuildRow(rect, "Line2", -DeadlineBanner.RowOffsetY,
                    DeadlineBanner.Line2WidthPx, fromLeft: false,
                    faceColor: Line2FaceColor, ghostColor: Line2GhostColor, haloColor: Line2HaloColor, italic: true);

                root.SetActive(false);
                _view = new ViewRefs(root, emojis, row1, row2);
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

        private static Image[] BuildEmojis(RectTransform parent)
        {
            Sprite? sprite = AssetCatalog.GetSprite("emoji_zany_face");
            if (sprite == null)
            {
                WLog.Line("deadline_banner_emoji_missing", secret: false);
                return Array.Empty<Image>();
            }

            var emojis = new Image[DeadlineBanner.EmojiCount];
            for (int i = 0; i < emojis.Length; i++)
            {
                var go = new GameObject("Emoji" + i, typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                rect.sizeDelta = new Vector2(DeadlineBanner.EmojiSizePx, DeadlineBanner.EmojiSizePx);
                var img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.raycastTarget = false;
                img.enabled = false;
                emojis[i] = img;
            }
            return emojis;
        }

        private static Row BuildRow(RectTransform parent, string name, float centerY, float targetWidthPx,
            bool fromLeft, Color faceColor, Color ghostColor, Color haloColor, bool italic)
        {
            var pos = new Vector2(0f, centerY);
            FontStyles style = italic ? (FontStyles.Bold | FontStyles.Italic) : FontStyles.Bold;

            TextMeshProUGUI slideGhost = CreateLayer(parent, name + "_SlideGhost", pos, ghostColor, style);
            TextMeshProUGUI haloOuter = CreateLayer(parent, name + "_HaloOuter", pos, haloColor, style,
                haloColor, HaloOuterOutlineWidth);
            TextMeshProUGUI haloInner = CreateLayer(parent, name + "_HaloInner", pos, haloColor, style,
                haloColor, HaloInnerOutlineWidth);
            TextMeshProUGUI main = CreateLayer(parent, name + "_Main", pos, faceColor, style,
                Color.Lerp(OutlineColor, haloColor, OutlineTintTowardHalo), OutlineWidth);
            TextMeshProUGUI popGhost = CreateLayer(parent, name + "_PopGhost", pos, ghostColor, style);
            return new Row(main, slideGhost, popGhost, haloInner, haloOuter,
                centerY, targetWidthPx, fromLeft);
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
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            try
            {
                ApplyText(view.Row1, line1 ?? string.Empty);
                ApplyText(view.Row2, line2 ?? string.Empty);
                view.ElapsedSec = 0f;
                view.Playing = true;
                if (!view.Root.activeSelf) view.Root.SetActive(true);
                Apply(view);
            }
            catch (Exception e)
            {
                WLog.Line("deadline_banner_show_error", secret: false, ("err", e.Message));
                Hide(view);
            }
        }

        public void Tick()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || !view.Playing) return;
            try
            {
                view.ElapsedSec += Time.unscaledDeltaTime;
                if (view.ElapsedSec > DeadlineBanner.TotalSec)
                {
                    Hide(view);
                    return;
                }
                Apply(view);
            }
            catch (Exception e)
            {
                WLog.Line("deadline_banner_tick_error", secret: false, ("err", e.Message));
                Hide(view);
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

            view.Playing = false;
            view.ElapsedSec = 0f;
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
            view.Playing = false;
            view.ElapsedSec = 0f;
            return null;
        }

        private static void Hide(ViewRefs view)
        {
            view.Playing = false;
            view.ElapsedSec = 0f;
            if (view.Root != null && view.Root.activeSelf) view.Root.SetActive(false);
        }

        private static void ApplyText(Row row, string text)
        {
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
            row.RestWidthPx = restWidth;
        }

        private static void Apply(ViewRefs view)
        {
            if (!UiKit.TryMeasureCanvas((RectTransform)view.Root.transform, out CanvasBounds canvas)) return;
            ApplyEmojis(view.Emojis, view.ElapsedSec, canvas);
            ApplyRow(view.Row1, view.ElapsedSec, canvas);
            ApplyRow(view.Row2, view.ElapsedSec - DeadlineBanner.Line2StaggerSec, canvas);
        }

        private static void ApplyEmojis(Image[] emojis, float t, CanvasBounds canvas)
        {
            for (int i = 0; i < emojis.Length; i++)
            {
                Image img = emojis[i];
                if (img == null) continue;
                BannerEmojiState state = DeadlineBanner.ComputeEmoji(t, i, canvas);
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

        private static void ApplyRow(Row row, float t, CanvasBounds canvas)
        {
            float startOffsetX = DeadlineBanner.StartOffsetX(canvas, row.RestWidthPx, row.FromLeft);
            BannerRowState state = DeadlineBanner.Compute(t, startOffsetX);
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
