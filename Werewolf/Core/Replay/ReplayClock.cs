using System;

namespace Werewolf.Core.Replay
{
    public sealed class ReplayClock
    {
        public const float NormalSpeed = 8f;
        public const float FastSpeed = 32f;

        private readonly ReplayPace _pace;

        public double Duration { get; private set; }
        public double T { get; private set; }
        public bool Playing { get; private set; }

        public bool Fast { get; set; }

        public ReplayClock(double duration) : this(duration, null) { }

        public ReplayClock(double duration, ReplayPace pace)
        {
            Duration = Math.Max(0, duration);
            _pace = pace;
        }

        public float EffectiveSpeed()
            => _pace != null ? _pace.SpeedAt(T, Fast) : BaseSpeed;

        public float BaseSpeed => Fast ? FastSpeed : NormalSpeed;

        public void Tick(double dtRealSec)
        {
            if (!Playing) return;
            if (dtRealSec < 0) dtRealSec = 0;
            T = _pace != null ? _pace.Advance(T, dtRealSec, Fast) : T + dtRealSec * BaseSpeed;
            if (T >= Duration)
            {
                T = Duration;
                Playing = false;
            }
        }

        public void TogglePlay() => SetPlaying(!Playing);

        public void SetPlaying(bool playing)
        {
            if (playing && !Playing && T >= Duration && Duration > 0) T = 0;
            Playing = playing;
        }

        public void Seek(double t)
        {
            if (t < 0) t = 0;
            if (t > Duration) t = Duration;
            T = t;
        }
    }
}
