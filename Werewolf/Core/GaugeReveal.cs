using System;

namespace Werewolf.Core
{
    public sealed class GaugeReveal
    {
        public const int HoldMs = 1000;

        public const int DurationMs = 1600;

        public bool Active { get; private set; }

        private long _startUnixMs;
        private int _fromPermille;
        private int _toPermille;
        private int _fromLoss;
        private int _toLoss;

        public static bool ShouldAnimate(int fromPermille, int toPermille)
            => toPermille > fromPermille;

        public void Start(int fromPermille, int toPermille, int fromLoss, int toLoss, long nowUnixMs)
        {
            _fromPermille = fromPermille;
            _toPermille = toPermille;
            _fromLoss = fromLoss <= toLoss ? fromLoss : toLoss;
            _toLoss = toLoss;
            _startUnixMs = nowUnixMs + HoldMs;
            Active = true;
        }

        public void Stop() => Active = false;

        public bool GrowthStarted(long nowUnixMs)
            => Active && nowUnixMs >= _startUnixMs;

        public double Progress(long nowUnixMs)
        {
            if (!Active) return 1.0;
            long elapsed = nowUnixMs - _startUnixMs;
            if (elapsed <= 0) return 0.0;
            if (elapsed >= DurationMs) return 1.0;
            double inv = 1.0 - elapsed / (double)DurationMs;
            return 1.0 - inv * inv * inv;
        }

        public int CurrentPermille(long nowUnixMs)
            => Interpolate(_fromPermille, _toPermille, Progress(nowUnixMs));

        public int CurrentLoss(long nowUnixMs)
            => Interpolate(_fromLoss, _toLoss, Progress(nowUnixMs));

        public bool Done(long nowUnixMs)
            => !Active || nowUnixMs - _startUnixMs >= DurationMs;

        private static int Interpolate(int from, int to, double progress)
            => from + (int)Math.Round((to - from) * progress);
    }
}
