using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public readonly struct ScenarioParticipant
    {
        public ScenarioParticipant(int actorNumber, int participantId, string name, Role role)
        {
            ActorNumber = actorNumber;
            ParticipantId = participantId;
            Name = name;
            Role = role;
        }

        public int ActorNumber { get; }

        public int ParticipantId { get; }

        public string Name { get; }

        public Role Role { get; }
    }

    public readonly struct ScenarioChatLine
    {
        public ScenarioChatLine(int participantId, TextId text)
        {
            ParticipantId = participantId;
            Text = text;
        }

        public int ParticipantId { get; }

        public TextId Text { get; }
    }

    public readonly struct ScenarioVote
    {
        public const int SkipTargetId = 0;

        public ScenarioVote(int voterParticipantId, int targetParticipantId)
        {
            VoterParticipantId = voterParticipantId;
            TargetParticipantId = targetParticipantId;
        }

        public static ScenarioVote Skip(int voterParticipantId)
            => new ScenarioVote(voterParticipantId, SkipTargetId);

        public int VoterParticipantId { get; }

        public int TargetParticipantId { get; }

        public bool IsSkip => TargetParticipantId == SkipTargetId;

        public int TargetActor(ScenarioCast cast, int playerActor)
            => IsSkip ? -1 : cast.ActorOf(TargetParticipantId, playerActor);
    }

    public readonly struct ScenarioMeetingScript
    {
        private static readonly ScenarioChatLine[] NoChat = new ScenarioChatLine[0];
        private static readonly ScenarioVote[] NoVotes = new ScenarioVote[0];

        private readonly ScenarioChatLine[] _chat;
        private readonly ScenarioVote[] _votes;

        public ScenarioMeetingScript(ScenarioChatLine[] chatScript, ScenarioVote[] botVotes)
        {
            _chat = chatScript ?? NoChat;
            _votes = botVotes ?? NoVotes;
        }

        public IReadOnlyList<ScenarioChatLine> ChatScript => _chat ?? NoChat;

        public IReadOnlyList<ScenarioVote> BotVotes => _votes ?? NoVotes;
    }

    public readonly struct ScenarioCast
    {
        private static readonly ScenarioParticipant[] NoBots = new ScenarioParticipant[0];

        private readonly ScenarioParticipant[] _bots;

        public ScenarioCast(int playerParticipantId, Role playerRole, ScenarioParticipant[] bots,
                            int werewolfCount, int blackCatChancePercent,
                            int bomberChancePercent, int shamanChancePercent)
        {
            PlayerParticipantId = playerParticipantId;
            PlayerRole = playerRole;
            _bots = bots ?? NoBots;
            WerewolfCount = werewolfCount;
            BlackCatChancePercent = blackCatChancePercent;
            BomberChancePercent = bomberChancePercent;
            ShamanChancePercent = shamanChancePercent;
        }

        public int PlayerParticipantId { get; }

        public Role PlayerRole { get; }

        public IReadOnlyList<ScenarioParticipant> Bots => _bots ?? NoBots;

        public int WerewolfCount { get; }

        public int BlackCatChancePercent { get; }

        public int BomberChancePercent { get; }

        public int ShamanChancePercent { get; }

        public bool TryGetBot(int participantId, out ScenarioParticipant bot)
        {
            ScenarioParticipant[] bots = _bots ?? NoBots;
            for (int i = 0; i < bots.Length; i++)
            {
                if (bots[i].ParticipantId != participantId) continue;
                bot = bots[i];
                return true;
            }
            bot = default;
            return false;
        }

        public int ActorOf(int participantId, int playerActor)
        {
            if (participantId == PlayerParticipantId) return playerActor;
            return TryGetBot(participantId, out var bot) ? bot.ActorNumber : 0;
        }
    }

    public readonly struct ScenarioEnvironmentSpec
    {
        public const string DefaultLevelName = ScenarioVanillaNames.MuseumLevel;

        private static readonly IReadOnlyDictionary<string, int> NoUpgrades =
            new Dictionary<string, int>();

        private readonly IReadOnlyDictionary<string, int> _startUpgrades;

        public ScenarioEnvironmentSpec(string forcedLevelName, int levelsCompleted, int seededLossPct,
                                       float truckLeashMeters, ScenarioSignal? truckLeashUntil,
                                       int haulGoalPctOfTotal = 0,
                                       IReadOnlyDictionary<string, int> startUpgrades = null,
                                       ScenarioMilestone truckLeashAfter = ScenarioMilestone.None,
                                       ScenarioMilestone truckLeashBefore = ScenarioMilestone.None,
                                       int moduleAmount = 0, int extractionPoints = 0,
                                       EnemySetCounts? enemySets = null)
        {
            ForcedLevelName = forcedLevelName;
            EnemySets = enemySets ?? EnemySetCounts.Untouched;
            LevelsCompleted = levelsCompleted;
            SeededLossPct = seededLossPct;
            TruckLeashMeters = truckLeashMeters;
            TruckLeashUntil = truckLeashUntil;
            HaulGoalPctOfTotal = haulGoalPctOfTotal;
            _startUpgrades = startUpgrades ?? NoUpgrades;
            TruckLeashAfter = truckLeashAfter;
            TruckLeashBefore = truckLeashBefore;
            ModuleAmount = moduleAmount;
            ExtractionPoints = extractionPoints;
        }

        public string ForcedLevelName { get; }

        public int LevelsCompleted { get; }

        public int SeededLossPct { get; }

        public float TruckLeashMeters { get; }

        public ScenarioSignal? TruckLeashUntil { get; }

        public ScenarioMilestone TruckLeashAfter { get; }

        public ScenarioMilestone TruckLeashBefore { get; }

        public int HaulGoalPctOfTotal { get; }

        public IReadOnlyDictionary<string, int> StartUpgrades => _startUpgrades ?? NoUpgrades;

        public int ModuleAmount { get; }

        public int ExtractionPoints { get; }

        public EnemySetCounts EnemySets { get; }

        public bool TruckLeashEnabled => TruckLeashMeters > 0f;

        public bool HaulGoalOverridden => HaulGoalPctOfTotal > 0;

        public bool LevelSizeOverridden => ModuleAmount > 0 || ExtractionPoints > 0;
    }

    public readonly struct ScenarioSafetySpec
    {
        public static readonly ScenarioSafetySpec Sheltered =
            new ScenarioSafetySpec(true, true, false, 0);

        public ScenarioSafetySpec(bool suppressEnemies, bool invincible, bool clampDamage,
                                  int grabReleaseMs)
        {
            SuppressEnemies = suppressEnemies;
            Invincible = invincible;
            ClampDamage = clampDamage;
            GrabReleaseMs = grabReleaseMs;
        }

        public bool SuppressEnemies { get; }

        public bool Invincible { get; }

        public bool ClampDamage { get; }

        public int GrabReleaseMs { get; }

        public bool GrabReleaseEnabled => GrabReleaseMs > 0;
    }

    public readonly struct ScenarioRoomSpec
    {
        public ScenarioRoomSpec(int? meetingRightsPerPlayer = null,
                                int? staminaUnlockPct = null, int? jumpUnlockPct = null,
                                int? enemyIgnoreUnlockPct = null, int? healUnlockPct = null,
                                int? beaconChargePct = null, int? beaconSuppressStartSec = null,
                                int? beaconSuppressAfterMeetingSec = null,
                                int? beaconCooldownSec = null,
                                int? bomberInitialCooldownSec = null,
                                int? bomberCooldownSec = null)
        {
            MeetingRightsPerPlayer = meetingRightsPerPlayer;
            StaminaUnlockPct = staminaUnlockPct;
            JumpUnlockPct = jumpUnlockPct;
            EnemyIgnoreUnlockPct = enemyIgnoreUnlockPct;
            HealUnlockPct = healUnlockPct;
            BeaconChargePct = beaconChargePct;
            BeaconSuppressStartSec = beaconSuppressStartSec;
            BeaconSuppressAfterMeetingSec = beaconSuppressAfterMeetingSec;
            BeaconCooldownSec = beaconCooldownSec;
            BomberInitialCooldownSec = bomberInitialCooldownSec;
            BomberCooldownSec = bomberCooldownSec;
        }

        public int? MeetingRightsPerPlayer { get; }

        public int? StaminaUnlockPct { get; }

        public int? JumpUnlockPct { get; }

        public int? EnemyIgnoreUnlockPct { get; }

        public int? HealUnlockPct { get; }

        public int? BeaconChargePct { get; }

        public int? BeaconSuppressStartSec { get; }

        public int? BeaconSuppressAfterMeetingSec { get; }

        public int? BeaconCooldownSec { get; }

        public int? BomberInitialCooldownSec { get; }

        public int? BomberCooldownSec { get; }
    }

    public readonly struct ScenarioCueTiming
    {
        public static readonly ScenarioCueTiming Default =
            new ScenarioCueTiming(chatStartDelayMs: 700, chatBaseDelayMs: 2000, chatPerCharMs: 50,
                                  chatMaxDelayMs: 4500, botVoteIntervalMs: 800,
                                  actionNagDelayMs: 25000);

        public ScenarioCueTiming(int chatStartDelayMs, int chatBaseDelayMs, int chatPerCharMs,
                                 int chatMaxDelayMs, int botVoteIntervalMs, int actionNagDelayMs)
        {
            ChatStartDelayMs = chatStartDelayMs;
            ChatBaseDelayMs = chatBaseDelayMs;
            ChatPerCharMs = chatPerCharMs;
            ChatMaxDelayMs = chatMaxDelayMs;
            BotVoteIntervalMs = botVoteIntervalMs;
            ActionNagDelayMs = actionNagDelayMs;
        }

        public int ChatStartDelayMs { get; }

        public int ChatBaseDelayMs { get; }

        public int ChatPerCharMs { get; }

        public int ChatMaxDelayMs { get; }

        public int BotVoteIntervalMs { get; }

        public int ActionNagDelayMs { get; }

        public long StartDelayMs(ScenarioCue cue)
        {
            switch (cue)
            {
                case ScenarioCue.ChatScript: return ChatStartDelayMs;
                case ScenarioCue.BotVotes: return BotVoteIntervalMs;
                case ScenarioCue.ActionNag: return ActionNagDelayMs;
                default: return 0;
            }
        }

        public int ChatDelayMs(string text)
        {
            int length = text != null ? text.Length : 0;
            int delay = ChatBaseDelayMs + length * ChatPerCharMs;
            return delay < ChatMaxDelayMs ? delay : ChatMaxDelayMs;
        }
    }

    public readonly struct ScenarioKeyHint
    {
        public ScenarioKeyHint(TextId formatId, TextId grabNoteId, TextId interactNoteId)
        {
            FormatId = formatId;
            GrabNoteId = grabNoteId;
            InteractNoteId = interactNoteId;
            HasValue = true;
        }

        public TextId FormatId { get; }

        public TextId GrabNoteId { get; }

        public TextId InteractNoteId { get; }

        public bool HasValue { get; }
    }

    public enum ScenarioBreakKind : byte
    {
        ResultBeforeTally = 0,

        MeetingLost = 1,

        BotVoteRejected = 2,

        AllExtractionsCompleted = 3,

        EarlyExtraction = 4,

        QuotaBagLost = 5,

        FatalBlastSurvived = 6,
    }

    public readonly struct ScenarioNotices
    {
        private static readonly TextId[] NoLines = new TextId[0];
        private static readonly int BreakKindCount = Enum.GetValues(typeof(ScenarioBreakKind)).Length;

        private readonly TextId[] _leashNag;
        private readonly TextId[] _earlyStart;
        private readonly TextId[] _extractionNag;
        private readonly TextId[] _mapAside;
        private readonly TextId[] _wrongTarget;
        private readonly TextId[] _alreadyOnTarget;
        private readonly TextId[][] _breakLines;

        public ScenarioNotices(TextId[] leashNag, ScenarioSignal? earlyStartSignal,
                               ScenarioMilestone earlyStartAfter, TextId[] earlyStartLines,
                               ScenarioKeyHint keyHint, TextId[] extractionNag = null,
                               TextId[] mapAside = null,
                               IReadOnlyDictionary<ScenarioBreakKind, TextId[]> breakLines = null,
                               TextId[] wrongTargetLines = null,
                               TextId[] alreadyOnTargetLines = null)
        {
            _leashNag = leashNag ?? NoLines;
            EarlyStartSignal = earlyStartSignal;
            EarlyStartAfter = earlyStartAfter;
            _earlyStart = earlyStartLines ?? NoLines;
            KeyHint = keyHint;
            _extractionNag = extractionNag ?? NoLines;
            _mapAside = mapAside ?? NoLines;
            _wrongTarget = wrongTargetLines ?? NoLines;
            _alreadyOnTarget = alreadyOnTargetLines ?? NoLines;
            _breakLines = new TextId[BreakKindCount][];
            if (breakLines != null)
            {
                foreach (KeyValuePair<ScenarioBreakKind, TextId[]> pair in breakLines)
                {
                    _breakLines[(int)pair.Key] = pair.Value;
                }
            }
        }

        public IReadOnlyList<TextId> LeashNag => _leashNag ?? NoLines;

        public ScenarioSignal? EarlyStartSignal { get; }

        public ScenarioMilestone EarlyStartAfter { get; }

        public IReadOnlyList<TextId> EarlyStartLines => _earlyStart ?? NoLines;

        public ScenarioKeyHint KeyHint { get; }

        public IReadOnlyList<TextId> ExtractionNag => _extractionNag ?? NoLines;

        public IReadOnlyList<TextId> BreakLines(ScenarioBreakKind kind)
        {
            TextId[][] table = _breakLines;
            int index = (int)kind;
            if (table == null || index < 0 || index >= table.Length) return NoLines;
            return table[index] ?? NoLines;
        }

        public IReadOnlyList<TextId> MapAside => _mapAside ?? NoLines;

        public IReadOnlyList<TextId> WrongTargetLines => _wrongTarget ?? NoLines;

        public IReadOnlyList<TextId> AlreadyOnTargetLines => _alreadyOnTarget ?? NoLines;
    }

    public readonly struct ScenarioBreakWatch
    {
        public ScenarioBreakWatch(bool requireTallyBeforeResult,
                                  ScenarioSignal? meetingRequiredFrom,
                                  ScenarioSignal? meetingRequiredUntil,
                                  bool breakOnAllExtractionsCompleted = false,
                                  ScenarioMilestone extractionBreaksBefore = ScenarioMilestone.None,
                                  ScenarioSignal? quotaBagUntil = null,
                                  int maxQuotaBagReissues = 0,
                                  bool breakIfFatalBlastSurvived = false)
        {
            RequireTallyBeforeResult = requireTallyBeforeResult;
            MeetingRequiredFrom = meetingRequiredFrom;
            MeetingRequiredUntil = meetingRequiredUntil;
            BreakOnAllExtractionsCompleted = breakOnAllExtractionsCompleted;
            ExtractionBreaksBefore = extractionBreaksBefore;
            QuotaBagUntil = quotaBagUntil;
            MaxQuotaBagReissues = maxQuotaBagReissues;
            BreakIfFatalBlastSurvived = breakIfFatalBlastSurvived;
        }

        public bool RequireTallyBeforeResult { get; }

        public ScenarioSignal? MeetingRequiredFrom { get; }

        public ScenarioSignal? MeetingRequiredUntil { get; }

        public bool BreakOnAllExtractionsCompleted { get; }

        public ScenarioMilestone ExtractionBreaksBefore { get; }

        public ScenarioSignal? QuotaBagUntil { get; }

        public int MaxQuotaBagReissues { get; }

        public bool BreakIfFatalBlastSurvived { get; }
    }

    public sealed class ScenarioDefinition
    {
        private readonly ScenarioPage[] _pages;
        private readonly ScenarioMeetingScript[] _meetings;

        public ScenarioDefinition(ScenarioId id, ScenarioCast cast, ScenarioEnvironmentSpec environment,
                                  ScenarioPage[] pages, ScenarioCueTiming timing,
                                  ScenarioNotices notices, ScenarioBreakWatch watch,
                                  ScenarioSafetySpec? safety = null, ScenarioRoomSpec room = default,
                                  ScenarioMeetingScript[] meetings = null,
                                  string introDecodeEmoji = null,
                                  ScenarioCorpseHint corpseHint = default)
        {
            if (pages == null) throw new ArgumentNullException(nameof(pages));
            if (pages.Length == 0) throw new ArgumentException("pages", nameof(pages));

            Id = id;
            Cast = cast;
            Environment = environment;
            _pages = ExpandNagPages(pages);
            _meetings = meetings != null && meetings.Length > 0
                ? meetings
                : new[] { new ScenarioMeetingScript(null, null) };
            Timing = timing;
            Notices = notices;
            Watch = watch;
            Safety = safety ?? ScenarioSafetySpec.Sheltered;
            Room = room;
            IntroDecodeEmoji = string.IsNullOrEmpty(introDecodeEmoji) ? null : introDecodeEmoji;
            CorpseHint = corpseHint;
        }

        public ScenarioId Id { get; }

        public ScenarioCast Cast { get; }

        public ScenarioEnvironmentSpec Environment { get; }

        public IReadOnlyList<ScenarioPage> Pages => _pages;

        public IReadOnlyList<ScenarioMeetingScript> Meetings => _meetings;

        public ScenarioMeetingScript MeetingScript(int meetingIndex)
            => meetingIndex >= 0 && meetingIndex < _meetings.Length ? _meetings[meetingIndex] : default;

        public string IntroDecodeEmoji { get; }

        public ScenarioCorpseHint CorpseHint { get; }

        public bool CanBreak(ScenarioBreakKind kind)
        {
            switch (kind)
            {
                case ScenarioBreakKind.ResultBeforeTally: return Watch.RequireTallyBeforeResult;
                case ScenarioBreakKind.MeetingLost: return Watch.MeetingRequiredFrom.HasValue;
                case ScenarioBreakKind.BotVoteRejected:
                    for (int i = 0; i < _meetings.Length; i++)
                    {
                        if (_meetings[i].BotVotes.Count > 0) return true;
                    }
                    return false;
                case ScenarioBreakKind.AllExtractionsCompleted: return Watch.BreakOnAllExtractionsCompleted;
                case ScenarioBreakKind.EarlyExtraction: return Watch.ExtractionBreaksBefore != ScenarioMilestone.None;
                case ScenarioBreakKind.QuotaBagLost: return Watch.MaxQuotaBagReissues > 0;
                case ScenarioBreakKind.FatalBlastSurvived: return Watch.BreakIfFatalBlastSurvived;
                default: return false;
            }
        }

        public ScenarioCueTiming Timing { get; }

        public ScenarioNotices Notices { get; }

        public ScenarioBreakWatch Watch { get; }

        public ScenarioSafetySpec Safety { get; }

        public ScenarioRoomSpec Room { get; }

        private static ScenarioPage[] ExpandNagPages(ScenarioPage[] pages)
        {
            var expanded = new List<ScenarioPage>(pages.Length);
            for (int i = 0; i < pages.Length; i++)
            {
                ScenarioPage prompt = pages[i];
                expanded.Add(prompt);
                if (prompt.NagLines.Count == 0) continue;

                var lines = new TextId[prompt.NagLines.Count];
                for (int n = 0; n < lines.Length; n++) lines[n] = prompt.NagLines[n];

                ScenarioPage nag = ScenarioPage.Say(ScenarioSignal.ActionNag, lines)
                    .At(prompt.Anchor)
                    .PointsAt(prompt.Highlight)
                    .Requires(prompt.Need)
                    .InMeeting(prompt.MeetingIndex)
                    .Detonation(prompt.BlastRule)
                    .BombTarget(prompt.BombTargetParticipantId);
                if (prompt.MarkBombTarget) nag = nag.MarksBombTarget();
                if (prompt.SkipIf.Count > 0)
                {
                    var skipIf = new ScenarioSignal[prompt.SkipIf.Count];
                    for (int k = 0; k < skipIf.Length; k++) skipIf[k] = prompt.SkipIf[k];
                    nag = nag.SkipWhen(skipIf);
                }

                expanded.Add(nag);
            }

            return expanded.ToArray();
        }

        public ScenarioFlow CreateFlow() => new ScenarioFlow(_pages);
    }
}
