using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public static class ScenarioGameConfig
    {
        public const int RoundSeconds = 86400;

        public const int MeetingSeconds = 900;

        public static GameConfig Build(GameConfig local, ScenarioDefinition definition)
        {
            var config = new GameConfig();
            if (local != null) CopyLocalSettings(local, config);

            config.WerewolfModeEnabled = true;
            config.DebugMode = false;

            if (definition != null)
            {
                ScenarioCast cast = definition.Cast;
                config.WerewolfCount = cast.WerewolfCount;
                config.BlackCatChancePercent = cast.BlackCatChancePercent;
                config.BomberChancePercent = cast.BomberChancePercent;
                config.ShamanChancePercent = cast.ShamanChancePercent;
            }

            config.TimeLimitDisabled = true;
            config.RoundSeconds = RoundSeconds;
            config.MeetingDurationSec = MeetingSeconds;
            config.VoteTimeCutEnabled = false;

            config.ConveneSuppressStartSec = 0;
            config.ConveneSuppressAfterSec = 0;

            config.GameOverAutoReturnSec = 0;

            config.MeetingScatterEnabled = false;

            if (definition != null) ApplyRoom(config, definition.Room);

            return config;
        }

        private static void CopyLocalSettings(GameConfig local, GameConfig config)
        {
            config.ButtonOffsetX = local.ButtonOffsetX;
            config.ButtonOffsetY = local.ButtonOffsetY;
            config.ButtonOffsetZ = local.ButtonOffsetZ;
            config.ButtonYaw = local.ButtonYaw;
            config.ButtonPitch = local.ButtonPitch;
            config.ToastDurationSec = local.ToastDurationSec;

            config.NecroVoiceVolume = local.NecroVoiceVolume;
            config.NecroVoiceLowPassCutoffHz = local.NecroVoiceLowPassCutoffHz;
            config.NecroVoiceEchoDelayMs = local.NecroVoiceEchoDelayMs;
            config.NecroVoiceEchoDecay = local.NecroVoiceEchoDecay;
            config.NecroVoiceReverbRoom = local.NecroVoiceReverbRoom;
            config.NecroVoiceReverbRoomHF = local.NecroVoiceReverbRoomHF;
            config.NecroVoiceReverbDecayTime = local.NecroVoiceReverbDecayTime;
            config.NecroVoiceReverbDecayHFRatio = local.NecroVoiceReverbDecayHFRatio;
            config.NecroVoiceReverbReflections = local.NecroVoiceReverbReflections;
            config.NecroVoiceReverbReflectionsDelay = local.NecroVoiceReverbReflectionsDelay;
            config.NecroVoiceReverbLevel = local.NecroVoiceReverbLevel;
            config.NecroVoiceReverbDelay = local.NecroVoiceReverbDelay;
            config.NecroVoiceReverbDiffusion = local.NecroVoiceReverbDiffusion;
            config.NecroVoiceReverbDensity = local.NecroVoiceReverbDensity;
            config.NecroVoiceReverbHFReference = local.NecroVoiceReverbHFReference;
        }

        private static void ApplyRoom(GameConfig config, ScenarioRoomSpec room)
        {
            config.MeetingRightsPerPlayer = room.MeetingRightsPerPlayer ?? config.MeetingRightsPerPlayer;
            config.StaminaUnlockPct = room.StaminaUnlockPct ?? config.StaminaUnlockPct;
            config.JumpUnlockPct = room.JumpUnlockPct ?? config.JumpUnlockPct;
            config.EnemyIgnoreUnlockPct = room.EnemyIgnoreUnlockPct ?? config.EnemyIgnoreUnlockPct;
            config.HealUnlockPct = room.HealUnlockPct ?? config.HealUnlockPct;
            config.BeaconChargePct = room.BeaconChargePct ?? config.BeaconChargePct;
            config.BeaconSuppressStartSec = room.BeaconSuppressStartSec ?? config.BeaconSuppressStartSec;
            config.BeaconSuppressAfterMeetingSec =
                room.BeaconSuppressAfterMeetingSec ?? config.BeaconSuppressAfterMeetingSec;
            config.BeaconCooldownSec = room.BeaconCooldownSec ?? config.BeaconCooldownSec;
            config.BomberInitialCooldownSec = room.BomberInitialCooldownSec ?? config.BomberInitialCooldownSec;
            config.BomberCooldownSec = room.BomberCooldownSec ?? config.BomberCooldownSec;
        }
    }
}
