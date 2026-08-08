using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class VoteRow
    {
        private const float PrefabScale = 2.5f;

        private static readonly Color BgAliveColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color BgDeadColor = new Color(0.05f, 0.05f, 0.05f, 0.7f);
        private static readonly Color BgExecutedColor = new Color(0.25f, 0.02f, 0.02f, 0.7f);
        private static readonly Color BgDisconnectedColor = new Color(0.1f, 0.1f, 0.15f, 0.7f);
        private static readonly Color DeadTextColor = new Color(0.7f, 0.7f, 0.7f, 0.9f);
        private static readonly Color VotedMarkColor = new Color(0.35f, 0.95f, 0.35f, 0.95f);
        private static readonly Color VoteButtonEnabledColor = new Color(0.55f, 0.12f, 0.12f, 0.9f);
        private static readonly Color VoteButtonDisabledColor = new Color(0.25f, 0.2f, 0.2f, 0.6f);
        private static readonly Color VoteButtonLabelDisabledColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color VoteButtonHoverColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        private static readonly ButtonPalette VoteButtonPalette = new ButtonPalette(
            VoteButtonEnabledColor, VoteButtonHoverColor, VoteButtonDisabledColor, VoteButtonLabelDisabledColor);
        private static readonly Color VoteCountColor = new Color(1f, 0.9f, 0.6f, 1f);
        private static readonly Color WerewolfMarkerBgColor = new Color(0.75f, 0.1f, 0.1f, 0.95f);
        private static readonly Color BomberMarkerBgColor = new Color(0.8f, 0.45f, 0.08f, 0.95f);
        private static readonly Color TalkIdleColor = new Color(0.35f, 0.35f, 0.35f);
        private static readonly Color TalkActiveColor = new Color(0.35f, 0.95f, 0.35f);

        private const int DeadCueSuppressWaitFrames = 120;

        private static readonly AccessTools.FieldRef<PlayerAvatar, PlayerVoiceChat> _voiceChatRef =
            GameRefs.PlayerAvatar_voiceChat;
        private static readonly AccessTools.FieldRef<PlayerAvatar, bool> _voiceChatFetchedRef =
            GameRefs.PlayerAvatar_voiceChatFetched;
        private static readonly AccessTools.FieldRef<PlayerVoiceChat, bool> _isTalkingRef =
            GameRefs.PlayerVoiceChat_isTalking;

        private static Func<int, bool> s_showDeadCues;

        private static Func<int, int> s_participantId;

        private readonly PlayerAvatar _avatar;
        private GameObject _root;
        private Image _background;
        private TextMeshProUGUI _fallbackName;
        private Image _talkIndicator;
        private TextMeshProUGUI _votedMark;
        private Image _voteButtonBg;
        private TextMeshProUGUI _voteButtonLabel;
        private TextMeshProUGUI _voteCountLabel;
        private Image _teamMarkerBg;
        private TextMeshProUGUI _teamMarkerLabel;
        private TextMeshProUGUI _idLabel;
        private Image _statusIcon;
        private Image _hostIcon;
        private Image _myVoteIcon;
        private TextMeshProUGUI _scatterBadge;
        private Vector2 _rowSize;
        private MenuPlayerListed _listed;
        private bool _deadCuesHidden;
        private int _deadCueWaitFrames;
        private bool _prefabUsed;
        private bool _voteButtonVisible;
        private bool _voteButtonEnabled;
        private bool _selected;
        private bool _armed;

        public int ActorNumber { get; }

        public string PlayerName { get; }

        public bool IsBot { get; }

        public RectTransform Root => _root != null ? (RectTransform)_root.transform : null;

        public RectTransform VoteButtonRect => _voteButtonBg != null ? _voteButtonBg.rectTransform : null;

        public bool VoteButtonActive => _root != null && _voteButtonVisible && _voteButtonEnabled;

        public bool PrefabUsed => _prefabUsed;

        private VoteRow(WPlayer player, PlayerAvatar avatar)
        {
            ActorNumber = player.ActorNumber;
            PlayerName = player.Name ?? $"#{player.ActorNumber}";
            IsBot = player.IsBot;
            _avatar = avatar;
        }

        public static VoteRow Build(Transform parent, WPlayer player, Func<int, PlayerAvatar> resolveAvatar, Vector2 size)
        {
            var row = new VoteRow(player, ResolveAvatarSafe(player, resolveAvatar));
            row.BuildInternal(parent, size);
            return row;
        }

        private static PlayerAvatar ResolveAvatarSafe(WPlayer player, Func<int, PlayerAvatar> resolveAvatar)
        {
            if (player.IsBot || resolveAvatar == null) return null;
            try { return resolveAvatar(player.ActorNumber); }
            catch { return null; }
        }

        private void BuildInternal(Transform parent, Vector2 size)
        {
            RectTransform rect = UiKit.CreateRect(parent, $"WW_VoteRow_{ActorNumber}", Vector2.zero, size);
            _root = rect.gameObject;
            _rowSize = size;

            _background = UiKit.CreateImage(rect, "Bg", Vector2.zero, size, BgAliveColor);

            _prefabUsed = !IsBot && _avatar != null && TryBuildFromPrefab(rect, size);
            if (!_prefabUsed)
            {
                BuildFallbackVisual(rect, size);
            }

            float rightX = size.x * 0.5f;

            _votedMark = UiKit.CreateText(rect, "Voted", new Vector2(rightX - 120f, 0f), new Vector2(36f, size.y),
                "✓", 26f, VotedMarkColor, TextAlignmentOptions.Center);
            _votedMark.gameObject.SetActive(false);

            Vector2 btnSize = new Vector2(84f, size.y - 16f);
            _voteButtonBg = UiKit.CreateImage(rect, "VoteButton", new Vector2(rightX - 55f, 0f),
                btnSize, VoteButtonEnabledColor);
            _voteButtonLabel = UiKit.CreateText(_voteButtonBg.rectTransform, "Label", Vector2.zero,
                btnSize, Texts.Get(TextId.VoteVoteLabel), 22f, Color.white, TextAlignmentOptions.Center);
            Sprite voteBtnIcon = AssetCatalog.GetSprite("icon_btn_vote");
            if (voteBtnIcon != null)
            {
                Image icon = UiKit.CreateImage(_voteButtonBg.rectTransform, "Icon",
                    new Vector2(-btnSize.x / 2f + 26f / 2f + 4f, 0f),
                    new Vector2(26f, 26f), Color.white);
                icon.sprite = voteBtnIcon;
                icon.preserveAspect = true;
            }

            _voteCountLabel = UiKit.CreateText(rect, "Count", new Vector2(rightX - 120f, 0f), new Vector2(56f, size.y),
                "", 22f, VoteCountColor, TextAlignmentOptions.Center);
            _voteCountLabel.gameObject.SetActive(false);

            float leftX2 = -size.x * 0.5f;
            int participantId = ResolveParticipantIdSafe(ActorNumber);
            if (participantId > 0)
            {
                _idLabel = UiKit.CreateText(rect, "IdLabel", new Vector2(leftX2 + 14f, 0f),
                    new Vector2(28f, size.y), participantId.ToString(), 22f, VoteCountColor,
                    TextAlignmentOptions.Center);
                _idLabel.enableWordWrapping = false;
            }

            _teamMarkerBg = UiKit.CreateImage(rect, "WwMarker", new Vector2(leftX2 + 40f, size.y * 0.5f - 14f),
                new Vector2(24f, 24f), WerewolfMarkerBgColor);
            _teamMarkerLabel = UiKit.CreateText(_teamMarkerBg.rectTransform, "Label", Vector2.zero,
                new Vector2(24f, 24f), Texts.Get(TextId.VoteWerewolfMarkerLabel), 18f, Color.white, TextAlignmentOptions.Center);
            _teamMarkerBg.gameObject.SetActive(false);

            _statusIcon = UiKit.CreateImage(rect, "StatusIcon",
                new Vector2(leftX2 + 82f, 0f), new Vector2(70f, 70f), Color.white);
            _statusIcon.gameObject.SetActive(false);

            _hostIcon = UiKit.CreateImage(rect, "HostIcon",
                new Vector2(rightX - 156f, 0f), new Vector2(32f, 32f), Color.white);
            Sprite hostSprite = AssetCatalog.GetSprite("icon_host_megaphone");
            if (hostSprite != null) _hostIcon.sprite = hostSprite;
            _hostIcon.gameObject.SetActive(false);

            Sprite myVoteSprite = AssetCatalog.GetSprite("icon_my_vote");
            if (myVoteSprite != null)
            {
                _myVoteIcon = UiKit.CreateImage(rect, "MyVoteIcon",
                    new Vector2(rightX - 212f, 0f), new Vector2(70f, 70f), Color.white);
                _myVoteIcon.sprite = myVoteSprite;
                _myVoteIcon.gameObject.SetActive(false);
            }

            SetVoteButtonVisible(false);
            SetStatus(RowStatus.Alive);

            WLog.Line("vote_row_built", secret: false,
                ("actor", ActorNumber), ("bot", IsBot), ("mode", _prefabUsed ? "prefab" : "fallback"));
        }

        private bool TryBuildFromPrefab(RectTransform rowRect, Vector2 size)
        {
            RectTransform holder = UiKit.CreateRect(rowRect, "AvatarHolder",
                new Vector2(-size.x * 0.5f + 85f, -40f), new Vector2(220f, size.y));
            holder.localScale = new Vector3(PrefabScale, PrefabScale, 1f);

            MenuPlayerListed listed = ListedAvatar.TryCreate(holder, _avatar, PlayerName);
            if (listed == null)
            {
                WLog.Line("vote_row_prefab_fallback", secret: false, ("actor", ActorNumber));
                UnityEngine.Object.Destroy(holder.gameObject);
                return false;
            }
            _listed = listed;
            return true;
        }

        private void BuildFallbackVisual(RectTransform rowRect, Vector2 size)
        {
            float leftX = -size.x * 0.5f;
            if (!IsBot && _avatar != null)
            {
                _talkIndicator = UiKit.CreateImage(rowRect, "Talk",
                    new Vector2(leftX + 22f, size.y * 0.5f - 14f),
                    new Vector2(14f, 14f), TalkIdleColor);
            }
            _fallbackName = UiKit.CreateText(rowRect, "Name", new Vector2(leftX + 130f, 0f),
                new Vector2(190f, size.y), PlayerName, 24f, Color.white, TextAlignmentOptions.MidlineLeft);
        }

        public void SetStatus(RowStatus status)
        {
            if (_root == null) return;
            Color bg;
            switch (status)
            {
                case RowStatus.Dead: bg = BgDeadColor; break;
                case RowStatus.Executed: bg = BgExecutedColor; break;
                case RowStatus.Disconnected: bg = BgDisconnectedColor; break;
                default: bg = BgAliveColor; break;
            }
            _background.color = bg;
            SetStatusIcon(status);
            if (status != RowStatus.Alive)
            {
                SetVoteButtonVisible(false);
                if (_fallbackName != null) _fallbackName.color = DeadTextColor;
            }
            else if (_fallbackName != null)
            {
                _fallbackName.color = Color.white;
            }
        }

        private void SetStatusIcon(RowStatus status)
        {
            if (_statusIcon == null) return;
            string key;
            switch (status)
            {
                case RowStatus.Dead: key = "icon_status_dead"; break;
                case RowStatus.Executed: key = "icon_status_executed"; break;
                case RowStatus.Disconnected: key = "icon_status_disconnected"; break;
                default: key = null; break;
            }
            if (key == null)
            {
                _statusIcon.gameObject.SetActive(false);
                return;
            }
            Sprite sprite = AssetCatalog.GetSprite(key);
            if (sprite == null)
            {
                _statusIcon.gameObject.SetActive(false);
                return;
            }
            _statusIcon.sprite = sprite;
            _statusIcon.gameObject.SetActive(true);
        }

        public void SetHostMarker(bool isHost)
        {
            if (_hostIcon == null) return;
            if (_hostIcon.gameObject.activeSelf != isHost)
            {
                _hostIcon.gameObject.SetActive(isHost);
            }
        }

        public void SetMyVoteMarker(bool visible)
        {
            if (_myVoteIcon == null) return;
            if (_myVoteIcon.gameObject.activeSelf != visible)
            {
                _myVoteIcon.gameObject.SetActive(visible);
            }
        }

        private static readonly Color ScatterBadgeSpinColor = new Color(0.8f, 0.8f, 0.85f, 0.9f);

        public void SetScatterBadge(string text, bool settled)
        {
            if (_root == null) return;
            if (text == null)
            {
                if (_scatterBadge != null) _scatterBadge.gameObject.SetActive(false);
                return;
            }
            if (_scatterBadge == null)
            {
                _scatterBadge = UiKit.CreateText((RectTransform)_root.transform, "ScatterBadge",
                    new Vector2(_rowSize.x * 0.5f - 55f, 0f), new Vector2(110f, _rowSize.y),
                    text, 28f, ScatterBadgeSpinColor, TextAlignmentOptions.Center);
                _scatterBadge.enableWordWrapping = false;
            }
            _scatterBadge.text = text;
            _scatterBadge.color = settled ? VoteCountColor : ScatterBadgeSpinColor;
            _scatterBadge.fontStyle = settled ? FontStyles.Bold : FontStyles.Normal;
            if (!_scatterBadge.gameObject.activeSelf) _scatterBadge.gameObject.SetActive(true);
        }

        public void SetVoted(bool voted)
        {
            if (_votedMark == null) return;
            bool countShown = _voteCountLabel != null && _voteCountLabel.gameObject.activeSelf;
            _votedMark.gameObject.SetActive(voted && !countShown);
        }

        public void SetVoteButtonVisible(bool visible)
        {
            _voteButtonVisible = visible;
            if (_voteButtonBg != null) _voteButtonBg.gameObject.SetActive(visible);
        }

        public void SetVoteButtonEnabled(bool enabled)
        {
            _voteButtonEnabled = enabled;
            RefreshVoteButtonVisual(hover: false);
        }

        public void SetSelected(bool selected)
        {
            if (_selected == selected) return;
            _selected = selected;
            RefreshVoteButtonVisual(hover: false);
        }

        public void SetArmed(bool armed)
        {
            if (_armed == armed) return;
            _armed = armed;
            if (_voteButtonLabel != null) _voteButtonLabel.text = armed ? Texts.Get(TextId.VoteConfirmLabel) : Texts.Get(TextId.VoteVoteLabel);
            RefreshVoteButtonVisual(hover: false);
        }

        private void RefreshVoteButtonVisual(bool hover)
        {
            if (_voteButtonBg == null) return;
            ButtonVisual.Resolve(VoteButtonPalette,
                armed: _armed && _voteButtonEnabled, hover: hover, selected: _selected, enabled: _voteButtonEnabled,
                out Color bg, out Color label);
            _voteButtonBg.color = bg;
            if (_voteButtonLabel != null) _voteButtonLabel.color = label;
        }

        public void SetTeamMarker(Role? role)
        {
            if (_idLabel != null)
            {
                _idLabel.color = MarkerColors.ForRole(role, VoteCountColor);
            }
            if (_teamMarkerBg == null) return;
            bool visible = role.HasValue;
            if (visible)
            {
                bool bomber = role.Value == Role.Bomber;
                _teamMarkerBg.color = bomber ? BomberMarkerBgColor : WerewolfMarkerBgColor;
                if (_teamMarkerLabel != null)
                {
                    _teamMarkerLabel.text = Texts.Get(
                        bomber ? TextId.VoteBomberMarkerLabel : TextId.VoteWerewolfMarkerLabel);
                }
            }
            if (_teamMarkerBg.gameObject.activeSelf != visible)
            {
                _teamMarkerBg.gameObject.SetActive(visible);
            }
        }

        public void SetHover(bool hover)
        {
            if (_voteButtonBg == null || !_voteButtonVisible) return;
            RefreshVoteButtonVisual(hover);
        }

        public void SetVoteCount(int count)
        {
            if (_voteCountLabel == null) return;
            if (count < 0)
            {
                _voteCountLabel.gameObject.SetActive(false);
                return;
            }
            _voteCountLabel.text = Texts.Format(TextId.VoteCountFormat, count);
            _voteCountLabel.gameObject.SetActive(true);
            if (_votedMark != null) _votedMark.gameObject.SetActive(false);
        }

        public static void SetDeadCueProvider(Func<int, bool> provider)
        {
            s_showDeadCues = provider;
        }

        public static void SetParticipantIdProvider(Func<int, int> provider)
        {
            s_participantId = provider;
        }

        private static int ResolveParticipantIdSafe(int actorNumber)
        {
            if (s_participantId == null) return 0;
            try { return s_participantId(actorNumber); }
            catch { return 0; }
        }

        public void Tick()
        {
            try
            {
                bool showCues = s_showDeadCues == null || s_showDeadCues(ActorNumber);
                TickDeadCues(showCues);
                TickFallbackTalk(showCues);
            }
            catch (Exception e)
            {
                WLog.Line("vote_row_tick_error", secret: false, ("actor", ActorNumber), ("err", e.Message));
            }
        }

        private void TickDeadCues(bool showCues)
        {
            if (_listed == null) return;
            if (showCues == !_deadCuesHidden) return;

            if (showCues)
            {
                ListedAvatar.ResumeSpeech(_listed);
            }
            else
            {
                if (!_listed.gameObject.activeInHierarchy) return;
                if (!ListedAvatar.IsReady(_listed) && _deadCueWaitFrames < DeadCueSuppressWaitFrames)
                {
                    _deadCueWaitFrames++;
                    return;
                }
                ListedAvatar.SuppressSpeech(_listed, DeadTextColor);
            }
            _deadCuesHidden = !showCues;
        }

        private void TickFallbackTalk(bool showCues)
        {
            if (_talkIndicator == null || _avatar == null) return;
            bool talking = false;
            if (showCues && HasVoiceRefs() && _voiceChatFetchedRef(_avatar))
            {
                PlayerVoiceChat vc = _voiceChatRef(_avatar);
                talking = vc != null && _isTalkingRef(vc);
            }
            _talkIndicator.color = talking ? TalkActiveColor : TalkIdleColor;
        }

        public void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
        }

        private static bool HasVoiceRefs()
        {
            return _voiceChatRef != null && _voiceChatFetchedRef != null && _isTalkingRef != null;
        }
    }
}
