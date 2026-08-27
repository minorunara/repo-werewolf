using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class WerewolfScenarioScript
    {
        public const int PlayerParticipantId = 1;

        private const int LevelsCompleted = 1;

        private const int HaulGoalPctOfTotal = 50;

        private const int StaminaUnlockPct = 10;
        private const int JumpUnlockPct = 20;
        private const int EnemyIgnoreUnlockPct = 30;
        private const int HealUnlockPct = 40;

        private const int BeaconChargePct = 10;

        private const int BeaconSuppressStartSec = 0;
        private const int BeaconCooldownSec = 30;

        private const int GrabReleaseMs = 12000;

        private static readonly EnemySetCounts EnemySets = new EnemySetCounts(danger1: 4, danger2: 1, danger3: 0);

        private static readonly Dictionary<string, int> StartUpgrades = new Dictionary<string, int>
        {
            { WorldgenUpgrades.Strength, 4 },
        };

        private static readonly ScenarioParticipant[] BotRoster = ScenarioRoster.Bots(
            (2, "SemibotA", Role.Villager),
            (3, "SemibotB", Role.Villager),
            (4, "SemibotC", Role.Villager),
            (5, "SemibotD", Role.Villager));

        private static readonly TextId[] DestroyEarly =
        {
            TextId.ScenarioWolfEarly1, TextId.ScenarioWolfEarly2,
        };

        private static readonly TextId[] ExtractionNag =
        {
            TextId.ScenarioWolfExtractionNag1, TextId.ScenarioWolfExtractionNag2,
        };

        private static readonly TextId[] ExtractionBreak =
        {
            TextId.ScenarioWolfExtractionBreak1, TextId.ScenarioWolfExtractionBreak2,
            TextId.ScenarioWolfExtractionBreak3,
        };

        private static readonly ScenarioPage[] FlowPages =
        {
            ScenarioPage.Say(ScenarioSignal.Advance,
                TextId.ScenarioWolfIntro1, TextId.ScenarioWolfIntro2, TextId.ScenarioWolfIntro3)
                .At(ScenarioAnchor.PlayHud),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfGauge1, TextId.ScenarioWolfGauge2, TextId.ScenarioWolfGauge3)
                .At(ScenarioAnchor.PlayHudRight).PointsAt(ScenarioHighlight.PlayGauge),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfEnemyMap1, TextId.ScenarioWolfEnemyMap2,
                    TextId.ScenarioWolfEnemyMap3)
                .At(ScenarioAnchor.PlayHud).Requires(ScenarioNeed.MapToolOpen),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfDestroy1, TextId.ScenarioWolfDestroy2)
                .At(ScenarioAnchor.PlayHud)
                .SkipWhen(ScenarioSignal.ValuableDestroyed)
                .Marks(ScenarioMilestone.ActionPrompted)
                .WithNag(TextId.ScenarioWolfDestroyNag1, TextId.ScenarioWolfDestroyNag2),

            ScenarioPage.Say(ScenarioSignal.ValuableDestroyed,
                    TextId.ScenarioWolfMarker1, TextId.ScenarioWolfMarker2, TextId.ScenarioWolfMarker3)
                .At(ScenarioAnchor.PlayHud).Requires(ScenarioNeed.MapToolOpen),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfMarker4, TextId.ScenarioWolfMarker5)
                .At(ScenarioAnchor.PlayHud).Requires(ScenarioNeed.MapToolOpen),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfRecord1, TextId.ScenarioWolfRecord2, TextId.ScenarioWolfRecord3)
                .At(ScenarioAnchor.PlayHudRight).PointsAt(ScenarioHighlight.RecordIcon)
                .SkipWhen(ScenarioSignal.RecordTurnedOff)
                .WithNag(TextId.ScenarioWolfRecordNag1),

            ScenarioPage.Say(ScenarioSignal.RecordTurnedOff,
                    TextId.ScenarioWolfStealth1, TextId.ScenarioWolfStealth2)
                .At(ScenarioAnchor.PlayHud)
                .SkipWhen(ScenarioSignal.StealthDestroyed)
                .WithNag(TextId.ScenarioWolfStealthNag1),
            ScenarioPage.Say(ScenarioSignal.StealthDestroyed,
                    TextId.ScenarioWolfStealth3, TextId.ScenarioWolfStealth4)
                .At(ScenarioAnchor.PlayHud).Requires(ScenarioNeed.MapToolOpen),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfKeep1, TextId.ScenarioWolfKeep2)
                .At(ScenarioAnchor.PlayHud)
                .SkipWhen(ScenarioSignal.PerkStaminaUnlocked),

            ScenarioPage.Say(ScenarioSignal.PerkStaminaUnlocked,
                    TextId.ScenarioWolfForm1, TextId.ScenarioWolfForm2,
                    TextId.ScenarioWolfForm3, TextId.ScenarioWolfForm4)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.WolfStatus),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfFormOn1, TextId.ScenarioWolfFormOn2)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.WolfStatus)
                .SkipWhen(ScenarioSignal.WolfFormTurnedOn)
                .WithNag(TextId.ScenarioWolfFormOnNag1),
            ScenarioPage.Say(ScenarioSignal.WolfFormTurnedOn,
                    TextId.ScenarioWolfFormDone1, TextId.ScenarioWolfFormDone2)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.WolfStatus)
                .Does(ScenarioEntryAction.GrantBeaconCharge),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfBeacon1, TextId.ScenarioWolfBeacon2, TextId.ScenarioWolfBeacon3)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.BeaconSlot)
                .Does(ScenarioEntryAction.HoldEnemiesDespawned)
                .SkipWhen(ScenarioSignal.BeaconUsed)
                .WithNag(TextId.ScenarioWolfBeaconNag1),
            ScenarioPage.Say(ScenarioSignal.BeaconUsed,
                    TextId.ScenarioWolfBeacon4, TextId.ScenarioWolfBeacon5, TextId.ScenarioWolfBeacon6)
                .At(ScenarioAnchor.PlayHud),

            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioWolfPush1)
                .At(ScenarioAnchor.PlayHudRight).PointsAt(ScenarioHighlight.PlayGauge)
                .SkipWhen(ScenarioSignal.PerkEnemyIgnoreUnlocked),
            ScenarioPage.Say(ScenarioSignal.PerkEnemyIgnoreUnlocked,
                    TextId.ScenarioWolfIgnore1, TextId.ScenarioWolfIgnore2)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.WolfStatus),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioWolfPush2)
                .At(ScenarioAnchor.PlayHudRight).PointsAt(ScenarioHighlight.PlayGauge)
                .SkipWhen(ScenarioSignal.PerkHealUnlocked),
            ScenarioPage.Say(ScenarioSignal.PerkHealUnlocked,
                    TextId.ScenarioWolfHeal1, TextId.ScenarioWolfHeal2)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.WolfStatus),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfOrb1, TextId.ScenarioWolfOrb2)
                .At(ScenarioAnchor.PlayHudRight).PointsAt(ScenarioHighlight.PlayGauge),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfFinish1, TextId.ScenarioWolfFinish2, TextId.ScenarioWolfFinish3)
                .At(ScenarioAnchor.PlayHud)
                .Marks(ScenarioMilestone.CheckmateArmed),

            ScenarioPage.Say(ScenarioSignal.WinCeremonyDone,
                    TextId.ScenarioWolfResult1, TextId.ScenarioWolfResult2)
                .At(ScenarioAnchor.CheckmateStamp)
                .MustBeRead(),

            ScenarioPage.Say(ScenarioSignal.ResultScreenShown,
                    TextId.ScenarioWolfPostgame1, TextId.ScenarioWolfPostgame2)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfReplay1, TextId.ScenarioWolfReplay2)
                .At(ScenarioAnchor.ReplayEntry)
                .PointsAt(ScenarioHighlight.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioWolfWrap1, TextId.ScenarioWolfWrap2)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioWolfPostgame3)
                .At(ScenarioAnchor.ReplayEntry),
        };

        private static readonly ScenarioDefinition Def = Build();

        public static ScenarioDefinition Definition => Def;

        private static ScenarioDefinition Build()
        {
            var cast = new ScenarioCast(PlayerParticipantId, Role.Werewolf, BotRoster,
                werewolfCount: 1, blackCatChancePercent: 0,
                bomberChancePercent: 0, shamanChancePercent: 0);

            var environment = new ScenarioEnvironmentSpec(
                ScenarioEnvironmentSpec.DefaultLevelName, LevelsCompleted, seededLossPct: 0,
                truckLeashMeters: 0f, truckLeashUntil: null,
                haulGoalPctOfTotal: HaulGoalPctOfTotal,
                startUpgrades: StartUpgrades,
                enemySets: EnemySets);

            var timing = ScenarioCueTiming.Default;

            var notices = new ScenarioNotices(leashNag: null,
                earlyStartSignal: ScenarioSignal.ValuableDestroyed,
                earlyStartAfter: ScenarioMilestone.ActionPrompted, earlyStartLines: DestroyEarly,
                keyHint: default, extractionNag: ExtractionNag,
                breakLines: new Dictionary<ScenarioBreakKind, TextId[]>
                {
                    [ScenarioBreakKind.AllExtractionsCompleted] = ExtractionBreak,
                });

            var watch = new ScenarioBreakWatch(requireTallyBeforeResult: false,
                meetingRequiredFrom: null, meetingRequiredUntil: null,
                breakOnAllExtractionsCompleted: true);

            var safety = new ScenarioSafetySpec(
                suppressEnemies: false, invincible: false,
                clampDamage: true, grabReleaseMs: GrabReleaseMs);

            var room = new ScenarioRoomSpec(
                meetingRightsPerPlayer: 0,
                staminaUnlockPct: StaminaUnlockPct, jumpUnlockPct: JumpUnlockPct,
                enemyIgnoreUnlockPct: EnemyIgnoreUnlockPct, healUnlockPct: HealUnlockPct,
                beaconChargePct: BeaconChargePct,
                beaconSuppressStartSec: BeaconSuppressStartSec,
                beaconSuppressAfterMeetingSec: 0,
                beaconCooldownSec: BeaconCooldownSec);

            return new ScenarioDefinition(ScenarioId.WerewolfPerks, cast, environment,
                FlowPages, timing: timing, notices: notices, watch: watch, safety: safety, room: room);
        }
    }
}
