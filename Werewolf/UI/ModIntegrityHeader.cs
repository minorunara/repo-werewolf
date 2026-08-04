using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class ModIntegrityHeader : IClientPanel
    {
        private static readonly Color NormalColor = new Color(0.12f, 0.15f, 0.2f, 0.94f);
        private static readonly Color CautionColor = new Color(0.42f, 0.3f, 0.04f, 0.96f);
        private static readonly Color SevereColor = new Color(0.42f, 0.06f, 0.06f, 0.96f);
        private GameObject _root;
        private Image _background;
        private TextMeshProUGUI _summary;
        private TextMeshProUGUI _selfWarning;
        private RectTransform _rect;

        public string LayerName => WerewolfUIManager.ModIntegrityLayer;
        public bool Exists => _root != null;
        public bool Visible => _root != null && _root.activeSelf;
        public Action OnOpenPanel;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            _rect = UiKit.CreateRect(layerRoot, "WW_ModIntegrityHeader", new Vector2(0f, -24f), new Vector2(760f, 94f));
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0.5f, 1f);
            _root = _rect.gameObject;
            _background = UiKit.CreateImage(_rect, "Bg", Vector2.zero, new Vector2(760f, 94f), NormalColor);
            _summary = UiKit.CreateText(_rect, "Summary", new Vector2(0f, 18f), new Vector2(730f, 42f),
                string.Empty, 28f, Color.white, TextAlignmentOptions.Center);
            _selfWarning = UiKit.CreateText(_rect, "SelfWarning", new Vector2(0f, -22f), new Vector2(730f, 34f),
                string.Empty, 21f, new Color(1f, 0.82f, 0.35f), TextAlignmentOptions.Center);
            _summary.richText = false;
            _selfWarning.richText = false;
            _root.SetActive(false);
        }

        public void SetSnapshot(ModIntegritySnapshot snapshot, int localActor)
        {
            if (_root == null || snapshot == null) return;
            int baseline = 0;
            int match = 0;
            int difference = 0;
            int unavailable = 0;
            int pending = 0;
            ModParticipantRecord local = null;
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
            _summary.text = allMatch
                ? Texts.Format(TextId.ModIntegrityHeaderAllMatchFormat, snapshot.Records.Count, snapshot.Records.Count)
                : Texts.Format(TextId.ModIntegrityHeaderCountsFormat, baseline, match, difference, unavailable, pending);

            _selfWarning.text = string.Empty;
            if (local != null && local.Status == ModIntegrityStatus.Difference)
            {
                _selfWarning.text = Texts.Format(
                    TextId.ModIntegritySelfDifferenceFormat,
                    local.Summary.Missing, local.Summary.Extra,
                    local.Summary.Version, local.Summary.Content);
            }
            else if (local != null && local.Status == ModIntegrityStatus.Unavailable)
            {
                _selfWarning.text = Texts.Get(TextId.ModIntegritySelfUnavailable);
            }

            _background.color = unavailable > 0 || pending > 0
                ? SevereColor
                : difference > 0 ? CautionColor : NormalColor;
        }

        public void Tick(bool active)
        {
            if (_root == null) return;
            if (_root.activeSelf != active) _root.SetActive(active);
            if (!active || _rect == null || !Input.GetMouseButtonDown(0)) return;
            if (RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition, null))
                OnOpenPanel?.Invoke();
        }

        public void Destroy()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _rect = null;
            _background = null;
            _summary = null;
            _selfWarning = null;
        }
    }
}
