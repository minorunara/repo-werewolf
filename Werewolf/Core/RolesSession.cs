using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class RolesSession
    {
        private readonly GameConfig _config;
        private readonly GameSession _session;
        private readonly Random _rng;

        private readonly PerkGauge _gauge;
        private readonly Dictionary<int, BeaconState> _beacons = new Dictionary<int, BeaconState>();
        private readonly Dictionary<int, bool> _wolfMode = new Dictionary<int, bool>();

        private bool _meetingGaugeSent;
        private int _lastSyncedPermille = -1;

        private readonly bool _catPossible;
        private readonly bool _bomberPossible;

        private int _checkmateLineDollars = -1;

        private long _catNextSyncUnixMs;

        private int _beaconUsesSinceAudit;

        public RolesSession(GameConfig config, GameSession session, long nowUnixMs, Random rng)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _gauge = new PerkGauge(config);

            _catPossible = config.BlackCatPossible(session.Players.Count);
            _bomberPossible = config.BomberPossible(session.Players.Count);

            long suppressUntil = nowUnixMs + config.BeaconSuppressStartSec * 1000L;
            foreach (var p in _session.Players)
            {
                if (p.Role != Role.Werewolf) continue;
                var beacon = new BeaconState(config);
                beacon.Suppress(suppressUntil);
                _beacons[p.ActorNumber] = beacon;
            }
        }

        public const int CurseKillDelaySec = 2;

        public event Action<OutboundMessage> OnSend;

        public event Action OnInformantEstablished;

        public event Action<int> OnCurseKill;

        public event Action<int> OnCurseSealed;

        public event Action<int, bool> OnEnemyIgnoreChanged;

        public event Action<PerkId> OnPerkUnlocked;

        public event Action<int> OnBeaconTriggered;

        public event Action<float> OnGaugePctChanged;

        public CurseSession ActiveCurse { get; private set; }

        public bool InformantEstablished { get; private set; }

        public PerkGauge Gauge => _gauge;

        public void FreezeBase(float totalDollars)
        {
            bool wasFrozen = _gauge.BaseFrozen;
            _gauge.FreezeBase(totalDollars);
            if (!wasFrozen && _gauge.BaseFrozen) SyncGaugeToTargets();
        }

        public void AddValueLoss(float lostDollars, bool isOrb)
        {
            if (isOrb && !_config.OrbGaugeEnabled) return;

            var events = _gauge.AddLoss(lostDollars);
            foreach (var e in events)
            {
                ApplyGaugeEvent(e);
            }

            if (events.Count > 0 || _gauge.DisplayPermille != _lastSyncedPermille)
            {
                SyncGaugeToTargets();
            }

            if (_gauge.BaseFrozen && _gauge.BaseDollars > 0f)
            {
                OnGaugePctChanged?.Invoke(_gauge.LostDollars / _gauge.BaseDollars * 100f);
            }
        }

        private void ApplyGaugeEvent(GaugeEvent e)
        {
            switch (e.Kind)
            {
                case GaugeEventKind.PerkUnlocked:
                    if (e.Perk == PerkId.EnemyIgnore)
                    {
                        foreach (var kv in _wolfMode)
                        {
                            if (kv.Value) OnEnemyIgnoreChanged?.Invoke(kv.Key, true);
                        }
                    }
                    OnPerkUnlocked?.Invoke(e.Perk);
                    break;

                case GaugeEventKind.BeaconCharged:
                    foreach (var beacon in _beacons.Values)
                    {
                        beacon.AddCharges(e.BeaconChargeCount);
                    }
                    break;

                case GaugeEventKind.InformantReady:
                    FireInformant();
                    break;
            }
        }

        private void FireInformant()
        {
            if (InformantEstablished) return;

            bool catAlive = false;
            foreach (var p in _session.Players)
            {
                if (p.Role == Role.BlackCat && p.Alive)
                {
                    catAlive = true;
                    break;
                }
            }
            if (!catAlive)
            {
                WLog.Line("informant_skipped", secret: true, ("reason", "cat_dead"));
                return;
            }

            InformantEstablished = true;
            WLog.Line("informant_established", secret: true, ("permille", _gauge.DisplayPermille));
            OnInformantEstablished?.Invoke();

            _catNextSyncUnixMs = 0;
        }

        public void HandleRoleAction(int senderActor, byte subtype, int arg, byte flag, long nowUnixMs)
        {
            switch (subtype)
            {
                case RoleActionSubtype.CurseDesignate:
                    HandleCurseDesignate(senderActor, arg, nowUnixMs);
                    break;

                case RoleActionSubtype.BeaconUse:
                    HandleBeaconUse(senderActor, nowUnixMs);
                    break;

                case RoleActionSubtype.WolfModeSync:
                    HandleWolfModeSync(senderActor, flag != 0);
                    break;

                default:
                    WLog.Line("role_action_dropped", secret: true,
                        ("sender", senderActor), ("reason", "unknown_subtype"), ("subtype", subtype));
                    break;
            }
        }

        private void HandleCurseDesignate(int senderActor, int targetActor, long nowUnixMs)
        {
            if (ActiveCurse == null)
            {
                WLog.Line("role_action_dropped", secret: true,
                    ("sender", senderActor), ("reason", "no_active_curse"));
                return;
            }
            ActiveCurse.Designate(senderActor, targetActor, nowUnixMs);
        }

        private void HandleBeaconUse(int senderActor, long nowUnixMs)
        {
            var sender = FindPlayer(senderActor);
            if (sender == null || sender.Role != Role.Werewolf || !sender.Alive
                || !_beacons.TryGetValue(senderActor, out BeaconState beacon))
            {
                WLog.Line("role_action_dropped", secret: true,
                    ("sender", senderActor), ("reason", "not_alive_werewolf"), ("action", "beacon"));
                return;
            }

            if (_session.Phase == GamePhase.Meeting)
            {
                WLog.Line("beacon_use", secret: true,
                    ("actor", senderActor), ("status", BeaconStatus.MeetingActive),
                    ("charges", beacon.Charges));
                SyncGaugeTo(senderActor, BeaconStatus.MeetingActive);
                return;
            }

            BeaconStatus status = beacon.TryUse(nowUnixMs);
            WLog.Line("beacon_use", secret: true,
                ("actor", senderActor), ("status", status), ("charges", beacon.Charges));

            if (status == BeaconStatus.Ok)
            {
                OnBeaconTriggered?.Invoke(senderActor);
                _beaconUsesSinceAudit++;
            }

            SyncGaugeTo(senderActor, status);
        }

        private void HandleWolfModeSync(int senderActor, bool on)
        {
            var sender = FindPlayer(senderActor);
            if (sender == null || sender.Role != Role.Werewolf || !sender.Alive)
            {
                WLog.Line("role_action_dropped", secret: true,
                    ("sender", senderActor), ("reason", "not_alive_werewolf"), ("action", "wolfmode"));
                return;
            }

            _wolfMode[senderActor] = on;
            WLog.Line("wolfmode_sync", secret: true, ("actor", senderActor), ("on", on));

            bool effective = on && PerkFlagsUtil.Has(_gauge.UnlockedFlags, PerkId.EnemyIgnore);
            OnEnemyIgnoreChanged?.Invoke(senderActor, effective);
        }

        public void OnMeetingStarted(long nowUnixMs, int extractedDollars = -1, int haulGoalDollars = -1,
                                     int checkmateLossDollars = -1)
        {
            if (_meetingGaugeSent) return;
            _meetingGaugeSent = true;

            int beaconUses = _beaconUsesSinceAudit;
            _beaconUsesSinceAudit = 0;
            Send(new OutboundMessage(
                WWRolesCodes.BeaconAudit,
                new object[] { (byte)(beaconUses > byte.MaxValue ? byte.MaxValue : beaconUses) },
                MessageTarget.All, null));
            WLog.Line("beacon_audit_sent", secret: false, ("uses", beaconUses));

            Send(new OutboundMessage(
                WWRolesCodes.RoleState,
                new object[]
                {
                    RoleStateSubtype.MeetingGauge,
                    BuildMeetingGaugeData(extractedDollars, haulGoalDollars, checkmateLossDollars),
                    0L,
                },
                MessageTarget.All, null));
            WLog.Line("meeting_gauge_sent", secret: false, ("permille", _gauge.DisplayPermille));

            ForceCatSync(nowUnixMs);
        }

        public int[] BuildMeetingGaugeData(int extractedDollars, int haulGoalDollars,
                                           int checkmateLossDollars = -1)
        {
            return new[]
            {
                _gauge.DisplayPermille,
                (int)_gauge.BaseDollars,
                _config.StaminaUnlockPct,
                _config.JumpUnlockPct,
                _config.EnemyIgnoreUnlockPct,
                _catPossible ? _config.InformantThresholdPct : 0,
                _config.BeaconChargePct,
                (int)(_gauge.LostDollars + 0.5f),
                extractedDollars,
                haulGoalDollars,
                _bomberPossible ? _config.BomberAmmoRefillPct : 0,
                checkmateLossDollars,
                _config.HealUnlockPct,
            };
        }

        public void OnMeetingEnded(long nowUnixMs)
        {
            _meetingGaugeSent = false;
            long until = nowUnixMs + _config.BeaconSuppressAfterMeetingSec * 1000L;
            foreach (var beacon in _beacons.Values)
            {
                beacon.Suppress(until);
            }
            SyncGaugeToTargets();

            ForceCatSync(nowUnixMs);
        }

        public bool TryStartCurse(int executedActor, long nowUnixMs, out long holdExtensionMs,
                                  int[] voterActors = null)
        {
            holdExtensionMs = 0;

            if (!_config.BlackCatCurseEnabled) return false;

            var executed = FindPlayer(executedActor);
            if (executed == null || executed.Role != Role.BlackCat) return false;
            if (ActiveCurse != null && !ActiveCurse.Resolved) return false;

            long resolveAt = nowUnixMs + _config.CurseWaitSec * 1000L;
            ActiveCurse = new CurseSession(executedActor, resolveAt, voterActors);
            holdExtensionMs = (_config.CurseWaitSec + CurseKillDelaySec
                - MeetingSession.PostResultKillDelaySec) * 1000L;

            if (voterActors != null && !executed.IsBot)
            {
                Send(new OutboundMessage(
                    WWRolesCodes.CurseCandidates,
                    new object[] { voterActors },
                    MessageTarget.Actors, new[] { executedActor }));
                WLog.Line("curse_candidates_sent", secret: true,
                    ("catActor", executedActor), ("count", voterActors.Length));
            }

            Send(new OutboundMessage(
                WWRolesCodes.RoleState,
                new object[] { RoleStateSubtype.CurseStarted, new[] { executedActor }, resolveAt },
                MessageTarget.All, null));
            WLog.Line("curse_started", secret: false,
                ("catActor", executedActor), ("resolveAt", resolveAt),
                ("restricted", voterActors != null));
            return true;
        }

        public void OnPlayerDied(int actorNumber)
        {
            if (_wolfMode.TryGetValue(actorNumber, out bool on) && on)
            {
                _wolfMode[actorNumber] = false;
                OnEnemyIgnoreChanged?.Invoke(actorNumber, false);
            }
        }

        public void Tick(long nowUnixMs)
        {
            TickCatGaugeSync(nowUnixMs);

            var curse = ActiveCurse;
            if (curse == null || curse.Resolved) return;

            var resolution = curse.TryResolve(nowUnixMs, _session.Players, InformantEstablished, _rng);
            if (resolution == null) return;

            WLog.Line("curse_resolved", secret: true,
                ("victim", resolution.VictimActor), ("designated", resolution.WasDesignated));

            OnCurseSealed?.Invoke(curse.CatActor);

            if (resolution.HasVictim)
            {
                OnCurseKill?.Invoke(resolution.VictimActor);
            }

            Send(new OutboundMessage(
                WWRolesCodes.RoleState,
                new object[] { RoleStateSubtype.CurseResolved, new[] { resolution.VictimActor }, 0L },
                MessageTarget.All, null));
        }

        public void DebugAddGaugePct(int pct)
        {
            if (!_gauge.BaseFrozen) FreezeBase(10000f);
            AddValueLoss(_gauge.BaseDollars * pct / 100f, isOrb: false);
        }

        public void DebugChargeBeacon(int count)
        {
            foreach (var beacon in _beacons.Values)
            {
                beacon.AddCharges(count);
            }
            SyncGaugeToTargets();
        }

        public void DebugUnlockPerk(PerkId perk)
        {
            if (!_gauge.DebugForceUnlock(perk)) return;
            ApplyGaugeEvent(GaugeEvent.Unlocked(perk));
            SyncGaugeToTargets();
        }

        public void DebugUseBeacon(int actorNumber, long nowUnixMs)
        {
            if (_beacons.TryGetValue(actorNumber, out BeaconState beacon))
            {
                beacon.DebugClearRestrictions();
            }
            HandleRoleAction(actorNumber, RoleActionSubtype.BeaconUse, 0, 0, nowUnixMs);
        }

        public void DebugForceInformant()
        {
            if (!_gauge.DebugForceInformant()) return;
            FireInformant();
            SyncGaugeToTargets();
        }

        public void UpdateCheckmateLine(int lineDollars)
        {
            if (lineDollars == _checkmateLineDollars) return;
            _checkmateLineDollars = lineDollars;
            SyncGaugeToTargets();
            WLog.Line("checkmate_line_sync", secret: false, ("line", lineDollars));
        }

        private void SyncGaugeToTargets()
        {
            _lastSyncedPermille = _gauge.DisplayPermille;

            bool catRealtime = _config.CatGaugeSyncIntervalSec <= 0;
            bool catAwakened = _session.BlackCatSelfAwarenessIssued;
            foreach (var p in _session.Players)
            {
                if (p.IsBot) continue;
                if (p.Role == Role.Werewolf || p.Role == Role.Bomber)
                {
                    SyncGaugeTo(p.ActorNumber, BeaconStatus.Ok);
                }
                else if (p.Role == Role.BlackCat && catAwakened && catRealtime)
                {
                    SyncGaugeTo(p.ActorNumber, BeaconStatus.Ok);
                }
            }
        }

        private void TickCatGaugeSync(long nowUnixMs)
        {
            if (_config.CatGaugeSyncIntervalSec <= 0) return;
            if (_session.Phase != GamePhase.Play) return;
            if (!_session.BlackCatSelfAwarenessIssued) return;
            if (nowUnixMs < _catNextSyncUnixMs) return;
            SyncCatNow(nowUnixMs);
        }

        private void ForceCatSync(long nowUnixMs)
        {
            if (_config.CatGaugeSyncIntervalSec <= 0) return;
            if (!_session.BlackCatSelfAwarenessIssued) return;
            SyncCatNow(nowUnixMs);
        }

        private void SyncCatNow(long nowUnixMs)
        {
            _catNextSyncUnixMs = nowUnixMs + _config.CatGaugeSyncIntervalSec * 1000L;
            foreach (var p in _session.Players)
            {
                if (p.IsBot || p.Role != Role.BlackCat) continue;
                SyncGaugeTo(p.ActorNumber, BeaconStatus.Ok, _config.CatGaugeSyncIntervalSec);
            }
        }

        private void SyncGaugeTo(int actorNumber, BeaconStatus status, int nextUpdateInSec = 0)
        {
            byte charges = 0;
            long readyUnixMs = 0;
            if (_beacons.TryGetValue(actorNumber, out BeaconState beacon))
            {
                charges = (byte)(beacon.Charges > byte.MaxValue ? byte.MaxValue : beacon.Charges);
                readyUnixMs = beacon.ReadyUnixMs;
            }

            Send(new OutboundMessage(
                WWRolesCodes.SyncPerkGauge,
                new object[]
                {
                    _gauge.DisplayPermille,
                    (byte)_gauge.UnlockedFlags,
                    charges,
                    (byte)status,
                    readyUnixMs,
                    new[]
                    {
                        (int)_gauge.BaseDollars,
                        _config.StaminaUnlockPct,
                        _config.JumpUnlockPct,
                        _config.EnemyIgnoreUnlockPct,
                        _catPossible ? _config.InformantThresholdPct : 0,
                        _config.BeaconChargePct,
                        nextUpdateInSec,
                        (int)(_gauge.LostDollars + 0.5f),
                        _checkmateLineDollars,
                        _config.HealUnlockPct,
                    },
                },
                MessageTarget.Actors, new[] { actorNumber }));
        }

        private WPlayer FindPlayer(int actorNumber)
        {
            foreach (var p in _session.Players)
            {
                if (p.ActorNumber == actorNumber) return p;
            }
            return null;
        }

        private void Send(OutboundMessage message) => OnSend?.Invoke(message);
    }
}
