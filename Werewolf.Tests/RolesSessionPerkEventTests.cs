using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class RolesSessionPerkEventTests : IDisposable
    {
        private const long Now = 1_000_000L;

        public RolesSessionPerkEventTests()
        {
            WLog.Sink = (line, secret) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Fact]
        public void AddValueLoss_FiresOnPerkUnlocked_OncePerPerk_InJudgeOrder()
        {
            var session = new GameSession();
            session.ReserveForcedRole(1, Role.Werewolf);
            var players = new List<WPlayer>();
            for (int i = 1; i <= 5; i++)
            {
                players.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            }
            var config = new GameConfig
            {
                RoundSeconds = 600,
                CatGaugeSyncIntervalSec = 0,
                StaminaUnlockPct = 10,
                JumpUnlockPct = 20,
                EnemyIgnoreUnlockPct = 30,
                HealUnlockPct = 40,
            };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            var roles = new RolesSession(config, session, Now, new Random(1));
            roles.OnSend += _ => { };
            var unlocked = new List<PerkId>();
            roles.OnPerkUnlocked += unlocked.Add;

            roles.FreezeBase(1000f);
            roles.AddValueLoss(350f, isOrb: false);

            Assert.Equal(
                new[] { PerkId.InfiniteStamina, PerkId.InfiniteJump, PerkId.EnemyIgnore },
                unlocked);

            unlocked.Clear();
            roles.AddValueLoss(100f, isOrb: false);
            Assert.Equal(new[] { PerkId.NaturalHeal }, unlocked);
        }
    }
}
