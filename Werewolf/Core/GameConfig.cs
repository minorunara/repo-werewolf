namespace Werewolf.Core
{
    public sealed class GameConfig
    {
        public int WerewolfCount = 1;

        public int BlackCatChancePercent = 0;

        public int BomberChancePercent = 0;

        public int ShamanChancePercent = 100;

        public int RoundSeconds = 1200;

        public int BlackCatRevealDelaySec = 60;

        public bool BlackCatCurseEnabled = true;

        public bool DebugMode = false;

        public int MeetingRightsPerPlayer = 1;

        public int ConveneSuppressStartSec = 15;

        public int ConveneSuppressAfterSec = 15;

        public int MeetingCountdownSec = 10;

        public int MeetingDurationSec = 180;

        public bool VoteTimeCutEnabled = true;

        public int ResultDisplaySec = 9;

        public float ButtonOffsetX = -6.2f;

        public float ButtonOffsetY = 0f;

        public float ButtonOffsetZ = 0f;

        public float ButtonYaw = -90f;

        public float ButtonPitch = -90f;

        public int StaminaUnlockPct = 10;

        public int JumpUnlockPct = 30;

        public int EnemyIgnoreUnlockPct = 40;

        public int HealUnlockPct = 60;

        public int HealIntervalSec = 3;

        public int BeaconChargePct = 20;

        public int InformantThresholdPct = 50;

        public int ExtraJumpCount = 10;

        public int BeaconCooldownSec = 60;

        public int BeaconSuppressStartSec = 60;

        public int BeaconSuppressAfterMeetingSec = 30;

        public int CurseWaitSec = 10;

        public int CatGaugeSyncIntervalSec = 120;

        public bool OrbGaugeEnabled = true;

        public bool WerewolfModeEnabled = false;

        public bool MinimapHideEnabled = true;

        public ValuableMapMode ValuableMapMode = ValuableMapMode.MeetingSync;

        public int GameOverAutoReturnSec = 60;

        public int ToastDurationSec = 9;

        public int StartLevelNumber = 11;

        public string StartMapName = "";

        public string StartItemsSpec = "";

        public int StartEnergyPct = 20;

        public string StartUpgradesSpec = "";

        public int OrbDropMax = 6;

        public NecroVoiceMode NecroVoiceMode = NecroVoiceMode.NonWerewolfDead;

        public float NecroVoiceVolume = 0.1f;

        public float NecroVoiceLowPassCutoffHz = 2500f;

        public float NecroVoiceEchoDelayMs = 500f;

        public float NecroVoiceEchoDecay = 0.2f;

        public float NecroVoiceReverbRoom = -600f;

        public float NecroVoiceReverbRoomHF = -800f;

        public float NecroVoiceReverbDecayTime = 7.0f;

        public float NecroVoiceReverbDecayHFRatio = 0.5f;

        public float NecroVoiceReverbReflections = -400f;

        public float NecroVoiceReverbReflectionsDelay = 0.03f;

        public float NecroVoiceReverbLevel = 300f;

        public float NecroVoiceReverbDelay = 0.07f;

        public float NecroVoiceReverbDiffusion = 100f;

        public float NecroVoiceReverbDensity = 100f;

        public float NecroVoiceReverbHFReference = 4500f;

        public bool BlackCatPossible(int playerCount)
            => BlackCatChancePercent > 0
               && playerCount - RoleAssigner.CorrectedWerewolfSlots(this, playerCount) >= 2;

        public bool BomberPossible(int playerCount)
            => BomberChancePercent > 0 && RoleAssigner.CorrectedWerewolfSlots(this, playerCount) >= 2;

        public int BomberProximityMeters = 9;

        public int BomberGaugeFullSec = 20;

        public int BomberInitialCooldownSec = 60;

        public int BomberCooldownSec = 30;

        public float BomberWarningSec = 1.0f;

        public float BomberBlastRadiusMeters = 9.0f;

        public int BomberBlastPlayerDamage = 80;

        public int BomberBlastEnemyDamage = 60;

        public int BomberAmmoRefillPct = 20;

        public float BomberTruckSafeRadiusMeters = 20f;

        public int ShamanGazeFullSec = 3;

        public int ShamanGhostCooldownSec = 10;

        public int ShamanStormWeakMeters = 30;

        public int ShamanStormMediumMeters = 20;

        public int ShamanStormStrongMeters = 10;
    }
}
