using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GaugeLostDollarsTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const int WolfActor = 1;

        public GaugeLostDollarsTests()
        {
            WLog.Sink = (line, secret) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private (RolesSession roles, List<OutboundMessage> sent) BuildScenario()
        {
            var session = new GameSession();
            session.ReserveForcedRole(WolfActor, Role.Werewolf);

            var players = new List<WPlayer>();
            for (int i = 1; i <= 5; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }
            var config = new GameConfig { RoundSeconds = 600, CatGaugeSyncIntervalSec = 0 };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            var roles = new RolesSession(config, session, Now, new Random(1));
            var sent = new List<OutboundMessage>();
            roles.OnSend += sent.Add;

            roles.FreezeBase(47400f);
            sent.Clear();
            return (roles, sent);
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

        private static int[] LastMeetingGaugeData(List<OutboundMessage> sent)
        {
            int[] data = null;
            foreach (var m in sent)
            {
                if (m.Code != WWRolesCodes.RoleState) continue;
                if ((byte)m.Payload[0] != RoleStateSubtype.MeetingGauge) continue;
                data = (int[])m.Payload[1];
            }
            return data;
        }

        [Fact]
        public void SyncPerkGauge_Meta8thElement_CarriesExactLostDollars()
        {
            var (roles, sent) = BuildScenario();

            roles.AddValueLoss(500f, isOrb: false);

            int[] meta = LastGaugeMetaTo(sent, WolfActor);
            Assert.NotNull(meta);
            Assert.True(meta.Length >= 8);
            Assert.Equal(500, meta[7]);
        }

        [Fact]
        public void MeetingGauge_Data8thElement_CarriesExactLostDollars()
        {
            var (roles, sent) = BuildScenario();
            roles.AddValueLoss(500f, isOrb: false);
            sent.Clear();

            roles.OnMeetingStarted(Now + 1000);

            int[] data = LastMeetingGaugeData(sent);
            Assert.NotNull(data);
            Assert.True(data.Length >= 8);
            Assert.Equal(500, data[7]);
            Assert.Equal(10, data[0]);
        }

        [Fact]
        public void ApplyGaugeSync_With8thMetaElement_SetsLostDollars()
        {
            var state = new RolesClientState();

            state.ApplyGaugeSync(10, 0, 0, 0, 0,
                new[] { 47400, 10, 30, 50, 70, 10, 0, 500 }, nowUnixMs: Now);

            Assert.NotNull(state.PlayGauge);
            Assert.Equal(500, state.PlayGauge.LostDollars);
        }

        [Fact]
        public void ApplyGaugeSync_LegacyMeta_LeavesLostDollarsUnknown()
        {
            var state = new RolesClientState();

            state.ApplyGaugeSync(10, 0, 0, 0, 0,
                new[] { 47400, 10, 30, 50, 70, 10, 0 }, nowUnixMs: Now);

            Assert.NotNull(state.PlayGauge);
            Assert.Equal(-1, state.PlayGauge.LostDollars);
        }

        [Fact]
        public void ApplyMeetingGauge_With8thElement_SetsLostDollars()
        {
            var state = new RolesClientState();

            state.ApplyMeetingGauge(new[] { 10, 47400, 10, 30, 50, 70, 10, 500 });

            Assert.NotNull(state.MeetingGauge);
            Assert.Equal(500, state.MeetingGauge.LostDollars);
        }

        [Fact]
        public void ApplyMeetingGauge_LegacyData_LeavesLostDollarsUnknown()
        {
            var state = new RolesClientState();

            state.ApplyMeetingGauge(new[] { 10, 47400, 10, 30, 50, 70, 10 });

            Assert.NotNull(state.MeetingGauge);
            Assert.Equal(-1, state.MeetingGauge.LostDollars);
        }
    }
}
