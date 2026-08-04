using System;

namespace Werewolf.Core
{
    public static class WWCheckmateCodes
    {
        public const byte CheckmateReveal = 187;
    }

    public static class CheckmateJudge
    {
        public static int RemainingQuotaDollars(int haulGoalDollars, int extractionPoints, int pointsCompleted)
        {
            if (haulGoalDollars <= 0 || extractionPoints <= 0) return -1;
            if (pointsCompleted < 0) pointsCompleted = 0;
            if (pointsCompleted >= extractionPoints) return 0;
            return haulGoalDollars / extractionPoints * (extractionPoints - pointsCompleted);
        }

        public static bool IsCheckmate(float obtainableDollars, int remainingQuotaDollars)
            => remainingQuotaDollars > 0 && obtainableDollars < remainingQuotaDollars;
    }

    public sealed class HaulFreeze
    {
        public const long TimeoutMs = 60_000;

        private bool _open;
        private long _lastSuckUnixMs;

        public void NoteSuck(long nowUnixMs)
        {
            _open = true;
            _lastSuckUnixMs = nowUnixMs;
        }

        public void Close() => _open = false;

        public bool IsHolding(long nowUnixMs)
            => _open && nowUnixMs - _lastSuckUnixMs < TimeoutMs;
    }

    public enum CheckmateAction : byte
    {
        None = 0,

        StartCeremony = 1,

        ConfirmWin = 2,
    }

    public sealed class CheckmateSequence
    {
        public const int CeremonyMs = 7000;

        public bool Detected { get; private set; }

        public bool CeremonyStarted { get; private set; }

        public bool Confirmed { get; private set; }

        private long _ceremonyStartUnixMs;

        public void NotifyDetected()
        {
            if (Confirmed) return;
            Detected = true;
        }

        public CheckmateAction Tick(long nowUnixMs, GamePhase phase, bool curseActive)
        {
            if (Confirmed) return CheckmateAction.None;

            if (!CeremonyStarted)
            {
                if (!Detected) return CheckmateAction.None;
                if (phase != GamePhase.Play || curseActive) return CheckmateAction.None;
                CeremonyStarted = true;
                _ceremonyStartUnixMs = nowUnixMs;
                return CheckmateAction.StartCeremony;
            }

            if (nowUnixMs - _ceremonyStartUnixMs >= CeremonyMs)
            {
                Confirmed = true;
                return CheckmateAction.ConfirmWin;
            }
            return CheckmateAction.None;
        }
    }

    public static class CheckmateCeremony
    {
        public const float BackdropFadeSec = 0.3f;

        public const float PostRevealPauseSec = 0.4f;

        public const float StampEntranceSec = 0.45f;

        public const float StampHoldSec = 3.0f;
    }
}
