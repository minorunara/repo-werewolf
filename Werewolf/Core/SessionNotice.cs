namespace Werewolf.Core
{
    public enum NoticeKind : byte
    {
        ConveneStarted = 0,

        BeaconAudit = 1,

        NoExecution = 2,

        Executed = 3,

        BlackCatRevealed = 4,

        CurseVictim = 5,

        ConveneDenied = 6,

        CatAwakened = 7,

        CorpseReportStarted = 8,

        MeetingCancelled = 9,

        PlayerDisconnected = 10,

        ConveneHoldHint = 11,

        ScatterGuardTripped = 12,
    }

    public sealed class SessionNotice
    {
        private SessionNotice(NoticeKind kind)
        {
            Kind = kind;
        }

        public NoticeKind Kind { get; }

        public string ActorName { get; private set; }

        public ConveneRejectReason DenyReason { get; private set; } = ConveneRejectReason.None;

        public int BeaconUseCount { get; private set; }

        public static SessionNotice ForConveneStarted(string callerName)
            => new SessionNotice(NoticeKind.ConveneStarted) { ActorName = callerName };

        public static SessionNotice ForBeaconAudit(int useCount)
            => new SessionNotice(NoticeKind.BeaconAudit) { BeaconUseCount = useCount };

        public static SessionNotice ForNoExecution()
            => new SessionNotice(NoticeKind.NoExecution);

        public static SessionNotice ForExecuted(string actorName)
            => new SessionNotice(NoticeKind.Executed) { ActorName = actorName };

        public static SessionNotice ForBlackCatRevealed(string actorName)
            => new SessionNotice(NoticeKind.BlackCatRevealed) { ActorName = actorName };

        public static SessionNotice ForCurseVictim(string actorName)
            => new SessionNotice(NoticeKind.CurseVictim) { ActorName = actorName };

        public static SessionNotice ForConveneDenied(ConveneRejectReason reason)
            => new SessionNotice(NoticeKind.ConveneDenied) { DenyReason = reason };

        public static SessionNotice ForCatAwakened()
            => new SessionNotice(NoticeKind.CatAwakened);

        public static SessionNotice ForCorpseReportStarted(string callerName)
            => new SessionNotice(NoticeKind.CorpseReportStarted) { ActorName = callerName };

        public static SessionNotice ForMeetingCancelled()
            => new SessionNotice(NoticeKind.MeetingCancelled);

        public static SessionNotice ForPlayerDisconnected(string actorName)
            => new SessionNotice(NoticeKind.PlayerDisconnected) { ActorName = actorName };

        public static SessionNotice ForConveneHoldHint()
            => new SessionNotice(NoticeKind.ConveneHoldHint);

        public static SessionNotice ForScatterGuardTripped()
            => new SessionNotice(NoticeKind.ScatterGuardTripped);
    }
}
