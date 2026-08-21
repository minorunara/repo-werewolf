namespace Werewolf.Core
{
    public static class NoticeCatalog
    {
        public static string Format(SessionNotice notice)
        {
            if (notice == null) return null;

            switch (notice.Kind)
            {
                case NoticeKind.ConveneStarted:
                    return Texts.Format(TextId.NoticeConveneStartedFormat, notice.ActorName);

                case NoticeKind.NoExecution:
                    return Texts.Get(TextId.NoticeNoExecution);

                case NoticeKind.Executed:
                    return Texts.Format(TextId.NoticeExecutedFormat, notice.ActorName);

                case NoticeKind.BlackCatRevealed:
                    return Texts.Format(TextId.NoticeBlackCatRevealedFormat, notice.ActorName);

                case NoticeKind.CurseVictim:
                    return Texts.Format(TextId.NoticeCurseVictimFormat, notice.ActorName);

                case NoticeKind.CatAwakened:
                    return Texts.Get(TextId.NoticeCatAwakened);

                case NoticeKind.CorpseReportStarted:
                    return Texts.Format(TextId.NoticeCorpseReportStartedFormat, notice.ActorName);

                case NoticeKind.MeetingCancelled:
                    return Texts.Get(TextId.NoticeMeetingCancelledExtraction);

                case NoticeKind.PlayerDisconnected:
                    return Texts.Format(TextId.NoticePlayerDisconnectedFormat, notice.ActorName);

                case NoticeKind.ConveneHoldHint:
                    return Texts.Get(TextId.NoticeConveneHoldHint);

                case NoticeKind.ScatterGuardTripped:
                    return Texts.Get(TextId.NoticeScatterGuardTripped);

                case NoticeKind.ConveneDenied:
                    return FormatConveneDenied(notice.DenyReason);

                default:
                    return null;
            }
        }

        private static string FormatConveneDenied(ConveneRejectReason reason)
        {
            switch (reason)
            {
                case ConveneRejectReason.None:
                    return null;

                case ConveneRejectReason.NoRight:
                    return Texts.Get(TextId.NoticeConveneDeniedNoRight);

                case ConveneRejectReason.Suppressed:
                    return Texts.Get(TextId.NoticeConveneDeniedSuppressed);

                case ConveneRejectReason.WrongPhase:
                    return Texts.Get(TextId.NoticeConveneDeniedWrongPhase);

                case ConveneRejectReason.CorpseReportLastRun:
                    return Texts.Get(TextId.NoticeConveneDeniedLastRun);

                case ConveneRejectReason.NoCorpse:
                    return Texts.Get(TextId.NoticeConveneDeniedNoCorpse);

                default:
                    return Texts.Get(TextId.NoticeConveneDeniedOther);
            }
        }
    }
}
