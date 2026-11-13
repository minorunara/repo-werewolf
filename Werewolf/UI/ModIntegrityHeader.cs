#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Vanilla;

namespace Werewolf.UI
{
    public sealed class ModIntegrityHeader : IClientPanel
    {
        private static readonly Color NormalColor = new Color(0.12f, 0.15f, 0.2f, 0.94f);
        private static readonly Color CautionColor = new Color(0.42f, 0.3f, 0.04f, 0.96f);
        private static readonly Color SevereColor = new Color(0.42f, 0.06f, 0.06f, 0.96f);
        private sealed class ViewRefs
        {
            public ViewRefs(
                GameObject root,
                RectTransform rect,
                Image background,
                TextMeshProUGUI summary,
                TextMeshProUGUI selfWarning)
            {
                Root = root;
                Rect = rect;
                Background = background;
                Summary = summary;
                SelfWarning = selfWarning;
            }

            public GameObject Root { get; }
            public RectTransform Rect { get; }
            public Image Background { get; }
            public TextMeshProUGUI Summary { get; }
            public TextMeshProUGUI SelfWarning { get; }
        }

        private ViewRefs? _view;

        public string LayerName => WerewolfUIManager.ModIntegrityLayer;
        public bool Exists => GetLiveViewOrNull() != null;
        public bool Visible
        {
            get
            {
                ViewRefs? view = GetLiveViewOrNull();
                return view != null && view.Root.activeSelf;
            }
        }
        public Action? OnOpenPanel;

        public void Build(Transform layerRoot)
        {
            if (GetLiveViewOrNull() != null || layerRoot == null) return;

            GameObject? root = null;
            try
            {
                if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

                RectTransform rect = UiKit.CreateRect(layerRoot, "WW_ModIntegrityHeader",
                    new Vector2(0f, -24f), new Vector2(760f, 94f));
                root = rect.gameObject;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                Image background = UiKit.CreateImage(rect, "Bg", Vector2.zero,
                    new Vector2(760f, 94f), NormalColor);
                TextMeshProUGUI summary = UiKit.CreateText(rect, "Summary", new Vector2(0f, 18f),
                    new Vector2(730f, 42f), string.Empty, 28f, Color.white,
                    TextAlignmentOptions.Center);
                TextMeshProUGUI selfWarning = UiKit.CreateText(rect, "SelfWarning",
                    new Vector2(0f, -22f), new Vector2(730f, 34f), string.Empty, 21f,
                    new Color(1f, 0.82f, 0.35f), TextAlignmentOptions.Center);
                summary.richText = false;
                selfWarning.richText = false;

                root.SetActive(false);
                _view = new ViewRefs(root, rect, background, summary, selfWarning);
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

        public void SetSnapshot(ModIntegritySnapshot snapshot, int localActor)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null || snapshot == null) return;
            int baseline = 0;
            int match = 0;
            int difference = 0;
            int unavailable = 0;
            int pending = 0;
            ModParticipantRecord? local = null;
            for (int i = 0; i < snapshot.Records.Count; i++)
            {
                ModParticipantRecord record = snapshot.Records[i];
                if (record.Actor == localActor) local = record;
                switch (record.Status)
                {
                    case ModIntegrityStatus.Baseline: baseline++; break;
                    case ModIntegrityStatus.Match: match++; break;
                    case ModIntegrityStatus.Difference: difference++; break;
                    case ModIntegrityStatus.Unavailable: unavailable++; break;
                    case ModIntegrityStatus.Pending: pending++; break;
                }
            }

            int participants = Math.Max(0, snapshot.Records.Count - baseline);
            bool allMatch = participants == match && difference == 0 && unavailable == 0 && pending == 0;
            view.Summary.text = allMatch
                ? Texts.Format(TextId.ModIntegrityHeaderAllMatchFormat, snapshot.Records.Count, snapshot.Records.Count)
                : Texts.Format(TextId.ModIntegrityHeaderCountsFormat, baseline, match, difference, unavailable, pending);

            view.SelfWarning.text = string.Empty;
            if (local != null && local.Status == ModIntegrityStatus.Difference)
            {
                view.SelfWarning.text = Texts.Format(
                    TextId.ModIntegritySelfDifferenceFormat,
                    local.Summary.Missing, local.Summary.Extra,
                    local.Summary.Version, local.Summary.Content);
            }
            else if (local != null && local.Status == ModIntegrityStatus.Unavailable)
            {
                view.SelfWarning.text = Texts.Get(TextId.ModIntegritySelfUnavailable);
            }

            view.Background.color = unavailable > 0 || pending > 0
                ? SevereColor
                : difference > 0 ? CautionColor : NormalColor;
        }

        public void Tick(bool active)
        {
            ViewRefs? view = GetLiveViewOrNull();
            if (view == null) return;
            if (view.Root.activeSelf != active) view.Root.SetActive(active);
            if (!active || !Input.GetMouseButtonDown(0)) return;
            if (RectTransformUtility.RectangleContainsScreenPoint(view.Rect, Input.mousePosition, null))
                OnOpenPanel?.Invoke();
        }

        private ViewRefs? GetLiveViewOrNull()
        {
            ViewRefs? view = _view;
            if (view == null) return null;
            if (view.Root != null) return view;

            _view = null;
            return null;
        }

        public void Destroy()
        {
            ViewRefs? view = _view;
            _view = null;
            if (view != null && view.Root != null) UnityEngine.Object.Destroy(view.Root);
        }
    }
}
