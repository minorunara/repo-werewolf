using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class BomberScenarioScript
    {
        public const int PlayerParticipantId = 1;

        public const int WerewolfParticipantId = 3;

        public const int TargetParticipantId = 4;

        public const int BystanderParticipantId = 5;

        public const int FarParticipantA = 2;
        public const int FarParticipantB = 6;

        private const int LevelsCompleted = 1;

        private const int BomberCooldownSec = 5;

        private static readonly ScenarioParticipant[] BotRoster = ScenarioRoster.Bots(
            (FarParticipantA, "SemibotA", Role.Villager),
            (WerewolfParticipantId, "SemibotB", Role.Werewolf),
            (TargetParticipantId, "SemibotC", Role.Villager),
            (BystanderParticipantId, "SemibotD", Role.Villager),
            (FarParticipantB, "SemibotE", Role.Villager));

        private static readonly ScenarioChatLine[] Meeting1Script =
        {
            new ScenarioChatLine(3, TextId.ScenarioBomberLineA1),
            new ScenarioChatLine(4, TextId.ScenarioBomberLineA2),
            new ScenarioChatLine(2, TextId.ScenarioBomberLineA3),
            new ScenarioChatLine(3, TextId.ScenarioBomberLineA4),
            new ScenarioChatLine(6, TextId.ScenarioBomberLineA5),
            new ScenarioChatLine(5, TextId.ScenarioBomberLineA6),
            new ScenarioChatLine(2, TextId.ScenarioBomberLineA7),
            new ScenarioChatLine(3, TextId.ScenarioBomberLineA8),
            new ScenarioChatLine(6, TextId.ScenarioBomberLineA9),
            new ScenarioChatLine(5, TextId.ScenarioBomberLineA10),
            new ScenarioChatLine(4, TextId.ScenarioBomberLineA11),
            new ScenarioChatLine(6, TextId.ScenarioBomberLineA12),
            new ScenarioChatLine(2, TextId.ScenarioBomberLineA13),
            new ScenarioChatLine(6, TextId.ScenarioBomberLineA14),
        };

        private static readonly ScenarioVote[] Meeting1Votes =
        {
            new ScenarioVote(FarParticipantA, PlayerParticipantId),
            ScenarioVote.Skip(WerewolfParticipantId),
            ScenarioVote.Skip(TargetParticipantId),
            ScenarioVote.Skip(BystanderParticipantId),
            ScenarioVote.Skip(FarParticipantB),
        };

        private static readonly ScenarioSignal[] BombSignals =
        {
            ScenarioSignal.BombPlanted, ScenarioSignal.BombDeniedTruckZone,
            ScenarioSignal.BombDetonationAccepted, ScenarioSignal.BombExploded,
        };

        private static readonly TextId[] PlantEarly =
        {
            TextId.ScenarioBomberEarly1, TextId.ScenarioBomberEarly2,
        };

        private static readonly TextId[] ExtractionBreak =
        {
            TextId.ScenarioBomberExtractionBreak1, TextId.ScenarioBomberExtractionBreak2,
        };

        private static readonly TextId[] SurvivedBreak =
        {
            TextId.ScenarioBomberSurvivedBreak1, TextId.ScenarioBomberSurvivedBreak2,
        };

        private static readonly TextId[] WrongTarget =
        {
            TextId.ScenarioBomberWrongTarget1, TextId.ScenarioBomberWrongTarget2,
        };

        private static readonly TextId[] AlreadyOnTarget =
        {
            TextId.ScenarioBomberAlreadyOnTarget1, TextId.ScenarioBomberAlreadyOnTarget2,
        };

        private static readonly ScenarioPage[] FlowPages =
        {
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberIntro1, TextId.ScenarioBomberIntro2)
                .At(ScenarioAnchor.PlayHud)
                .BombTarget(WerewolfParticipantId)
                .Does(ScenarioEntryAction.BodyToPlantSite, WerewolfParticipantId)
                .Does(ScenarioEntryAction.BodyToGatherSite, TargetParticipantId)
                .Does(ScenarioEntryAction.BodyToGatherSite, BystanderParticipantId)
                .Does(ScenarioEntryAction.BodyToFarSite, FarParticipantA)
                .Does(ScenarioEntryAction.BodyToFarSite, FarParticipantB),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberIntro3, TextId.ScenarioBomberIntro4,
                    TextId.ScenarioBomberIntro5)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.BomberHud)
                .BombTarget(WerewolfParticipantId),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberAim1, TextId.ScenarioBomberAim2)
                .At(ScenarioAnchor.PlayHud)
                .BombTarget(WerewolfParticipantId),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberPlant1, TextId.ScenarioBomberPlant2)
                .At(ScenarioAnchor.PlayHud)
                .BombTarget(WerewolfParticipantId).MarksBombTarget()
                .SkipWhen(ScenarioSignal.BombPlanted),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioBomberPlant3)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.BomberHud)
                .Requires(ScenarioNeed.BombTargetNear)
                .BombTarget(WerewolfParticipantId).MarksBombTarget()
                .SkipWhen(ScenarioSignal.BombPlanted)
                .Marks(ScenarioMilestone.ActionPrompted)
                .WithNag(TextId.ScenarioBomberPlantNag1),

            ScenarioPage.Say(ScenarioSignal.BombPlanted,
                    TextId.ScenarioBomberPlanted1, TextId.ScenarioBomberNotice1, TextId.ScenarioBomberNotice2)
                .At(ScenarioAnchor.PlayHud)
                .BombTarget(WerewolfParticipantId)
                .Does(ScenarioEntryAction.BodyToTruck, WerewolfParticipantId)
                .Detonation(ScenarioBlastRule.AwaitTruckZone)
                .SkipWhen(ScenarioSignal.BombDeniedTruckZone),

            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioBomberTry1)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.BomberHud)
                .BombTarget(WerewolfParticipantId)
                .Detonation(ScenarioBlastRule.AwaitTruckZone)
                .SkipWhen(ScenarioSignal.BombDeniedTruckZone)
                .WithNag(TextId.ScenarioBomberTryNag1),
            ScenarioPage.Say(ScenarioSignal.BombDeniedTruckZone,
                    TextId.ScenarioBomberLock1, TextId.ScenarioBomberLock2)
                .At(ScenarioAnchor.PlayHud)
                .BombTarget(WerewolfParticipantId)
                .Does(ScenarioEntryAction.BodyToGatherSite, WerewolfParticipantId)
                .Detonation(ScenarioBlastRule.OutsideBlast)
                .SkipWhen(ScenarioSignal.BombDetonationAccepted),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberSafe1, TextId.ScenarioBomberSafe2)
                .At(ScenarioAnchor.PlayHud)
                .Requires(ScenarioNeed.BombOnTarget).BombTarget(WerewolfParticipantId)
                .Detonation(ScenarioBlastRule.OutsideBlast)
                .SkipWhen(ScenarioSignal.BombDetonationAccepted)
                .WithNag(TextId.ScenarioBomberSafeNag1),
            ScenarioPage.Wait(ScenarioSignal.BombDetonationAccepted),
            ScenarioPage.Say(ScenarioSignal.BombExploded,
                    TextId.ScenarioBomberBoom1, TextId.ScenarioBomberBoom2)
                .At(ScenarioAnchor.PlayHud),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberMeet1, TextId.ScenarioBomberMeet2)
                .At(ScenarioAnchor.PlayHud),
            ScenarioPage.Wait(ScenarioSignal.Advance)
                .Does(ScenarioEntryAction.ConveneAsParticipant, WerewolfParticipantId),
            ScenarioPage.Wait(ScenarioSignal.ConveneAccepted),

            ScenarioPage.Say(ScenarioSignal.MeetingOpened,
                    TextId.ScenarioBomberOpened1, TextId.ScenarioBomberOpened2)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen),
            ScenarioPage.Wait(ScenarioSignal.Advance)
                .At(ScenarioAnchor.ChatLog).WithCue(ScenarioCue.ChatScript)
                .Requires(ScenarioNeed.ChatLogOpen),
            ScenarioPage.Say(ScenarioSignal.ChatScriptDone,
                    TextId.ScenarioBomberVote1, TextId.ScenarioBomberVote2)
                .SkipWhen(ScenarioSignal.VoteAccepted)
                .Requires(ScenarioNeed.MapClosed),
            ScenarioPage.Wait(ScenarioSignal.VoteAccepted).WithCue(ScenarioCue.BotVotes),
            ScenarioPage.Say(ScenarioSignal.TallyRevealed,
                    TextId.ScenarioBomberTally1, TextId.ScenarioBomberTally2)
                .MustBeRead(),

            ScenarioPage.Say(ScenarioSignal.MeetingReleased,
                    TextId.ScenarioBomberReplant1, TextId.ScenarioBomberReplant2, TextId.ScenarioBomberReplant3)
                .At(ScenarioAnchor.PlayHud)
                .BombTarget(TargetParticipantId).MarksBombTarget()
                .Does(ScenarioEntryAction.GrantBombAmmo)
                .Does(ScenarioEntryAction.BodyToGatherSite, WerewolfParticipantId)
                .Does(ScenarioEntryAction.BodyToGatherSite, BystanderParticipantId)
                .Does(ScenarioEntryAction.BodyToPlantSite, TargetParticipantId)
                .Forgets(BombSignals),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioBomberReplant4)
                .At(ScenarioAnchor.PlayHud).PointsAt(ScenarioHighlight.BomberHud)
                .Requires(ScenarioNeed.BombTargetNear)
                .BombTarget(TargetParticipantId).MarksBombTarget()
                .SkipWhen(ScenarioSignal.BombPlanted)
                .WithNag(TextId.ScenarioBomberReplantNag1),

            ScenarioPage.Say(ScenarioSignal.BombPlanted,
                    TextId.ScenarioBomberFire1, TextId.ScenarioBomberFire2)
                .At(ScenarioAnchor.PlayHud)
                .Requires(ScenarioNeed.BombOnTarget).BombTarget(TargetParticipantId)
                .Detonation(ScenarioBlastRule.PointBlank)
                .SkipWhen(ScenarioSignal.BombDetonationAccepted)
                .WithNag(TextId.ScenarioBomberFireNag1),
            ScenarioPage.Wait(ScenarioSignal.BombDetonationAccepted)
                .Marks(ScenarioMilestone.FatalBlastArmed),

            ScenarioPage.Wait(ScenarioSignal.BombExploded)
                .Does(ScenarioEntryAction.KillParticipant, WerewolfParticipantId)
                .Does(ScenarioEntryAction.KillParticipant, BystanderParticipantId),
            ScenarioPage.Say(ScenarioSignal.WinCeremonyDone,
                    TextId.ScenarioBomberResult1, TextId.ScenarioBomberResult2)
                .MustBeRead(),

            ScenarioPage.Say(ScenarioSignal.ResultScreenShown,
                    TextId.ScenarioBomberPostgame1, TextId.ScenarioBomberPostgame2)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberCheck1, TextId.ScenarioBomberCheck2)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberAlly1, TextId.ScenarioBomberAlly2)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberClue1, TextId.ScenarioBomberClue2)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioBomberWrap1, TextId.ScenarioBomberWrap2)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioBomberPostgame3)
                .At(ScenarioAnchor.ReplayEntry),
        };

        private static readonly ScenarioDefinition Def = Build();

        public static ScenarioDefinition Definition => Def;

        private static ScenarioDefinition Build()
        {
            var cast = new ScenarioCast(PlayerParticipantId, Role.Bomber, BotRoster,
                werewolfCount: 2, blackCatChancePercent: 0,
                bomberChancePercent: 100, shamanChancePercent: 0);

            var environment = new ScenarioEnvironmentSpec(
                ScenarioEnvironmentSpec.DefaultLevelName, LevelsCompleted, seededLossPct: 0,
                truckLeashMeters: 0f, truckLeashUntil: null);

            var timing = ScenarioCueTiming.Default;

            var notices = new ScenarioNotices(leashNag: null,
                earlyStartSignal: ScenarioSignal.BombPlanted,
                earlyStartAfter: ScenarioMilestone.ActionPrompted, earlyStartLines: PlantEarly,
                keyHint: default,
                breakLines: new Dictionary<ScenarioBreakKind, TextId[]>
                {
                    [ScenarioBreakKind.AllExtractionsCompleted] = ExtractionBreak,
                    [ScenarioBreakKind.FatalBlastSurvived] = SurvivedBreak,
                },
                wrongTargetLines: WrongTarget, alreadyOnTargetLines: AlreadyOnTarget);

            var watch = new ScenarioBreakWatch(requireTallyBeforeResult: false,
                meetingRequiredFrom: ScenarioSignal.ConveneAccepted,
                meetingRequiredUntil: ScenarioSignal.TallyRevealed,
                breakOnAllExtractionsCompleted: true,
                breakIfFatalBlastSurvived: true);

            var room = new ScenarioRoomSpec(meetingRightsPerPlayer: 0,
                bomberInitialCooldownSec: BomberCooldownSec, bomberCooldownSec: BomberCooldownSec);

            return new ScenarioDefinition(ScenarioId.Bomber, cast, environment,
                FlowPages, timing: timing, notices: notices, watch: watch, room: room,
                meetings: new[] { new ScenarioMeetingScript(Meeting1Script, Meeting1Votes) });
        }
    }
}
