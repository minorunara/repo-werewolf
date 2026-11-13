#nullable enable
using System;

namespace Werewolf.Core
{
    internal sealed class HostRoundState
    {
        internal HostRoundState(GameConfig config, GameSession session)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }

        internal GameConfig Config { get; }

        internal GameSession Session { get; }

        internal HostPlayState? Play { get; private set; }

        internal HostPlayState BeginPlay(
            long playStartUnixMs,
            Random rng,
            Action<HostPlayState> prepare)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (prepare == null) throw new ArgumentNullException(nameof(prepare));
            if (Play != null) return Play;
            if (Session.Phase != GamePhase.Play)
            {
                throw new InvalidOperationException(
                    "host play state can only be created after GameSession enters Play");
            }

            var candidate = new HostPlayState(Config, Session, playStartUnixMs, rng);
            prepare(candidate);
            Play = candidate;
            return candidate;
        }
    }

    internal sealed class HostPlayState
    {
        internal HostPlayState(
            GameConfig config,
            GameSession session,
            long playStartUnixMs,
            Random rng)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            Config = config;
            Session = session;
            Meeting = new MeetingSession(config, session, session.Players, playStartUnixMs);
            Roles = new RolesSession(config, session, playStartUnixMs, rng);
            Bomber = new BombSession(config, session, playStartUnixMs);
        }

        internal GameConfig Config { get; }
        internal GameSession Session { get; }
        internal MeetingSession Meeting { get; }
        internal RolesSession Roles { get; }
        internal BombSession Bomber { get; }
    }
}
