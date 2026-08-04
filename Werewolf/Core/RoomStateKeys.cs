namespace Werewolf.Core
{
    public static class RoomStateKeys
    {

        public const string Phase = "WW_Phase";

        public const string RoundEndTime = "WW_RoundEndTime";

        public const string IsAlive = "WW_IsAlive";

        public const string MeetingCaller = "WW_MeetingCaller";

        public const string MeetingEndTime = "WW_MeetingEndTime";

        public const string CfgMinimapHide = "WW_CfgMinimapHide";

        public const string CfgCatPossible = "WW_CfgCatPossible";

        public const string CfgValuableMapMode = "WW_CfgValuableMapMode";

        public const string Rights = "WW_Rights";

        public const string CfgShared = "WW_CfgShared";

        public const string CfgNecroVoiceMode = "WW_CfgNecroVoiceMode";

        public const string CfgExtraJump = "WW_CfgExtraJump";

        public const string CfgConveneSuppressStart = "WW_CfgConveneSuppressStart";

        public const string CfgConveneSuppressAfter = "WW_CfgConveneSuppressAfter";

        public const string CfgHealInterval = "WW_CfgHealInterval";

        public const string CfgBomb = "WW_CfgBomb";

        public const string CfgShaman = "WW_CfgShaman";

        public static readonly string[] AllKeys =
        {
            Phase, RoundEndTime, IsAlive, MeetingCaller, MeetingEndTime,
            CfgMinimapHide, CfgCatPossible, CfgValuableMapMode, Rights, CfgShared,
            CfgNecroVoiceMode,
            CfgExtraJump, CfgConveneSuppressStart, CfgConveneSuppressAfter,
            CfgHealInterval,
            CfgBomb,
            CfgShaman,
        };

        public static class BombIndex
        {
            public const int BomberPossible = 0;

            public const int ProximityCm = 1;

            public const int GaugeFullMs = 2;

            public const int CooldownMs = 3;

            public const int BlastRadiusCm = 4;

            public const int BlastPlayerDamage = 5;

            public const int BlastEnemyDamage = 6;

            public const int InitialCooldownMs = 7;

            public const int Length = 8;
        }

        public static int[] EncodeBomb(GameConfig config, int playerCount)
        {
            var packed = new int[BombIndex.Length];
            packed[BombIndex.BomberPossible]   = EncodeBool(config.BomberPossible(playerCount));
            packed[BombIndex.ProximityCm]      = MetersToCm(config.BomberProximityMeters);
            packed[BombIndex.GaugeFullMs]      = SecondsToMs(config.BomberGaugeFullSec);
            packed[BombIndex.CooldownMs]       = config.BomberCooldownSec * 1000;
            packed[BombIndex.BlastRadiusCm]    = MetersToCm(config.BomberBlastRadiusMeters);
            packed[BombIndex.BlastPlayerDamage]= config.BomberBlastPlayerDamage;
            packed[BombIndex.BlastEnemyDamage] = config.BomberBlastEnemyDamage;
            packed[BombIndex.InitialCooldownMs]= config.BomberInitialCooldownSec * 1000;
            return packed;
        }

        public static class ShamanIndex
        {
            public const int GazeFullMs = 0;

            public const int GhostCooldownMs = 1;

            public const int StormWeakCm = 2;

            public const int StormMediumCm = 3;

            public const int StormStrongCm = 4;

            public const int Length = 5;
        }

        public static int[] EncodeShaman(GameConfig config)
        {
            var packed = new int[ShamanIndex.Length];
            packed[ShamanIndex.GazeFullMs]      = SecondsToMs(config.ShamanGazeFullSec);
            packed[ShamanIndex.GhostCooldownMs] = SecondsToMs(config.ShamanGhostCooldownSec);
            packed[ShamanIndex.StormWeakCm]     = MetersToCm(config.ShamanStormWeakMeters);
            packed[ShamanIndex.StormMediumCm]   = MetersToCm(config.ShamanStormMediumMeters);
            packed[ShamanIndex.StormStrongCm]   = MetersToCm(config.ShamanStormStrongMeters);
            return packed;
        }

        private static int MetersToCm(float meters)
        {
            if (float.IsNaN(meters) || meters < 0f) return 0;
            return (int)System.Math.Round(meters * 100f);
        }

        private static int SecondsToMs(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0f) return 0;
            return (int)System.Math.Round(seconds * 1000f);
        }

        public static byte EncodeBool(bool value) => (byte)(value ? 1 : 0);

        public static bool DecodeBool(byte value) => value != 0;

        public static byte EncodeRights(int remaining)
        {
            if (remaining < 0) return 0;
            if (remaining > byte.MaxValue) return byte.MaxValue;
            return (byte)remaining;
        }

        public static int DecodeRights(byte value) => value;

        public static byte EncodeValuableMapMode(ValuableMapMode mode) => (byte)mode;

        public static ValuableMapMode DecodeValuableMapMode(byte value)
        {
            return value switch
            {
                (byte)ValuableMapMode.Realtime => ValuableMapMode.Realtime,
                (byte)ValuableMapMode.MeetingSync => ValuableMapMode.MeetingSync,
                (byte)ValuableMapMode.Hidden => ValuableMapMode.Hidden,
                _ => ValuableMapMode.MeetingSync,
            };
        }

        public static byte EncodeNecroVoiceMode(NecroVoiceMode mode) => (byte)mode;

        public static NecroVoiceMode DecodeNecroVoiceMode(byte value)
        {
            return value switch
            {
                (byte)NecroVoiceMode.Off => NecroVoiceMode.Off,
                (byte)NecroVoiceMode.NonWerewolfDead => NecroVoiceMode.NonWerewolfDead,
                (byte)NecroVoiceMode.AllDead => NecroVoiceMode.AllDead,
                _ => NecroVoiceMode.Off,
            };
        }

        public static byte EncodeExtraJump(int count)
        {
            if (count < -1) count = -1;
            if (count > byte.MaxValue - 1) count = byte.MaxValue - 1;
            return (byte)(count + 1);
        }

        public static int DecodeExtraJump(byte value) => value - 1;
    }
}
