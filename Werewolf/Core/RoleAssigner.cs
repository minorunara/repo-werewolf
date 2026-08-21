using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public readonly struct RoleAssignResult
    {
        public RoleAssignResult(int werewolves, int blackCats, int bombers, int shamans, bool corrected)
        {
            Werewolves = werewolves;
            BlackCats = blackCats;
            Bombers = bombers;
            Shamans = shamans;
            Corrected = corrected;
        }

        public int Werewolves { get; }

        public int BlackCats { get; }

        public int Bombers { get; }

        public int Shamans { get; }

        public bool Corrected { get; }
    }

    public static class RoleAssigner
    {
        public static int CorrectedWerewolfSlots(GameConfig config, int playerCount)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var (n, _) = CorrectWerewolfSlots(config, playerCount);
            return n;
        }

        private static (int N, bool Corrected) CorrectWerewolfSlots(GameConfig config, int playerCount)
        {
            bool corrected = false;
            int n = config.WerewolfCount;
            if (n >= playerCount)
            {
                n = playerCount - 1;
                corrected = true;
            }
            if (n < 1)
            {
                n = 1;
                corrected = true;
            }
            return (n, corrected);
        }

        public static RoleAssignResult Assign(
            IReadOnlyList<WPlayer> players, GameConfig config, Random rng,
            IReadOnlyDictionary<int, Role> forcedRoles = null)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (players.Count < PlayerCountGate.MinimumPlayers)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(players), players.Count, "役職割当は3人以上でのみ定義される。");
            }

            var (n, corrected) = CorrectWerewolfSlots(config, players.Count);

            bool forcedBomberExists = false, forcedBlackCatExists = false, forcedShamanExists = false;
            if (forcedRoles != null)
            {
                foreach (var pair in forcedRoles)
                {
                    if (pair.Value == Role.Bomber) forcedBomberExists = true;
                    else if (pair.Value == Role.BlackCat) forcedBlackCatExists = true;
                    else if (pair.Value == Role.Shaman) forcedShamanExists = true;
                }
            }

            int bomberSlots = 0;
            if (forcedBomberExists)
            {
                bomberSlots = 1;
            }
            else if (n >= 2 && config.BomberChancePercent > 0)
            {
                int chance = config.BomberChancePercent;
                if (chance >= 100 || rng.Next(100) < chance) bomberSlots = 1;
            }

            int werewolfSlots = n - bomberSlots;
            if (werewolfSlots < 0) werewolfSlots = 0;

            var randomPool = new List<WPlayer>(players.Count);
            int forcedWw = 0, forcedBc = 0, forcedBomber = 0, forcedShaman = 0;
            foreach (var player in players)
            {
                if (forcedRoles != null && forcedRoles.TryGetValue(player.ActorNumber, out var forced))
                {
                    player.Role = forced;
                    switch (forced)
                    {
                        case Role.Werewolf: forcedWw++; break;
                        case Role.BlackCat: forcedBc++; break;
                        case Role.Bomber: forcedBomber++; break;
                        case Role.Shaman: forcedShaman++; break;
                    }
                }
                else
                {
                    player.Role = Role.Villager;
                    randomPool.Add(player);
                }
            }

            int remainingWw = Math.Max(0, werewolfSlots - forcedWw);
            int remainingBomber = Math.Max(0, bomberSlots - forcedBomber);

            Shuffle(randomPool, rng);
            int index = 0;
            for (int i = 0; i < remainingWw && index < randomPool.Count; i++)
                randomPool[index++].Role = Role.Werewolf;
            for (int i = 0; i < remainingBomber && index < randomPool.Count; i++)
                randomPool[index++].Role = Role.Bomber;

            int blackCatSlots = 0;
            if (!forcedBlackCatExists)
            {
                int chance = config.BlackCatChancePercent;
                if (chance > 0 && players.Count - n >= 2 && (chance >= 100 || rng.Next(100) < chance))
                {
                    blackCatSlots = 1;
                }
            }
            for (int i = 0; i < blackCatSlots && index < randomPool.Count; i++)
                randomPool[index++].Role = Role.BlackCat;

            int shamanSlots = 0;
            if (!forcedShamanExists)
            {
                int shamanChance = config.ShamanChancePercent;
                if (shamanChance > 0 && (shamanChance >= 100 || rng.Next(100) < shamanChance))
                {
                    shamanSlots = 1;
                }
            }
            for (int i = 0; i < shamanSlots && index < randomPool.Count; i++)
                randomPool[index++].Role = Role.Shaman;

            int finalWw = 0, finalBc = 0, finalBomber = 0, finalShaman = 0;
            foreach (var player in players)
            {
                switch (player.Role)
                {
                    case Role.Werewolf: finalWw++; break;
                    case Role.BlackCat: finalBc++; break;
                    case Role.Bomber: finalBomber++; break;
                    case Role.Shaman: finalShaman++; break;
                }
            }

            if (corrected)
            {
                WLog.Line("warn", secret: false,
                    ("reason", "abnormal_config"),
                    ("players", players.Count),
                    ("n", n));
            }

            WLog.Line("assign", secret: true,
                ("players", players.Count),
                ("werewolves", finalWw),
                ("blackcats", finalBc),
                ("bombers", finalBomber),
                ("shamans", finalShaman),
                ("corrected", corrected));

            return new RoleAssignResult(finalWw, finalBc, finalBomber, finalShaman, corrected);
        }

        private static void Shuffle(List<WPlayer> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
