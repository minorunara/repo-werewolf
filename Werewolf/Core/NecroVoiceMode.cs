namespace Werewolf.Core
{
    public enum NecroVoiceMode : byte
    {
        Off = 0,

        NonWerewolfDead = 1,

        AllDead = 2,
    }

    public static class NecroVoiceModes
    {
        public static NecroVoiceMode FromByte(byte value)
        {
            return value switch
            {
                (byte)NecroVoiceMode.Off => NecroVoiceMode.Off,
                (byte)NecroVoiceMode.NonWerewolfDead => NecroVoiceMode.NonWerewolfDead,
                (byte)NecroVoiceMode.AllDead => NecroVoiceMode.AllDead,
                _ => NecroVoiceMode.Off,
            };
        }
    }
}
