using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CurseResolutionOrderTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const int RoundSeconds = 600;

        public CurseResolutionOrderTests()
        {
            WLog.Sink = (line, secret) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private (GameSession session, RolesSession roles, GameConfig config) BuildScenario()
        {
            var session = new GameSession();
            session.ReserveForcedRole(1, Role.Werewolf);
            session.ReserveForcedRole(2, Role.BlackCat);

            var players = new List<WPlayer>();
            for (int i = 1; i <= 5; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }
            var config = new GameConfig
            {
                RoundSeconds = RoundSeconds,
                CurseWaitSec = 10,
                ShamanChancePercent = 0,
            };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            var roles = new RolesSession(config, session, Now, new Random(1));
            return (session, roles, config);
        }

        [Fact]
        public void TryStartCurse_WhenDisabled_DoesNotStartCurse()
        {
            var (_, roles, config) = BuildScenario();
            config.BlackCatCurseEnabled = false;

            Assert.False(roles.TryStartCurse(executedActor: 2, nowUnixMs: Now, out long holdMs));
            Assert.Equal(0, holdMs);
            Assert.Null(roles.ActiveCurse);
        }

        [Fact]
        public void Tick_CurseResolves_FiresOnCurseSealedBeforeOnCurseKill()
        {
            var (session, roles, config) = BuildScenario();

            var callOrder = new List<string>();
            roles.OnCurseSealed += cat => callOrder.Add("sealed:" + cat);
            roles.OnCurseKill += victim => callOrder.Add("kill:" + victim);

            Assert.True(roles.TryStartCurse(executedActor: 2, nowUnixMs: Now, out _));
            roles.Tick(Now + config.CurseWaitSec * 1000L);

            Assert.Equal(2, callOrder.Count);
            Assert.StartsWith("sealed:2", callOrder[0]);
            Assert.StartsWith("kill:", callOrder[1]);
        }

        [Fact]
        public void Tick_WhenVictimIsRecorded_BlackCatIsAlreadyDeadWithVoteCause()
        {
            var (session, roles, config) = BuildScenario();

            roles.OnCurseSealed += cat =>
            {
                session.MarkNextDeathAsVote(cat);
                session.RecordDeath(cat, Now + config.CurseWaitSec * 1000L);
            };

            WPlayer catSnapshotAtVictimDeath = null;
            roles.OnCurseKill += victim =>
            {
                foreach (var p in session.Players)
                {
                    if (p.ActorNumber == 2) { catSnapshotAtVictimDeath = p; break; }
                }
                session.RecordDeath(victim, Now + config.CurseWaitSec * 1000L);
            };

            Assert.True(roles.TryStartCurse(executedActor: 2, nowUnixMs: Now, out _));
            roles.Tick(Now + config.CurseWaitSec * 1000L);

            Assert.NotNull(catSnapshotAtVictimDeath);
            Assert.False(catSnapshotAtVictimDeath.Alive);
            Assert.Equal(DeathCause.Vote, catSnapshotAtVictimDeath.DeathCause);
        }

        [Fact]
        public void Tick_ResolvesWithNoVictim_StillFiresOnCurseSealed()
        {
            var (session, roles, config) = BuildScenario();

            roles.DebugForceInformant();
            foreach (var p in session.Players)
            {
                if (p.Role == Role.Villager) p.Alive = false;
            }

            int sealedCount = 0;
            int killCount = 0;
            int sealedActor = -999;
            roles.OnCurseSealed += cat => { sealedCount++; sealedActor = cat; };
            roles.OnCurseKill += _ => killCount++;

            Assert.True(roles.TryStartCurse(executedActor: 2, nowUnixMs: Now, out _));
            roles.Tick(Now + config.CurseWaitSec * 1000L);

            Assert.Equal(1, sealedCount);
            Assert.Equal(2, sealedActor);
            Assert.Equal(0, killCount);
        }

        [Fact]
        public void Tick_ResolvesWithNoSealedSubscriber_DoesNotThrow()
        {
            var (session, roles, config) = BuildScenario();
            roles.OnCurseKill += _ => { };

            Assert.True(roles.TryStartCurse(executedActor: 2, nowUnixMs: Now, out _));
            var ex = Record.Exception(() => roles.Tick(Now + config.CurseWaitSec * 1000L));
            Assert.Null(ex);
        }
    }
}
