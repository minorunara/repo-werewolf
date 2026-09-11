using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Werewolf.Core
{
    public enum Language
    {
        Japanese = 0,
        English = 1,
    }

    public enum TextId
    {
        NoticeConveneStartedFormat,
        NoticeNoExecution,
        NoticeExecutedFormat,
        NoticeBlackCatRevealedFormat,
        NoticeCurseVictimFormat,
        NoticeCatAwakened,
        NoticeConveneDeniedNoRight,
        NoticeConveneDeniedSuppressed,
        NoticeConveneDeniedWrongPhase,
        NoticeConveneDeniedOther,
        NoticeCorpseReportStartedFormat,
        NoticeMeetingCancelledExtraction,
        NoticeConveneDeniedLastRun,
        NoticeConveneDeniedNoCorpse,
        NoticePlayerDisconnectedFormat,
        NoticeConveneHoldHint,
        NoticeScatterSlogan,
        NoticeScatterGroupLineFormat,
        NoticeScatterMemberFormat,
        NoticeScatterGuardTripped,
        NoticeScatterGroupSeparator,
        VoteScatterBadgeFormat,
        VoteScatterBadgeUnknown,

        OutfitBlockedHeader,
        OutfitBlockedBody,
        OutfitBlockedOk,

        RevealTeammatePrefix,
        RevealHeadingWinCondition,
        RevealHeadingAbility,
        RevealVillagerTitleMaybeCat,
        RevealVillagerTitle,
        RevealVillagerWinCondition1,
        RevealVillagerWinCondition2,
        RevealVillagerFlavor,
        RevealWolfTeamWinCondition1,
        RevealWolfTeamWinCondition2,
        RevealWolfTeamWinCondition3,
        RevealWolfAbility1,
        RevealWolfAbility2,
        RevealWolfAbility3,
        RevealBomberAbility1,
        RevealBomberAbility2,
        RevealBomberAbility3,
        RevealBlackCatAbility,
        RevealBlackCatNoCurse,
        RevealWerewolfTitle,
        RevealBlackCatTitle,
        RevealBlackCatAwakeningTitle,
        RevealBomberTitle,
        RevealShamanTitle,
        RevealShamanAbility1,
        RevealShamanAbility2,
        RevealHeadingTips,
        RevealVillagerTipConvene,
        RevealVillagerTipReport,
        RevealVillagerTipValuableMap,
        RevealVillagerTipAliveCheck,
        TutorialSkipHint,
        RevealSelfIdFormat,

        RoleNameWerewolf,
        RoleNameBlackCat,
        RoleNameVillager,
        RoleNameBomber,
        RoleNameShaman,

        GaugePerkStaminaLabel,
        GaugePerkJumpLabel,
        GaugePerkEnemyIgnoreLabel,
        GaugePerkHealLabel,
        GaugePerkInformantLabel,

        GaugeBeaconRuleFormat,
        GaugeBombRuleFormat,
        GaugePercentDollarsFormat,
        GaugeLossOverBaseFormat,
        GaugeNextUpdateFormat,

        HudTimerFrozenFormat,
        HudGaugeFormat,
        HudRightsFormat,
        HudTimeRemainingFormat,
        HudWolfToggleFormat,
        HudBeaconKeyFormat,
        HudBeaconLabel,
        HudTestPlayBanner,
        HudScatterGuardBanner,

        BannerMeetTheDeadline,
        BannerHurryUp,

        ImpactDiscussLeft,
        ImpactDiscussRight,

        HudSelfIdFormat,
        IdNameFormat,

        StartHoldWaitingOthers,

        PreplayAbortTitle,
        PreplayAbortTooFewPlayers,
        PreplayAbortInvalidConfig,
        PreplayAbortRoleRevealDisconnect,
        PreplayAbortNoRoles,
        PreplayAbortRolesDiscarded,

        ResultBannerVillagerWin,
        ResultBannerWerewolfWin,
        ResultBannerDefault,
        ResultStatusAlive,
        ResultStatusDead,
        ResultStatusExecuted,
        ResultStatusDisconnected,
        ResultReturnButtonLabel,
        ResultReturnButtonWithKeyFormat,
        ResultAutoReturnCountdownFormat,
        ResultWaitingHost,
        ResultFooterWithCountdownFormat,

        ResultBannerVoid,
        ResultBannerPeacefulSurvived,
        ResultBannerPeacefulCollapsed,
        VoidMatchChargingLabel,
        VoidMatchArmHeader,
        VoidMatchArmBody,
        VoidMatchConfirmPromptFormat,
        VoidMatchCancelHintFormat,

        ResultDigestHeader,
        DigestMatchStart,
        DigestMeetingButtonFormat,
        DigestMeetingReportFormat,
        DigestExecutedFormat,
        DigestNoExecution,
        DigestCurseStartedFormat,
        DigestCurseFollowFormat,
        DigestDeathFormat,
        DigestBombDetonatedFormat,
        DigestCheckmate,
        DigestMatchEndFormat,
        DigestReasonWerewolvesEradicated,
        DigestReasonVillagersEradicated,
        DigestReasonExtractionCompleted,
        DigestReasonTimerExpired,
        DigestReasonExtractionFailed,
        DigestReasonValueCheckmate,
        DigestExtractionDoneFormat,
        DigestPerkUnlockedFormat,
        DigestInformant,
        DigestFinalBalanceFormat,

        EpigraphWerewolvesEradicated,
        EpigraphVillagersEradicated,
        EpigraphExtractionCompleted,
        EpigraphTimerExpired,
        EpigraphExtractionFailed,
        EpigraphValueCheckmate,
        EpigraphPeacefulSurvived,
        EpigraphPeacefulCollapsed,

        VoteMeetingTitle,
        VoteSkipLabel,
        VoteConfirmLabel,
        VoteVoteLabel,
        VoteWerewolfMarkerLabel,
        VoteBomberMarkerLabel,
        VoteCountFormat,
        VoteExecutedFormat,
        VoteNoExecution,
        VoteSkipSuffixFormat,

        ChatLogTitle,
        ChatLogEmpty,
        ChatLogDeadHint,
        ChatLogToggleLabelFormat,
        ChatLogVoted,
        ChatLogSystemName,
        ChatLogScatterTitle,
        ChatLogScatterLineFormat,
        ChatLogMeetingNumberFormat,
        ChatLogFilteringTitle,

        ChatStampButtonLabel,
        ChatStampUnknown,
        ChatStampCategoryGuide,
        ChatStampCategoryPress,
        ChatStampCategoryDefense,
        ChatStampCategoryClaim,
        ChatStampCategoryChatter,
        ChatStampGuideVoteSkip,
        ChatStampGuideVoteSuspect,
        ChatStampGuideExtraction,
        ChatStampGuideFriendly,
        ChatStampPressSuspicious,
        ChatStampPressWhere,
        ChatStampPressWithWhom,
        ChatStampPressDoing,
        ChatStampPressItems,
        ChatStampPressDropWeapon,
        ChatStampDefenseInnocent,
        ChatStampDefenseWasDead,
        ChatStampDefenseAccident,
        ChatStampDefenseDontKnow,
        ChatStampDefenseForgot,
        ChatStampDefenseNoIdea,
        ChatStampClaimAgree,
        ChatStampClaimDisagree,
        ChatStampClaimObjection,
        ChatStampClaimTheyDidIt,
        ChatStampClaimItWasMe,
        ChatStampClaimTogether,
        ChatStampChatterMorning,
        ChatStampChatterGg,
        ChatStampChatterWantLive,
        ChatStampChatterDied,
        ChatStampChatterLookAtMe,
        ChatStampChatterAsPlanned,
        ChatStampChatterWhatever,
        ChatStampChatterSorry,
        ChatStampChatterHelp,
        ChatStampChatterCry,

        ResultChatTitle,
        ResultChatToggleLabelFormat,

        ReplayToggleLabel,
        ReplayPlay,
        ReplayPause,
        ReplaySpeedFormat,
        ReplaySaveButton,
        ReplaySavedButton,
        ReplaySavedToDownloads,
        ReplaySavedToModFolder,
        ReplayAlreadySaved,
        ReplaySaveFailed,
        ReplayNoData,
        ReplayPositionCapped,
        ReplaySavedCapped,
        ReplayGaugeLossFormat,
        ReplayGaugeDeliveredFormat,
        ReplayEpStateNone,
        ReplayEpStateIdle,
        ReplayEpStateActive,
        ReplayEpStateSuccess,
        ReplayEpStateWarning,
        ReplayEpStateCancel,
        ReplayEpStateExtracting,
        ReplayEpStateComplete,
        ReplayEpStateSurplus,
        ReplayEpStateTaxReturn,
        ReplayStampNoExecution,
        ReplayStampExecutedFormat,

        RecapNameSeparator,
        RecapDeathsFormat,
        RecapDeathsNone,
        RecapLostFormat,
        RecapHaulFormat,
        RecapBeaconFormat,
        RecapBeaconNone,

        DeathRevealTitle,
        DeathRevealNone,
        DeathRevealScatterGuardTitle,

        CheckmateTitle,

        EradicationLastWerewolfDied,
        EradicationLastVillagerDied,
        EradicationLastWerewolfVanished,
        EradicationLastVillagerVanished,
        EradicationPeacefulCollapsed,

        MapOverlayToggleLabelFormat,
        MapOverlayMapToolBlocked,

        LobbySettingsFooterHintFormat,
        LobbySettingsMiniHintFormat,

        ModIntegrityHeaderAllMatchFormat,
        ModIntegrityHeaderCountsFormat,
        ModIntegritySelfDifferenceFormat,
        ModIntegritySelfUnavailable,
        ModIntegrityPanelTitle,
        ModIntegrityFilterNeedsReview,
        ModIntegrityFilterMatch,
        ModIntegrityFilterAll,
        ModIntegrityStatusBaseline,
        ModIntegrityStatusPending,
        ModIntegrityStatusMatch,
        ModIntegrityStatusDifference,
        ModIntegrityStatusUnavailable,
        ModIntegrityReasonNoResponse,
        ModIntegrityReasonUnsupportedProtocol,
        ModIntegrityReasonInvalidPayload,
        ModIntegrityReasonTooLarge,
        ModIntegrityReasonCollectionFailed,
        ModIntegrityDetailLoading,
        ModIntegrityDetailFailed,
        ModIntegrityDisclaimer,
        ModIntegrityDetailMissingFormat,
        ModIntegrityDetailExtraFormat,
        ModIntegrityDetailVersionFormat,
        ModIntegrityDetailContentFormat,
        ModIntegrityStartCautionTitle,
        ModIntegrityStartSevereTitle,
        ModIntegrityStartBodyFormat,
        ModIntegrityButtonBack,
        ModIntegrityButtonDetails,
        ModIntegrityButtonContinue,
        ModIntegrityButtonClose,
        LobbyStartTooFewPlayersTitle,
        LobbyStartTooFewPlayersBodyFormat,
        LobbyStartTeamOverflowTitle,
        LobbyStartTeamOverflowBodyFormat,
        LobbyStartBlockedRemedy,

        ConveneCountdownDefaultCallerName,
        ConveneCountdownHeaderFormat,
        ConveneCountdownCorpseHeaderFormat,
        ConveneCountdownScatterGuardHeader,

        HudCorpseReportKeyFormat,

        MeetingButtonSuppressCountdownFormat,
        MeetingButtonConveneGrabPrompt,
        MeetingButtonConveneInteractPrompt,
        MeetingButtonSuppressedPrompt,
        MeetingButtonRightsSuffixFormat,

        CurseBlackCatRevealedFormat,
        CurseNoVictim,

        SettingsSectionGeneral,
        SettingsSectionMeeting,
        SettingsSectionRoleAssignment,
        SettingsSectionRoles,
        SettingsSectionBlackCat,
        SettingsSectionBomber,
        SettingsSectionShaman,
        SettingsSectionWorldgen,
        SettingsSectionStartItemList,
        SettingsSectionStartUpgradeList,

        SettingsBoolEnabled,
        SettingsBoolDisabled,
        SettingsAuto,
        SettingsValueVanilla,
        SettingsListEmpty,
        SettingsPresent,
        SettingsRandom,
        SettingsValuableMapRealtime,
        SettingsValuableMapMeetingSync,
        SettingsValuableMapHidden,
        SettingsNecroVoiceOff,
        SettingsNecroVoiceNonWerewolfDead,
        SettingsNecroVoiceAllDead,
        SettingsItemsAggregateFormat,
        SettingsAggregateMoreSuffix,
        SettingsUpgradeItemFormat,
        SettingsListSeparator,
        SettingsUnitSeconds,
        SettingsUnitTimes,
        SettingsUnitItems,
        SettingsUnitPeople,
        SettingsUnitMeters,
        SettingsUnitPercent,
        SettingsUnitDamage,
        SettingsUnitSets,

        SettingsLabelWerewolfCount,
        SettingsLabelBlackCatChancePercent,
        SettingsLabelBomberChancePercent,
        SettingsLabelPeacefulVillageChancePercent,
        SettingsLabelShamanChancePercent,
        SettingsLabelRoundSeconds,
        SettingsLabelBlackCatRevealDelaySec,
        SettingsLabelBlackCatCurseEnabled,
        SettingsLabelMeetingRightsPerPlayer,
        SettingsLabelConveneSuppressStartSec,
        SettingsLabelConveneSuppressAfterSec,
        SettingsLabelMeetingCountdownSec,
        SettingsLabelMeetingDurationSec,
        SettingsLabelVoteTimeCutEnabled,
        SettingsLabelMeetingScatterEnabled,
        SettingsLabelScatterGuardSec,
        SettingsLabelMeetingEnemyRespawnScalePct,
        SettingsLabelResultDisplaySec,
        SettingsLabelStaminaUnlockPct,
        SettingsLabelJumpUnlockPct,
        SettingsLabelEnemyIgnoreUnlockPct,
        SettingsLabelHealUnlockPct,
        SettingsLabelHealIntervalSec,
        SettingsLabelBeaconChargePct,
        SettingsLabelInformantThresholdPct,
        SettingsLabelExtraJumpCount,
        SettingsLabelBeaconCooldownSec,
        SettingsLabelBeaconSuppressStartSec,
        SettingsLabelBeaconSuppressAfterMeetingSec,
        SettingsLabelCatGaugeSyncIntervalSec,
        SettingsLabelOrbGaugeEnabled,
        SettingsLabelWerewolfModeEnabled,
        SettingsLabelMinimapHideEnabled,
        SettingsLabelOutfitChangeAllowed,
        SettingsLabelValuableMapMode,
        SettingsLabelGameOverAutoReturnSec,
        SettingsLabelNecroVoiceMode,
        SettingsLabelStartLevelNumber,
        SettingsLabelStartMapName,
        SettingsLabelStartItemsSpec,
        SettingsLabelStartEnergyPct,
        SettingsLabelStartUpgradesSpec,
        SettingsLabelOrbDropMax,
        SettingsLabelStartHaulGoalPct,
        SettingsLabelEnemySetsDanger1,
        SettingsLabelEnemySetsDanger2,
        SettingsLabelEnemySetsDanger3,

        SettingsLabelBomberProximityMeters,
        SettingsLabelBomberGaugeFullSec,
        SettingsLabelBomberInitialCooldownSec,
        SettingsLabelBomberCooldownSec,
        SettingsLabelBomberBlastRadiusMeters,
        SettingsLabelBomberBlastPlayerDamage,
        SettingsLabelBomberBlastEnemyDamage,
        SettingsLabelBomberAmmoRefillPct,
        SettingsLabelShamanGazeFullSec,
        SettingsLabelShamanGhostCooldownSec,
        SettingsLabelShamanStormWeakMeters,
        SettingsLabelShamanStormMediumMeters,
        SettingsLabelShamanStormStrongMeters,

        HudBomberPlantKeyFormat,
        HudBomberDetonateKeyFormat,

        HudValuableRecordOnFormat,
        HudValuableRecordOffFormat,

        BomberDenyNoAmmo,
        BomberDenyNoFullTarget,
        BomberDenyPlantCooldown,
        BomberDenyDetonateCooldown,
        BomberDenyNoBomb,
        BomberDenyMeetingLocked,
        BomberDenyTruckZone,
        BomberDudTargetDead,
        BomberTargetDisconnected,
        BomberProximityWarning,

        TutorialCorpseDiscovery,
        TutorialMeetingCountdownStarted,
        TutorialFirstMeetingAsVillager,
        TutorialWerewolfRoleDrawn,
        TutorialFirstValuableSeen,
        TutorialWolfModeFirstUnlock,
        TutorialBeaconFirstCharged,
        TutorialFirstMeetingAsWerewolf,
        TutorialFirstMeetingAsBlackCat,
        TutorialVillagerSeesCatAwakened,
        TutorialBlackCatRoleDrawn,
        TutorialBlackCatRoleDrawnNoCurse,
        TutorialLastRunApproaching,
        TutorialRoundTimeWarningVillager,
        TutorialRoundTimeWarningWerewolf,
        TutorialFinalExtractionVillager,
        TutorialFinalExtractionWerewolf,
        TutorialInformantUnlockedAsWerewolf,
        TutorialInformantUnlockedAsBlackCat,
        TutorialEnemyIgnoreUnlockedAsWerewolf,
        TutorialNaturalHealUnlockedAsWerewolf,
        TutorialWerewolfSeesCatAwakened,
        TutorialBeaconFirstUsedAsWerewolf,
        TutorialBlackCatSelectedForExecution,
        TutorialBlackCatExecutionRevealed,
        TutorialFirstDeath,
        TutorialBomberRoleDrawn,
        TutorialBombPlantedAsBomber,
        TutorialBomberProximityWarnedAsVillager,
        TutorialSelfBombExplodedAsVillager,
        TutorialShamanRoleDrawn,
        TutorialShamanGhostSighted,
        TutorialShamanTranceEntered,
        TutorialShamanStormEntered,
        TutorialEquipBlockedByOtherGrabber,
        TutorialValuableRecordSuppressed,

        ManualToggleLabelFormat,
        ManualPageFooterFormat,
        ManualNavHint,
        ManualSectionBasics,
        ManualSectionExploration,
        ManualSectionMeeting,
        ManualSectionGauge,
        ManualSectionVillager,
        ManualSectionShaman,
        ManualSectionWerewolf,
        ManualSectionBlackCat,
        ManualSectionBomber,
        ManualSectionAfterDeath,
        ManualWelcomeTitle,
        ManualWelcomeBody,
        ManualGameFlowTitle,
        ManualGameFlowBody,
        ManualVillagerWinTitle,
        ManualVillagerWinBody,
        ManualWerewolfWinTitle,
        ManualWerewolfWinBody,
        ManualPeacefulVillageTitle,
        ManualPeacefulVillageBody,
        ManualValuablesMapTitle,
        ManualValuablesMapBody,
        ManualValuableRecordTitle,
        ManualValuableRecordBody,
        ManualValuableRecordToggle,
        ManualCombatTitle,
        ManualCombatBody,
        ManualEndgamePrepTitle,
        ManualEndgamePrepBody,
        ManualCorpseTitle,
        ManualCorpseBody,
        ManualConveneTitle,
        ManualConveneBody,
        ManualMeetingFlowTitle,
        ManualMeetingFlowBody,
        ManualVotingTitle,
        ManualVotingBody,
        ManualScatterTitle,
        ManualScatterBody,
        ManualGaugeBasicsTitle,
        ManualGaugeIntro,
        ManualGaugeLoss,
        ManualGaugeDelivery,
        ManualGaugeLines,
        ManualRoleVillagerTitle,
        ManualRoleVillagerBody,
        ManualRoleWerewolfTitle,
        ManualRoleWerewolfIntro,
        ManualRoleWerewolfEnemyMap,
        ManualRoleWerewolfPerksTitle,
        ManualRoleWerewolfPerkStamina,
        ManualRoleWerewolfPerkJump,
        ManualRoleWerewolfPerksTitle2,
        ManualRoleWerewolfPerkEnemyIgnore,
        ManualRoleWerewolfPerkHeal,
        ManualRoleWerewolfPerkToggle,
        ManualRoleWerewolfBeaconTitle,
        ManualRoleWerewolfBeaconBody,
        ManualRoleBlackCatTitle,
        ManualRoleBlackCatIntro,
        ManualBlackCatInformantTitle,
        ManualRoleBlackCatInformant,
        ManualRoleBlackCatGaugeNote,
        ManualBlackCatCounterTitle,
        ManualBlackCatCounterBody,
        ManualRoleBomberTitle,
        ManualRoleBomberIntro,
        ManualBomberPlantTitle,
        ManualRoleBomberPlant,
        ManualRoleBomberDetonateTitle,
        ManualRoleBomberDetonateBody,
        ManualRoleShamanTitle,
        ManualRoleShamanIntro,
        ManualShamanSenseTitle,
        ManualShamanGhost,
        ManualShamanStorm,
        ManualAfterDeathTitle,
        ManualAfterDeathBody,

        ScenarioSelectTitle,
        ScenarioNameMeeting,
        ScenarioDescMeeting,
        ScenarioNameWolf,
        ScenarioDescWolf,
        ScenarioNameShaman,
        ScenarioDescShaman,
        ScenarioNameBomber,
        ScenarioDescBomber,
        ScenarioLocked,
        ScenarioCompleted,
        ScenarioBack,
        ScenarioReturnToSelect,
        ScenarioReturnToSelectWithKeyFormat,
        ScenarioAbortConfirm,
        ScenarioHudBadge,
        ScenarioAdvanceHint,
        ScenarioAbortHint,
        ScenarioAbortCancelHint,
        ScenarioBrokenNotice,
        ScenarioNeedOpenChatLogNag,
        ScenarioNeedCloseMapNag,
        ScenarioNeedOpenMapNag,
        ScenarioNeedCloseMapToolNag,
        ScenarioNeedOpenMapToolNag,
        ScenarioNeedShamanTranceNag,
        ScenarioNeedShamanSenseReadyNag,
        ScenarioNeedShamanStormNag,
        ScenarioNeedCorpseNearNag,
        ScenarioNeedBombTargetNearNag,
        ScenarioNeedBombOnTargetNag,
        ScenarioBlastDenyTooClose1,
        ScenarioBlastDenyTooClose2,
        ScenarioBlastDenyTooFar1,
        ScenarioBlastDenyTooFar2,
        ScenarioBlastDenyAwaitTruck1,
        ScenarioBlastDenyAwaitTruck2,
        ScenarioBlastDenyMoving1,
        ScenarioBlastDenyMoving2,
        ScenarioBlastDenyGeneric1,
        ScenarioMeetingLeashNag,

        ScenarioDecodeReceiving,
        ScenarioDecodeInitializing,
        ScenarioDecodeDone,

        ScenarioMeetingIntro1,
        ScenarioMeetingIntro2,
        ScenarioMeetingIntro3,
        ScenarioMeetingIntro4,
        ScenarioMeetingIntro5,
        ScenarioMeetingIntro6,
        ScenarioMeetingConvene1,
        ScenarioMeetingConvene2,
        ScenarioMeetingConveneNag1,
        ScenarioMeetingConveneNag2Format,
        ScenarioMeetingConveneNag3Grab,
        ScenarioMeetingConveneNag3Interact,
        ScenarioMeetingConveneEarly1,
        ScenarioMeetingConveneEarly2,
        ScenarioMeetingOpened1,
        ScenarioMeetingOpened2,
        ScenarioMeetingOpened3,
        ScenarioMeetingOpened4,
        ScenarioMeetingOpened5,
        ScenarioMeetingOpened6,
        ScenarioMeetingOpened7,
        ScenarioMeetingOpened8,
        ScenarioMeetingOpened9,
        ScenarioMeetingClock1,
        ScenarioMeetingClock2,
        ScenarioMeetingClock3,
        ScenarioMeetingClock4,
        ScenarioMeetingGauge1,
        ScenarioMeetingGauge2,
        ScenarioMeetingGauge3,
        ScenarioMeetingGauge4,
        ScenarioMeetingGauge5,
        ScenarioMeetingMap1,
        ScenarioMeetingMap2,
        ScenarioMeetingMap3,
        ScenarioMeetingMap4,
        ScenarioMeetingMap5,
        ScenarioMeetingChatUnavailable1,
        ScenarioMeetingChatUnavailable2,
        ScenarioMeetingVote1,
        ScenarioMeetingVote2,
        ScenarioMeetingVote3,
        ScenarioMeetingTally,
        ScenarioMeetingResult1,
        ScenarioMeetingResult2,
        ScenarioMeetingPostgame1,
        ScenarioMeetingPostgame2,
        ScenarioMeetingReplay1,
        ScenarioMeetingReplay2,
        ScenarioMeetingPostgame3,
        ScenarioMeetingLine1,
        ScenarioMeetingLine2,
        ScenarioMeetingLine3,
        ScenarioMeetingLine4,
        ScenarioMeetingLine5,
        ScenarioMeetingLine6,
        ScenarioMeetingLine7,
        ScenarioMeetingLine8,
        ScenarioMeetingLine9,
        ScenarioMeetingLine10,
        ScenarioMeetingLine11,
        ScenarioMeetingLine12,
        ScenarioMeetingLine13,
        ScenarioMeetingLine14,

        ScenarioWolfIntro1,
        ScenarioWolfIntro2,
        ScenarioWolfIntro3,
        ScenarioWolfGauge1,
        ScenarioWolfGauge2,
        ScenarioWolfGauge3,
        ScenarioWolfDestroy1,
        ScenarioWolfDestroy2,
        ScenarioWolfDestroyNag1,
        ScenarioWolfDestroyNag2,
        ScenarioWolfEarly1,
        ScenarioWolfEarly2,
        ScenarioWolfMarker1,
        ScenarioWolfMarker2,
        ScenarioWolfMarker3,
        ScenarioWolfMarker4,
        ScenarioWolfMarker5,
        ScenarioWolfRecord1,
        ScenarioWolfRecord2,
        ScenarioWolfRecord3,
        ScenarioWolfRecordNag1,
        ScenarioWolfStealth1,
        ScenarioWolfStealth2,
        ScenarioWolfStealthNag1,
        ScenarioWolfStealth3,
        ScenarioWolfStealth4,
        ScenarioWolfKeep1,
        ScenarioWolfKeep2,
        ScenarioWolfPush1,
        ScenarioWolfPush2,
        ScenarioWolfForm1,
        ScenarioWolfForm2,
        ScenarioWolfForm3,
        ScenarioWolfForm4,
        ScenarioWolfFormOn1,
        ScenarioWolfFormOn2,
        ScenarioWolfFormOnNag1,
        ScenarioWolfFormDone1,
        ScenarioWolfFormDone2,
        ScenarioWolfEnemyMap1,
        ScenarioWolfEnemyMap2,
        ScenarioWolfEnemyMap3,
        ScenarioWolfBeacon1,
        ScenarioWolfBeacon2,
        ScenarioWolfBeacon3,
        ScenarioWolfBeaconNag1,
        ScenarioWolfBeacon4,
        ScenarioWolfBeacon5,
        ScenarioWolfBeacon6,
        ScenarioWolfIgnore1,
        ScenarioWolfIgnore2,
        ScenarioWolfHeal1,
        ScenarioWolfHeal2,
        ScenarioWolfOrb1,
        ScenarioWolfOrb2,
        ScenarioWolfFinish1,
        ScenarioWolfFinish2,
        ScenarioWolfFinish3,
        ScenarioWolfResult1,
        ScenarioWolfResult2,
        ScenarioWolfPostgame1,
        ScenarioWolfPostgame2,
        ScenarioWolfReplay1,
        ScenarioWolfReplay2,
        ScenarioWolfWrap1,
        ScenarioWolfWrap2,
        ScenarioWolfPostgame3,
        ScenarioWolfExtractionNag1,
        ScenarioWolfExtractionNag2,
        ScenarioWolfExtractionBreak1,
        ScenarioWolfExtractionBreak2,
        ScenarioWolfExtractionBreak3,

        ScenarioShamanIntro1,
        ScenarioShamanIntro2,
        ScenarioShamanIntro3,
        ScenarioShamanIntro4,
        ScenarioShamanIntro5,
        ScenarioShamanTrance1,
        ScenarioShamanTrance2,
        ScenarioShamanTrance3,
        ScenarioShamanTranceOn1,
        ScenarioShamanTranceOn2,
        ScenarioShamanDrip1,
        ScenarioShamanDrip2,
        ScenarioShamanDowse1,
        ScenarioShamanDowse2,
        ScenarioShamanDowseNag1,
        ScenarioShamanDowseNag2,
        ScenarioShamanBurst1,
        ScenarioShamanBurst2,
        ScenarioShamanBurst3,
        ScenarioShamanStorm1,
        ScenarioShamanStorm2,
        ScenarioShamanStorm3,
        ScenarioShamanReport1,
        ScenarioShamanReport2,
        ScenarioShamanReportNag1,
        ScenarioShamanCorpseHint1,
        ScenarioShamanCorpseHint2,
        ScenarioShamanEarly1,
        ScenarioShamanEarly2,
        ScenarioShamanOpened1,
        ScenarioShamanOpened2,
        ScenarioShamanOpened3,
        ScenarioShamanVote1,
        ScenarioShamanVote2,
        ScenarioShamanTally1,
        ScenarioShamanTally2,
        ScenarioShamanDeliver1,
        ScenarioShamanDeliverNag1,
        ScenarioShamanLastRun1,
        ScenarioShamanLastRun2,
        ScenarioShamanLastRun3,
        ScenarioShamanBlocked1,
        ScenarioShamanBlockedNag1,
        ScenarioShamanDenied1,
        ScenarioShamanDenied2,
        ScenarioShamanButton1,
        ScenarioShamanButtonNag1,
        ScenarioShamanButtonNag2Format,
        ScenarioShamanButtonNag3Grab,
        ScenarioShamanButtonNag3Interact,
        ScenarioShamanOpened4,
        ScenarioShamanOpened5,
        ScenarioShamanVote3,
        ScenarioShamanVote4,
        ScenarioShamanTally3,
        ScenarioShamanResult1,
        ScenarioShamanResult2,
        ScenarioShamanPostgame1,
        ScenarioShamanPostgame2,
        ScenarioShamanReplay1,
        ScenarioShamanReplay2,
        ScenarioShamanWrap1,
        ScenarioShamanWrap2,
        ScenarioShamanWrap3,
        ScenarioShamanPostgame3,
        ScenarioShamanLeashNag1,
        ScenarioShamanLeashNag2,
        ScenarioShamanExtractionBreak1,
        ScenarioShamanExtractionBreak2,
        ScenarioShamanExtractionBreak3,
        ScenarioShamanEarlyExtractionBreak1,
        ScenarioShamanEarlyExtractionBreak2,
        ScenarioShamanEarlyExtractionBreak3,
        ScenarioShamanQuotaBagBreak1,
        ScenarioShamanQuotaBagBreak2,
        ScenarioShamanQuotaBagBreak3,
        ScenarioShamanMapAside1,
        ScenarioShamanMapAside2,
        ScenarioShamanMapAside3,

        ScenarioBomberIntro1,
        ScenarioBomberIntro2,
        ScenarioBomberIntro3,
        ScenarioBomberIntro4,
        ScenarioBomberIntro5,
        ScenarioBomberAim1,
        ScenarioBomberAim2,
        ScenarioBomberPlant1,
        ScenarioBomberPlant2,
        ScenarioBomberPlant3,
        ScenarioBomberPlantNag1,
        ScenarioBomberPlanted1,
        ScenarioBomberNotice1,
        ScenarioBomberNotice2,
        ScenarioBomberTry1,
        ScenarioBomberTryNag1,
        ScenarioBomberLock1,
        ScenarioBomberLock2,
        ScenarioBomberSafe1,
        ScenarioBomberSafe2,
        ScenarioBomberSafeNag1,
        ScenarioBomberBoom1,
        ScenarioBomberBoom2,
        ScenarioBomberMeet1,
        ScenarioBomberMeet2,
        ScenarioBomberOpened1,
        ScenarioBomberOpened2,
        ScenarioBomberVote1,
        ScenarioBomberVote2,
        ScenarioBomberTally1,
        ScenarioBomberTally2,
        ScenarioBomberReplant1,
        ScenarioBomberReplant2,
        ScenarioBomberReplant3,
        ScenarioBomberReplant4,
        ScenarioBomberReplantNag1,
        ScenarioBomberFire1,
        ScenarioBomberFire2,
        ScenarioBomberFireNag1,
        ScenarioBomberResult1,
        ScenarioBomberResult2,
        ScenarioBomberPostgame1,
        ScenarioBomberPostgame2,
        ScenarioBomberCheck1,
        ScenarioBomberCheck2,
        ScenarioBomberAlly1,
        ScenarioBomberAlly2,
        ScenarioBomberClue1,
        ScenarioBomberClue2,
        ScenarioBomberWrap1,
        ScenarioBomberWrap2,
        ScenarioBomberPostgame3,
        ScenarioBomberEarly1,
        ScenarioBomberEarly2,
        ScenarioBomberExtractionBreak1,
        ScenarioBomberExtractionBreak2,
        ScenarioBomberSurvivedBreak1,
        ScenarioBomberSurvivedBreak2,
        ScenarioBomberWrongTarget1,
        ScenarioBomberWrongTarget2,
        ScenarioBomberAlreadyOnTarget1,
        ScenarioBomberAlreadyOnTarget2,
        ScenarioBomberLineA1,
        ScenarioBomberLineA2,
        ScenarioBomberLineA3,
        ScenarioBomberLineA4,
        ScenarioBomberLineA5,
        ScenarioBomberLineA6,
        ScenarioBomberLineA7,
        ScenarioBomberLineA8,
        ScenarioBomberLineA9,
        ScenarioBomberLineA10,
        ScenarioBomberLineA11,
        ScenarioBomberLineA12,
        ScenarioBomberLineA13,
        ScenarioBomberLineA14,
        ScenarioShamanLineA1,
        ScenarioShamanLineA2,
        ScenarioShamanLineA3,
        ScenarioShamanLineA4,
        ScenarioShamanLineA5,
        ScenarioShamanLineA6,
        ScenarioShamanLineA7,
        ScenarioShamanLineA8,
        ScenarioShamanLineA9,
        ScenarioShamanLineA10,
        ScenarioShamanLineA11,
        ScenarioShamanLineA12,
        ScenarioShamanLineA13,
        ScenarioShamanLineA14,
        ScenarioShamanLineA15,
        ScenarioShamanLineA16,
        ScenarioShamanLineA17,
        ScenarioShamanLineA18,
        ScenarioShamanLineA19,
        ScenarioShamanLineA20,
        ScenarioShamanLineB1,
        ScenarioShamanLineB2,
        ScenarioShamanLineB3,
        ScenarioShamanLineB4,
        ScenarioShamanLineB5,
        ScenarioShamanLineB6,
        ScenarioShamanLineB7,
        ScenarioShamanLineB8,
        ScenarioShamanLineB9,
        ScenarioShamanLineB10,
        ScenarioShamanLineB11,
        ScenarioShamanLineB12,
        ScenarioShamanLineB13,
        ScenarioShamanLineB14,
    }

    public static class Texts
    {
        public static Language Current { get; set; } = Language.Japanese;

        private static IReadOnlyDictionary<TextId, string> _override;

        public static Action<string> FormatErrorLogger;

        private static readonly HashSet<TextId> _formatErrorLogged = new HashSet<TextId>();

        public static void SetOverride(IReadOnlyDictionary<TextId, string> table)
        {
            _override = table;
            _formatErrorLogged.Clear();
        }

        public static void ClearOverride()
        {
            _override = null;
            _formatErrorLogged.Clear();
        }

        public static string Get(TextId id)
        {
            if (_override != null && _override.TryGetValue(id, out var overridden))
            {
                return overridden;
            }
            var table = TableFor(Current);
            if (table.TryGetValue(id, out var value))
            {
                return value;
            }
            return JapaneseTable.TryGetValue(id, out var ja) ? ja : string.Empty;
        }

        public static string Format(TextId id, params object[] args)
        {
            string template = Get(id);
            if (args == null || args.Length == 0) return template;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException)
            {
                LogFormatError(id);
                string fallback = JapaneseTable.TryGetValue(id, out var jaTemplate) ? jaTemplate : template;
                try
                {
                    return string.Format(CultureInfo.InvariantCulture, fallback, args);
                }
                catch (FormatException)
                {
                    return fallback;
                }
            }
        }

        public static string ExportTemplate()
        {
            var sb = new StringBuilder();
            foreach (TextId id in Enum.GetValues(typeof(TextId)))
            {
                string ja = JapaneseTable.TryGetValue(id, out var value) ? value : string.Empty;
                sb.Append("# ").Append(ja.Replace("\n", "\\n")).Append('\n');
                sb.Append(id).Append('=').Append(ja.Replace("\n", "\\n")).Append('\n');
            }
            return sb.ToString();
        }

        private static void LogFormatError(TextId id)
        {
            if (!_formatErrorLogged.Add(id)) return;
            try
            {
                FormatErrorLogger?.Invoke(id.ToString());
            }
            catch
            {
            }
        }

        internal static IReadOnlyDictionary<TextId, string> TableFor(Language language)
        {
            switch (language)
            {
                case Language.English:
                    return EnglishTable;
                case Language.Japanese:
                default:
                    return JapaneseTable;
            }
        }

        private static readonly IReadOnlyDictionary<TextId, string> JapaneseTable = new Dictionary<TextId, string>
        {
            [TextId.NoticeConveneStartedFormat] = "{0}が緊急会議を招集しました",
            [TextId.NoticeNoExecution] = "誰も処刑されませんでした",
            [TextId.NoticeExecutedFormat] = "{0}が処刑されました",
            [TextId.NoticeBlackCatRevealedFormat] = "{0}は黒猫でした",
            [TextId.NoticeCurseVictimFormat] = "{0}は道連れにされました",
            [TextId.NoticeCatAwakened] = "もし黒猫がいるなら、目覚めている頃です…",
            [TextId.NoticeConveneDeniedNoRight] = "会議を開催できません（開催権がありません）",
            [TextId.NoticeConveneDeniedSuppressed] = "会議を開催できません（現在は抑止時間中です）",
            [TextId.NoticeConveneDeniedWrongPhase] = "会議を開催できません（今は開催できません）",
            [TextId.NoticeConveneDeniedOther] = "会議を開催できません",
            [TextId.NoticeCorpseReportStartedFormat] = "{0}が死体の頭を発見しました",
            [TextId.NoticeMeetingCancelledExtraction] = "最終抽出直前のため死体の通報は中止されました",
            [TextId.NoticeConveneDeniedLastRun] = "会議を開催できません（最終抽出直前からは通報できません）",
            [TextId.NoticeConveneDeniedNoCorpse] = "会議を開催できません（通報できる死体が無い）",
            [TextId.NoticePlayerDisconnectedFormat] = "{0}がゲームから切断されました",
            [TextId.NoticeConveneHoldHint] = "会議を招集するには、ボタンを長押ししてください",
            [TextId.NoticeScatterSlogan] = "サイロ化を解消しよう[[;)]]",
            [TextId.NoticeScatterGroupLineFormat] = "組分け【{0}】{1}",
            [TextId.NoticeScatterMemberFormat] = "{0}番",
            [TextId.NoticeScatterGuardTripped] = "引き継ぎのトラブルが発生しました",
            [TextId.NoticeScatterGroupSeparator] = "・",
            [TextId.VoteScatterBadgeFormat] = "【{0}】",
            [TextId.VoteScatterBadgeUnknown] = "【？】",

            [TextId.OutfitBlockedHeader] = "着替えられません",
            [TextId.OutfitBlockedBody] = "人狼の試合中は見た目を変更できません。ロビーで着替えるか、ルーム設定を変更してください。",
            [TextId.OutfitBlockedOk] = "OK",

            [TextId.RevealTeammatePrefix] = "人狼仲間：",
            [TextId.RevealHeadingWinCondition] = "◆ 勝利条件（いずれか）",
            [TextId.RevealHeadingAbility] = "◆ あなたの能力",
            [TextId.RevealVillagerTitleMaybeCat] = "あなたは村人…かもしれません。",
            [TextId.RevealVillagerTitle] = "あなたは村人です",
            [TextId.RevealVillagerWinCondition1] = "全ての抽出ポイントを完了し、トラックを発車させる",
            [TextId.RevealVillagerWinCondition2] = "人狼陣営を全滅させる",
            [TextId.RevealVillagerFlavor] = "特別な能力はない。貴重品の回収と会議で人狼に立ち向かえ。",
            [TextId.RevealWolfTeamWinCondition1] = "その時点で存在する貴重品を全て集めても、最後の抽出を完了できない状態にする",
            [TextId.RevealWolfTeamWinCondition2] = "時間切れまでトラックを発車させない",
            [TextId.RevealWolfTeamWinCondition3] = "村人陣営を全滅させる",
            [TextId.RevealWolfAbility1] = "貴重品を壊すほどバフがかかる",
            [TextId.RevealWolfAbility2] = "マップで敵の位置が分かる",
            [TextId.RevealWolfAbility3] = "ビーコンで敵を今いる場所におびき寄せる",
            [TextId.RevealBomberAbility1] = "十分な時間近くで過ごした他のプレイヤーを爆弾に変えられる",
            [TextId.RevealBomberAbility2] = "好きなタイミングで起爆し、周囲を破壊する。爆弾にされた本人もダメージを受けるが、HP1で耐える",
            [TextId.RevealBomberAbility3] = "その爆発に巻き込まれると自分が即死する",
            [TextId.RevealBlackCatAbility] = "処刑対象に選ばれると、自分に投票したプレイヤーのうち1人を道連れに死亡させる",
            [TextId.RevealBlackCatNoCurse] = "人狼とは互いに正体を知らされないが、人狼陣営として勝敗を共にする",
            [TextId.RevealWerewolfTitle] = "あなたは人狼です",
            [TextId.RevealBlackCatTitle] = "あなたは黒猫です",
            [TextId.RevealBlackCatAwakeningTitle] = "あなたは黒猫でした。",
            [TextId.RevealBomberTitle] = "あなたは爆弾魔です",
            [TextId.RevealShamanTitle] = "あなたは祈祷師です",
            [TextId.RevealShamanAbility1] = "立ち止まって視線を据えると視界が褪せ、霊視が始まる。未発見の死体がある方を見つめ続けると、霊障がする",
            [TextId.RevealShamanAbility2] = "未発見の死体に近づくほど、画面に強い霊障が出る",
            [TextId.RevealHeadingTips] = "◆ ヒント",
            [TextId.RevealVillagerTipConvene] = "トラック最奥の赤いボタン長押しで会議を開ける",
            [TextId.RevealVillagerTipReport] = "死体の近くで通報キーを押すと会議を開ける",
            [TextId.RevealVillagerTipValuableMap] = "会議のたびにマップの貴重品情報が更新される",
            [TextId.RevealVillagerTipAliveCheck] = "会議では全プレイヤーの生死を確認できる",
            [TextId.TutorialSkipHint] = "[menu] でスキップ",
            [TextId.RevealSelfIdFormat] = "あなたの識別番号はNo.{0}",

            [TextId.RoleNameWerewolf] = "人狼",
            [TextId.RoleNameBlackCat] = "黒猫",
            [TextId.RoleNameVillager] = "村人",
            [TextId.RoleNameBomber] = "爆弾魔",
            [TextId.RoleNameShaman] = "祈祷師",

            [TextId.GaugePerkStaminaLabel] = "無限スタミナ",
            [TextId.GaugePerkJumpLabel] = "追加ジャンプ",
            [TextId.GaugePerkEnemyIgnoreLabel] = "敵認識無効",
            [TextId.GaugePerkHealLabel] = "自然治癒",
            [TextId.GaugePerkInformantLabel] = "黒猫に人狼開示",

            [TextId.GaugeBeaconRuleFormat] = "ビーコン+1/{0}%",
            [TextId.GaugeBombRuleFormat] = "爆弾+1/{0}%",
            [TextId.GaugePercentDollarsFormat] = "{0}% (${1})",
            [TextId.GaugeLossOverBaseFormat] = "${0} / ${1}",
            [TextId.GaugeNextUpdateFormat] = "次の更新まで {0}秒",

            [TextId.HudTimerFrozenFormat] = "{0}  停止中",
            [TextId.HudGaugeFormat] = "ゲージ {0}%",
            [TextId.HudRightsFormat] = "会議招集残 {0} 回",
            [TextId.HudTimeRemainingFormat] = "残り {0}:{1}",
            [TextId.HudWolfToggleFormat] = "狼化 [{0}]",
            [TextId.HudBeaconKeyFormat] = "ビーコン [{0}]",
            [TextId.HudBeaconLabel] = "ビーコン",
            [TextId.HudTestPlayBanner] = "テストプレイ中：ホストがデバッグモードを有効にしています",
            [TextId.HudScatterGuardBanner] = "引き継ぎ期間中…",

            [TextId.BannerMeetTheDeadline] = "納期は厳守せよ",
            [TextId.BannerHurryUp] = "Hurry up!!",

            [TextId.ImpactDiscussLeft] = "議論",
            [TextId.ImpactDiscussRight] = "開始",

            [TextId.HudSelfIdFormat] = "No.{0}",
            [TextId.IdNameFormat] = "{0}. {1}",

            [TextId.StartHoldWaitingOthers] = "他のプレイヤーの準備を待っています…",

            [TextId.PreplayAbortTitle] = "試合を開始できませんでした",
            [TextId.PreplayAbortTooFewPlayers] =
                "ロード中に参加者が退出し、最低参加人数の3人を下回りました。",
            [TextId.PreplayAbortInvalidConfig] =
                "ロード中の退出により、現在の参加人数では設定された配役が成立しません。",
            [TextId.PreplayAbortRoleRevealDisconnect] =
                "役職公開中の退出により、試合を続行できる陣営構成ではなくなりました。",
            [TextId.PreplayAbortNoRoles] = "このラウンドでは役職は割り当てられていません。",
            [TextId.PreplayAbortRolesDiscarded] =
                "公開済みの役職は破棄され、勝敗・報酬・リプレイは記録されません。",

            [TextId.ResultBannerVillagerWin] = "村人陣営の勝利",
            [TextId.ResultBannerWerewolfWin] = "人狼陣営の勝利",
            [TextId.ResultBannerDefault] = "試合結果",

            [TextId.ResultBannerVoid] = "無効試合",

            [TextId.ResultBannerPeacefulSurvived] = "平和村は生き延びた",
            [TextId.ResultBannerPeacefulCollapsed] = "平和村は崩壊した",
            [TextId.VoidMatchChargingLabel] = "無効試合",
            [TextId.VoidMatchArmHeader] = "この試合を無効にしますか？",
            [TextId.VoidMatchArmBody] =
                "勝者なしで試合を終了します。コスメティックトークンも配布されません。",
            [TextId.VoidMatchConfirmPromptFormat] = "確定するには {0} キーを {1} 秒長押し",
            [TextId.VoidMatchCancelHintFormat] = "Esc で取消（{0} 秒で自動的に閉じます）",
            [TextId.ResultStatusAlive] = "生存",
            [TextId.ResultStatusDead] = "死亡",
            [TextId.ResultStatusExecuted] = "処刑",
            [TextId.ResultStatusDisconnected] = "切断",
            [TextId.ResultReturnButtonLabel] = "ロビーへ戻る",
            [TextId.ResultReturnButtonWithKeyFormat] = "ロビーへ戻る [{0}]",
            [TextId.ResultAutoReturnCountdownFormat] = "自動帰還まで約 {0} 秒",
            [TextId.ResultWaitingHost] = "ホストの操作を待っています…",
            [TextId.ResultFooterWithCountdownFormat] = "{0}　｜　自動帰還まで約 {1} 秒",

            [TextId.ResultDigestHeader] = "── 試合ログ（マウスホイールでスクロール）──",
            [TextId.DigestMatchStart] = "試合開始",
            [TextId.DigestMeetingButtonFormat] = "{0} が会議を召集",
            [TextId.DigestMeetingReportFormat] = "{0} が死体を通報",
            [TextId.DigestExecutedFormat] = "投票により {0} が処刑",
            [TextId.DigestNoExecution] = "開票: 処刑なし",
            [TextId.DigestCurseStartedFormat] = "処刑された {0} は黒猫だった",
            [TextId.DigestCurseFollowFormat] = "黒猫の道連れで {0} が死亡",
            [TextId.DigestDeathFormat] = "{0} が死亡",
            [TextId.DigestBombDetonatedFormat] = "{0} に仕掛けられた爆弾が爆発",
            [TextId.DigestCheckmate] = "資産詰みが成立",
            [TextId.DigestMatchEndFormat] = "{0}（{1}）",
            [TextId.DigestReasonWerewolvesEradicated] = "人狼全滅",
            [TextId.DigestReasonVillagersEradicated] = "村人陣営全滅",
            [TextId.DigestReasonExtractionCompleted] = "トラック発車",
            [TextId.DigestReasonTimerExpired] = "時間切れ",
            [TextId.DigestReasonExtractionFailed] = "抽出失敗",
            [TextId.DigestReasonValueCheckmate] = "資産詰み",
            [TextId.DigestExtractionDoneFormat] = "抽出完了（{0}/{1}）",
            [TextId.DigestPerkUnlockedFormat] = "人狼特典「{0}」解禁",
            [TextId.DigestInformant] = "内通が成立（黒猫へ人狼陣営を開示）",
            [TextId.DigestFinalBalanceFormat] = "最終収支: 抽出 ${0} ／ 残ノルマ ${1} ／ 回収可能 ${2}",

            [TextId.EpigraphWerewolvesEradicated] = "人狼は全滅した",
            [TextId.EpigraphVillagersEradicated] = "村人は全滅した",
            [TextId.EpigraphExtractionCompleted] = "債権を回収した",
            [TextId.EpigraphTimerExpired] = "履行遅滞に陥った",
            [TextId.EpigraphExtractionFailed] = "債権が焦げ付いた",
            [TextId.EpigraphValueCheckmate] = "債務超過に陥った",
            [TextId.EpigraphPeacefulSurvived] = "人狼かと疑った。それでも手を取り合った。",
            [TextId.EpigraphPeacefulCollapsed] = "人狼などいなかった。それでも村は滅びた。",

            [TextId.VoteMeetingTitle] = "緊急会議",
            [TextId.VoteSkipLabel] = "スキップ",
            [TextId.VoteConfirmLabel] = "本当に？",
            [TextId.VoteVoteLabel] = "投票",
            [TextId.VoteWerewolfMarkerLabel] = "狼",
            [TextId.VoteBomberMarkerLabel] = "爆",
            [TextId.VoteCountFormat] = "{0}票",
            [TextId.VoteExecutedFormat] = "処刑: {0}",
            [TextId.VoteNoExecution] = "処刑なし",
            [TextId.VoteSkipSuffixFormat] = "（スキップ {0}票）",

            [TextId.ChatLogTitle] = "会議チャットログ",
            [TextId.ChatLogEmpty] = "まだ発言はありません",
            [TextId.ChatLogDeadHint] = "灰色の発言は冥界にだけ見えています",
            [TextId.ChatLogToggleLabelFormat] = "会議チャットログ [{0}]",
            [TextId.ChatLogVoted] = "が投票しました。",
            [TextId.ChatLogSystemName] = "Taxman",
            [TextId.ChatLogScatterTitle] = "前回の組分け",
            [TextId.ChatLogScatterLineFormat] = "【{0}】{1}",
            [TextId.ChatLogMeetingNumberFormat] = "第{0}回の会議",
            [TextId.ChatLogFilteringTitle] = "フィルター中",

            [TextId.ChatStampButtonLabel] = "スタンプ",
            [TextId.ChatStampUnknown] = "（未対応のスタンプ）",
            [TextId.ChatStampCategoryGuide] = "会議誘導",
            [TextId.ChatStampCategoryPress] = "追及",
            [TextId.ChatStampCategoryDefense] = "弁明",
            [TextId.ChatStampCategoryClaim] = "主張",
            [TextId.ChatStampCategoryChatter] = "雑談",
            [TextId.ChatStampGuideVoteSkip] = "スキップに入れましょう",
            [TextId.ChatStampGuideVoteSuspect] = "怪しい人に投票しましょう",
            [TextId.ChatStampGuideExtraction] = "納品勝ちを目指しましょう",
            [TextId.ChatStampGuideFriendly] = "ほんわかレス推奨です！",
            [TextId.ChatStampPressSuspicious] = "怪しいですね",
            [TextId.ChatStampPressWhere] = "どこにいましたか？",
            [TextId.ChatStampPressWithWhom] = "誰といましたか？",
            [TextId.ChatStampPressDoing] = "何をしていましたか？",
            [TextId.ChatStampPressItems] = "アイテムは何を持っていますか？",
            [TextId.ChatStampPressDropWeapon] = "武器を手放してください",
            [TextId.ChatStampDefenseInnocent] = "無実です",
            [TextId.ChatStampDefenseWasDead] = "死んでいました",
            [TextId.ChatStampDefenseAccident] = "事故です",
            [TextId.ChatStampDefenseDontKnow] = "知りません",
            [TextId.ChatStampDefenseForgot] = "忘れました",
            [TextId.ChatStampDefenseNoIdea] = "なんもわからん",
            [TextId.ChatStampClaimAgree] = "その通り！",
            [TextId.ChatStampClaimDisagree] = "それは違うよ！",
            [TextId.ChatStampClaimObjection] = "異議あり！",
            [TextId.ChatStampClaimTheyDidIt] = "あの人がやっていました",
            [TextId.ChatStampClaimItWasMe] = "それは…ボクだよ！",
            [TextId.ChatStampClaimTogether] = "一緒に行動しましょう",
            [TextId.ChatStampChatterMorning] = "おはよ",
            [TextId.ChatStampChatterGg] = "お疲れ様でした",
            [TextId.ChatStampChatterWantLive] = "生きたい！",
            [TextId.ChatStampChatterDied] = "死んでしまいました…",
            [TextId.ChatStampChatterLookAtMe] = "こんなになっちゃった…たはは",
            [TextId.ChatStampChatterAsPlanned] = "計画通り",
            [TextId.ChatStampChatterWhatever] = "なんとかなれー！",
            [TextId.ChatStampChatterSorry] = "ごめんね",
            [TextId.ChatStampChatterHelp] = "助けてください！",
            [TextId.ChatStampChatterCry] = "うえーん",

            [TextId.ResultChatTitle] = "感想戦チャット",
            [TextId.ResultChatToggleLabelFormat] = "感想戦チャット [{0}]",

            [TextId.ReplayToggleLabel] = "リプレイ再生",
            [TextId.ReplayPlay] = "再生",
            [TextId.ReplayPause] = "停止",
            [TextId.ReplaySpeedFormat] = "{0}倍速",
            [TextId.ReplaySaveButton] = "保存",
            [TextId.ReplaySavedButton] = "保存済み",
            [TextId.ReplaySavedToDownloads] = "リプレイを保存しました（ダウンロード）",
            [TextId.ReplaySavedToModFolder] = "リプレイを保存しました（MODフォルダ内 Replays）",
            [TextId.ReplayAlreadySaved] = "この試合は保存済みです",
            [TextId.ReplaySaveFailed] = "保存に失敗しました",
            [TextId.ReplayNoData] = "リプレイデータがありません",
            [TextId.ReplayPositionCapped] = "録画上限に達したため、ここから先は位置の記録がありません",
            [TextId.ReplaySavedCapped] = "（位置は途中まで）",
            [TextId.ReplayGaugeLossFormat] = "減額 ${0} / 総額 ${1}",
            [TextId.ReplayGaugeDeliveredFormat] = "抽出 ${0}",
            [TextId.ReplayEpStateNone] = "未起動",
            [TextId.ReplayEpStateIdle] = "待機",
            [TextId.ReplayEpStateActive] = "受付中",
            [TextId.ReplayEpStateSuccess] = "ノルマ達成",
            [TextId.ReplayEpStateWarning] = "作動中",
            [TextId.ReplayEpStateCancel] = "中止",
            [TextId.ReplayEpStateExtracting] = "吸い込み中",
            [TextId.ReplayEpStateComplete] = "完了",
            [TextId.ReplayEpStateSurplus] = "余剰",
            [TextId.ReplayEpStateTaxReturn] = "お釣り",
            [TextId.ReplayStampNoExecution] = "投票結果／処刑なし",
            [TextId.ReplayStampExecutedFormat] = "処刑／{0}. {1}",

            [TextId.RecapNameSeparator] = "、",
            [TextId.RecapDeathsFormat] = "死亡: {0}",
            [TextId.RecapDeathsNone] = "死亡: なし",
            [TextId.RecapLostFormat] = "破壊された貴重品: -${0}",
            [TextId.RecapHaulFormat] = "抽出: ${0} ／ ノルマ ${1}",
            [TextId.RecapBeaconFormat] = "ビーコン使用: {0}回",
            [TextId.RecapBeaconNone] = "ビーコン使用: なし",

            [TextId.DeathRevealTitle] = "死亡者",
            [TextId.DeathRevealNone] = "誰も死んでいない",
            [TextId.DeathRevealScatterGuardTitle] = "引き継ぎトラブル発生",

            [TextId.CheckmateTitle] = "債権はもはや徴収不能だ",

            [TextId.EradicationLastWerewolfDied] = "最後の人狼が死亡した",
            [TextId.EradicationLastVillagerDied] = "最後の村人が死亡した",
            [TextId.EradicationLastWerewolfVanished] = "最後の人狼が失踪した",
            [TextId.EradicationLastVillagerVanished] = "最後の村人が失踪した",
            [TextId.EradicationPeacefulCollapsed] = "平和村が消滅した",

            [TextId.MapOverlayToggleLabelFormat] = "全体マップ [{0}]",
            [TextId.MapOverlayMapToolBlocked] = "全体マップを開くには、手持ちのマップを閉じてください",

            [TextId.LobbySettingsFooterHintFormat] = "[{0}]キー : パネルを隠す ／ ホイール : スクロール",
            [TextId.LobbySettingsMiniHintFormat] = "[{0}]キー : 人狼の部屋設定を表示",

            [TextId.ModIntegrityHeaderAllMatchFormat] = "✓ MOD構成一致 {0}/{1}",
            [TextId.ModIntegrityHeaderCountsFormat] = "基準 {0}  ✓ {1}  ! {2}  × {3}  ? {4}",
            [TextId.ModIntegritySelfDifferenceFormat] = "あなたの構成に差分があります（不足{0} / 追加{1} / Version{2} / 内容{3}）",
            [TextId.ModIntegritySelfUnavailable] = "あなたのMOD情報を確認できていません",
            [TextId.ModIntegrityPanelTitle] = "MOD構成（ルーム基準比較）",
            [TextId.ModIntegrityFilterNeedsReview] = "要確認",
            [TextId.ModIntegrityFilterMatch] = "一致",
            [TextId.ModIntegrityFilterAll] = "すべて",
            [TextId.ModIntegrityStatusBaseline] = "基準",
            [TextId.ModIntegrityStatusPending] = "確認中",
            [TextId.ModIntegrityStatusMatch] = "一致",
            [TextId.ModIntegrityStatusDifference] = "差分あり",
            [TextId.ModIntegrityStatusUnavailable] = "取得不可",
            [TextId.ModIntegrityReasonNoResponse] = "応答なし",
            [TextId.ModIntegrityReasonUnsupportedProtocol] = "非対応バージョン",
            [TextId.ModIntegrityReasonInvalidPayload] = "比較不能な応答",
            [TextId.ModIntegrityReasonTooLarge] = "情報量が上限超過",
            [TextId.ModIntegrityReasonCollectionFailed] = "MOD情報の収集失敗",
            [TextId.ModIntegrityDetailLoading] = "詳細を取得中…",
            [TextId.ModIntegrityDetailFailed] = "詳細を取得できませんでした  再取得",
            [TextId.ModIntegrityDisclaimer] = "この表示はクライアント申告のルーム基準比較であり、チート不在を保証しません。",
            [TextId.ModIntegrityDetailMissingFormat] = "不足: {0} ({1})",
            [TextId.ModIntegrityDetailExtraFormat] = "追加: {0} ({1})",
            [TextId.ModIntegrityDetailVersionFormat] = "Version: {0}  基準 {1} → 参加者 {2}",
            [TextId.ModIntegrityDetailContentFormat] = "内容: {0}  基準 {1} → 参加者 {2}",
            [TextId.ModIntegrityStartCautionTitle] = "MOD構成に差分があります",
            [TextId.ModIntegrityStartSevereTitle] = "未確認のMOD構成があります",
            [TextId.ModIntegrityStartBodyFormat] = "差分あり {0}人 / 取得不可 {1}人 / 確認中 {2}人\n公平性へ影響する可能性があります。",
            [TextId.ModIntegrityButtonBack] = "戻る",
            [TextId.ModIntegrityButtonDetails] = "構成を確認",
            [TextId.ModIntegrityButtonContinue] = "それでも開始する",
            [TextId.ModIntegrityButtonClose] = "閉じる",
            [TextId.LobbyStartTooFewPlayersTitle] = "人数が足りません",
            [TextId.LobbyStartTooFewPlayersBodyFormat] =
                "現在 {0}人 / 人狼モードで遊ぶには最低 {1}人 必要です。",
            [TextId.LobbyStartTeamOverflowTitle] = "人狼の人数が多すぎます",
            [TextId.LobbyStartTeamOverflowBodyFormat] =
                "人狼の設定 {0}人 に対し参加者 {1}人 では村人が1人もいません。\n"
                + "人狼の人数（WerewolfCount）を下げてください。",
            [TextId.LobbyStartBlockedRemedy] =
                "通常のREPOとして遊ぶ場合は、ホストの設定で\n"
                + "人狼モード（WerewolfModeEnabled）をOFFにしてください。",

            [TextId.ConveneCountdownDefaultCallerName] = "誰か",
            [TextId.ConveneCountdownHeaderFormat] = "{0}が会議を招集しました！\nワープまで残り…",
            [TextId.ConveneCountdownCorpseHeaderFormat] = "{0}が事件現場を通報しました！\nワープまで残り…",
            [TextId.ConveneCountdownScatterGuardHeader] = "引き継ぎのトラブルが発生しました！\nワープまで残り…",

            [TextId.HudCorpseReportKeyFormat] = "通報 [{0}]",

            [TextId.MeetingButtonSuppressCountdownFormat] = "緊急会議（あと {0}秒）",
            [TextId.MeetingButtonConveneGrabPrompt] = "緊急会議を招集 [Grabを長押し]",
            [TextId.MeetingButtonConveneInteractPrompt] = "緊急会議を招集 [Interactを長押し]",
            [TextId.MeetingButtonSuppressedPrompt] = "緊急会議（現在は招集不可）",
            [TextId.MeetingButtonRightsSuffixFormat] = "（残り{0}回）",

            [TextId.CurseBlackCatRevealedFormat] = "{0}は黒猫でした。道連れを選択中…",
            [TextId.CurseNoVictim] = "誰も道連れになりませんでした",

            [TextId.SettingsSectionGeneral] = "基本",
            [TextId.SettingsSectionMeeting] = "会議",
            [TextId.SettingsSectionRoleAssignment] = "役職 - 役職配分",
            [TextId.SettingsSectionRoles] = "役職 - 人狼",
            [TextId.SettingsSectionBlackCat] = "役職 - 黒猫",
            [TextId.SettingsSectionBomber] = "役職 - 爆弾魔",
            [TextId.SettingsSectionShaman] = "役職 - 祈祷師",
            [TextId.SettingsSectionWorldgen] = "開始環境",
            [TextId.SettingsSectionStartItemList] = "持ち込みアイテム一覧",
            [TextId.SettingsSectionStartUpgradeList] = "能力強化（全員）",

            [TextId.SettingsBoolEnabled] = "有効",
            [TextId.SettingsBoolDisabled] = "無効",
            [TextId.SettingsAuto] = "自動",
            [TextId.SettingsValueVanilla] = "バニラのまま",
            [TextId.SettingsListEmpty] = "なし",
            [TextId.SettingsPresent] = "あり",
            [TextId.SettingsRandom] = "ランダム",
            [TextId.SettingsValuableMapRealtime] = "リアルタイム",
            [TextId.SettingsValuableMapMeetingSync] = "会議同期",
            [TextId.SettingsValuableMapHidden] = "非表示",
            [TextId.SettingsNecroVoiceOff] = "OFF",
            [TextId.SettingsNecroVoiceNonWerewolfDead] = "人狼以外の死者",
            [TextId.SettingsNecroVoiceAllDead] = "全死者",
            [TextId.SettingsItemsAggregateFormat] = "{0}種{1}個（{2}）",
            [TextId.SettingsAggregateMoreSuffix] = "…",
            [TextId.SettingsUpgradeItemFormat] = "{0}+{1}",
            [TextId.SettingsListSeparator] = "、",
            [TextId.SettingsUnitSeconds] = "秒",
            [TextId.SettingsUnitTimes] = "回",
            [TextId.SettingsUnitItems] = "個",
            [TextId.SettingsUnitPeople] = "人",
            [TextId.SettingsUnitMeters] = "m",
            [TextId.SettingsUnitPercent] = "%",
            [TextId.SettingsUnitDamage] = "ダメージ",
            [TextId.SettingsUnitSets] = "セット",

            [TextId.SettingsLabelWerewolfCount] = "人狼の人数",
            [TextId.SettingsLabelBlackCatChancePercent] = "黒猫の出現確率",
            [TextId.SettingsLabelBomberChancePercent] = "爆弾魔の出現確率",
            [TextId.SettingsLabelPeacefulVillageChancePercent] = "平和村の確率",
            [TextId.SettingsLabelShamanChancePercent] = "祈祷師の出現確率",
            [TextId.SettingsLabelRoundSeconds] = "ラウンド制限時間",
            [TextId.SettingsLabelBlackCatRevealDelaySec] = "黒猫自覚の遅延",
            [TextId.SettingsLabelBlackCatCurseEnabled] = "黒猫の道連れ",
            [TextId.SettingsLabelMeetingRightsPerPlayer] = "1人あたりの会議開催権",
            [TextId.SettingsLabelConveneSuppressStartSec] = "開始直後の召集抑止",
            [TextId.SettingsLabelConveneSuppressAfterSec] = "会議後の召集抑止",
            [TextId.SettingsLabelMeetingCountdownSec] = "会議開始までの予告時間",
            [TextId.SettingsLabelMeetingDurationSec] = "会議の制限時間",
            [TextId.SettingsLabelVoteTimeCutEnabled] = "投票による会議時間短縮",
            [TextId.SettingsLabelMeetingScatterEnabled] = "会議後の分散ワープ",
            [TextId.SettingsLabelScatterGuardSec] = "分散ワープ後の監視時間",
            [TextId.SettingsLabelMeetingEnemyRespawnScalePct] = "会議中の敵リスポーン係数",
            [TextId.SettingsLabelResultDisplaySec] = "開票結果の表示保持",
            [TextId.SettingsLabelStaminaUnlockPct] = "無限スタミナ解禁閾値",
            [TextId.SettingsLabelJumpUnlockPct] = "追加ジャンプ解禁閾値",
            [TextId.SettingsLabelEnemyIgnoreUnlockPct] = "敵認識無効の解禁閾値",
            [TextId.SettingsLabelHealUnlockPct] = "自然治癒の解禁閾値",
            [TextId.SettingsLabelHealIntervalSec] = "自然治癒の回復間隔",
            [TextId.SettingsLabelBeaconChargePct] = "ビーコン補充の閾値",
            [TextId.SettingsLabelInformantThresholdPct] = "黒猫の人狼開示の閾値",
            [TextId.SettingsLabelExtraJumpCount] = "滞空中の追加ジャンプ回数",
            [TextId.SettingsLabelBeaconCooldownSec] = "ビーコンのクールダウン",
            [TextId.SettingsLabelBeaconSuppressStartSec] = "開始直後のビーコン抑止",
            [TextId.SettingsLabelBeaconSuppressAfterMeetingSec] = "会議後のビーコン抑止",
            [TextId.SettingsLabelCatGaugeSyncIntervalSec] = "黒猫ゲージの更新間隔",
            [TextId.SettingsLabelOrbGaugeEnabled] = "オーブの減額ゲージ算入",
            [TextId.SettingsLabelWerewolfModeEnabled] = "人狼モード",
            [TextId.SettingsLabelMinimapHideEnabled] = "死体のミニマップ非表示",
            [TextId.SettingsLabelOutfitChangeAllowed] = "試合中の着替え",
            [TextId.SettingsLabelValuableMapMode] = "貴重品マップ表示モード",
            [TextId.SettingsLabelGameOverAutoReturnSec] = "結果画面の自動帰還秒数（0=自動で戻らない）",
            [TextId.SettingsLabelNecroVoiceMode] = "冥界の声（死者→生存人狼の傍聴）",
            [TextId.SettingsLabelStartLevelNumber] = "レベル",
            [TextId.SettingsLabelStartMapName] = "マップ",
            [TextId.SettingsLabelStartItemsSpec] = "持ち込みアイテム",
            [TextId.SettingsLabelStartEnergyPct] = "トラックの充電器",
            [TextId.SettingsLabelStartUpgradesSpec] = "開始時の能力強化",
            [TextId.SettingsLabelOrbDropMax] = "敵が落とすオーブの数",
            [TextId.SettingsLabelStartHaulGoalPct] = "ノルマ（マップ総額比）",
            [TextId.SettingsLabelEnemySetsDanger1] = "敵のセット数（危険度1）",
            [TextId.SettingsLabelEnemySetsDanger2] = "敵のセット数（危険度2）",
            [TextId.SettingsLabelEnemySetsDanger3] = "敵のセット数（危険度3）",

            [TextId.SettingsLabelBomberProximityMeters] = "濃厚接触判定距離",
            [TextId.SettingsLabelBomberGaugeFullSec] = "濃厚接触判定秒",
            [TextId.SettingsLabelBomberInitialCooldownSec] = "開始直後クールダウン",
            [TextId.SettingsLabelBomberCooldownSec] = "通常クールダウン",
            [TextId.SettingsLabelBomberBlastRadiusMeters] = "爆風・警告音半径",
            [TextId.SettingsLabelBomberBlastPlayerDamage] = "プレイヤーへのダメージ",
            [TextId.SettingsLabelBomberBlastEnemyDamage] = "敵ダメージ",
            [TextId.SettingsLabelBomberAmmoRefillPct] = "弾1発の補充閾値",
            [TextId.SettingsLabelShamanGazeFullSec] = "霊視の注視秒数",
            [TextId.SettingsLabelShamanGhostCooldownSec] = "霊視クールダウン",
            [TextId.SettingsLabelShamanStormWeakMeters] = "霊障（弱）半径",
            [TextId.SettingsLabelShamanStormMediumMeters] = "霊障（中）半径",
            [TextId.SettingsLabelShamanStormStrongMeters] = "霊障（強）半径",
            [TextId.HudBomberPlantKeyFormat] = "爆弾にする [{0}]",
            [TextId.HudBomberDetonateKeyFormat] = "起爆 [{0}]",

            [TextId.HudValuableRecordOnFormat] = "記録する [{0}長押し]",
            [TextId.HudValuableRecordOffFormat] = "記録しない [{0}長押し]",

            [TextId.BomberDenyNoAmmo] = "残弾がありません",
            [TextId.BomberDenyNoFullTarget] = "爆弾にできる対象がいません",
            [TextId.BomberDenyPlantCooldown] = "爆弾にできません（クールダウン中）",
            [TextId.BomberDenyDetonateCooldown] = "起爆できません（クールダウン中）",
            [TextId.BomberDenyNoBomb] = "起爆できません（爆弾なし）",
            [TextId.BomberDenyMeetingLocked] = "起爆できません（会議中）",
            [TextId.BomberDenyTruckZone] = "起爆できません（対象がトラック付近）",
            [TextId.BomberDudTargetDead] = "対象者は既に死亡していたので不発に終わりました",
            [TextId.BomberTargetDisconnected] = "対象者が切断したため爆弾は消滅しました",
            [TextId.BomberProximityWarning] = "爆弾を仕掛けられたかもしれない…",

            [TextId.TutorialCorpseDiscovery] =
                "未発見の死体を発見した。Good Job!😂\n" +
                "死者は蘇らないから抽出ポイントに運ぶ必要はない。\n" +
                "未発見の死体の近くでは、開催権に関係なく通報ができる（右下HUDのアイコンが色付きで脈動する）。\n" +
                "どんな状況で死んでいたか、会議で皆に共有すること。",

            [TextId.TutorialMeetingCountdownStarted] =
                "会議が招集された。トラックへワープするまでのカウントダウンだ。\n" +
                "手に持っている貴重品・武器・ドローンは、その場に残される。インベントリへ仕舞えば持ち帰れる。\n" +
                "貴重品を落として傷つけたり、通路をふさいでモンスターに壊されないよう急いで片付けること。",

            [TextId.TutorialFirstMeetingAsVillager] =
                "会議中は、貴重品の位置と現存状況が更新された全体マップを見られる。\n" +
                "状況を確認し、情報を交換し、誰が怪しいか、誰が怪しくないか報告し合うこと。\n" +
                "怪しい奴には投票しろ。処刑できる。",

            [TextId.TutorialWerewolfRoleDrawn] =
                "お前は人狼だ。おめでとう😂\n" +
                "貴重品を壊すほど、強力な能力が解禁される。\n" +
                "村人にバレないように貴重品を壊し、特典を受け取ること。\n" +
                "人狼にだけは、マップにモンスターの位置が表示される。うまく使え。",

            [TextId.TutorialFirstValuableSeen] =
                "貴重品を発見した。\n" +
                "マップに位置が登録されるが、その後に移動・破壊されてもマップは更新されない。\n" +
                "更新されるのは、会議の開始時と抽出の完了時だけだ。\n" +
                "マップにあるはずの貴重品がそこに無いなら、誰かが運び去ったか、壊したということだ。",

            [TextId.TutorialWolfModeFirstUnlock] =
                "十分な額の貴重品が破壊された。狼化が解禁だ。\n" +
                "狼化中は強力な能力が使える。\n" +
                "村人の前でうっかり能力を使うな。狼化はオフにもできる。",

            [TextId.TutorialBeaconFirstCharged] =
                "十分な額の貴重品が破壊された。ビーコンが解禁だ。\n" +
                "ビーコンは、マップの広い範囲からモンスターを現在位置へ誘導する。倒されて消えたモンスターも呼び戻す。\n" +
                "モンスターが殺到している場所にいた奴は発信源だと怪しまれる。使う場所を選ぶこと。\n" +
                "マップでモンスターの位置を確認しろ。自分が巻き込まれても知らないぞ。",

            [TextId.TutorialFirstMeetingAsWerewolf] =
                "会議中はマップの貴重品情報が更新され、様々な情報が全員に共有される。\n" +
                "人狼が処刑されると、人狼陣営は不利になる。\n" +
                "言い訳をしろ。潔白を装え。無意味な情報を話し、大事な情報は隠せ。嘘で怪しい奴を増やせ。\n" +
                "健闘を祈る😂",

            [TextId.TutorialFirstMeetingAsBlackCat] =
                "黒猫は会議で処刑されると、自分に投票した奴の中から誰か1人を道連れにできる。\n" +
                "道連れにしたい相手が決まっているなら、そいつの票が自分に入るよう誘導すること。\n" +
                "仲間の人狼も、黒猫に投票していれば道連れ対象に入る。連れて行く相手は慎重に選べ。",

            [TextId.TutorialVillagerSeesCatAwakened] =
                "業務連絡だ。この村には人狼陣営に黒猫がいるかもしれない。\n" +
                "黒猫は会議で処刑されると、黒猫に投票した者の中から1人を道連れに殺せる。\n" +
                "会議で処刑するか。反撃とリソース消費を覚悟のうえで、実力行使で排除するか。\n" +
                "慎重に決めること。",

            [TextId.TutorialBlackCatRoleDrawn] =
                "お前は黒猫だ。おめでとう😂\n" +
                "会議の投票で処刑されると、黒猫に投票した奴の中から誰か1人を道連れにできる。\n" +
                "村人のフリで忙しい人狼の代わりに、大暴れしてサポートしろ。\n" +
                "ただし、誰が人狼かは知らされない。うっかり人狼を道連れにしないこと。\n" +
                "黒猫は人狼陣営だが、人狼の生存数には数えない。人狼が全滅すれば、黒猫が生きていても村人陣営の勝ちだ。",

            [TextId.TutorialBlackCatRoleDrawnNoCurse] =
                "お前は黒猫だ。おめでとう😂\n" +
                "黒猫は人狼陣営だが、人狼と黒猫はお互いの正体を知らされない。\n" +
                "村人のフリで忙しい人狼の代わりに、大暴れしてサポートしろ。\n" +
                "黒猫は人狼の生存数には数えない。人狼が全滅すれば、黒猫が生きていても村人陣営の勝ちだ。",

            [TextId.TutorialLastRunApproaching] =
                "最終抽出指令が出た。これ以降、死体通報は受理されない。\n" +
                "会議はトラックの会議ボタンからのみ開ける。\n" +
                "誰をトラックに残し、誰に最後の抽出ポイントを任せるか慎重に決めろ。",

            [TextId.TutorialRoundTimeWarningVillager] =
                "間もなく納期だ。\n" +
                "納期までに全ての抽出ポイントを終え、トラックを発車させろ。できなければ村人の負けだ。\n" +
                "間に合わないなら、人狼を全滅させるしかない。",

            [TextId.TutorialRoundTimeWarningWerewolf] =
                "間もなく納期だ。\n" +
                "納期までにトラックの発車を阻止すれば、人狼の勝ちだ。\n" +
                "最後まで気を抜くな。",

            [TextId.TutorialFinalExtractionVillager] =
                "最終抽出を完了した。\n" +
                "トラックの発車時に村人が1人でも生きていれば勝利だ。\n" +
                "最後まで気を抜くな。",

            [TextId.TutorialFinalExtractionWerewolf] =
                "最終抽出が完了してしまった。\n" +
                "トラックの発車時に村人が生きていれば人狼の負けだ。\n" +
                "こうなったら、村人を全滅させるしかない。",

            [TextId.TutorialInformantUnlockedAsWerewolf] =
                "もし黒猫がいたらの話だが、黒猫は誰が人狼か知ったようだ。\n" +
                "どこかでこっそり接触できるように動くと良いだろう。\n" +
                "それか、会議で向こうから仕掛けてくるかもしれない。うまく話を合わせろ。",

            [TextId.TutorialInformantUnlockedAsBlackCat] =
                "十分な額の貴重品が破壊された。人狼が誰か分かるようになった。\n" +
                "名前が赤く見えたり、会議の時に狼のマークがついているのが人狼だ。\n" +
                "村人にバレないようにこっそり人狼に自分が黒猫だとアピールしろ。",

            [TextId.TutorialEnemyIgnoreUnlockedAsWerewolf] =
                "十分な額の貴重品が破壊された。モンスターはお前を仲間だとみなすようになった。\n" +
                "狼化が有効な間は、ほとんどのモンスターはお前を見ても反応しなくなるぞ。\n" +
                "音には反応するし攻撃の巻き添えは食らうがな。\n" +
                "ちなみに、大きな音を出すと消えたモンスターが早く復活することはもう知っているか？",

            [TextId.TutorialNaturalHealUnlockedAsWerewolf] =
                "十分な額の貴重品が破壊された。お前の肉体は人間離れしつつある。\n" +
                "狼化が有効な間、体力が少しずつ自然に回復するようになった。\n" +
                "光ったり音が出たりはしないが、背中の体力ゲージは他のプレイヤーからも見える。\n" +
                "さっきまで瀕死だったのにいつの間にか回復していたら、言い逃れは難しいだろう。",

            [TextId.TutorialWerewolfSeesCatAwakened] =
                "業務連絡だ。この村には人狼陣営に黒猫がいるかもしれない。\n" +
                "人狼と黒猫は仲間同士だが、人狼からは誰が黒猫かは分からない。\n" +
                "黒猫は人狼陣営だが、人狼の生存数には数えない。（人狼が全滅すれば、黒猫の生死に関わらず村人陣営の勝利）",

            [TextId.TutorialBeaconFirstUsedAsWerewolf] =
                "ビーコンを使用したな。\n" +
                "マップ中のモンスターがビーコンを使った場所へ集まってくるだろう。\n" +
                "ビーコンが何回使われたかは会議で明かされるから、村人に推理材料を与えないように気をつけろ。",

            [TextId.TutorialBlackCatSelectedForExecution] =
                "処刑対象に選ばれたな。Good Job!😂\n" +
                "誰を道連れにするか選べ。\n" +
                "選ばないとランダムで選ばれる。",

            [TextId.TutorialBlackCatExecutionRevealed] =
                "最多票を集めたのは黒猫だった。\n" +
                "黒猫が処刑されると、黒猫に投票したプレイヤーの中から誰か1人が道連れにされる。",

            [TextId.TutorialFirstDeath] =
                "お前の身体は破壊され、今やクラウドにアップロードされた精神データだけの存在だ。\n" +
                "試合の間は新しい身体が支給されることはない。\n" +
                "死体の頭を使って生きている連中と話すこともできない。",

            [TextId.TutorialBomberRoleDrawn] =
                "お前は爆弾魔だ。おめでとう😂\n" +
                "誰かとしばらく一緒に過ごすと、そいつを人間爆弾に変えられるようになる。\n" +
                "好きなタイミングで起爆できる。爆弾にされた本人も少しはダメージを受けるが、HP1で耐え、吹き飛ばない。\n" +
                "自分が巻き込まれたら即死する。距離を取ってから起爆しろ。",

            [TextId.TutorialBombPlantedAsBomber] =
                "上手く爆弾を取り付けたな。Good Job!😂\n" +
                "あとは自分が巻き込まれないように気をつけながら起爆するだけだ。\n" +
                "取り付けた相手がトラック周辺にいたら起爆させないぞ。トラックが傷つくからな。",

            [TextId.TutorialBomberProximityWarnedAsVillager] =
                "貴重品を効率よく差し押さえるためには他の誰かとの協力が必要なこともあるだろう。\n" +
                "しかし、爆弾魔はしばらく近くで過ごした他人に爆弾を仕掛けることができる。\n" +
                "後で自分が爆弾にされていたと分かったときのために、誰と一緒に仕事していたか覚えておくんだな。",

            [TextId.TutorialSelfBombExplodedAsVillager] =
                "お前は爆弾魔に爆弾を仕掛けられていたようだ。\n" +
                "爆発の中心にいたため傷ついたが、爆弾そのものでは死なず、吹き飛ばされることもない。\n" +
                "爆弾魔はしばらく近くで過ごした他人を爆弾に変え、好きなタイミングで起爆できる。\n" +
                "爆弾魔自身はその爆発に巻き込まれると即死する。\n" +
                "爆弾魔が取り付けられる爆弾は同時に一つまでだ。",

            [TextId.TutorialShamanRoleDrawn] =
                "お前は祈祷師だ。\n" +
                "しばらく立ち止まり、視線を止めると霊視ができる。\n" +
                "未周知の死体の近くにいるときは霊障が出る。\n" +
                "お前が死体にならないように気をつけろ。",

            [TextId.TutorialShamanGhostSighted] =
                "霊視が反応した。\n" +
                "今向いていた方向に未周知の死体があるということだ。",

            [TextId.TutorialShamanTranceEntered] =
                "水滴が聞こえた。\n" +
                "これは「今向いている方向に未周知の死体はない」という合図だ。",

            [TextId.TutorialShamanStormEntered] =
                "霊障が発生した。\n" +
                "近くに未周知の死体がある。霊障が強いほど、死体は近い。",

            [TextId.TutorialEquipBlockedByOtherGrabber] =
                "他人が掴んでいるアイテムはインベントリに入れられない。\n" +
                "奪いたいなら、近接武器で殴るなりして落とさせてから拾え。",

            [TextId.TutorialValuableRecordSuppressed] =
                "貴重品を見つけたが、地図には記録しなかった。\n" +
                "記録をOFFにしている間は貴重品を発見しない。誰にも知られていない貴重品なら、\n" +
                "壊しても「あったはずの物が無い」と気づかれずに済む。\n" +
                "記録を再開したくなったら、通報キーの長押しで切り替えろ。",

            [TextId.ManualToggleLabelFormat] = "説明書 [{0}]",
            [TextId.ManualPageFooterFormat] = "{0}　　{1} / {2}",
            [TextId.ManualNavHint] = "← → ページ送り　　Shift＋← → 章送り　　{0} または [menu] で閉じる",
            [TextId.ManualSectionBasics] = "基本ルール",
            [TextId.ManualSectionExploration] = "探索と戦闘",
            [TextId.ManualSectionMeeting] = "会議と投票",
            [TextId.ManualSectionGauge] = "貴重品減額ゲージ",
            [TextId.ManualSectionVillager] = "村人",
            [TextId.ManualSectionShaman] = "祈祷師",
            [TextId.ManualSectionWerewolf] = "人狼",
            [TextId.ManualSectionBlackCat] = "黒猫",
            [TextId.ManualSectionBomber] = "爆弾魔",
            [TextId.ManualSectionAfterDeath] = "死亡後",

            [TextId.ManualWelcomeTitle] = "REPO人狼へようこそ",
            [TextId.ManualWelcomeBody] =
                "REPO人狼では、プレイヤーは村人陣営と人狼陣営に分かれ、それぞれの勝利を目指して水面下で争います。\n" +
                "村人陣営の目標は、通常のREPOと同じです。マップから貴重品を集めて全ての抽出ポイントを完了させ、トラックを出発させることです。\n" +
                "人狼陣営の目標は、正体を隠したままそれを妨害することです。\n" +
                "誰が誰を疑い、誰が誰と手を組んでいるのか？\n" +
                "疑心暗鬼の中から真実を見つけ出せるでしょうか。それとも、偽りを真実だと信じ込ませられるでしょうか？\n" +
                "安易な答えに飛びついて同士討ちを始めたとき、プレイヤー自身が最凶のモンスターと化すでしょう。",

            [TextId.ManualGameFlowTitle] = "ゲームの流れ",
            [TextId.ManualGameFlowBody] =
                "REPO人狼を遊ぶには最低3人のプレイヤーが必要です。\n" +
                "ホストのルーム設定から、好きなステージやレベル、持ち込むアイテムやプレイヤーのアップグレード状況などを調整できます。\n" +
                "既存のセーブデータをロードして試合を開始すると、そのデータは試合用の状態に上書きされて消去されるので注意してください。\n" +
                "ショップや次のレベルという概念はなく、1つのレベルで1試合が完結します。\n" +
                "どちらかの陣営の勝利が確定すると試合結果が表示され、そのままロビーに戻ります。\n" +
                "また、試合中はCosmetic Boxが出現しません。その代わり、一定の条件を満たすと、試合終了後にトークンを獲得できます。",

            [TextId.ManualVillagerWinTitle] = "村人陣営の勝利条件",
            [TextId.ManualVillagerWinBody] =
                "村人陣営の勝利条件は次の2つです。どちらかを満たせば勝利します。\n" +
                "・全ての抽出ポイントを完了させ、村人陣営が1人以上生存した状態でトラックを出発させる\n" +
                "・人狼陣営が全滅する（黒猫が生き残っていても勝利できます）\n" +
                "トラックを発車させた人物の陣営は勝敗に影響しません。発車時の村人陣営の生存状況によって決まります。\n" +
                "つまり、通常のREPOと同じようにゲームクリアを目指せば勝利に近づきます。\n" +
                "ただし人狼陣営はあの手この手で妨害してくるため、普通のプレイとは違う戦略が必要になるでしょう。",

            [TextId.ManualWerewolfWinTitle] = "人狼陣営の勝利条件",
            [TextId.ManualWerewolfWinBody] =
                "人狼陣営の勝利条件は次の3つです。いずれかを満たせば勝利します。\n" +
                "・場に出ている全ての貴重品を集めてもノルマを達成できない状態（詰み）にする\n" +
                "・制限時間切れまでトラックの発車を阻止する\n" +
                "・村人陣営が全滅する\n" +
                "「詰み」が成立すると、その瞬間に人狼陣営の勝利が確定します。\n" +
                "最終抽出が完了していても、トラックの発車までに村人陣営を全滅させれば人狼陣営の勝利です。\n" +
                "人狼がトラックを発車させ、置き去りによって村人陣営が全滅した場合もこれに含まれます。",

            [TextId.ManualPeacefulVillageTitle] = "平和村",
            [TextId.ManualPeacefulVillageBody] =
                "ルーム設定によっては、人狼陣営が一人も配役されない「平和村」になることがあります。\n" +
                "平和村では倒すべき人狼陣営が初めから存在しないため、勝利条件は通常のREPOと同様に全ての抽出ポイントを完了させてトラックを出発させることだけです。\n" +
                "ただし、試合中に平和村であることを確かめる手段はありません。役職の公開、会議の議事録、黒猫の自覚通知、接近警告など、見える情報は通常村と同じです。\n" +
                "一方で、人狼陣営の活動によってビーコンが使われる、人狼でしかあり得ない行動をしている、爆弾魔による爆発が起きるなどした場合は、平和村ではないことが確定します。\n" +
                "疑いを捨てて互いに協力すべき平和村なのか、人狼が息を潜めて油断を誘っている通常村なのかはその時が来るまで分かりません。\n" +
                "平和村には、通常村における村人陣営の敗北条件に加えて特殊敗北条件があり、生存者が1人以下になるとその時点で平和村は崩壊して全員が敗北します。",

            [TextId.ManualValuablesMapTitle] = "貴重品とマップ",
            [TextId.ManualValuablesMapBody] =
                "誰かが貴重品を発見したり、敵がオーブを落としたりすると、マップに黄色いマーカーが登録されます。\n" +
                "通常のREPOと違い、標準設定ではこのマーカーはリアルタイムに更新されません。\n" +
                "更新されるのは、抽出ポイントを完了したときと会議を開いたときだけです。\n" +
                "マーカーの場所に現物が無い場合、誰かが運んだか、壊されたかのどちらかです。\n" +
                "マップからは区別できません。\n" +
                "注意深い村人は、会議のたびに「あの貴重品はどこへ行ったのか」を棚卸しして推理します。",

            [TextId.ManualValuableRecordTitle] = "貴重品の記録切り替え（人狼陣営）",
            [TextId.ManualValuableRecordBody] =
                "貴重品マーカーがマップに登録されるのは「誰かがその貴重品を視界に捉えた」瞬間です。\n" +
                "人狼陣営（人狼・爆弾魔・自覚した黒猫）は、記録をOFFにしている間、見つけた貴重品をマップへ登録しません。\n" +
                "そのため、まだ誰にも見つかっていない貴重品を人狼陣営が先回りして壊した場合、マップには最初から何も残りません。\n" +
                "逆に、村人が先に見つけて記録済みの貴重品を壊せば、マーカーだけが残り「あったはずの物が無い」と気づかれます。",
            [TextId.ManualValuableRecordToggle] =
                "記録するかどうかは、通報キーの長押しでいつでも切り替えられます（右下のアイコンが現在の状態です）。\n" +
                "ステルス破壊を狙うときだけ記録をOFFにし、探索へ戻る前に必要に応じてONへ戻しましょう。",

            [TextId.ManualCombatTitle] = "PvP",
            [TextId.ManualCombatBody] =
                "通常のREPO本編と違い、人狼モードでは近接武器でプレイヤーにダメージを与えられ、武器のエネルギーも消費されます（スマブラ風アリーナと同様）。\n" +
                "致命的なダメージを瀕死で耐える（いわゆる根性・気合・踏ん張り）救済措置もありません。\n" +
                "近接武器には武装解除の能力が付与されています。\n" +
                "近接武器で殴られると手で掴んでいるものを落とす上に、インベントリの所持品が一つ外に飛び出します。\n" +
                "逆に、他のプレイヤーが掴んでいるアイテムを掴んでインベントリに入れることはできません。奪うには殴って叩き落とす必要があります。\n" +
                "つまり、先に攻撃を当てた側が有利です。背中を預ける相手は慎重に選びましょう。",

            [TextId.ManualEndgamePrepTitle] = "最終局面への備え",
            [TextId.ManualEndgamePrepBody] =
                "試合終盤、会議による決着が難しくなると、人狼陣営と村人陣営が直接対決する最終局面に移ることがあります。\n" +
                "最終局面での戦いやすさは、それまでに各陣営が築いた状況によって変化します。\n" +
                "村人陣営は、危険な武器を確保・没収して人狼の手に渡るのを防ぐほか、信頼できる仲間を守り、回復手段を残しておくことで有利に戦えます。\n" +
                "人狼陣営は、貴重品を破壊して特典を解禁するほか、信頼を得ている村人を殺害し、村人同士の連携を崩すことで優位を築けます。\n" +
                "最後にぶつかるのは、両陣営が試合を通して築いてきた優位性です。\n" +
                "終盤の展開も見据え、序盤から準備を進めましょう。",

            [TextId.ManualCorpseTitle] = "死体について",
            [TextId.ManualCorpseBody] =
                "REPO人狼では、どんな方法でも死者は蘇生できません。\n" +
                "また、死体の位置はマップに表示されません。死体は自分の目で見つける必要があります。\n" +
                "まだ会議で周知されていない死体の近くで通報キー（画面右下に表示されます）を押すと、その場から会議を開催できます。",

            [TextId.ManualConveneTitle] = "会議の開き方",
            [TextId.ManualConveneBody] =
                "会議を開く方法は2つあります。\n" +
                "・トラック最奥にある赤いボタンを掴んで長押しする\n" +
                "・未周知の死体の近くで通報キーを押す\n" +
                "ボタンで会議を開ける回数は1人1回までです（ルーム設定で変更できます）。\n" +
                "ゲーム開始直後と会議終了直後のしばらくは、ボタンで会議を開けません。\n" +
                "また、未完了の抽出ポイントが最後の1箇所になると、死体通報による会議の開催はできなくなります（例: 抽出ポイントが4箇所なら、3箇所目の完了以降は通報不可）。\n" +
                "ボタンでの会議開催は最後まで可能です。",

            [TextId.ManualMeetingFlowTitle] = "会議の流れ",
            [TextId.ManualMeetingFlowBody] =
                "会議が始まると、全員がトラックへワープして身動きが取れなくなります。\n" +
                "会議中は敵が消え、新たに現れることもないので安全です。\n" +
                "まず前回の会議以降の死亡者が発表され、続いて貴重品減額ゲージの変化が開示されます。\n" +
                "その後、投票が始まります。会議中は全体マップを開いて、更新された貴重品の情報を確認できます。\n" +
                "試合の制限時間は会議中は停止するので、議論で持ち時間は減りません。\n" +
                "ただし、会議中にも敵のリスポーンタイマーは減っていくので、会議で時間を浪費しすぎると敵が復活してしまいます（ルーム設定で係数を設定可能）。\n" +
                "会議終了時点でリスポーンの準備が整っている敵は即座にスポーンします。",

            [TextId.ManualVotingTitle] = "投票と処刑",
            [TextId.ManualVotingBody] =
                "会議では生存者だけが1人1票を投じます。スキップ（誰にも投票しない）も選べます。\n" +
                "誰が投票を済ませたかは全員に見えますが、誰に投票したかは分かりません。\n" +
                "最も票を集めたプレイヤーが処刑されます。最多票が同数の場合は誰も処刑されません。\n" +
                "会議には制限時間があり、誰かが投票するたびに残り時間が少し減ります。",

            [TextId.ManualScatterTitle] = "会議後の散開",
            [TextId.ManualScatterBody] =
                "ルーム設定によっては、会議が終わった時に生存者が6人以上いると、ランダムに3人以上の組へ分けられ、組ごとに別々の場所へワープして再出発します。\n" +
                "行き先はトラックか、2箇所目以降の完了済みの抽出ポイントのいずれかです（最初の抽出ポイントは行き先になりません）。\n" +
                "組分けは全員に発表されますが、どの組がどこへ行くのかは分かりません。前回の組分けは、次の会議の冒頭に会議チャットログで再掲されます。\n" +
                "2箇所目の抽出が完了するまでの間、最終抽出を完了させた後、生存者が6人に満たないときは、散開せず全員がトラックから再出発します。\n" +
                "散開の直後、画面上部に「引き継ぎ期間中…」と表示されている間に誰かが死亡すると、「引き継ぎトラブル発生」として自動で緊急会議が開かれます。\n" +
                "この会議は予告なしで即座に始まり、誰の会議開催回数も消費しません。",

            [TextId.ManualGaugeBasicsTitle] = "貴重品減額ゲージの見方",
            [TextId.ManualGaugeIntro] =
                "貴重品減額ゲージは、村人陣営と人狼陣営の貴重品の奪い合いを視覚化したものです。試合開始時点でマップにある貴重品の総額が基準になります。",
            [TextId.ManualGaugeLoss] =
                "黄色のゲージ（左から伸びる）…傷つき、失われた金額",
            [TextId.ManualGaugeDelivery] =
                "水色のゲージ（右から伸びる）…抽出済みの金額",
            [TextId.ManualGaugeLines] =
                "・青い縦線…ノルマ達成に必要な金額。水色のゲージがここに届けば、トラックの発車が可能になります\n" +
                "・赤い縦線…場に出ている貴重品を全て集めてもノルマを達成できなくなる「詰み」のライン。黄色のゲージがここに達すると、その瞬間に人狼陣営が勝利します\n" +
                "敵がオーブを落としたり、新たな貴重品が場に増えたりすると、赤い縦線は右へ移動します（村人側の余裕が増えます）。人狼陣営の詰み勝利が確定した後に移動しても覆りません。",

            [TextId.ManualRoleVillagerTitle] = "役職: 村人",
            [TextId.ManualRoleVillagerBody] =
                "所属陣営：村人陣営\n" +
                "村人に特殊な能力はありません。通常のREPOプレイヤーとして貴重品を集め、抽出完了と帰還を目指します。\n" +
                "最大の武器は観察と議論です。貴重品マップの棚卸し・死体の発見・他プレイヤーの行動の記憶を持ち寄り、会議で人狼をあぶり出しましょう。",

            [TextId.ManualRoleWerewolfTitle] = "役職: 人狼",
            [TextId.ManualRoleWerewolfIntro] =
                "所属陣営：人狼陣営\n" +
                "人狼（役職）は人狼陣営の中心となる役職で、最低1人は必ず配役されます。\n" +
                "貴重品が傷つくほど強くなります。\n" +
                "誰が傷つけたかに関係なく、貴重品の減額が一定額に達するたびに特典が順番に解禁されます。",
            [TextId.ManualRoleWerewolfEnemyMap] =
                "人狼（役職）だけは、マップで敵の位置が分かります。",
            [TextId.ManualRoleWerewolfPerksTitle] = "役職: 人狼 — 特典（1/2）",
            [TextId.ManualRoleWerewolfPerkStamina] =
                "スタミナ無限…ダッシュや壁捕まりでスタミナが減らない",
            [TextId.ManualRoleWerewolfPerkJump] =
                "追加ジャンプ…滞空中に多段ジャンプが可能（回数はルーム設定次第）",
            [TextId.ManualRoleWerewolfPerksTitle2] = "役職: 人狼 — 特典（2/2）",
            [TextId.ManualRoleWerewolfPerkEnemyIgnore] =
                "敵無視…ほとんどの敵に狙われなくなる（ただし音には反応されます）",
            [TextId.ManualRoleWerewolfPerkHeal] =
                "自然治癒…狼化中、時間経過で体力が少しずつ回復する（回復エフェクトは出ませんが、背中の体力ゲージは他のプレイヤーからも見えます）",
            [TextId.ManualRoleWerewolfPerkToggle] =
                "特典は狼化キーでON/OFFできます。村人に特典の使用を見られると正体がバレるので注意。",

            [TextId.ManualRoleWerewolfBeaconTitle] = "役職: 人狼 — ビーコン",
            [TextId.ManualRoleWerewolfBeaconBody] =
                "ビーコンは人狼（役職）の特殊能力です。貴重品を一定額傷つけるたびに使用回数がチャージされます。\n" +
                "使用すると、プレイヤーには聞こえない音を発してマップ中の敵をその場所へ呼び寄せます。倒されて消えているモンスターも呼び戻します（呼び戻しには人狼陣営で共有のクールダウンがあります）。\n" +
                "はぐれたプレイヤーを\"事故死\"させたり、村人の集団に敵をけしかけてパニックにさせたりできます。\n" +
                "ただし、オーブの供給を増やしてノルマ達成に近づけてしまったり、自分自身が戦闘に巻き込まれるリスクもあります。\n" +
                "使用したこと自体はその場では通知されませんが、次の会議で「前回の会議以降にビーコンが何回使われたか」が全員に公開されます。",

            [TextId.ManualRoleBlackCatTitle] = "役職: 黒猫",
            [TextId.ManualRoleBlackCatIntro] =
                "所属陣営：人狼陣営（勝敗のみ共有）\n" +
                "元となる役職：村人\n" +
                "ルーム設定により、村人の中から割り当てられます。\n" +
                "試合開始時には本人にも「村人」と伝えられ、しばらくしてから正体を自覚します。\n" +
                "能力は村人と同等です。ルーム設定で道連れが有効な場合、会議で処刑されると、自分に投票したプレイヤーの中から1人を選んで道連れにします。選ばなかった場合はランダムに決まります。\n" +
                "人狼からは誰が黒猫か分からず、黒猫からも最初は誰が人狼か分かりません。",
            [TextId.ManualBlackCatInformantTitle] = "役職: 黒猫 — 内通",
            [TextId.ManualRoleBlackCatInformant] =
                "特典「内通」…貴重品が一定額傷つくと、黒猫にだけ人狼陣営のメンバーが表示されるようになります。",
            [TextId.ManualRoleBlackCatGaugeNote] =
                "黒猫にも貴重品減額ゲージが表示されますが、更新はリアルタイムではなく一定間隔です。",

            [TextId.ManualBlackCatCounterTitle] = "黒猫への対処法",
            [TextId.ManualBlackCatCounterBody] =
                "ルーム設定で有効な場合、黒猫の道連れは「投票による処刑」のときにだけ発動します。\n" +
                "つまり、黒猫か疑わしいときは安易に処刑してはいけません。処刑すると、黒猫に投票したプレイヤーの誰かが道連れになります。\n" +
                "どうしても処刑する場合は、道連れになる覚悟のある人だけが投票しましょう。\n" +
                "武器や落下など、処刑以外の方法で死亡した場合、道連れは発動しませんがPvPに発展するリスクもあります。\n" +
                "合意を経ない私刑を行えば、他の村人には正当な理由があったのか判断できません。\n" +
                "処刑するほどの確証がない相手は、武器を没収して様子を見るのも一つの手です。武器がなければ、たとえ人狼陣営でも簡単には人を殺せません。",

            [TextId.ManualRoleBomberTitle] = "役職: 爆弾魔",
            [TextId.ManualRoleBomberIntro] =
                "所属陣営：人狼陣営\n" +
                "元となる役職：人狼\n" +
                "ルーム設定により、人狼（役職）の中から割り当てられます。\n" +
                "近くで一緒に過ごしたプレイヤーを爆弾に変え、好きなタイミングで起爆できる役職です。",
            [TextId.ManualBomberPlantTitle] = "役職: 爆弾魔 — 爆弾にする",
            [TextId.ManualRoleBomberPlant] =
                "試合開始直後と会議のあとは「爆弾にする」にクールダウンがあり、明けてから他のプレイヤーの近くに居続けると、そのプレイヤーに黄色いメーターが溜まっていきます（壁越し無効、離れると減少）。\n" +
                "メーターが満タンになり緑色になったら、画面左下の「爆弾にする」キーでそのプレイヤーを爆弾に変えられます。\n" +
                "爆弾にされたプレイヤーは、爆弾魔にだけ見える爆弾アイコンが付きます。本人は爆弾にされたことに気づきません。\n" +
                "爆弾魔自身を爆弾にすることはできませんが、人狼や黒猫を爆弾にすることもできます。\n" +
                "爆弾にできるのは同時に1人まで。起爆前なら別のプレイヤーに付け替えられます。",

            [TextId.ManualRoleBomberDetonateTitle] = "役職: 爆弾魔 — 起爆",
            [TextId.ManualRoleBomberDetonateBody] =
                "「起爆」のクールダウンが明けてからキーを押すと、爆弾にしたプレイヤーを中心に爆発が起きます。\n" +
                "周囲のプレイヤー・貴重品・敵が巻き込まれます。爆弾にされた本人も少しダメージを受けますが、体力が1を下回ることはなく、吹き飛ばされません。\n" +
                "爆風に爆弾魔自身が巻き込まれると即死します。\n" +
                "対象がトラックの近くにいる間は起爆できません。\n" +
                "対象が死亡した状態で起爆すると不発になり、爆弾は失われます。\n" +
                "爆弾の残数は、貴重品が一定額傷つくたびに補充されます。",

            [TextId.ManualRoleShamanTitle] = "役職: 祈祷師",
            [TextId.ManualRoleShamanIntro] =
                "所属陣営：村人陣営\n" +
                "元となる役職：村人\n" +
                "ルーム設定により、村人の中から割り当てられます。\n" +
                "会議でまだ発表されていない「未周知の死体」の気配を感じ取れる役職です。\n" +
                "死体がある方向や、死亡した時期を推測できます。\n" +
                "ただし霊視には立ち止まる必要があるため隙が大きく、霊障が発生すると視界も悪くなります。\n" +
                "ミイラ取りがミイラにならないよう、注意してください。",
            [TextId.ManualShamanSenseTitle] = "役職: 祈祷師 — 霊視と霊障",
            [TextId.ManualShamanGhost] =
                "霊視は遠くの死体がある方向を察知できる能力です。\n" +
                "その場に立ち止まり、視線を一点に据えると視界が色褪せて、霊視が始まります。\n" +
                "霊視中に一定間隔で聞こえる水滴の音は「その方向に未周知の死体はない」の合図です。\n" +
                "未周知の死体がある方向を数秒間見つめ続けると、画面が一瞬強く乱れます。\n" +
                "壁や距離の制限はなく、一度画面が乱れるとしばらくの間は霊視が反応しなくなります。試合開始直後も、しばらくは霊視が反応しません。\n" +
                "未周知の死体が複数ある場合、自分から最も近い死体だけを対象とし、それより遠い死体には反応しません。\n" +
                "移動したり視線を大きく動かすと霊視は中断します。死体が近くて霊障が発生している間は霊視ができません。\n" +
                "視界が色褪せるのは、霊視が働いている間だけです。\n" +
                "会議で発表済みの死体には反応しません。",
            [TextId.ManualShamanStorm] =
                "霊障は近くの死体の距離を察知できる能力です。\n" +
                "未周知の死体の近くにいると霊障が発生し、その強さは距離に応じて弱・中・強の3段階に変化します。\n" +
                "未完了の抽出ポイントが最後の1箇所になると、霊障による視界の歪みは消え、音だけになります。\n" +
                "会議で発表済みの死体には反応しません。",

            [TextId.ManualAfterDeathTitle] = "死んでしまったら",
            [TextId.ManualAfterDeathBody] =
                "死亡すると観戦モードになり、そのまま試合の行方を見届けることになります。\n" +
                "死者同士では会話できますが、通常のREPOのようにDeathHeadなどを用いて生存者に情報を伝える手段は一切ありません。\n" +
                "ルーム設定次第では人狼陣営にエコーがかった声で死者の声が聞こえます。\n" +
                "勝敗は陣営で共有されるため、死亡していても所属陣営が勝てば勝利です。",

            [TextId.ScenarioSelectTitle] = "REPO人狼チュートリアル",
            [TextId.ScenarioNameMeeting] = "会議・投票",
            [TextId.ScenarioDescMeeting] = "会議を開き、証言を読み、投票で人狼を処刑する",
            [TextId.ScenarioNameWolf] = "人狼の能力",
            [TextId.ScenarioDescWolf] = "貴重品を壊して回り、能力を解禁して詰み勝利を狙う",

            [TextId.ScenarioNameShaman] = "霊視・死体通報",
            [TextId.ScenarioDescShaman] = "霊視で死体を探し、通報で会議を開き、終盤戦の通報不可を体験する",
            [TextId.ScenarioNameBomber] = "爆弾化・起爆",
            [TextId.ScenarioDescBomber] = "爆弾にして、トラック圏の起爆ロックと安全距離を体験する",
            [TextId.ScenarioLocked] = "準備中",
            [TextId.ScenarioCompleted] = "クリア済み",
            [TextId.ScenarioBack] = "戻る",
            [TextId.ScenarioReturnToSelect] = "チュートリアル選択へ戻る",
            [TextId.ScenarioReturnToSelectWithKeyFormat] = "チュートリアル選択へ戻る [{0}]",
            [TextId.ScenarioAbortConfirm] = "チュートリアルを中止してタイトルへ戻りたいか？",
            [TextId.ScenarioHudBadge] = "チュートリアル（時間制限なし）",
            [TextId.ScenarioAdvanceHint] = "[interact] で次へ",
            [TextId.ScenarioAbortHint] = "[menu] でタイトルへ",
            [TextId.ScenarioAbortCancelHint] = "[interact] でキャンセル",
            [TextId.ScenarioBrokenNotice] = "何か問題が起きたようだ。もう一度出直してくれ。",
            [TextId.ScenarioNeedOpenChatLogNag] = "会議チャットログを開け。議事録も発言もそこに記録される。",
            [TextId.ScenarioNeedCloseMapNag] = "投票をするには、先に全体マップを閉じろ。",
            [TextId.ScenarioNeedOpenMapNag] = "全体マップを開け。左下のボタンだ。",
            [TextId.ScenarioNeedCloseMapToolNag] = "全体マップを開くには、先に手持ちマップ[[map]]を閉じろ。",
            [TextId.ScenarioNeedOpenMapToolNag] = "手持ちのマップ[[map]]を開け。",
            [TextId.ScenarioNeedShamanTranceNag] = "立ち止まってじっとしてみろ。視線も動かすなよ。",
            [TextId.ScenarioNeedShamanSenseReadyNag] = "今は霊視のクールダウン中だ。少し待て。",
            [TextId.ScenarioNeedShamanStormNag] = "霊視を頼りに死体[[deathhead]]を探せ。",
            [TextId.ScenarioNeedCorpseNearNag] = "死体[[deathhead]]の近くに行け。",
            [TextId.ScenarioNeedBombTargetNearNag] = "離れすぎだ。No.{0}に近づけ。壁を挟まず近くに立てば、No.{0}の頭上のゲージがまた増え始める。",
            [TextId.ScenarioNeedBombOnTargetNag] = "爆弾が付いている相手が違う。No.{0}の近くで頭上のゲージを満タンにして [plantkey] を押し、No.{0}へ付け替えろ。",
            [TextId.ScenarioBlastDenyTooClose1] = "近すぎる。爆弾魔が自分の仕掛けた爆弾の爆風に巻き込まれると即死するぞ。",
            [TextId.ScenarioBlastDenyTooClose2] = "今の起爆は通していない。No.{0}の近接ゲージが溜まらないところまで離れてから、もう一度押せ。",
            [TextId.ScenarioBlastDenyTooFar1] = "遠すぎる。今回は自分から爆風に巻き込まれてもらう。",
            [TextId.ScenarioBlastDenyTooFar2] = "今の起爆は通していない。No.{0}に密着してから、もう一度押せ。",
            [TextId.ScenarioBlastDenyAwaitTruck1] = "まだ早い。No.{0}はトラックへ歩いている途中だ。",
            [TextId.ScenarioBlastDenyAwaitTruck2] = "今の起爆は通していない。No.{0}がトラックに着くのを見てから、もう一度押せ。",
            [TextId.ScenarioBlastDenyMoving1] = "待て。No.{0}はまだ歩いている途中だ。",
            [TextId.ScenarioBlastDenyMoving2] = "今の起爆は通していない。立ち止まってから間合いを測って、もう一度押せ。",
            [TextId.ScenarioBlastDenyGeneric1] = "今は起爆できない。",

            [TextId.ScenarioMeetingLeashNag] = "勝手に持ち場を離れるな[[:/]]トラックへ戻すぞ。",

            [TextId.ScenarioDecodeReceiving] = "Taxmanから通信を受信中...",
            [TextId.ScenarioDecodeInitializing] = "意味解釈モジュールを初期化中...",
            [TextId.ScenarioDecodeDone] = "完了",

            [TextId.ScenarioMeetingIntro1] = "ここでは趣向を変えて人狼ゲームを演じてもらう。",
            [TextId.ScenarioMeetingIntro2] = "何食わぬ顔で一緒に仕事をしているが、裏では任務失敗を企てている…",
            [TextId.ScenarioMeetingIntro3] = "お前の役職、「村人」はそんな人狼を処刑することも仕事だ。",
            [TextId.ScenarioMeetingIntro4] = "勿論、いつもの貴重品回収という仕事も忘れるなよ[[;)]]",
            [TextId.ScenarioMeetingIntro5] = "今回のチュートリアルでは会議と投票だけ教える。",
            [TextId.ScenarioMeetingIntro6] = "練習用に、ここにはモンスターを出していないし、時間制限も外してある。実戦ではどちらもある。",

            [TextId.ScenarioMeetingConvene1] = "トラック[[truck]]の後ろにある会議招集ボタンを長押ししろ。",
            [TextId.ScenarioMeetingConvene2] = "会議を開けば、全員がトラックへ集められる。",
            [TextId.ScenarioMeetingConveneNag1] = "おい、聞いているか？[[disappointed]]",
            [TextId.ScenarioMeetingConveneNag2Format] = "トラック[[truck]]の後ろについている赤色の会議招集ボタンを、{0} で長押しするんだ。",
            [TextId.ScenarioMeetingConveneNag3Grab] = "物を掴むときに使うキーだ。",
            [TextId.ScenarioMeetingConveneNag3Interact] = "物を操作するときに使うキーだ。",
            [TextId.ScenarioMeetingConveneEarly1] = "なんだ、もう知っていたのか[[:)]]",
            [TextId.ScenarioMeetingConveneEarly2] = "なら話は早い。ボタンの説明は省くぞ。",

            [TextId.ScenarioMeetingOpened1] = "会議が始まった。ここで確認できるのは3つ。",
            [TextId.ScenarioMeetingOpened2] = "死亡者[[deathhead]]、貴重品の損失と納品状況[[extraction]]、そして全体マップだ。",
            [TextId.ScenarioMeetingOpened3] = "会議チャットログにはテキストチャットが記録されるほか、会議毎の議事録もまとめられる。",
            [TextId.ScenarioMeetingOpened4] = "議事録の一番上の行は何回目の会議かを示す。",
            [TextId.ScenarioMeetingOpened5] = "その次に、死亡者[[deathhead]]は誰か。",
            [TextId.ScenarioMeetingOpened6] = "前回の会議から今回までに破壊された貴重品の額。",
            [TextId.ScenarioMeetingOpened7] = "納品に成功した金額[[$$$]]と、トラックを発車[[truck]][[arrowright]][[road]]させるのに必要なノルマ総額[[extraction]]",
            [TextId.ScenarioMeetingOpened8] = "前回の会議から今回までに使用されたビーコンの回数。",
            [TextId.ScenarioMeetingOpened9] = "今回は出ていないが、直前の会議で組分けがあれば、その組み合わせ[[semibots]]も別の行として続く。",

            [TextId.ScenarioMeetingClock1] = "実戦では、会議そのものにも制限時間がある。ただし会議の間、試合の残り時間[[clock]]は止まる。",
            [TextId.ScenarioMeetingClock2] = "モンスター[[headman]][[clown]]も姿を消し、会議中に新しく現れることはない。",
            [TextId.ScenarioMeetingClock3] = "だが、モンスター[[headman]][[clown]]の復活待ちと、時間経過による復活間隔の短縮は完全には止まらない。ルーム設定で決めた割合だけ進む。",
            [TextId.ScenarioMeetingClock4] = "会議が終わった時点で復活の準備ができているモンスター[[headman]][[clown]]は、すぐ現れる。",

            [TextId.ScenarioMeetingGauge1] = "画面の一番上の帯は、貴重品の状況だ。",
            [TextId.ScenarioMeetingGauge2] = "上の黄色いバーが、これまでに壊された貴重品の総額。会議のたびに、増えた分を見せている。",
            [TextId.ScenarioMeetingGauge3] = "下の段は納品した金額[[extraction]]で、右から水色で埋まっていく。今回はまだ何も納品していないから空だ。",
            [TextId.ScenarioMeetingGauge4] = "青い縦線がノルマだ。水色がここまで届けば、トラックを発車させられる。",
            [TextId.ScenarioMeetingGauge5] = "黄色が赤い縦線まで伸びたら、残り全部を納品してもノルマに届かない。その時点で人狼の勝ちだ。",

            [TextId.ScenarioMeetingMap1] = "これが全体マップだ。会議中だけ開ける。",
            [TextId.ScenarioMeetingMap2] = "誰かが一度見つけた貴重品が記録される。貴重品の現在地が更新されるのは会議の開始時と抽出完了時[[extraction]]だけだ。",
            [TextId.ScenarioMeetingMap3] = "マップにあるはずの貴重品がそこに無いなら、誰かが運び去ったか、壊したということだ。",
            [TextId.ScenarioMeetingMap4] = "格子にはA1のような座標が振ってある。「C5の部屋で死体を見た」と言えば、全員に同じ場所が伝わる。",
            [TextId.ScenarioMeetingMap5] = "見終わったら、同じボタンで閉じろ。",

            [TextId.ScenarioMeetingChatUnavailable1] = "今はチュートリアルだからテキストチャット[[message]]を使えない[[pleading]]",
            [TextId.ScenarioMeetingChatUnavailable2] = "実戦のマルチプレイなら通常のREPOと同じチャットキー（[chat]）で発言でき、会議中の発言はこの会議記録に残る。",

            [TextId.ScenarioMeetingVote1] = "議論が進み、No.3が疑われているようだ。",
            [TextId.ScenarioMeetingVote2] = "自由に投票してみろ。",
            [TextId.ScenarioMeetingVote3] = "同じ相手をもう一度押せば確定だ。確定前なら選び直せる。",

            [TextId.ScenarioMeetingTally] = "というわけで、単独最多得票数のNo.3の処刑が決定した[[scared]]",
            [TextId.ScenarioMeetingResult1] = "処刑されたNo.3は人狼だった。Good Job![[creepycrying]]",
            [TextId.ScenarioMeetingResult2] = "人狼が全滅したから、村人陣営の勝ちだ。",

            [TextId.ScenarioMeetingPostgame1] = "結果画面だ。",
            [TextId.ScenarioMeetingPostgame2] = "マルチプレイなら、ここで一緒に感想戦ができる。",
            [TextId.ScenarioMeetingReplay1] = "今の試合は丸ごと録画されている。",
            [TextId.ScenarioMeetingReplay2] = "これを開けば、さっきの議論がそのまま流れる。",
            [TextId.ScenarioMeetingPostgame3] = "会議のチュートリアルはここまでだ[[wave]]",

            [TextId.ScenarioMeetingLine1] = "No.3が何かの貴重品を壊していました。",
            [TextId.ScenarioMeetingLine2] = "誤解です。わざとじゃなくて本当に落としたんです。",
            [TextId.ScenarioMeetingLine3] = "ちなみに何を運んでたんですか？",
            [TextId.ScenarioMeetingLine4] = "チェンソーです。",
            [TextId.ScenarioMeetingLine5] = "自分に刃が当たりそうで怖くて、一旦持ち直そうと離したら壊れてしまいました。",
            [TextId.ScenarioMeetingLine6] = "本当にミスっただけです。",
            [TextId.ScenarioMeetingLine7] = "ちなみに誰が怪しいと思いますか？",
            [TextId.ScenarioMeetingLine8] = "分かりませんが、私を疑ってきたNo.2が怪しいと思っています。",
            [TextId.ScenarioMeetingLine9] = "ちょっと待ってください。No.3が自分から壊したと言ったならまだしも…",
            [TextId.ScenarioMeetingLine10] = "黙っていたから私が代わりに説明したんですよ？",
            [TextId.ScenarioMeetingLine11] = "見られているとは思いませんでした。説明して疑われるとややこしくなるのが嫌だったんです。",
            [TextId.ScenarioMeetingLine12] = "変に怪しまれるくらいなら、黙ってスムーズにクリアした方が良いじゃないですか。",
            [TextId.ScenarioMeetingLine13] = "No.3が怪しいように思います。",
            [TextId.ScenarioMeetingLine14] = "No.3に投票しましょう。",

            [TextId.ScenarioWolfIntro1] = "今回はお前に人狼をやってもらう[[;)]]",
            [TextId.ScenarioWolfIntro2] = "人狼の目標は、村人にバレないように[[hush]]貴重品を壊して任務を失敗させることだ。",
            [TextId.ScenarioWolfIntro3] = "ここにはモンスター[[headman]][[clown]]がいる。襲われれば痛いだろうが、練習だからお前は死なないし、時間制限も外してある。実戦では人狼も死ぬし、時間も切れる。",

            [TextId.ScenarioWolfGauge1] = "右下のゲージが、試合開始時にこのマップにあった貴重品の総額と、これまでに壊された貴重品の総額だ。人狼にしか見えない。",
            [TextId.ScenarioWolfGauge2] = "赤い縦線まで伸ばせば、残り全部を回収してもノルマに届かなくなる。その瞬間、人狼の勝ちだ。",
            [TextId.ScenarioWolfGauge3] = "このレベル帯なら本番の赤線はもっと遠い。ここはノルマを高くしてあるから、半分ほど壊せば届く。",

            [TextId.ScenarioWolfDestroy1] = "まずは貴重品を1つ壊してこい。",
            [TextId.ScenarioWolfDestroy2] = "掴んで壁に叩きつけるか、高い所から落とすか、踏みつければ壊れる。",
            [TextId.ScenarioWolfDestroyNag1] = "貴重品は見つけたか？[[eyesleft]]",
            [TextId.ScenarioWolfDestroyNag2] = "貴重品を掴んで、壁か床へ思い切り叩きつけるか、踏みつけるんだ。",
            [TextId.ScenarioWolfEarly1] = "気が早いな。壊し方は知っているようだ[[shades]]",
            [TextId.ScenarioWolfEarly2] = "なら説明は省くぞ。",

            [TextId.ScenarioWolfMarker1] = "今壊した貴重品のマーカーが、まだ残っているだろう。",
            [TextId.ScenarioWolfMarker2] = "お前がその貴重品を目にした時点で、全員のマップに貴重品の位置が記録されている。",
            [TextId.ScenarioWolfMarker3] = "マーカーは壊しても消えない。運び去られたのか壊されたのかは、マップからは分からない。",
            [TextId.ScenarioWolfMarker4] = "だがマーカーの場所へ行けば、現物が無いことは分かる。お前が近くにいた事が分かれば、疑われる。",
            [TextId.ScenarioWolfMarker5] = "そもそもマーカーを付けずに済む方法もある。",

            [TextId.ScenarioWolfRecord1] = "右下の記録アイコンを見ろ。",
            [TextId.ScenarioWolfRecord2] = "通報キー [reportkey] を長押しすれば、貴重品の記録を切れる。",
            [TextId.ScenarioWolfRecord3] = "切っている間は、見つけた貴重品が誰のマップにも載らない。",
            [TextId.ScenarioWolfRecordNag1] = "まだ記録が切れていないぞ。通報キー [reportkey] を押しっぱなしにするんだ。",

            [TextId.ScenarioWolfStealth1] = "その状態で、まだ誰も見つけていない貴重品を壊してこい。",
            [TextId.ScenarioWolfStealth2] = "村人が先に見つけた物を壊してもマーカーは残る。狙うのは手付かずの物だ。",
            [TextId.ScenarioWolfStealthNag1] = "記録は切ったな。あとはまだ記録されていない貴重品を壊すだけだ。",
            [TextId.ScenarioWolfStealth3] = "今度はマーカーが残っていない。",
            [TextId.ScenarioWolfStealth4] = "壊れた総額そのものは次の会議で開示される。隠せるのは場所だけだ。",

            [TextId.ScenarioWolfKeep1] = "その調子で壊し続けろ。",
            [TextId.ScenarioWolfKeep2] = "一定割合の貴重品が壊されるたびに、人狼は強くなっていく。",
            [TextId.ScenarioWolfPush1] = "引き続き貴重品を壊せ。ゲージが伸びれば次が解禁される。",
            [TextId.ScenarioWolfPush2] = "特典はまだ残っている。貴重品を探して壊し続けろ。",

            [TextId.ScenarioWolfForm1] = "左下を見ろ。特典が1つ解禁された。",
            [TextId.ScenarioWolfForm2] = "アイコンは4つ。無限スタミナ、追加ジャンプ、モンスターからの無視、自然治癒だ。",
            [TextId.ScenarioWolfForm3] = "解禁されただけでは効いていない。狼化キーでONにして初めて効く。",
            [TextId.ScenarioWolfForm4] = "解禁の閾値はルーム設定で変わる。ここでは早めに解禁されるようにしてある。",
            [TextId.ScenarioWolfFormOn1] = "狼化キー [wolfkey] を押してONにしてみろ。",
            [TextId.ScenarioWolfFormOn2] = "村人の前でONにしたままだと、動きの違いで正体がバレるぞ[[hush]]",
            [TextId.ScenarioWolfFormOnNag1] = "狼化キーは [wolfkey] だ。",
            [TextId.ScenarioWolfFormDone1] = "それでいい。今は走ってもスタミナが減らないだろう。解禁済みの特典が効いている状態だ。",
            [TextId.ScenarioWolfFormDone2] = "もう一度押せば戻る。村人に混ざって行動するときはOFFにしておけ[[hush]]",

            [TextId.ScenarioWolfEnemyMap1] = "マップには近くにいるモンスター[[headman]][[clown]]の位置が表示される。",
            [TextId.ScenarioWolfEnemyMap2] = "これは人狼にしか見えない。未探索の部屋にいるモンスター[[headman]][[clown]]も分かる。",
            [TextId.ScenarioWolfEnemyMap3] = "モンスター[[headman]][[clown]]の場所が分かっているかのような動きをすれば疑われる。自然にやれ[[hush]]",

            [TextId.ScenarioWolfBeacon1] = "左下のビーコンスロットを見ろ。貴重品を一定割合壊すたびに残数が増える。",
            [TextId.ScenarioWolfBeacon2] = "ビーコンキー [beaconkey] を押せば、その場にモンスター[[headman]][[clown]]を呼び寄せる。倒されたモンスターも呼び戻す。",
            [TextId.ScenarioWolfBeacon3] = "1発ぶん都合しておいた。試しに撃ってみろ。",
            [TextId.ScenarioWolfBeaconNag1] = "残数はあるはずだ。ビーコンキー [beaconkey] を押すだけだぞ。",
            [TextId.ScenarioWolfBeacon4] = "マップを見ていろ。徘徊するモンスター[[headman]][[clown]]ならこっちへ寄ってくるはずだ。",
            [TextId.ScenarioWolfBeacon5] = "ビーコンを使った瞬間は村人からも分からない。会議で回数だけが開示される。",
            [TextId.ScenarioWolfBeacon6] = "実戦では村人がいる場所へ撃つと良いだろう。",

            [TextId.ScenarioWolfIgnore1] = "モンスター[[headman]][[clown]]と仲間になった。狼化がONの間、大抵のモンスターはお前を狙わなくなる。反撃状態になったモンスターには狙われるかもしれないがな。",
            [TextId.ScenarioWolfIgnore2] = "モンスターと仲良く歩いているところを村人に見られないように気をつけろ[[hush]]",

            [TextId.ScenarioWolfHeal1] = "自然治癒が解禁された。狼化がONの間、体力が少しずつ回復する。",
            [TextId.ScenarioWolfHeal2] = "体力バーは他人からも見える。減った体力が勝手に戻っていれば怪しまれる。",

            [TextId.ScenarioWolfOrb1] = "モンスターを倒すと落ちるオーブも貴重品だ。落ちた分だけ村人が回収できる総額が増え、赤線が遠のく。",
            [TextId.ScenarioWolfOrb2] = "だがそのオーブを壊せば実損に乗り、特典の解禁は進む。倒したなら壊しておけ。",

            [TextId.ScenarioWolfFinish1] = "あとは赤線まで壊し切れ。",
            [TextId.ScenarioWolfFinish2] = "狼化は有効にしたか？探索が楽になるぞ。",
            [TextId.ScenarioWolfFinish3] = "マップ中の貴重品を探して壊して回るんだ。",

            [TextId.ScenarioWolfResult1] = "回収可能な総額がノルマを下回った。もう村人は任務を達成できない[[dry]]",
            [TextId.ScenarioWolfResult2] = "貴重品の詰みによる、人狼陣営の勝ちだ。",
            [TextId.ScenarioWolfPostgame1] = "結果画面だ。誰がどの役職だったかが開示される。",
            [TextId.ScenarioWolfPostgame2] = "マルチプレイなら、ここで一緒に感想戦ができる。",
            [TextId.ScenarioWolfReplay1] = "今のチュートリアルも丸ごと録画されている。",
            [TextId.ScenarioWolfReplay2] = "どこで何を壊したかも残っているぞ。",
            [TextId.ScenarioWolfWrap1] = "言っておくが、実戦でここまで壊し切るのは難しい。村人の目があるからな[[eyesleft]]",
            [TextId.ScenarioWolfWrap2] = "それでも壊す価値はある。特典が解禁され、村人のノルマは遠のく。全滅を狙うにせよ、時間切れを狙うにせよ、壊した分だけ勝ちに近づく。",
            [TextId.ScenarioWolfPostgame3] = "人狼のチュートリアルはここまでだ[[wave]]",

            [TextId.ScenarioWolfExtractionNag1] = "おい、お前は人狼だぞ。",
            [TextId.ScenarioWolfExtractionNag2] = "抽出を進めてどうする[[disappointed]]貴重品は壊すんだ。",
            [TextId.ScenarioWolfExtractionBreak1] = "捻くれたやつだな[[:/]]",
            [TextId.ScenarioWolfExtractionBreak2] = "ノルマが達成されたから、もう詰み勝利は不可能だ[[disappointed2]]",
            [TextId.ScenarioWolfExtractionBreak3] = "始めから出直してこい。",

            [TextId.ScenarioShamanIntro1] = "今回はお前に祈祷師をやってもらう。",
            [TextId.ScenarioShamanIntro2] = "祈祷師は、まだ会議で周知されていない死体[[deathhead]]の気配を感じ取れる[[ghost]]",
            [TextId.ScenarioShamanIntro3] = "会議で死亡者として周知された死体[[deathhead]]は対象から外れる。",
            [TextId.ScenarioShamanIntro4] = "練習用に、ここにはモンスター[[headman]][[clown]]を出していないし、お前も死なない。時間制限[[clock]]も外してある。",
            [TextId.ScenarioShamanIntro5] = "穴に落ちてもジャンプで上がれるように、跳べる回数も多めにしてある。実戦ではどれも違う。",

            [TextId.ScenarioShamanTrance1] = "まずは霊視の入り方だ。その場に立ち止まり、一点を見つめ続けろ[[realistic_eye]]",
            [TextId.ScenarioShamanTrance2] = "しばらくすると画面の色が抜ける。色が抜けている間だけ、探知が働いている。",
            [TextId.ScenarioShamanTrance3] = "首を振るな。向きを変えたら、そこで一度止まれ。視線を動かした瞬間に数え直しになる。",

            [TextId.ScenarioShamanTranceOn1] = "今、視界の色が抜けているだろう。",
            [TextId.ScenarioShamanTranceOn2] = "この状態が霊視だ。色が戻ったら霊視も止まっている。",

            [TextId.ScenarioShamanDrip1] = "見つめ続けて水滴の音が鳴ったら、その方向を見つめ切ったが、死体[[deathhead]]は無かったということだ。",
            [TextId.ScenarioShamanDrip2] = "死体[[deathhead]]が今はまだ無いか、向いている方向には無いということが分かる。",

            [TextId.ScenarioShamanDowse1] = "では探すぞ。向きを変えて、止まって、見つめる。これを繰り返せ。",
            [TextId.ScenarioShamanDowse2] = "死体[[deathhead]]がある方角を霊視すると、視界が乱れる。壁の向こうでも、どれだけ遠くても霊視できる。",

            [TextId.ScenarioShamanDowseNag1] = "向きを変えたら必ず止まる。視界を動かしている間は何も起きない。",
            [TextId.ScenarioShamanDowseNag2] = "視界が乱れっぱなしなら、死体[[deathhead]]はもう近い。歩いて探せ。",

            [TextId.ScenarioShamanBurst1] = "霊視が成功した。その方向に死体[[deathhead]]があるということだ。死体[[deathhead]]を探しに行け。",
            [TextId.ScenarioShamanBurst2] = "途中で迷ったら、また霊視で方向を絞り込め。",
            [TextId.ScenarioShamanBurst3] = "ただし一度成功すると、しばらくは霊視が反応しない。色が抜けなければ待ち時間だ。",

            [TextId.ScenarioShamanStorm1] = "霊障が始まった。近くに死体[[deathhead]]があるということだ。近づくほど三段階で霊障が強くなる。",
            [TextId.ScenarioShamanStorm2] = "霊障が起きている間は霊視ができない。霊視では方角が、霊障では距離が分かると言える。",
            [TextId.ScenarioShamanStorm3] = "あとは霊障が一番強くなる場所を探せ。死体[[deathhead]]はすぐそこだ。",

            [TextId.ScenarioShamanReport1] = "死体[[deathhead]]を見つけたら、その近くで [reportkey] を押せ。右下の通報アイコンが光っていれば押せる。",
            [TextId.ScenarioShamanReport2] = "死体通報は会議を開く。開催権は要らない。",

            [TextId.ScenarioShamanReportNag1] = "死体[[deathhead]]の近くで [reportkey] だ。右下のアイコンが光る距離まで近づけ。死体[[deathhead]]は高いところにある可能性もある。",

            [TextId.ScenarioShamanCorpseHint1] = "死体[[deathhead]]を見つけるのに手間取っているな[[:)]]",
            [TextId.ScenarioShamanCorpseHint2] = "チュートリアルだから、今回は特別に死体[[deathhead]]の位置を示してやろう[[;)]][[thumbsup]]",

            [TextId.ScenarioShamanEarly1] = "通報のやり方は知っているようだな[[kiss]]",
            [TextId.ScenarioShamanEarly2] = "話が早い。通報の説明は省くぞ。",

            [TextId.ScenarioShamanOpened1] = "通報で会議が開き、No.7が死亡者として発表された。",
            [TextId.ScenarioShamanOpened2] = "周知された死体[[deathhead]]は検知されなくなる。No.7の霊障[[ghost]]はもう出ない。",
            [TextId.ScenarioShamanOpened3] = "実戦ならここで死体[[deathhead]]の状況を説明することになるが、今はチュートリアルだから議論を聞くだけで良い。",

            [TextId.ScenarioShamanVote1] = "決め手がないまま時間切れだ。好きに投票しろ。",
            [TextId.ScenarioShamanVote2] = "今回は事故か殺人か断定できない。スキップが多数になるだろう。",

            [TextId.ScenarioShamanTally1] = "スキップ票が最多で処刑なしだ。No.3は監視つきの約束で生き残った。",
            [TextId.ScenarioShamanTally2] = "会議は終わる。ここからは終盤戦を体験してもらう。",

            [TextId.ScenarioShamanDeliver1] = "目の前にお金袋[[$$$]]を置いた。トラック付近の抽出ポイント[[extraction]]へ運んで、抽出を完了させろ。",

            [TextId.ScenarioShamanDeliverNag1] = "袋[[$$$]]はどうした？ トラック付近の抽出ポイント[[extraction]]へ運べ。見失ったなら、目の前にもう1つ出しておく。",

            [TextId.ScenarioShamanLastRun1] = "抽出が完了した。残る抽出ポイント[[extraction]]はあと1箇所。ここからは終盤戦だ。",
            [TextId.ScenarioShamanLastRun2] = "終盤戦では死体通報ができない。霊障で視界も乱れなくなる。音は残る。",
            [TextId.ScenarioShamanLastRun3] = "トラックの中に死体[[deathhead]]がある。音はするのに視界は乱れない、これが終盤戦の霊障だ。",

            [TextId.ScenarioShamanBlocked1] = "死体[[deathhead]]の近くで [reportkey] を押してみろ。",

            [TextId.ScenarioShamanBlockedNag1] = "トラック[[truck]]の中の死体[[deathhead]]だ。近くで [reportkey] を押せ。",

            [TextId.ScenarioShamanDenied1] = "『会議を開催できません』という通知が出たな。規則により通報は無効化された。",
            [TextId.ScenarioShamanDenied2] = "だが会議そのものは開ける。通報できなくても、会議ボタンは生きている。",

            [TextId.ScenarioShamanButton1] = "練習用に開催権を渡しておく。実戦では開始時から各自が持っている。トラック[[truck]]の後ろにある会議招集ボタンを長押しして会議を開け。",

            [TextId.ScenarioShamanButtonNag1] = "おい、聞いているか？[[disappointed]][[wave]]",

            [TextId.ScenarioShamanButtonNag2Format] = "トラック[[truck]]の後ろについている赤色の会議招集ボタンを、{0} で長押しするんだ。",

            [TextId.ScenarioShamanButtonNag3Grab] = "物を掴むときに使うキーだ。",

            [TextId.ScenarioShamanButtonNag3Interact] = "物を操作するときに使うキーだ。",

            [TextId.ScenarioShamanOpened4] = "会議が開いた。No.2の死亡が発表された。",
            [TextId.ScenarioShamanOpened5] = "議論を見守ってみろ。",

            [TextId.ScenarioShamanVote3] = "No.3は自分が祈祷師だと宣言した。だが本物の祈祷師は一人だけだ。",
            [TextId.ScenarioShamanVote4] = "好きに投票しろ。",

            [TextId.ScenarioShamanTally3] = "というわけで、単独最多得票数のNo.3の処刑が決定した[[scared]]",

            [TextId.ScenarioShamanResult1] = "人狼が全滅したから、村人陣営の勝ちだ。",
            [TextId.ScenarioShamanResult2] = "祈祷師も村人陣営だから、お前の勝ちということだ。",

            [TextId.ScenarioShamanPostgame1] = "結果画面だ。誰がどの役職だったかが開示される。",
            [TextId.ScenarioShamanPostgame2] = "No.3が人狼だったことも、ここで全員に分かる。",

            [TextId.ScenarioShamanReplay1] = "今のチュートリアルも丸ごと録画されている。",
            [TextId.ScenarioShamanReplay2] = "会議の発言も、お前の移動経路も残っている。",

            [TextId.ScenarioShamanWrap1] = "祈祷師はいつ・どこで死体が生まれたかが分かるから、殺人が起きたときには強力な情報を得られる。",
            [TextId.ScenarioShamanWrap2] = "ただし、祈祷師が死体を探している間は隙だらけだ。自分が祈祷師だとバレれば、人狼陣営の優先的な暗殺対象になるだろう。",
            [TextId.ScenarioShamanWrap3] = "祈祷師が居なくなったと分かれば人狼陣営は大胆な連続殺人を成功させやすくなり、村人陣営は窮地に陥る。",

            [TextId.ScenarioShamanPostgame3] = "祈祷師のチュートリアルはここまでだ[[wave]]",

            [TextId.ScenarioShamanLeashNag1] = "おい、どこへ行く[[:O]]",
            [TextId.ScenarioShamanLeashNag2] = "今はトラック[[truck]]付近が舞台だ。戻すぞ。",

            [TextId.ScenarioShamanExtractionBreak1] = "全部納品してどうする[[angry]]",
            [TextId.ScenarioShamanExtractionBreak2] = "確かにこの状態でトラック[[truck]]を出発させれば村人の勝ちだが、台本と違う。",
            [TextId.ScenarioShamanExtractionBreak3] = "始めから出直してこい。",

            [TextId.ScenarioShamanEarlyExtractionBreak1] = "順番が違う。会議を開く前に納品したな[[angry]]",
            [TextId.ScenarioShamanEarlyExtractionBreak2] = "残る抽出ポイントが最後の1箇所になった。ここからは死体通報ができない。あの死体[[deathhead]]の会議はもう開けない。",
            [TextId.ScenarioShamanEarlyExtractionBreak3] = "始めから出直してこい。",

            [TextId.ScenarioShamanQuotaBagBreak1] = "わざとやっているのか？[[angry]]",
            [TextId.ScenarioShamanQuotaBagBreak2] = "その袋[[$$$]]は納品用だ。壊すな、放り投げるな。これでは練習にならん[[disappointed]]",
            [TextId.ScenarioShamanQuotaBagBreak3] = "始めから出直してこい。",

            [TextId.ScenarioShamanMapAside1] = "あぁ、そいつら[[semibots]]が話している座標は気にしなくていい。",
            [TextId.ScenarioShamanMapAside2] = "そいつらはお前[[point]]と違って魂なき機械人形だ。決まったことしか話せない[[yawn]]",
            [TextId.ScenarioShamanMapAside3] = "実戦では、正確な座標を覚えておくことが重要だ。",

            [TextId.ScenarioBomberIntro1] = "今回は人狼陣営の爆弾魔をやってもらう。",
            [TextId.ScenarioBomberIntro2] = "近くの部屋にいるNo.3が仲間の人狼だ。近づけば頭上の識別番号が赤いのが見える。",
            [TextId.ScenarioBomberIntro3] = "爆弾魔は人狼の特典を使えない。代わりに、他人を爆弾に変えて好きな時に起爆できる。",
            [TextId.ScenarioBomberIntro4] = "左下を見ろ。左が爆弾化、右が起爆。緑の数字は残弾で、今は1発ある。",
            [TextId.ScenarioBomberIntro5] =
                "本番では爆弾化にも起爆にも長いクールダウンがあるが、チュートリアルだから5秒に縮めてある。",
            [TextId.ScenarioBomberAim1] = "1発目は、その仲間のNo.3を爆弾に変えろ。",
            [TextId.ScenarioBomberAim2] = "爆心にされた本人は生き残り、会議で「誰が近くにいたか」を証言できる。唯一の目撃者を味方にしておくということだ。",
            [TextId.ScenarioBomberPlant1] = "まず近くの部屋のNo.3に近づけ。十分に近ければ相手の頭上のゲージがたまる。",
            [TextId.ScenarioBomberPlant2] = "離れたり壁越しだとゲージは減る。満タンになるまでNo.3の近くにいろ。",
            [TextId.ScenarioBomberPlant3] = "ガイドを頼りに近くの部屋にいるNo.3に近づき、No.3の頭上のゲージが満タンになるまでそこで待て。満タンになったら [plantkey] を押して爆弾に変えろ。",
            [TextId.ScenarioBomberPlantNag1] = "まだNo.3を爆弾に変えていないな。壁を挟まず近くに立ち続ければ、No.3の頭上のゲージが増えていく。20秒ほどで満タンだ。離れると減るから、そこを動くな。満タンになったら [plantkey] を押せ。",
            [TextId.ScenarioBomberPlanted1] = "爆弾のアイコンが相手に付いたら爆弾になったということだ。",
            [TextId.ScenarioBomberNotice1] = "一方で、No.3にも「爆弾にされるのに十分な時間、誰かに近づかれていた」という接近警告が出ていた。",
            [TextId.ScenarioBomberNotice2] = "接近警告が出た時に近くにいたプレイヤーは記憶され、後で会議の材料になる。だが今回それを覚えているのは仲間だ。",
            [TextId.ScenarioBomberTry1] = "No.3が自分でトラック[[truck]]へ歩いていく。No.3がトラック[[truck]]に入るのを見届けたら、起爆のキー[bombkey] を押してみろ。",
            [TextId.ScenarioBomberTryNag1] = "まだ押していないな。No.3はもうトラック[[truck]]の中だ。左下のアイコンの右側が起爆で、割り当ては [bombkey] だ。",
            [TextId.ScenarioBomberLock1] = "対象がトラック[[truck]]の近くにいる間は起爆させない規則だ。",
            [TextId.ScenarioBomberLock2] = "No.3が外へ出るのを待て。広場にいるNo.4とNo.5のところへ向かう。",
            [TextId.ScenarioBomberSafe1] = "No.3がトラック[[truck]]付近から離れれば起爆できる。ただし、自分が爆風に巻き込まれると即死だ。",
            [TextId.ScenarioBomberSafe2] = "広場のNo.3から離れろ。No.3の頭上のゲージが増えなくなるところまで下がってから、[bombkey] を押して起爆しろ。今回はチュートリアルだから近すぎるうちは受け付けない。",
            [TextId.ScenarioBomberSafeNag1] = "まだ起爆していないな。No.3は広場でNo.4・No.5と固まっている。No.3の頭上のゲージが増えなくなる距離まで下がれ。下がりきったら [bombkey] だ。今回はチュートリアルだから爆風の範囲内では起爆させない。",
            [TextId.ScenarioBomberBoom1] = "爆風の範囲内にいると心臓の鼓動音が聞こえるが、爆風の範囲外にいると聞こえない。",
            [TextId.ScenarioBomberBoom2] = "爆風に巻き込まれると爆弾魔は即死するから、間合いを取るのが重要だ。",
            [TextId.ScenarioBomberMeet1] = "爆弾にされた本人は爆心でも生き残る。生き延びたNo.3は、誰と長い間行動したか証言できる。",
            [TextId.ScenarioBomberMeet2] = "仲間だからそこは隠してくれる。No.3が被害者として会議を開く。予告のあと全員がトラック[[truck]]へ集められるぞ。",
            [TextId.ScenarioBomberOpened1] = "死亡者はいない。爆心のNo.3も、巻き込まれたNo.4とNo.5も生きている。",
            [TextId.ScenarioBomberOpened2] = "会議チャットログを開け。村人がどう爆弾魔を絞るか、議論を聞いていろ。",
            [TextId.ScenarioBomberVote1] = "容疑者だが、決め手はないと見られている。",
            [TextId.ScenarioBomberVote2] = "好きに投票しろ。誰かを選ぶかスキップを押し、もう一度押して確定だ。",
            [TextId.ScenarioBomberTally1] = "スキップが最多票となり、誰も処刑されなかった。",
            [TextId.ScenarioBomberTally2] = "決め手を潰したのは仲間の証言だ。ただしNo.3も爆心のダメージを負っている。次の爆発に巻き込まれれば死ぬ。",
            [TextId.ScenarioBomberReplant1] = "次は危険範囲を覚えるため、わざと自爆してチュートリアルは終わりにする[[;)]]",
            [TextId.ScenarioBomberReplant2] = "弾を1発足しておいた。No.3・No.4・No.5は広場に固まったままだ。",
            [TextId.ScenarioBomberReplant3] = "今度は広場のNo.4を爆弾に変えろ。",
            [TextId.ScenarioBomberReplant4] = "広場のNo.4に密着し、No.4の頭上のゲージが満タンになるまでそこで待て。満タンになったら [plantkey] だ。爆弾は満タンの相手のうち一番近い者に付くから、押す瞬間にNo.4より近い奴がいないようにしろ。",
            [TextId.ScenarioBomberReplantNag1] = "まだNo.4を爆弾に変えていないな。広場に固まっている3人のうち、狙うのはNo.4だけだ。No.4に密着して待てば頭上のゲージが増える。満タンになったら [plantkey]。1発目と同じ手順だぞ。",
            [TextId.ScenarioBomberFire1] = "うまく爆弾に変えたな。今度は逆に、自分から爆風を浴びてもらう。",
            [TextId.ScenarioBomberFire2] = "No.4に密着して [bombkey] を押せ。今回はチュートリアルだから離れた位置では起爆させない。押すと心臓の音が鳴るが、爆風の中にいる合図だ。その場から動くな。",
            [TextId.ScenarioBomberFireNag1] = "まだ起爆していないな。No.4のすぐ隣まで詰めて密着しろ。今回はチュートリアルだから離れた位置では起爆させない。密着したら [bombkey] を押す。心臓の音は爆風の中にいる合図だから、鳴っても逃げずにその場で待て。",
            [TextId.ScenarioBomberResult1] = "Good Job![[creepycrying]]この敗北は台本どおりだ。",
            [TextId.ScenarioBomberResult2] = "お前と、爆心で削れていた仲間のNo.3が死亡し、人狼陣営が全滅したため村人陣営の勝利になった。",
            [TextId.ScenarioBomberPostgame1] = "結果画面だ。誰がどの役職だったかが開示される。",
            [TextId.ScenarioBomberPostgame2] = "負けで終わったが、これで爆弾魔のチュートリアルは成功裏に終わった。",
            [TextId.ScenarioBomberCheck1] = "起爆前に三つ確認しろ。",
            [TextId.ScenarioBomberCheck2] = "対象はトラック[[truck]]付近にいないか。自分は爆風の範囲外か。仲間の人狼を巻き込まないか。",
            [TextId.ScenarioBomberAlly1] = "1発目で仲間を爆弾に変えたのは、爆心の証言を有耶無耶にするためだ。",
            [TextId.ScenarioBomberAlly2] = "代わりに仲間は爆心のダメージを負うし、村人の集団に何食わぬ顔で紛れている間は妨害工作をしにくいという代償を払うことになる。",
            [TextId.ScenarioBomberClue1] = "村人は、爆心本人が誰と行動していたかの証言、爆風を受けても生きている者から爆弾魔を絞る。",
            [TextId.ScenarioBomberClue2] = "爆弾魔は爆発に巻き込まれたら死ぬ。爆風を受けた事が明らかな生存者は、爆弾魔ではないという強い証拠だ。",
            [TextId.ScenarioBomberWrap1] = "村人陣営にとっては集団行動をしているほど、誰に爆弾にされたのか絞りにくくなるだけでなく、起爆で受ける被害も大きくなる。",
            [TextId.ScenarioBomberWrap2] = "爆弾魔がいるかもしれない、と信じ込ませるだけでも村人は集団で動きにくくなる。逆に爆弾魔の心配が無くなったと確信されれば、村人は安心して集団で行動するようになり、人狼陣営は身動きが取れなくなるだろう。",
            [TextId.ScenarioBomberPostgame3] = "爆弾魔のチュートリアルはここまでだ[[wave]]",
            [TextId.ScenarioBomberEarly1] = "なんだ、もう爆弾に変えたのか[[:O]]",
            [TextId.ScenarioBomberEarly2] = "話が早い[[:)]]",
            [TextId.ScenarioBomberExtractionBreak1] = "お前……仕事中毒でおかしくなったのか？[[pleading]]",
            [TextId.ScenarioBomberExtractionBreak2] = "今は抽出はしなくて良い。出直してこい。",
            [TextId.ScenarioBomberSurvivedBreak1] = "なんで爆発に巻き込まれていないんだ？[[weary]]",
            [TextId.ScenarioBomberSurvivedBreak2] = "爆弾が無駄になったじゃないか。出直してこい[[angry]]",
            [TextId.ScenarioBomberWrongTarget1] = "爆弾が付いた相手が違う。その爆弾はNo.{0}に付けるものだ。",
            [TextId.ScenarioBomberWrongTarget2] =
                "爆弾は一番近くにいる相手に付く。誰よりもNo.{0}に近づいた状態で、クールダウンが明けたらもう一度 [plantkey] を押して付け替えろ。付け替えに残弾は要らない。",
            [TextId.ScenarioBomberAlreadyOnTarget1] = "その爆弾はもうNo.{0}に付いている。付け替える必要はない。",
            [TextId.ScenarioBomberAlreadyOnTarget2] =
                "本番なら別の相手へ付け替えられる（残弾は減らない）が、今はNo.{0}のまま進める。次は起爆の方だ。",
            [TextId.ScenarioBomberLineA1] = "私が爆弾にされていました。私を中心に爆発して、No.4とNo.5が巻き込まれました。",
            [TextId.ScenarioBomberLineA2] = "大ダメージです。あと一発食らったら死にます。",
            [TextId.ScenarioBomberLineA3] = "爆弾魔がいるということですね。No.3、誰と行動していましたか？",
            [TextId.ScenarioBomberLineA4] = "少し前に「爆弾を仕掛けられたかもしれない…」という接近通知は出ました。ただ、そのときは人が大勢いたので、誰が長く近くにいたかまでは覚えてません。",
            [TextId.ScenarioBomberLineA5] = "No.4とNo.5は爆風を食らっても生きています。爆弾魔なら巻き込まれた時点で死ぬので、この二人は爆弾魔ではないことは確かです。",
            [TextId.ScenarioBomberLineA6] = "爆心のNo.3も違います。爆弾魔は自分自身を爆弾にはできません。残るのはNo.1・No.2・No.6の三人の中の誰かです。",
            [TextId.ScenarioBomberLineA7] = "No.1、No.3と一緒に行動してませんでしたか？妙にべったりくっついているなと思いましたが。",
            [TextId.ScenarioBomberLineA8] = "……そんなに長かったですか？私も貴重品を運ぶのに忙しかったので分からないです。",
            [TextId.ScenarioBomberLineA9] = "爆心の本人が覚えていないなら、決め手にはなりません。",
            [TextId.ScenarioBomberLineA10] = "容疑者が多すぎます。今回はスキップにして、No.1・No.2・No.6とは距離を取りましょう。",
            [TextId.ScenarioBomberLineA11] = "単独行動は怖いです。人狼に襲われやすくなります。",
            [TextId.ScenarioBomberLineA12] = "No.3も爆弾魔では無いとしても、人狼陣営である可能性は残っています。",
            [TextId.ScenarioBomberLineA13] = "私はNo.1に入れます。",
            [TextId.ScenarioBomberLineA14] = "もう時間がありません。皆さん、投票をしてください。",

            [TextId.ScenarioShamanLineA1] = "No.1がA2でNo.7の死体を発見したんですね。",
            [TextId.ScenarioShamanLineA2] = "まずは皆さん現在の位置を教えてください。私はC4でNo.2と一緒にいます。",
            [TextId.ScenarioShamanLineA3] = "私はNo.6とE3にいます。",
            [TextId.ScenarioShamanLineA4] = "No.3は今どこにいますか？",
            [TextId.ScenarioShamanLineA5] = "えぇと、A3の辺りだと思います。",
            [TextId.ScenarioShamanLineA6] = "死体に近いですね。何か知っていますか？",
            [TextId.ScenarioShamanLineA7] = "いや、さっぱり分かりません。多分、モンスターに襲われたか、足が滑って穴に落ちたかした事故じゃないですか？",
            [TextId.ScenarioShamanLineA8] = "それは疑わしいですね。",
            [TextId.ScenarioShamanLineA9] = "No.3は今何の武器を持っていますか？",
            [TextId.ScenarioShamanLineA10] = "今はSledgeHammerとGunを持っています。",
            [TextId.ScenarioShamanLineA11] = "殺人に最適な武器を持っているというわけです。",
            [TextId.ScenarioShamanLineA12] = "単なる護身用です。モンスターを倒すために持っています。",
            [TextId.ScenarioShamanLineA13] = "もしNo.3が人狼なら、これだけ強い武器を持たせたまま会議を終えるのは危険です。私たちを襲うかもしれません。",
            [TextId.ScenarioShamanLineA14] = "そんなことはしません、私は人狼ではありません。",
            [TextId.ScenarioShamanLineA15] = "分かりました。それでは、私の武器は全て手放します。だから私を処刑しないでください。",
            [TextId.ScenarioShamanLineA16] = "うぅん、それは疑わしいです。",
            [TextId.ScenarioShamanLineA17] = "ただ、No.7が殺されたのか、事故死だったのかは断定できない状況です。",
            [TextId.ScenarioShamanLineA18] = "とりあえず今回はスキップして、No.3を監視するのはどうでしょうか。",
            [TextId.ScenarioShamanLineA19] = "私はここでNo.3を処刑するべきだと思います。",
            [TextId.ScenarioShamanLineA20] = "もう時間がありません、皆さん、投票をしてください。",

            [TextId.ScenarioShamanLineB1] = "皆さん、やられました。No.3がNo.2を殺害していました。",
            [TextId.ScenarioShamanLineB2] = "誤解です。No.2が私を襲ってきたので、身を守っただけです。正当防衛です。",
            [TextId.ScenarioShamanLineB3] = "いくらなんでも無理があります。",
            [TextId.ScenarioShamanLineB4] = "No.3は武器を手放す約束だったのではないですか？",
            [TextId.ScenarioShamanLineB5] = "私もNo.3が先に攻撃しているのを見ました。",
            [TextId.ScenarioShamanLineB6] = "ちょっと待ってください、実は私は祈祷師です。",
            [TextId.ScenarioShamanLineB7] = "最初に死体の近くに居たのは、死体を探していたのでした。",
            [TextId.ScenarioShamanLineB8] = "人狼に狙われたくないので黙っていましたが。",
            [TextId.ScenarioShamanLineB9] = "しかし、実際に死体を見つけたのはNo.1です。",
            [TextId.ScenarioShamanLineB10] = "先を越されただけです。もっと時間があれば、私が先に見つけていました。",
            [TextId.ScenarioShamanLineB11] = "No.1が本物の祈祷師だと思います。No.3は祈祷師を騙った人狼ではないでしょうか。",
            [TextId.ScenarioShamanLineB12] = "No.3で決定です。投票しましょう。",
            [TextId.ScenarioShamanLineB13] = "あぁ、惜しかったですね。もう少しで全員を殺害できそうだったのですが。",
            [TextId.ScenarioShamanLineB14] = "これだけの人数差があっては、例え強い武器があっても全員を殺害することは難しいでしょう。",

        };

        private static readonly IReadOnlyDictionary<TextId, string> EnglishTable = new Dictionary<TextId, string>

        {
            [TextId.NoticeConveneStartedFormat] = "{0} called an emergency meeting",
            [TextId.NoticeNoExecution] = "No one was executed",
            [TextId.NoticeExecutedFormat] = "{0} was executed",
            [TextId.NoticeBlackCatRevealedFormat] = "{0} was the Black Cat",
            [TextId.NoticeCurseVictimFormat] = "{0} was taken down with the Black Cat",
            [TextId.NoticeCatAwakened] = "If there is a Black Cat, they should be awake by now…",
            [TextId.NoticeConveneDeniedNoRight] = "Cannot call a meeting (no calls remaining)",
            [TextId.NoticeConveneDeniedSuppressed] = "Cannot call a meeting (currently suppressed)",
            [TextId.NoticeConveneDeniedWrongPhase] = "Cannot call a meeting (not available right now)",
            [TextId.NoticeConveneDeniedOther] = "Cannot call a meeting",
            [TextId.NoticeCorpseReportStartedFormat] = "{0} found a dead body",
            [TextId.NoticeMeetingCancelledExtraction] = "The body report was canceled because the final extraction is imminent",
            [TextId.NoticeConveneDeniedLastRun] = "Cannot call a meeting (body reports are disabled this close to the final extraction)",
            [TextId.NoticeConveneDeniedNoCorpse] = "Cannot call a meeting (no body to report)",
            [TextId.NoticePlayerDisconnectedFormat] = "{0} has disconnected from the game",
            [TextId.NoticeConveneHoldHint] = "Hold the button to call an emergency meeting",
            [TextId.NoticeScatterSlogan] = "Let's maximize our bus factor[[;)]]",
            [TextId.NoticeScatterGroupLineFormat] = "Group {0}: {1}",
            [TextId.NoticeScatterMemberFormat] = "No.{0}",
            [TextId.NoticeScatterGuardTripped] = "A handover incident has occurred",
            [TextId.NoticeScatterGroupSeparator] = ", ",
            [TextId.VoteScatterBadgeFormat] = "[{0}]",
            [TextId.VoteScatterBadgeUnknown] = "[?]",

            [TextId.OutfitBlockedHeader] = "OUTFIT LOCKED",
            [TextId.OutfitBlockedBody] = "You cannot change your appearance during a werewolf match. Change outfits in the lobby, or change the room settings.",
            [TextId.OutfitBlockedOk] = "OK",

            [TextId.RevealTeammatePrefix] = "Fellow werewolves: ",
            [TextId.RevealHeadingWinCondition] = "◆ Win Conditions (meet any one)",
            [TextId.RevealHeadingAbility] = "◆ Your Abilities",
            [TextId.RevealVillagerTitleMaybeCat] = "You may be a Villager…",
            [TextId.RevealVillagerTitle] = "You are a Villager",
            [TextId.RevealVillagerWinCondition1] = "Complete every extraction and send the truck on its way",
            [TextId.RevealVillagerWinCondition2] = "Eliminate the werewolf team",
            [TextId.RevealVillagerFlavor] = "No special abilities. Recover the valuables and stand up to the werewolves in meetings.",
            [TextId.RevealWolfTeamWinCondition1] = "Make the final extraction impossible even if every available valuable were collected",
            [TextId.RevealWolfTeamWinCondition2] = "Keep the truck from departing until time runs out",
            [TextId.RevealWolfTeamWinCondition3] = "Eliminate the villager team",
            [TextId.RevealWolfAbility1] = "Gain stronger perks as more valuables are damaged",
            [TextId.RevealWolfAbility2] = "See enemy locations on the map",
            [TextId.RevealWolfAbility3] = "Use the beacon to lure enemies to your current location",
            [TextId.RevealBomberAbility1] = "Turn another player into a bomb after spending enough time near them",
            [TextId.RevealBomberAbility2] = "Detonate whenever you like and destroy the surroundings. The bomb carrier also takes damage, but survives with 1 HP",
            [TextId.RevealBomberAbility3] = "If you are caught in that blast, you die instantly",
            [TextId.RevealBlackCatAbility] = "When chosen for execution, take one of the players who voted for you down with you",
            [TextId.RevealBlackCatNoCurse] = "You and the werewolves do not know each other's identities, but win or lose together as the werewolf team",
            [TextId.RevealWerewolfTitle] = "You are a Werewolf",
            [TextId.RevealBlackCatTitle] = "You are the Black Cat",
            [TextId.RevealBlackCatAwakeningTitle] = "You were the Black Cat all along.",
            [TextId.RevealBomberTitle] = "You are the Bomber",
            [TextId.RevealShamanTitle] = "You are the Shaman",
            [TextId.RevealShamanAbility1] = "Stand still and hold your gaze steady as your vision fades and spirit vision begins. Keep looking toward an unreported body to trigger a haunting.",
            [TextId.RevealShamanAbility2] = "The closer you get to an unreported body, the stronger the haunting on your screen",
            [TextId.RevealHeadingTips] = "◆ Tips",
            [TextId.RevealVillagerTipConvene] = "Open a meeting by holding the red button at the back of the truck",
            [TextId.RevealVillagerTipReport] = "Press the report key near a body to call a meeting",
            [TextId.RevealVillagerTipValuableMap] = "The map's data on valuables is updated at every meeting",
            [TextId.RevealVillagerTipAliveCheck] = "Meetings show whether each player is alive or dead",
            [TextId.TutorialSkipHint] = "[menu] to skip",
            [TextId.RevealSelfIdFormat] = "Your ID number is No.{0}",

            [TextId.RoleNameWerewolf] = "Werewolf",
            [TextId.RoleNameBlackCat] = "Black Cat",
            [TextId.RoleNameVillager] = "Villager",
            [TextId.RoleNameBomber] = "Bomber",
            [TextId.RoleNameShaman] = "Shaman",

            [TextId.GaugePerkStaminaLabel] = "Infinite Stamina",
            [TextId.GaugePerkJumpLabel] = "Extra Jumps",
            [TextId.GaugePerkEnemyIgnoreLabel] = "Monster Camouflage",
            [TextId.GaugePerkHealLabel] = "Regeneration",
            [TextId.GaugePerkInformantLabel] = "Werewolves Revealed to Black Cat",

            [TextId.GaugeBeaconRuleFormat] = "Beacon +1/{0}%",
            [TextId.GaugeBombRuleFormat] = "Bomb +1/{0}%",
            [TextId.GaugePercentDollarsFormat] = "{0}% (${1})",
            [TextId.GaugeLossOverBaseFormat] = "${0} / ${1}",
            [TextId.GaugeNextUpdateFormat] = "Next update in {0}s",

            [TextId.HudTimerFrozenFormat] = "{0}  paused",
            [TextId.HudGaugeFormat] = "Gauge {0}%",
            [TextId.HudRightsFormat] = "Meeting calls left: {0}",
            [TextId.HudTimeRemainingFormat] = "{0}:{1} left",
            [TextId.HudWolfToggleFormat] = "Wolf Mode [{0}]",
            [TextId.HudBeaconKeyFormat] = "Beacon [{0}]",
            [TextId.HudBeaconLabel] = "Beacon",
            [TextId.HudTestPlayBanner] = "TEST PLAY: the host has debug mode enabled",
            [TextId.HudScatterGuardBanner] = "Handover in progress…",

            [TextId.BannerMeetTheDeadline] = "Meet the deadline.",
            [TextId.BannerHurryUp] = "Hurry up!!",

            [TextId.ImpactDiscussLeft] = "DISCUSS",
            [TextId.ImpactDiscussRight] = "NOW!",

            [TextId.HudSelfIdFormat] = "No.{0}",
            [TextId.IdNameFormat] = "{0}. {1}",

            [TextId.StartHoldWaitingOthers] = "Waiting for other players…",

            [TextId.PreplayAbortTitle] = "The match could not be started",
            [TextId.PreplayAbortTooFewPlayers] =
                "A player left during loading, leaving fewer than the minimum of three players.",
            [TextId.PreplayAbortInvalidConfig] =
                "After a player left during loading, the configured roles no longer fit the current player count.",
            [TextId.PreplayAbortRoleRevealDisconnect] =
                "A player left during the role reveal, leaving no playable team composition.",
            [TextId.PreplayAbortNoRoles] = "No roles were assigned for this round.",
            [TextId.PreplayAbortRolesDiscarded] =
                "The revealed roles are discarded. No outcome, rewards, or replay are recorded.",

            [TextId.ResultBannerVillagerWin] = "Villager Team Wins",
            [TextId.ResultBannerWerewolfWin] = "Werewolf Team Wins",
            [TextId.ResultBannerDefault] = "Match Results",

            [TextId.ResultBannerVoid] = "No Contest",

            [TextId.ResultBannerPeacefulSurvived] = "The Peaceful Village Endured",
            [TextId.ResultBannerPeacefulCollapsed] = "The Peaceful Village Collapsed",
            [TextId.VoidMatchChargingLabel] = "No Contest",
            [TextId.VoidMatchArmHeader] = "Declare this match a no contest?",
            [TextId.VoidMatchArmBody] =
                "The match ends with no winner. No cosmetic tokens are awarded.",
            [TextId.VoidMatchConfirmPromptFormat] = "Hold {0} for {1} seconds to confirm",
            [TextId.VoidMatchCancelHintFormat] = "Esc to cancel (closes automatically in {0}s)",
            [TextId.ResultStatusAlive] = "Alive",
            [TextId.ResultStatusDead] = "Dead",
            [TextId.ResultStatusExecuted] = "Executed",
            [TextId.ResultStatusDisconnected] = "Disconnected",
            [TextId.ResultReturnButtonLabel] = "Return to Lobby",
            [TextId.ResultReturnButtonWithKeyFormat] = "Return to Lobby [{0}]",
            [TextId.ResultAutoReturnCountdownFormat] = "Auto-return in about {0}s",
            [TextId.ResultWaitingHost] = "Waiting for the host…",
            [TextId.ResultFooterWithCountdownFormat] = "{0}  |  Auto-return in about {1}s",

            [TextId.ResultDigestHeader] = "── Match Timeline (scroll with mouse wheel) ──",
            [TextId.DigestMatchStart] = "Match start",
            [TextId.DigestMeetingButtonFormat] = "{0} called a meeting",
            [TextId.DigestMeetingReportFormat] = "{0} reported a body",
            [TextId.DigestExecutedFormat] = "{0} was executed by vote",
            [TextId.DigestNoExecution] = "Vote result: no one was executed",
            [TextId.DigestCurseStartedFormat] = "{0} was the Black Cat",
            [TextId.DigestCurseFollowFormat] = "{0} was taken down with the Black Cat",
            [TextId.DigestDeathFormat] = "{0} died",
            [TextId.DigestBombDetonatedFormat] = "The bomb planted on {0} detonated",
            [TextId.DigestCheckmate] = "Value Checkmate",
            [TextId.DigestMatchEndFormat] = "{0} ({1})",
            [TextId.DigestReasonWerewolvesEradicated] = "all werewolves eliminated",
            [TextId.DigestReasonVillagersEradicated] = "villager team eliminated",
            [TextId.DigestReasonExtractionCompleted] = "truck departed",
            [TextId.DigestReasonTimerExpired] = "time expired",
            [TextId.DigestReasonExtractionFailed] = "extraction failed",
            [TextId.DigestReasonValueCheckmate] = "Value Checkmate",
            [TextId.DigestExtractionDoneFormat] = "Extraction point completed ({0}/{1})",
            [TextId.DigestPerkUnlockedFormat] = "Werewolf perk \"{0}\" unlocked",
            [TextId.DigestInformant] = "Informant activated (werewolf team revealed to the Black Cat)",
            [TextId.DigestFinalBalanceFormat] = "Final tally: extracted ${0} / quota left ${1} / obtainable ${2}",

            [TextId.EpigraphWerewolvesEradicated] = "The werewolves were wiped out.",
            [TextId.EpigraphVillagersEradicated] = "The villagers were wiped out.",
            [TextId.EpigraphExtractionCompleted] = "The debt was collected.",
            [TextId.EpigraphTimerExpired] = "The debt fell into delinquency.",
            [TextId.EpigraphExtractionFailed] = "The debt was written off.",
            [TextId.EpigraphValueCheckmate] = "The village went insolvent.",
            [TextId.EpigraphPeacefulSurvived] = "They doubted one another. Yet they still stood together.",
            [TextId.EpigraphPeacefulCollapsed] = "There were never any werewolves. Yet the village still fell.",

            [TextId.VoteMeetingTitle] = "Emergency Meeting",
            [TextId.VoteSkipLabel] = "Skip",
            [TextId.VoteConfirmLabel] = "Sure?",
            [TextId.VoteVoteLabel] = "Vote",
            [TextId.VoteWerewolfMarkerLabel] = "W",
            [TextId.VoteBomberMarkerLabel] = "B",
            [TextId.VoteCountFormat] = "×{0}",
            [TextId.VoteExecutedFormat] = "Executed: {0}",
            [TextId.VoteNoExecution] = "No execution",
            [TextId.VoteSkipSuffixFormat] = " (Skip: {0})",

            [TextId.ChatLogTitle] = "Meeting Chat Log",
            [TextId.ChatLogEmpty] = "No messages yet",
            [TextId.ChatLogDeadHint] = "Gray messages are visible only to the dead",
            [TextId.ChatLogToggleLabelFormat] = "Meeting Chat Log [{0}]",
            [TextId.ChatLogVoted] = "has voted.",
            [TextId.ChatLogSystemName] = "Taxman",
            [TextId.ChatLogScatterTitle] = "Previous groups",
            [TextId.ChatLogScatterLineFormat] = "{0}: {1}",
            [TextId.ChatLogMeetingNumberFormat] = "Meeting #{0}",
            [TextId.ChatLogFilteringTitle] = "Filtering",

            [TextId.ChatStampButtonLabel] = "Stamps",
            [TextId.ChatStampUnknown] = "(Unsupported stamp)",
            [TextId.ChatStampCategoryGuide] = "Guide",
            [TextId.ChatStampCategoryPress] = "Ask",
            [TextId.ChatStampCategoryDefense] = "Defense",
            [TextId.ChatStampCategoryClaim] = "Claim",
            [TextId.ChatStampCategoryChatter] = "Chat",
            [TextId.ChatStampGuideVoteSkip] = "Let's vote to skip.",
            [TextId.ChatStampGuideVoteSuspect] = "Let's vote out whoever's suspicious.",
            [TextId.ChatStampGuideExtraction] = "Let's go for the delivery win.",
            [TextId.ChatStampGuideFriendly] = "Be kind, do not swear, please!",
            [TextId.ChatStampPressSuspicious] = "That's suspicious.",
            [TextId.ChatStampPressWhere] = "Where were you?",
            [TextId.ChatStampPressWithWhom] = "Who were you with?",
            [TextId.ChatStampPressDoing] = "What were you doing?",
            [TextId.ChatStampPressItems] = "What's in your inventory?",
            [TextId.ChatStampPressDropWeapon] = "Please drop your weapon.",
            [TextId.ChatStampDefenseInnocent] = "I'm innocent.",
            [TextId.ChatStampDefenseWasDead] = "I was dead.",
            [TextId.ChatStampDefenseAccident] = "It was an accident.",
            [TextId.ChatStampDefenseDontKnow] = "I don't know.",
            [TextId.ChatStampDefenseForgot] = "I forgot.",
            [TextId.ChatStampDefenseNoIdea] = "Clear as mud.",
            [TextId.ChatStampClaimAgree] = "Exactly!",
            [TextId.ChatStampClaimDisagree] = "No, that's wrong!",
            [TextId.ChatStampClaimObjection] = "Objection!",
            [TextId.ChatStampClaimTheyDidIt] = "They did it.",
            [TextId.ChatStampClaimItWasMe] = "That was... me!",
            [TextId.ChatStampClaimTogether] = "Let's stick together.",
            [TextId.ChatStampChatterMorning] = "hello.",
            [TextId.ChatStampChatterGg] = "Good game, everyone.",
            [TextId.ChatStampChatterWantLive] = "I want to be alive!",
            [TextId.ChatStampChatterDied] = "I'm dead...",
            [TextId.ChatStampChatterLookAtMe] = "Look what I've become... Hahaha..",
            [TextId.ChatStampChatterAsPlanned] = "Exactly as planned.",
            [TextId.ChatStampChatterWhatever] = "Work out somehow!",
            [TextId.ChatStampChatterSorry] = "Sorry!",
            [TextId.ChatStampChatterHelp] = "Please help me!",
            [TextId.ChatStampChatterCry] = "Waaah!",

            [TextId.ResultChatTitle] = "Post-Match Chat",
            [TextId.ResultChatToggleLabelFormat] = "Post-Match Chat [{0}]",

            [TextId.ReplayToggleLabel] = "Replay",
            [TextId.ReplayPlay] = "Play",
            [TextId.ReplayPause] = "Pause",
            [TextId.ReplaySpeedFormat] = "{0}x",
            [TextId.ReplaySaveButton] = "Save",
            [TextId.ReplaySavedButton] = "Saved",
            [TextId.ReplaySavedToDownloads] = "Replay saved (Downloads)",
            [TextId.ReplaySavedToModFolder] = "Replay saved (Replays in the MOD folder)",
            [TextId.ReplayAlreadySaved] = "This match is already saved",
            [TextId.ReplaySaveFailed] = "Failed to save",
            [TextId.ReplayNoData] = "No replay data",
            [TextId.ReplayPositionCapped] = "Recording limit reached: no position data past this point",
            [TextId.ReplaySavedCapped] = "(positions truncated)",
            [TextId.ReplayGaugeLossFormat] = "Lost ${0} / Total ${1}",
            [TextId.ReplayGaugeDeliveredFormat] = "Extracted ${0}",
            [TextId.ReplayEpStateNone] = "Inactive",
            [TextId.ReplayEpStateIdle] = "Idle",
            [TextId.ReplayEpStateActive] = "Open",
            [TextId.ReplayEpStateSuccess] = "Goal met",
            [TextId.ReplayEpStateWarning] = "Closing",
            [TextId.ReplayEpStateCancel] = "Cancelled",
            [TextId.ReplayEpStateExtracting] = "Extracting",
            [TextId.ReplayEpStateComplete] = "Complete",
            [TextId.ReplayEpStateSurplus] = "Surplus",
            [TextId.ReplayEpStateTaxReturn] = "Tax return",
            [TextId.ReplayStampNoExecution] = "Vote result / No execution",
            [TextId.ReplayStampExecutedFormat] = "Executed / {0}. {1}",

            [TextId.RecapNameSeparator] = ", ",
            [TextId.RecapDeathsFormat] = "Deaths: {0}",
            [TextId.RecapDeathsNone] = "Deaths: none",
            [TextId.RecapLostFormat] = "Valuables destroyed: -${0}",
            [TextId.RecapHaulFormat] = "Extracted: ${0} / Quota ${1}",
            [TextId.RecapBeaconFormat] = "Beacon uses: {0}",
            [TextId.RecapBeaconNone] = "Beacon uses: none",

            [TextId.DeathRevealTitle] = "Deaths",
            [TextId.DeathRevealNone] = "No one died",
            [TextId.DeathRevealScatterGuardTitle] = "HANDOVER INCIDENT",

            [TextId.CheckmateTitle] = "THE DEBT CAN NO LONGER BE COLLECTED",

            [TextId.EradicationLastWerewolfDied] = "THE LAST WEREWOLF HAS DIED",
            [TextId.EradicationLastVillagerDied] = "THE LAST VILLAGER HAS DIED",
            [TextId.EradicationLastWerewolfVanished] = "THE LAST WEREWOLF HAS VANISHED",
            [TextId.EradicationLastVillagerVanished] = "THE LAST VILLAGER HAS VANISHED",
            [TextId.EradicationPeacefulCollapsed] = "THE PEACEFUL VILLAGE HAS PERISHED",

            [TextId.MapOverlayToggleLabelFormat] = "Full Map [{0}]",
            [TextId.MapOverlayMapToolBlocked] = "Close your handheld map to open the full map",

            [TextId.LobbySettingsFooterHintFormat] = "[{0}]: Hide panel  /  Mouse wheel: Scroll",
            [TextId.LobbySettingsMiniHintFormat] = "[{0}]: Show Werewolf room settings",

            [TextId.ModIntegrityHeaderAllMatchFormat] = "✓ Matching mod configurations: {0}/{1}",
            [TextId.ModIntegrityHeaderCountsFormat] = "Baseline {0}  ✓ {1}  ! {2}  × {3}  ? {4}",
            [TextId.ModIntegritySelfDifferenceFormat] = "Your mod configuration differs (missing {0} / extra {1} / version {2} / content {3})",
            [TextId.ModIntegritySelfUnavailable] = "Your mod info could not be verified",
            [TextId.ModIntegrityPanelTitle] = "Mod Configurations (Room Baseline Comparison)",
            [TextId.ModIntegrityFilterNeedsReview] = "Needs review",
            [TextId.ModIntegrityFilterMatch] = "Match",
            [TextId.ModIntegrityFilterAll] = "All",
            [TextId.ModIntegrityStatusBaseline] = "Baseline",
            [TextId.ModIntegrityStatusPending] = "Checking",
            [TextId.ModIntegrityStatusMatch] = "Match",
            [TextId.ModIntegrityStatusDifference] = "Differs",
            [TextId.ModIntegrityStatusUnavailable] = "Unavailable",
            [TextId.ModIntegrityReasonNoResponse] = "No response",
            [TextId.ModIntegrityReasonUnsupportedProtocol] = "Unsupported version",
            [TextId.ModIntegrityReasonInvalidPayload] = "Invalid comparison data",
            [TextId.ModIntegrityReasonTooLarge] = "Data exceeds the size limit",
            [TextId.ModIntegrityReasonCollectionFailed] = "Failed to collect mod info",
            [TextId.ModIntegrityDetailLoading] = "Loading details…",
            [TextId.ModIntegrityDetailFailed] = "Failed to load details  Retry",
            [TextId.ModIntegrityDisclaimer] = "This view compares client self-reports against the room baseline and does not guarantee the absence of cheats.",
            [TextId.ModIntegrityDetailMissingFormat] = "Missing: {0} ({1})",
            [TextId.ModIntegrityDetailExtraFormat] = "Extra: {0} ({1})",
            [TextId.ModIntegrityDetailVersionFormat] = "Version: {0}  baseline {1} → player {2}",
            [TextId.ModIntegrityDetailContentFormat] = "Content: {0}  baseline {1} → player {2}",
            [TextId.ModIntegrityStartCautionTitle] = "Mod configurations differ",
            [TextId.ModIntegrityStartSevereTitle] = "Some mod configurations are unverified",
            [TextId.ModIntegrityStartBodyFormat] = "Different: {0}  Unavailable: {1}  Checking: {2}\nThis may affect fair play.",
            [TextId.ModIntegrityButtonBack] = "Back",
            [TextId.ModIntegrityButtonDetails] = "View mod configurations",
            [TextId.ModIntegrityButtonContinue] = "Start anyway",
            [TextId.ModIntegrityButtonClose] = "Close",
            [TextId.LobbyStartTooFewPlayersTitle] = "Not enough players",
            [TextId.LobbyStartTooFewPlayersBodyFormat] =
                "Current players: {0} / Werewolf mode needs at least {1}.",
            [TextId.LobbyStartTeamOverflowTitle] = "Too many werewolves",
            [TextId.LobbyStartTeamOverflowBodyFormat] =
                "With {0} werewolves among {1} players, no villagers remain.\n"
                + "Lower the werewolf count (WerewolfCount).",
            [TextId.LobbyStartBlockedRemedy] =
                "To play regular R.E.P.O. instead, turn Werewolf Mode\n"
                + "(WerewolfModeEnabled) off in the host's settings.",

            [TextId.ConveneCountdownDefaultCallerName] = "Someone",
            [TextId.ConveneCountdownHeaderFormat] = "{0} called a meeting!\nWarping in…",
            [TextId.ConveneCountdownCorpseHeaderFormat] = "{0} reported a body!\nWarping in…",
            [TextId.ConveneCountdownScatterGuardHeader] = "A handover incident has occurred!\nWarping in…",

            [TextId.HudCorpseReportKeyFormat] = "Report [{0}]",

            [TextId.MeetingButtonSuppressCountdownFormat] = "Emergency Meeting (in {0}s)",
            [TextId.MeetingButtonConveneGrabPrompt] = "Call Emergency Meeting [Hold Grab]",
            [TextId.MeetingButtonConveneInteractPrompt] = "Call Emergency Meeting [Hold Interact]",
            [TextId.MeetingButtonSuppressedPrompt] = "Emergency Meeting (unavailable now)",
            [TextId.MeetingButtonRightsSuffixFormat] = " ({0} left)",

            [TextId.CurseBlackCatRevealedFormat] = "{0} was the Black Cat. They are choosing someone to take with them…",
            [TextId.CurseNoVictim] = "The Black Cat took no one with them",

            [TextId.SettingsSectionGeneral] = "General",
            [TextId.SettingsSectionMeeting] = "Meeting",
            [TextId.SettingsSectionRoleAssignment] = "Roles - Assignment",
            [TextId.SettingsSectionRoles] = "Roles - Werewolf",
            [TextId.SettingsSectionBlackCat] = "Roles - Black Cat",
            [TextId.SettingsSectionBomber] = "Roles - Bomber",
            [TextId.SettingsSectionShaman] = "Roles - Shaman",
            [TextId.SettingsSectionWorldgen] = "Starting Environment",
            [TextId.SettingsSectionStartItemList] = "Starting Items",
            [TextId.SettingsSectionStartUpgradeList] = "Upgrades (everyone)",

            [TextId.SettingsBoolEnabled] = "On",
            [TextId.SettingsBoolDisabled] = "Off",
            [TextId.SettingsAuto] = "Auto",
            [TextId.SettingsValueVanilla] = "Vanilla",
            [TextId.SettingsListEmpty] = "None",
            [TextId.SettingsPresent] = "Yes",
            [TextId.SettingsRandom] = "Random",
            [TextId.SettingsValuableMapRealtime] = "Real-Time",
            [TextId.SettingsValuableMapMeetingSync] = "Meeting sync",
            [TextId.SettingsValuableMapHidden] = "Hidden",
            [TextId.SettingsNecroVoiceOff] = "OFF",
            [TextId.SettingsNecroVoiceNonWerewolfDead] = "Dead non-werewolves",
            [TextId.SettingsNecroVoiceAllDead] = "All dead players",
            [TextId.SettingsItemsAggregateFormat] = "{0} item types, {1} total ({2})",
            [TextId.SettingsAggregateMoreSuffix] = "…",
            [TextId.SettingsUpgradeItemFormat] = "{0}+{1}",
            [TextId.SettingsListSeparator] = ", ",
            [TextId.SettingsUnitSeconds] = "s",
            [TextId.SettingsUnitTimes] = " time(s)",
            [TextId.SettingsUnitItems] = " item(s)",
            [TextId.SettingsUnitPeople] = " player(s)",
            [TextId.SettingsUnitMeters] = "m",
            [TextId.SettingsUnitPercent] = "%",
            [TextId.SettingsUnitDamage] = " dmg",
            [TextId.SettingsUnitSets] = " set(s)",

            [TextId.SettingsLabelWerewolfCount] = "Werewolves",
            [TextId.SettingsLabelBlackCatChancePercent] = "Black Cat chance",
            [TextId.SettingsLabelBomberChancePercent] = "Bomber chance",
            [TextId.SettingsLabelPeacefulVillageChancePercent] = "Peaceful village chance",
            [TextId.SettingsLabelShamanChancePercent] = "Shaman chance",
            [TextId.SettingsLabelRoundSeconds] = "Round time limit",
            [TextId.SettingsLabelBlackCatRevealDelaySec] = "Black Cat awakening delay",
            [TextId.SettingsLabelBlackCatCurseEnabled] = "Black Cat's Revenge",
            [TextId.SettingsLabelMeetingRightsPerPlayer] = "Meeting calls per player",
            [TextId.SettingsLabelConveneSuppressStartSec] = "Meeting lockout after start",
            [TextId.SettingsLabelConveneSuppressAfterSec] = "Meeting lockout after meeting",
            [TextId.SettingsLabelMeetingCountdownSec] = "Meeting warp countdown",
            [TextId.SettingsLabelMeetingDurationSec] = "Meeting time limit",
            [TextId.SettingsLabelVoteTimeCutEnabled] = "Votes shorten the meeting",
            [TextId.SettingsLabelMeetingScatterEnabled] = "Scatter warp after meetings",
            [TextId.SettingsLabelScatterGuardSec] = "Post-scatter guard window",
            [TextId.SettingsLabelMeetingEnemyRespawnScalePct] = "Enemy respawn clock during meetings",
            [TextId.SettingsLabelResultDisplaySec] = "Vote result display duration",
            [TextId.SettingsLabelStaminaUnlockPct] = "Infinite stamina unlock",
            [TextId.SettingsLabelJumpUnlockPct] = "Extra jump unlock",
            [TextId.SettingsLabelEnemyIgnoreUnlockPct] = "Monster camouflage unlock",
            [TextId.SettingsLabelHealUnlockPct] = "Regeneration unlock",
            [TextId.SettingsLabelHealIntervalSec] = "Regeneration interval",
            [TextId.SettingsLabelBeaconChargePct] = "Beacon recharge threshold",
            [TextId.SettingsLabelInformantThresholdPct] = "Informant threshold",
            [TextId.SettingsLabelExtraJumpCount] = "Extra mid-air jumps",
            [TextId.SettingsLabelBeaconCooldownSec] = "Beacon cooldown",
            [TextId.SettingsLabelBeaconSuppressStartSec] = "Beacon lockout after start",
            [TextId.SettingsLabelBeaconSuppressAfterMeetingSec] = "Beacon lockout after meeting",
            [TextId.SettingsLabelCatGaugeSyncIntervalSec] = "Black Cat gauge update interval",
            [TextId.SettingsLabelOrbGaugeEnabled] = "Include orb value in loss gauge",
            [TextId.SettingsLabelWerewolfModeEnabled] = "Werewolf Mode",
            [TextId.SettingsLabelMinimapHideEnabled] = "Hide corpses on minimap",
            [TextId.SettingsLabelOutfitChangeAllowed] = "Outfit changes during a match",
            [TextId.SettingsLabelValuableMapMode] = "Valuables map mode",
            [TextId.SettingsLabelGameOverAutoReturnSec] = "Result screen auto-return (0 = never)",
            [TextId.SettingsLabelNecroVoiceMode] = "Voices of the dead (dead → living werewolves)",
            [TextId.SettingsLabelStartLevelNumber] = "Level",
            [TextId.SettingsLabelStartMapName] = "Map",
            [TextId.SettingsLabelStartItemsSpec] = "Starting items",
            [TextId.SettingsLabelStartEnergyPct] = "Starting truck charge",
            [TextId.SettingsLabelStartUpgradesSpec] = "Starting upgrades",
            [TextId.SettingsLabelOrbDropMax] = "Orbs dropped by enemies",
            [TextId.SettingsLabelStartHaulGoalPct] = "Quota (share of map total)",
            [TextId.SettingsLabelEnemySetsDanger1] = "Enemy sets (Danger 1)",
            [TextId.SettingsLabelEnemySetsDanger2] = "Enemy sets (Danger 2)",
            [TextId.SettingsLabelEnemySetsDanger3] = "Enemy sets (Danger 3)",

            [TextId.SettingsLabelBomberProximityMeters] = "Bomb-planting range",
            [TextId.SettingsLabelBomberGaugeFullSec] = "Time required to plant",
            [TextId.SettingsLabelBomberInitialCooldownSec] = "Starting cooldown",
            [TextId.SettingsLabelBomberCooldownSec] = "Regular cooldown",
            [TextId.SettingsLabelBomberBlastRadiusMeters] = "Blast and warning radius",
            [TextId.SettingsLabelBomberBlastPlayerDamage] = "Player damage",
            [TextId.SettingsLabelBomberBlastEnemyDamage] = "Enemy damage",
            [TextId.SettingsLabelBomberAmmoRefillPct] = "Bomb refill threshold",
            [TextId.SettingsLabelShamanGazeFullSec] = "Spirit vision gaze time",
            [TextId.SettingsLabelShamanGhostCooldownSec] = "Spirit vision cooldown",
            [TextId.SettingsLabelShamanStormWeakMeters] = "Haunting (weak) radius",
            [TextId.SettingsLabelShamanStormMediumMeters] = "Haunting (medium) radius",
            [TextId.SettingsLabelShamanStormStrongMeters] = "Haunting (strong) radius",
            [TextId.HudBomberPlantKeyFormat] = "Plant Bomb [{0}]",
            [TextId.HudBomberDetonateKeyFormat] = "Detonate [{0}]",

            [TextId.HudValuableRecordOnFormat] = "Recording [hold {0}]",
            [TextId.HudValuableRecordOffFormat] = "Not Recording [hold {0}]",

            [TextId.BomberDenyNoAmmo] = "No bombs left",
            [TextId.BomberDenyNoFullTarget] = "No target is ready for a bomb",
            [TextId.BomberDenyPlantCooldown] = "Cannot plant a bomb (on cooldown)",
            [TextId.BomberDenyDetonateCooldown] = "Cannot detonate (on cooldown)",
            [TextId.BomberDenyNoBomb] = "Cannot detonate (no bomb planted)",
            [TextId.BomberDenyMeetingLocked] = "Cannot detonate (meeting in progress)",
            [TextId.BomberDenyTruckZone] = "Cannot detonate (target is near the truck)",
            [TextId.BomberDudTargetDead] = "The target was already dead. The bomb was a dud",
            [TextId.BomberTargetDisconnected] = "The target disconnected. The bomb is gone",
            [TextId.BomberProximityWarning] = "Someone may have planted a bomb on me…",

            [TextId.TutorialCorpseDiscovery] =
                "You found an unreported body. Good job! 😂\n" +
                "The dead don't come back, so no need to haul them to an extraction point.\n" +
                "You can report an unreported body even with no meeting calls left (the icon at the bottom right of the HUD pulses in color when you are close enough).\n" +
                "At the meeting, tell everyone where and how you found the body.",

            [TextId.TutorialMeetingCountdownStarted] =
                "A meeting has been called. This is the countdown until everyone warps to the truck.\n" +
                "Whatever valuables, weapons, or drones you're holding will be left behind. Put them in your inventory to take them with you.\n" +
                "Hurry and secure any valuables so they are not dropped and damaged or left in a corridor where monsters can smash them.",

            [TextId.TutorialFirstMeetingAsVillager] =
                "During a meeting, you can view the full map with updated locations and status information for the valuables.\n" +
                "Assess the situation, share information, and tell everyone whom you do and do not suspect.\n" +
                "Vote for anyone you find suspicious. You can have them executed.",

            [TextId.TutorialWerewolfRoleDrawn] =
                "You are a Werewolf. Congratulations! 😂\n" +
                "The more valuables you destroy, the more powerful perks you unlock.\n" +
                "Damage valuables without the villagers noticing and reap the rewards.\n" +
                "Only Werewolves can see monster positions on the map. Use that knowledge well.",

            [TextId.TutorialFirstValuableSeen] =
                "You found a valuable.\n" +
                "Its position is recorded on the map, but the map will not update if it is moved or destroyed afterward.\n" +
                "It only updates when a meeting starts or an extraction is completed.\n" +
                "If a valuable isn't where the map says it should be, someone carried it off—or destroyed it.",

            [TextId.TutorialWolfModeFirstUnlock] =
                "Enough valuables have been destroyed. Wolf Mode is unlocked.\n" +
                "You can use powerful abilities while Wolf Mode is active.\n" +
                "Do not get careless and use them in front of the villagers. You can turn Wolf Mode off to avoid giving yourself away.",

            [TextId.TutorialBeaconFirstCharged] =
                "Enough valuables have been destroyed. The beacon is unlocked.\n" +
                "The beacon lures monsters from across the map to your current position. It even brings back monsters that were killed and despawned.\n" +
                "Anyone seen at the center of a monster swarm will look like the source. Choose your location carefully.\n" +
                "Check the monsters' positions on the map first. Do not blame me if you get caught in the chaos yourself.",

            [TextId.TutorialFirstMeetingAsWerewolf] =
                "During a meeting, the valuables map is updated and the latest information is shared with everyone.\n" +
                "Every Werewolf executed puts the werewolf team at a disadvantage.\n" +
                "Make excuses. Play innocent. Share useless information and hide what matters. Use lies to make others look suspicious.\n" +
                "Good luck 😂",

            [TextId.TutorialFirstMeetingAsBlackCat] =
                "If the Black Cat is executed at a meeting, they can take one of the players who voted for them down with them.\n" +
                "If you already know who you want to take with you, steer them into voting for you.\n" +
                "Any Werewolf who voted for you is a valid target too. Choose whom you take with you carefully.",

            [TextId.TutorialVillagerSeesCatAwakened] =
                "Official notice: there may be a Black Cat on the werewolf team.\n" +
                "When the Black Cat is executed at a meeting, they can take one of the players who voted for them down with them.\n" +
                "Will you execute them at a meeting? Or remove them by force and accept the risk of retaliation and the cost in resources?\n" +
                "Decide carefully.",

            [TextId.TutorialBlackCatRoleDrawn] =
                "You are the Black Cat. Congratulations! 😂\n" +
                "When you are executed by vote, you can take one of the players who voted for you down with you.\n" +
                "While the Werewolves are busy posing as Villagers, run wild and support them.\n" +
                "But you are not told who the Werewolves are. Try not to take one down by accident.\n" +
                "The Black Cat belongs to the werewolf team but does not count as a surviving Werewolf. If every Werewolf dies, the villager team wins even if the Black Cat is still alive.",

            [TextId.TutorialBlackCatRoleDrawnNoCurse] =
                "You are the Black Cat. Congratulations! 😂\n" +
                "The Black Cat belongs to the werewolf team, but the Werewolves and the Black Cat do not know each other's identities.\n" +
                "While the Werewolves are busy posing as Villagers, run wild and support them.\n" +
                "The Black Cat does not count as a surviving Werewolf. If every Werewolf dies, the villager team wins even if the Black Cat is still alive.",

            [TextId.TutorialLastRunApproaching] =
                "The final extraction order has been issued. Body reports are no longer accepted.\n" +
                "Meetings can now only be called using the button in the truck.\n" +
                "Choose carefully who stays with the truck and who handles the final extraction.",

            [TextId.TutorialRoundTimeWarningVillager] =
                "The deadline is coming up.\n" +
                "Complete every extraction and send the truck on its way before the deadline. If you fail, the villagers lose.\n" +
                "If you can't make it in time, wiping out the werewolves is your only option.",

            [TextId.TutorialRoundTimeWarningWerewolf] =
                "The deadline is coming up.\n" +
                "Keep the truck from departing until the deadline, and the werewolves win.\n" +
                "Stay sharp until the very end.",

            [TextId.TutorialFinalExtractionVillager] =
                "The final extraction is complete.\n" +
                "If even one villager is alive when the truck departs, you win.\n" +
                "Stay sharp until the very end.",

            [TextId.TutorialFinalExtractionWerewolf] =
                "The final extraction has been completed.\n" +
                "If any villager is alive when the truck departs, the werewolves lose.\n" +
                "At this point, wiping out the villagers is your only option.",

            [TextId.TutorialInformantUnlockedAsWerewolf] =
                "If there is a Black Cat, they now know who the Werewolves are.\n" +
                "Try to arrange a discreet meeting somewhere.\n" +
                "Or they may make the first move during a meeting. Be ready to play along.",

            [TextId.TutorialInformantUnlockedAsBlackCat] =
                "Enough valuables have been destroyed. You can now tell who the Werewolves are.\n" +
                "Players whose names appear in red—or who have a wolf icon during meetings—are Werewolves.\n" +
                "Quietly let the Werewolves know you are the Black Cat without alerting the villagers.",

            [TextId.TutorialEnemyIgnoreUnlockedAsWerewolf] =
                "Enough valuables have been destroyed. Monsters now see you as one of their own.\n" +
                "While Wolf Mode is on, most monsters won't react to seeing you.\n" +
                "They still react to sound, and you can still be caught in their attacks.\n" +
                "By the way, did you know that loud noises make vanished monsters return sooner?",

            [TextId.TutorialNaturalHealUnlockedAsWerewolf] =
                "Enough valuables have been destroyed. Your body is becoming something beyond human.\n" +
                "While Wolf Mode is on, your health now slowly recovers on its own.\n" +
                "There is no visual effect or sound, but other players can see the health bar on your back.\n" +
                "If you were near death moments ago but your health has mysteriously recovered, explaining it will not be easy.",

            [TextId.TutorialWerewolfSeesCatAwakened] =
                "Official notice: there may be a Black Cat on the werewolf team.\n" +
                "The Werewolves and the Black Cat are allies, but the Werewolves are not told who the Black Cat is.\n" +
                "The Black Cat belongs to the werewolf team but does not count as a surviving Werewolf. If every Werewolf dies, the villager team wins whether the Black Cat is alive or dead.",

            [TextId.TutorialBeaconFirstUsedAsWerewolf] =
                "So you used the beacon.\n" +
                "Monsters across the map will now gather at that spot.\n" +
                "The number of beacon uses is revealed at the next meeting, so be careful not to hand the villagers any clues.",

            [TextId.TutorialBlackCatSelectedForExecution] =
                "You've been chosen for execution. Good job! 😂\n" +
                "Pick someone to take down with you.\n" +
                "If you don't, someone will be picked at random.",

            [TextId.TutorialBlackCatExecutionRevealed] =
                "The player with the most votes was the Black Cat.\n" +
                "When the Black Cat is executed, one of the players who voted for them is taken down with them.",

            [TextId.TutorialFirstDeath] =
                "Your body has been destroyed. You now exist only as consciousness data uploaded to the cloud.\n" +
                "No new body will be issued during this match.\n" +
                "You cannot use your own Death Head to talk to the living, either.",

            [TextId.TutorialBomberRoleDrawn] =
                "You are the Bomber. Congratulations! 😂\n" +
                "Spend enough time near someone and you can turn them into a walking bomb.\n" +
                "Detonate whenever you like. The bomb carrier takes some damage, but survives with 1 HP and is not thrown by the blast.\n" +
                "If you get caught in the blast, you die instantly. Get clear before you detonate.",

            [TextId.TutorialBombPlantedAsBomber] =
                "Bomb planted. Good job! 😂\n" +
                "Now get clear of the blast and detonate it.\n" +
                "You cannot detonate while your target is near the truck. We cannot have the truck getting scratched.",

            [TextId.TutorialBomberProximityWarnedAsVillager] =
                "Repossessing valuables efficiently sometimes requires help from another player.\n" +
                "But a Bomber can plant a bomb on someone after staying near them for a while.\n" +
                "Remember who you've been working beside, in case you later discover that you were turned into a bomb.",

            [TextId.TutorialSelfBombExplodedAsVillager] =
                "It looks like the Bomber planted a bomb on you.\n" +
                "The blast injured you, but the bomb itself cannot kill or throw its carrier.\n" +
                "The Bomber can turn someone into a bomb by staying near them for a while, then detonate them at any time.\n" +
                "The Bomber dies instantly if caught in that blast.\n" +
                "The Bomber can only have one bomb planted at a time.",

            [TextId.TutorialShamanRoleDrawn] =
                "You are the Shaman.\n" +
                "Stand still for a moment and hold your gaze steady to use spirit vision.\n" +
                "A haunting sets in when you are near an unreported body.\n" +
                "Be careful not to become the next corpse.",

            [TextId.TutorialShamanGhostSighted] =
                "Your spirit vision reacted.\n" +
                "That means there is an unreported body in the direction you were just facing.",

            [TextId.TutorialShamanTranceEntered] =
                "You heard a drip.\n" +
                "That means there is no unreported body in the direction you are facing.",

            [TextId.TutorialShamanStormEntered] =
                "A haunting has set in.\n" +
                "An unreported body is nearby. The stronger the haunting, the closer the body.",

            [TextId.TutorialEquipBlockedByOtherGrabber] =
                "You cannot put an item into your inventory while another player is holding it.\n" +
                "If you want to take it, hit them with a melee weapon to knock it loose, then pick it up.",

            [TextId.TutorialValuableRecordSuppressed] =
                "You spotted a valuable, but did not record it on the map.\n" +
                "While recording is off, the werewolf team does not record newly discovered valuables. If nobody else knows\n" +
                "about a valuable, you can destroy it without leaving a marker to show that something is missing.\n" +
                "Hold the report key whenever you want to turn recording on.",

            [TextId.ManualToggleLabelFormat] = "Manual [{0}]",
            [TextId.ManualPageFooterFormat] = "{0}　　{1} / {2}",
            [TextId.ManualNavHint] = "← →: Pages　　Shift + ← →: Sections　　{0} or [menu]: Close",
            [TextId.ManualSectionBasics] = "Basics",
            [TextId.ManualSectionExploration] = "Exploration and Combat",
            [TextId.ManualSectionMeeting] = "Meetings and Voting",
            [TextId.ManualSectionGauge] = "Valuable Loss Gauge",
            [TextId.ManualSectionVillager] = "Villager",
            [TextId.ManualSectionShaman] = "Shaman",
            [TextId.ManualSectionWerewolf] = "Werewolf",
            [TextId.ManualSectionBlackCat] = "Black Cat",
            [TextId.ManualSectionBomber] = "Bomber",
            [TextId.ManualSectionAfterDeath] = "After Death",

            [TextId.ManualWelcomeTitle] = "Welcome to REPO Werewolf",
            [TextId.ManualWelcomeBody] =
                "In REPO Werewolf, players are split into the villager and werewolf teams, each working behind the scenes to secure victory.\n" +
                "The villager team's goal is the same as in regular R.E.P.O.: collect valuables, complete every extraction, and send the truck on its way.\n" +
                "The werewolf team's goal is to sabotage those efforts without revealing their identities.\n" +
                "Who suspects whom, and who is working with whom?\n" +
                "Can you uncover the truth amid all that suspicion? Or can you convince everyone that a lie is the truth?\n" +
                "The moment you seize on an easy answer and turn on your own, the players themselves become the most dangerous monsters of all.",

            [TextId.ManualGameFlowTitle] = "How a Match Works",
            [TextId.ManualGameFlowBody] =
                "REPO Werewolf requires at least 3 players.\n" +
                "In the room settings, the host can choose the stage and level, starting items, player upgrades, and more.\n" +
                "Warning: starting a match from an existing save file overwrites that save with the match state, erasing its previous contents.\n" +
                "There is no shop or next level—each match takes place in a single level.\n" +
                "Once either team secures victory, the results are shown and everyone returns to the lobby.\n" +
                "Cosmetic Boxes do not appear during a match. Instead, you can earn tokens afterward by meeting certain conditions.",

            [TextId.ManualVillagerWinTitle] = "Villager Team Win Conditions",
            [TextId.ManualVillagerWinBody] =
                "The villager team has two win conditions. Satisfy either one to win the match.\n" +
                "・Complete every extraction and send the truck on its way with at least one villager still alive\n" +
                "・Eliminate the werewolf team (the villagers still win if the Black Cat survives)\n" +
                "The team of the player who starts the truck has no bearing on the result. What matters is whether any villagers are still alive when it departs.\n" +
                "In other words, completing the level as you normally would in R.E.P.O. also brings the villagers closer to victory.\n" +
                "The werewolf team will use every trick available to interfere, however, so this mode demands a different strategy from a normal run.",

            [TextId.ManualWerewolfWinTitle] = "Werewolf Team Win Conditions",
            [TextId.ManualWerewolfWinBody] =
                "The werewolf team has three win conditions. Satisfy any one to win the match.\n" +
                "・Make the quota impossible to reach even if every available valuable were collected (value checkmate)\n" +
                "・Keep the truck from departing until time runs out\n" +
                "・Eliminate the villager team\n" +
                "The moment value checkmate is reached, the werewolf team's victory is final.\n" +
                "Even after the final extraction is complete, the werewolf team wins if every villager dies before the truck departs.\n" +
                "This includes a Werewolf starting the truck and leaving every villager behind to die.",

            [TextId.ManualPeacefulVillageTitle] = "Peaceful Village",
            [TextId.ManualPeacefulVillageBody] =
                "Depending on the room settings, a match can turn out to be a \"peaceful village\" where nobody at all is assigned to the werewolf team.\n" +
                "Since there is no werewolf team to bring down in the first place, the only win condition is the same as regular R.E.P.O.: complete the extractions and send the truck on its way.\n" +
                "There is no way to confirm that you are in a peaceful village while the match is running. Role reveals, meeting minutes, the Black Cat's awakening notice, bomb warnings, and everything else you can see are identical to a normal village.\n" +
                "On the other hand, a beacon being used by the werewolf team, someone acting in a way only a werewolf could, or an explosion set off by the Bomber all prove that this is not a peaceful village.\n" +
                "Until such a moment arrives, you cannot tell a peaceful village—where everyone should drop their suspicions and cooperate—from a normal village where a werewolf is lying low to lull you into carelessness.\n" +
                "A peaceful village has a special loss condition on top of the villager team's usual ones: the moment the survivors drop to one or fewer, the peaceful village collapses and everyone loses.",

            [TextId.ManualValuablesMapTitle] = "Valuables and the Map",
            [TextId.ManualValuablesMapBody] =
                "When a player discovers a valuable or an enemy drops an orb, a yellow marker is added to the map.\n" +
                "Unlike regular R.E.P.O., these markers do not update in real time under the default settings.\n" +
                "They update only when an extraction is completed or a meeting starts.\n" +
                "If no valuable is at a marker, someone either carried it away or destroyed it.\n" +
                "The map cannot tell you which.\n" +
                "Careful villagers take inventory at every meeting and ask where each missing valuable went.",

            [TextId.ManualValuableRecordTitle] = "Valuable Recording Toggle (Werewolf Team)",
            [TextId.ManualValuableRecordBody] =
                "A marker is added the moment a player sees a valuable.\n" +
                "While recording is off, the werewolf team (Werewolves, Bombers, and awakened Black Cats) does not add valuables it discovers to the map.\n" +
                "If a member of the werewolf team reaches and destroys a valuable before anyone else finds it, the map shows no trace that it ever existed.\n" +
                "If a villager has already found and recorded it, however, the marker remains—alerting everyone that something is missing.",
            [TextId.ManualValuableRecordToggle] =
                "You can switch recording on and off at any time by holding the report key (the icon at the bottom right shows the current state).\n" +
                "Turn recording off only when attempting a stealthy break, then turn it back on as needed before exploring.",

            [TextId.ManualCombatTitle] = "PvP",
            [TextId.ManualCombatBody] =
                "Unlike in regular R.E.P.O., melee weapons can damage players in Werewolf Mode, consuming weapon energy in the process (just as they do in the Super Smash Bros.-style arena).\n" +
                "There is also no safeguard that lets you survive an otherwise fatal blow at low health.\n" +
                "Melee attacks can disarm players as well.\n" +
                "When struck by a melee weapon, you drop whatever you are holding and one item is knocked out of your inventory.\n" +
                "You also cannot place an item in your inventory while another player is holding it. To take it, you must knock it out of their hands first.\n" +
                "In other words, whoever lands the first hit has the advantage. Choose carefully whom you trust to watch your back.",

            [TextId.ManualEndgamePrepTitle] = "Preparing for the Endgame",
            [TextId.ManualEndgamePrepBody] =
                "Late in the match, when meetings can no longer settle the conflict, the game may shift into an endgame where the werewolf team and the villager team face off directly.\n" +
                "How well each side can fight in the endgame depends on the position it has built over the course of the match.\n" +
                "Villagers can gain an advantage by securing and confiscating dangerous weapons to keep them out of the werewolf team's hands, protecting trusted allies, and keeping healing supplies in reserve.\n" +
                "The werewolf team can build its advantage by destroying valuables to unlock perks, killing the most trusted villagers, and disrupting the villagers' coordination.\n" +
                "The final confrontation is where the advantages both teams have built throughout the match come into play.\n" +
                "Keep the endgame in mind and prepare for it from the beginning.",

            [TextId.ManualCorpseTitle] = "About Corpses",
            [TextId.ManualCorpseBody] =
                "In REPO Werewolf, dead players cannot be revived by any means.\n" +
                "Body locations are not shown on the map, either. You must find them with your own eyes.\n" +
                "Press the report key (shown at the bottom right of the screen) near an unreported body to call a meeting on the spot.",

            [TextId.ManualConveneTitle] = "Calling a Meeting",
            [TextId.ManualConveneBody] =
                "There are two ways to call a meeting.\n" +
                "・Grab and hold the red button at the back of the truck\n" +
                "・Press the report key near an unreported body\n" +
                "Each player can use the button to call a meeting once (this can be changed in the room settings).\n" +
                "The button is temporarily unavailable just after the match starts and after a meeting ends.\n" +
                "Once only one incomplete extraction point remains, meetings can no longer be called by reporting a body (for example, if the quota is 4 extraction points, reports are disabled after the 3rd is completed).\n" +
                "The button remains available until the very end.",

            [TextId.ManualMeetingFlowTitle] = "How Meetings Work",
            [TextId.ManualMeetingFlowBody] =
                "When a meeting starts, everyone is warped to the truck and immobilized.\n" +
                "During a meeting, enemies disappear and no new ones spawn, so you are safe.\n" +
                "Deaths since the previous meeting are announced first, followed by changes to the valuable loss gauge.\n" +
                "Voting begins afterward. During the meeting, you can open the full map to review the updated information on valuables.\n" +
                "The match time limit is paused during a meeting, so discussion does not eat into your remaining time.\n" +
                "Enemy respawn timers continue to count down, however, so an overly long meeting brings the enemies back sooner (the room settings control how fast).\n" +
                "Any enemy whose respawn timer has expired by the end of the meeting spawns immediately.",

            [TextId.ManualVotingTitle] = "Voting and Execution",
            [TextId.ManualVotingBody] =
                "Each surviving player casts one vote at a meeting. Skipping (voting for no one) is also an option.\n" +
                "Everyone can see who has finished voting, but not who they voted for.\n" +
                "The player with the most votes is executed. If there is a tie for the most votes, no one is executed.\n" +
                "Meetings have a time limit, and each vote cast reduces the remaining time slightly.",

            [TextId.ManualScatterTitle] = "Scattering After Meetings",
            [TextId.ManualScatterBody] =
                "Depending on the room settings, when a meeting ends with 6 or more survivors, they are randomly split into groups of three or more, and each group warps to a different location to set out again.\n" +
                "Destinations are either the truck or one of the completed extraction points, starting from the second one (the first extraction point is never a destination).\n" +
                "The groupings are announced to everyone, but which group goes where is not revealed. The previous groupings are reposted in the meeting chat log at the start of the next meeting.\n" +
                "Until the second extraction point has been completed, after every extraction is complete, or when fewer than 6 players are alive, no one scatters and everyone sets out from the truck.\n" +
                "If someone dies right after scattering while \"Handover in progress…\" is shown at the top of the screen, an emergency meeting is called automatically as a \"handover incident\".\n" +
                "This meeting starts instantly with no warning countdown and does not consume anyone's meeting call.",

            [TextId.ManualGaugeBasicsTitle] = "Reading the Valuable Loss Gauge",
            [TextId.ManualGaugeIntro] =
                "The valuable loss gauge tracks the struggle over valuables between the villager and werewolf teams. Its baseline is the total value of all valuables on the map when the match starts.",
            [TextId.ManualGaugeLoss] =
                "Yellow bar (grows from the left)… total value lost through damage",
            [TextId.ManualGaugeDelivery] =
                "Cyan bar (grows from the right)… total value extracted",
            [TextId.ManualGaugeLines] =
                "・Blue line… the value required to meet the quota. Once the cyan bar reaches this line, the truck can depart\n" +
                "・Red line… the value checkmate threshold, where the quota can no longer be met even if every available valuable is collected. The moment the yellow bar reaches this line, the werewolf team wins\n" +
                "When an enemy drops an orb or new valuables appear on the map, the red line moves to the right, giving the villagers more leeway. This cannot overturn a checkmate victory once it is locked in.",

            [TextId.ManualRoleVillagerTitle] = "Role: Villager",
            [TextId.ManualRoleVillagerBody] =
                "Team: Villagers\n" +
                "Villagers have no special abilities. Play as you would in regular R.E.P.O.: collect valuables and complete the extractions.\n" +
                "Your greatest weapons are observation and discussion. Compare notes on the valuables map, any bodies found, and what other players have done, then expose the Werewolves during meetings.",

            [TextId.ManualRoleWerewolfTitle] = "Role: Werewolf",
            [TextId.ManualRoleWerewolfIntro] =
                "Team: Werewolves\n" +
                "The Werewolf role is the core of the werewolf team, and at least one player is always assigned this role.\n" +
                "Werewolves grow stronger as valuables are damaged.\n" +
                "Regardless of who caused the damage, perks unlock in sequence as the total value lost reaches each threshold.",
            [TextId.ManualRoleWerewolfEnemyMap] =
                "Only players with the Werewolf role can see enemy positions on the map.",
            [TextId.ManualRoleWerewolfPerksTitle] = "Role: Werewolf — Perks (1/2)",
            [TextId.ManualRoleWerewolfPerkStamina] =
                "Infinite Stamina… dashing and ledge-grabbing no longer drain stamina",
            [TextId.ManualRoleWerewolfPerkJump] =
                "Extra Jumps… jump multiple times in midair (the number depends on the room settings)",
            [TextId.ManualRoleWerewolfPerksTitle2] = "Role: Werewolf — Perks (2/2)",
            [TextId.ManualRoleWerewolfPerkEnemyIgnore] =
                "Monster Camouflage… most enemies stop targeting you (they still react to sound)",
            [TextId.ManualRoleWerewolfPerkHeal] =
                "Regeneration… while Wolf Mode is on, your health slowly recovers over time (no healing effect is shown, but the health gauge on your back is visible to other players)",
            [TextId.ManualRoleWerewolfPerkToggle] =
                "Use the Wolf Mode key to turn perks on or off. Be careful—a villager who sees you use one will know what you are.",

            [TextId.ManualRoleWerewolfBeaconTitle] = "Role: Werewolf — Beacon",
            [TextId.ManualRoleWerewolfBeaconBody] =
                "The Beacon is a special ability of the Werewolf role. You gain one use each time valuables lose a certain total amount of value.\n" +
                "When activated, it emits a sound that players cannot hear and draws enemies from across the map to its location. It also brings back monsters that have been killed and despawned, subject to a revival cooldown shared by the entire werewolf team.\n" +
                "Use it to arrange an \"accident\" for an isolated player or send enemies after a group of villagers to cause panic.\n" +
                "However, this may create more available orbs and help the villagers reach their quota. You also risk being caught in the ensuing fight.\n" +
                "Using the Beacon is not announced immediately. At the next meeting, however, everyone is told how many times it has been used since the previous meeting.",

            [TextId.ManualRoleBlackCatTitle] = "Role: Black Cat",
            [TextId.ManualRoleBlackCatIntro] =
                "Team: Werewolves (win/loss only)\n" +
                "Base role: Villager\n" +
                "Depending on the room settings, the Black Cat is assigned from among the Villagers.\n" +
                "At the start of the match, even the Black Cat is told they are a Villager. They awaken to their true identity after a short delay.\n" +
                "Otherwise, they have the same abilities as a Villager. If Black Cat's Revenge is enabled in the room settings, being executed at a meeting lets them choose one of their voters to take down with them. If they choose no one, a target is selected at random.\n" +
                "The Werewolves do not know who the Black Cat is, and initially the Black Cat does not know who the Werewolves are.",
            [TextId.ManualBlackCatInformantTitle] = "Role: Black Cat — Informant",
            [TextId.ManualRoleBlackCatInformant] =
                "Informant… once valuables lose a certain total amount of value, the members of the werewolf team are revealed to the Black Cat alone.",
            [TextId.ManualRoleBlackCatGaugeNote] =
                "The Black Cat can also see the valuable loss gauge, but it updates at intervals rather than in real time.",

            [TextId.ManualBlackCatCounterTitle] = "Dealing with the Black Cat",
            [TextId.ManualBlackCatCounterBody] =
                "When enabled in the room settings, the Black Cat's Revenge ability triggers only when they are executed by vote.\n" +
                "Do not rush to execute someone you suspect is the Black Cat. If you are right, one of the players who voted for them will be taken down too.\n" +
                "If an execution is unavoidable, only players prepared to be taken down should cast a vote.\n" +
                "The ability does not trigger if the Black Cat dies another way, such as to a weapon or a fall, but using force may ignite a fight.\n" +
                "If you kill someone without the group's agreement, the other villagers have no way to know whether you had good reason.\n" +
                "If your suspicion falls short of an execution, confiscating that player's weapons and watching them is another option. Without a weapon, even the werewolf team cannot easily kill anyone.",

            [TextId.ManualRoleBomberTitle] = "Role: Bomber",
            [TextId.ManualRoleBomberIntro] =
                "Team: Werewolves\n" +
                "Base role: Werewolf\n" +
                "Depending on the room settings, the Bomber is assigned from among the Werewolves.\n" +
                "The Bomber can turn a nearby player into a bomb after spending enough time near them, then detonate the bomb at any time.",
            [TextId.ManualBomberPlantTitle] = "Role: Bomber — Planting a Bomb",
            [TextId.ManualRoleBomberPlant] =
                "Plant Bomb is on cooldown at the start of the match and after each meeting. Once the cooldown ends, staying near another player gradually fills a yellow meter for that player. The meter does not fill through walls and drains when you move away.\n" +
                "When the meter is full and turns green, press the Plant Bomb key shown in the bottom-left corner of the screen to turn that player into a bomb.\n" +
                "A bomb icon visible only to the Bomber marks the affected player. They do not know they have been turned into a bomb.\n" +
                "The Bomber cannot turn themselves into a bomb, but can target a Werewolf or the Black Cat.\n" +
                "Only one player can be a bomb at a time. Before detonating it, you can reassign the bomb to another player.",

            [TextId.ManualRoleBomberDetonateTitle] = "Role: Bomber — Detonation",
            [TextId.ManualRoleBomberDetonateBody] =
                "Once the Detonate cooldown ends, press the key to trigger an explosion centered on the player you turned into a bomb.\n" +
                "The blast hits nearby players, valuables, and enemies. The bomb carrier also takes some damage, but their health never drops below 1 and they are not thrown.\n" +
                "If the Bomber is caught in the blast, they die instantly.\n" +
                "You cannot detonate while the target is near the truck.\n" +
                "Detonating after the target has died produces a dud, and the bomb is lost.\n" +
                "Your supply of bombs is replenished whenever valuables lose a certain total amount of value.",

            [TextId.ManualRoleShamanTitle] = "Role: Shaman",
            [TextId.ManualRoleShamanIntro] =
                "Team: Villagers\n" +
                "Base role: Villager\n" +
                "Depending on the room settings, the Shaman is assigned from among the Villagers.\n" +
                "The Shaman can sense unreported bodies—those not yet revealed at a meeting.\n" +
                "They can infer the direction of a body and roughly when the player died.\n" +
                "Spirit vision requires standing still, leaving you vulnerable, and a haunting obscures your view when it sets in.\n" +
                "Be careful not to end up as the next body.",
            [TextId.ManualShamanSenseTitle] = "Role: Shaman — Spirit Vision and Haunting",
            [TextId.ManualShamanGhost] =
                "Spirit vision lets you sense the direction of a distant body.\n" +
                "Stand still and hold your gaze on one point. Your vision fades as spirit vision begins.\n" +
                "A dripping sound heard at regular intervals during spirit vision means there is no unreported body in that direction.\n" +
                "Keep looking toward an unreported body for several seconds, and your screen becomes heavily distorted for a moment.\n" +
                "Walls and distance do not matter. After the screen distorts, spirit vision will not react again until its cooldown ends. It is also unavailable briefly after the match starts.\n" +
                "If several unreported bodies exist, only the one nearest to you counts. Spirit vision will not react to any body farther away.\n" +
                "Moving or turning your gaze too far interrupts spirit vision. You cannot use it while a body is close enough to cause a haunting.\n" +
                "Your vision remains faded only while spirit vision is active.\n" +
                "Bodies already announced at a meeting produce no response.",
            [TextId.ManualShamanStorm] =
                "A haunting lets you sense how close a nearby body is.\n" +
                "Near an unreported body, the haunting changes with distance across three levels: weak, medium, and strong.\n" +
                "Once only the final extraction point remains, the visual distortion disappears and only the sound remains.\n" +
                "Bodies already announced at a meeting produce no response.",

            [TextId.ManualAfterDeathTitle] = "When You Die",
            [TextId.ManualAfterDeathBody] =
                "When you die, you enter spectator mode and watch the rest of the match play out.\n" +
                "The dead can talk to one another, but there is no way to pass information to the living—no Death Head tricks like in regular R.E.P.O.\n" +
                "Depending on the room settings, the werewolf team may hear the voices of the dead with an echo effect.\n" +
                "Victory and defeat are shared by the team, so you still win if your team wins, even after you die.",

            [TextId.ScenarioSelectTitle] = "REPO Werewolf Tutorial",
            [TextId.ScenarioNameMeeting] = "Meetings & Voting",
            [TextId.ScenarioDescMeeting] = "Call a meeting, review the testimony, and vote to execute the Werewolf",
            [TextId.ScenarioNameWolf] = "Werewolf Abilities",
            [TextId.ScenarioDescWolf] = "Destroy valuables, unlock your abilities, and make the quota impossible",

            [TextId.ScenarioNameShaman] = "Spirit Vision & Body Reports",
            [TextId.ScenarioDescShaman] = "Use spirit vision to find and report a body, then experience the endgame restriction on body reports",
            [TextId.ScenarioNameBomber] = "Planting & Detonation",
            [TextId.ScenarioDescBomber] = "Plant a bomb and learn about the truck-zone lockout and safe blast distance",
            [TextId.ScenarioLocked] = "Coming soon",
            [TextId.ScenarioCompleted] = "Completed",
            [TextId.ScenarioBack] = "Back",
            [TextId.ScenarioReturnToSelect] = "Back to tutorials",
            [TextId.ScenarioReturnToSelectWithKeyFormat] = "Back to tutorials [{0}]",
            [TextId.ScenarioAbortConfirm] = "Quit the tutorial and return to the title screen?",
            [TextId.ScenarioHudBadge] = "TUTORIAL (no time limit)",
            [TextId.ScenarioAdvanceHint] = "[interact] to continue",
            [TextId.ScenarioAbortHint] = "[menu] to quit",
            [TextId.ScenarioAbortCancelHint] = "[interact] to go back",
            [TextId.ScenarioBrokenNotice] = "Something went wrong. Come back and start over.",
            [TextId.ScenarioNeedOpenChatLogNag] = "Open the meeting log. The meeting minutes and all chat messages are recorded there.",
            [TextId.ScenarioNeedCloseMapNag] = "Close the full map before you vote.",
            [TextId.ScenarioNeedOpenMapNag] = "Open the full map. The button is at the bottom left.",
            [TextId.ScenarioNeedCloseMapToolNag] = "Close your handheld map[[map]] before you open the full map.",
            [TextId.ScenarioNeedOpenMapToolNag] = "Take out your handheld map[[map]].",
            [TextId.ScenarioNeedShamanTranceNag] = "Stand still and hold your gaze steady. Do not look around.",
            [TextId.ScenarioNeedShamanSenseReadyNag] = "Spirit vision is on cooldown right now. Wait a moment.",
            [TextId.ScenarioNeedShamanStormNag] = "Hunt the body[[deathhead]] down with spirit vision.",
            [TextId.ScenarioNeedCorpseNearNag] = "Get close to the body[[deathhead]].",
            [TextId.ScenarioNeedBombTargetNearNag] = "Too far away. Get back to No.{0}. Stand close with no wall between you, and the gauge over their head will start climbing again.",
            [TextId.ScenarioNeedBombOnTargetNag] = "The bomb is on the wrong person. Get close to No.{0}, fill the gauge over their head and press [plantkey] to move it onto them.",
            [TextId.ScenarioBlastDenyTooClose1] = "Too close. A Bomber caught in their own blast dies on the spot.",
            [TextId.ScenarioBlastDenyTooClose2] = "I blocked that detonation. Back off from No.{0} until their proximity gauge stops climbing, then press it again.",
            [TextId.ScenarioBlastDenyTooFar1] = "Too far. This time you are the one who has to take the blast.",
            [TextId.ScenarioBlastDenyTooFar2] = "I blocked that detonation. Get right up against No.{0}, then press it again.",
            [TextId.ScenarioBlastDenyAwaitTruck1] = "Not yet. No.{0} is still walking to the truck.",
            [TextId.ScenarioBlastDenyAwaitTruck2] = "I blocked that detonation. Wait until No.{0} reaches the truck, then press it again.",
            [TextId.ScenarioBlastDenyMoving1] = "Hold on. No.{0} is still on the move.",
            [TextId.ScenarioBlastDenyMoving2] = "I blocked that detonation. Wait until they stop, judge the distance, then press it again.",
            [TextId.ScenarioBlastDenyGeneric1] = "You cannot detonate right now.",

            [TextId.ScenarioMeetingLeashNag] = "Do not leave your post. [[:/]] I am sending you back to the truck.",

            [TextId.ScenarioDecodeReceiving] = "RECEIVING TAXMAN SIGNAL...",
            [TextId.ScenarioDecodeInitializing] = "INITIALIZING SEMANTIC DECODER...",
            [TextId.ScenarioDecodeDone] = "DONE",

            [TextId.ScenarioMeetingIntro1] = "For a change of pace, you are going to play Werewolf.",
            [TextId.ScenarioMeetingIntro2] = "One of your coworkers is working beside you as if nothing is wrong, while secretly plotting to make the mission fail...",
            [TextId.ScenarioMeetingIntro3] = "As a Villager, executing that Werewolf is part of your job.",
            [TextId.ScenarioMeetingIntro4] = "And do not forget your usual job: hauling valuables. [[;)]]",
            [TextId.ScenarioMeetingIntro5] = "This tutorial only covers meetings and voting.",
            [TextId.ScenarioMeetingIntro6] = "For practice, there are no monsters here and the time limit is disabled. A real run has both.",

            [TextId.ScenarioMeetingConvene1] = "Hold down the meeting button at the back of the truck[[truck]].",
            [TextId.ScenarioMeetingConvene2] = "Call a meeting and everyone is pulled back to the truck.",
            [TextId.ScenarioMeetingConveneNag1] = "Hello! Are you listening? [[disappointed]]",
            [TextId.ScenarioMeetingConveneNag2Format] = "Press and hold {0} on the red meeting button at the back of the truck[[truck]].",
            [TextId.ScenarioMeetingConveneNag3Grab] = "The same key you use to grab things.",
            [TextId.ScenarioMeetingConveneNag3Interact] = "The same key you use to interact with things.",
            [TextId.ScenarioMeetingConveneEarly1] = "Oh. You already knew that one. [[:)]]",
            [TextId.ScenarioMeetingConveneEarly2] = "Good, that saves time. I will skip the button lesson.",

            [TextId.ScenarioMeetingOpened1] = "The meeting has started. There are three things to check here.",
            [TextId.ScenarioMeetingOpened2] = "The dead[[deathhead]], the value lost and extracted[[extraction]], and the full map.",
            [TextId.ScenarioMeetingOpened3] = "The meeting log records text chat and also keeps the minutes from each meeting.",
            [TextId.ScenarioMeetingOpened4] = "The top line of the minutes tells you the meeting number.",
            [TextId.ScenarioMeetingOpened5] = "Next, who died[[deathhead]].",
            [TextId.ScenarioMeetingOpened6] = "How much value has been lost to destroyed valuables since the previous meeting.",
            [TextId.ScenarioMeetingOpened7] = "How much value has been extracted[[$$$]], and the total quota[[extraction]] required for the truck to leave[[truck]][[arrowright]][[road]].",
            [TextId.ScenarioMeetingOpened8] = "How many times the beacon was used since the last meeting.",
            [TextId.ScenarioMeetingOpened9] = "If groups were assigned at the previous meeting, those assignments[[semibots]] also appear on a separate line. There were none this time.",

            [TextId.ScenarioMeetingClock1] = "In a real match the meeting itself is on a clock. The match timer[[clock]], though, stops while the meeting runs.",
            [TextId.ScenarioMeetingClock2] = "The monsters[[headman]][[clown]] vanish as well, and none show up while a meeting is on.",
            [TextId.ScenarioMeetingClock3] = "But the respawn wait[[headman]][[clown]], and the way the respawn interval shortens over time, do not stop completely. They keep running at the rate the room settings decide.",
            [TextId.ScenarioMeetingClock4] = "Any monster[[headman]][[clown]] that is ready to respawn when the meeting ends shows up immediately, with no further wait.",

            [TextId.ScenarioMeetingGauge1] = "That band across the top of the screen shows the status of the valuables.",
            [TextId.ScenarioMeetingGauge2] = "The upper yellow bar shows the total value destroyed so far. At each meeting, it shows how much the total has increased.",
            [TextId.ScenarioMeetingGauge3] = "The row under it fills in cyan from the right as you extract valuables[[extraction]]. You have extracted nothing yet, so it is empty.",
            [TextId.ScenarioMeetingGauge4] = "The blue vertical line is the quota. Get the cyan bar to that line and the truck can leave.",
            [TextId.ScenarioMeetingGauge5] = "If the yellow bar reaches the red line, extracting everything left still misses the quota, and the Werewolf wins on the spot.",

            [TextId.ScenarioMeetingMap1] = "This is the full map. You can only open it during a meeting.",
            [TextId.ScenarioMeetingMap2] = "Every valuable that anyone has found appears here. The map only refreshes at the start of a meeting and when an extraction is completed[[extraction]].",
            [TextId.ScenarioMeetingMap3] = "If a valuable on the map is not there any more, someone carried it off or broke it.",
            [TextId.ScenarioMeetingMap4] = "The grid uses coordinates like A1. Say \"I saw a body in C5\" and everyone will know exactly which room you mean.",
            [TextId.ScenarioMeetingMap5] = "When you are done looking, close it with the same button.",

            [TextId.ScenarioMeetingChatUnavailable1] = "This is a tutorial, so you cannot use text chat. [[pleading]]",
            [TextId.ScenarioMeetingChatUnavailable2] = "In a real multiplayer run, you can speak using the usual REPO chat key ([chat]), and anything said during a meeting remains in this log[[message]].",

            [TextId.ScenarioMeetingVote1] = "The discussion has progressed, and No.3 appears to be under suspicion.",
            [TextId.ScenarioMeetingVote2] = "Vote however you like.",
            [TextId.ScenarioMeetingVote3] = "Press the same target again to confirm. You can change it before confirming.",

            [TextId.ScenarioMeetingTally] = "No.3 received more votes than anyone else, so No.3 will be executed. [[scared]]",
            [TextId.ScenarioMeetingResult1] = "The executed No.3 was the Werewolf. Good job! [[creepycrying]]",
            [TextId.ScenarioMeetingResult2] = "Every Werewolf is dead, so the Villagers win.",

            [TextId.ScenarioMeetingPostgame1] = "This is the results screen.",
            [TextId.ScenarioMeetingPostgame2] = "In multiplayer, this is where everyone talks the match over.",
            [TextId.ScenarioMeetingReplay1] = "The whole match was recorded.",
            [TextId.ScenarioMeetingReplay2] = "Open the recording to replay the discussion you just had.",
            [TextId.ScenarioMeetingPostgame3] = "That is the whole meeting tutorial. [[wave]]",

            [TextId.ScenarioMeetingLine1] = "I saw No.3 break something valuable.",
            [TextId.ScenarioMeetingLine2] = "That is a misunderstanding. I did not do it on purpose—I really did just drop it.",
            [TextId.ScenarioMeetingLine3] = "What were you carrying, anyway?",
            [TextId.ScenarioMeetingLine4] = "A chainsaw.",
            [TextId.ScenarioMeetingLine5] = "I was afraid the blade would hit me, so I let go to reposition my grip. It broke when I dropped it.",
            [TextId.ScenarioMeetingLine6] = "It really was just a mistake.",
            [TextId.ScenarioMeetingLine7] = "Then who do you think is suspicious?",
            [TextId.ScenarioMeetingLine8] = "I have no idea, but No.2, the one accusing me, looks suspicious.",
            [TextId.ScenarioMeetingLine9] = "Hold on. If No.3 had admitted to breaking it, that would be one thing...",
            [TextId.ScenarioMeetingLine10] = "They stayed quiet, so I had to explain it for them.",
            [TextId.ScenarioMeetingLine11] = "I did not think anyone had seen me. I did not want to explain it and make things complicated by drawing suspicion.",
            [TextId.ScenarioMeetingLine12] = "If speaking up would only make me look suspicious, is it not better to stay quiet and focus on finishing the job?",
            [TextId.ScenarioMeetingLine13] = "No.3 looks suspicious to me.",
            [TextId.ScenarioMeetingLine14] = "We should vote No.3.",

            [TextId.ScenarioWolfIntro1] = "This time, you will play as a Werewolf. [[;)]]",
            [TextId.ScenarioWolfIntro2] = "A Werewolf's goal is to destroy the valuables and make the run fail without the Villagers realizing you did it. [[hush]]",
            [TextId.ScenarioWolfIntro3] = "There are monsters[[headman]][[clown]] here. Their attacks will hurt, but this is practice, so you cannot die. There is no time limit either. In a real run, Werewolves can die like anyone else, and time can run out.",

            [TextId.ScenarioWolfGauge1] = "The gauge at the bottom right shows the total value of all valuables at the start of the run and the total value destroyed so far. Only Werewolves can see it.",
            [TextId.ScenarioWolfGauge2] = "Fill it to the red line and the Villagers can no longer reach the quota, even by hauling everything left. The Werewolves win the instant that happens.",
            [TextId.ScenarioWolfGauge3] = "In a real run at this level, the red line would be much farther away. The quota is set high here, so destroying about half the valuables will reach it.",

            [TextId.ScenarioWolfDestroy1] = "Start by destroying one valuable.",
            [TextId.ScenarioWolfDestroy2] = "You can break it by slamming it into a wall, dropping it from somewhere high, or stomping on it.",
            [TextId.ScenarioWolfDestroyNag1] = "Found a valuable yet? [[eyesleft]]",
            [TextId.ScenarioWolfDestroyNag2] = "Grab a valuable and slam it hard into a wall or the floor, or stomp on it.",
            [TextId.ScenarioWolfEarly1] = "Eager, aren't you? You already know how to break things. [[shades]]",
            [TextId.ScenarioWolfEarly2] = "Then I will skip that part.",

            [TextId.ScenarioWolfMarker1] = "The marker for the valuable you just destroyed is still there.",
            [TextId.ScenarioWolfMarker2] = "The moment you laid eyes on it, its location was recorded on everyone's map.",
            [TextId.ScenarioWolfMarker3] = "The marker remains even after the valuable is destroyed. The map alone cannot tell you whether it was carried away or destroyed.",
            [TextId.ScenarioWolfMarker4] = "But anyone who visits the marker will see that the valuable is gone. If they know you were nearby, they will suspect you.",
            [TextId.ScenarioWolfMarker5] = "There is also a way to avoid the marker in the first place.",

            [TextId.ScenarioWolfRecord1] = "Look at the recording icon at the bottom right.",
            [TextId.ScenarioWolfRecord2] = "Hold the report key [reportkey] to turn valuable recording off.",
            [TextId.ScenarioWolfRecord3] = "While it is off, the valuables you find never appear on anyone's map.",
            [TextId.ScenarioWolfRecordNag1] = "Recording is still on. Press and hold the report key [reportkey].",

            [TextId.ScenarioWolfStealth1] = "Now go destroy a valuable nobody has found yet.",
            [TextId.ScenarioWolfStealth2] = "Breaking one the Villagers already found still leaves its marker. Go for untouched ones.",
            [TextId.ScenarioWolfStealthNag1] = "Recording is off. Now all you have to do is break one nobody has recorded yet.",
            [TextId.ScenarioWolfStealth3] = "No marker this time.",
            [TextId.ScenarioWolfStealth4] = "The total value destroyed is still revealed at the next meeting. Only the location stays hidden.",

            [TextId.ScenarioWolfKeep1] = "Keep breaking them.",
            [TextId.ScenarioWolfKeep2] = "Every time a certain percentage of the valuables is destroyed, the Werewolf grows stronger.",
            [TextId.ScenarioWolfPush1] = "Keep destroying valuables. Fill the gauge to unlock the next perk.",
            [TextId.ScenarioWolfPush2] = "You still have another perk to unlock. Go find more valuables and keep breaking them.",

            [TextId.ScenarioWolfForm1] = "Look at the bottom left. One perk has been unlocked.",
            [TextId.ScenarioWolfForm2] = "There are four perks: unlimited stamina, extra jumps, being ignored by most monsters, and natural healing.",
            [TextId.ScenarioWolfForm3] = "Unlocking a perk does not activate it. Perks only take effect while Wolf Form is on.",
            [TextId.ScenarioWolfForm4] = "The unlock thresholds are a room setting. Here they are set low so you see them early.",
            [TextId.ScenarioWolfFormOn1] = "Press the Wolf Form key [wolfkey] and turn it on.",
            [TextId.ScenarioWolfFormOn2] = "Leave it on in front of the Villagers and the way you move will give you away. [[hush]]",
            [TextId.ScenarioWolfFormOnNag1] = "The Wolf Form key is [wolfkey].",
            [TextId.ScenarioWolfFormDone1] = "Good. You can run without draining your stamina now. Every perk you have unlocked is active.",
            [TextId.ScenarioWolfFormDone2] = "Press it again to turn it off. Keep it off whenever you are moving around with the Villagers. [[hush]]",

            [TextId.ScenarioWolfEnemyMap1] = "Your map shows the positions of nearby monsters[[headman]][[clown]].",
            [TextId.ScenarioWolfEnemyMap2] = "Only Werewolves can see them, even when the monsters[[headman]][[clown]] are in rooms nobody has explored yet.",
            [TextId.ScenarioWolfEnemyMap3] = "Move like you already know where they are and people will notice. Keep it natural. [[hush]]",

            [TextId.ScenarioWolfBeacon1] = "Look at the beacon slot at the bottom left. Each time you destroy a certain percentage of the valuables, you gain a charge.",
            [TextId.ScenarioWolfBeacon2] = "Press the beacon key [beaconkey] to lure monsters[[headman]][[clown]] to your position. It brings back monsters that were killed, too.",
            [TextId.ScenarioWolfBeacon3] = "I have added one charge for you. Give it a try.",
            [TextId.ScenarioWolfBeaconNag1] = "You should have a charge. Just press the beacon key [beaconkey].",
            [TextId.ScenarioWolfBeacon4] = "Keep watching the map. Any wandering monsters[[headman]][[clown]] should start heading your way.",
            [TextId.ScenarioWolfBeacon5] = "The Villagers are not notified when you use it. Only the number of uses is revealed at the next meeting.",
            [TextId.ScenarioWolfBeacon6] = "In a real run, try using it where the Villagers are.",

            [TextId.ScenarioWolfIgnore1] = "You and the monsters[[headman]][[clown]] are on the same side now. While Wolf Form is on, most of them will not come after you. One you have provoked still might.",
            [TextId.ScenarioWolfIgnore2] = "Do not let the Villagers catch you walking alongside a monster as if it were friendly. [[hush]]",

            [TextId.ScenarioWolfHeal1] = "Natural healing is unlocked. While Wolf Form is on, your health creeps back.",
            [TextId.ScenarioWolfHeal2] = "Your health bar is visible to everyone. If your missing health recovers on its own, you will look suspicious.",

            [TextId.ScenarioWolfOrb1] = "The orbs monsters drop are valuables too. Each one adds to what the Villagers can still haul, pushing the red line away.",
            [TextId.ScenarioWolfOrb2] = "Destroy an orb and its value counts as a loss, advancing your perk unlocks. If you kill something, destroy what it drops.",

            [TextId.ScenarioWolfFinish1] = "Now keep destroying valuables until the gauge reaches the red line.",
            [TextId.ScenarioWolfFinish2] = "Have you turned Wolf Form on? It makes searching a good deal easier.",
            [TextId.ScenarioWolfFinish3] = "Comb the map for valuables and break every one you find.",

            [TextId.ScenarioWolfResult1] = "What can still be recovered has fallen below the quota. The Villagers can no longer complete the run. [[dry]]",
            [TextId.ScenarioWolfResult2] = "The Werewolf team wins by making the quota impossible.",
            [TextId.ScenarioWolfPostgame1] = "The results screen. Everyone's role is revealed here.",
            [TextId.ScenarioWolfPostgame2] = "In multiplayer you can talk the match over here together.",
            [TextId.ScenarioWolfReplay1] = "This tutorial was recorded in full as well.",
            [TextId.ScenarioWolfReplay2] = "It even shows where you destroyed each valuable.",
            [TextId.ScenarioWolfWrap1] = "Mind you, breaking this much in a real run is hard. The Villagers are watching. [[eyesleft]]",
            [TextId.ScenarioWolfWrap2] = "It is still worth doing. It unlocks your perks and makes the Villagers' quota harder to reach. Whether you aim to wipe them out or run down the clock, every valuable you destroy brings you closer to victory.",
            [TextId.ScenarioWolfPostgame3] = "That is the end of the Werewolf tutorial. [[wave]]",

            [TextId.ScenarioWolfExtractionNag1] = "Hey. You are the Werewolf.",
            [TextId.ScenarioWolfExtractionNag2] = "What are you doing completing an extraction? [[disappointed]] Break the valuables.",
            [TextId.ScenarioWolfExtractionBreak1] = "Always have to do the opposite, do you? [[:/]]",
            [TextId.ScenarioWolfExtractionBreak2] = "The quota has been met, so a checkmate win is off the table. [[disappointed2]]",
            [TextId.ScenarioWolfExtractionBreak3] = "Come back and start over.",

            [TextId.ScenarioShamanIntro1] = "This time, you will play as the Shaman.",
            [TextId.ScenarioShamanIntro2] = "A Shaman can sense bodies[[deathhead]] that have not yet been reported at a meeting. [[ghost]]",
            [TextId.ScenarioShamanIntro3] = "Once a player's death is announced at a meeting, you can no longer sense their body[[deathhead]].",
            [TextId.ScenarioShamanIntro4] = "For practice, there are no monsters[[headman]][[clown]] here, you cannot die, and the time limit[[clock]] is off.",
            [TextId.ScenarioShamanIntro5] = "I also gave you plenty of extra jumps, so you can climb out if you fall into a pit. A real run is different on every count.",

            [TextId.ScenarioShamanTrance1] = "First, let me show you how to enter spirit vision. Stand still and keep staring at one point. [[realistic_eye]]",
            [TextId.ScenarioShamanTrance2] = "After a moment, the screen loses its color. Spirit vision only works while the color is gone.",
            [TextId.ScenarioShamanTrance3] = "Do not look around. If you turn, stop again. The moment your gaze moves, the timer starts over.",

            [TextId.ScenarioShamanTranceOn1] = "The color has drained from your view now.",
            [TextId.ScenarioShamanTranceOn2] = "This is spirit vision. When the color comes back, spirit vision has stopped.",

            [TextId.ScenarioShamanDrip1] = "If you keep staring until you hear a drop of water, you have completed the scan without finding a body[[deathhead]] in that direction.",
            [TextId.ScenarioShamanDrip2] = "It means either that there are no bodies[[deathhead]] yet, or that none lies in the direction you are facing.",

            [TextId.ScenarioShamanDowse1] = "Now go find it. Turn, stop, stare. Repeat.",
            [TextId.ScenarioShamanDowse2] = "Use spirit vision while facing a body[[deathhead]] and your view will distort. It works through walls and at any distance.",

            [TextId.ScenarioShamanDowseNag1] = "Every time you turn, stop. Nothing happens while your view is moving.",
            [TextId.ScenarioShamanDowseNag2] = "If your view stays distorted, the body[[deathhead]] is already close. Search for it on foot.",

            [TextId.ScenarioShamanBurst1] = "Spirit vision reacted. There is a body[[deathhead]] in that direction. Go and find it.",
            [TextId.ScenarioShamanBurst2] = "If you lose your bearings on the way, use spirit vision again to narrow down the direction.",
            [TextId.ScenarioShamanBurst3] = "But once it reacts, spirit vision goes quiet for a while. If the color does not drain, you are only waiting it out.",

            [TextId.ScenarioShamanStorm1] = "A haunting has begun. That means a body[[deathhead]] is nearby. It grows stronger through three levels as you get closer.",
            [TextId.ScenarioShamanStorm2] = "You cannot use spirit vision while a haunting is active. Spirit vision tells you the direction; a haunting tells you the distance.",
            [TextId.ScenarioShamanStorm3] = "Now find where the haunting is strongest. The body[[deathhead]] is right there.",

            [TextId.ScenarioShamanReport1] = "When you find the body[[deathhead]], get close to it and press [reportkey]. You can press it whenever the report icon at the bottom right lights up.",
            [TextId.ScenarioShamanReport2] = "Reporting a body calls a meeting, and you do not need a meeting call to do it.",

            [TextId.ScenarioShamanReportNag1] = "Get close to the body[[deathhead]] and press [reportkey]. Move close enough for the bottom-right icon to light up. The body[[deathhead]] might be in a high place.",

            [TextId.ScenarioShamanCorpseHint1] = "You are having trouble finding the body[[deathhead]]. [[:)]]",
            [TextId.ScenarioShamanCorpseHint2] = "Since this is a tutorial, just this once I will show you where the body[[deathhead]] is. [[;)]][[thumbsup]]",

            [TextId.ScenarioShamanEarly1] = "Looks like you already know how to report. [[kiss]]",
            [TextId.ScenarioShamanEarly2] = "Good, that saves time. I will skip the report lesson.",

            [TextId.ScenarioShamanOpened1] = "Your report called a meeting, where No.7 was announced dead.",
            [TextId.ScenarioShamanOpened2] = "Once a body[[deathhead]] has been announced, you can no longer detect it. No.7's body will no longer trigger a haunting[[ghost]].",
            [TextId.ScenarioShamanOpened3] = "In a real run, this is where you would explain the circumstances in which you found the body[[deathhead]]. This is a tutorial, so just listen to the discussion.",

            [TextId.ScenarioShamanVote1] = "Time is up with nothing decisive. Vote however you like.",
            [TextId.ScenarioShamanVote2] = "Nobody can tell whether this was an accident or a murder. Most will probably vote to skip.",

            [TextId.ScenarioShamanTally1] = "Skip received the most votes, so nobody was executed. No.3 survives on the condition that everyone keeps an eye on them.",
            [TextId.ScenarioShamanTally2] = "The meeting is over. Next you will see the endgame.",

            [TextId.ScenarioShamanDeliver1] = "I put a money bag[[$$$]] in front of you. Carry it to the extraction point[[extraction]] near the truck and finish the extraction.",

            [TextId.ScenarioShamanDeliverNag1] = "Where did the bag[[$$$]] go? Carry it to the extraction point[[extraction]] near the truck. If you lost it, here is another one in front of you.",

            [TextId.ScenarioShamanLastRun1] = "The extraction is complete. Only one extraction point[[extraction]] remains. The endgame starts now.",
            [TextId.ScenarioShamanLastRun2] = "In the endgame, body reports are disabled. A haunting no longer distorts your view either. The sound remains.",
            [TextId.ScenarioShamanLastRun3] = "There is a body[[deathhead]] inside the truck. You can hear the haunting, but your view stays clear. That is how a haunting works in the endgame.",

            [TextId.ScenarioShamanBlocked1] = "Get close to the body[[deathhead]] and press [reportkey].",

            [TextId.ScenarioShamanBlockedNag1] = "The body[[deathhead]] is inside the truck[[truck]]. Get close to it and press [reportkey].",

            [TextId.ScenarioShamanDenied1] = "You saw the 'Cannot call a meeting' notice. The rules disabled your report.",
            [TextId.ScenarioShamanDenied2] = "But a meeting itself can still be called. The report is blocked; the meeting button still works.",

            [TextId.ScenarioShamanButton1] = "For practice, I am giving you a meeting call. In a real run, everyone starts with one. Hold down the meeting button at the back of the truck[[truck]] to call a meeting.",

            [TextId.ScenarioShamanButtonNag1] = "Hello! Are you listening? [[disappointed]][[wave]]",

            [TextId.ScenarioShamanButtonNag2Format] = "Press and hold {0} on the red meeting button at the back of the truck[[truck]].",

            [TextId.ScenarioShamanButtonNag3Grab] = "The same key you use to grab things.",

            [TextId.ScenarioShamanButtonNag3Interact] = "The same key you use to interact with things.",

            [TextId.ScenarioShamanOpened4] = "The meeting has started. No.2 was announced dead.",
            [TextId.ScenarioShamanOpened5] = "Watch how the discussion goes.",

            [TextId.ScenarioShamanVote3] = "No.3 declared themselves the Shaman. But there is only one real Shaman.",
            [TextId.ScenarioShamanVote4] = "Vote however you like.",

            [TextId.ScenarioShamanTally3] = "No.3 received more votes than anyone else, so No.3 will be executed. [[scared]]",

            [TextId.ScenarioShamanResult1] = "Every Werewolf is dead, so the Villagers win.",
            [TextId.ScenarioShamanResult2] = "The Shaman is on the Villager side, so you win too.",

            [TextId.ScenarioShamanPostgame1] = "The results screen. It reveals who had which role.",
            [TextId.ScenarioShamanPostgame2] = "Everyone learns here that No.3 was the Werewolf.",

            [TextId.ScenarioShamanReplay1] = "This tutorial was recorded too.",
            [TextId.ScenarioShamanReplay2] = "The meeting chat and the path you walked are all in there.",

            [TextId.ScenarioShamanWrap1] = "A Shaman can work out when and where a body appeared, providing powerful information when a murder occurs.",
            [TextId.ScenarioShamanWrap2] = "But a Shaman searching for a body is extremely vulnerable. Once the Werewolf team knows who you are, you will become a priority target.",
            [TextId.ScenarioShamanWrap3] = "Once the Werewolf team knows the Shaman is gone, they can get away with a bold killing spree much more easily, putting the Villagers in serious danger.",

            [TextId.ScenarioShamanPostgame3] = "That is the whole Shaman tutorial. [[wave]]",

            [TextId.ScenarioShamanLeashNag1] = "Hey, where are you going? [[:O]]",
            [TextId.ScenarioShamanLeashNag2] = "For now, stay near the truck[[truck]]. Back you go.",

            [TextId.ScenarioShamanExtractionBreak1] = "Why did you extract everything? [[angry]]",
            [TextId.ScenarioShamanExtractionBreak2] = "Granted, the Villagers would win if the truck[[truck]] left now, but that is not in the script.",
            [TextId.ScenarioShamanExtractionBreak3] = "Come back and start over.",

            [TextId.ScenarioShamanEarlyExtractionBreak1] = "Wrong order. You extracted before calling the meeting. [[angry]]",
            [TextId.ScenarioShamanEarlyExtractionBreak2] = "Only one extraction point is left now. Body reports are disabled from here, so you can no longer call a meeting by reporting that body[[deathhead]].",
            [TextId.ScenarioShamanEarlyExtractionBreak3] = "Come back and start over.",

            [TextId.ScenarioShamanQuotaBagBreak1] = "Are you doing that on purpose? [[angry]]",
            [TextId.ScenarioShamanQuotaBagBreak2] = "That bag[[$$$]] is for the extraction. Do not break it or throw it away. We cannot practice like this. [[disappointed]]",
            [TextId.ScenarioShamanQuotaBagBreak3] = "Come back and start over.",

            [TextId.ScenarioShamanMapAside1] = "Ah, never mind the coordinates they[[semibots]] keep calling out.",
            [TextId.ScenarioShamanMapAside2] = "Unlike you[[point]], they are soulless machines. They can only say what they were built to say. [[yawn]]",
            [TextId.ScenarioShamanMapAside3] = "In a real run, remembering the exact coordinates matters.",

            [TextId.ScenarioBomberIntro1] = "This time, you will play as the Bomber.",
            [TextId.ScenarioBomberIntro2] = "No.3 in the next room is a fellow Werewolf. Get close and you will see their number turn red.",
            [TextId.ScenarioBomberIntro3] = "A Bomber cannot use Werewolf perks. Instead, you can turn other players into bombs and detonate them whenever you like.",
            [TextId.ScenarioBomberIntro4] = "Look at the bottom left. The left control plants a bomb, and the right one detonates it. The green number is your ammo; you have one bomb right now.",
            [TextId.ScenarioBomberIntro5] =
                "In a real match, both abilities have long cooldowns. I have shortened them to five seconds for this tutorial.",
            [TextId.ScenarioBomberAim1] = "For your first bomb, turn your ally No.3 into a bomb.",
            [TextId.ScenarioBomberAim2] = "The bomb carrier survives the blast and can tell the meeting who had been near them. By choosing an ally, you keep the key witness on your side.",
            [TextId.ScenarioBomberPlant1] = "First, get close to No.3 in the next room. Stay near them with nothing in between, and the gauge over their head fills.",
            [TextId.ScenarioBomberPlant2] = "Move away or put a wall between you and the gauge will drain. Stay near No.3 until it is full.",
            [TextId.ScenarioBomberPlant3] = "Get close to No.3 in the next room and wait there until the gauge over their head is full. When it is, press [plantkey] to turn No.3 into a bomb.",
            [TextId.ScenarioBomberPlantNag1] = "No.3 is still not a bomb. Stand close with no wall between you and the gauge over their head will rise. It takes about 20 seconds to fill and drains if you move away, so hold your ground. When it is full, press [plantkey].",
            [TextId.ScenarioBomberPlanted1] = "The bomb icon over their head means it worked.",
            [TextId.ScenarioBomberNotice1] = "Meanwhile, No.3 received a warning that someone had stayed close to them long enough to plant a bomb.",
            [TextId.ScenarioBomberNotice2] = "Whoever receives that warning should remember which players were nearby; it can become evidence at the next meeting. This time, though, the person who remembers is your ally.",
            [TextId.ScenarioBomberTry1] = "No.3 is walking to the truck[[truck]] on their own. Once you see them enter it, press the detonate key [bombkey], not the plant key—and see what happens.",
            [TextId.ScenarioBomberTryNag1] = "You still have not pressed it. No.3 is inside the truck[[truck]] now. The right icon at the bottom left is the detonate control; its assigned key is [bombkey].",
            [TextId.ScenarioBomberLock1] = "The rules do not allow detonation while the target is near the truck[[truck]].",
            [TextId.ScenarioBomberLock2] = "Wait for No.3 to leave. They are heading for No.4 and No.5 in the open area.",
            [TextId.ScenarioBomberSafe1] = "Once No.3 is out of the truck[[truck]] zone, you can detonate. But if you are inside the blast radius, you die instantly.",
            [TextId.ScenarioBomberSafe2] = "Back off from No.3 in the open area. Once the gauge over their head stops climbing, press [bombkey] to detonate. This is a tutorial, so I will not accept the detonation while you are too close.",
            [TextId.ScenarioBomberSafeNag1] = "You still have not detonated the bomb. No.3 is huddled with No.4 and No.5 in the open area. Back off until the gauge over No.3 stops climbing, then press [bombkey]. This is a tutorial, so I will not allow you to detonate from inside the blast radius.",
            [TextId.ScenarioBomberBoom1] = "You will hear a heartbeat while you are inside the blast radius. Once you are outside it, you will not.",
            [TextId.ScenarioBomberBoom2] = "A Bomber dies instantly if caught in the blast, so keeping your distance is vital.",
            [TextId.ScenarioBomberMeet1] = "The bomb carrier survives at the center of the blast. No.3 can testify about who spent a long time near them before the explosion.",
            [TextId.ScenarioBomberMeet2] = "No.3 is your ally, so they will cover for you. They will call a meeting as the victim, and everyone will be pulled back to the truck[[truck]] after the warning.",
            [TextId.ScenarioBomberOpened1] = "Nobody died. No.3 at the center and No.4 and No.5 caught in the blast are all alive.",
            [TextId.ScenarioBomberOpened2] = "Open the meeting log and listen to the Villagers discuss how to narrow down who the Bomber is.",
            [TextId.ScenarioBomberVote1] = "They suspect you, but they do not think they have enough to go on.",
            [TextId.ScenarioBomberVote2] = "Vote for anyone you like, or skip. Press once to select and again to confirm.",
            [TextId.ScenarioBomberTally1] = "Skip received the most votes, so nobody was executed.",
            [TextId.ScenarioBomberTally2] = "Your ally's testimony kept the Villagers from finding conclusive evidence. But No.3 took damage at the center of the blast, and the next explosion will kill them.",
            [TextId.ScenarioBomberReplant1] = "Next, you will deliberately blow yourself up to learn the danger radius and bring this tutorial to an end. [[;)]]",
            [TextId.ScenarioBomberReplant2] = "I gave you one more bomb. No.3, No.4, and No.5 are still huddled in the open area.",
            [TextId.ScenarioBomberReplant3] = "This time, turn No.4 in the open area into a bomb.",
            [TextId.ScenarioBomberReplant4] = "Get right up against No.4 in the open area and wait until the gauge over their head is full. Then press [plantkey]. The bomb goes to the nearest player whose gauge is full, so make sure nobody is closer than No.4 when you press it.",
            [TextId.ScenarioBomberReplantNag1] = "No.4 is still not a bomb. Of the three people huddled there, No.4 is your only target. Stand right against them and wait for the gauge over their head to fill, then press [plantkey]. It is the same process as before.",
            [TextId.ScenarioBomberFire1] = "Nicely done. This time, do the opposite and let yourself get caught in the blast.",
            [TextId.ScenarioBomberFire2] = "Get right up against No.4 and press [bombkey]. This is a tutorial, so I will not accept the detonation from farther away. You will hear a heartbeat, which means you are inside the blast radius. Do not move.",
            [TextId.ScenarioBomberFireNag1] = "You still have not detonated the bomb. Get right up against No.4. This is a tutorial, so I will not accept the detonation from farther away. Once you are touching them, press [bombkey]. The heartbeat means you are inside the blast radius, so do not run when you hear it.",
            [TextId.ScenarioBomberResult1] = "Good job! [[creepycrying]] That defeat went exactly according to script.",
            [TextId.ScenarioBomberResult2] = "You died, along with No.3, who had already been weakened at the center of the first blast. That wiped out the Werewolf team, so the Villagers won.",
            [TextId.ScenarioBomberPostgame1] = "This is the results screen. Everyone's role is revealed here.",
            [TextId.ScenarioBomberPostgame2] = "You lost the match, but the Bomber tutorial was a success.",
            [TextId.ScenarioBomberCheck1] = "Check three things before you detonate.",
            [TextId.ScenarioBomberCheck2] = "Is the target out of the truck[[truck]] zone? Are you out of the blast radius? Will the blast hit a fellow Werewolf?",
            [TextId.ScenarioBomberAlly1] = "You turned your ally into the first bomb so they could muddy the bomb carrier's testimony.",
            [TextId.ScenarioBomberAlly2] = "The tradeoff is that your ally takes damage at the center of the blast and has fewer opportunities to sabotage the mission while blending into a group of Villagers.",
            [TextId.ScenarioBomberClue1] = "Villagers narrow down the Bomber using the bomb carrier's account of who they were with and by ruling out players who were caught in the blast and survived.",
            [TextId.ScenarioBomberClue2] = "A Bomber dies if caught in the blast. Surviving a known hit from the blast is therefore strong evidence that a player is not the Bomber.",
            [TextId.ScenarioBomberWrap1] = "The more the Villagers travel as a group, the harder it is to tell who planted the bomb—but the greater the damage when it detonates.",
            [TextId.ScenarioBomberWrap2] = "Simply convincing the Villagers that there might be a Bomber makes it harder for them to move as a group. Once they are sure there is no Bomber to fear, they can group up safely and leave the Werewolf team little room to operate.",
            [TextId.ScenarioBomberPostgame3] = "That is the whole Bomber tutorial. [[wave]]",
            [TextId.ScenarioBomberEarly1] = "Oh, you already planted it? [[:O]]",
            [TextId.ScenarioBomberEarly2] = "Quick learner. [[:)]]",
            [TextId.ScenarioBomberExtractionBreak1] = "You... has the job finally turned you into a workaholic? [[pleading]]",
            [TextId.ScenarioBomberExtractionBreak2] = "There is no need to extract anything right now. Come back and start over.",
            [TextId.ScenarioBomberSurvivedBreak1] = "Why were you not caught in the explosion? [[weary]]",
            [TextId.ScenarioBomberSurvivedBreak2] = "Now the bomb has gone to waste. Come back and start over. [[angry]]",
            [TextId.ScenarioBomberWrongTarget1] = "The bomb landed on the wrong one. It belongs on No.{0}.",
            [TextId.ScenarioBomberWrongTarget2] =
                "The bomb goes to whoever is nearest. Stand closer to No.{0} than to anyone else and press [plantkey] again once the cooldown ends to move it. Moving a planted bomb costs no ammo.",
            [TextId.ScenarioBomberAlreadyOnTarget1] = "That bomb is already on No.{0}. There is no need to move it.",
            [TextId.ScenarioBomberAlreadyOnTarget2] =
                "In a real match you could move it to someone else without spending ammo, but here we stay on No.{0}. Detonating is the next step.",
            [TextId.ScenarioBomberLineA1] = "I was turned into a bomb. It exploded with me at the center, and No.4 and No.5 were caught in the blast.",
            [TextId.ScenarioBomberLineA2] = "I took heavy damage. One more hit and I am dead.",
            [TextId.ScenarioBomberLineA3] = "So there is a Bomber. No.3, who were you with?",
            [TextId.ScenarioBomberLineA4] = "I got the 'Someone may have planted a bomb on me' warning earlier. But a lot of people were around then, so I don't remember who it was.",
            [TextId.ScenarioBomberLineA5] = "No.4 and No.5 survived the blast and are still alive. A Bomber dies the moment they are caught in it, so neither of them can be the Bomber.",
            [TextId.ScenarioBomberLineA6] = "No.3, at the center of the blast, is out too. A Bomber cannot turn themselves into a bomb. That leaves three: No.1, No.2, and No.6.",
            [TextId.ScenarioBomberLineA7] = "No.1, you were moving around with No.3, weren't you? I thought the two of you were sticking together unusually closely.",
            [TextId.ScenarioBomberLineA8] = "Was it really that long? I was busy hauling valuables myself, so I'm not sure.",
            [TextId.ScenarioBomberLineA9] = "If the bomb carrier does not remember, that is not enough to go on.",
            [TextId.ScenarioBomberLineA10] = "There are too many suspects. We should skip the vote and keep our distance from No.1, No.2, and No.6.",
            [TextId.ScenarioBomberLineA11] = "Splitting up is frightening, though. It makes it easier for the Werewolves to pick us off.",
            [TextId.ScenarioBomberLineA12] = "Even if No.3 is not the Bomber, they could still be on the Werewolf team.",
            [TextId.ScenarioBomberLineA13] = "I'm voting No.1.",
            [TextId.ScenarioBomberLineA14] = "We're out of time. Everyone, cast your votes.",

            [TextId.ScenarioShamanLineA1] = "So No.1 found No.7's body at A2.",
            [TextId.ScenarioShamanLineA2] = "First, everyone tell us where you are. I am at C4 with No.2.",
            [TextId.ScenarioShamanLineA3] = "I am at E3 with No.6.",
            [TextId.ScenarioShamanLineA4] = "No.3, where are you right now?",
            [TextId.ScenarioShamanLineA5] = "Uh, somewhere around A3, I think.",
            [TextId.ScenarioShamanLineA6] = "That is close to the body. Do you know anything?",
            [TextId.ScenarioShamanLineA7] = "No idea. It was probably an accident—a monster got them, or they fell into a pit.",
            [TextId.ScenarioShamanLineA8] = "That sounds suspicious.",
            [TextId.ScenarioShamanLineA9] = "No.3, what weapons are you carrying right now?",
            [TextId.ScenarioShamanLineA10] = "A sledgehammer and a gun.",
            [TextId.ScenarioShamanLineA11] = "So you are carrying the perfect tools for a murder.",
            [TextId.ScenarioShamanLineA12] = "Just for self-defense. I carry them to fight monsters.",
            [TextId.ScenarioShamanLineA13] = "If No.3 is the Werewolf, ending the meeting while they hold weapons that strong is dangerous. They could attack us.",
            [TextId.ScenarioShamanLineA14] = "I would never. I am not the Werewolf.",
            [TextId.ScenarioShamanLineA15] = "Fine. I will drop all my weapons. So please do not execute me.",
            [TextId.ScenarioShamanLineA16] = "Hmm, that is still suspicious.",
            [TextId.ScenarioShamanLineA17] = "Still, we cannot tell whether No.7 was murdered or died in an accident.",
            [TextId.ScenarioShamanLineA18] = "How about we skip this time and keep an eye on No.3?",
            [TextId.ScenarioShamanLineA19] = "I think we should execute No.3 right here.",
            [TextId.ScenarioShamanLineA20] = "We are out of time. Everyone, vote.",

            [TextId.ScenarioShamanLineB1] = "Bad news, everyone. No.3 killed No.2.",
            [TextId.ScenarioShamanLineB2] = "That is a misunderstanding. No.2 attacked me and I defended myself. Self-defense.",
            [TextId.ScenarioShamanLineB3] = "That story is too much of a stretch.",
            [TextId.ScenarioShamanLineB4] = "Weren't you supposed to drop your weapons, No.3?",
            [TextId.ScenarioShamanLineB5] = "I saw No.3 strike first as well.",
            [TextId.ScenarioShamanLineB6] = "Wait a moment. The truth is, I am the Shaman.",
            [TextId.ScenarioShamanLineB7] = "I was near the body at the start because I was looking for it.",
            [TextId.ScenarioShamanLineB8] = "I kept quiet because I did not want the Werewolf coming after me.",
            [TextId.ScenarioShamanLineB9] = "But the one who actually found the body was No.1.",
            [TextId.ScenarioShamanLineB10] = "They just beat me to it. With more time I would have found it first.",
            [TextId.ScenarioShamanLineB11] = "I think No.1 is the real Shaman. No.3 is a Werewolf faking the Shaman.",
            [TextId.ScenarioShamanLineB12] = "No.3 it is. Let us vote.",
            [TextId.ScenarioShamanLineB13] = "Ah, so close. I almost had all of you.",
            [TextId.ScenarioShamanLineB14] = "With numbers like these, even strong weapons would not be enough to kill everyone.",

        };
    }
}
