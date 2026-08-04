using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class RoleAssignerTests : IDisposable
    {
        private readonly List<(string Line, bool Secret)> _log = new List<(string, bool)>();

        public RoleAssignerTests()
        {
            WLog.Sink = (line, secret) => _log.Add((line, secret));
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        private static List<WPlayer> MakePlayers(int count)
        {
            var list = new List<WPlayer>(count);
            for (int i = 1; i <= count; i++)
            {
                list.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }
            return list;
        }

        private static int CountRole(IEnumerable<WPlayer> players, Role role)
            => players.Count(p => p.Role == role);

        private static int CountTeam(IEnumerable<WPlayer> players)
            => players.Count(p => p.Role == Role.Werewolf || p.Role == Role.BlackCat || p.Role == Role.Bomber);

        private static int CountWolfSlots(IEnumerable<WPlayer> players)
            => players.Count(p => p.Role == Role.Werewolf || p.Role == Role.Bomber);

        [Theory]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(10)]
        public void Assign_DefaultN_IsOneRegardlessOfPlayerCount(int playerCount)
        {
            var players = MakePlayers(playerCount);
            var config = new GameConfig { BlackCatChancePercent = 0, BomberChancePercent = 0 };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(1, result.Werewolves);
            Assert.Equal(0, result.BlackCats);
            Assert.Equal(0, result.Bombers);
            Assert.Equal(1, CountTeam(players));
            Assert.False(result.Corrected);
        }

        [Fact]
        public void Assign_BothVariantsGuaranteed_CatComesFromVillagerSide()
        {
            var players = MakePlayers(7);
            var config = new GameConfig
            {
                WerewolfCount = 3,
                BlackCatChancePercent = 100,
                BomberChancePercent = 100,
                ShamanChancePercent = 0,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(2, result.Werewolves);
            Assert.Equal(1, result.Bombers);
            Assert.Equal(1, result.BlackCats);
            Assert.Equal(3, CountWolfSlots(players));
            Assert.Equal(4, CountTeam(players));
            Assert.Equal(3, CountRole(players, Role.Villager));
        }

        [Fact]
        public void Assign_BomberNotDrawn_WhenOnlyOneWolfSlot()
        {
            var players = MakePlayers(5);
            var config = new GameConfig
            {
                WerewolfCount = 1,
                BlackCatChancePercent = 0,
                BomberChancePercent = 100,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(0, result.Bombers);
            Assert.Equal(1, result.Werewolves);
        }

        [Fact]
        public void Assign_BlackCatNotDrawn_WhenOnlyOneVillagerSlot()
        {
            var players = MakePlayers(3);
            var config = new GameConfig
            {
                WerewolfCount = 2,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
                ShamanChancePercent = 0,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(0, result.BlackCats);
            Assert.Equal(2, result.Werewolves);
            Assert.Equal(1, CountRole(players, Role.Villager));
        }

        [Fact]
        public void Assign_BlackCat100_ConvertsExactlyOneVillager_WolfSlotsUnaffected()
        {
            var players = MakePlayers(5);
            var config = new GameConfig
            {
                WerewolfCount = 2,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
                ShamanChancePercent = 0,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(1, result.BlackCats);
            Assert.Equal(2, result.Werewolves);
            Assert.Equal(2, CountWolfSlots(players));
            Assert.Equal(3, CountTeam(players));
            Assert.Equal(2, CountRole(players, Role.Villager));
        }

        [Fact]
        public void Assign_ChanceZero_NeverPicksVariant()
        {
            var players = MakePlayers(5);
            var config = new GameConfig
            {
                WerewolfCount = 3,
                BlackCatChancePercent = 0,
                BomberChancePercent = 100,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(0, result.BlackCats);
            Assert.Equal(1, result.Bombers);
            Assert.Equal(2, result.Werewolves);
        }

        [Fact]
        public void Assign_NExceedsPlayers_IsCorrectedToPlayersMinusOne()
        {
            var players = MakePlayers(5);
            var config = new GameConfig
            {
                WerewolfCount = 5,
                BlackCatChancePercent = 0,
                BomberChancePercent = 0,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(4, CountTeam(players));
            Assert.Equal(4, result.Werewolves);
            Assert.True(result.Corrected);
            Assert.Contains(_log, e => e.Line.Contains("abnormal_config"));
        }

        [Fact]
        public void Assign_NCorrectedToPlayersMinusOne_SuppressesBlackCat()
        {
            var players = MakePlayers(5);
            var config = new GameConfig
            {
                WerewolfCount = 9,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
                ShamanChancePercent = 0,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(0, result.BlackCats);
            Assert.Equal(4, result.Werewolves);
            Assert.Equal(1, CountRole(players, Role.Villager));
        }

        [Fact]
        public void Assign_ForcedBomber_ConsumesWolfSlot_CatDrawIndependent()
        {
            var players = MakePlayers(5);
            var forced = new Dictionary<int, Role> { [3] = Role.Bomber };
            var config = new GameConfig
            {
                WerewolfCount = 2,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1), forced);

            Assert.Equal(Role.Bomber, players.Single(p => p.ActorNumber == 3).Role);
            Assert.Equal(1, result.Bombers);
            Assert.Equal(1, result.Werewolves);
            Assert.Equal(1, result.BlackCats);
            Assert.Equal(2, CountWolfSlots(players));
        }

        [Fact]
        public void Assign_ForcedWerewolf_ConsumesFillSlot()
        {
            var players = MakePlayers(7);
            var config = new GameConfig
            {
                WerewolfCount = 3,
                BlackCatChancePercent = 0,
                BomberChancePercent = 0,
            };
            var forced = new Dictionary<int, Role> { [2] = Role.Werewolf };

            var result = RoleAssigner.Assign(players, config, new Random(1), forced);

            Assert.Equal(Role.Werewolf, players.Single(p => p.ActorNumber == 2).Role);
            Assert.Equal(3, result.Werewolves);
        }

        [Fact]
        public void Assign_ForcedBlackCat_SuppressesRandomDraw()
        {
            var players = MakePlayers(5);
            var config = new GameConfig
            {
                WerewolfCount = 1,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
            };
            var forced = new Dictionary<int, Role> { [4] = Role.BlackCat };

            var result = RoleAssigner.Assign(players, config, new Random(1), forced);

            Assert.Equal(1, result.BlackCats);
            Assert.Equal(Role.BlackCat, players.Single(p => p.ActorNumber == 4).Role);
        }

        [Fact]
        public void Assign_ForcedBlackCat_BypassesVillagerSlotGate()
        {
            var players = MakePlayers(3);
            var config = new GameConfig
            {
                WerewolfCount = 2,
                BlackCatChancePercent = 0,
                BomberChancePercent = 0,
            };
            var forced = new Dictionary<int, Role> { [1] = Role.BlackCat };

            var result = RoleAssigner.Assign(players, config, new Random(1), forced);

            Assert.Equal(1, result.BlackCats);
            Assert.Equal(2, result.Werewolves);
        }

        [Fact]
        public void Assign_ForcedVillager_ExcludedFromRandomPool()
        {
            var players = MakePlayers(3);
            var config = new GameConfig { BlackCatChancePercent = 0, BomberChancePercent = 0 };
            var forced = new Dictionary<int, Role> { [1] = Role.Villager };

            RoleAssigner.Assign(players, config, new Random(1), forced);

            Assert.Equal(Role.Villager, players.Single(p => p.ActorNumber == 1).Role);
            Assert.Equal(1, CountRole(players, Role.Werewolf));
        }

        [Fact]
        public void Assign_SameSeed_ProducesIdenticalTable()
        {
            var a = MakePlayers(9);
            var b = MakePlayers(9);
            var config = new GameConfig
            {
                WerewolfCount = 3,
                BlackCatChancePercent = 50,
                BomberChancePercent = 50,
            };

            RoleAssigner.Assign(a, config, new Random(42));
            RoleAssigner.Assign(b, config, new Random(42));

            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(a[i].Role, b[i].Role);
            }
        }

        [Fact]
        public void Assign_DifferentSeeds_ProduceDifferentTablesAtLeastOnce()
        {
            var baseline = MakePlayers(9);
            var config = new GameConfig();
            RoleAssigner.Assign(baseline, config, new Random(0));

            bool anyDifferent = false;
            for (int seed = 1; seed <= 20 && !anyDifferent; seed++)
            {
                var other = MakePlayers(9);
                RoleAssigner.Assign(other, config, new Random(seed));
                anyDifferent = Enumerable.Range(0, 9).Any(i => baseline[i].Role != other[i].Role);
            }

            Assert.True(anyDifferent, "20シード試しても割当が一度も変化しない");
        }

        [Fact]
        public void Assign_Bots_AreTreatedSameAsRealPlayers()
        {
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Name = "Host" },
                new WPlayer { ActorNumber = -1, Name = "Bot1", IsBot = true },
                new WPlayer { ActorNumber = -2, Name = "Bot2", IsBot = true },
                new WPlayer { ActorNumber = -3, Name = "Bot3", IsBot = true },
                new WPlayer { ActorNumber = -4, Name = "Bot4", IsBot = true },
            };
            var config = new GameConfig
            {
                WerewolfCount = 2,
                BlackCatChancePercent = 100,
                BomberChancePercent = 0,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(2, result.Werewolves);
            Assert.Equal(1, result.BlackCats);
            Assert.Equal(3, CountTeam(players));
        }

        [Fact]
        public void Assign_LessThanThreePlayers_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RoleAssigner.Assign(MakePlayers(2), new GameConfig(), new Random(1)));
        }

        [Fact]
        public void Assign_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(
                () => RoleAssigner.Assign(null, new GameConfig(), new Random(1)));
            Assert.Throws<ArgumentNullException>(
                () => RoleAssigner.Assign(MakePlayers(3), null, new Random(1)));
            Assert.Throws<ArgumentNullException>(
                () => RoleAssigner.Assign(MakePlayers(3), new GameConfig(), null));
        }

        [Fact]
        public void Assign_Invariants_HoldAcrossManySeeds()
        {
            var config = new GameConfig
            {
                WerewolfCount = 3,
                BlackCatChancePercent = 50,
                BomberChancePercent = 50,
            };
            for (int seed = 0; seed < 30; seed++)
            {
                var players = MakePlayers(7);
                var result = RoleAssigner.Assign(players, config, new Random(seed));
                Assert.Equal(3, CountWolfSlots(players));
                Assert.Equal(3, result.Werewolves + result.Bombers);
                Assert.Equal(3 + result.BlackCats, CountTeam(players));
                Assert.True(result.Werewolves >= 1, "純人狼は最低1（爆弾魔の固定規則）");
                Assert.True(CountRole(players, Role.Villager) >= 1, "村人が最低1人残る");
            }
        }

        [Fact]
        public void Assign_VillagerTeam_NeverEmpty_EvenAtMaxN()
        {
            for (int playerCount = 3; playerCount <= 6; playerCount++)
            {
                for (int seed = 0; seed < 10; seed++)
                {
                    var players = MakePlayers(playerCount);
                    var config = new GameConfig
                    {
                        WerewolfCount = playerCount - 1,
                        BlackCatChancePercent = 100,
                        BomberChancePercent = 100,
                    };
                    RoleAssigner.Assign(players, config, new Random(seed));
                    int villagerTeam = players.Count(p => RoleDistribution.TeamOf(p.Role) == Team.Villagers);
                    Assert.True(villagerTeam >= 1, "村人陣営が0人になった");
                }
            }
        }

        [Fact]
        public void Assign_Shaman100_ConvertsExactlyOneVillager_TeamCountUnaffected()
        {
            var players = MakePlayers(5);
            var config = new GameConfig
            {
                WerewolfCount = 2,
                ShamanChancePercent = 100,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(1, result.Shamans);
            Assert.Equal(1, CountRole(players, Role.Shaman));
            Assert.Equal(2, CountTeam(players));
            Assert.Equal(2, CountRole(players, Role.Villager));
        }

        [Fact]
        public void Assign_Shaman0_NeverAppears()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var players = MakePlayers(5);
                var config = new GameConfig { ShamanChancePercent = 0 };
                var result = RoleAssigner.Assign(players, config, new Random(seed));
                Assert.Equal(0, result.Shamans);
                Assert.Equal(0, CountRole(players, Role.Shaman));
            }
        }

        [Fact]
        public void Assign_ShamanChance_DoesNotAffectEarlierDraws()
        {
            var config1 = new GameConfig
            {
                WerewolfCount = 3,
                BlackCatChancePercent = 50,
                BomberChancePercent = 50,
                ShamanChancePercent = 0,
            };
            var config2 = new GameConfig
            {
                WerewolfCount = 3,
                BlackCatChancePercent = 50,
                BomberChancePercent = 50,
                ShamanChancePercent = 100,
            };
            for (int seed = 0; seed < 10; seed++)
            {
                var players1 = MakePlayers(7);
                var players2 = MakePlayers(7);
                RoleAssigner.Assign(players1, config1, new Random(seed));
                RoleAssigner.Assign(players2, config2, new Random(seed));
                for (int i = 0; i < players1.Count; i++)
                {
                    Role r1 = players1[i].Role;
                    Role r2 = players2[i].Role;
                    if (r2 == Role.Shaman)
                    {
                        Assert.Equal(Role.Villager, r1);
                    }
                    else
                    {
                        Assert.Equal(r1, r2);
                    }
                }
            }
        }

        [Fact]
        public void Assign_CatAndShaman_AreAlwaysDistinctPlayers()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var players = MakePlayers(5);
                var config = new GameConfig
                {
                    WerewolfCount = 1,
                    BlackCatChancePercent = 100,
                    ShamanChancePercent = 100,
                };
                var result = RoleAssigner.Assign(players, config, new Random(seed));
                Assert.Equal(1, result.BlackCats);
                Assert.Equal(1, result.Shamans);
                Assert.Equal(1, result.Werewolves);
            }
        }

        [Fact]
        public void Assign_ForcedShaman_SuppressesRandomDraw()
        {
            var players = MakePlayers(5);
            var config = new GameConfig { ShamanChancePercent = 100 };
            var forced = new Dictionary<int, Role> { [4] = Role.Shaman };

            var result = RoleAssigner.Assign(players, config, new Random(1), forced);

            Assert.Equal(1, result.Shamans);
            Assert.Equal(Role.Shaman, players.Single(p => p.ActorNumber == 4).Role);
            Assert.Equal(1, CountRole(players, Role.Shaman));
        }

        [Fact]
        public void Assign_MinimalLobby_ShamanCanTakeTheOnlyVillagerSlot()
        {
            var players = MakePlayers(3);
            var config = new GameConfig
            {
                WerewolfCount = 2,
                ShamanChancePercent = 100,
            };

            var result = RoleAssigner.Assign(players, config, new Random(1));

            Assert.Equal(1, result.Shamans);
            Assert.Equal(2, CountTeam(players));
            Assert.Equal(0, CountRole(players, Role.Villager));
        }
    }
}
