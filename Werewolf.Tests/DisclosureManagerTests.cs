using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class DisclosureManagerTests
    {
        private const long GameStart = 1_000_000;
        private const int DelaySec = 60;

        private static List<WPlayer> Make(params Role[] roles)
        {
            var list = new List<WPlayer>(roles.Length);
            for (int i = 0; i < roles.Length; i++)
            {
                list.Add(new WPlayer { ActorNumber = i + 1, Name = "P" + (i + 1), Role = roles[i] });
            }
            return list;
        }

        private static DisclosureManager Create(List<WPlayer> players)
            => new DisclosureManager(players, GameStart, DelaySec);

        [Fact]
        public void Initial_EveryPlayerGetsOwnRoleNotice_TargetedToSelfOnly()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.IssueInitialDisclosures();

            var notices = disclosures.Where(d => d.Type == DisclosureType.RoleNotice).ToList();
            Assert.Equal(3, notices.Count);
            foreach (var player in players)
            {
                var notice = notices.Single(n => n.TargetActors.Single() == player.ActorNumber);
                Assert.Equal(player.Role, notice.ShownRole);
            }
        }

        [Fact]
        public void Initial_BlackCatIsNotifiedAsVillager()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.IssueInitialDisclosures();

            var catNotice = disclosures.Single(
                d => d.Type == DisclosureType.RoleNotice && d.TargetActors.Single() == 2);
            Assert.Equal(Role.Villager, catNotice.ShownRole);
            var wolfNotice = disclosures.Single(
                d => d.Type == DisclosureType.RoleNotice && d.TargetActors.Single() == 1);
            Assert.Equal(Role.Werewolf, wolfNotice.ShownRole);
        }

        [Fact]
        public void Initial_ZeroDelay_BlackCatIsNotifiedAsBlackCatWithoutLaterReveal()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            var manager = new DisclosureManager(players, GameStart, blackCatRevealDelaySec: 0);

            var disclosures = manager.IssueInitialDisclosures();

            var catNotice = disclosures.Single(
                d => d.Type == DisclosureType.RoleNotice && d.TargetActors.Single() == 2);
            Assert.Equal(Role.BlackCat, catNotice.ShownRole);
            Assert.True(manager.SelfAwarenessIssued);
            Assert.Empty(manager.Tick(GameStart));
            Assert.Empty(manager.NotifyCondition(DisclosureKind.BlackCatSelfAwareness));
        }

        [Fact]
        public void Initial_TwoWerewolves_MutualRevealPerWolf()
        {
            var players = Make(Role.Werewolf, Role.Werewolf, Role.Villager, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.IssueInitialDisclosures();

            var reveals = disclosures.Where(d => d.Type == DisclosureType.TeammatesReveal).ToList();
            Assert.Equal(2, reveals.Count);
            foreach (int wolfActor in new[] { 1, 2 })
            {
                var reveal = reveals.Single(r => r.TargetActors.Single() == wolfActor);
                Assert.Equal(new[] { 1, 2 }, reveal.WerewolfActors.OrderBy(a => a).ToArray());
            }
        }

        [Fact]
        public void Initial_SingleWerewolf_NoTeammatesReveal()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.IssueInitialDisclosures();

            Assert.DoesNotContain(disclosures, d => d.Type == DisclosureType.TeammatesReveal);
        }

        [Fact]
        public void Initial_BlackCatIsNotIncludedInWolfMutualReveal()
        {
            var players = Make(Role.Werewolf, Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.IssueInitialDisclosures();

            var reveals = disclosures.Where(d => d.Type == DisclosureType.TeammatesReveal).ToList();
            Assert.Equal(2, reveals.Count);
            Assert.DoesNotContain(reveals, r => r.TargetActors.Contains(3));
            Assert.All(reveals, r => Assert.Equal(new[] { 1, 2 }, r.WerewolfActors.OrderBy(a => a).ToArray()));
        }

        [Fact]
        public void Initial_RoleNoticesComeBeforeTeammatesReveal()
        {
            var players = Make(Role.Werewolf, Role.Werewolf, Role.Villager, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.IssueInitialDisclosures();

            int lastNotice = disclosures.ToList().FindLastIndex(d => d.Type == DisclosureType.RoleNotice);
            int firstReveal = disclosures.ToList().FindIndex(d => d.Type == DisclosureType.TeammatesReveal);
            Assert.True(lastNotice < firstReveal);
        }

        [Fact]
        public void Initial_SecondCall_ReturnsEmpty()
        {
            var players = Make(Role.Werewolf, Role.Werewolf, Role.Villager);
            var manager = Create(players);

            manager.IssueInitialDisclosures();

            Assert.Empty(manager.IssueInitialDisclosures());
        }

        [Fact]
        public void Tick_BeforeDelay_EmitsNothing()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            var manager = Create(players);

            Assert.Empty(manager.Tick(GameStart + DelaySec * 1000L - 1));
        }

        [Fact]
        public void Tick_AfterDelay_NotifiesBlackCatOnly_Once()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.Tick(GameStart + DelaySec * 1000L);

            var reveal = Assert.Single(disclosures);
            Assert.Equal(DisclosureType.SelfRoleReveal, reveal.Type);
            Assert.Equal(new[] { 2 }, reveal.TargetActors);
            Assert.Equal(Role.BlackCat, reveal.ShownRole);

            Assert.Empty(manager.Tick(GameStart + DelaySec * 1000L + 1));
        }

        [Fact]
        public void Tick_NoBlackCat_NeverEmits()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);
            var manager = Create(players);

            Assert.Empty(manager.Tick(GameStart + DelaySec * 1000L * 10));
        }

        [Fact]
        public void Condition_BlackCatSeesWerewolves_TargetsBlackCatOnly()
        {
            var players = Make(Role.Werewolf, Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.NotifyCondition(DisclosureKind.BlackCatSeesWerewolves);

            var reveal = Assert.Single(disclosures);
            Assert.Equal(DisclosureType.TeammatesReveal, reveal.Type);
            Assert.Equal(new[] { 3 }, reveal.TargetActors);
            Assert.Equal(new[] { 1, 2 }, reveal.WerewolfActors.OrderBy(a => a).ToArray());
        }

        [Fact]
        public void Condition_BlackCatSeesWerewolves_Duplicate_ReturnsEmpty()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            var manager = Create(players);

            manager.NotifyCondition(DisclosureKind.BlackCatSeesWerewolves);

            Assert.Empty(manager.NotifyCondition(DisclosureKind.BlackCatSeesWerewolves));
        }

        [Fact]
        public void Condition_NoBlackCat_ReturnsEmpty()
        {
            var players = Make(Role.Werewolf, Role.Villager, Role.Villager);
            var manager = Create(players);

            Assert.Empty(manager.NotifyCondition(DisclosureKind.BlackCatSeesWerewolves));
        }

        [Fact]
        public void Condition_ForcedSelfAwareness_FiresImmediately_AndSuppressesLaterTick()
        {
            var players = Make(Role.Werewolf, Role.BlackCat, Role.Villager, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.NotifyCondition(DisclosureKind.BlackCatSelfAwareness);

            var reveal = Assert.Single(disclosures);
            Assert.Equal(DisclosureType.SelfRoleReveal, reveal.Type);
            Assert.Equal(new[] { 2 }, reveal.TargetActors);
            Assert.Equal(Role.BlackCat, reveal.ShownRole);

            Assert.Empty(manager.Tick(GameStart + DelaySec * 1000L + 1));
            Assert.Empty(manager.NotifyCondition(DisclosureKind.BlackCatSelfAwareness));
        }

        [Fact]
        public void Initial_BomberIsIncludedInWolfTeamMutualReveal_WithTrueRole()
        {
            var players = Make(Role.Werewolf, Role.Bomber, Role.Villager, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.IssueInitialDisclosures();

            var reveals = disclosures.Where(d => d.Type == DisclosureType.TeammatesReveal).ToList();
            Assert.Equal(2, reveals.Count);
            Assert.Equal(new[] { 1, 2 },
                reveals.Select(r => r.TargetActors.Single()).OrderBy(a => a).ToArray());
            foreach (var reveal in reveals)
            {
                Assert.Equal(new[] { 1, 2 }, reveal.WerewolfActors.OrderBy(a => a).ToArray());
                Assert.NotNull(reveal.WerewolfActorRoles);
                Assert.Equal(reveal.WerewolfActors.Length, reveal.WerewolfActorRoles.Length);
                for (int i = 0; i < reveal.WerewolfActors.Length; i++)
                {
                    Role expected = reveal.WerewolfActors[i] == 1 ? Role.Werewolf : Role.Bomber;
                    Assert.Equal((byte)expected, reveal.WerewolfActorRoles[i]);
                }
            }
        }

        [Fact]
        public void Condition_BlackCatInformant_IncludesBomberActor_WithTrueRoleBytes()
        {
            var players = Make(Role.Werewolf, Role.Bomber, Role.BlackCat, Role.Villager, Role.Villager);
            var manager = Create(players);

            var disclosures = manager.NotifyCondition(DisclosureKind.BlackCatSeesWerewolves);

            var reveal = Assert.Single(disclosures);
            Assert.Equal(new[] { 3 }, reveal.TargetActors);
            Assert.Equal(new[] { 1, 2 }, reveal.WerewolfActors.OrderBy(a => a).ToArray());
            Assert.NotNull(reveal.WerewolfActorRoles);
            Assert.Equal(reveal.WerewolfActors.Length, reveal.WerewolfActorRoles.Length);
            for (int i = 0; i < reveal.WerewolfActors.Length; i++)
            {
                Role expected = reveal.WerewolfActors[i] == 1 ? Role.Werewolf : Role.Bomber;
                Assert.Equal((byte)expected, reveal.WerewolfActorRoles[i]);
            }
        }

        [Fact]
        public void Ctor_NullPlayers_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DisclosureManager(null, GameStart, DelaySec));
        }
    }
}
