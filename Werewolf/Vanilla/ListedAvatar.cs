using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class ListedAvatar
    {
        internal const float OriginOffsetX = -23f;

        internal const float RowHeight = 22f;

        internal const float NameOffsetX = 37f;

        internal static float NameStartFromHolder(float scale) => (OriginOffsetX + NameOffsetX) * scale;

        private const string CrownImageName = "Arena Crown";
        private const string SteamImageName = "Steam Icon";

        private static readonly Color DimNameColor = new Color(0.2f, 0.2f, 0.2f);

        private static readonly AccessTools.FieldRef<MenuPlayerListed, int> _listSpotRef =
            GameRefs.MenuPlayerListed_listSpot;
        private static readonly AccessTools.FieldRef<MenuPlayerHead, bool> _headIsTalkingRef =
            GameRefs.MenuPlayerHead_isTalking;
        private static readonly AccessTools.FieldRef<MenuPlayerHead, RectTransform> _headTransformRef =
            GameRefs.MenuPlayerHead_headTransform;

        public static ListedRow TryCreate(Transform holder, PlayerHandle player, string playerName)
        {
            PlayerAvatar avatar = player.Avatar;
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
                return new ListedRow(listed);
            }
            catch (Exception e)
            {
                WLog.Line("listed_visual_failed", secret: false, ("err", e.Message));
                return null;
            }
        }

        public static ListedRow TryCreateStatic(Transform holder, string playerName, int colorIndex)
        {
            try
            {
                GameObject prefab = ResolveRowPrefab();
                if (prefab == null) return null;

                GameObject visual = UnityEngine.Object.Instantiate(prefab, holder);
                MenuPlayerListed listed = visual.GetComponent<MenuPlayerListed>();
                MenuPlayerHead head = listed != null ? listed.playerHead : null;
                if (head == null)
                {
                    UnityEngine.Object.Destroy(visual);
                    return null;
                }
                listed.enabled = false;
                head.enabled = false;
                StripChatDisplay(visual);
                foreach (MenuPlayerListedSteam steam in visual.GetComponentsInChildren<MenuPlayerListedSteam>(true))
                {
                    steam.gameObject.SetActive(false);
                }

                if (listed.leftCrown != null) listed.leftCrown.SetActive(false);
                if (listed.rightCrown != null) listed.rightCrown.SetActive(false);
                if (head.headRight != null) head.headRight.gameObject.SetActive(true);
                if (head.headLeft != null) head.headLeft.gameObject.SetActive(false);
                if (head.muteIconTransform != null) head.muteIconTransform.localScale = Vector3.zero;

                Color nameColor = listed.brightName ? Color.white : DimNameColor;
                if (listed.playerName != null)
                {
                    listed.playerName.text = playerName ?? string.Empty;
                    listed.playerName.color = nameColor;
                }
                listed.transform.localPosition = SemiFunc.RunIsLobbyMenu()
                    ? Vector3.zero
                    : new Vector3(OriginOffsetX, 0f, 0f);

                Color headColor = BotColors.ColorAt(colorIndex);
                TintHead(head, headColor);
                return new ListedRow(listed, headColor, nameColor);
            }
            catch (Exception e)
            {
                WLog.Line("listed_static_failed", secret: false, ("err", e.Message));
                return null;
            }
        }

        public static void SetStaticShade(ListedRow row, bool dead, Color deadNameColor)
        {
            MenuPlayerListed listed = row?.Listed;
            if (listed == null || !row.IsStatic || listed.playerHead == null) return;

            TintHead(listed.playerHead, dead ? Color.Lerp(row.StaticHeadColor, Color.black, 0.9f) : row.StaticHeadColor);
            row.SetNameColor(dead ? deadNameColor : row.StaticNameColor);
        }

        private static void TintHead(MenuPlayerHead head, Color color)
        {
            foreach (RawImage image in head.GetComponentsInChildren<RawImage>(true))
            {
                if (image == null || image.name == CrownImageName || image.name == SteamImageName) continue;
                image.color = color;
            }
        }

        public static bool IsReady(ListedRow row)
        {
            if (row != null && row.IsStatic) return row.Exists;
            MenuPlayerListed listed = row?.Listed;
            if (listed == null || listed.playerHead == null) return false;
            return _headTransformRef != null && _headTransformRef(listed.playerHead) != null;
        }

        public static void Freeze(ListedRow row, Color nameColor)
        {
            MenuPlayerListed listed = row?.Listed;
            if (listed == null) return;
            try
            {
                SuppressSpeech(row, nameColor);
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

        public static void SuppressSpeech(ListedRow row, Color mutedNameColor)
        {
            MenuPlayerListed listed = row?.Listed;
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

        public static void ResumeSpeech(ListedRow row)
        {
            MenuPlayerListed listed = row?.Listed;
            if (listed == null || row.IsStatic) return;
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
