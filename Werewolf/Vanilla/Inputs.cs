using System;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal static class Inputs
    {
        public static bool Down(GameKey key) => SemiFunc.InputDown(ToVanilla(key));

        public static bool Hold(GameKey key) => SemiFunc.InputHold(ToVanilla(key));

        public static bool Up(GameKey key) => SemiFunc.InputUp(ToVanilla(key));

        public static bool NoTextInputsActive => SemiFunc.NoTextInputsActive();

        public static void DisableMovement() => SemiFunc.InputDisableMovement();

        public static void UnlockCursor(float seconds) => SemiFunc.CursorUnlock(seconds);

        public static string ReplaceDisplayTags(string text, string prefix, string suffix)
            => TryReplaceDisplayTags(text, prefix, suffix, out string replaced) ? replaced : text;

        public static bool TryReplaceDisplayTags(string text, string prefix, string suffix, out string replaced)
        {
            InputManager input = InputManager.instance;
            if (input == null)
            {
                replaced = text;
                return false;
            }
            replaced = input.InputDisplayReplaceTags(text, prefix, suffix);
            return true;
        }

        private static InputKey ToVanilla(GameKey key)
        {
            switch (key)
            {
                case GameKey.Menu: return InputKey.Menu;
                case GameKey.Interact: return InputKey.Interact;
                case GameKey.Grab: return InputKey.Grab;
                case GameKey.Confirm: return InputKey.Confirm;
                case GameKey.ToggleMute: return InputKey.ToggleMute;
                default: throw new ArgumentOutOfRangeException(nameof(key), key, null);
            }
        }
    }
}
