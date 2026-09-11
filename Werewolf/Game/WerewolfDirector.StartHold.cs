using Werewolf.Core;
using Werewolf.Scenarios.Game;
using Werewolf.Vanilla;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {

        private readonly StartHoldGate _startHold = new StartHoldGate();
        private readonly StartHoldOverlay _startHoldOverlay = new StartHoldOverlay();

        private const float StartHoldKeepAliveSeconds = 0.1f;

        internal bool IsWerewolfRoundExpected()
        {
            if (ScenarioRun.Active) return true;
            if (Session.IsHost) return LiveConfig.WerewolfModeEnabled();
            return _lobbySettingsFeature.PanelModeEnabled;
        }

        private void TickStartHold()
        {
            bool inRunLevel = Session.InRunLevel;
            bool operable = inRunLevel && Session.IsGameStateMain;

            StartHoldPhase before = _startHold.Phase;
            bool freeze = _startHold.Tick(
                inRunLevel, operable, IsWerewolfRoundExpected(),
                _roundMirror.GameStartUnixMs > 0);

            if (before != StartHoldPhase.Holding && _startHold.Phase == StartHoldPhase.Holding)
            {
                if (_uiManager.EnsureCreated(gameObject)) EnsurePanelBuilt(_startHoldOverlay);
            }

            if (freeze)
            {
                Inputs.DisableMovement();
                LocalPlayer.DisableGrab(StartHoldKeepAliveSeconds);
            }

            bool showWaiting = freeze
                && ClientPhase != GamePhase.RoleReveal
                && ClientPhase != GamePhase.PreplayAborted;
            _startHoldOverlay.Tick(showWaiting);
        }
    }
}
