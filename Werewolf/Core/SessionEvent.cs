namespace Werewolf.Core
{
    public enum SessionEventKind : byte
    {
        PhaseChanged = 0,

        PlayerDied = 1,

        WinnerConfirmed = 2,

        MatchVoided = 3,

        WinLocked = 4,
    }

    public sealed class SessionEvent
    {
        private SessionEvent(SessionEventKind kind)
        {
            Kind = kind;
        }

        public SessionEventKind Kind { get; }

        public GamePhase Phase { get; private set; }

        public long RoundEndUnixMs { get; private set; }

        public int ActorNumber { get; private set; }

        public DeathCause DeathCause { get; private set; }

        public WinResult Winner { get; private set; }

        public bool Vanished { get; private set; }

        public static SessionEvent ForPhaseChanged(GamePhase phase, long roundEndUnixMs)
            => new SessionEvent(SessionEventKind.PhaseChanged) { Phase = phase, RoundEndUnixMs = roundEndUnixMs };

        public static SessionEvent ForPlayerDied(int actorNumber, DeathCause cause)
            => new SessionEvent(SessionEventKind.PlayerDied) { ActorNumber = actorNumber, DeathCause = cause };

        public static SessionEvent ForWinnerConfirmed(WinResult winner)
            => new SessionEvent(SessionEventKind.WinnerConfirmed) { Winner = winner };

        public static SessionEvent ForMatchVoided()
            => new SessionEvent(SessionEventKind.MatchVoided);

        public static SessionEvent ForWinLocked(WinResult pending, int actorNumber, bool vanished)
            => new SessionEvent(SessionEventKind.WinLocked)
               { Winner = pending, ActorNumber = actorNumber, Vanished = vanished };
    }
}
