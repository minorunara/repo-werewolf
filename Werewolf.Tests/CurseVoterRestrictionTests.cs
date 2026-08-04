using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class CurseVoterRestrictionTests : IDisposable
    {
        private const long Now = 1_000_000;
        private const long Deadline = Now + 10_000;

        public CurseVoterRestrictionTests()
        {
            WLog.Sink = (line, secret) => { };
        }

        public void Dispose()
        {
            WLog.Sink = null;
        }

        [Fact]
        public void VotersFor_ReturnsOnlyVotersForTarget_SortedByActor()
        {
            var box = new VoteBox(new[] { 1, 2, 3, 4 });
            Assert.Equal(VoteRejectReason.None, box.TryCast(3, 2));
            Assert.Equal(VoteRejectReason.None, box.TryCast(1, 2));
            Assert.Equal(VoteRejectReason.None, box.TryCast(2, 1));
            Assert.Equal(VoteRejectReason.None, box.TryCast(4, -1));

            Assert.Equal(new[] { 1, 3 }, box.VotersFor(2));
            Assert.Equal(new[] { 2 }, box.VotersFor(1));
        }

        [Fact]
        public void VotersFor_NoVotesForTarget_ReturnsEmpty()
        {
            var box = new VoteBox(new[] { 1, 2 });
            Assert.Equal(VoteRejectReason.None, box.TryCast(1, -1));
            Assert.Empty(box.VotersFor(2));
        }

        private static List<WPlayer> Roster()
        {
            return new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Role = Role.Villager },
                new WPlayer { ActorNumber = -1, Role = Role.Werewolf },
                new WPlayer { ActorNumber = -2, Role = Role.BlackCat, Alive = false },
                new WPlayer { ActorNumber = -3, Role = Role.Villager },
            };
        }

        [Fact]
        public void Designate_NonVoter_IsRejected()
        {
            var curse = new CurseSession(-2, Deadline, new[] { 1, -1 });
            Assert.False(curse.Designate(-2, -3, Now));
            Assert.True(curse.Designate(-2, 1, Now));
        }

        [Fact]
        public void Designate_RejectedNonVoter_DoesNotOverwritePriorDesignation()
        {
            var curse = new CurseSession(-2, Deadline, new[] { 1, -1 });
            Assert.True(curse.Designate(-2, 1, Now));
            Assert.False(curse.Designate(-2, -3, Now + 1));

            var res = curse.TryResolve(Deadline, Roster(), informantEstablished: false, new Random(1));
            Assert.NotNull(res);
            Assert.True(res.WasDesignated);
            Assert.Equal(1, res.VictimActor);
        }

        [Fact]
        public void TryResolve_RandomFallback_LimitedToVoters()
        {
            var seen = new HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                var curse = new CurseSession(-2, Deadline, new[] { 1, -1 });
                var res = curse.TryResolve(Deadline, Roster(), informantEstablished: false, new Random(seed));
                Assert.NotNull(res);
                seen.Add(res.VictimActor);
            }
            Assert.Contains(1, seen);
            Assert.Contains(-1, seen);
            Assert.DoesNotContain(-3, seen);
        }

        [Fact]
        public void TryResolve_PostInformant_PrefersVillagerVoters()
        {
            var seen = new HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                var curse = new CurseSession(-2, Deadline, new[] { 1, -1 });
                var res = curse.TryResolve(Deadline, Roster(), informantEstablished: true, new Random(seed));
                Assert.NotNull(res);
                seen.Add(res.VictimActor);
            }
            Assert.Contains(1, seen);
            Assert.DoesNotContain(-1, seen);
        }

        private static List<WPlayer> RosterWithVariants()
        {
            return new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Role = Role.Villager },
                new WPlayer { ActorNumber = -1, Role = Role.Werewolf },
                new WPlayer { ActorNumber = -2, Role = Role.BlackCat, Alive = false },
                new WPlayer { ActorNumber = -3, Role = Role.Shaman },
                new WPlayer { ActorNumber = -4, Role = Role.Bomber },
            };
        }

        [Fact]
        public void TryResolve_PostInformant_ShamanVoterCountsAsVillagerTeam()
        {
            var seen = new HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                var curse = new CurseSession(-2, Deadline, new[] { -3, -1 });
                var res = curse.TryResolve(Deadline, RosterWithVariants(),
                    informantEstablished: true, new Random(seed));
                Assert.NotNull(res);
                seen.Add(res.VictimActor);
            }
            Assert.Equal(new HashSet<int> { -3 }, seen);
        }

        [Fact]
        public void TryResolve_NullVoters_PostInformant_IncludesShaman_ExcludesBomber()
        {
            var seen = new HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                var curse = new CurseSession(-2, Deadline);
                var res = curse.TryResolve(Deadline, RosterWithVariants(),
                    informantEstablished: true, new Random(seed));
                Assert.NotNull(res);
                seen.Add(res.VictimActor);
            }
            Assert.Contains(1, seen);
            Assert.Contains(-3, seen);
            Assert.DoesNotContain(-1, seen);
            Assert.DoesNotContain(-4, seen);
        }

        [Fact]
        public void TryResolve_WolfOnlyVoters_PostInformant_HitsWolf()
        {
            var curse = new CurseSession(-2, Deadline, new[] { -1 });
            var res = curse.TryResolve(Deadline, Roster(), informantEstablished: true, new Random(1));
            Assert.NotNull(res);
            Assert.Equal(-1, res.VictimActor);
        }

        [Fact]
        public void TryResolve_NoAliveVoters_Fizzles()
        {
            var roster = Roster();
            roster[3].Alive = false;
            var curse = new CurseSession(-2, Deadline, new[] { -3 });
            var res = curse.TryResolve(Deadline, roster, informantEstablished: false, new Random(1));
            Assert.NotNull(res);
            Assert.False(res.HasVictim);
        }

        [Fact]
        public void TryResolve_NullVoters_KeepsLegacyInformantRule()
        {
            var seen = new HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                var curse = new CurseSession(-2, Deadline);
                var res = curse.TryResolve(Deadline, Roster(), informantEstablished: true, new Random(seed));
                Assert.NotNull(res);
                seen.Add(res.VictimActor);
            }
            Assert.Contains(1, seen);
            Assert.Contains(-3, seen);
            Assert.DoesNotContain(-1, seen);
        }

        private static (GameSession session, RolesSession roles, List<OutboundMessage> sent)
            BuildRolesScenario(bool catIsBot)
        {
            var session = new GameSession();
            int catActor = catIsBot ? -2 : 2;
            session.ReserveForcedRole(1, Role.Werewolf);
            session.ReserveForcedRole(catActor, Role.BlackCat);

            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Name = "P1" },
                new WPlayer { ActorNumber = catActor, Name = "Cat", IsBot = catIsBot },
                new WPlayer { ActorNumber = 3, Name = "P3" },
                new WPlayer { ActorNumber = 4, Name = "P4" },
                new WPlayer { ActorNumber = 5, Name = "P5" },
            };
            var config = new GameConfig { RoundSeconds = 600, CurseWaitSec = 10 };
            Assert.True(session.Start(config, players, Now, new Random(1)).Success);

            var roles = new RolesSession(config, session, Now, new Random(1));
            var sent = new List<OutboundMessage>();
            roles.OnSend += sent.Add;
            return (session, roles, sent);
        }

        [Fact]
        public void TryStartCurse_WithVoters_Sends179ToCatOnlyBefore175()
        {
            var (_, roles, sent) = BuildRolesScenario(catIsBot: false);
            var voters = new[] { 3, 4 };

            Assert.True(roles.TryStartCurse(2, Now, out _, voters));

            int idx179 = sent.FindIndex(m => m.Code == WWRolesCodes.CurseCandidates);
            int idx175 = sent.FindIndex(m => m.Code == WWRolesCodes.RoleState);
            Assert.True(idx179 >= 0, "179 not sent");
            Assert.True(idx175 >= 0, "175 not sent");
            Assert.True(idx179 < idx175, "179 must precede 175 (UI装着時に候補が揃う順序)");

            var msg = sent[idx179];
            Assert.Equal(MessageTarget.Actors, msg.Target);
            Assert.Equal(new[] { 2 }, msg.TargetActors);
            Assert.Equal(voters, (int[])msg.Payload[0]);
        }

        [Fact]
        public void TryStartCurse_BotCat_DoesNotSend179_ButRestrictionStillApplies()
        {
            var (_, roles, sent) = BuildRolesScenario(catIsBot: true);

            var curseKills = new List<int>();
            roles.OnCurseKill += curseKills.Add;
            roles.OnCurseSealed += _ => { };

            Assert.True(roles.TryStartCurse(-2, Now, out _, new[] { 3 }));
            Assert.DoesNotContain(sent, m => m.Code == WWRolesCodes.CurseCandidates);

            roles.HandleRoleAction(-2, RoleActionSubtype.CurseDesignate, 4, 0, Now + 1000);
            roles.Tick(Now + 10_000);
            Assert.Equal(new[] { 3 }, curseKills);
        }

        [Fact]
        public void TryStartCurse_NullVoters_DoesNotSend179()
        {
            var (_, roles, sent) = BuildRolesScenario(catIsBot: false);
            Assert.True(roles.TryStartCurse(2, Now, out _));
            Assert.DoesNotContain(sent, m => m.Code == WWRolesCodes.CurseCandidates);
        }

        [Fact]
        public void ClientState_CurseCandidates_MirroredAndClearedOnResolve()
        {
            var client = new RolesClientState();
            Assert.Null(client.CurseCandidates);

            client.ApplyCurseCandidates(new[] { 3, 4 });
            client.ApplyCurseStarted(2, Deadline);
            Assert.Equal(new[] { 3, 4 }, client.CurseCandidates);

            client.ApplyCurseResolved();
            Assert.Null(client.CurseCandidates);
        }

        [Fact]
        public void ClientState_CurseCandidates_ClearedOnReset()
        {
            var client = new RolesClientState();
            client.ApplyCurseCandidates(new[] { 3 });
            client.Reset();
            Assert.Null(client.CurseCandidates);
        }
    }
}
