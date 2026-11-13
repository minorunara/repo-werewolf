#nullable enable
namespace Werewolf.Vanilla
{
    internal static class GameLocale
    {
        public static string? CodeOrNull
            => LocalizationManager.instance != null ? SemiFunc.LocaleGetCode() : null;
    }
}
