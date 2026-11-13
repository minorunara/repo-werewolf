#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class DeathRevealPanel : IClientPanel
    {
        public string LayerName => "DeathReveal";

        private const float GridTopY = -80f;
        private static readonly Vector2 IconSize = new Vector2(280f, 280f);

        private const float ExitRiseY = 700f;

        private const float TitleFontSize = 120f;
        private const float GuardTitleFontSizeMin = 56f;

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.75f);

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, CanvasGroup group, RectTransform content,
                Image iconImage, TextMeshProUGUI titleText, TextMeshProUGUI titleTextBack)
            {
                Root = root;
                Group = group;
                Content = content;
                IconImage = iconImage;
                TitleText = titleText;
                TitleTextBack = titleTextBack;
            }

            public GameObject Root { get; }
            public CanvasGroup Group { get; }
            public RectTransform Content { get; }
            public Image IconImage { get; }
            public TextMeshProUGUI TitleText { get; }
            public TextMeshProUGUI TitleTextBack { get; }
            public int RunGeneration { get; set; }
        }

        private sealed class Presentation
        {
            public Presentation(List<VoteRow> rows) => Rows = rows;
            public List<VoteRow> Rows { get; }
        }

        private ViewRefs? _view;
        private Presentation? _presentation;

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
                RevealPanelKit.FullscreenRoot shell = RevealPanelKit.BuildFullscreenRoot(
                    layerRoot, "WW_DeathReveal", BackdropColor);
                RectTransform rect = shell.Rect;
                root = rect.gameObject;

                RectTransform content = UiKit.CreateRect(
                    rect, "Content", Vector2.zero, new Vector2(1280f, 900f));
                Image iconImage = UiKit.CreateImage(
                    content, "Icon", new Vector2(0f, 260f), IconSize, Color.white);
                iconImage.enabled = false;
                TextMeshProUGUI titleText = RevealPanelKit.CreateStampTitle(
                    content, new Vector2(0f, 40f), new Vector2(1200f, 140f), "",
                    TitleFontSize, Color.black, Color.red, 0.15f,
                    out TextMeshProUGUI titleTextBack);

                root.SetActive(false);
                _view = new ViewRefs(root, shell.Group, content, iconImage, titleText, titleTextBack);
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

        public void Show(IReadOnlyList<WPlayer>? deadRoster, Func<int, PlayerHandle> resolveAvatar,
                         ConveneKind kind = ConveneKind.Button)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;

            InvalidateRun(view);
            ClearPresentation();
            view.Root.SetActive(false);
            var rows = new List<VoteRow>();
            try
            {
                bool hasDeaths = deadRoster != null && deadRoster.Count > 0;
                bool guard = kind == ConveneKind.ScatterGuard && hasDeaths;
                string title = Texts.Get(guard ? TextId.DeathRevealScatterGuardTitle
                    : hasDeaths ? TextId.DeathRevealTitle : TextId.DeathRevealNone);
                ApplyTitle(view.TitleText, title, autoShrink: guard);
                ApplyTitle(view.TitleTextBack, title, autoShrink: guard);

                Sprite? icon = guard
                    ? (AssetCatalog.GetSprite("img_taxman_handover")
                        ?? AssetCatalog.GetSprite("img_taxman_death"))
                    : AssetCatalog.GetSprite(hasDeaths ? "img_taxman_death" : "img_taxman_nodeath");
                RevealPanelKit.SetIcon(view.IconImage, icon);

                if (deadRoster != null)
                {
                    for (int i = 0; i < deadRoster.Count; i++)
                    {
                        WPlayer player = deadRoster[i];
                        if (player == null) continue;
                        VoteRow row = VoteRow.Build(
                            view.Content, player, resolveAvatar, VoteRowGrid.RowSize);
                        rows.Add(row);
                        RectTransform? rowRect = row.Root;
                        if (rowRect == null) continue;
                        if (deadRoster.Count == 1)
                        {
                            rowRect.anchoredPosition = new Vector2(0f, GridTopY);
                        }
                        else
                        {
                            rowRect.anchoredPosition = VoteRowGrid.Position(
                                i % VoteRowGrid.Columns,
                                Math.Min(i / VoteRowGrid.Columns, VoteRowGrid.RowsPerColumn - 1),
                                GridTopY);
                        }
                    }
                }

                _presentation = new Presentation(rows);
            }
            catch (Exception e)
            {
                DestroyRows(rows);
                _presentation = null;
                view.Root.SetActive(false);
                WLog.Line("death_reveal_show_error", secret: false, ("err", e.Message));
            }
        }

        public IEnumerator Play(Action? onImpact)
        {
            ViewRefs? view = GetLiveViewOrNull();
            Presentation? presentation = _presentation;
            if (view == null || presentation == null)
            {
                WLog.Line("death_reveal_skip", secret: false,
                    ("reason", view == null ? "not_built" : "not_prepared"));
                yield break;
            }

            int generation = NextRun(view);
            view.Group.alpha = 0f;
            view.Content.localScale = Vector3.one * RevealPanelKit.EntranceStartScale;
            view.Content.anchoredPosition = Vector2.zero;
            view.Root.SetActive(true);
            WLog.Line("death_reveal_show", secret: false, ("rows", presentation.Rows.Count));

            yield return RunGuarded(view, presentation, generation,
                RevealPanelKit.StampEntrance(view.Content, view.Group, DeathReveal.EntranceSec));
            if (!IsCurrentRun(view, presentation, generation)) yield break;

            RevealPanelKit.InvokeCallback(onImpact, "death_reveal_impact_error");
            if (!IsCurrentRun(view, presentation, generation)) yield break;

            yield return RunGuarded(
                view, presentation, generation, UiTween.Hold(DeathReveal.HoldSec));
            if (!IsCurrentRun(view, presentation, generation)) yield break;

            yield return RunGuarded(view, presentation, generation, UiTween.Parallel(
                UiTween.Move(view.Content, Vector2.zero, new Vector2(0f, ExitRiseY),
                    DeathReveal.ExitSec, UiTween.EaseIn()),
                UiTween.Fade(view.Group, 1f, 0f, DeathReveal.ExitSec, UiTween.EaseIn())));
            if (!IsCurrentRun(view, presentation, generation)) yield break;

            HideCurrent(view);
            WLog.Line("death_reveal_end", secret: false);
        }

        public void Tick()
        {
            ViewRefs? view = GetLiveViewOrNull();
            Presentation? presentation = _presentation;
            if (view == null || !view.Root.activeSelf || presentation == null) return;
            foreach (VoteRow row in presentation.Rows) row.Tick();
        }

        public void HideNow()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) HideCurrent(view);
            else ClearPresentation();
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view != null) InvalidateRun(view);
            ClearPresentation();
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private IEnumerator RunGuarded(
            ViewRefs view, Presentation presentation, int generation, IEnumerator routine)
        {
            while (IsCurrentRun(view, presentation, generation))
            {
                bool moved;
                try { moved = routine.MoveNext(); }
                catch (Exception e)
                {
                    WLog.Line("death_reveal_tween_error", secret: false, ("err", e.Message));
                    if (IsCurrentRun(view, presentation, generation)) HideCurrent(view);
                    yield break;
                }
                if (!moved || !IsCurrentRun(view, presentation, generation)) yield break;
                yield return routine.Current;
            }
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            InvalidateRun(view);
            ClearPresentation();
            return null;
        }

        private int NextRun(ViewRefs view)
        {
            InvalidateRun(view);
            return view.RunGeneration;
        }

        private static void InvalidateRun(ViewRefs view)
        {
            unchecked { view.RunGeneration++; }
        }

        private bool IsCurrentRun(ViewRefs view, Presentation presentation, int generation)
        {
            return ReferenceEquals(_view, view)
                && ReferenceEquals(_presentation, presentation)
                && view.Root != null
                && view.RunGeneration == generation;
        }

        private void HideCurrent(ViewRefs view)
        {
            InvalidateRun(view);
            ClearPresentation();
            if (view.Root == null) return;
            view.Group.alpha = 0f;
            view.Content.localScale = Vector3.one;
            view.Content.anchoredPosition = Vector2.zero;
            view.Root.SetActive(false);
        }

        private void ClearPresentation()
        {
            Presentation? presentation = _presentation;
            _presentation = null;
            if (presentation != null) DestroyRows(presentation.Rows);
        }

        private static void DestroyRows(List<VoteRow> rows)
        {
            foreach (VoteRow row in rows) row.Destroy();
            rows.Clear();
        }

        private static void ApplyTitle(TextMeshProUGUI label, string title, bool autoShrink)
        {
            if (label == null) return;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSize = TitleFontSize;
            label.fontSizeMin = GuardTitleFontSizeMin;
            label.fontSizeMax = TitleFontSize;
            label.enableAutoSizing = autoShrink;
            label.text = title;
        }
    }
}
