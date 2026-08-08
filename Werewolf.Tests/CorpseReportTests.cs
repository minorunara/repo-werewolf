using System;
using System.Linq;
using Werewolf.Core;
using Werewolf.Net;
using Xunit;

namespace Werewolf.Tests
{
    public class CorpseReportTests : IDisposable
    {
        private const byte StartMeeting = 163;
        private const byte ConveneDenied = 176;
        private const byte MeetingCancelled = 178;
        private const long Now = MeetingSessionHarness.GameStart;

        public CorpseReportTests() { WLog.Sink = (_, __) => { }; }
        public void Dispose() { WLog.Sink = null; }

        [Fact]
        public void CorpseReport_BypassesStartSuppression()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.ConveneSuppressStartSec = 600);

            Assert.Equal(ConveneRejectReason.Suppressed, h.Session.TryConvene(3, Now));
            var reason = h.Session.TryConvene(3, Now, ConveneKind.CorpseReport,
                lastRunActive: false, corpseAvailable: true);

            Assert.Equal(ConveneRejectReason.None, reason);
            Assert.Equal(MeetingStage.Countdown, h.Session.Stage);
            Assert.Equal(ConveneKind.CorpseReport, h.Session.CurrentKind);
        }

        [Fact]
        public void CorpseReport_AcceptedWithoutMeetingRight_AndDoesNotConsume()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.MeetingRightsPerPlayer = 0);

            var reason = h.Session.TryConvene(3, Now, ConveneKind.CorpseReport,
                lastRunActive: false, corpseAvailable: true);

            Assert.Equal(ConveneRejectReason.None, reason);
            Assert.Equal(MeetingStage.Countdown, h.Session.Stage);
            Assert.Equal(0, h.Session.RightsRemaining(3));
            Assert.Empty(h.RightsChanges);
        }

        [Fact]
        public void CorpseReport_RejectedDuringLastRun_AndWireCarriesReason()
        {
            var h = MeetingSessionHarness.Create();

            var reason = h.Session.TryConvene(3, Now, ConveneKind.CorpseReport,
                lastRunActive: true, corpseAvailable: true);

            Assert.Equal(ConveneRejectReason.CorpseReportLastRun, reason);
            var denied = Assert.Single(h.ByCode(ConveneDenied).ToList());
            Assert.Equal((byte)4, denied.Payload[0]);
            Assert.Equal(1, h.Session.RightsRemaining(3));
        }

        [Fact]
        public void CorpseReport_RejectedWithoutCorpse()
        {
            var h = MeetingSessionHarness.Create();

            var reason = h.Session.TryConvene(3, Now, ConveneKind.CorpseReport,
                lastRunActive: false, corpseAvailable: false);

            Assert.Equal(ConveneRejectReason.NoCorpse, reason);
            var denied = Assert.Single(h.ByCode(ConveneDenied).ToList());
            Assert.Equal((byte)5, denied.Payload[0]);
        }

        [Fact]
        public void ButtonConvene_AcceptedDuringLastRun()
        {
            var h = MeetingSessionHarness.Create();

            var reason = h.Session.TryConvene(3, Now, ConveneKind.Button,
                lastRunActive: true, corpseAvailable: false);

            Assert.Equal(ConveneRejectReason.None, reason);
        }

        [Fact]
        public void StartMeetingPayload_CarriesConveneKind()
        {
            var h = MeetingSessionHarness.Create();
            h.Session.TryConvene(3, Now, ConveneKind.CorpseReport,
                lastRunActive: false, corpseAvailable: true);

            var start = Assert.Single(h.ByCode(StartMeeting).ToList());
            Assert.Equal(4, start.Payload.Length);
            Assert.Equal((byte)ConveneKind.CorpseReport, start.Payload[3]);
        }

        [Fact]
        public void StartMeetingPayload_ButtonKindIsZero()
        {
            var h = MeetingSessionHarness.Create();
            h.Session.TryConvene(3, Now);

            var start = Assert.Single(h.ByCode(StartMeeting).ToList());
            Assert.Equal((byte)ConveneKind.Button, start.Payload[3]);
        }

        [Fact]
        public void CancelCorpseReport_DuringCountdown_ReturnsToPlay()
        {
            var h = MeetingSessionHarness.Create();
            h.Session.TryConvene(3, Now, ConveneKind.CorpseReport,
                lastRunActive: false, corpseAvailable: true);
            Assert.Equal(1, h.Session.RightsRemaining(3));

            bool cancelled = h.Session.TryCancelCorpseReportCountdown(Now + 2_000);

            Assert.True(cancelled);
            Assert.Equal(MeetingStage.Idle, h.Session.Stage);
            Assert.Equal(-1, h.Session.CallerActor);
            Assert.Equal(ConveneKind.Button, h.Session.CurrentKind);

            Assert.Equal(1, h.Session.RightsRemaining(3));
            Assert.Empty(h.RightsChanges);

            var msg = Assert.Single(h.ByCode(MeetingCancelled).ToList());
            Assert.Equal(MessageTarget.All, msg.Target);
            Assert.Equal((byte)0, msg.Payload[0]);

            Assert.Equal((-1, 0L), h.MeetingStates.Last());
            Assert.Equal(GamePhase.Play, h.PhaseRequests.Last());

            Assert.Equal(long.MinValue, h.Session.LastMeetingEndUnixMs);
        }

        [Fact]
        public void CancelCorpseReport_ThenReconveneImmediately_Succeeds()
        {
            var h = MeetingSessionHarness.Create(tune: c => c.ConveneSuppressAfterSec = 600);
            h.Session.TryConvene(3, Now, ConveneKind.CorpseReport,
                lastRunActive: false, corpseAvailable: true);
            h.Session.TryCancelCorpseReportCountdown(Now + 2_000);

            var reason = h.Session.TryConvene(3, Now + 3_000, ConveneKind.CorpseReport,
                lastRunActive: false, corpseAvailable: true);
            Assert.Equal(ConveneRejectReason.None, reason);
        }

        [Fact]
        public void CancelCorpseReport_ButtonCountdown_IsNotCancelled()
        {
            var h = MeetingSessionHarness.Create();
            h.Session.TryConvene(3, Now);

            Assert.False(h.Session.TryCancelCorpseReportCountdown(Now + 2_000));
            Assert.Equal(MeetingStage.Countdown, h.Session.Stage);
            Assert.Empty(h.ByCode(MeetingCancelled));
        }

        [Fact]
        public void CancelCorpseReport_AfterVotingStarted_IsNotCancelled()
        {
            var h = MeetingSessionHarness.Create();
            h.Session.TryConvene(3, Now, ConveneKind.CorpseReport,
                lastRunActive: false, corpseAvailable: true);
            h.Session.Tick(Now + 6_000);
            Assert.Equal(MeetingStage.Voting, h.Session.Stage);

            Assert.False(h.Session.TryCancelCorpseReportCountdown(Now + 6_500));
            Assert.Equal(MeetingStage.Voting, h.Session.Stage);
        }

        [Fact]
        public void ClientState_StoresKindFromStartMeeting()
        {
            var state = new MeetingClientState();
            Assert.Equal(ConveneKind.Button, state.Kind);

            state.ApplyStartMeeting(3, Now + 5_000, Now + 60_000, ConveneKind.CorpseReport);
            Assert.Equal(ConveneKind.CorpseReport, state.Kind);

            state.Reset();
            Assert.Equal(ConveneKind.Button, state.Kind);
        }

        [Fact]
        public void ClientState_ApplyCancelled_DeactivatesWithoutAnnouncingDead()
        {
            var state = new MeetingClientState();
            state.ApplyPlayerDied(5, DeathCause.Other);
            state.ApplyStartMeeting(3, Now + 5_000, Now + 60_000, ConveneKind.CorpseReport);
            Assert.True(state.MeetingActive);

            state.ApplyCancelled();
            Assert.False(state.MeetingActive);
            Assert.Equal(-1, state.CallerActor);

            state.ApplyPhase(GamePhase.Play);
            Assert.True(state.IsDeadUnannounced(5));
        }

        [Fact]
        public void ClientState_NormalMeetingEnd_AnnouncesDead_Contrast()
        {
            var state = new MeetingClientState();
            state.ApplyPlayerDied(5, DeathCause.Other);
            state.ApplyStartMeeting(3, Now + 5_000, Now + 60_000);

            state.ApplyPhase(GamePhase.Play);
            Assert.False(state.IsDeadUnannounced(5));
        }

        [Fact]
        public void ConveneDeniedWire_RoundtripsNewReasons()
        {
            Assert.Equal((byte)4, ConveneDeniedWire.ToWire(ConveneRejectReason.CorpseReportLastRun));
            Assert.Equal((byte)5, ConveneDeniedWire.ToWire(ConveneRejectReason.NoCorpse));
            Assert.Equal(ConveneRejectReason.CorpseReportLastRun, ConveneDeniedWire.FromWire(4));
            Assert.Equal(ConveneRejectReason.NoCorpse, ConveneDeniedWire.FromWire(5));
        }

        [Fact]
        public void NoticeCatalog_FormatsCorpseReportNotices()
        {
            string started = NoticeCatalog.Format(SessionNotice.ForCorpseReportStarted("P3"));
            Assert.Contains("P3", started);
            Assert.Contains("死体の頭を発見", started);

            string cancelled = NoticeCatalog.Format(SessionNotice.ForMeetingCancelled());
            Assert.Contains("中止", cancelled);

            string lastRun = NoticeCatalog.Format(
                SessionNotice.ForConveneDenied(ConveneRejectReason.CorpseReportLastRun));
            string noCorpse = NoticeCatalog.Format(
                SessionNotice.ForConveneDenied(ConveneRejectReason.NoCorpse));
            Assert.NotNull(lastRun);
            Assert.NotNull(noCorpse);
            Assert.NotEqual(lastRun, noCorpse);
        }

        [Fact]
        public void NoticeSfx_CorpseReportUsesDedicatedClip()
        {
            Assert.Equal("sfx_corpse_report",
                NoticeSfx.Resolve(SessionNotice.ForCorpseReportStarted("P3")));
            Assert.Equal(NoticeSfx.DefaultClipKey,
                NoticeSfx.Resolve(SessionNotice.ForMeetingCancelled()));
        }

        [Fact]
        public void EventCodes_MeetingCancelledAndUpdatedSchemas()
        {
            Assert.Equal(178, MessageCodes.MeetingCancelled);
            Assert.True(MessageCodes.IsInRange(MessageCodes.MeetingCancelled));
            Assert.False(MessageCodes.IsSecret(MessageCodes.MeetingCancelled));
            Assert.False(MessageCodes.IsMasterInbound(MessageCodes.MeetingCancelled));

            Assert.Equal(new[] { typeof(int), typeof(long), typeof(long), typeof(byte) },
                MessageCodes.Schema(MessageCodes.StartMeeting));
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.RequestMeeting));
            Assert.Equal(new[] { typeof(byte) }, MessageCodes.Schema(MessageCodes.MeetingCancelled));
        }
    }
}
