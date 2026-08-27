using TMPro;
using UnityEngine;

namespace Werewolf.Vanilla
{
    internal static class UiText
    {
        public static Vector2 PreferredAutoscaledSize(TextMeshProUGUI text, float maxWidth, float maxHeight, int iterations)
            => SemiFunc.PreferredAutoscaledTextSize(text, maxWidth, maxHeight, iterations);

        public static TMP_FontAsset ChatFontOrNull
            => ChatUI.instance != null && ChatUI.instance.chatText != null ? ChatUI.instance.chatText.font : null;
    }
}
