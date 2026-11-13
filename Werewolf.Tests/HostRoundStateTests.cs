#nullable enable
using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class HostRoundStateTests
    {
        private const long Now = 1_000_000;

        private static List<WPlayer> Players()
            => new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Name = "P1" },
                new WPlayer { ActorNumber = 2, Name = "P2" },
                new WPlayer { ActorNumber = 3, Name = "P3" },
            };

        private static HostRoundState StartedRound(out GameConfig config)
        {
            config = new GameConfig { PeacefulVillageChancePercent = 0 };
            var session = new GameSession();
            var round = new HostRoundState(config, session);
            Assert.True(session.StartPreplay(config, Players(), Now, new Random(1)).Success);
            Assert.Null(round.Play);
            Assert.Equal(BeginPlayResult.Started,
                session.BeginPlay(Now + Preplay.RoleRevealDurationMs));
            return round;
        }

        [Fact]
        public void BeginPlay_PublishesAllRequiredSessionsOnlyAfterPreparation()
        {
            HostRoundState round = StartedRound(out GameConfig config);
            HostPlayState? prepared = null;

            HostPlayState play = round.BeginPlay(Now, new Random(2), candidate =>
            {
                Assert.Null(round.Play);
                Assert.NotNull(candidate.Meeting);
                Assert.NotNull(candidate.Roles);
                Assert.NotNull(candidate.Bomber);
                prepared = candidate;
            });

            Assert.Same(config, round.Config);
            Assert.Same(config, play.Config);
            Assert.Same(round.Session, play.Session);
            Assert.Same(prepared, play);
            Assert.Same(play, round.Play);

            bool preparedAgain = false;
            HostPlayState same = round.BeginPlay(Now, new Random(3), _ => preparedAgain = true);
            Assert.Same(play, same);
            Assert.False(preparedAgain);
        }

        [Fact]
        public void BeginPlay_WhenPreparationFails_DoesNotPublishPartialState()
        {
            HostRoundState round = StartedRound(out _);

            Assert.Throws<InvalidOperationException>(() =>
                round.BeginPlay(Now, new Random(2), _ =>
                    throw new InvalidOperationException("wire failed")));

            Assert.Null(round.Play);
        }

        [Fact]
        public void BeginPlay_BeforeSessionEntersPlay_IsRejectedWithoutStateChange()
        {
            var config = new GameConfig { PeacefulVillageChancePercent = 0 };
            var session = new GameSession();
            var round = new HostRoundState(config, session);
            Assert.True(session.StartPreplay(config, Players(), Now, new Random(1)).Success);
            Assert.Equal(GamePhase.RoleReveal, session.Phase);

            Assert.Throws<InvalidOperationException>(() =>
                round.BeginPlay(Now, new Random(1), _ => { }));

            Assert.Null(round.Play);
        }
    }
}
