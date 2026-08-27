using Werewolf.Core;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class TutorialController : IRoundFeature
    {
        private readonly WerewolfDirector _d;

        private readonly TutorialPresenter _tutorialPresenter = new TutorialPresenter();

        private readonly TutorialBubblePanel _tutorialBubble = new TutorialBubblePanel();

        private bool _wasRoleRevealVisible;

        private int _prevBeaconCharges;

        internal TutorialController(WerewolfDirector director)
        {
            _d = director;
        }

        internal void MaybeShowTutorial(TutorialId id)
        {
            if (_d.ClientScenarioSession) return;

            bool blackCatCurseEnabled = _d.IsBlackCatCurseEnabledForClient();
            if (!TutorialCatalog.ShouldShow(id, blackCatCurseEnabled)) return;

            var bindings = Plugin.Bindings;
            if (bindings == null || bindings.IsTutorialSeen(id)) return;
            if (_tutorialPresenter.IsPending(id)) return;
            if (!_d.IsLocalAlive()
                && id != TutorialId.FirstDeath
                && id != TutorialId.BlackCatSelectedForExecution)
                return;

            string message = TutorialCatalog.Format(id, blackCatCurseEnabled);
            if (string.IsNullOrEmpty(message)) return;

            _tutorialPresenter.Enqueue(id, message);
        }

        public void Tick(long now)
        {
            TickPresenter();
            Plugin.Bindings?.TickTutorialReset();
            TickTriggers();
        }

        private void TickPresenter()
        {
            var bindings = Plugin.Bindings;
            if (bindings != null) _tutorialPresenter.FontScale = bindings.TutorialFontScale.Value;

            bool meetingUiVisible = _d.UiManagerView != null
                && _d.UiManagerView.IsLayerVisible(WerewolfUIManager.MeetingLayer);
            if (meetingUiVisible) _d.EnsurePanelBuilt(_tutorialBubble);
            _tutorialPresenter.Bubble = _tutorialBubble;

            TutorialId? shown = _tutorialPresenter.Tick(meetingUiVisible);
            if (shown.HasValue)
            {
                bindings?.MarkTutorialSeen(shown.Value);
                WLog.Line("tutorial_shown", secret: false, ("id", shown.Value));
            }
        }

        private void TickTriggers()
        {
            if (!_d.IsRoundActiveClient)
            {
                _wasRoleRevealVisible = false;
                return;
            }

            Role? localRole = _d.LocalRoleClient;
            RolesClientState rolesClient = _d.RolesClient;

            if (localRole == Role.Werewolf && rolesClient.UnlockedFlags != PerkFlags.None)
            {
                MaybeShowTutorial(TutorialId.WolfModeFirstUnlock);
            }

            if (localRole == Role.Werewolf
                && PerkFlagsUtil.Has(rolesClient.UnlockedFlags, PerkId.EnemyIgnore))
            {
                MaybeShowTutorial(TutorialId.EnemyIgnoreUnlockedAsWerewolf);
            }

            if (localRole == Role.Werewolf
                && PerkFlagsUtil.Has(rolesClient.UnlockedFlags, PerkId.NaturalHeal))
            {
                MaybeShowTutorial(TutorialId.NaturalHealUnlockedAsWerewolf);
            }

            if (localRole == Role.Werewolf && rolesClient.PlayGauge != null
                && rolesClient.PlayGauge.InformantPct > 0
                && rolesClient.RatioPermille >= rolesClient.PlayGauge.InformantPct * 10)
            {
                MaybeShowTutorial(TutorialId.InformantUnlockedAsWerewolf);
            }

            if (rolesClient.BeaconCharges > 0)
            {
                MaybeShowTutorial(TutorialId.BeaconFirstCharged);
            }

            int currentBeaconCharges = rolesClient.BeaconCharges;
            if (currentBeaconCharges < _prevBeaconCharges && localRole == Role.Werewolf)
            {
                MaybeShowTutorial(TutorialId.BeaconFirstUsedAsWerewolf);
            }
            _prevBeaconCharges = currentBeaconCharges;

            if (LastRunGate.IsLastRunActive())
            {
                MaybeShowTutorial(TutorialId.LastRunApproaching);
            }

            if (_d.ClientPhaseMirror == GamePhase.Play && !_d.MeetingClientView.MeetingActive
                && _d.ClientRoundEndUnixMs > 0)
            {
                long remainingMs = _d.ClientRoundEndUnixMs - SessionClock.NowMs();
                if (remainingMs > 0 && BellSchedule.AlertActive(remainingMs))
                {
                    MaybeShowTutorial(_d.LocalIsVillagerTeam
                        ? TutorialId.RoundTimeWarningVillager
                        : TutorialId.RoundTimeWarningWerewolf);
                }
            }

            if (LastRunGate.IsAllExtractionCompleted())
            {
                MaybeShowTutorial(_d.LocalIsVillagerTeam
                    ? TutorialId.FinalExtractionVillager
                    : TutorialId.FinalExtractionWerewolf);
            }

            bool revealVisibleNow = _d.RoleRevealVisible;
            if (_wasRoleRevealVisible && !revealVisibleNow)
            {
                if (localRole == Role.Werewolf) MaybeShowTutorial(TutorialId.WerewolfRoleDrawn);
                else if (localRole == Role.BlackCat) MaybeShowTutorial(TutorialId.BlackCatRoleDrawn);
                else if (localRole == Role.Bomber) MaybeShowTutorial(TutorialId.BomberRoleDrawn);
                else if (localRole == Role.Shaman) MaybeShowTutorial(TutorialId.ShamanRoleDrawn);
            }
            _wasRoleRevealVisible = revealVisibleNow;
        }

        public void Reset()
        {
            _tutorialPresenter.Cancel();
            _wasRoleRevealVisible = false;
            _prevBeaconCharges = 0;
        }

        public void DestroyUi()
        {
            _tutorialBubble.Destroy();
        }
    }
}
