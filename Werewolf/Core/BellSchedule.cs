using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class BellSchedule
    {
        public const int AlertThresholdSec = 300;

        public const float BaseVolumeScale = 0.5f;

        public const float FinalVolumeScale = 1.0f;

        public const float FiveMinuteVolumeScale = 1.0f;

        public const string DefaultClipKey = "sfx_bell";

        public const string FiveMinuteClipKey = "sfx_bell_five_minutes";

        private const long RearmToleranceMs = 1000;

        private static readonly (int ThresholdSec, int IntervalSec)[] Tiers =
        {
            (10, 1),
            (30, 3),
            (60, 5),
            (180, 10),
            (AlertThresholdSec, 30),
        };

        private static readonly int[] MarksSec = BuildMarks();

        private bool _armed;
        private int _nextIdx;
        private long _lastRemainingMs;

        public static bool AlertActive(long remainingMs)
            => remainingMs <= AlertThresholdSec * 1000L;

        public static int IntervalSecFor(long remainingSec)
        {
            if (remainingSec <= 0) return 0;
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (remainingSec <= Tiers[i].ThresholdSec) return Tiers[i].IntervalSec;
            }
            return 0;
        }

        public static float VolumeScaleFor(int markSec)
        {
            if (markSec == AlertThresholdSec) return FiveMinuteVolumeScale;
            int finalTierTop = Tiers[0].ThresholdSec;
            if (markSec > finalTierTop) return BaseVolumeScale;
            if (markSec < 1) markSec = 1;
            float t = (finalTierTop - markSec) / (float)(finalTierTop - 1);
            return BaseVolumeScale + (FinalVolumeScale - BaseVolumeScale) * t;
        }

        public static string ClipKeyFor(int markSec)
            => markSec == AlertThresholdSec ? FiveMinuteClipKey : DefaultClipKey;

        public int Tick(long remainingMs)
        {
            if (!_armed || remainingMs > _lastRemainingMs + RearmToleranceMs)
            {
                _armed = true;
                _lastRemainingMs = remainingMs;
                _nextIdx = 0;
                while (_nextIdx < MarksSec.Length && MarksSec[_nextIdx] * 1000L >= remainingMs) _nextIdx++;
                return 0;
            }

            _lastRemainingMs = remainingMs;

            int rung = 0;
            while (_nextIdx < MarksSec.Length && remainingMs <= MarksSec[_nextIdx] * 1000L)
            {
                rung = MarksSec[_nextIdx];
                _nextIdx++;
            }
            return rung;
        }

        public void Reset()
        {
            _armed = false;
            _nextIdx = 0;
            _lastRemainingMs = 0;
        }

        private static int[] BuildMarks()
        {
            var marks = new List<int>();
            for (int s = AlertThresholdSec; s >= 1; s--)
            {
                int interval = IntervalSecFor(s);
                if (interval > 0 && s % interval == 0) marks.Add(s);
            }
            return marks.ToArray();
        }
    }
}
