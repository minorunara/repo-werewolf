using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal static class ListedAvatar
    {
        internal const float OriginOffsetX = -23f;

        internal const float RowHeight = 22f;

        private static readonly AccessTools.FieldRef<MenuPlayerListed, int> _listSpotRef =
            GameRefs.MenuPlayerListed_listSpot;
        private static readonly AccessTools.FieldRef<MenuPlayerHead, bool> _headIsTalkingRef =
            GameRefs.MenuPlayerHead_isTalking;
        private static readonly AccessTools.FieldRef<MenuPlayerHead, RectTransform> _headTransformRef =
            GameRefs.MenuPlayerHead_headTransform;

        public static MenuPlayerListed TryCreate(Transform holder, PlayerAvatar avatar, string playerName)
        {
            if (avatar == null) return null;
            try
            {
                GameObject prefab = ResolveRowPrefab();
                if (prefab == null || !HasFieldRefs()) return null;

                GameObject visual = UnityEngine.Object.Instantiate(prefab, holder);
                MenuPlayerListed listed = visual.GetComponent<MenuPlayerListed>();
                if (listed == null)
                {
                    UnityEngine.Object.Destroy(visual);
                    return null;
                }
                StripChatDisplay(visual);
                _listSpotRef(listed) = 0;
                listed.ForcePlayer(avatar);
                if (listed.playerName != null && !string.IsNullOrEmpty(playerName))
                {
                    listed.playerName.text = playerName;
                }
                listed.transform.localPosition = SemiFunc.RunIsLobbyMenu()
                    ? Vector3.zero
                    : new Vector3(OriginOffsetX, 0f, 0f);
                return listed;
            }
            catch (Exception e)
            {
                WLog.Line("listed_visual_failed", secret: false, ("err", e.Message));
                return null;
            }
        }

        public static bool IsReady(MenuPlayerListed listed)
        {
            if (listed == null || listed.playerHead == null) return false;
            return _headTransformRef != null && _headTransformRef(listed.playerHead) != null;
        }

        public static void Freeze(MenuPlayerListed listed, Color nameColor)
        {
            if (listed == null) return;
            try
            {
                SuppressSpeech(listed, nameColor);
                if (listed.leftCrown != null) listed.leftCrown.SetActive(false);
                if (listed.rightCrown != null) listed.rightCrown.SetActive(false);

                MenuPlayerHead head = listed.playerHead;
                if (head == null) return;
                if (head.muteIconTransform != null) head.muteIconTransform.localScale = Vector3.zero;
                if (MenuManager.instance != null) MenuManager.instance.PlayerHeadRemove(head);
            }
            catch (Exception e)
            {
                WLog.Line("listed_visual_freeze_failed", secret: false, ("err", e.Message));
            }
        }

        public static void SuppressSpeech(MenuPlayerListed listed, Color mutedNameColor)
        {
            if (listed == null) return;
            listed.enabled = false;
            if (listed.playerName != null) listed.playerName.color = mutedNameColor;

            MenuPlayerHead head = listed.playerHead;
            if (head == null) return;
            head.enabled = false;
            if (head.headRight != null) head.headRight.localEulerAngles = Vector3.zero;
            if (head.headLeft != null) head.headLeft.localEulerAngles = Vector3.zero;
            if (_headIsTalkingRef != null) _headIsTalkingRef(head) = false;
        }

        public static void ResumeSpeech(MenuPlayerListed listed)
        {
            if (listed == null) return;
            listed.enabled = true;
            if (listed.playerHead != null) listed.playerHead.enabled = true;
        }

        private static void StripChatDisplay(GameObject visual)
        {
            LobbyChatUI[] chats = visual.GetComponentsInChildren<LobbyChatUI>(true);
            for (int i = 0; i < chats.Length; i++)
            {
                LobbyChatUI chat = chats[i];
                if (chat == null) continue;
                if (chat.gameObject != visual)
                {
                    chat.gameObject.SetActive(false);
                    continue;
                }
                chat.enabled = false;
                TextMeshProUGUI label = chat.GetComponent<TextMeshProUGUI>();
                if (label != null) label.enabled = false;
            }
        }

        private static GameObject ResolveRowPrefab()
        {
            MenuSpectateList list = UnityEngine.Object.FindObjectOfType<MenuSpectateList>(true);
            return list != null ? list.menuPlayerListedPrefab : null;
        }

        private static bool HasFieldRefs()
        {
            return _listSpotRef != null && _headIsTalkingRef != null && _headTransformRef != null;
        }
    }
}
