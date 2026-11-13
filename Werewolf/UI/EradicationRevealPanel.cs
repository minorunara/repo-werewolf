#nullable enable
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class EradicationRevealPanel : IClientPanel
    {
        public string LayerName => "EradicationReveal";

        private static readonly Vector2 IconSize = new Vector2(280f, 280f);
        private static readonly Vector2 IconPos = new Vector2(0f, 250f);
        private static readonly Vector2 TitlePos = new Vector2(0f, 30f);
        private static readonly Vector2 RowPos = new Vector2(0f, -140f);

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color TitleColor = new Color(0.85f, 0.08f, 0.08f, 1f);

        private sealed class ViewRefs
        {
            public ViewRefs(GameObject root, CanvasGroup group, RectTransform stampRect,
                CanvasGroup stampGroup, Image iconImage,
                TextMeshProUGUI titleText, TextMeshProUGUI titleTextBack)
            {
                Root = root;
                Group = group;
                StampRect = stampRect;
                StampGroup = stampGroup;
                IconImage = iconImage;
                TitleText = titleText;
                TitleTextBack = titleTextBack;
            }

            public GameObject Root { get; }
            public CanvasGroup Group { get; }
            public RectTransform StampRect { get; }
            public CanvasGroup StampGroup { get; }
            public Image IconImage { get; }
            public TextMeshProUGUI TitleText { get; }
            public TextMeshProUGUI TitleTextBack { get; }
            public int RunGeneration { get; set; }
        }

        private sealed class PreparedContent
        {
            public PreparedContent(VoteRow row) => Row = row;
            public VoteRow Row { get; }
        }

        private ViewRefs? _view;
        private PreparedContent? _prepared;

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
                    layerRoot, "WW_EradicationReveal", BackdropColor);
                RectTransform rect = shell.Rect;
                root = rect.gameObject;

                RectTransform stampRect = UiKit.CreateRect(
                    rect, "Stamp", Vector2.zero, new Vector2(1400f, 900f));
                CanvasGroup stampGroup = stampRect.gameObject.AddComponent<CanvasGroup>();
                stampGroup.alpha = 0f;
                stampGroup.blocksRaycasts = false;
                stampGroup.interactable = false;

                Image iconImage = UiKit.CreateImage(stampRect, "Icon", IconPos, IconSize, Color.white);
                iconImage.enabled = false;
                TextMeshProUGUI titleText = RevealPanelKit.CreateStampTitle(
                    stampRect, TitlePos, new Vector2(1400f, 130f), "", 96f,
                    TitleColor, Color.black, 0.12f, out TextMeshProUGUI titleTextBack);

                root.SetActive(false);
                _view = new ViewRefs(root, shell.Group, stampRect, stampGroup,
                    iconImage, titleText, titleTextBack);
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

        public void Show(WPlayer victim, Func<int, PlayerHandle> resolveAvatar,
                         byte outcome, bool vanished)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || victim == null) return;

            InvalidateRun(view);
            ClearPrepared();
            view.Root.SetActive(false);
            VoteRow? row = null;
            try
            {
                string title = Texts.Get(EradicationCeremony.TitleId(outcome, vanished));
                view.TitleText.text = title;
                view.TitleTextBack.text = title;
                RevealPanelKit.SetIcon(
                    view.IconImage, AssetCatalog.GetSprite("img_taxman_death"));

                row = VoteRow.Build(view.StampRect, victim, resolveAvatar, VoteRowGrid.RowSize);
                RectTransform? rowRoot = row.Root;
                if (rowRoot != null) rowRoot.anchoredPosition = RowPos;

                view.StampGroup.alpha = 0f;
                view.StampRect.localScale = Vector3.one;
                _prepared = new PreparedContent(row);
                row = null;
            }
            catch (Exception e)
            {
                row?.Destroy();
                _prepared = null;
                view.Root.SetActive(false);
                WLog.Line("eradication_reveal_prepare_error", secret: false, ("err", e.Message));
            }
        }

        public IEnumerator Play(Action? onStamp, Action? onDone = null)
        {
            ViewRefs? view = GetLiveViewOrNull();
            PreparedContent? prepared = _prepared;
            if (view == null || prepared == null)
            {
                WLog.Line("eradication_reveal_skip", secret: false,
                    ("reason", view == null ? "not_built" : "not_prepared"));
                yield break;
            }

            int generation = NextRun(view);
            yield return RunGuarded(
                view, prepared, generation, UiTween.Hold(EradicationCeremony.GraceSec));
            if (!IsCurrentRun(view, prepared, generation)) yield break;

            view.Group.alpha = 0f;
            view.Root.transform.parent.SetAsLastSibling();
            view.Root.SetActive(true);
            WLog.Line("eradication_reveal_show", secret: false);

            yield return RunGuarded(view, prepared, generation,
                UiTween.Fade(view.Group, 0f, 1f,
                    EradicationCeremony.BackdropFadeSec, UiTween.EaseIn()));
            if (!IsCurrentRun(view, prepared, generation)) yield break;

            yield return RunGuarded(view, prepared, generation,
                RevealPanelKit.StampEntrance(
                    view.StampRect, view.StampGroup, EradicationCeremony.StampEntranceSec));
            if (!IsCurrentRun(view, prepared, generation)) yield break;

            RevealPanelKit.InvokeCallback(onStamp, "eradication_stamp_sfx_error");
            if (!IsCurrentRun(view, prepared, generation)) yield break;

            yield return RunGuarded(
                view, prepared, generation, UiTween.Hold(EradicationCeremony.StampHoldSec));
            if (!IsCurrentRun(view, prepared, generation)) yield break;
            WLog.Line("eradication_reveal_hold_end", secret: false);
            RevealPanelKit.InvokeCallback(onDone, "eradication_done_callback_error");
        }

        public void Tick()
        {
            ViewRefs? view = GetLiveViewOrNull();
            PreparedContent? prepared = _prepared;
            if (view == null || !view.Root.activeSelf || prepared == null) return;
            prepared.Row.Tick();
        }

        public void HideNow()
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view != null) HideCurrent(view);
            else ClearPrepared();
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view != null) InvalidateRun(view);
            ClearPrepared();
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }

        private IEnumerator RunGuarded(
            ViewRefs view, PreparedContent prepared, int generation, IEnumerator routine)
        {
            while (IsCurrentRun(view, prepared, generation))
            {
                bool moved;
                try { moved = routine.MoveNext(); }
                catch (Exception e)
                {
                    WLog.Line("eradication_reveal_tween_error", secret: false, ("err", e.Message));
                    if (IsCurrentRun(view, prepared, generation)) HideCurrent(view);
                    yield break;
                }
                if (!moved || !IsCurrentRun(view, prepared, generation)) yield break;
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
            ClearPrepared();
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

        private bool IsCurrentRun(ViewRefs view, PreparedContent prepared, int generation)
        {
            return ReferenceEquals(_view, view)
                && ReferenceEquals(_prepared, prepared)
                && view.Root != null
                && view.RunGeneration == generation;
        }

        private void HideCurrent(ViewRefs view)
        {
            InvalidateRun(view);
            ClearPrepared();
            if (view.Root == null) return;
            view.Group.alpha = 0f;
            view.StampGroup.alpha = 0f;
            view.StampRect.localScale = Vector3.one;
            view.Root.SetActive(false);
        }

        private void ClearPrepared()
        {
            PreparedContent? prepared = _prepared;
            _prepared = null;
            prepared?.Row.Destroy();
        }
    }
}
