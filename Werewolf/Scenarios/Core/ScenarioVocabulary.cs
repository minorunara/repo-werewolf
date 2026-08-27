namespace Werewolf.Scenarios.Core
{
    public enum ScenarioSignal : byte
    {
        Advance = 0,
        ConveneAccepted = 1,
        MeetingOpened = 2,
        ChatScriptDone = 3,
        VoteAccepted = 4,
        TallyRevealed = 5,
        WinCeremonyDone = 6,
        ResultScreenShown = 7,

        ActionNag = 8,

        MapClosed = 9,

        MeetingReleased = 29,

        ValuableDestroyed = 10,

        RecordTurnedOff = 11,

        StealthDestroyed = 12,

        PerkStaminaUnlocked = 13,

        PerkJumpUnlocked = 14,

        PerkEnemyIgnoreUnlocked = 15,

        PerkHealUnlocked = 16,

        WolfFormTurnedOn = 17,

        BeaconUsed = 18,

        ShamanGhostFired = 19,

        ShamanStormEntered = 20,

        CorpseReportDenied = 21,

        ExtractionCompleted = 22,

        LastRunReached = 23,

        BombPlanted = 24,

        BombDeniedTruckZone = 25,

        BombDetonationAccepted = 26,

        BombExploded = 27,

        BombOnWrongTarget = 28,

        BombDeniedDistance = 30,

        BombDeniedTargetMoving = 31,

        BombAlreadyOnTarget = 32,
    }

    public enum ScenarioAnchor : byte
    {
        BottomLeft = 0,

        MapButton = 1,

        ChatLog = 2,

        ChatLogButton = 3,

        MeetingGauge = 4,

        ReplayEntry = 5,

        PlayHud = 6,

        PlayHudRight = 7,

        CheckmateStamp = 8,

        DecodeFocus = 9,
    }

    public enum ScenarioNeed : byte
    {
        None = 0,

        ChatLogOpen = 1,

        MapClosed = 2,

        MapOpen = 3,

        MapToolOpen = 4,

        ShamanTranceActive = 5,

        ShamanStormActive = 6,

        CorpseNear = 7,

        MapToolClosed = 8,

        BombTargetNear = 9,

        BombOnTarget = 10,

        ShamanSenseReady = 11,
    }

    public enum ScenarioHighlight : byte
    {
        None = 0,

        MeetingGauge = 1,

        MapButton = 2,

        MapOverlay = 3,

        ChatLogButton = 4,

        ChatLogPanel = 5,

        RecapTitle = 6,

        RecapDeaths = 7,

        RecapLost = 8,

        RecapHaul = 9,

        RecapBeacon = 10,

        ReplayEntry = 11,

        PlayGauge = 12,

        WolfStatus = 13,

        BeaconSlot = 14,

        RecordIcon = 15,

        ReportIcon = 16,

        BomberHud = 17,
    }

    public enum ScenarioGuideHint : byte
    {
        Advance = 0,

        ActionOnly = 1,

        AbortConfirm = 2,

        None = 3,
    }

    public enum ScenarioCue : byte
    {
        None = 0,

        ChatScript = 1,

        BotVotes = 2,

        ActionNag = 3,
    }

    public enum ScenarioEntryAction : byte
    {
        None = 0,

        GrantBeaconCharge = 1,

        PlaceFarCorpse = 2,

        PlaceTruckCorpse = 3,

        GrantMeetingRight = 4,

        SpawnQuotaBag = 5,

        BodyToPlantSite = 6,

        BodyToTruck = 7,

        BodyToGatherSite = 8,

        BodyToFarSite = 9,

        GrantBombAmmo = 11,

        ConveneAsParticipant = 12,

        KillParticipant = 13,

        HoldEnemiesDespawned = 14,
    }

    public readonly struct ScenarioEntryStep
    {
        public ScenarioEntryStep(ScenarioEntryAction action, int arg)
        {
            Action = action;
            Arg = arg;
        }

        public ScenarioEntryAction Action { get; }

        public int Arg { get; }
    }

    public enum ScenarioBlastRule : byte
    {
        None = 0,

        OutsideBlast = 1,

        PointBlank = 2,

        AwaitTruckZone = 3,
    }

    public enum ScenarioMilestone : byte
    {
        None = 0,

        ActionPrompted = 1,

        CheckmateArmed = 2,

        TruckBound = 3,

        Roaming = 4,

        FatalBlastArmed = 5,
    }
}
