using System;
using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Tests
{
    internal sealed class MeetingSessionHarness
    {
        public const long GameStart = 1_000_000;

        public GameSession Game { get; private set; }
        public MeetingSession Session { get; private set; }
        public GameConfig Config { get; private set; }

        public readonly List<OutboundMessage> Sent = new List<OutboundMessage>();
        public readonly List<GamePhase> PhaseRequests = new List<GamePhase>();
        public readonly List<(int Caller, long End)> MeetingStates = new List<(int, long)>();
        public readonly List<int> Executed = new List<int>();
        public readonly List<(int Actor, int Remaining)> RightsChanges = new List<(int, int)>();

        public static MeetingSessionHarness Create(
            int playerCount = 5,
            Action<GameConfig> tune = null,
            params (int Actor, Role Role)[] forcedRoles)
        {
            var h = new MeetingSessionHarness();

            var game = new GameSession();
            foreach (var (actor, role) in forcedRoles) game.ReserveForcedRole(actor, role);

            var players = new List<WPlayer>();
            for (int i = 1; i <= playerCount; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }

            var config = new GameConfig
            {
                RoundSeconds = 1800,
                MeetingRightsPerPlayer = 1,
                ConveneSuppressStartSec = 0,
                ConveneSuppressAfterSec = 0,
                MeetingCountdownSec = 5,
                MeetingDurationSec = 120,
                VoteTimeCutEnabled = true,
                ResultDisplaySec = 6,
            };
            tune?.Invoke(config);

            if (!game.Start(config, players, GameStart, new Random(1)).Success)
                throw new InvalidOperationException("テスト用 GameSession の開始に失敗しました。");

            var session = new MeetingSession(config, game, game.Players, GameStart);
            session.OnSend += h.Sent.Add;
            session.OnPhaseChangeRequest += h.PhaseRequests.Add;
            session.OnMeetingStateChanged += (c, e) => h.MeetingStates.Add((c, e));
            session.OnExecutePlayer += h.Executed.Add;
            session.OnRightsChanged += (a, r) => h.RightsChanges.Add((a, r));

            h.Game = game;
            h.Session = session;
            h.Config = config;
            return h;
        }

        public WPlayer Player(int actor)
        {
            foreach (var p in Game.Players)
                if (p.ActorNumber == actor) return p;
            return null;
        }

        public IEnumerable<OutboundMessage> ByCode(byte code)
        {
            foreach (var m in Sent)
                if (m.Code == code) yield return m;
        }
    }
}
