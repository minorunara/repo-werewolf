using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class DeliveryGaugeTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const int WolfActor = 1;

        public DeliveryGaugeTests()
        {
            WLog.Sink = (line, secret) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private (RolesSession roles, List<OutboundMessage> sent) BuildScenario(GameConfig config = null)
        {
            var session = new GameSession();
            session.ReserveForcedRole(WolfActor, Role.Werewolf);

            var players = new List<WPlayer>();
            for (int i = 1; i <= 5; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }
            config = config ?? new GameConfig { RoundSeconds = 600, CatGaugeSyncIntervalSec = 0 };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            var roles = new RolesSession(config, session, Now, new Random(1));
            var sent = new List<OutboundMessage>();
            roles.OnSend += sent.Add;

            roles.FreezeBase(47400f);
            sent.Clear();
            return (roles, sent);
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
        public void OnMeetingStarted_WithHaulValues_Carries9thAnd10thElements()
        {
            var (roles, sent) = BuildScenario();

            roles.OnMeetingStarted(Now + 1000, extractedDollars: 12000, haulGoalDollars: 30000);

            int[] data = LastMeetingGaugeData(sent);
            Assert.NotNull(data);
            Assert.True(data.Length >= 10);
            Assert.Equal(12000, data[8]);
            Assert.Equal(30000, data[9]);
        }

        [Fact]
        public void OnMeetingStarted_WithoutHaulValues_CarriesMinusOne()
        {
            var (roles, sent) = BuildScenario();

            roles.OnMeetingStarted(Now + 1000);

            int[] data = LastMeetingGaugeData(sent);
            Assert.NotNull(data);
            Assert.True(data.Length >= 10);
            Assert.Equal(-1, data[8]);
            Assert.Equal(-1, data[9]);
        }

        [Fact]
        public void OnMeetingStarted_BomberPossible_Carries11thElementRefillPct()
        {
            var (roles, sent) = BuildScenario(new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                WerewolfCount = 2,
                BomberChancePercent = 50,
                BomberAmmoRefillPct = 25,
            });

            roles.OnMeetingStarted(Now + 1000);

            int[] data = LastMeetingGaugeData(sent);
            Assert.NotNull(data);
            Assert.True(data.Length >= 11);
            Assert.Equal(25, data[10]);
        }

        [Fact]
        public void OnMeetingStarted_BomberImpossible_Carries11thElementZero()
        {
            var (roles, sent) = BuildScenario(new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                BomberChancePercent = 0,
            });

            roles.OnMeetingStarted(Now + 1000);

            int[] data = LastMeetingGaugeData(sent);
            Assert.NotNull(data);
            Assert.Equal(0, data[10]);
        }

        [Fact]
        public void ApplyMeetingGauge_With11Elements_SetsDeliveryFields()
        {
            var state = new RolesClientState();

            state.ApplyMeetingGauge(new[] { 10, 47400, 10, 30, 50, 70, 10, 500, 12000, 30000, 25 });

            Assert.NotNull(state.MeetingGauge);
            Assert.Equal(12000, state.MeetingGauge.ExtractedDollars);
            Assert.Equal(30000, state.MeetingGauge.HaulGoalDollars);
            Assert.Equal(25, state.MeetingGauge.BombRefillPct);
        }

        [Fact]
        public void ApplyMeetingGauge_LegacyData_LeavesDeliveryUnknown()
        {
            var state = new RolesClientState();

            state.ApplyMeetingGauge(new[] { 10, 47400, 10, 30, 50, 70, 10, 500 });

            Assert.NotNull(state.MeetingGauge);
            Assert.Equal(-1, state.MeetingGauge.ExtractedDollars);
            Assert.Equal(-1, state.MeetingGauge.HaulGoalDollars);
            Assert.Equal(-1, state.MeetingGauge.BombRefillPct);
        }

        [Fact]
        public void DeliveryPermille_UsesBaseDollarsScale()
        {
            var s = new MeetingGaugeSnapshot
            {
                BaseDollars = 40000,
                ExtractedDollars = 10000,
                HaulGoalDollars = 30000,
            };

            Assert.Equal(250, s.DeliveryPermille());
            Assert.Equal(750, s.QuotaPermille());
        }

        [Fact]
        public void DeliveryPermille_ClampsAt1000_WhenExceedingBase()
        {
            var s = new MeetingGaugeSnapshot
            {
                BaseDollars = 10000,
                ExtractedDollars = 12000,
                HaulGoalDollars = 15000,
            };

            Assert.Equal(1000, s.DeliveryPermille());
            Assert.Equal(1000, s.QuotaPermille());
        }

        [Fact]
        public void DeliveryPermille_InvalidData_ReturnsMinusOne()
        {
            Assert.Equal(-1, new MeetingGaugeSnapshot
            {
                BaseDollars = 10000,
            }.DeliveryPermille());

            Assert.Equal(-1, new MeetingGaugeSnapshot
            {
                BaseDollars = 0,
                ExtractedDollars = 5000,
            }.DeliveryPermille());

            Assert.Equal(-1, new MeetingGaugeSnapshot
            {
                BaseDollars = 10000,
            }.QuotaPermille());
        }
    }
}
