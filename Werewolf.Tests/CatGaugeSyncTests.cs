using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CatGaugeSyncTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const int WolfActor = 1;
        private const int CatActor = 2;
        private const int IntervalSec = 90;
        private const long IntervalMs = IntervalSec * 1000L;

        public CatGaugeSyncTests()
        {
            WLog.Sink = (line, secret) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private (GameSession session, RolesSession roles, List<OutboundMessage> sent, GameConfig config)
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
            return (session, roles, sent, config);
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

        private static void Awaken(GameSession session)
            => session.NotifyDisclosureCondition(DisclosureKind.BlackCatSelfAwareness);

        [Fact]
        public void AddValueLoss_PeriodicMode_SendsToWolfOnly()
        {
            var (session, roles, sent, _) = BuildScenario();
            Awaken(session);

            roles.AddValueLoss(500f, isOrb: false);

            Assert.Equal(1, CountGaugeTo(sent, WolfActor));
            Assert.Equal(0, CountGaugeTo(sent, CatActor));
        }

        [Fact]
        public void AddValueLoss_RealtimeMode_SendsToCatWithZeroNextUpdate()
        {
            var (session, roles, sent, _) = BuildScenario(catIntervalSec: 0);
            Awaken(session);

            roles.AddValueLoss(500f, isOrb: false);

            Assert.Equal(1, CountGaugeTo(sent, CatActor));
            int[] meta = LastGaugeMetaTo(sent, CatActor);
            Assert.NotNull(meta);
            Assert.True(meta.Length >= 7);
            Assert.Equal(0, meta[6]);
        }

        [Fact]
        public void Tick_BeforeAwaken_DoesNotSendToCat()
        {
            var (_, roles, sent, _) = BuildScenario();

            roles.Tick(Now + 1000);

            Assert.Equal(0, CountGaugeTo(sent, CatActor));
        }

        [Fact]
        public void Tick_AfterAwaken_SendsImmediately_ThenEveryInterval()
        {
            var (session, roles, sent, _) = BuildScenario();
            Awaken(session);

            long t0 = Now + 1000;
            roles.Tick(t0);
            Assert.Equal(1, CountGaugeTo(sent, CatActor));

            roles.AddValueLoss(500f, isOrb: false);
            roles.Tick(t0 + IntervalMs - 1);
            Assert.Equal(1, CountGaugeTo(sent, CatActor));

            roles.Tick(t0 + IntervalMs);
            Assert.Equal(2, CountGaugeTo(sent, CatActor));
        }

        [Fact]
        public void Tick_PeriodicSend_CarriesNextUpdateSeconds()
        {
            var (session, roles, sent, _) = BuildScenario();
            Awaken(session);

            roles.Tick(Now + 1000);

            int[] meta = LastGaugeMetaTo(sent, CatActor);
            Assert.NotNull(meta);
            Assert.True(meta.Length >= 7);
            Assert.Equal(IntervalSec, meta[6]);
        }

        [Fact]
        public void Tick_PeriodicSend_IsSingleTargetToCat()
        {
            var (session, roles, sent, _) = BuildScenario();
            Awaken(session);

            roles.Tick(Now + 1000);

            foreach (var m in sent)
            {
                if (m.Code != WWRolesCodes.SyncPerkGauge) continue;
                Assert.Equal(MessageTarget.Actors, m.Target);
                Assert.NotNull(m.TargetActors);
                Assert.Single(m.TargetActors);
            }
        }

        [Fact]
        public void MeetingStart_ForcesCatSync_AndTickIsSuppressedDuringMeeting()
        {
            var (session, roles, sent, _) = BuildScenario();
            Awaken(session);
            long t0 = Now + 1000;
            roles.Tick(t0);
            sent.Clear();

            Assert.True(session.RequestPhaseChange(GamePhase.Meeting, t0 + 5000).Success);
            roles.OnMeetingStarted(t0 + 5000);
            Assert.Equal(1, CountGaugeTo(sent, CatActor));

            roles.Tick(t0 + 5000 + IntervalMs * 2);
            Assert.Equal(1, CountGaugeTo(sent, CatActor));
        }

        [Fact]
        public void MeetingEnd_ForcesCatSync_AndRestartsCycle()
        {
            var (session, roles, sent, _) = BuildScenario();
            Awaken(session);
            long meetingEnd = Now + 60_000;

            roles.OnMeetingEnded(meetingEnd);
            Assert.Equal(1, CountGaugeTo(sent, CatActor));

            roles.Tick(meetingEnd + IntervalMs - 1);
            Assert.Equal(1, CountGaugeTo(sent, CatActor));
            roles.Tick(meetingEnd + IntervalMs);
            Assert.Equal(2, CountGaugeTo(sent, CatActor));
        }

        [Fact]
        public void MeetingHooks_BeforeAwaken_DoNotSendToCat()
        {
            var (_, roles, sent, _) = BuildScenario();

            roles.OnMeetingStarted(Now + 5000);
            roles.OnMeetingEnded(Now + 60_000);

            Assert.Equal(0, CountGaugeTo(sent, CatActor));
        }

        [Fact]
        public void InformantEstablished_ForcesCatSyncOnNextTick()
        {
            var (session, roles, sent, config) = BuildScenario();
            Awaken(session);
            long t0 = Now + 1000;
            roles.Tick(t0);
            sent.Clear();

            roles.AddValueLoss(10000f * config.InformantThresholdPct / 100f, isOrb: false);
            Assert.True(roles.InformantEstablished);
            Assert.Equal(0, CountGaugeTo(sent, CatActor));

            roles.Tick(t0 + 1000);
            Assert.Equal(1, CountGaugeTo(sent, CatActor));
        }

        [Fact]
        public void ApplyGaugeSync_With7thMetaElement_AnchorsNextUpdateAtLocalClock()
        {
            var state = new RolesClientState();
            var meta = new[] { 10000, 15, 30, 50, 60, 10, IntervalSec };

            state.ApplyGaugeSync(250, 0, 0, 0, 0, meta, nowUnixMs: Now);

            Assert.Equal(Now + IntervalMs, state.GaugeNextUpdateUnixMs);
            Assert.NotNull(state.PlayGauge);
            Assert.Equal(250, state.PlayGauge.RatioPermille);
        }

        [Fact]
        public void ApplyGaugeSync_RealtimeOrLegacyMeta_LeavesNextUpdateZero()
        {
            var state = new RolesClientState();

            state.ApplyGaugeSync(250, 0, 0, 0, 0, new[] { 10000, 15, 30, 50, 60, 10, 0 }, nowUnixMs: Now);
            Assert.Equal(0, state.GaugeNextUpdateUnixMs);

            state.ApplyGaugeSync(250, 0, 0, 0, 0, new[] { 10000, 15, 30, 50, 60, 10 }, nowUnixMs: Now);
            Assert.Equal(0, state.GaugeNextUpdateUnixMs);
        }

        [Fact]
        public void Reset_ClearsNextUpdate()
        {
            var state = new RolesClientState();
            state.ApplyGaugeSync(250, 0, 0, 0, 0, new[] { 10000, 15, 30, 50, 60, 10, IntervalSec }, nowUnixMs: Now);

            state.Reset();

            Assert.Equal(0, state.GaugeNextUpdateUnixMs);
        }
    }
}
