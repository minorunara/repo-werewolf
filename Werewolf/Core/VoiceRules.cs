namespace Werewolf.Core
{
    public enum VoicePlanKind : byte
    {
        None = 0,
        ResultAll = 1,
        Eavesdrop = 2,
    }

    public static class VoiceRules
    {
        public static VoicePlanKind DecideKind(
            GamePhase phase, Role? localRole, bool localAlive, NecroVoiceMode mode)
        {
            if (phase == GamePhase.GameOver)
            {
                return VoicePlanKind.ResultAll;
            }

            if (phase != GamePhase.Play)
            {
                return VoicePlanKind.None;
            }

            bool localCanEavesdrop = localRole == Role.Werewolf || localRole == Role.Bomber;
            if (localCanEavesdrop && localAlive && mode != NecroVoiceMode.Off)
            {
                return VoicePlanKind.Eavesdrop;
            }

            return VoicePlanKind.None;
        }

        public static bool IsEavesdropTarget(
            bool targetDead, bool targetIsKnownWerewolf, NecroVoiceMode mode)
        {
            if (!targetDead)
            {
                return false;
            }

            switch (mode)
            {
                case NecroVoiceMode.AllDead:
                    return true;
                case NecroVoiceMode.NonWerewolfDead:
                    return !targetIsKnownWerewolf;
                case NecroVoiceMode.Off:
                default:
                    return false;
            }
        }

        public static bool IsDeadCueMuteActive(GamePhase phase, bool localAlive)
        {
            return (phase == GamePhase.Play || phase == GamePhase.Meeting) && localAlive;
        }

        public static bool IsDeadCueMuteTarget(bool targetDead, bool targetEavesdropAudible)
        {
            return targetDead && !targetEavesdropAudible;
        }

        public static bool ShouldShowDeadCues(
            GamePhase phase, bool localAlive, bool speakerDead, bool speakerEavesdropAudible)
        {
            if (!speakerDead)
            {
                return true;
            }

            if (!localAlive)
            {
                return true;
            }

            if (phase == GamePhase.GameOver)
            {
                return true;
            }

            return speakerEavesdropAudible;
        }

        public static bool ShouldShowDeadText(GamePhase phase, bool localAlive, bool speakerDead)
        {
            if (!speakerDead)
            {
                return true;
            }

            if (!localAlive)
            {
                return true;
            }

            if (phase == GamePhase.Lobby || phase == GamePhase.GameOver)
            {
                return true;
            }

            return false;
        }

        public static bool ShouldApplyNecroFilter(VoicePlanKind plan, bool targetEavesdropAudible)
        {
            return plan == VoicePlanKind.Eavesdrop && targetEavesdropAudible;
        }
    }
}
