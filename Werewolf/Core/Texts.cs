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
        NoticeBeaconAuditFormat,
        NoticeBeaconAuditNone,
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
        RevealSkipHint,

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

        StartHoldWaitingOthers,

        ResultBannerVillagerWin,
        ResultBannerWerewolfWin,
        ResultBannerDefault,
        ResultStatusAlive,
        ResultStatusDead,
        ResultStatusExecuted,
        ResultStatusDisconnected,
        ResultReturnPromptFormat,
        ResultWaitingHost,
        ResultFooterWithCountdownFormat,

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

        RecapTitle,
        RecapNameSeparator,
        RecapDeathsFormat,
        RecapDeathsNone,
        RecapLostFormat,
        RecapHaulFormat,
        RecapBeaconFormat,
        RecapBeaconNone,

        DeathRevealTitle,
        DeathRevealNone,

        CheckmateTitle,

        MapOverlayToggleLabelFormat,

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

        ConveneCountdownDefaultCallerName,
        ConveneCountdownHeaderFormat,
        ConveneCountdownCorpseHeaderFormat,

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

        SettingsLabelWerewolfCount,
        SettingsLabelBlackCatChancePercent,
        SettingsLabelBomberChancePercent,
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
        SettingsLabelValuableMapMode,
        SettingsLabelGameOverAutoReturnSec,
        SettingsLabelNecroVoiceMode,
        SettingsLabelStartLevelNumber,
        SettingsLabelStartMapName,
        SettingsLabelStartItemsSpec,
        SettingsLabelStartEnergyPct,
        SettingsLabelStartUpgradesSpec,
        SettingsLabelOrbDropMax,

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
            [TextId.NoticeBeaconAuditFormat] = "前回の会議以降、ビーコンは{0}回使用されました",
            [TextId.NoticeBeaconAuditNone] = "前回の会議以降、ビーコンは使用されませんでした",
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

            [TextId.RevealTeammatePrefix] = "人狼仲間：",
            [TextId.RevealHeadingWinCondition] = "◆ 勝利条件（いずれか）",
            [TextId.RevealHeadingAbility] = "◆ あなたの能力",
            [TextId.RevealVillagerTitleMaybeCat] = "あなたは村人…かもしれません。",
            [TextId.RevealVillagerTitle] = "あなたは村人です",
            [TextId.RevealVillagerWinCondition1] = "全ての抽出を完了し、トラックを発車させる",
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
            [TextId.RevealSkipHint] = "[menu] でスキップ",

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

            [TextId.StartHoldWaitingOthers] = "他のプレイヤーの準備を待っています…",

            [TextId.ResultBannerVillagerWin] = "村人陣営の勝利",
            [TextId.ResultBannerWerewolfWin] = "人狼陣営の勝利",
            [TextId.ResultBannerDefault] = "試合結果",
            [TextId.ResultStatusAlive] = "生存",
            [TextId.ResultStatusDead] = "死亡",
            [TextId.ResultStatusExecuted] = "処刑",
            [TextId.ResultStatusDisconnected] = "切断",
            [TextId.ResultReturnPromptFormat] = "[{0}] ロビーへ戻る",
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
            [TextId.DigestExtractionDoneFormat] = "納品完了（{0}/{1}）",
            [TextId.DigestPerkUnlockedFormat] = "人狼特典「{0}」解禁",
            [TextId.DigestInformant] = "内通が成立（黒猫へ人狼陣営を開示）",
            [TextId.DigestFinalBalanceFormat] = "最終収支: 納品 ${0} ／ 残ノルマ ${1} ／ 回収可能 ${2}",

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

            [TextId.RecapTitle] = "ここまでの経過",
            [TextId.RecapNameSeparator] = "、",
            [TextId.RecapDeathsFormat] = "死亡: {0}",
            [TextId.RecapDeathsNone] = "死亡: なし",
            [TextId.RecapLostFormat] = "破壊された貴重品: ${0}",
            [TextId.RecapHaulFormat] = "納品: ${0} ／ ノルマ ${1}",
            [TextId.RecapBeaconFormat] = "ビーコン使用: {0}回",
            [TextId.RecapBeaconNone] = "ビーコン使用: なし",

            [TextId.DeathRevealTitle] = "死亡者",
            [TextId.DeathRevealNone] = "誰も死んでいない",

            [TextId.CheckmateTitle] = "債権はもはや徴収不能だ",

            [TextId.MapOverlayToggleLabelFormat] = "全体マップ [{0}]",

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
                "現在 {0}人 / 人狼で遊ぶには最低でも {1}人 必要です\n"
                + "通常のREPOとして遊びたい場合は、ルーム設定で人狼モード\n"
                + "（WerewolfModeEnabled）をOFFにしてください。",
            [TextId.LobbyStartTeamOverflowTitle] = "人狼の人数が多すぎます",
            [TextId.LobbyStartTeamOverflowBodyFormat] =
                "人狼の設定 {0}人 / 現在の参加者 {1}人\n"
                + "村人が1人もいない配役は成立しません。ルーム設定で\n"
                + "人狼の人数（WerewolfCount）を下げてください。",

            [TextId.ConveneCountdownDefaultCallerName] = "誰か",
            [TextId.ConveneCountdownHeaderFormat] = "{0}が会議を招集しました！\nワープまで残り…",
            [TextId.ConveneCountdownCorpseHeaderFormat] = "{0}が事件現場を通報しました！\nワープまで残り…",

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

            [TextId.SettingsLabelWerewolfCount] = "人狼の人数",
            [TextId.SettingsLabelBlackCatChancePercent] = "黒猫の出現確率",
            [TextId.SettingsLabelBomberChancePercent] = "爆弾魔の出現確率",
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
            [TextId.SettingsLabelValuableMapMode] = "貴重品マップ表示モード",
            [TextId.SettingsLabelGameOverAutoReturnSec] = "結果画面の自動帰還秒数（0=自動で戻らない）",
            [TextId.SettingsLabelNecroVoiceMode] = "冥界の声（死者→生存人狼の傍聴）",
            [TextId.SettingsLabelStartLevelNumber] = "レベル",
            [TextId.SettingsLabelStartMapName] = "マップ",
            [TextId.SettingsLabelStartItemsSpec] = "持ち込みアイテム",
            [TextId.SettingsLabelStartEnergyPct] = "トラックの充電器",
            [TextId.SettingsLabelStartUpgradesSpec] = "開始時の能力強化",
            [TextId.SettingsLabelOrbDropMax] = "敵が落とすオーブの数",

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
                "死者は蘇らないから抽出場所に運ぶ必要はない。\n" +
                "未発見の死体の近くでは、開催権に関係なく通報ができる（右下HUDのアイコンが色付きで脈動する）。\n" +
                "どんな状況で死んでいたか、会議で皆に共有すること。",

            [TextId.TutorialMeetingCountdownStarted] =
                "会議が招集された。トラックへワープするまでのカウントダウンだ。\n" +
                "手に持っている貴重品・武器・ドローンは、その場に残される（インベントリの中身は持ち帰れる）\n" +
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
                "誰をトラックに残し、誰に最後の抽出を任せるか慎重に決めろ。",

            [TextId.TutorialRoundTimeWarningVillager] =
                "間もなく納期だ。\n" +
                "納期までに全ての抽出を終え、トラックを発車させろ。できなければ村人の負けだ。\n" +
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
                "人狼陣営は既定で貴重品を発見しない。誰にも知られていない貴重品なら、\n" +
                "壊しても「あったはずの物が無い」と気づかれずに済む。\n" +
                "記録したくなったら、通報キーの長押しで切り替えろ。",

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
                "村人陣営の目標は、通常のREPOと同じです。マップから貴重品を集めて全ての抽出を完了させ、トラックを出発させることです。\n" +
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
                "・全ての抽出を完了させ、村人陣営が1人以上生存した状態でトラックを出発させる\n" +
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

            [TextId.ManualValuablesMapTitle] = "貴重品とマップ",
            [TextId.ManualValuablesMapBody] =
                "誰かが貴重品を発見したり、敵がオーブを落としたりすると、マップに黄色いマーカーが登録されます。\n" +
                "通常のREPOと違い、標準設定ではこのマーカーはリアルタイムに更新されません。\n" +
                "更新されるのは、抽出を完了したときと会議を開いたときだけです。\n" +
                "マーカーの場所に現物が無い場合、誰かが運んだか、壊されたかのどちらかです。\n" +
                "マップからは区別できません。\n" +
                "注意深い村人は、会議のたびに「あの貴重品はどこへ行ったのか」を棚卸しして推理します。",

            [TextId.ManualValuableRecordTitle] = "貴重品を記録しない（人狼陣営）",
            [TextId.ManualValuableRecordBody] =
                "貴重品マーカーがマップに登録されるのは「誰かがその貴重品を視界に捉えた」瞬間です。\n" +
                "人狼陣営（人狼・爆弾魔・自覚した黒猫）は、この記録を既定で行いません。\n" +
                "そのため、まだ誰にも見つかっていない貴重品を人狼陣営が先回りして壊した場合、マップには最初から何も残りません。\n" +
                "逆に、村人が先に見つけて記録済みの貴重品を壊せば、マーカーだけが残り「あったはずの物が無い」と気づかれます。",
            [TextId.ManualValuableRecordToggle] =
                "記録するかどうかは、通報キーの長押しでいつでも切り替えられます（右下のアイコンが現在の状態です）。\n" +
                "村人に紛れて探索するときなど、必要に応じて記録をONにしましょう。",

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
                "また、未完了の抽出地点が最後の1箇所になると、死体通報による会議の開催はできなくなります（例: 抽出ノルマが4箇所なら、3箇所目の完了以降は通報不可）。\n" +
                "ボタンでの会議開催は最後まで可能です。",

            [TextId.ManualMeetingFlowTitle] = "会議の流れ",
            [TextId.ManualMeetingFlowBody] =
                "会議が始まると、全員がトラックへワープして身動きが取れなくなります。\n" +
                "会議中は敵が消え、新たに現れることもないので安全です。\n" +
                "まず前回の会議以降の死亡者が発表され、続いて貴重品減額ゲージの変化が開示されます。\n" +
                "その後、投票が始まります。会議中は全体マップを開いて、更新された貴重品の情報を確認できます。\n" +
                "試合の制限時間は会議中は停止するので、議論で持ち時間は減りません。\n" +
                "ただし、会議中にも敵のリスポーンタイマーは減っていくので、会議で時間を浪費しすぎると敵が復活してしまいます。\n" +
                "会議終了時点でリスポーンの準備が整っている敵は即座にスポーンします。",

            [TextId.ManualVotingTitle] = "投票と処刑",
            [TextId.ManualVotingBody] =
                "会議では生存者だけが1人1票を投じます。スキップ（誰にも投票しない）も選べます。\n" +
                "誰が投票を済ませたかは全員に見えますが、誰に投票したかは分かりません。\n" +
                "最も票を集めたプレイヤーが処刑されます。最多票が同数の場合は誰も処刑されません。\n" +
                "会議には制限時間があり、誰かが投票するたびに残り時間が少し減ります。",

            [TextId.ManualGaugeBasicsTitle] = "貴重品減額ゲージの見方",
            [TextId.ManualGaugeIntro] =
                "貴重品減額ゲージは、村人陣営と人狼陣営の貴重品の奪い合いを視覚化したものです。試合開始時点でマップにある貴重品の総額が基準になります。",
            [TextId.ManualGaugeLoss] =
                "黄色のゲージ（左から伸びる）…傷つき、失われた金額",
            [TextId.ManualGaugeDelivery] =
                "水色のゲージ（右から伸びる）…抽出を完了して納品した金額",
            [TextId.ManualGaugeLines] =
                "・青い縦線…ノルマ達成に必要な金額。水色のゲージがここに届けば、トラックの発車が可能になります\n" +
                "・赤い縦線…場に出ている貴重品を全て集めてもノルマを達成できなくなる「詰み」のライン。黄色のゲージがここに達すると、その瞬間に人狼陣営が勝利します\n" +
                "敵がオーブを落としたり、新たな貴重品が場に増えたりすると、赤い縦線は右へ移動します（村人側の余裕が増えます）。人狼陣営の詰み勝利が確定した後に移動しても覆りません。",

            [TextId.ManualRoleVillagerTitle] = "役職: 村人",
            [TextId.ManualRoleVillagerBody] =
                "所属陣営：村人陣営\n" +
                "村人に特殊な能力はありません。通常のREPOプレイヤーとして貴重品を集め、抽出を目指します。\n" +
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
                "吊るほどの確証がない相手は、武器を没収して様子を見るのも一つの手です。武器がなければ、たとえ人狼陣営でも簡単には人を殺せません。",

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
                "未完了の抽出地点が最後の1箇所になると、霊障による視界の歪みは消え、音だけになります。\n" +
                "会議で発表済みの死体には反応しません。",

            [TextId.ManualAfterDeathTitle] = "死んでしまったら",
            [TextId.ManualAfterDeathBody] =
                "死亡すると観戦モードになり、そのまま試合の行方を見届けることになります。\n" +
                "死者同士では会話できますが、通常のREPOのようにDeathHeadなどを用いて生存者に情報を伝える手段は一切ありません。\n" +
                "ルーム設定次第では人狼陣営にエコーがかった声で死者の声が聞こえます。\n" +
                "勝敗は陣営で共有されるため、死亡していても所属陣営が勝てば勝利です。",
        };

        private static readonly IReadOnlyDictionary<TextId, string> EnglishTable = new Dictionary<TextId, string>
        {
            [TextId.NoticeConveneStartedFormat] = "{0} called an emergency meeting",
            [TextId.NoticeBeaconAuditFormat] = "Beacon uses since the last meeting: {0}",
            [TextId.NoticeBeaconAuditNone] = "The beacon has not been used since the last meeting",
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
            [TextId.RevealSkipHint] = "[menu] to skip",

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

            [TextId.StartHoldWaitingOthers] = "Waiting for other players…",

            [TextId.ResultBannerVillagerWin] = "Villager Team Wins",
            [TextId.ResultBannerWerewolfWin] = "Werewolf Team Wins",
            [TextId.ResultBannerDefault] = "Match Results",
            [TextId.ResultStatusAlive] = "Alive",
            [TextId.ResultStatusDead] = "Dead",
            [TextId.ResultStatusExecuted] = "Executed",
            [TextId.ResultStatusDisconnected] = "Disconnected",
            [TextId.ResultReturnPromptFormat] = "[{0}] Return to Lobby",
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
            [TextId.DigestFinalBalanceFormat] = "Final tally: delivered ${0} / quota left ${1} / obtainable ${2}",

            [TextId.VoteMeetingTitle] = "Emergency Meeting",
            [TextId.VoteSkipLabel] = "Skip",
            [TextId.VoteConfirmLabel] = "Sure?",
            [TextId.VoteVoteLabel] = "Vote",
            [TextId.VoteWerewolfMarkerLabel] = "W",
            [TextId.VoteBomberMarkerLabel] = "B",
            [TextId.VoteCountFormat] = "{0} votes",
            [TextId.VoteExecutedFormat] = "Executed: {0}",
            [TextId.VoteNoExecution] = "No execution",
            [TextId.VoteSkipSuffixFormat] = " (Skip: {0})",

            [TextId.ChatLogTitle] = "Meeting Chat Log",
            [TextId.ChatLogEmpty] = "No messages yet",
            [TextId.ChatLogDeadHint] = "Gray messages are visible only to the dead",
            [TextId.ChatLogToggleLabelFormat] = "Meeting Chat Log [{0}]",
            [TextId.ChatLogVoted] = "has voted.",

            [TextId.RecapTitle] = "Recap",
            [TextId.RecapNameSeparator] = ", ",
            [TextId.RecapDeathsFormat] = "Deaths: {0}",
            [TextId.RecapDeathsNone] = "Deaths: none",
            [TextId.RecapLostFormat] = "Valuables destroyed: ${0}",
            [TextId.RecapHaulFormat] = "Delivered: ${0} / Quota ${1}",
            [TextId.RecapBeaconFormat] = "Beacon uses: {0}",
            [TextId.RecapBeaconNone] = "Beacon uses: none",

            [TextId.DeathRevealTitle] = "Deaths",
            [TextId.DeathRevealNone] = "No one died",

            [TextId.CheckmateTitle] = "THE DEBT CAN NO LONGER BE COLLECTED",

            [TextId.MapOverlayToggleLabelFormat] = "Full Map [{0}]",

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
                "Current players: {0} / Minimum required: {1}\n"
                + "To play regular R.E.P.O., turn Werewolf Mode\n"
                + "(WerewolfModeEnabled) off in the room settings.",
            [TextId.LobbyStartTeamOverflowTitle] = "Too many werewolves",
            [TextId.LobbyStartTeamOverflowBodyFormat] =
                "Werewolves: {0} / Current players: {1}\n"
                + "A game with no villagers cannot start. Lower the\n"
                + "Werewolf count (WerewolfCount) in the room settings.",

            [TextId.ConveneCountdownDefaultCallerName] = "Someone",
            [TextId.ConveneCountdownHeaderFormat] = "{0} called a meeting!\nWarping in…",
            [TextId.ConveneCountdownCorpseHeaderFormat] = "{0} reported a body!\nWarping in…",

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

            [TextId.SettingsLabelWerewolfCount] = "Werewolves",
            [TextId.SettingsLabelBlackCatChancePercent] = "Black Cat chance",
            [TextId.SettingsLabelBomberChancePercent] = "Bomber chance",
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
            [TextId.SettingsLabelValuableMapMode] = "Valuables map mode",
            [TextId.SettingsLabelGameOverAutoReturnSec] = "Result screen auto-return (0 = never)",
            [TextId.SettingsLabelNecroVoiceMode] = "Voices of the dead (dead → living werewolves)",
            [TextId.SettingsLabelStartLevelNumber] = "Level",
            [TextId.SettingsLabelStartMapName] = "Map",
            [TextId.SettingsLabelStartItemsSpec] = "Starting items",
            [TextId.SettingsLabelStartEnergyPct] = "Starting truck charge",
            [TextId.SettingsLabelStartUpgradesSpec] = "Starting upgrades",
            [TextId.SettingsLabelOrbDropMax] = "Orbs dropped by enemies",

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
                "Whatever you're holding—valuables, weapons, or drones—stays behind. (Your inventory comes with you.)\n" +
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
                "The werewolf team does not record newly discovered valuables by default. If nobody else knows\n" +
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

            [TextId.ManualValuablesMapTitle] = "Valuables and the Map",
            [TextId.ManualValuablesMapBody] =
                "When a player discovers a valuable or an enemy drops an orb, a yellow marker is added to the map.\n" +
                "Unlike regular R.E.P.O., these markers do not update in real time under the default settings.\n" +
                "They update only when an extraction is completed or a meeting starts.\n" +
                "If no valuable is at a marker, someone either carried it away or destroyed it.\n" +
                "The map cannot tell you which.\n" +
                "Careful villagers take inventory at every meeting and ask where each missing valuable went.",

            [TextId.ManualValuableRecordTitle] = "Not Recording Valuables (Werewolf Team)",
            [TextId.ManualValuableRecordBody] =
                "A marker is added the moment a player sees a valuable.\n" +
                "By default, the werewolf team (Werewolves, Bombers, and awakened Black Cats) does not record newly discovered valuables.\n" +
                "If a member of the werewolf team reaches and destroys a valuable before anyone else finds it, the map shows no trace that it ever existed.\n" +
                "If a villager has already found and recorded it, however, the marker remains—alerting everyone that something is missing.",
            [TextId.ManualValuableRecordToggle] =
                "You can switch recording on and off at any time by holding the report key (the icon at the bottom right shows the current state).\n" +
                "Turn recording on as needed when you want to blend in with villagers while exploring.",

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
                "Enemy respawn timers continue to count down, however, so an overly long meeting brings the enemies back sooner.\n" +
                "Any enemy whose respawn timer has expired by the end of the meeting spawns immediately.",

            [TextId.ManualVotingTitle] = "Voting and Execution",
            [TextId.ManualVotingBody] =
                "Each surviving player casts one vote at a meeting. Skipping (voting for no one) is also an option.\n" +
                "Everyone can see who has finished voting, but not who they voted for.\n" +
                "The player with the most votes is executed. If there is a tie for the most votes, no one is executed.\n" +
                "Meetings have a time limit, and each vote cast reduces the remaining time slightly.",

            [TextId.ManualGaugeBasicsTitle] = "Reading the Valuable Loss Gauge",
            [TextId.ManualGaugeIntro] =
                "The valuable loss gauge tracks the struggle over valuables between the villager and werewolf teams. Its baseline is the total value of all valuables on the map when the match starts.",
            [TextId.ManualGaugeLoss] =
                "Yellow bar (grows from the left)… total value lost through damage",
            [TextId.ManualGaugeDelivery] =
                "Cyan bar (grows from the right)… total value delivered through completed extractions",
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
        };
    }
}
