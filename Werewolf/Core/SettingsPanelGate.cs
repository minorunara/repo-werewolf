namespace Werewolf.Core
{
    public readonly struct SettingsPanelView
    {
        public bool Panel { get; }

        public bool Hint { get; }

        public bool HintAtTop { get; }

        public bool Elevated { get; }

        public SettingsPanelView(bool panel, bool hint, bool hintAtTop, bool elevated)
        {
            Panel = panel;
            Hint = hint;
            HintAtTop = hintAtTop;
            Elevated = elevated;
        }

        public static SettingsPanelView Hidden => default;
    }

    public static class SettingsPanelGate
    {
        public static bool IsMatchScope(GamePhase phase, bool inRunLevel)
            => inRunLevel && (phase == GamePhase.RoleReveal
                              || phase == GamePhase.Play || phase == GamePhase.Meeting);

        public static SettingsPanelView Lobby(bool inLobby, bool hasData, bool modeEnabled, bool userHidden)
        {
            bool baseShow = inLobby && hasData && modeEnabled;
            if (!baseShow) return SettingsPanelView.Hidden;
            return new SettingsPanelView(panel: !userHidden, hint: userHidden, hintAtTop: false, elevated: false);
        }

        public static SettingsPanelView Match(bool escMenuOpen, bool hasData, bool manualOpen, bool userHidden)
        {
            bool baseShow = escMenuOpen && hasData && !manualOpen;
            if (!baseShow) return SettingsPanelView.Hidden;
            return new SettingsPanelView(panel: !userHidden, hint: userHidden, hintAtTop: true, elevated: true);
        }
    }
}
