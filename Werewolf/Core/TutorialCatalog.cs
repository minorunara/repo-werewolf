namespace Werewolf.Core
{
    public static class TutorialCatalog
    {
        public static bool ShouldShow(TutorialId id, bool blackCatCurseEnabled)
            => blackCatCurseEnabled
               || (id != TutorialId.FirstMeetingAsBlackCat
                   && id != TutorialId.VillagerSeesCatAwakened
                   && id != TutorialId.BlackCatSelectedForExecution
                   && id != TutorialId.BlackCatExecutionRevealed);

        public static string Format(TutorialId id, bool blackCatCurseEnabled = true)
        {
            switch (id)
            {
                case TutorialId.CorpseDiscovery:
                    return Texts.Get(TextId.TutorialCorpseDiscovery);

                case TutorialId.MeetingCountdownStarted:
                    return Texts.Get(TextId.TutorialMeetingCountdownStarted);

                case TutorialId.FirstMeetingAsVillager:
                    return Texts.Get(TextId.TutorialFirstMeetingAsVillager);

                case TutorialId.WerewolfRoleDrawn:
                    return Texts.Get(TextId.TutorialWerewolfRoleDrawn);

                case TutorialId.FirstValuableSeen:
                    return Texts.Get(TextId.TutorialFirstValuableSeen);

                case TutorialId.WolfModeFirstUnlock:
                    return Texts.Get(TextId.TutorialWolfModeFirstUnlock);

                case TutorialId.BeaconFirstCharged:
                    return Texts.Get(TextId.TutorialBeaconFirstCharged);

                case TutorialId.FirstMeetingAsWerewolf:
                    return Texts.Get(TextId.TutorialFirstMeetingAsWerewolf);

                case TutorialId.FirstMeetingAsBlackCat:
                    return Texts.Get(TextId.TutorialFirstMeetingAsBlackCat);

                case TutorialId.VillagerSeesCatAwakened:
                    return Texts.Get(TextId.TutorialVillagerSeesCatAwakened);

                case TutorialId.BlackCatRoleDrawn:
                    return Texts.Get(blackCatCurseEnabled
                        ? TextId.TutorialBlackCatRoleDrawn
                        : TextId.TutorialBlackCatRoleDrawnNoCurse);

                case TutorialId.LastRunApproaching:
                    return Texts.Get(TextId.TutorialLastRunApproaching);

                case TutorialId.RoundTimeWarningVillager:
                    return Texts.Get(TextId.TutorialRoundTimeWarningVillager);

                case TutorialId.RoundTimeWarningWerewolf:
                    return Texts.Get(TextId.TutorialRoundTimeWarningWerewolf);

                case TutorialId.FinalExtractionVillager:
                    return Texts.Get(TextId.TutorialFinalExtractionVillager);

                case TutorialId.FinalExtractionWerewolf:
                    return Texts.Get(TextId.TutorialFinalExtractionWerewolf);

                case TutorialId.InformantUnlockedAsWerewolf:
                    return Texts.Get(TextId.TutorialInformantUnlockedAsWerewolf);

                case TutorialId.InformantUnlockedAsBlackCat:
                    return Texts.Get(TextId.TutorialInformantUnlockedAsBlackCat);

                case TutorialId.EnemyIgnoreUnlockedAsWerewolf:
                    return Texts.Get(TextId.TutorialEnemyIgnoreUnlockedAsWerewolf);

                case TutorialId.NaturalHealUnlockedAsWerewolf:
                    return Texts.Get(TextId.TutorialNaturalHealUnlockedAsWerewolf);

                case TutorialId.WerewolfSeesCatAwakened:
                    return Texts.Get(TextId.TutorialWerewolfSeesCatAwakened);

                case TutorialId.BeaconFirstUsedAsWerewolf:
                    return Texts.Get(TextId.TutorialBeaconFirstUsedAsWerewolf);

                case TutorialId.BlackCatSelectedForExecution:
                    return Texts.Get(TextId.TutorialBlackCatSelectedForExecution);

                case TutorialId.BlackCatExecutionRevealed:
                    return Texts.Get(TextId.TutorialBlackCatExecutionRevealed);

                case TutorialId.FirstDeath:
                    return Texts.Get(TextId.TutorialFirstDeath);

                case TutorialId.BomberRoleDrawn:
                    return Texts.Get(TextId.TutorialBomberRoleDrawn);

                case TutorialId.BombPlantedAsBomber:
                    return Texts.Get(TextId.TutorialBombPlantedAsBomber);

                case TutorialId.BomberProximityWarnedAsVillager:
                    return Texts.Get(TextId.TutorialBomberProximityWarnedAsVillager);

                case TutorialId.SelfBombExplodedAsVillager:
                    return Texts.Get(TextId.TutorialSelfBombExplodedAsVillager);

                case TutorialId.ShamanRoleDrawn:
                    return Texts.Get(TextId.TutorialShamanRoleDrawn);

                case TutorialId.ShamanGhostSighted:
                    return Texts.Get(TextId.TutorialShamanGhostSighted);

                case TutorialId.ShamanTranceEntered:
                    return Texts.Get(TextId.TutorialShamanTranceEntered);

                case TutorialId.ShamanStormEntered:
                    return Texts.Get(TextId.TutorialShamanStormEntered);

                case TutorialId.EquipBlockedByOtherGrabber:
                    return Texts.Get(TextId.TutorialEquipBlockedByOtherGrabber);

                case TutorialId.ValuableRecordSuppressed:
                    return Texts.Get(TextId.TutorialValuableRecordSuppressed);

                default:
                    return null;
            }
        }
    }
}
