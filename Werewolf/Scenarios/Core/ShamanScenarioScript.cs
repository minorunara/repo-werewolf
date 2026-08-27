using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ShamanScenarioScript
    {
        public const int PlayerParticipantId = 1;

        public const int WerewolfParticipantId = 3;

        public const int FirstCorpseParticipantId = 7;

        public const int SecondCorpseParticipantId = 2;

        private const int LevelsCompleted = 2;

        private const int ModuleAmount = ScenarioLevelSizeRules.MaxModuleAmount;

        private const int ExtractionPoints = 2;

        private const float TruckLeashMeters = GameConfig.DefaultTruckSafeRadiusMeters;

        public const long CorpseHintDelayMs = 120_000L;

        private static readonly Dictionary<string, int> StartUpgrades = new Dictionary<string, int>
        {
            { WorldgenUpgrades.ExtraJump, 20 },
            { WorldgenUpgrades.TumbleClimb, 20 },
            { WorldgenUpgrades.Stamina, 20 },
            { WorldgenUpgrades.CrouchRest, 20 },
            { WorldgenUpgrades.Speed, 2 },
        };

        private const int MaxQuotaBagReissues = 5;

        private static readonly ScenarioParticipant[] BotRoster = ScenarioRoster.Bots(
            (SecondCorpseParticipantId, "SemibotA", Role.Villager),
            (WerewolfParticipantId, "SemibotB", Role.Werewolf),
            (4, "SemibotC", Role.Villager),
            (5, "SemibotD", Role.Villager),
            (6, "SemibotE", Role.Villager),
            (FirstCorpseParticipantId, "SemibotF", Role.Villager));

        private static readonly ScenarioChatLine[] Meeting1Script =
        {
            new ScenarioChatLine(2, TextId.ScenarioShamanLineA1),
            new ScenarioChatLine(4, TextId.ScenarioShamanLineA2),
            new ScenarioChatLine(5, TextId.ScenarioShamanLineA3),
            new ScenarioChatLine(6, TextId.ScenarioShamanLineA4),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineA5),
            new ScenarioChatLine(2, TextId.ScenarioShamanLineA6),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineA7),
            new ScenarioChatLine(6, TextId.ScenarioShamanLineA8),
            new ScenarioChatLine(5, TextId.ScenarioShamanLineA9),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineA10),
            new ScenarioChatLine(5, TextId.ScenarioShamanLineA11),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineA12),
            new ScenarioChatLine(4, TextId.ScenarioShamanLineA13),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineA14),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineA15),
            new ScenarioChatLine(2, TextId.ScenarioShamanLineA16),
            new ScenarioChatLine(6, TextId.ScenarioShamanLineA17),
            new ScenarioChatLine(6, TextId.ScenarioShamanLineA18),
            new ScenarioChatLine(2, TextId.ScenarioShamanLineA19),
            new ScenarioChatLine(6, TextId.ScenarioShamanLineA20),
        };

        private static readonly ScenarioVote[] Meeting1Votes =
        {
            new ScenarioVote(2, WerewolfParticipantId),
            ScenarioVote.Skip(WerewolfParticipantId),
            ScenarioVote.Skip(4),
            ScenarioVote.Skip(5),
            ScenarioVote.Skip(6),
        };

        private static readonly ScenarioChatLine[] Meeting2Script =
        {
            new ScenarioChatLine(4, TextId.ScenarioShamanLineB1),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineB2),
            new ScenarioChatLine(4, TextId.ScenarioShamanLineB3),
            new ScenarioChatLine(5, TextId.ScenarioShamanLineB4),
            new ScenarioChatLine(6, TextId.ScenarioShamanLineB5),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineB6),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineB7),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineB8),
            new ScenarioChatLine(6, TextId.ScenarioShamanLineB9),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineB10),
            new ScenarioChatLine(5, TextId.ScenarioShamanLineB11),
            new ScenarioChatLine(6, TextId.ScenarioShamanLineB12),
            new ScenarioChatLine(3, TextId.ScenarioShamanLineB13),
            new ScenarioChatLine(4, TextId.ScenarioShamanLineB14),
        };

        private static readonly ScenarioVote[] Meeting2Votes =
        {
            new ScenarioVote(4, WerewolfParticipantId),
            new ScenarioVote(5, WerewolfParticipantId),
            new ScenarioVote(6, WerewolfParticipantId),
            new ScenarioVote(WerewolfParticipantId, PlayerParticipantId),
        };

        private static readonly ScenarioSignal[] MeetingSignals =
        {
            ScenarioSignal.ConveneAccepted, ScenarioSignal.MeetingOpened,
            ScenarioSignal.ChatScriptDone, ScenarioSignal.VoteAccepted,
            ScenarioSignal.TallyRevealed, ScenarioSignal.MeetingReleased,
            ScenarioSignal.WinCeremonyDone,
        };

        private static readonly TextId[] LeashNag =
        {
            TextId.ScenarioShamanLeashNag1, TextId.ScenarioShamanLeashNag2,
        };

        private static readonly TextId[] CorpseHintLines =
        {
            TextId.ScenarioShamanCorpseHint1, TextId.ScenarioShamanCorpseHint2,
        };

        private static readonly TextId[] MapAside =
        {
            TextId.ScenarioShamanMapAside1, TextId.ScenarioShamanMapAside2,
            TextId.ScenarioShamanMapAside3,
        };

        private static readonly TextId[] ReportEarly =
        {
            TextId.ScenarioShamanEarly1, TextId.ScenarioShamanEarly2,
        };

        private static readonly TextId[] ExtractionBreak =
        {
            TextId.ScenarioShamanExtractionBreak1, TextId.ScenarioShamanExtractionBreak2,
            TextId.ScenarioShamanExtractionBreak3,
        };

        private static readonly TextId[] EarlyExtractionBreak =
        {
            TextId.ScenarioShamanEarlyExtractionBreak1, TextId.ScenarioShamanEarlyExtractionBreak2,
            TextId.ScenarioShamanEarlyExtractionBreak3,
        };

        private static readonly TextId[] QuotaBagBreak =
        {
            TextId.ScenarioShamanQuotaBagBreak1, TextId.ScenarioShamanQuotaBagBreak2,
            TextId.ScenarioShamanQuotaBagBreak3,
        };

        private static readonly ScenarioPage[] FlowPages =
        {
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioShamanIntro1, TextId.ScenarioShamanIntro2, TextId.ScenarioShamanIntro3)
                .At(ScenarioAnchor.PlayHud),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioShamanIntro4, TextId.ScenarioShamanIntro5)
                .At(ScenarioAnchor.PlayHud),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioShamanTrance1, TextId.ScenarioShamanTrance2, TextId.ScenarioShamanTrance3)
                .At(ScenarioAnchor.PlayHud)
                .SkipWhen(ScenarioSignal.ConveneAccepted),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioShamanTranceOn1, TextId.ScenarioShamanTranceOn2)
                .At(ScenarioAnchor.PlayHud)
                .Requires(ScenarioNeed.ShamanTranceActive)
                .SkipWhen(ScenarioSignal.ShamanStormEntered, ScenarioSignal.ConveneAccepted),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioShamanDrip1, TextId.ScenarioShamanDrip2)
                .At(ScenarioAnchor.PlayHud)
                .SkipWhen(ScenarioSignal.ShamanStormEntered, ScenarioSignal.ConveneAccepted),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioShamanDowse1, TextId.ScenarioShamanDowse2)
                .At(ScenarioAnchor.PlayHud)
                .Does(ScenarioEntryAction.PlaceFarCorpse, FirstCorpseParticipantId)
                .Marks(ScenarioMilestone.Roaming)
                .SkipWhen(ScenarioSignal.ShamanGhostFired, ScenarioSignal.ShamanStormEntered,
                    ScenarioSignal.ConveneAccepted)
                .WithNag(TextId.ScenarioShamanDowseNag1, TextId.ScenarioShamanDowseNag2),
            ScenarioPage.Say(ScenarioSignal.ShamanGhostFired,
                    TextId.ScenarioShamanBurst1, TextId.ScenarioShamanBurst2,
                    TextId.ScenarioShamanBurst3)
                .At(ScenarioAnchor.PlayHud)
                .SkipWhen(ScenarioSignal.ShamanStormEntered, ScenarioSignal.ConveneAccepted),

            ScenarioPage.Say(ScenarioSignal.ShamanStormEntered,
                    TextId.ScenarioShamanStorm1, TextId.ScenarioShamanStorm2, TextId.ScenarioShamanStorm3)
                .At(ScenarioAnchor.PlayHud)
                .Requires(ScenarioNeed.ShamanStormActive)
                .SkipWhen(ScenarioSignal.ConveneAccepted),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioShamanReport1, TextId.ScenarioShamanReport2)
                .At(ScenarioAnchor.PlayHudRight).PointsAt(ScenarioHighlight.ReportIcon)
                .SkipWhen(ScenarioSignal.ConveneAccepted)
                .Marks(ScenarioMilestone.ActionPrompted)
                .WithNag(TextId.ScenarioShamanReportNag1),
            ScenarioPage.Wait(ScenarioSignal.ConveneAccepted),

            ScenarioPage.Say(ScenarioSignal.MeetingOpened,
                    TextId.ScenarioShamanOpened1, TextId.ScenarioShamanOpened2, TextId.ScenarioShamanOpened3)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen),
            ScenarioPage.Wait(ScenarioSignal.Advance)
                .At(ScenarioAnchor.ChatLog).WithCue(ScenarioCue.ChatScript)
                .Requires(ScenarioNeed.ChatLogOpen),
            ScenarioPage.Say(ScenarioSignal.ChatScriptDone,
                    TextId.ScenarioShamanVote1, TextId.ScenarioShamanVote2)
                .SkipWhen(ScenarioSignal.VoteAccepted)
                .Requires(ScenarioNeed.MapClosed),
            ScenarioPage.Wait(ScenarioSignal.VoteAccepted).WithCue(ScenarioCue.BotVotes),
            ScenarioPage.Say(ScenarioSignal.TallyRevealed,
                    TextId.ScenarioShamanTally1, TextId.ScenarioShamanTally2)
                .Forgets(MeetingSignals)
                .Marks(ScenarioMilestone.TruckBound)
                .MustBeRead(),

            ScenarioPage.Say(ScenarioSignal.MeetingReleased,
                    TextId.ScenarioShamanDeliver1)
                .At(ScenarioAnchor.PlayHud)
                .Does(ScenarioEntryAction.SpawnQuotaBag)
                .SkipWhen(ScenarioSignal.LastRunReached)
                .WithNag(TextId.ScenarioShamanDeliverNag1),

            ScenarioPage.Say(ScenarioSignal.LastRunReached,
                    TextId.ScenarioShamanLastRun1, TextId.ScenarioShamanLastRun2, TextId.ScenarioShamanLastRun3)
                .At(ScenarioAnchor.PlayHud)
                .Does(ScenarioEntryAction.PlaceTruckCorpse, SecondCorpseParticipantId),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioShamanBlocked1)
                .At(ScenarioAnchor.PlayHudRight).PointsAt(ScenarioHighlight.ReportIcon)
                .Requires(ScenarioNeed.CorpseNear)
                .SkipWhen(ScenarioSignal.CorpseReportDenied)
                .WithNag(TextId.ScenarioShamanBlockedNag1),
            ScenarioPage.Say(ScenarioSignal.CorpseReportDenied,
                    TextId.ScenarioShamanDenied1, TextId.ScenarioShamanDenied2)
                .At(ScenarioAnchor.PlayHud),

            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioShamanButton1)
                .At(ScenarioAnchor.PlayHud)
                .Does(ScenarioEntryAction.GrantMeetingRight)
                .SkipWhen(ScenarioSignal.ConveneAccepted)
                .WithNag(TextId.ScenarioShamanButtonNag1, TextId.ScenarioShamanButtonNag2Format,
                    TextId.ScenarioShamanButtonNag3Grab),
            ScenarioPage.Wait(ScenarioSignal.ConveneAccepted),

            ScenarioPage.Say(ScenarioSignal.MeetingOpened,
                    TextId.ScenarioShamanOpened4, TextId.ScenarioShamanOpened5)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen),
            ScenarioPage.Wait(ScenarioSignal.Advance)
                .At(ScenarioAnchor.ChatLog).WithCue(ScenarioCue.ChatScript).InMeeting(1)
                .Requires(ScenarioNeed.ChatLogOpen),
            ScenarioPage.Say(ScenarioSignal.ChatScriptDone,
                    TextId.ScenarioShamanVote3, TextId.ScenarioShamanVote4)
                .SkipWhen(ScenarioSignal.VoteAccepted)
                .Requires(ScenarioNeed.MapClosed),
            ScenarioPage.Wait(ScenarioSignal.VoteAccepted).WithCue(ScenarioCue.BotVotes).InMeeting(1),
            ScenarioPage.Say(ScenarioSignal.TallyRevealed, TextId.ScenarioShamanTally3),
            ScenarioPage.Say(ScenarioSignal.WinCeremonyDone,
                    TextId.ScenarioShamanResult1, TextId.ScenarioShamanResult2)
                .MustBeRead(),

            ScenarioPage.Say(ScenarioSignal.ResultScreenShown,
                    TextId.ScenarioShamanPostgame1, TextId.ScenarioShamanPostgame2)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioShamanReplay1, TextId.ScenarioShamanReplay2)
                .At(ScenarioAnchor.ReplayEntry)
                .PointsAt(ScenarioHighlight.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioShamanWrap1, TextId.ScenarioShamanWrap2,
                    TextId.ScenarioShamanWrap3)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioShamanPostgame3)
                .At(ScenarioAnchor.ReplayEntry),
        };

        private static readonly ScenarioDefinition Def = Build();

        public static ScenarioDefinition Definition => Def;

        private static ScenarioDefinition Build()
        {
            var cast = new ScenarioCast(PlayerParticipantId, Role.Shaman, BotRoster,
                werewolfCount: 1, blackCatChancePercent: 0,
                bomberChancePercent: 0, shamanChancePercent: 0);

            var environment = new ScenarioEnvironmentSpec(
                ScenarioEnvironmentSpec.DefaultLevelName, LevelsCompleted, seededLossPct: 0,
                TruckLeashMeters, truckLeashUntil: ScenarioSignal.ConveneAccepted,
                startUpgrades: StartUpgrades,
                truckLeashAfter: ScenarioMilestone.TruckBound,
                truckLeashBefore: ScenarioMilestone.Roaming,
                moduleAmount: ModuleAmount, extractionPoints: ExtractionPoints);

            var timing = ScenarioCueTiming.Default;

            var notices = new ScenarioNotices(LeashNag,
                earlyStartSignal: ScenarioSignal.ConveneAccepted,
                earlyStartAfter: ScenarioMilestone.ActionPrompted, earlyStartLines: ReportEarly,
                keyHint: new ScenarioKeyHint(TextId.ScenarioShamanButtonNag2Format,
                    TextId.ScenarioShamanButtonNag3Grab, TextId.ScenarioShamanButtonNag3Interact),
                mapAside: MapAside,
                breakLines: new Dictionary<ScenarioBreakKind, TextId[]>
                {
                    [ScenarioBreakKind.AllExtractionsCompleted] = ExtractionBreak,
                    [ScenarioBreakKind.EarlyExtraction] = EarlyExtractionBreak,
                    [ScenarioBreakKind.QuotaBagLost] = QuotaBagBreak,
                });

            var watch = new ScenarioBreakWatch(requireTallyBeforeResult: true,
                meetingRequiredFrom: ScenarioSignal.ConveneAccepted,
                meetingRequiredUntil: ScenarioSignal.TallyRevealed,
                breakOnAllExtractionsCompleted: true,
                extractionBreaksBefore: ScenarioMilestone.TruckBound,
                quotaBagUntil: ScenarioSignal.LastRunReached,
                maxQuotaBagReissues: MaxQuotaBagReissues);

            var room = new ScenarioRoomSpec(meetingRightsPerPlayer: 0);

            var corpseHint = new ScenarioCorpseHint(FirstCorpseParticipantId,
                after: ScenarioSignal.ShamanStormEntered, until: ScenarioSignal.ConveneAccepted,
                delayMs: CorpseHintDelayMs, lines: CorpseHintLines);

            return new ScenarioDefinition(ScenarioId.ShamanSense, cast, environment,
                FlowPages, timing: timing, notices: notices, watch: watch, room: room,
                meetings: new[]
                {
                    new ScenarioMeetingScript(Meeting1Script, Meeting1Votes),
                    new ScenarioMeetingScript(Meeting2Script, Meeting2Votes),
                },
                corpseHint: corpseHint);
        }
    }
}
