using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CheckmateLineSyncTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const int WolfActor = 1;
        private const int CatActor = 2;
        private const int IntervalSec = 90;
        private const long IntervalMs = IntervalSec * 1000L;

        public CheckmateLineSyncTests()
        {
            WLog.Sink = (line, secret) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private (GameSession session, RolesSession roles, List<OutboundMessage> sent)
            BuildScenario(int catIntervalSec = IntervalSec)
        {
            var session = new GameSession();
            session.ReserveForcedRole(WolfActor, Role.Werewolf);
            session.ReserveForcedRole(CatActor, Role.BlackCat);

            var players = new List<WPlayer>();
            for (int i = 1; i <= 5; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }
            var config = new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = catIntervalSec,
            };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            var roles = new RolesSession(config, session, Now, new Random(1));
            var sent = new List<OutboundMessage>();
            roles.OnSend += sent.Add;

            roles.FreezeBase(10000f);
            sent.Clear();
            return (session, roles, sent);
        }

        private static int CountGaugeTo(List<OutboundMessage> sent, int actor)
        {
            int count = 0;
            foreach (var m in sent)
            {
                if (m.Code != WWRolesCodes.SyncPerkGauge) continue;
                if (m.Target != MessageTarget.Actors || m.TargetActors == null) continue;
                foreach (int a in m.TargetActors)
                {
                    if (a == actor) { count++; break; }
                }
            }
            return count;
        }

        private static int[] LastGaugeMetaTo(List<OutboundMessage> sent, int actor)
        {
            int[] meta = null;
            foreach (var m in sent)
            {
                if (m.Code != WWRolesCodes.SyncPerkGauge || m.TargetActors == null) continue;
                foreach (int a in m.TargetActors)
                {
                    if (a == actor) { meta = (int[])m.Payload[5]; break; }
                }
            }
            return meta;
        }

        [Fact]
        public void UpdateCheckmateLine_Changed_ResyncsWolfWithNinthMetaElement()
        {
            var (_, roles, sent) = BuildScenario();

            roles.UpdateCheckmateLine(7200);

            Assert.Equal(1, CountGaugeTo(sent, WolfActor));
            int[] meta = LastGaugeMetaTo(sent, WolfActor);
            Assert.NotNull(meta);
            Assert.True(meta.Length >= 9);
            Assert.Equal(7200, meta[8]);
        }

        [Fact]
        public void UpdateCheckmateLine_Unchanged_DoesNotResync()
        {
            var (_, roles, sent) = BuildScenario();
            roles.UpdateCheckmateLine(7200);
            sent.Clear();

            roles.UpdateCheckmateLine(7200);

            Assert.Empty(sent);
        }

        [Fact]
        public void UpdateCheckmateLine_DefaultBeforeUpdate_CarriesMinusOne()
        {
            var (_, roles, sent) = BuildScenario();

            roles.AddValueLoss(500f, isOrb: false);

            int[] meta = LastGaugeMetaTo(sent, WolfActor);
            Assert.NotNull(meta);
            Assert.True(meta.Length >= 9);
            Assert.Equal(-1, meta[8]);
        }

        [Fact]
        public void UpdateCheckmateLine_PeriodicCatMode_DefersToPeriodicSync()
        {
            var (session, roles, sent) = BuildScenario();
            session.NotifyDisclosureCondition(DisclosureKind.BlackCatSelfAwareness);
            long t0 = Now + 1000;
            roles.Tick(t0);
            sent.Clear();

            roles.UpdateCheckmateLine(7200);

            Assert.Equal(1, CountGaugeTo(sent, WolfActor));
            Assert.Equal(0, CountGaugeTo(sent, CatActor));

            roles.Tick(t0 + IntervalMs);
            Assert.Equal(1, CountGaugeTo(sent, CatActor));
            int[] meta = LastGaugeMetaTo(sent, CatActor);
            Assert.NotNull(meta);
            Assert.Equal(7200, meta[8]);
        }

        [Fact]
        public void UpdateCheckmateLine_RealtimeCatMode_SyncsAwakenedCatImmediately()
        {
            var (session, roles, sent) = BuildScenario(catIntervalSec: 0);
            session.NotifyDisclosureCondition(DisclosureKind.BlackCatSelfAwareness);

            roles.UpdateCheckmateLine(7200);

            Assert.Equal(1, CountGaugeTo(sent, CatActor));
            int[] meta = LastGaugeMetaTo(sent, CatActor);
            Assert.NotNull(meta);
            Assert.Equal(7200, meta[8]);
        }

        [Fact]
        public void ApplyGaugeSync_NineElementMeta_SetsCheckmateLossDollars()
        {
            var state = new RolesClientState();
            var meta = new[] { 10000, 15, 30, 50, 60, 10, 0, 500, 7200 };

            state.ApplyGaugeSync(50, 0, 0, 0, 0, meta, nowUnixMs: Now);

            Assert.NotNull(state.PlayGauge);
            Assert.Equal(7200, state.PlayGauge.CheckmateLossDollars);
            Assert.Equal(720, state.PlayGauge.CheckmateLinePermille());
        }

        [Fact]
        public void ApplyGaugeSync_LegacyEightElementMeta_LeavesCheckmateHidden()
        {
            var state = new RolesClientState();
            var meta = new[] { 10000, 15, 30, 50, 60, 10, 0, 500 };

            state.ApplyGaugeSync(50, 0, 0, 0, 0, meta, nowUnixMs: Now);

            Assert.NotNull(state.PlayGauge);
            Assert.Equal(-1, state.PlayGauge.CheckmateLossDollars);
            Assert.Equal(-1, state.PlayGauge.CheckmateLinePermille());
        }
    }
}
