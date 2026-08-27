using System;
using System.Collections.Generic;
using System.Text;

namespace Werewolf.Core
{
    public static class ResultDigestText
    {
        public static List<string> FormatLines(
            IReadOnlyList<DigestEntry> entries, Func<int, string> resolveName)
        {
            var lines = new List<string>();
            if (entries == null) return lines;

            foreach (DigestEntry e in entries)
            {
                if (e == null) continue;
                string body = FormatBody(e, resolveName);
                if (body == null) continue;
                lines.Add(FormatTime(e.AtSec) + "  " + body);
            }
            return lines;
        }

        public static string FormatTime(int atSec)
        {
            if (atSec < 0) atSec = 0;
            int h = atSec / 3600;
            int m = (atSec % 3600) / 60;
            int s = atSec % 60;
            if (h > 0)
            {
                return h.ToString() + ":" + m.ToString("00") + ":" + s.ToString("00");
            }
            return m.ToString("00") + ":" + s.ToString("00");
        }

        private static string FormatBody(DigestEntry e, Func<int, string> resolveName)
        {
            switch (e.Kind)
            {
                case DigestKind.MatchStart:
                    return Texts.Get(TextId.DigestMatchStart);
                case DigestKind.MeetingConvened:
                    return Texts.Format(
                        e.ArgA == 1 ? TextId.DigestMeetingReportFormat : TextId.DigestMeetingButtonFormat,
                        Name(e.Actor, resolveName));
                case DigestKind.Executed:
                    return Texts.Format(TextId.DigestExecutedFormat, Name(e.Actor, resolveName));
                case DigestKind.NoExecution:
                    return Texts.Get(TextId.DigestNoExecution);
                case DigestKind.CurseStarted:
                    return Texts.Format(TextId.DigestCurseStartedFormat, Name(e.Actor, resolveName));
                case DigestKind.CurseFollow:
                    return Texts.Format(TextId.DigestCurseFollowFormat, Name(e.Actor, resolveName));
                case DigestKind.Death:
                    return Texts.Format(TextId.DigestDeathFormat, Name(e.Actor, resolveName));
                case DigestKind.BombDetonated:
                    return Texts.Format(TextId.DigestBombDetonatedFormat, Name(e.Actor, resolveName));
                case DigestKind.Checkmate:
                    return Texts.Get(TextId.DigestCheckmate);
                case DigestKind.MatchEnd:
                    return FormatMatchEnd(e);
                case DigestKind.ExtractionDone:
                    return Texts.Format(TextId.DigestExtractionDoneFormat, e.ArgA, e.ArgB);
                case DigestKind.PerkUnlocked:
                    return Texts.Format(TextId.DigestPerkUnlockedFormat, PerkLabel(e.ArgA));
                case DigestKind.InformantEstablished:
                    return Texts.Get(TextId.DigestInformant);
                case DigestKind.FinalBalance:
                    return Texts.Format(TextId.DigestFinalBalanceFormat, e.ArgA, e.ArgB, e.Actor);
                default:
                    return null;
            }
        }

        private static string PerkLabel(int perkId)
        {
            switch ((PerkId)perkId)
            {
                case PerkId.InfiniteStamina: return Texts.Get(TextId.GaugePerkStaminaLabel);
                case PerkId.InfiniteJump:    return Texts.Get(TextId.GaugePerkJumpLabel);
                case PerkId.EnemyIgnore:     return Texts.Get(TextId.GaugePerkEnemyIgnoreLabel);
                case PerkId.NaturalHeal:     return Texts.Get(TextId.GaugePerkHealLabel);
                default:                     return "?";
            }
        }

        private static string FormatMatchEnd(DigestEntry e)
        {
            if (e.ArgA == TeamCodes.VoidMatch) return Texts.Get(TextId.ResultBannerVoid);

            string team = OutcomeText((byte)e.ArgA);
            string reason = ReasonText(e.ArgB);
            return reason == null ? team : Texts.Format(TextId.DigestMatchEndFormat, team, reason);
        }

        public static string OutcomeText(byte outcome)
        {
            switch (outcome)
            {
                case (byte)Team.Villagers: return Texts.Get(TextId.ResultBannerVillagerWin);
                case (byte)Team.Werewolves: return Texts.Get(TextId.ResultBannerWerewolfWin);
                case TeamCodes.VoidMatch: return Texts.Get(TextId.ResultBannerVoid);
                case TeamCodes.PeacefulSurvived: return Texts.Get(TextId.ResultBannerPeacefulSurvived);
                case TeamCodes.PeacefulCollapsed: return Texts.Get(TextId.ResultBannerPeacefulCollapsed);
                default: return Texts.Get(TextId.ResultBannerDefault);
            }
        }

        public static string ReasonText(int reason)
        {
            switch (reason)
            {
                case (int)WinReason.WerewolvesEradicated: return Texts.Get(TextId.DigestReasonWerewolvesEradicated);
                case (int)WinReason.VillagersEradicated: return Texts.Get(TextId.DigestReasonVillagersEradicated);
                case (int)WinReason.ExtractionCompleted: return Texts.Get(TextId.DigestReasonExtractionCompleted);
                case (int)WinReason.TimerExpired: return Texts.Get(TextId.DigestReasonTimerExpired);
                case (int)WinReason.ExtractionFailed: return Texts.Get(TextId.DigestReasonExtractionFailed);
                case (int)WinReason.ValueCheckmate: return Texts.Get(TextId.DigestReasonValueCheckmate);
                case (int)WinReason.PeacefulSurvived: return Texts.Get(TextId.DigestReasonExtractionCompleted);
                default: return null;
            }
        }

        public static string EpigraphText(int reason)
        {
            switch (reason)
            {
                case (int)WinReason.WerewolvesEradicated: return Texts.Get(TextId.EpigraphWerewolvesEradicated);
                case (int)WinReason.VillagersEradicated: return Texts.Get(TextId.EpigraphVillagersEradicated);
                case (int)WinReason.ExtractionCompleted: return Texts.Get(TextId.EpigraphExtractionCompleted);
                case (int)WinReason.TimerExpired: return Texts.Get(TextId.EpigraphTimerExpired);
                case (int)WinReason.ExtractionFailed: return Texts.Get(TextId.EpigraphExtractionFailed);
                case (int)WinReason.ValueCheckmate: return Texts.Get(TextId.EpigraphValueCheckmate);
                case (int)WinReason.PeacefulSurvived: return Texts.Get(TextId.EpigraphPeacefulSurvived);
                case (int)WinReason.PeacefulCollapsed: return Texts.Get(TextId.EpigraphPeacefulCollapsed);
                default: return null;
            }
        }

        private static string Name(int actor, Func<int, string> resolveName)
        {
            string name = resolveName != null ? resolveName(actor) : null;
            return string.IsNullOrEmpty(name) ? "Actor" + actor : name;
        }
    }
}
