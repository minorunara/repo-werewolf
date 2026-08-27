using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    internal static class RevealPanelKit
    {
        internal const float EntranceStartScale = 2.6f;

        internal static RectTransform BuildFullscreenRoot(
            Transform layerRoot, string name, Color backdropColor, out CanvasGroup group)
        {
            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            RectTransform rect = UiKit.CreateStretched(layerRoot, name);

            group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            UiKit.CreateBackdrop(rect, "Backdrop", backdropColor);
            return rect;
        }

        internal static TextMeshProUGUI CreateStampTitle(
            RectTransform parent, Vector2 pos, Vector2 size, string text, float fontSize,
            Color faceColor, Color faceOutlineColor, float faceOutlineWidth,
            out TextMeshProUGUI back)
        {
            back = UiKit.CreateText(parent, "TitleBack", pos, size, text, fontSize,
                Color.black, TextAlignmentOptions.Center);
            back.outlineColor = Color.white;
            back.outlineWidth = 0.30f;
            back.fontStyle = FontStyles.Bold;

            TextMeshProUGUI front = UiKit.CreateText(parent, "Title", pos, size, text, fontSize,
                faceColor, TextAlignmentOptions.Center);
            front.outlineColor = faceOutlineColor;
            front.outlineWidth = faceOutlineWidth;
            front.fontStyle = FontStyles.Bold;
            return front;
        }

        internal static void SetIcon(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = sprite != null;
        }

        internal static IEnumerator StampEntrance(
            RectTransform target, CanvasGroup fadeGroup, float durationSec)
        {
            target.localScale = Vector3.one * EntranceStartScale;
            return UiTween.Parallel(
                UiTween.Scale(target, EntranceStartScale, 1f, durationSec, UiTween.EaseIn()),
                UiTween.Fade(fadeGroup, 0f, 1f, durationSec, UiTween.EaseIn()));
        }

        internal static void InvokeCallback(Action callback, string errorLogKey)
        {
            try { callback?.Invoke(); }
            catch (Exception e)
            {
                WLog.Line(errorLogKey, secret: false, ("err", e.Message));
            }
        }
    }
}
