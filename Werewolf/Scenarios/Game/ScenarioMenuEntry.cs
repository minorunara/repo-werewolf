using System;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using MenuLib;
using MenuLib.MonoBehaviors;
using TMPro;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.UI;
using Werewolf.UI;

namespace Werewolf.Scenarios.Game
{
    internal static class ScenarioMenuEntry
    {
        private const string MenuLibGuid = "nickklmao.menulib";

        private const float RightMargin = 24f;

        private const float BottomMargin = 70f;

        private static readonly Vector2 FallbackPosition = new Vector2(470f, BottomMargin);

        private static bool _installed;

        internal static void TryInstall()
        {
            if (_installed) return;

            if (!ScenarioRun.Available)
            {
                return;
            }

            if (!Chainloader.PluginInfos.ContainsKey(MenuLibGuid))
            {
                return;
            }

            try
            {
                Install();
                _installed = true;
            }
            catch (Exception e)
            {
                WLog.Line("scenario_entry_error", secret: false,
                    ("phase", "install"), ("err", e.GetType().Name + ": " + e.Message));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Install()
        {
            MenuAPI.AddElementToMainMenu(BuildButton);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BuildButton(Transform parent)
        {
            try
            {
                string label = Texts.Get(TextId.ScenarioSelectTitle);
                REPOButton button = MenuAPI.CreateREPOButton(label, ScenarioSelection.RequestOpen, parent);
                ApplyJapaneseCapableFont(button.labelTMP, label);

                Vector2 size = button.GetLabelSize();
                Vector2 pivot = button.rectTransform.pivot;
                Vector2 position = ResolveBottomRight(parent, size, pivot);
                button.rectTransform.localPosition = new Vector3(position.x, position.y, 0f);
            }
            catch (Exception e)
            {
                WLog.Line("scenario_entry_error", secret: false,
                    ("phase", "build"), ("err", e.GetType().Name + ": " + e.Message));
            }
        }

        private static Vector2 ResolveBottomRight(Transform parent, Vector2 buttonSize, Vector2 pivot)
        {
            var rect = parent as RectTransform;
            if (rect == null) return FallbackPosition;

            Rect area = rect.rect;
            if (area.width <= 1f || area.height <= 1f) return FallbackPosition;

            return new Vector2(
                area.xMax - RightMargin - buttonSize.x * (1f - pivot.x),
                area.yMin + BottomMargin + buttonSize.y * pivot.y);
        }

        private static void ApplyJapaneseCapableFont(TextMeshProUGUI label, string text)
        {
            if (label == null || string.IsNullOrEmpty(text)) return;
            try
            {
                TMP_FontAsset current = label.font;
                if (current != null && current.HasCharacters(text, out uint[] _, searchFallbacks: true)) return;

                TMP_FontAsset replacement = UiKit.ResolveTmpFont();
                if (replacement == null || replacement == current) return;
                label.font = replacement;
            }
            catch (Exception e)
            {
                WLog.Line("scenario_entry_error", secret: false,
                    ("phase", "font"), ("err", e.GetType().Name + ": " + e.Message));
            }
        }
    }
}
