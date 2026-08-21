using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private readonly TutorialPresenter _tutorialPresenter = new TutorialPresenter();

        private readonly TutorialBubblePanel _tutorialBubble = new TutorialBubblePanel();

        private bool _wasRoleRevealVisible;

        private int _prevBeaconCharges;

        private bool LocalIsVillagerTeam =>
            _localRole.HasValue && RoleDistribution.TeamOf(_localRole.Value) == Team.Villagers;

        internal void MaybeShowTutorial(TutorialId id)
        {
            bool blackCatCurseEnabled = IsBlackCatCurseEnabledForClient();
            if (!TutorialCatalog.ShouldShow(id, blackCatCurseEnabled)) return;

            var bindings = Plugin.Bindings;
            if (bindings == null || bindings.IsTutorialSeen(id)) return;
            if (_tutorialPresenter.IsPending(id)) return;
            if (!IsLocalAlive()
                && id != TutorialId.FirstDeath
                && id != TutorialId.BlackCatSelectedForExecution)
                return;

            string message = TutorialCatalog.Format(id, blackCatCurseEnabled);
            if (string.IsNullOrEmpty(message)) return;

            _tutorialPresenter.Enqueue(id, message);
        }

        private bool IsBlackCatCurseEnabledForClient()
        {
            string blob = ResolveLobbySettingsBlob();
            if (SettingsCatalog.TryDecodeBlob(blob, out var values)
                && values.TryGetValue("BlackCatCurseEnabled", out string raw))
                return raw == "1";
            return true;
        }

        private void TickTutorialPresenter()
        {
            var bindings = Plugin.Bindings;
            if (bindings != null) _tutorialPresenter.FontScale = bindings.TutorialFontScale.Value;

            bool meetingUiVisible = _uiManager != null
                && _uiManager.IsLayerVisible(WerewolfUIManager.MeetingLayer);
            if (meetingUiVisible) EnsurePanelBuilt(_tutorialBubble);
            _tutorialPresenter.Bubble = _tutorialBubble;

            TutorialId? shown = _tutorialPresenter.Tick(meetingUiVisible);
            if (shown.HasValue)
            {
                bindings?.MarkTutorialSeen(shown.Value);
                WLog.Line("tutorial_shown", secret: false, ("id", shown.Value));
            }
        }

        private void TickTutorialTriggers()
        {
            if (!IsRoundActiveClient)
            {
                _wasRoleRevealVisible = false;
                return;
            }

            if (_localRole == Role.Werewolf && RolesClient.UnlockedFlags != PerkFlags.None)
            {
                MaybeShowTutorial(TutorialId.WolfModeFirstUnlock);
            }

            if (_localRole == Role.Werewolf
                && PerkFlagsUtil.Has(RolesClient.UnlockedFlags, PerkId.EnemyIgnore))
            {
                MaybeShowTutorial(TutorialId.EnemyIgnoreUnlockedAsWerewolf);
            }

            if (_localRole == Role.Werewolf
                && PerkFlagsUtil.Has(RolesClient.UnlockedFlags, PerkId.NaturalHeal))
            {
                MaybeShowTutorial(TutorialId.NaturalHealUnlockedAsWerewolf);
            }

            if (_localRole == Role.Werewolf && RolesClient.PlayGauge != null
                && RolesClient.PlayGauge.InformantPct > 0
                && RolesClient.RatioPermille >= RolesClient.PlayGauge.InformantPct * 10)
            {
                MaybeShowTutorial(TutorialId.InformantUnlockedAsWerewolf);
            }

            if (RolesClient.BeaconCharges > 0)
            {
                MaybeShowTutorial(TutorialId.BeaconFirstCharged);
            }

            int currentBeaconCharges = RolesClient.BeaconCharges;
            if (currentBeaconCharges < _prevBeaconCharges && _localRole == Role.Werewolf)
            {
                MaybeShowTutorial(TutorialId.BeaconFirstUsedAsWerewolf);
            }
            _prevBeaconCharges = currentBeaconCharges;

            if (LastRunGate.IsLastRunActive())
            {
                MaybeShowTutorial(TutorialId.LastRunApproaching);
            }

            if (_clientPhase == GamePhase.Play && !_meetingClient.MeetingActive && _clientRoundEndUnixMs > 0)
            {
                long remainingMs = _clientRoundEndUnixMs - NowUnixMs();
                if (remainingMs > 0 && BellSchedule.AlertActive(remainingMs))
                {
                    MaybeShowTutorial(LocalIsVillagerTeam
                        ? TutorialId.RoundTimeWarningVillager
                        : TutorialId.RoundTimeWarningWerewolf);
                }
            }

            if (LastRunGate.IsAllExtractionCompleted())
            {
                MaybeShowTutorial(LocalIsVillagerTeam
                    ? TutorialId.FinalExtractionVillager
                    : TutorialId.FinalExtractionWerewolf);
            }

            bool revealVisibleNow = _revealCinematic.Visible;
            if (_wasRoleRevealVisible && !revealVisibleNow)
            {
                if (_localRole == Role.Werewolf) MaybeShowTutorial(TutorialId.WerewolfRoleDrawn);
                else if (_localRole == Role.BlackCat) MaybeShowTutorial(TutorialId.BlackCatRoleDrawn);
                else if (_localRole == Role.Bomber) MaybeShowTutorial(TutorialId.BomberRoleDrawn);
                else if (_localRole == Role.Shaman) MaybeShowTutorial(TutorialId.ShamanRoleDrawn);
            }
            _wasRoleRevealVisible = revealVisibleNow;
        }
    }
}
