using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal sealed class ChatRowFactory
    {
        public const float SpeakerRowHeight = 40f;
        public const float SpeakerRowWidth = 210f;

        public const float VoteRowHeight = 36f;

        private const float AvatarPrefabScale = 1.35f;

        private const float AvatarOriginTrim = 2.2f;

        private const float VoteAvatarScale = 0.95f;
        private const float VoteAvatarWidth = 168f;
        private const float VoteTextWidth = 122f;
        private const float VoteTextFontSize = 17f;

        private const float SystemIconSize = 34f;
        private const float SystemIconGap = 6f;

        private const string SystemFallbackFace = "img_taxman_nodeath";

        private const float BubbleWidthRatio = 0.86f;
        private const float BubblePadding = 9f;

        private const float TailWidth = 9f;
        private const float TailHeight = 14f;
        private const float TailInset = 3f;
        private const float TailTopInset = 10f;

        private const float BodyFontSize = 20f;
        private const float NameFontSize = 19f;

        private const int AvatarFreezeWaitFrames = 120;

        private static readonly Color AliveBubbleColor = new Color(0.94f, 0.94f, 0.96f, 0.96f);
        private static readonly Color AliveBodyColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        private static readonly Color SelfBubbleColor = new Color(0.66f, 0.88f, 0.42f, 0.96f);
        private static readonly Color SelfBodyColor = new Color(0.06f, 0.13f, 0.03f, 1f);
        private static readonly Color DeadBubbleColor = new Color(0.34f, 0.35f, 0.40f, 0.90f);
        private static readonly Color DeadBodyColor = new Color(0.90f, 0.91f, 0.95f, 1f);
        private static readonly Color VoteTextColor = new Color(0.62f, 0.90f, 0.62f, 0.95f);
        private static readonly Color NameTextColor = new Color(0.86f, 0.88f, 0.94f, 1f);

        private static readonly Color SystemBubbleColor = new Color(0.10f, 0.10f, 0.17f, 0.94f);
        private static readonly Color SystemBodyColor = new Color(0.86f, 0.88f, 0.93f, 1f);
        private static readonly Color SystemTitleColor = new Color(1f, 0.9f, 0.6f, 1f);

        private readonly float _contentWidth;
        private readonly float _sidePadding;

        private RectTransform _content;
        private TextMeshProUGUI _measure;
        private bool _measureErrorLogged;

        private int _localActor;
        private Func<int, PlayerAvatar> _resolveAvatar;
        private Func<int, int> _resolveId;
        private Func<int, Role?> _markedRole;

        private readonly List<PendingAvatar> _pendingAvatars = new List<PendingAvatar>();

        private struct PendingAvatar
        {
            public MenuPlayerListed Listed;
            public int WaitedFrames;
            public string DisplayName;
        }

        public ChatRowFactory(float contentWidth, float sidePadding)
        {
            _contentWidth = contentWidth;
            _sidePadding = sidePadding;
        }

        private float MaxBubbleWidth => (_contentWidth - _sidePadding * 2f) * BubbleWidthRatio;

        private float MaxTextWidth => MaxBubbleWidth - BubblePadding * 2f;

        public void Build(Transform measureParent)
        {
            _measure = UiKit.CreateText(measureParent, "Measure", Vector2.zero, Vector2.zero,
                string.Empty, BodyFontSize, new Color(0f, 0f, 0f, 0f), TextAlignmentOptions.TopLeft);
            _measure.enableWordWrapping = true;
            _measure.spriteAsset = EmojiSprites.Asset;
        }

        public void Attach(RectTransform content)
        {
            _content = content;
        }

        public void SetContext(int localActor, Func<int, PlayerAvatar> resolveAvatar,
                               Func<int, int> resolveId, Func<int, Role?> markedRole)
        {
            _localActor = localActor;
            _resolveAvatar = resolveAvatar;
            _resolveId = resolveId;
            _markedRole = markedRole;
        }

        public GameObject CreateRow(float topY, ChatLayoutBlock block, ChatLogEntry entry, bool groupHead)
        {
            if (block.Kind == ChatBlockKind.Vote) return CreateVoteRow(topY, block, entry);

            if (entry.Kind == ChatEntryKind.System)
            {
                return block.Kind == ChatBlockKind.Speaker
                    ? CreateSystemSpeakerRow(topY, block, entry)
                    : CreateSystemBubble(topY, block, entry, groupHead);
            }

            bool isSelf = entry.Actor == _localActor;
            return block.Kind == ChatBlockKind.Speaker
                ? CreateSpeakerRow(topY, block, entry, isSelf)
                : CreateBubble(topY, block, entry, isSelf, groupHead);
        }

        public ChatBlockSize MeasureBubble(ChatLogEntry entry)
        {
            bool system = entry.Kind == ChatEntryKind.System;
            string body = system ? ComposeSystemText(entry) : entry.Text;

            float textWidth = MaxTextWidth;
            float textHeight = BodyFontSize;
            try
            {
                if (_measure != null)
                {
                    if (!system)
                    {
                        Vector2 unconstrained = _measure.GetPreferredValues(body, MaxTextWidth, 0f);
                        textWidth = Mathf.Clamp(unconstrained.x, BodyFontSize, MaxTextWidth);
                    }
                    textHeight = Mathf.Max(BodyFontSize, _measure.GetPreferredValues(body, textWidth, 0f).y);
                }
            }
            catch (Exception e)
            {
                if (!_measureErrorLogged)
                {
                    _measureErrorLogged = true;
                    WLog.Line("chat_measure_error", secret: false, ("err", e.Message));
                }
            }
            return new ChatBlockSize(textWidth + BubblePadding * 2f, textHeight + BubblePadding * 2f);
        }

        public RectTransform CreateBlock(string name, Vector2 size, float topY, float anchorX)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_content, false);
            rect.anchorMin = new Vector2(anchorX, 1f);
            rect.anchorMax = new Vector2(anchorX, 1f);
            rect.pivot = new Vector2(anchorX, 1f);
            rect.sizeDelta = size;
            float x = anchorX <= 0f ? _sidePadding : (anchorX >= 1f ? -_sidePadding : 0f);
            rect.anchoredPosition = new Vector2(x, topY);
            return rect;
        }

        public void TickAvatarFreeze()
        {
            for (int i = _pendingAvatars.Count - 1; i >= 0; i--)
            {
                PendingAvatar pending = _pendingAvatars[i];
                MenuPlayerListed listed = pending.Listed;
                if (listed == null)
                {
                    _pendingAvatars.RemoveAt(i);
                    continue;
                }
                if (!listed.gameObject.activeInHierarchy) continue;

                pending.WaitedFrames++;
                if (!ListedAvatar.IsReady(listed) && pending.WaitedFrames < AvatarFreezeWaitFrames)
                {
                    _pendingAvatars[i] = pending;
                    continue;
                }

                ListedAvatar.Freeze(listed, NameTextColor);
                if (listed.playerName != null && !string.IsNullOrEmpty(pending.DisplayName))
                {
                    listed.playerName.text = pending.DisplayName;
                }
                _pendingAvatars.RemoveAt(i);
            }
        }

        public void ClearPending()
        {
            _pendingAvatars.Clear();
        }

        public void Detach()
        {
            _content = null;
            _measure = null;
            _measureErrorLogged = false;
            _resolveAvatar = null;
            _resolveId = null;
            _markedRole = null;
            _pendingAvatars.Clear();
        }

        private GameObject CreateSpeakerRow(float topY, ChatLayoutBlock block, ChatLogEntry entry, bool isSelf)
        {
            RectTransform row = CreateBlock("Speaker", new Vector2(block.Width, block.Height),
                topY, isSelf ? 1f : 0f);

            string displayName = DecorateName(entry.Actor, entry.Name);
            MenuPlayerListed listed = TryCreateAvatar(row, entry, displayName,
                new Vector2(AvatarHolderX(block.Width, isSelf), 0f),
                new Vector2(block.Width, block.Height), AvatarPrefabScale);

            if (listed == null)
            {
                CreateNameLabel(row, displayName, Vector2.zero,
                    new Vector2(block.Width, block.Height),
                    isSelf ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft);
            }

            return row.gameObject;
        }

        private GameObject CreateVoteRow(float topY, ChatLayoutBlock block, ChatLogEntry entry)
        {
            RectTransform row = CreateBlock("Vote", new Vector2(block.Width, block.Height), topY, 0.5f);

            float avatarX = -block.Width * 0.5f + VoteAvatarWidth * 0.5f;
            var labelPos = new Vector2(block.Width * 0.5f - VoteTextWidth * 0.5f, 0f);
            var labelSize = new Vector2(VoteTextWidth, block.Height);

            string displayName = DecorateName(entry.Actor, entry.Name);
            MenuPlayerListed listed = TryCreateAvatar(row, entry, displayName, new Vector2(avatarX, 0f),
                new Vector2(VoteAvatarWidth, block.Height), VoteAvatarScale);

            if (listed == null)
            {
                CreateNameLabel(row, displayName, new Vector2(avatarX, 0f),
                    new Vector2(VoteAvatarWidth, block.Height), TextAlignmentOptions.MidlineLeft);
            }

            TextMeshProUGUI text = UiKit.CreateText(row, "VoteText", labelPos, labelSize,
                entry.Text, VoteTextFontSize, VoteTextColor, TextAlignmentOptions.MidlineRight);
            text.enableWordWrapping = false;

            return row.gameObject;
        }

        private GameObject CreateSystemSpeakerRow(float topY, ChatLayoutBlock block, ChatLogEntry entry)
        {
            RectTransform row = CreateBlock("SystemSpeaker", new Vector2(block.Width, block.Height), topY, 0f);

            Sprite face = AssetCatalog.GetSprite(entry.Icon)
                ?? AssetCatalog.GetSprite(SystemFallbackFace);
            float nameX = 0f;
            float nameWidth = block.Width;
            if (face != null)
            {
                Image icon = UiKit.CreateImage(row, "Icon",
                    new Vector2(-block.Width * 0.5f + SystemIconSize * 0.5f, 0f),
                    new Vector2(SystemIconSize, SystemIconSize), Color.white);
                icon.sprite = face;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                nameX = (SystemIconSize + SystemIconGap) * 0.5f;
                nameWidth = block.Width - SystemIconSize - SystemIconGap;
            }

            CreateNameLabel(row, entry.Name, new Vector2(nameX, 0f),
                new Vector2(nameWidth, block.Height), TextAlignmentOptions.MidlineLeft);
            return row.gameObject;
        }

        private GameObject CreateSystemBubble(float topY, ChatLayoutBlock block, ChatLogEntry entry, bool groupHead)
        {
            RectTransform rect = CreateBlock("SystemBubble", new Vector2(block.Width, block.Height), topY, 0f);
            var bubble = rect.gameObject.AddComponent<Image>();
            bubble.color = SystemBubbleColor;
            bubble.sprite = UiKit.RoundedRectSprite();
            bubble.type = Image.Type.Sliced;
            bubble.raycastTarget = false;
            if (groupHead) CreateTail(rect, SystemBubbleColor, isSelf: false);

            float textWidth = block.Width - BubblePadding * 2f;
            float textHeight = block.Height - BubblePadding * 2f;
            TextMeshProUGUI label = UiKit.CreateText(rect, "Text", Vector2.zero,
                new Vector2(textWidth, textHeight), ComposeSystemText(entry), BodyFontSize,
                SystemBodyColor, TextAlignmentOptions.TopLeft);
            label.enableWordWrapping = true;
            label.spriteAsset = EmojiSprites.Asset;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(textWidth, textHeight);
            labelRect.anchoredPosition = new Vector2(BubblePadding, -BubblePadding);

            return rect.gameObject;
        }

        private static string ComposeSystemText(ChatLogEntry entry)
        {
            string body = EmojiSprites.Substitute(entry.Text);
            if (string.IsNullOrEmpty(entry.Title)) return body;
            return "<b><color=#" + ColorUtility.ToHtmlStringRGB(SystemTitleColor) + ">"
                   + EmojiSprites.Substitute(entry.Title) + "</color></b>\n" + body;
        }

        private GameObject CreateBubble(float topY, ChatLayoutBlock block, ChatLogEntry entry,
                                        bool isSelf, bool groupHead)
        {
            bool dead = entry.Speaker == ChatSpeaker.Dead;
            Color bubbleColor = dead ? DeadBubbleColor : (isSelf ? SelfBubbleColor : AliveBubbleColor);
            Color bodyColor = dead ? DeadBodyColor : (isSelf ? SelfBodyColor : AliveBodyColor);

            RectTransform rect = CreateBlock("Bubble", new Vector2(block.Width, block.Height),
                topY, isSelf ? 1f : 0f);
            var bubble = rect.gameObject.AddComponent<Image>();
            bubble.color = bubbleColor;
            bubble.sprite = UiKit.RoundedRectSprite();
            bubble.type = Image.Type.Sliced;
            bubble.raycastTarget = false;
            if (groupHead) CreateTail(rect, bubbleColor, isSelf);

            float textWidth = block.Width - BubblePadding * 2f;
            float textHeight = block.Height - BubblePadding * 2f;
            TextMeshProUGUI label = UiKit.CreateText(rect, "Text", Vector2.zero,
                new Vector2(textWidth, textHeight), entry.Text, BodyFontSize,
                bodyColor, TextAlignmentOptions.TopLeft);
            label.enableWordWrapping = true;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(textWidth, textHeight);
            labelRect.anchoredPosition = new Vector2(BubblePadding, -BubblePadding);

            return rect.gameObject;
        }

        private static void CreateTail(RectTransform bubble, Color color, bool isSelf)
        {
            Image image = UiKit.CreateImage(bubble, "Tail", Vector2.zero,
                new Vector2(TailWidth, TailHeight), color);
            image.sprite = UiKit.BubbleTailSprite();

            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(isSelf ? 1f : 0f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(isSelf ? -TailInset : TailInset, -TailTopInset);
            if (isSelf) rect.localScale = new Vector3(-1f, 1f, 1f);
        }

        private static float AvatarHolderX(float blockWidth, bool isSelf)
        {
            return isSelf ? 0f : -blockWidth * 0.5f - ListedAvatar.OriginOffsetX * AvatarPrefabScale;
        }

        private MenuPlayerListed TryCreateAvatar(RectTransform row, ChatLogEntry entry, string displayName,
                                                 Vector2 holderPos, Vector2 holderSize, float scale)
        {
            PlayerAvatar avatar = _resolveAvatar != null ? _resolveAvatar(entry.Actor) : null;
            if (avatar == null) return null;

            holderPos.y -= (ListedAvatar.RowHeight * 0.5f + AvatarOriginTrim) * scale;
            RectTransform holder = UiKit.CreateRect(row, "AvatarHolder", holderPos, holderSize);
            holder.localScale = new Vector3(scale, scale, 1f);
            MenuPlayerListed listed = ListedAvatar.TryCreate(holder, avatar, displayName);
            if (listed == null)
            {
                UnityEngine.Object.Destroy(holder.gameObject);
                return null;
            }
            QueueAvatarFreeze(listed, displayName);
            return listed;
        }

        private static void CreateNameLabel(RectTransform row, string playerName,
                                            Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            TextMeshProUGUI name = UiKit.CreateText(row, "Name", pos, size,
                playerName, NameFontSize, NameTextColor, align);
            name.enableWordWrapping = false;
        }

        private void QueueAvatarFreeze(MenuPlayerListed listed, string displayName)
        {
            _pendingAvatars.Add(new PendingAvatar
            {
                Listed = listed,
                WaitedFrames = 0,
                DisplayName = displayName,
            });
        }

        private string DecorateName(int actor, string rawName)
        {
            int id = 0;
            try { id = _resolveId != null ? _resolveId(actor) : 0; }
            catch { id = 0; }
            if (id <= 0) return rawName;

            Role? marked = null;
            try { marked = _markedRole != null ? _markedRole(actor) : null; }
            catch { marked = null; }

            string number = id.ToString();
            string hex = MarkerColors.HexFor(marked);
            if (hex != null) number = "<color=" + hex + ">" + number + "</color>";
            return Texts.Format(TextId.IdNameFormat, number, rawName ?? string.Empty);
        }
    }
}
