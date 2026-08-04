using TMPro;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class ModIntegrityPlayerBadge : MonoBehaviour
    {
        private const float BadgeX = 135f;
        private const float BadgeYOffsetFromName = 5f;
        private TextMeshProUGUI _label;
        private RectTransform _rect;
        private int _lastRevision = -1;
        private ModIntegrityStatus _lastStatus;

        public static ModIntegrityPlayerBadge GetOrCreate(MenuPlayerListed row)
        {
            if (row == null) return null;
            ModIntegrityPlayerBadge badge = row.GetComponent<ModIntegrityPlayerBadge>();
            if (badge == null) badge = row.gameObject.AddComponent<ModIntegrityPlayerBadge>();
            badge.EnsureBuilt();
            return badge;
        }

        public void SetRecord(ModParticipantRecord record, int revision)
        {
            EnsureBuilt();
            if (_label == null) return;
            if (record == null)
            {
                _label.gameObject.SetActive(false);
                return;
            }
            if (_lastRevision == revision && _lastStatus == record.Status && _label.gameObject.activeSelf) return;
            _lastRevision = revision;
            _lastStatus = record.Status;
            _label.text = Icon(record.Status);
            _label.color = ColorFor(record.Status);
            _label.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_label != null) _label.gameObject.SetActive(false);
        }

        private void EnsureBuilt()
        {
            if (_label != null)
            {
                AlignWithPlayerName();
                return;
            }
            _rect = UiKit.CreateRect(transform, "WW_ModIntegrityBadge", new Vector2(BadgeX, 0f), new Vector2(34f, 30f));
            _label = UiKit.CreateText(_rect, "Label", Vector2.zero, _rect.sizeDelta, "?", 22f, Color.white, TextAlignmentOptions.Center);
            _label.richText = false;
            _label.raycastTarget = false;
            _label.gameObject.SetActive(false);
            AlignWithPlayerName();
        }

        private void AlignWithPlayerName()
        {
            if (_rect == null) return;
            MenuPlayerListed row = GetComponent<MenuPlayerListed>();
            RectTransform nameRect = row != null && row.playerName != null ? row.playerName.rectTransform : null;
            if (nameRect == null) return;

            Vector3 nameCenter = transform.InverseTransformPoint(nameRect.TransformPoint(nameRect.rect.center));
            Vector2 anchoredPosition = _rect.anchoredPosition;
            anchoredPosition.x = BadgeX;
            _rect.anchoredPosition = anchoredPosition;

            Vector3 position = _rect.localPosition;
            position.y = nameCenter.y + BadgeYOffsetFromName;
            _rect.localPosition = position;
        }

        private static string Icon(ModIntegrityStatus status)
        {
            switch (status)
            {
                case ModIntegrityStatus.Baseline: return "◆";
                case ModIntegrityStatus.Match: return "✓";
                case ModIntegrityStatus.Difference: return "!";
                case ModIntegrityStatus.Unavailable: return "×";
                default: return "?";
            }
        }

        private static Color ColorFor(ModIntegrityStatus status)
        {
            switch (status)
            {
                case ModIntegrityStatus.Baseline: return new Color(0.78f, 0.72f, 1f);
                case ModIntegrityStatus.Match: return new Color(0.4f, 1f, 0.55f);
                case ModIntegrityStatus.Difference: return new Color(1f, 0.75f, 0.2f);
                case ModIntegrityStatus.Unavailable: return new Color(1f, 0.28f, 0.3f);
                default: return new Color(0.85f, 0.85f, 0.85f);
            }
        }
    }
}
