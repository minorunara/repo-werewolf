using Werewolf.Core;
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
            if (SemiFunc.IsMasterClientOrSingleplayer()) return LiveWerewolfModeEnabled();
            return _lastPanelModeEnabled;
        }

        private void TickStartHold(long now)
        {
            bool inRunLevel = SemiFunc.RunIsLevel();
            bool operable = inRunLevel
                && GameDirector.instance != null
                && GameDirector.instance.currentState == GameDirector.gameState.Main;

            StartHoldPhase before = _startHold.Phase;
            long heldMsBefore = _startHold.HeldMs(now);
            bool freeze = _startHold.Tick(
                inRunLevel, operable, IsWerewolfRoundExpected(),
                _gameStartUnixMsClient > 0, now, out StartHoldRelease released);

            if (before != StartHoldPhase.Holding && _startHold.Phase == StartHoldPhase.Holding)
            {
                WLog.Line("start_hold_begin", secret: false);
                if (_uiManager.EnsureCreated(gameObject)) EnsurePanelBuilt(_startHoldOverlay);
            }
            if (released != StartHoldRelease.None)
            {
                WLog.Line("start_hold_end", secret: false,
                    ("reason", released == StartHoldRelease.GameStart ? "game_start" : "failsafe"),
                    ("heldMs", heldMsBefore));
            }
            if (_startHold.LateGameStartGapMs >= 0)
            {
                WLog.Line("start_hold_late_gamestart", secret: false,
                    ("gapMs", _startHold.LateGameStartGapMs));
            }

            if (freeze)
            {
                SemiFunc.InputDisableMovement();
                PhysGrabber grabber = PhysGrabber.instance;
                if (grabber != null) grabber.OverrideGrabDisable(StartHoldKeepAliveSeconds);
            }

            _startHoldOverlay.Tick(freeze);
        }
    }
}
