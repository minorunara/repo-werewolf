using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class HudModelTests
    {
        private static HudInput Input(
            GamePhase phase,
            Role? role,
            long roundEndUnixMs = 60_000,
            RolesClientState rolesClient = null,
            bool nearMeetingButton = false,
            int rightsRemaining = 0,
            long nowUnixMs = 0,
            bool debugSession = false)
            => new HudInput(phase, role, roundEndUnixMs, rolesClient, nearMeetingButton, rightsRemaining, nowUnixMs,
                debugSession: debugSession);

        [Theory]
        [InlineData(GamePhase.Lobby)]
        [InlineData(GamePhase.GameOver)]
        public void Compute_SessionInactivePhase_HidesAllElements(GamePhase phase)
        {
            var model = new HudModel();

            var state = model.Compute(Input(phase, Role.Werewolf,
                rolesClient: MakeGaugeReceivedState(), nearMeetingButton: true, rightsRemaining: 2,
                debugSession: true));

            Assert.False(state.ShowBadge);
            Assert.False(state.ShowTimer);
            Assert.False(state.ShowGauge);
            Assert.False(state.ShowRights);
            Assert.False(state.ShowTestPlay);
        }

        [Fact]
        public void Compute_PlayPhase_ShowsBadgeWithLocalRole()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Play, Role.Villager));

            Assert.True(state.ShowBadge);
            Assert.Equal(Role.Villager, state.BadgeRole);
        }

        [Fact]
        public void Compute_MeetingPhase_KeepsBadge()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Meeting, Role.Villager));

            Assert.True(state.ShowBadge);
            Assert.Equal(Role.Villager, state.BadgeRole);
        }

        [Fact]
        public void Compute_ConveneCountdown_KeepsBadge()
        {
            var model = new HudModel();
            model.Compute(Input(GamePhase.Play, Role.Werewolf, roundEndUnixMs: 60_000, nowUnixMs: 40_000));

            var countdown = new HudInput(GamePhase.Meeting, Role.Werewolf, 60_000, null,
                nearMeetingButton: false, rightsRemaining: 0, nowUnixMs: 41_000, meetingActive: true);

            var state = model.Compute(countdown);

            Assert.True(state.ShowBadge);
            Assert.Equal(Role.Werewolf, state.BadgeRole);
        }

        [Fact]
        public void Compute_PlayPhase_ShowsTimerAsEndMinusNow()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 60_000, nowUnixMs: 40_000));

            Assert.True(state.ShowTimer);
            Assert.Equal(20_000, state.TimerRemainingMs);
        }

        [Fact]
        public void Compute_PlayPhase_TimerClampsAtZeroPastEnd()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 60_000, nowUnixMs: 99_999));

            Assert.Equal(0, state.TimerRemainingMs);
        }

        [Fact]
        public void Compute_MeetingPhase_FreezesTimerAtTransitionMoment()
        {
            var model = new HudModel();
            model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 60_000, nowUnixMs: 40_000));

            var atEntry = model.Compute(Input(GamePhase.Meeting, Role.Villager, roundEndUnixMs: 60_000, nowUnixMs: 40_000));
            Assert.Equal(20_000, atEntry.TimerRemainingMs);

            var later = model.Compute(Input(GamePhase.Meeting, Role.Villager, roundEndUnixMs: 60_000, nowUnixMs: 55_000));
            Assert.Equal(20_000, later.TimerRemainingMs);

            var muchLater = model.Compute(Input(GamePhase.Meeting, Role.Villager, roundEndUnixMs: 60_000, nowUnixMs: 999_999));
            Assert.Equal(20_000, muchLater.TimerRemainingMs);
        }

        [Fact]
        public void Compute_ResumeToPlay_UnfreezesAndUsesLiveRemaining()
        {
            var model = new HudModel();
            model.Compute(Input(GamePhase.Meeting, Role.Villager, roundEndUnixMs: 60_000, nowUnixMs: 40_000));

            var resumed = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 80_000, nowUnixMs: 50_000));

            Assert.Equal(30_000, resumed.TimerRemainingMs);
        }

        [Fact]
        public void Compute_SecondMeetingCycle_RefreezesAtNewTransition()
        {
            var model = new HudModel();
            model.Compute(Input(GamePhase.Meeting, Role.Villager, roundEndUnixMs: 60_000, nowUnixMs: 40_000));
            model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 80_000, nowUnixMs: 50_000));

            var secondEntry = model.Compute(Input(GamePhase.Meeting, Role.Villager, roundEndUnixMs: 80_000, nowUnixMs: 70_000));
            Assert.Equal(10_000, secondEntry.TimerRemainingMs);

            var stillFrozen = model.Compute(Input(GamePhase.Meeting, Role.Villager, roundEndUnixMs: 80_000, nowUnixMs: 79_999));
            Assert.Equal(10_000, stillFrozen.TimerRemainingMs);
        }

        [Fact]
        public void Compute_RemainingAboveFiveMinutes_NoTimerAlert()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 400_000, nowUnixMs: 0));

            Assert.False(state.TimerAlert);
        }

        [Fact]
        public void Compute_RemainingAtOrBelowFiveMinutes_TimerAlertActive()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 300_000, nowUnixMs: 0));

            Assert.True(state.TimerAlert);
        }

        [Fact]
        public void Compute_BellRingsOnceOnMarkCrossing_WithBaseVolume()
        {
            var model = new HudModel();
            model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 400_000, nowUnixMs: 0));

            var before = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 400_000, nowUnixMs: 99_000));
            Assert.Equal(0f, before.BellVolumeScale);

            var atMark = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 400_000, nowUnixMs: 100_000));
            Assert.Equal(BellSchedule.BaseVolumeScale, atMark.BellVolumeScale);

            var after = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 400_000, nowUnixMs: 100_100));
            Assert.Equal(0f, after.BellVolumeScale);
        }

        [Fact]
        public void Compute_MeetingFreeze_SuppressesBellWhileFrozen()
        {
            var model = new HudModel();
            model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 400_000, nowUnixMs: 0));

            var entry = model.Compute(Input(GamePhase.Meeting, Role.Villager, roundEndUnixMs: 400_000, nowUnixMs: 50_000));
            Assert.True(entry.TimerFrozen);
            Assert.Equal(0f, entry.BellVolumeScale);

            var during = model.Compute(Input(GamePhase.Meeting, Role.Villager, roundEndUnixMs: 400_000, nowUnixMs: 200_000));
            Assert.Equal(0f, during.BellVolumeScale);

            model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 550_000, nowUnixMs: 200_000));
            var resumedMark = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 550_000, nowUnixMs: 250_000));
            Assert.Equal(BellSchedule.BaseVolumeScale, resumedMark.BellVolumeScale);
        }

        [Fact]
        public void Compute_SessionInactive_ResetsBellGateForNextRound()
        {
            var model = new HudModel();
            model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 310_000, nowUnixMs: 0));
            var firstRing = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 310_000, nowUnixMs: 10_000));
            Assert.Equal(BellSchedule.BaseVolumeScale, firstRing.BellVolumeScale);

            model.Compute(Input(GamePhase.GameOver, Role.Villager));

            var rearm = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 500_000, nowUnixMs: 100_000));
            Assert.Equal(0f, rearm.BellVolumeScale);
            var secondRing = model.Compute(Input(GamePhase.Play, Role.Villager, roundEndUnixMs: 500_000, nowUnixMs: 200_000));
            Assert.Equal(BellSchedule.BaseVolumeScale, secondRing.BellVolumeScale);
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void Compute_WerewolfWithGaugeReceived_ShowsGauge(GamePhase phase)
        {
            var model = new HudModel();
            var rolesClient = MakeGaugeReceivedState(ratioPermille: 450);

            var state = model.Compute(Input(phase, Role.Werewolf, rolesClient: rolesClient));

            Assert.True(state.ShowGauge);
            Assert.Equal(450, state.GaugeRatioPermille);
        }

        [Fact]
        public void Compute_BlackCatWithGaugeReceived_ShowsGauge()
        {
            var model = new HudModel();
            var rolesClient = MakeGaugeReceivedState(ratioPermille: 100);

            var state = model.Compute(Input(GamePhase.Play, Role.BlackCat, rolesClient: rolesClient));

            Assert.True(state.ShowGauge);
        }

        [Fact]
        public void Compute_Villager_NeverShowsGaugeEvenIfClientStatePresent()
        {
            var model = new HudModel();
            var rolesClient = MakeGaugeReceivedState(ratioPermille: 900);

            var state = model.Compute(Input(GamePhase.Play, Role.Villager, rolesClient: rolesClient));

            Assert.False(state.ShowGauge);
        }

        [Fact]
        public void Compute_Werewolf_GaugeNotYetReceived_HidesGauge()
        {
            var model = new HudModel();
            var rolesClient = new RolesClientState();

            var state = model.Compute(Input(GamePhase.Play, Role.Werewolf, rolesClient: rolesClient));

            Assert.False(state.ShowGauge);
        }

        [Fact]
        public void Compute_NullRolesClient_HidesGauge()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Play, Role.Werewolf, rolesClient: null));

            Assert.False(state.ShowGauge);
        }

        [Fact]
        public void Compute_NearMeetingButton_ShowsRightsRemaining()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Play, Role.Villager, nearMeetingButton: true, rightsRemaining: 3));

            Assert.True(state.ShowRights);
            Assert.Equal(3, state.RightsRemaining);
        }

        [Fact]
        public void Compute_NotNearMeetingButton_HidesRights()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Play, Role.Villager, nearMeetingButton: false, rightsRemaining: 3));

            Assert.False(state.ShowRights);
        }

        [Theory]
        [InlineData(GamePhase.Play)]
        [InlineData(GamePhase.Meeting)]
        public void Compute_DebugSession_ShowsTestPlayBannerWhileSessionActive(GamePhase phase)
        {
            var model = new HudModel();

            var state = model.Compute(Input(phase, Role.Villager, debugSession: true));

            Assert.True(state.ShowTestPlay);
        }

        [Fact]
        public void Compute_NonDebugSession_HidesTestPlayBanner()
        {
            var model = new HudModel();

            var state = model.Compute(Input(GamePhase.Play, Role.Villager, debugSession: false));

            Assert.False(state.ShowTestPlay);
        }

        private static RolesClientState MakeGaugeReceivedState(int ratioPermille = 0)
        {
            var state = new RolesClientState();
            state.ApplyGaugeSync(ratioPermille, unlockedFlags: 0, beaconCharges: 0,
                beaconStatus: 0, beaconReadyUnixMs: 0);
            return state;
        }
    }
}
