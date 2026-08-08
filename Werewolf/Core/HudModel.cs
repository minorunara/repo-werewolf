namespace Werewolf.Core
{
    public sealed class HudModel
    {
        private bool _meetingFrozen;
        private long _frozenRemainingMs;
        private readonly BellSchedule _bell = new BellSchedule();

        public HudState Compute(HudInput input)
        {
            bool sessionActive = input.Phase == GamePhase.Play || input.Phase == GamePhase.Meeting;

            bool meetingWindow = input.Phase == GamePhase.Meeting || input.MeetingActive;

            if (meetingWindow)
            {
                if (!_meetingFrozen)
                {
                    _frozenRemainingMs = RemainingMs(input.RoundEndUnixMs, input.NowUnixMs);
                    _meetingFrozen = true;
                }
            }
            else
            {
                _meetingFrozen = false;
            }

            if (!sessionActive)
            {
                _bell.Reset();
                return HudState.Hidden;
            }

            bool showGauge = input.RolesClient != null && input.RolesClient.CanShowHud(input.LocalRole);

            long timerRemainingMs = meetingWindow
                ? _frozenRemainingMs
                : RemainingMs(input.RoundEndUnixMs, input.NowUnixMs);

            int rungMarkSec = meetingWindow ? 0 : _bell.Tick(timerRemainingMs);

            return new HudState(
                showBadge: true,
                badgeRole: input.LocalRole,
                showTimer: true, timerRemainingMs: timerRemainingMs,
                timerFrozen: meetingWindow,
                timerAlert: BellSchedule.AlertActive(timerRemainingMs),
                bellMarkSec: rungMarkSec,
                bellVolumeScale: rungMarkSec > 0 ? BellSchedule.VolumeScaleFor(rungMarkSec) : 0f,
                showGauge: showGauge,
                gaugeRatioPermille: showGauge ? input.RolesClient.RatioPermille : 0,
                showRights: input.NearMeetingButton,
                rightsRemaining: input.RightsRemaining,
                showTestPlay: input.DebugSession,
                showScatterGuard: input.ScatterGuardActive,
                showSelfId: input.SelfParticipantId > 0,
                selfId: input.SelfParticipantId);
        }

        private static long RemainingMs(long endUnixMs, long nowUnixMs)
        {
            long remaining = endUnixMs - nowUnixMs;
            return remaining > 0 ? remaining : 0;
        }
    }

    public readonly struct HudInput
    {
        public GamePhase Phase { get; }

        public Role? LocalRole { get; }

        public long RoundEndUnixMs { get; }

        public RolesClientState RolesClient { get; }

        public bool NearMeetingButton { get; }

        public int RightsRemaining { get; }

        public long NowUnixMs { get; }

        public bool MeetingActive { get; }

        public bool DebugSession { get; }

        public int SelfParticipantId { get; }

        public bool ScatterGuardActive { get; }

        public HudInput(
            GamePhase phase,
            Role? localRole,
            long roundEndUnixMs,
            RolesClientState rolesClient,
            bool nearMeetingButton,
            int rightsRemaining,
            long nowUnixMs,
            bool meetingActive = false,
            bool debugSession = false,
            int selfParticipantId = 0,
            bool scatterGuardActive = false)
        {
            Phase = phase;
            LocalRole = localRole;
            RoundEndUnixMs = roundEndUnixMs;
            RolesClient = rolesClient;
            NearMeetingButton = nearMeetingButton;
            RightsRemaining = rightsRemaining;
            NowUnixMs = nowUnixMs;
            MeetingActive = meetingActive;
            DebugSession = debugSession;
            SelfParticipantId = selfParticipantId;
            ScatterGuardActive = scatterGuardActive;
        }
    }

    public readonly struct HudState
    {
        public bool ShowBadge { get; }

        public Role? BadgeRole { get; }

        public bool ShowTimer { get; }

        public long TimerRemainingMs { get; }

        public bool TimerFrozen { get; }

        public bool TimerAlert { get; }

        public float BellVolumeScale { get; }

        public int BellMarkSec { get; }

        public bool ShowGauge { get; }

        public int GaugeRatioPermille { get; }

        public bool ShowRights { get; }

        public int RightsRemaining { get; }

        public bool ShowTestPlay { get; }

        public bool ShowSelfId { get; }

        public int SelfId { get; }

        public bool ShowScatterGuard { get; }

        public HudState(
            bool showBadge, Role? badgeRole,
            bool showTimer, long timerRemainingMs, bool timerFrozen,
            bool timerAlert, int bellMarkSec, float bellVolumeScale,
            bool showGauge, int gaugeRatioPermille,
            bool showRights, int rightsRemaining,
            bool showTestPlay = false,
            bool showSelfId = false, int selfId = 0,
            bool showScatterGuard = false)
        {
            ShowBadge = showBadge;
            BadgeRole = badgeRole;
            ShowTimer = showTimer;
            TimerRemainingMs = timerRemainingMs;
            TimerFrozen = timerFrozen;
            TimerAlert = timerAlert;
            BellMarkSec = bellMarkSec;
            BellVolumeScale = bellVolumeScale;
            ShowGauge = showGauge;
            GaugeRatioPermille = gaugeRatioPermille;
            ShowRights = showRights;
            RightsRemaining = rightsRemaining;
            ShowTestPlay = showTestPlay;
            ShowSelfId = showSelfId;
            SelfId = selfId;
            ShowScatterGuard = showScatterGuard;
        }

        public static readonly HudState Hidden = new HudState(
            showBadge: false, badgeRole: null,
            showTimer: false, timerRemainingMs: 0, timerFrozen: false,
            timerAlert: false, bellMarkSec: 0, bellVolumeScale: 0f,
            showGauge: false, gaugeRatioPermille: 0,
            showRights: false, rightsRemaining: 0);
    }
}
