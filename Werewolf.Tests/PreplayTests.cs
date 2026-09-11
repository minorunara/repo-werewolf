using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class PreplayTests
    {
        private const long Now = 1_000_000L;

        private static List<WPlayer> Players(int count)
        {
            var result = new List<WPlayer>();
            for (int i = 1; i <= count; i++)
                result.Add(new WPlayer { ActorNumber = i, Name = "P" + i });
            return result;
        }

        private static GameSession Prepared(int count, out List<OutboundMessage> sent,
            Action<GameSession> reserve = null, GameConfig config = null)
        {
            var session = new GameSession();
            reserve?.Invoke(session);
            sent = new List<OutboundMessage>();
            session.OnSend += sent.Add;
            Assert.True(session.StartPreplay(config ?? new GameConfig(), Players(count), Now, new Random(1)).Success);
            return session;
        }

        [Fact]
        public void StartPreplay_AssignsRolesButDoesNotStartMatchClock()
        {
            GameSession session = Prepared(5, out var sent);

            Assert.Equal(GamePhase.RoleReveal, session.Phase);
            Assert.Equal(0, session.StartUnixMs);
            Assert.Equal(Now, session.RoleRevealStartUnixMs);
            Assert.Equal(Now + Preplay.RoleRevealDurationMs, session.RoleRevealEndUnixMs);
            Assert.DoesNotContain(sent, m => m.Code == WWEventCodes.GameStart);

            OutboundMessage start = Assert.Single(sent, m => m.Code == WWEventCodes.PreplayStart);
            Assert.Equal(Now, (long)start.Payload[0]);
            Assert.Equal(Now + Preplay.RoleRevealDurationMs, (long)start.Payload[1]);
            Assert.Equal(8, start.Payload.Length);
            Assert.Contains(sent, m => m.Code == WWEventCodes.AssignRole);
        }

        [Fact]
        public void RevealBoundary_StartsPlayAndClockAtHostTransitionFrame()
        {
            GameSession session = Prepared(5, out var sent);

            session.Tick(Now + Preplay.RoleRevealDurationMs - 1);
            Assert.Equal(GamePhase.RoleReveal, session.Phase);
            Assert.DoesNotContain(sent, m => m.Code == WWEventCodes.GameStart);

            Assert.Equal(BeginPlayResult.Started,
                session.BeginPlay(Now + Preplay.RoleRevealDurationMs + 17));
            Assert.Equal(GamePhase.Play, session.Phase);
            Assert.Equal(Now + Preplay.RoleRevealDurationMs + 17, session.StartUnixMs);
            OutboundMessage gameStart = Assert.Single(sent, m => m.Code == WWEventCodes.GameStart);
            Assert.Equal(session.StartUnixMs + new GameConfig().RoundSeconds * 1000L,
                (long)gameStart.Payload[0]);
        }

        [Fact]
        public void DisconnectDuringReveal_ImmediateWinnerBecomesPreplayAbortWithoutGameOver()
        {
            GameSession session = Prepared(3, out var sent, reserve: s =>
            {
                s.ReserveForcedRole(1, Role.Werewolf);
                s.ReserveForcedRole(2, Role.Villager);
                s.ReserveForcedRole(3, Role.Villager);
            });

            session.NotifyPlayerLeft(2, Now + 2_000);
            session.NotifyPlayerLeft(3, Now + 3_000);
            Assert.Equal(BeginPlayResult.Aborted,
                session.BeginPlay(Now + Preplay.RoleRevealDurationMs));

            Assert.Equal(GamePhase.PreplayAborted, session.Phase);
            Assert.Null(session.Winner);
            Assert.Equal(0, session.StartUnixMs);
            Assert.Empty(session.Players);
            Assert.DoesNotContain(sent, m => m.Code == WWEventCodes.GameStart);
            Assert.DoesNotContain(sent, m => m.Code == WWEventCodes.GameOver);
            OutboundMessage aborted = Assert.Single(sent, m => m.Code == WWEventCodes.PreplayAborted);
            Assert.Equal((byte)PreplayAbortReason.RoleRevealRosterInvalid, (byte)aborted.Payload[0]);
        }

        [Fact]
        public void DelayedHostTick_UsesActualPlayTransitionForEveryMatchClock()
        {
            GameSession session = Prepared(5, out var sent);
            long actualPlay = Now + Preplay.RoleRevealDurationMs + 2_500;

            Assert.Equal(BeginPlayResult.Started, session.BeginPlay(actualPlay));

            Assert.Equal(actualPlay, session.StartUnixMs);
            OutboundMessage gameStart = Assert.Single(sent, m => m.Code == WWEventCodes.GameStart);
            Assert.Equal(actualPlay + new GameConfig().RoundSeconds * 1000L,
                Assert.IsType<long>(gameStart.Payload[0]));
        }

        [Fact]
        public void StartPreplay_MessageOrder_IsSecretsThenPublicStartThenRoleRevealPhase()
        {
            Prepared(5, out var sent, config: new GameConfig { WerewolfCount = 2 });

            int preplay = sent.FindIndex(m => m.Code == WWEventCodes.PreplayStart);
            int phase = sent.FindIndex(m => m.Code == WWEventCodes.PhaseChanged);
            Assert.True(preplay > 0);
            Assert.Equal(sent.Count - 2, preplay);
            Assert.Equal(sent.Count - 1, phase);
            Assert.All(sent.Take(preplay), m => Assert.True(
                m.Code == WWEventCodes.AssignRole || m.Code == WWEventCodes.RevealTeammates));
            Assert.Equal((byte)GamePhase.RoleReveal, Assert.IsType<byte>(sent[phase].Payload[0]));
        }

        [Fact]
        public void RosterGate_RequiresEveryCurrentPhotonActorAndIgnoresDepartedAvatar()
        {
            var room = new[] { 1, 3 };
            var avatars = new[]
            {
                new ParticipantLoadState(1, completed: true),
                new ParticipantLoadState(2, completed: true),
                new ParticipantLoadState(3, completed: true),
            };

            Assert.True(PreplayRosterGate.AllCurrentParticipantsLoaded(room, avatars));
            Assert.False(PreplayRosterGate.AllCurrentParticipantsLoaded(room, new[]
            {
                new ParticipantLoadState(1, completed: true),
                new ParticipantLoadState(2, completed: true),
            }));
            Assert.False(PreplayRosterGate.AllCurrentParticipantsLoaded(room, new[]
            {
                new ParticipantLoadState(1, completed: true),
                new ParticipantLoadState(3, completed: false),
            }));
        }

        [Fact]
        public void RosterGate_RejectsEmptyInvalidOrDuplicateCurrentRoster()
        {
            Assert.False(PreplayRosterGate.AllCurrentParticipantsLoaded(
                Array.Empty<int>(), Array.Empty<ParticipantLoadState>()));
            Assert.False(PreplayRosterGate.AllCurrentParticipantsLoaded(
                new[] { 1, 1 }, new[] { new ParticipantLoadState(1, true) }));
            Assert.False(PreplayRosterGate.AllCurrentParticipantsLoaded(
                new[] { 1, -1 }, new[] { new ParticipantLoadState(1, true) }));
        }

        [Fact]
        public void DisconnectDuringReveal_WhenBothTeamsRemain_StillStartsPlay()
        {
            GameSession session = Prepared(4, out _, reserve: s =>
            {
                s.ReserveForcedRole(1, Role.Werewolf);
                s.ReserveForcedRole(2, Role.Villager);
                s.ReserveForcedRole(3, Role.Villager);
                s.ReserveForcedRole(4, Role.Villager);
            });
            session.NotifyPlayerLeft(4, Now + 2_000);

            Assert.Equal(BeginPlayResult.Started,
                session.BeginPlay(Now + Preplay.RoleRevealDurationMs));
            Assert.Equal(GamePhase.Play, session.Phase);
        }

        [Fact]
        public void BlackCatDelayClockDoesNotAdvanceDuringReveal()
        {
            var config = new GameConfig { BlackCatRevealDelaySec = 5, WerewolfCount = 1 };
            GameSession session = Prepared(4, out var sent, reserve: s =>
            {
                s.ReserveForcedRole(1, Role.Werewolf);
                s.ReserveForcedRole(2, Role.BlackCat);
                s.ReserveForcedRole(3, Role.Villager);
                s.ReserveForcedRole(4, Role.Villager);
            }, config: config);

            session.Tick(Now + Preplay.RoleRevealDurationMs - 1);
            Assert.DoesNotContain(sent, m => m.Code == WWEventCodes.RevealSelfRole);
            session.Tick(Now + Preplay.RoleRevealDurationMs);
            session.Tick(Now + Preplay.RoleRevealDurationMs + 4_999);
            Assert.DoesNotContain(sent, m => m.Code == WWEventCodes.RevealSelfRole);
            session.Tick(Now + Preplay.RoleRevealDurationMs + 5_000);
            Assert.Single(sent, m => m.Code == WWEventCodes.RevealSelfRole);
        }

        [Fact]
        public void Timeline_UsesBackFlipTwoPagesAndFinalStowWithoutProgressState()
        {
            RoleRevealFrame entry = RoleRevealTimeline.At(0);
            RoleRevealFrame settledBack = RoleRevealTimeline.At(RoleRevealTimeline.DropEndMs);
            RoleRevealFrame beforeFlip = RoleRevealTimeline.At(RoleRevealTimeline.FlipStartMs - 1);
            RoleRevealFrame front = RoleRevealTimeline.At(RoleRevealTimeline.FlipEndMs);
            RoleRevealFrame second = RoleRevealTimeline.At(Preplay.PageBoundaryMs + 1);
            RoleRevealFrame stowed = RoleRevealTimeline.At(Preplay.RoleRevealDurationMs);

            Assert.True(entry.ShowBack);
            Assert.True(settledBack.ShowBack);
            Assert.Equal(1f, settledBack.CardDropRatio);
            Assert.Equal(1f, settledBack.CardFlipScaleX);
            Assert.True(beforeFlip.ShowBack);
            Assert.Equal(1f, beforeFlip.CardFlipScaleX);
            Assert.False(front.ShowBack);
            Assert.Equal(0, front.PageIndex);
            Assert.Equal(1, second.PageIndex);
            Assert.Equal(1f, stowed.StowRatio);
            Assert.Equal(0f, stowed.TextAlpha);
            Assert.Equal(0f, stowed.BackdropAlpha);
        }

        [Fact]
        public void Protocol_DeclaresPublicPreplayMessages()
        {
            Assert.Equal(0, (byte)GamePhase.Lobby);
            Assert.Equal(1, (byte)GamePhase.Play);
            Assert.Equal(2, (byte)GamePhase.Meeting);
            Assert.Equal(3, (byte)GamePhase.GameOver);
            Assert.Equal(4, (byte)GamePhase.RoleReveal);
            Assert.Equal(5, (byte)GamePhase.PreplayAborted);
            Assert.Equal(195, MessageCodes.PreplayStart);
            Assert.Equal(196, MessageCodes.PreplayAborted);
            Assert.Equal(196, MessageCodes.MaxCode);
            Assert.Equal(new[] { typeof(long), typeof(long), typeof(int), typeof(byte), typeof(byte),
                typeof(int), typeof(byte), typeof(int[]) }, MessageCodes.Schema(MessageCodes.PreplayStart));
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.PreplayAborted));
            Assert.False(MessageCodes.IsSecret(MessageCodes.PreplayStart));
            Assert.False(MessageCodes.IsSecret(MessageCodes.PreplayAborted));
        }

        [Fact]
        public void InitialReveal_DoesNotSuppressMenuOrImplementSkipInput()
        {
            string source = File.ReadAllText(Path.Combine(
                AuditEnvironment.Meta("Werewolf.SourceDir"), "UI", "RoleRevealCinematic.cs"));
            Assert.DoesNotContain("DisableEscMenu", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SkipInput", source, StringComparison.Ordinal);
            Assert.Contains("Session.IsEscMenuOpen", source, StringComparison.Ordinal);
        }

    }
}
