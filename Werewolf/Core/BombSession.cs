using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class BombSession
    {
        private readonly GameConfig _config;
        private readonly IReadOnlyList<WPlayer> _players;

        private int _bomberActor;
        private int _targetActor;
        private byte _ammo;
        private long _plantReadyUnixMs;
        private long _detonateReadyUnixMs;
        private BombDenyReason _lastDeny;

        private float _lastGaugePct;

        public BombSession(GameConfig config, GameSession session, long nowUnixMs)
            : this(config, session != null ? session.Players : null, nowUnixMs) { }

        public BombSession(GameConfig config, IReadOnlyList<WPlayer> players, long nowUnixMs)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _players = players ?? throw new ArgumentNullException(nameof(players));

            _bomberActor = ResolveBomberActor(_players);
            _targetActor = -1;
            long initialCooldownMs = _config.BomberInitialCooldownSec * 1000L;
            _plantReadyUnixMs = _bomberActor >= 0 ? nowUnixMs + initialCooldownMs : 0;
            _detonateReadyUnixMs = 0;
            _ammo = _bomberActor >= 0 ? (byte)1 : (byte)0;
            _lastDeny = BombDenyReason.None;
            Dirty = _bomberActor >= 0;
        }

        public int BomberActor => _bomberActor;

        public bool HasBomb => _targetActor != -1;

        public int TargetActor => _targetActor;

        public byte Ammo => _ammo;

        public BombDenyReason LastDeny => _lastDeny;

        public bool Dirty { get; private set; }

        public BombDenyReason TryPlant(int senderActor, int targetActor, long nowUnixMs)
        {
            if (_bomberActor < 0) return Deny(BombDenyReason.NotBomber);
            if (senderActor != _bomberActor) return Deny(BombDenyReason.NotBomber);

            if (nowUnixMs < _plantReadyUnixMs) return Deny(BombDenyReason.PlantCooldown);

            if (targetActor == _bomberActor) return Deny(BombDenyReason.TargetInvalid);
            var target = FindPlayer(targetActor);
            if (target == null || !target.Alive) return Deny(BombDenyReason.TargetInvalid);

            if (_targetActor == targetActor) return BombDenyReason.None;

            bool replant = _targetActor != -1;
            if (!replant && _ammo == 0) return Deny(BombDenyReason.NoAmmo);

            if (!replant) _ammo--;
            _targetActor = targetActor;

            long cdMs = _config.BomberCooldownSec * 1000L;
            _plantReadyUnixMs = nowUnixMs + cdMs;
            _detonateReadyUnixMs = nowUnixMs + cdMs;
            _lastDeny = BombDenyReason.None;
            Dirty = true;
            return BombDenyReason.None;
        }

        public BombDenyReason TryDetonate(int senderActor, long nowUnixMs,
            bool meetingLocked, bool targetNearTruck, out int detonatedTargetActor)
        {
            detonatedTargetActor = -1;

            if (_bomberActor < 0 || senderActor != _bomberActor) return Deny(BombDenyReason.NotBomber);
            if (_targetActor == -1) return Deny(BombDenyReason.NoBomb);
            if (nowUnixMs < _detonateReadyUnixMs) return Deny(BombDenyReason.DetonateCooldown);
            if (meetingLocked) return Deny(BombDenyReason.MeetingLocked);
            if (targetNearTruck) return Deny(BombDenyReason.TruckZone);

            var target = FindPlayer(_targetActor);
            bool targetDead = target == null || !target.Alive;

            long cdMs = _config.BomberCooldownSec * 1000L;
            _plantReadyUnixMs = nowUnixMs + cdMs;
            _detonateReadyUnixMs = 0;

            if (targetDead)
            {
                _targetActor = -1;
                _lastDeny = BombDenyReason.TargetDead;
                Dirty = true;
                return BombDenyReason.TargetDead;
            }

            detonatedTargetActor = _targetActor;
            _targetActor = -1;
            _lastDeny = BombDenyReason.None;
            Dirty = true;
            return BombDenyReason.None;
        }

        public void OnGaugeChanged(float cumulativeGaugePct)
        {
            if (_bomberActor < 0) return;
            int refillPct = _config.BomberAmmoRefillPct;
            if (refillPct <= 0) return;

            int prev = (int)(_lastGaugePct / refillPct);
            int now = (int)(cumulativeGaugePct / refillPct);
            _lastGaugePct = cumulativeGaugePct;

            int delta = now - prev;
            if (delta <= 0) return;

            int next = _ammo + delta;
            if (next > byte.MaxValue) next = byte.MaxValue;
            if (next == _ammo) return;
            _ammo = (byte)next;
            Dirty = true;
        }

        public void OnMeetingEnded(long nowUnixMs)
        {
            if (_bomberActor < 0) return;

            long readyUnixMs = nowUnixMs + _config.BomberCooldownSec * 1000L;
            _plantReadyUnixMs = readyUnixMs;
            _detonateReadyUnixMs = _targetActor != -1 ? readyUnixMs : 0;
            _lastDeny = BombDenyReason.None;
            Dirty = true;
        }

        public void OnPlayerDied(int actor)
        {
            if (_bomberActor < 0) return;
            if (actor == _bomberActor)
            {
                _bomberActor = -1;
                _targetActor = -1;
                _plantReadyUnixMs = 0;
                _detonateReadyUnixMs = 0;
                Dirty = true;
            }
        }

        public void OnPlayerDisconnected(int actor)
        {
            if (_bomberActor < 0) return;
            if (actor == _bomberActor)
            {
                _bomberActor = -1;
                _targetActor = -1;
                _plantReadyUnixMs = 0;
                _detonateReadyUnixMs = 0;
                Dirty = true;
                return;
            }
            if (actor == _targetActor)
            {
                _targetActor = -1;
                _detonateReadyUnixMs = 0;
                Dirty = true;
            }
        }

        public void DebugGrantAmmo(int n)
        {
            if (_bomberActor < 0 || n <= 0) return;
            int next = _ammo + n;
            if (next > byte.MaxValue) next = byte.MaxValue;
            if (next == _ammo) return;
            _ammo = (byte)next;
            Dirty = true;
        }

        public BomberStateSnapshot BuildSnapshot()
        {
            Dirty = false;
            return new BomberStateSnapshot(
                _targetActor, _ammo, _plantReadyUnixMs, _detonateReadyUnixMs, _lastDeny);
        }

        private BombDenyReason Deny(BombDenyReason reason)
        {
            _lastDeny = reason;
            Dirty = true;
            return reason;
        }

        private WPlayer FindPlayer(int actor)
        {
            foreach (var p in _players)
            {
                if (p.ActorNumber == actor) return p;
            }
            return null;
        }

        private static int ResolveBomberActor(IReadOnlyList<WPlayer> players)
        {
            foreach (var p in players)
            {
                if (p.Role == Role.Bomber && p.Alive) return p.ActorNumber;
            }
            return -1;
        }
    }
}
