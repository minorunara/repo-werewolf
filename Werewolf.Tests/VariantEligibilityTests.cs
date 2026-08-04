using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class VariantEligibilityTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const int WolfActor = 1;

        public VariantEligibilityTests()
        {
            WLog.Sink = (line, secret) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Theory]
        [InlineData(5, 3, 3)]
        [InlineData(5, 1, 1)]
        [InlineData(3, 5, 2)]
        [InlineData(5, 0, 1)]
        public void CorrectedWerewolfSlots_MatchesAssignCorrection(
            int playerCount, int n, int expected)
        {
            var config = new GameConfig { WerewolfCount = n };

            Assert.Equal(expected, RoleAssigner.CorrectedWerewolfSlots(config, playerCount));
        }

        [Fact]
        public void Assign_NoBomber_WhenSingleWolfSlot_EvenWithFullChance()
        {
            var config = new GameConfig
            {
                WerewolfCount = 1,
                BlackCatChancePercent = 0,
                BomberChancePercent = 100,
            };

            for (int seed = 0; seed < 10; seed++)
            {
                var result = RoleAssigner.Assign(MakePlayers(5), config, new Random(seed));
                Assert.Equal(0, result.Bombers);
                Assert.Equal(1, result.Werewolves);
            }
        }

        [Fact]
        public void Assign_NoBlackCat_WhenSingleVillagerSlot_EvenWithFullChance()
        {
            var config = new GameConfig
            {
                WerewolfCount = 2,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
            };

            for (int seed = 0; seed < 10; seed++)
            {
                var result = RoleAssigner.Assign(MakePlayers(3), config, new Random(seed));
                Assert.Equal(0, result.BlackCats);
            }
        }

        [Fact]
        public void Assign_NoBlackCat_AfterPlayerCountCorrection()
        {
            var config = new GameConfig
            {
                WerewolfCount = 5,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
            };

            for (int seed = 0; seed < 10; seed++)
            {
                var result = RoleAssigner.Assign(MakePlayers(3), config, new Random(seed));
                Assert.Equal(0, result.BlackCats);
            }
        }

        private static List<WPlayer> MakePlayers(int count)
        {
            var players = new List<WPlayer>();
            for (int i = 1; i <= count; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }
            return players;
        }

        [Theory]
        [InlineData(50, 2, 5, true)]
        [InlineData(0, 2, 5, false)]
        [InlineData(100, 1, 5, true)]
        [InlineData(50, 2, 4, true)]
        [InlineData(100, 4, 5, false)]
        [InlineData(100, 2, 3, false)]
        [InlineData(100, 5, 3, false)]
        public void BlackCatPossible_RequiresChanceAndVillagerSlots(
            int chance, int n, int playerCount, bool expected)
        {
            var config = new GameConfig
            {
                BlackCatChancePercent = chance,
                WerewolfCount = n,
            };

            Assert.Equal(expected, config.BlackCatPossible(playerCount));
        }

        [Theory]
        [InlineData(50, 2, 5, true)]
        [InlineData(0, 2, 5, false)]
        [InlineData(100, 1, 5, false)]
        [InlineData(50, 5, 3, true)]
        [InlineData(100, 1, 3, false)]
        public void BomberPossible_RequiresChanceAndTwoWolfSlots(
            int chance, int n, int playerCount, bool expected)
        {
            var config = new GameConfig
            {
                BomberChancePercent = chance,
                WerewolfCount = n,
            };

            Assert.Equal(expected, config.BomberPossible(playerCount));
        }

        private (RolesSession roles, List<OutboundMessage> sent) BuildScenario(GameConfig config)
        {
            var session = new GameSession();
            session.ReserveForcedRole(WolfActor, Role.Werewolf);

            var players = new List<WPlayer>();
            for (int i = 1; i <= 5; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            var roles = new RolesSession(config, session, Now, new Random(1));
            var sent = new List<OutboundMessage>();
            roles.OnSend += sent.Add;

            roles.FreezeBase(10000f);
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
        public void MeetingGauge_CatPossible_CarriesInformantThreshold()
        {
            var (roles, sent) = BuildScenario(new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                WerewolfCount = 2,
                BlackCatChancePercent = 50,
                InformantThresholdPct = 60,
            });

            roles.OnMeetingStarted(Now + 1000);

            int[] data = LastMeetingGaugeData(sent);
            Assert.NotNull(data);
            Assert.Equal(60, data[5]);
        }

        [Fact]
        public void MeetingGauge_CatChanceZero_CarriesInformantZero()
        {
            var (roles, sent) = BuildScenario(new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                WerewolfCount = 2,
                BlackCatChancePercent = 0,
                InformantThresholdPct = 60,
            });

            roles.OnMeetingStarted(Now + 1000);

            int[] data = LastMeetingGaugeData(sent);
            Assert.NotNull(data);
            Assert.Equal(0, data[5]);
        }

        [Fact]
        public void MeetingGauge_NoVillagerSlot_CarriesInformantZero()
        {
            var (roles, sent) = BuildScenario(new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                WerewolfCount = 4,
                BlackCatChancePercent = 50,
                InformantThresholdPct = 60,
            });

            roles.OnMeetingStarted(Now + 1000);

            int[] data = LastMeetingGaugeData(sent);
            Assert.NotNull(data);
            Assert.Equal(0, data[5]);
        }

        [Fact]
        public void MeetingGauge_SingleWolfSlot_CarriesBombRefillZero()
        {
            var (roles, sent) = BuildScenario(new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                WerewolfCount = 1,
                BomberChancePercent = 50,
                BomberAmmoRefillPct = 25,
            });

            roles.OnMeetingStarted(Now + 1000);

            int[] data = LastMeetingGaugeData(sent);
            Assert.NotNull(data);
            Assert.Equal(0, data[10]);
        }

        [Fact]
        public void GaugeMeta_CatPossible_CarriesInformantThreshold()
        {
            var (roles, sent) = BuildScenario(new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                WerewolfCount = 2,
                BlackCatChancePercent = 50,
                InformantThresholdPct = 60,
            });

            roles.AddValueLoss(500f, isOrb: false);

            int[] meta = LastGaugeMetaTo(sent, WolfActor);
            Assert.NotNull(meta);
            Assert.Equal(60, meta[4]);
        }

        [Fact]
        public void GaugeMeta_CatChanceZero_CarriesInformantZero()
        {
            var (roles, sent) = BuildScenario(new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                WerewolfCount = 2,
                BlackCatChancePercent = 0,
                InformantThresholdPct = 60,
            });

            roles.AddValueLoss(500f, isOrb: false);

            int[] meta = LastGaugeMetaTo(sent, WolfActor);
            Assert.NotNull(meta);
            Assert.Equal(0, meta[4]);
        }

        [Fact]
        public void GaugeMeta_NoVillagerSlot_CarriesInformantZero()
        {
            var (roles, sent) = BuildScenario(new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                WerewolfCount = 4,
                BlackCatChancePercent = 50,
                InformantThresholdPct = 60,
            });

            roles.AddValueLoss(500f, isOrb: false);

            int[] meta = LastGaugeMetaTo(sent, WolfActor);
            Assert.NotNull(meta);
            Assert.Equal(0, meta[4]);
        }
    }
}
