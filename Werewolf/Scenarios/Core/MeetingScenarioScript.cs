using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class MeetingScenarioScript
    {
        public const int PlayerParticipantId = 1;

        public const int WerewolfParticipantId = 3;

        private const int LevelsCompleted = 1;

        private const int SeededLossPct = 4;

        private const float TruckLeashMeters = GameConfig.DefaultTruckSafeRadiusMeters;

        public const string IntroDecodeEmoji =
            "[[cryinglaughing]]🎭🐺[[hush]][[player]]👉[[bang]] 🗳️ ✋️[[player]]💦"
            + "[[police]][[bang]][[deathhead]][[grave]] [[creepycrying]]";

        private static readonly ScenarioParticipant[] BotRoster = ScenarioRoster.Bots(
            (2, "SemibotA", Role.Villager),
            (WerewolfParticipantId, "SemibotB", Role.Werewolf),
            (4, "SemibotC", Role.Villager),
            (5, "SemibotD", Role.Villager));

        private static readonly ScenarioChatLine[] Script =
        {
            new ScenarioChatLine(2, TextId.ScenarioMeetingLine1),
            new ScenarioChatLine(3, TextId.ScenarioMeetingLine2),
            new ScenarioChatLine(4, TextId.ScenarioMeetingLine3),
            new ScenarioChatLine(3, TextId.ScenarioMeetingLine4),
            new ScenarioChatLine(3, TextId.ScenarioMeetingLine5),
            new ScenarioChatLine(3, TextId.ScenarioMeetingLine6),
            new ScenarioChatLine(5, TextId.ScenarioMeetingLine7),
            new ScenarioChatLine(3, TextId.ScenarioMeetingLine8),
            new ScenarioChatLine(2, TextId.ScenarioMeetingLine9),
            new ScenarioChatLine(2, TextId.ScenarioMeetingLine10),
            new ScenarioChatLine(3, TextId.ScenarioMeetingLine11),
            new ScenarioChatLine(3, TextId.ScenarioMeetingLine12),
            new ScenarioChatLine(4, TextId.ScenarioMeetingLine13),
            new ScenarioChatLine(5, TextId.ScenarioMeetingLine14),
        };

        private static readonly ScenarioVote[] Votes =
        {
            new ScenarioVote(2, WerewolfParticipantId),
            new ScenarioVote(4, WerewolfParticipantId),
            new ScenarioVote(5, WerewolfParticipantId),
            new ScenarioVote(WerewolfParticipantId, 2),
        };

        private static readonly TextId[] LeashNag =
        {
            TextId.ScenarioMeetingConveneNag1, TextId.ScenarioMeetingLeashNag,
        };

        private static readonly TextId[] ConveneEarly =
        {
            TextId.ScenarioMeetingConveneEarly1, TextId.ScenarioMeetingConveneEarly2,
        };

        private static readonly ScenarioPage[] FlowPages =
        {
            ScenarioPage.Say(ScenarioSignal.Advance,
                TextId.ScenarioMeetingIntro1, TextId.ScenarioMeetingIntro2, TextId.ScenarioMeetingIntro3),
            ScenarioPage.Say(ScenarioSignal.Advance,
                TextId.ScenarioMeetingIntro4, TextId.ScenarioMeetingIntro5, TextId.ScenarioMeetingIntro6),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioMeetingConvene1, TextId.ScenarioMeetingConvene2)
                .SkipWhen(ScenarioSignal.ConveneAccepted)
                .Marks(ScenarioMilestone.ActionPrompted)
                .WithNag(TextId.ScenarioMeetingConveneNag1, TextId.ScenarioMeetingConveneNag2Format,
                    TextId.ScenarioMeetingConveneNag3Grab),
            ScenarioPage.Wait(ScenarioSignal.ConveneAccepted),

            ScenarioPage.Say(ScenarioSignal.MeetingOpened,
                TextId.ScenarioMeetingOpened1, TextId.ScenarioMeetingOpened2),

            ScenarioPage.Say(ScenarioSignal.Advance,
                TextId.ScenarioMeetingClock1, TextId.ScenarioMeetingClock2),
            ScenarioPage.Say(ScenarioSignal.Advance,
                TextId.ScenarioMeetingClock3, TextId.ScenarioMeetingClock4),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioMeetingGauge1, TextId.ScenarioMeetingGauge2,
                    TextId.ScenarioMeetingGauge3)
                .At(ScenarioAnchor.MeetingGauge).PointsAt(ScenarioHighlight.MeetingGauge),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioMeetingGauge4, TextId.ScenarioMeetingGauge5)
                .At(ScenarioAnchor.MeetingGauge).PointsAt(ScenarioHighlight.MeetingGauge),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioMeetingOpened3, TextId.ScenarioMeetingOpened4)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen)
                .PointsAt(ScenarioHighlight.RecapTitle),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioMeetingOpened5)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen)
                .PointsAt(ScenarioHighlight.RecapDeaths),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioMeetingOpened6)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen)
                .PointsAt(ScenarioHighlight.RecapLost),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioMeetingOpened7)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen)
                .PointsAt(ScenarioHighlight.RecapHaul),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioMeetingOpened8)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen)
                .PointsAt(ScenarioHighlight.RecapBeacon),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioMeetingOpened9)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen)
                .PointsAt(ScenarioHighlight.ChatLogPanel),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioMeetingMap1, TextId.ScenarioMeetingMap2, TextId.ScenarioMeetingMap3)
                .Requires(ScenarioNeed.MapOpen).PointsAt(ScenarioHighlight.MapOverlay),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioMeetingMap4)
                .Requires(ScenarioNeed.MapOpen).PointsAt(ScenarioHighlight.MapOverlay),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioMeetingMap5)
                .At(ScenarioAnchor.MapButton).PointsAt(ScenarioHighlight.MapButton),

            ScenarioPage.Wait(ScenarioSignal.MapClosed)
                .At(ScenarioAnchor.ChatLog).WithCue(ScenarioCue.ChatScript)
                .Requires(ScenarioNeed.ChatLogOpen),

            ScenarioPage.Say(ScenarioSignal.ChatScriptDone,
                    TextId.ScenarioMeetingChatUnavailable1, TextId.ScenarioMeetingChatUnavailable2)
                .At(ScenarioAnchor.ChatLog).Requires(ScenarioNeed.ChatLogOpen),

            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioMeetingVote1, TextId.ScenarioMeetingVote2, TextId.ScenarioMeetingVote3)
                .SkipWhen(ScenarioSignal.VoteAccepted)
                .Requires(ScenarioNeed.MapClosed),
            ScenarioPage.Wait(ScenarioSignal.VoteAccepted).WithCue(ScenarioCue.BotVotes),

            ScenarioPage.Say(ScenarioSignal.TallyRevealed, TextId.ScenarioMeetingTally),
            ScenarioPage.Say(ScenarioSignal.WinCeremonyDone,
                    TextId.ScenarioMeetingResult1, TextId.ScenarioMeetingResult2)
                .MustBeRead(),

            ScenarioPage.Say(ScenarioSignal.ResultScreenShown,
                    TextId.ScenarioMeetingPostgame1, TextId.ScenarioMeetingPostgame2)
                .At(ScenarioAnchor.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance,
                    TextId.ScenarioMeetingReplay1, TextId.ScenarioMeetingReplay2)
                .At(ScenarioAnchor.ReplayEntry)
                .PointsAt(ScenarioHighlight.ReplayEntry),
            ScenarioPage.Say(ScenarioSignal.Advance, TextId.ScenarioMeetingPostgame3)
                .At(ScenarioAnchor.ReplayEntry),
        };

        private static readonly ScenarioDefinition Def = Build();

        public static ScenarioDefinition Definition => Def;

        private static ScenarioDefinition Build()
        {
            var cast = new ScenarioCast(PlayerParticipantId, Role.Villager, BotRoster,
                werewolfCount: 1, blackCatChancePercent: 0,
                bomberChancePercent: 0, shamanChancePercent: 0);

            var environment = new ScenarioEnvironmentSpec(
                ScenarioEnvironmentSpec.DefaultLevelName, LevelsCompleted, SeededLossPct,
                TruckLeashMeters, truckLeashUntil: ScenarioSignal.ConveneAccepted);

            var timing = ScenarioCueTiming.Default;

            var notices = new ScenarioNotices(LeashNag,
                earlyStartSignal: ScenarioSignal.ConveneAccepted,
                earlyStartAfter: ScenarioMilestone.ActionPrompted, earlyStartLines: ConveneEarly,
                keyHint: new ScenarioKeyHint(TextId.ScenarioMeetingConveneNag2Format,
                    TextId.ScenarioMeetingConveneNag3Grab, TextId.ScenarioMeetingConveneNag3Interact));

            var watch = new ScenarioBreakWatch(requireTallyBeforeResult: true,
                meetingRequiredFrom: ScenarioSignal.ConveneAccepted,
                meetingRequiredUntil: ScenarioSignal.TallyRevealed);

            var room = new ScenarioRoomSpec(meetingRightsPerPlayer: 1);

            return new ScenarioDefinition(ScenarioId.MeetingVote, cast, environment,
                FlowPages, timing: timing, notices: notices, watch: watch, room: room,
                meetings: new[] { new ScenarioMeetingScript(Script, Votes) },
                introDecodeEmoji: IntroDecodeEmoji);
        }
    }
}
