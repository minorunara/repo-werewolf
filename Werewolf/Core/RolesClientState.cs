using System;

namespace Werewolf.Core
{
    public sealed class RolesClientState
    {

        public int RatioPermille { get; private set; }

        public PerkFlags UnlockedFlags { get; private set; }

        public int BeaconCharges { get; private set; }

        public BeaconStatus LastBeaconStatus { get; private set; }

        public long BeaconReadyUnixMs { get; private set; }

        public bool GaugeReceived { get; private set; }

        public long GaugeNextUpdateUnixMs { get; private set; }

        public int CurseCatActor { get; private set; } = CurseResolution.NoVictim;

        public long CurseDeadlineUnixMs { get; private set; }

        public int[] CurseCandidates { get; private set; }

        public MeetingGaugeSnapshot MeetingGauge { get; private set; }

        public MeetingGaugeSnapshot PlayGauge { get; private set; }

        public bool WolfMode { get; private set; }

        public bool StaminaActive => WolfMode && PerkFlagsUtil.Has(UnlockedFlags, PerkId.InfiniteStamina);

        public bool JumpActive => WolfMode && PerkFlagsUtil.Has(UnlockedFlags, PerkId.InfiniteJump);

        public bool EnemyIgnoreActive => WolfMode && PerkFlagsUtil.Has(UnlockedFlags, PerkId.EnemyIgnore);

        public bool HealActive => WolfMode && PerkFlagsUtil.Has(UnlockedFlags, PerkId.NaturalHeal);

        public bool TryToggleWolfMode(Role? localRole)
        {
            if (localRole != Role.Werewolf) return false;
            if (UnlockedFlags == PerkFlags.None) return false;

            WolfMode = !WolfMode;
            WLog.Line("wolfmode_toggle", secret: true, ("on", WolfMode));
            return true;
        }

        public bool ForceWolfModeOff()
        {
            if (!WolfMode) return false;
            WolfMode = false;
            WLog.Line("wolfmode_toggle", secret: true, ("on", false), ("via", "reset"));
            return true;
        }

        public bool ValuableRecordOn { get; private set; }

        public bool ToggleValuableRecord(Role? localRole)
        {
            if (!ValuableRecordGate.IsWerewolfTeam(localRole)) return false;

            ValuableRecordOn = !ValuableRecordOn;
            WLog.Line("valuable_record_toggle", secret: true, ("on", ValuableRecordOn));
            return true;
        }

        public bool ForceValuableRecordOff()
        {
            if (!ValuableRecordOn) return false;
            ValuableRecordOn = false;
            WLog.Line("valuable_record_toggle", secret: true, ("on", false), ("via", "reset"));
            return true;
        }

        public bool CanShowHud(Role? localRole)
            => GaugeReceived && (localRole == Role.Werewolf || localRole == Role.BlackCat || localRole == Role.Bomber);

        public void ApplyGaugeSync(int ratioPermille, byte unlockedFlags, byte beaconCharges,
                                   byte beaconStatus, long beaconReadyUnixMs, int[] gaugeMeta = null,
                                   long nowUnixMs = 0)
        {
            RatioPermille = ratioPermille;
            UnlockedFlags = (PerkFlags)unlockedFlags;
            BeaconCharges = beaconCharges;
            LastBeaconStatus = (BeaconStatus)beaconStatus;
            BeaconReadyUnixMs = beaconReadyUnixMs;
            GaugeReceived = true;

            GaugeNextUpdateUnixMs =
                gaugeMeta != null && gaugeMeta.Length >= 7 && gaugeMeta[6] > 0 && nowUnixMs > 0
                    ? nowUnixMs + gaugeMeta[6] * 1000L
                    : 0;

            if (gaugeMeta != null && gaugeMeta.Length >= 6)
            {
                PlayGauge = new MeetingGaugeSnapshot
                {
                    RatioPermille = ratioPermille,
                    BaseDollars = gaugeMeta[0],
                    StaminaPct = gaugeMeta[1],
                    JumpPct = gaugeMeta[2],
                    EnemyIgnorePct = gaugeMeta[3],
                    InformantPct = gaugeMeta[4],
                    BeaconChargePct = gaugeMeta[5],
                    LostDollars = gaugeMeta.Length >= 8 ? gaugeMeta[7] : -1,
                    CheckmateLossDollars = gaugeMeta.Length >= 9 ? gaugeMeta[8] : -1,
                    HealPct = gaugeMeta.Length >= 10 ? gaugeMeta[9] : 0,
                };
            }
        }

        public void ApplyCurseStarted(int catActor, long deadlineUnixMs)
        {
            CurseCatActor = catActor;
            CurseDeadlineUnixMs = deadlineUnixMs;
        }

        public void ApplyCurseCandidates(int[] voterActors)
        {
            CurseCandidates = voterActors;
        }

        public void ApplyCurseResolved()
        {
            CurseCatActor = CurseResolution.NoVictim;
            CurseDeadlineUnixMs = 0;
            CurseCandidates = null;
        }

        public bool CurseActive(long nowUnixMs)
            => CurseCatActor != CurseResolution.NoVictim && nowUnixMs < CurseDeadlineUnixMs;

        public void ApplyMeetingGauge(int[] data)
        {
            var snapshot = MeetingGaugeSnapshot.FromData(data);
            if (snapshot != null) MeetingGauge = snapshot;
        }

        public void ClearMeetingGauge()
        {
            MeetingGauge = null;
        }

        public void Reset()
        {
            RatioPermille = 0;
            UnlockedFlags = PerkFlags.None;
            BeaconCharges = 0;
            LastBeaconStatus = BeaconStatus.Ok;
            BeaconReadyUnixMs = 0;
            GaugeReceived = false;
            GaugeNextUpdateUnixMs = 0;
            PlayGauge = null;
            CurseCatActor = CurseResolution.NoVictim;
            CurseDeadlineUnixMs = 0;
            CurseCandidates = null;
            MeetingGauge = null;
            WolfMode = false;
            ValuableRecordOn = false;
        }
    }

    public sealed class MeetingGaugeSnapshot
    {
        public int RatioPermille;
        public int BaseDollars;
        public int StaminaPct;
        public int JumpPct;
        public int EnemyIgnorePct;
        public int InformantPct;
        public int BeaconChargePct;

        public int HealPct;

        public int LostDollars = -1;

        public int ExtractedDollars = -1;

        public int HaulGoalDollars = -1;

        public int BombRefillPct = -1;

        public int CheckmateLossDollars = -1;

        public int DeliveryPermille()
            => PermilleOf(ExtractedDollars, BaseDollars);

        public int QuotaPermille()
            => PermilleOf(HaulGoalDollars, BaseDollars);

        public int CheckmateLinePermille()
            => PermilleOf(CheckmateLossDollars, BaseDollars);

        private static int PermilleOf(int dollars, int baseDollars)
        {
            if (dollars < 0 || baseDollars <= 0) return -1;
            long permille = (long)dollars * 1000 / baseDollars;
            return permille > 1000 ? 1000 : (int)permille;
        }

        public static MeetingGaugeSnapshot FromData(int[] data)
        {
            if (data == null || data.Length < 7) return null;
            return new MeetingGaugeSnapshot
            {
                RatioPermille = data[0],
                BaseDollars = data[1],
                StaminaPct = data[2],
                JumpPct = data[3],
                EnemyIgnorePct = data[4],
                InformantPct = data[5],
                BeaconChargePct = data[6],
                LostDollars = data.Length >= 8 ? data[7] : -1,
                ExtractedDollars = data.Length >= 9 ? data[8] : -1,
                HaulGoalDollars = data.Length >= 10 ? data[9] : -1,
                BombRefillPct = data.Length >= 11 ? data[10] : -1,
                CheckmateLossDollars = data.Length >= 12 ? data[11] : -1,
                HealPct = data.Length >= 13 ? data[12] : 0,
            };
        }
    }
}
