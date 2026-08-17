using System;
using TMPro;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Core.Replay;

namespace Werewolf.UI
{
    public sealed partial class ReplayViewerPanel
    {

        private void UpdateDanmaku(double prevT, bool wasPlaying)
        {
            if (_danmaku == null)
            {
                HideDanmakuViews();
                return;
            }
            float dt = Mathf.Max(0f, Time.unscaledDeltaTime);
            if (DemoActive) TickDemoChat(dt);

            if (!_seeking)
            {
                double newT = _clock != null ? _clock.T : prevT;
                _danmaku.Step(prevT, newT, wasPlaying || DemoActive ? dt : 0.0, MeasureCommentWidth);
            }

            int used = 0;
            foreach (ReplayDanmakuComment c in _danmaku.Active)
            {
                CommentView view = EnsureCommentView(used++);
                if (!ReferenceEquals(view.Bound, c)) BindCommentView(view, c);
                ReplayDanmaku.LandingShakeAt(c, c.Elapsed, out double shakeX, out double shakeY);
                ReplayDanmaku.IdleMotionAt(c, c.Elapsed, out double idleX, out double idleY);
                float x = (float)(ReplayDanmaku.CenterXAt(c, c.Elapsed) * PanelWidth)
                    + (float)shakeX * DanmakuLandingShakePx + (float)idleX;
                float y = (float)(ReplayDanmaku.CenterYRatioAt(c, c.Elapsed) * MapHeight)
                    + (float)shakeY * DanmakuLandingShakePx + (float)idleY;
                view.Rect.anchoredPosition = new Vector2(x, y);
                float scale = (float)ReplayDanmaku.ScaleAt(c, c.Elapsed);
                view.Rect.localScale = new Vector3(scale, scale, 1f);
                view.Rect.localEulerAngles = new Vector3(0f, 0f,
                    (float)ReplayDanmaku.RotationDegreesAt(c, c.Elapsed));
                view.Opacity.alpha = (float)ReplayDanmaku.OpacityAt(c, c.Elapsed);
                float accentAlpha = (float)ReplayDanmaku.AccentOpacityAt(c, c.Elapsed);
                float accentFullWidth = (float)(c.WidthRatio * PanelWidth) + DanmakuAccentExtraWidth;
                float accentWidthScale = (float)ReplayDanmaku.AccentWidthScaleAt(c, c.Elapsed);
                Vector2 accentSize = view.Accent.rectTransform.sizeDelta;
                accentSize.x = accentFullWidth * accentWidthScale;
                view.Accent.rectTransform.sizeDelta = accentSize;
                view.Accent.rectTransform.anchoredPosition = new Vector2(
                    accentFullWidth * (float)ReplayDanmaku.AccentCenterOffsetFactorAt(c, c.Elapsed), 0f);
                Color accentColor = view.Accent.color;
                accentColor.a = accentAlpha;
                view.Accent.color = accentColor;
                bool showAccent = accentAlpha > 0.001f;
                if (view.Accent.gameObject.activeSelf != showAccent)
                {
                    view.Accent.gameObject.SetActive(showAccent);
                }
                if (!view.Root.activeSelf) view.Root.SetActive(true);
            }
            HideCommentExtra(used);
            UpdateStamp();
        }

        private void TickDemoChat(float dt)
        {
            _demoChatTimer += dt;
            if (_demoChatTimer < DemoChatIntervalSec) return;
            _demoChatTimer = 0f;
            int actor = _demoCount > 0 ? (_demoChatSeq % _demoCount) + 1 : 1;
            _danmaku.SpawnAdHoc(actor, DemoChatTexts[_demoChatSeq % DemoChatTexts.Length],
                MeasureCommentWidth);
            _demoChatSeq++;
        }

        private void UpdateStamp()
        {
            if (_stampLabel == null) return;
            int executed = 0;
            double progress = 0;
            bool show = _danmaku != null && _danmaku.TryGetStamp(out executed, out progress);
            if (!show)
            {
                if (_stampLabel.gameObject.activeSelf) _stampLabel.gameObject.SetActive(false);
                return;
            }

            string want;
            Color color;
            if (executed == -1)
            {
                want = Texts.Get(TextId.ReplayStampNoExecution);
                color = StampNoExecutionColor;
            }
            else
            {
                _danmakuPlayers.TryGetValue(executed, out ReplayPlayerEntry entry);
                string idText = entry != null && entry.ParticipantId > 0
                    ? entry.ParticipantId.ToString()
                    : executed.ToString();
                string name = entry != null && !string.IsNullOrEmpty(entry.Name) ? entry.Name : "???";
                want = Texts.Format(TextId.ReplayStampExecutedFormat, idText, name);
                color = StampExecutedColor;
            }
            color.a = 1f - (float)(progress * progress);
            if (_stampLabel.text != want) _stampLabel.text = want;
            _stampLabel.color = color;
            if (!_stampLabel.gameObject.activeSelf) _stampLabel.gameObject.SetActive(true);
        }

        private double MeasureCommentWidth(ReplayDanmakuComment c)
        {
            if (_danmakuMeasurer == null) return 0.2;
            try
            {
                _danmakuMeasurer.fontSize = BodyFontSize(c);
                string prefix = DanmakuPrefix(c);
                float width = _danmakuMeasurer.GetPreferredValues(prefix + c.Line1).x;
                if (c.Line2 != null)
                {
                    float line2 = _danmakuMeasurer.GetPreferredValues(prefix).x
                        + _danmakuMeasurer.GetPreferredValues(c.Line2).x;
                    if (line2 > width) width = line2;
                }
                return (width + DanmakuWidthPaddingPx) / PanelWidth;
            }
            catch (Exception e)
            {
                WLog.Line("replay_danmaku_measure_error", secret: false, ("err", e.Message));
                return 0.2;
            }
        }

        private string DanmakuPrefix(ReplayDanmakuComment c)
        {
            if (_danmakuPlayers.TryGetValue(c.Actor, out ReplayPlayerEntry p) && p.ParticipantId > 0)
            {
                return p.ParticipantId + ": ";
            }
            return (DemoActive ? c.Actor.ToString() : "?") + ": ";
        }

        private void BindCommentView(CommentView view, ReplayDanmakuComment c)
        {
            _danmakuPlayers.TryGetValue(c.Actor, out ReplayPlayerEntry entry);
            int pid = entry != null ? entry.ParticipantId : (DemoActive ? c.Actor : 0);
            ReplayMarkerPalette.ColorFor(pid, out float r, out float g, out float b);
            var bodyColor = new Color(r, g, b, 1f);
            Color accentColor = bodyColor;
            accentColor.a = 0f;
            view.Accent.color = accentColor;
            float accentHeight;
            switch (c.Profile)
            {
                case ReplayDanmakuProfile.Impact:
                    accentHeight = 18f;
                    break;
                case ReplayDanmakuProfile.Cool:
                    accentHeight = 8f;
                    break;
                default:
                    accentHeight = 12f;
                    break;
            }
            view.Accent.rectTransform.sizeDelta = new Vector2(
                (float)(c.WidthRatio * PanelWidth) + DanmakuAccentExtraWidth, accentHeight);
            view.Accent.rectTransform.localEulerAngles = Vector3.zero;

            string prefix = DanmakuPrefix(c);
            float bodyFontSize = BodyFontSize(c);
            float lineHeight = bodyFontSize * 1.10f;
            float metaOffsetY = bodyFontSize * 0.92f + 12f;
            float left = -(float)(c.WidthRatio * PanelWidth) * 0.5f;
            float prefixW = 0f;
            if (_danmakuMeasurer != null)
            {
                _danmakuMeasurer.fontSize = bodyFontSize;
                prefixW = _danmakuMeasurer.GetPreferredValues(prefix).x;
            }

            view.Body1.rectTransform.anchoredPosition = new Vector2(left, 0f);
            view.Body1.fontSize = bodyFontSize;
            view.Body1.rectTransform.sizeDelta = new Vector2(1400f, lineHeight * 1.2f);
            string body1 = prefix + c.Line1;
            if (view.Body1.text != body1) view.Body1.text = body1;
            view.Body1.color = bodyColor;

            bool two = c.Line2 != null;
            if (view.Body2.gameObject.activeSelf != two) view.Body2.gameObject.SetActive(two);
            if (two)
            {
                view.Body2.rectTransform.anchoredPosition = new Vector2(left + prefixW, -lineHeight);
                view.Body2.fontSize = bodyFontSize;
                view.Body2.rectTransform.sizeDelta = new Vector2(1400f, lineHeight * 1.2f);
                if (view.Body2.text != c.Line2) view.Body2.text = c.Line2;
                view.Body2.color = bodyColor;
            }

            string meta = "";
            if (entry != null)
            {
                meta = entry.Name ?? "";
                if (entry.Role != ReplayPlayerEntry.RoleUnknown)
                {
                    meta += "（" + RoleText.Label((Role)entry.Role) + "）";
                }
            }
            else if (DemoActive)
            {
                meta = "P" + c.Actor;
            }
            view.Meta.rectTransform.anchoredPosition = new Vector2(left + prefixW, metaOffsetY);
            if (view.Meta.text != meta) view.Meta.text = meta;
            view.Meta.color = entry != null && entry.IsWerewolfSide
                ? DanmakuMetaWolfColor
                : DanmakuMetaColor;

            view.Bound = c;
        }

        private static float BodyFontSize(ReplayDanmakuComment c)
            => DanmakuBodyFontSize * (float)ReplayDanmaku.FontScaleFor(c);

        private CommentView EnsureCommentView(int index)
        {
            while (_danmakuPool.Count <= index)
            {
                var view = new CommentView();
                RectTransform root = UiKit.CreateRect(_danmakuRoot, "Comment" + _danmakuPool.Count,
                    Vector2.zero, new Vector2(1f, DanmakuShotRootHeight));
                view.Root = root.gameObject;
                view.Rect = root;
                view.Opacity = root.gameObject.AddComponent<CanvasGroup>();
                view.Opacity.alpha = 0f;
                view.Opacity.interactable = false;
                view.Opacity.blocksRaycasts = false;
                view.Accent = UiKit.CreateImage(root, "Accent", Vector2.zero,
                    new Vector2(1f, 12f), Color.clear);
                view.Accent.gameObject.SetActive(false);
                view.Meta = UiKit.CreateText(root, "Meta", Vector2.zero, new Vector2(1200f, 30f),
                    "", DanmakuMetaFontSize, DanmakuMetaColor, TextAlignmentOptions.MidlineLeft);
                view.Meta.rectTransform.pivot = new Vector2(0f, 0.5f);
                view.Body1 = UiKit.CreateText(root, "Body1", Vector2.zero,
                    new Vector2(1400f, DanmakuBodyFontSize * 1.2f),
                    "", DanmakuBodyFontSize, Color.white, TextAlignmentOptions.MidlineLeft);
                view.Body1.rectTransform.pivot = new Vector2(0f, 0.5f);
                view.Body2 = UiKit.CreateText(root, "Body2", Vector2.zero,
                    new Vector2(1400f, DanmakuBodyFontSize * 1.2f),
                    "", DanmakuBodyFontSize, Color.white, TextAlignmentOptions.MidlineLeft);
                view.Body2.rectTransform.pivot = new Vector2(0f, 0.5f);
                if (_danmakuOutlineMaterial != null)
                {
                    view.Body1.fontSharedMaterial = _danmakuOutlineMaterial;
                    view.Body2.fontSharedMaterial = _danmakuOutlineMaterial;
                }
                view.Root.SetActive(false);
                _danmakuPool.Add(view);
            }
            return _danmakuPool[index];
        }

        private void HideCommentExtra(int used)
        {
            for (int i = used; i < _danmakuPool.Count; i++)
            {
                if (_danmakuPool[i].Root.activeSelf) _danmakuPool[i].Root.SetActive(false);
                _danmakuPool[i].Bound = null;
            }
        }

        private void HideDanmakuViews()
        {
            HideCommentExtra(0);
            if (_stampLabel != null && _stampLabel.gameObject.activeSelf)
            {
                _stampLabel.gameObject.SetActive(false);
            }
        }

        private void BuildDanmakuOutlineMaterial()
        {
            try
            {
                if (_danmakuMeasurer == null || _danmakuMeasurer.fontSharedMaterial == null) return;
                _danmakuOutlineMaterial = new Material(_danmakuMeasurer.fontSharedMaterial);
                _danmakuOutlineMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, DanmakuOutlineWidth);
                _danmakuOutlineMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.9f));
            }
            catch (Exception e)
            {
                WLog.Line("replay_danmaku_outline_error", secret: false, ("err", e.Message));
                _danmakuOutlineMaterial = null;
            }
        }
    }
}
