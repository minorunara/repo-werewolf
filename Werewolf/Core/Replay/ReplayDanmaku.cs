using System;
using System.Collections.Generic;

namespace Werewolf.Core.Replay
{
    public enum ReplayDanmakuProfile
    {
        Slash = 0,
        Impact = 1,
        Cool = 2,
    }

    public enum ReplayDanmakuIdleStyle
    {
        Still = 0,
        Glide = 1,
        Float = 2,
    }

    public sealed class ReplayDanmakuComment
    {
        public int Actor;
        public string Text = "";
        public string Line1 = "";
        public string Line2;
        public int DisplayChars;

        public int Slot;

        public int Depth;

        public double DepthFrom;

        public double HandoffElapsed;

        public double SpawnT;

        public double Elapsed;

        public double WidthRatio;

        public double DwellSec;

        public uint VisualSeed;

        public ReplayDanmakuProfile Profile;

        public double TotalSec => ReplayChatText.SlideInSec + DwellSec + ReplayChatText.SlideOutSec;
    }

    public sealed class ReplayDanmaku
    {
        public const int SlotCount = 4;

        public const int MaxVisibleShots = 4;

        public const double HandoffSec = 0.26;

        public const double StampLifeSec = 1.5;

        public const double LandingShakeSec = 0.14;

        public const double ArrivalTiltSettleSec = 0.24;

        public const double IdleFadeInSec = 0.24;

        private const double FocusOffsetX = 0.10;
        private const double EchoStepX = 0.065;
        private const double EdgeMargin = 0.025;

        private readonly IReadOnlyList<(double T, int Actor, string Text)> _chats;
        private readonly IReadOnlyList<(double T, int ExecutedActor)> _results;
        private readonly ReplayPace _pace;
        private readonly List<ReplayDanmakuComment> _active = new List<ReplayDanmakuComment>();

        private int _chatCursor;
        private int _resultCursor;
        private bool _stampVisible;
        private int _stampExecutedActor;
        private double _stampElapsed;

        public ReplayDanmaku(
            IReadOnlyList<(double T, int Actor, string Text)> chats,
            IReadOnlyList<(double T, int ExecutedActor)> results,
            ReplayPace pace)
        {
            _chats = chats ?? Array.Empty<(double, int, string)>();
            _results = results ?? Array.Empty<(double, int)>();
            _pace = pace;
        }

        public IReadOnlyList<ReplayDanmakuComment> Active => _active;

        public bool TryGetStamp(out int executedActor, out double progress)
        {
            executedActor = _stampExecutedActor;
            progress = _stampVisible ? _stampElapsed / StampLifeSec : 0;
            return _stampVisible;
        }

        public void Step(double prevT, double newT, double dtRealPlaying,
            Func<ReplayDanmakuComment, double> measureWidth)
        {
            if (newT < prevT)
            {
                ClearActive();
                _chatCursor = 0;
                _resultCursor = 0;
                return;
            }

            if (dtRealPlaying > 0)
            {
                bool focusExpired = false;
                for (int i = _active.Count - 1; i >= 0; i--)
                {
                    ReplayDanmakuComment c = _active[i];
                    c.Elapsed += dtRealPlaying;
                    c.HandoffElapsed += dtRealPlaying;
                    bool evictionComplete = c.Depth >= MaxVisibleShots
                        && c.HandoffElapsed >= HandoffSec;
                    if (evictionComplete || c.Elapsed >= c.TotalSec)
                    {
                        if (c.Depth == 0) focusExpired = true;
                        _active.RemoveAt(i);
                    }
                }
                if (focusExpired) _active.Clear();
                if (_stampVisible)
                {
                    _stampElapsed += dtRealPlaying;
                    if (_stampElapsed >= StampLifeSec) _stampVisible = false;
                }
            }

            if (newT <= prevT) return;

            while (_chatCursor < _chats.Count && _chats[_chatCursor].T <= prevT) _chatCursor++;
            while (_chatCursor < _chats.Count && _chats[_chatCursor].T <= newT)
            {
                (double t, int actor, string text) = _chats[_chatCursor];
                _chatCursor++;
                Spawn(t, actor, text, elapsed: 0, measureWidth);
            }

            while (_resultCursor < _results.Count && _results[_resultCursor].T <= prevT) _resultCursor++;
            while (_resultCursor < _results.Count && _results[_resultCursor].T <= newT)
            {
                _stampVisible = true;
                _stampExecutedActor = _results[_resultCursor].ExecutedActor;
                _stampElapsed = 0;
                _resultCursor++;
            }
        }

        public void ClearActive()
        {
            _active.Clear();
            _stampVisible = false;
        }

        public void RebuildAtSeek(double t, bool fast, Func<ReplayDanmakuComment, double> measureWidth)
        {
            ClearActive();
            _chatCursor = ReplayPlayback.LastIndexAtOrBefore(CollectChatTimes(), t) + 1;
            _resultCursor = 0;
            while (_resultCursor < _results.Count && _results[_resultCursor].T <= t) _resultCursor++;
            if (_pace == null) return;

            if (_chatCursor > 0)
            {
                (double T, int Actor, string Text) latest = _chats[_chatCursor - 1];
                double latestElapsed = _pace.RealSecondsBetween(latest.T, t, fast);
                double latestTotal = ReplayChatText.TotalSeconds(
                    ReplayChatText.DisplayLength(latest.Text ?? ""));
                if (latestElapsed >= latestTotal) return;
            }

            double maxTotal = ReplayChatText.SlideInSec + ReplayChatText.DwellMaxSec
                + ReplayChatText.SlideOutSec;
            var restore = new List<(double T, int Actor, string Text, double Elapsed)>();
            for (int i = _chatCursor - 1; i >= 0; i--)
            {
                double elapsed = _pace.RealSecondsBetween(_chats[i].T, t, fast);
                if (elapsed >= maxTotal) break;
                double total = ReplayChatText.TotalSeconds(
                    ReplayChatText.DisplayLength(_chats[i].Text ?? ""));
                if (elapsed >= total) continue;
                restore.Add((_chats[i].T, _chats[i].Actor, _chats[i].Text, elapsed));
                if (restore.Count >= MaxVisibleShots) break;
            }
            restore.Reverse();
            foreach ((double spawnT, int actor, string text, double elapsed) in restore)
            {
                Spawn(spawnT, actor, text, elapsed, measureWidth);
            }

            if (_active.Count == 0) return;
            double newestT = _active[_active.Count - 1].SpawnT;
            double sinceNewest = _pace.RealSecondsBetween(newestT, t, fast);
            for (int i = 0; i < _active.Count; i++)
            {
                ReplayDanmakuComment c = _active[i];
                int depth = _active.Count - 1 - i;
                c.Depth = depth;
                c.DepthFrom = depth > 0 ? depth - 1 : 0;
                c.HandoffElapsed = depth > 0 ? sinceNewest : HandoffSec;
            }
        }

        public ReplayDanmakuComment SpawnAdHoc(int actor, string text,
            Func<ReplayDanmakuComment, double> measureWidth)
            => Spawn(0, actor, text, elapsed: 0, measureWidth);

        public static double FontScaleFor(ReplayDanmakuComment c)
        {
            if (c.DisplayChars <= 8) return 1.55;
            if (c.DisplayChars <= 20) return 1.27;
            if (c.DisplayChars <= 34) return 1.09;
            return 0.95;
        }

        public static double VisualDepthAt(ReplayDanmakuComment c)
        {
            if (c.Depth <= 0) return 0;
            double tau = SmoothStep(Clamp01(c.HandoffElapsed / HandoffSec));
            return Lerp(c.DepthFrom, c.Depth, tau);
        }

        public static int EntrySign(ReplayDanmakuComment c) => (c.Actor & 1) == 0 ? -1 : 1;

        public static double CenterXAt(ReplayDanmakuComment c, double elapsed)
        {
            int sign = EntrySign(c);
            double depth = VisualDepthAt(c);
            double targetScale = ScaleForDepth(depth);
            double maxCenter = Math.Max(0, 0.5 - c.WidthRatio * targetScale * 0.5 - EdgeMargin);
            double focus = sign * Math.Min(FocusOffsetX, maxCenter);
            double target = focus + sign * EchoStepX * depth;
            target = sign * Math.Min(Math.Abs(target), maxCenter);

            double x;
            if (elapsed <= 0)
            {
                x = StartX(c);
            }
            else if (elapsed < ReplayChatText.SlideInSec)
            {
                double tau = EaseOutCubic(elapsed / ReplayChatText.SlideInSec);
                x = Lerp(StartX(c), target, tau);
            }
            else
            {
                x = target;
            }

            double outStart = ReplayChatText.SlideInSec + c.DwellSec;
            if (elapsed > outStart)
            {
                double tau = Clamp01((elapsed - outStart) / ReplayChatText.SlideOutSec);
                x += sign * 0.16 * tau * tau;
            }
            return x;
        }

        public static double CenterYRatioAt(ReplayDanmakuComment c, double elapsed)
        {
            double y;
            switch (c.Slot)
            {
                case 0: y = 0.30; break;
                case 1: y = 0.10; break;
                case 2: y = -0.10; break;
                default: y = -0.30; break;
            }
            double depth = VisualDepthAt(c);
            double outward = Math.Sign(y);
            y += outward * 0.025 * depth;

            if (elapsed < ReplayChatText.SlideInSec)
            {
                double tau = SmoothStep(Clamp01(elapsed / ReplayChatText.SlideInSec));
                y += TiltSign(c) * 0.055 * (1 - tau);
            }
            return y;
        }

        public static double OpacityAt(ReplayDanmakuComment c, double elapsed)
        {
            double baseOpacity;
            double dwellEnd = ReplayChatText.SlideInSec + c.DwellSec;
            if (elapsed <= 0) baseOpacity = 0;
            else if (elapsed < ReplayChatText.SlideInSec)
                baseOpacity = SmoothStep(elapsed / ReplayChatText.SlideInSec);
            else if (elapsed <= dwellEnd) baseOpacity = 1;
            else if (elapsed >= c.TotalSec) baseOpacity = 0;
            else baseOpacity = 1 - SmoothStep((elapsed - dwellEnd) / ReplayChatText.SlideOutSec);
            double visualDepth = VisualDepthAt(c);
            double evictionOpacity = visualDepth <= MaxVisibleShots - 1
                ? 1
                : 1 - SmoothStep(visualDepth - (MaxVisibleShots - 1));
            return baseOpacity * evictionOpacity;
        }

        public static double ScaleAt(ReplayDanmakuComment c, double elapsed)
        {
            double start;
            double peak;
            switch (c.Profile)
            {
                case ReplayDanmakuProfile.Impact:
                    start = 0.70;
                    peak = 1.18;
                    break;
                case ReplayDanmakuProfile.Cool:
                    start = 0.88;
                    peak = 1.05;
                    break;
                default:
                    start = 0.79;
                    peak = 1.11;
                    break;
            }

            double arrival = 1;
            if (elapsed <= 0) arrival = start;
            else if (elapsed < ReplayChatText.SlideInSec)
            {
                const double peakAt = 0.70;
                double tau = elapsed / ReplayChatText.SlideInSec;
                arrival = tau < peakAt
                    ? Lerp(start, peak, SmoothStep(tau / peakAt))
                    : Lerp(peak, 1, SmoothStep((tau - peakAt) / (1 - peakAt)));
            }

            double outStart = ReplayChatText.SlideInSec + c.DwellSec;
            if (elapsed > outStart)
            {
                double tau = Clamp01((elapsed - outStart) / ReplayChatText.SlideOutSec);
                arrival *= 1 - 0.10 * SmoothStep(tau);
            }
            return arrival * ScaleForDepth(VisualDepthAt(c));
        }

        public static double RotationDegreesAt(ReplayDanmakuComment c, double elapsed)
        {
            double startAngle;
            switch (c.Profile)
            {
                case ReplayDanmakuProfile.Impact:
                    startAngle = 5.0;
                    break;
                case ReplayDanmakuProfile.Cool:
                    startAngle = 3.0;
                    break;
                default:
                    startAngle = 7.0;
                    break;
            }

            double restingAngle = RestingTiltDegrees(c);
            double arrival = restingAngle;
            if (elapsed < ReplayChatText.SlideInSec)
            {
                arrival = TrajectoryTiltSign(c) * Lerp(startAngle, Math.Abs(restingAngle),
                    SmoothStep(Clamp01(elapsed / ReplayChatText.SlideInSec)));
            }
            else if (elapsed < ReplayChatText.SlideInSec + ArrivalTiltSettleSec)
            {
                LandingShakeAt(c, elapsed, out double shakeX, out _);
                double settle = 1 - SmoothStep((elapsed - ReplayChatText.SlideInSec)
                    / ArrivalTiltSettleSec);
                arrival += shakeX * 0.45 * settle;
            }
            return arrival;
        }

        public static void LandingShakeAt(ReplayDanmakuComment c, double elapsed,
            out double x, out double y)
        {
            double u = elapsed - ReplayChatText.SlideInSec;
            if (u < 0 || u >= LandingShakeSec || VisualDepthAt(c) >= 0.95)
            {
                x = 0;
                y = 0;
                return;
            }

            double remaining = 1 - u / LandingShakeSec;
            double decay = remaining * remaining;
            double amplitude;
            switch (c.Profile)
            {
                case ReplayDanmakuProfile.Impact: amplitude = 1.35; break;
                case ReplayDanmakuProfile.Cool: amplitude = 0.45; break;
                default: amplitude = 1.0; break;
            }
            double phase = ((c.VisualSeed >> 8) & 1023u) / 1023.0 * Math.PI * 2;
            x = Math.Sin(phase + u * Math.PI * 2 * 27) * decay * amplitude;
            y = Math.Sin(phase * 1.7 + u * Math.PI * 2 * 35) * decay * amplitude * 0.65;
        }

        public static ReplayDanmakuIdleStyle IdleStyleFor(ReplayDanmakuComment c)
            => (ReplayDanmakuIdleStyle)(c.VisualSeed % 3u);

        public static double IdleDepthFactor(ReplayDanmakuComment c)
        {
            double depth = VisualDepthAt(c);
            if (depth <= 1) return Lerp(1, 0.65, depth);
            if (depth <= 2) return Lerp(0.65, 0.40, depth - 1);
            if (depth <= 3) return Lerp(0.40, 0.25, depth - 2);
            if (depth <= 4) return Lerp(0.25, 0, depth - 3);
            return 0;
        }

        public static void IdleMotionAt(ReplayDanmakuComment c, double elapsed,
            out double x, out double y)
        {
            x = 0;
            y = 0;
            double idleT = elapsed - ReplayChatText.SlideInSec;
            double remaining = c.TotalSec - elapsed;
            if (idleT <= 0 || remaining <= 0) return;

            double fadeIn = SmoothStep(idleT / IdleFadeInSec);
            double depthGain = IdleDepthFactor(c);
            if (fadeIn <= 0 || depthGain <= 0) return;

            uint seed = c.VisualSeed;
            double phase = (seed & 1023u) / 1023.0 * Math.PI * 2;
            double unitA = ((seed >> 10) & 1023u) / 1023.0;
            double unitB = ((seed >> 20) & 1023u) / 1023.0;
            ReplayDanmakuIdleStyle style = IdleStyleFor(c);

            if (style == ReplayDanmakuIdleStyle.Still) return;
            if (style == ReplayDanmakuIdleStyle.Glide)
            {
                double angle = RestingTiltDegrees(c) * Math.PI / 180;
                double direction = -EntrySign(c);
                double speed = 10 + unitA * 3;
                double distance = speed * idleT * fadeIn * depthGain;
                x = Math.Cos(angle) * direction * distance;
                y = Math.Sin(angle) * direction * distance;
                return;
            }

            double fadeOut = remaining >= ReplayChatText.SlideOutSec
                ? 1 : SmoothStep(remaining / ReplayChatText.SlideOutSec);
            double gain = fadeIn * fadeOut * depthGain;
            double waveX = Math.Sin(phase + idleT * Math.PI * 2 / (1.55 + unitA * 0.30));
            double waveY = Math.Sin(phase * 1.31 + idleT * Math.PI * 2 / (2.05 + unitB * 0.40));
            x = waveX * 7.0 * gain;
            y = waveY * 4.0 * gain;
        }

        public static double AccentOpacityAt(ReplayDanmakuComment c, double elapsed)
        {
            if (elapsed <= 0) return 0;
            double peak;
            switch (c.Profile)
            {
                case ReplayDanmakuProfile.Impact: peak = 0.42; break;
                case ReplayDanmakuProfile.Cool: peak = 0.22; break;
                default: peak = 0.32; break;
            }
            double alpha;
            if (elapsed < ReplayChatText.SlideInSec)
            {
                alpha = peak * SmoothStep(Clamp01(elapsed / 0.06));
            }
            else if (elapsed >= ReplayChatText.SlideInSec + ArrivalTiltSettleSec)
            {
                alpha = 0;
            }
            else
            {
                alpha = peak * (1 - SmoothStep(
                    (elapsed - ReplayChatText.SlideInSec) / ArrivalTiltSettleSec));
            }
            return alpha * (1 - Clamp01(VisualDepthAt(c)));
        }

        public static double AccentWidthScaleAt(ReplayDanmakuComment c, double elapsed)
        {
            double initial;
            double wipeSec;
            switch (c.Profile)
            {
                case ReplayDanmakuProfile.Impact:
                    initial = 0.14;
                    wipeSec = 0.07;
                    break;
                case ReplayDanmakuProfile.Cool:
                    initial = 0.28;
                    wipeSec = 0.14;
                    break;
                default:
                    initial = 0.20;
                    wipeSec = 0.10;
                    break;
            }
            if (elapsed <= 0) return initial;
            if (elapsed >= wipeSec) return 1;
            return Lerp(initial, 1, EaseOutCubic(elapsed / wipeSec));
        }

        public static double AccentCenterOffsetFactorAt(ReplayDanmakuComment c, double elapsed)
            => EntrySign(c) * (1 - AccentWidthScaleAt(c, elapsed)) * 0.5;

        public static uint VisualSeedFor(int actor, double spawnT, int displayChars)
        {
            long millis = (long)Math.Round(spawnT * 1000.0, MidpointRounding.AwayFromZero);
            unchecked
            {
                uint hash = 2166136261u;
                hash = Mix(hash, (uint)actor);
                hash = Mix(hash, (uint)millis);
                hash = Mix(hash, (uint)(millis >> 32));
                hash = Mix(hash, (uint)displayChars);
                return hash;
            }
        }

        public static ReplayDanmakuProfile ProfileForClaim(string text, int displayChars)
        {
            if (displayChars >= 31) return ReplayDanmakuProfile.Cool;
            if ((displayChars > 0 && displayChars <= 8) || EndsWithImpactMark(text))
                return ReplayDanmakuProfile.Impact;
            return ReplayDanmakuProfile.Slash;
        }

        public static uint SpeakerTrajectorySeedFor(int actor) => VisualSeedFor(actor, 0, 0);

        public static int HomeSlotFor(int actor)
            => (int)((SpeakerTrajectorySeedFor(actor) >> 8) & 3u);

        public static int TiltSign(ReplayDanmakuComment c) => HomeSlotFor(c.Actor) <= 1 ? 1 : -1;

        public static int TrajectoryTiltSign(ReplayDanmakuComment c)
            => TiltSign(c) * EntrySign(c);

        public static double RestingTiltDegrees(ReplayDanmakuComment c)
        {
            double unit = ((c.VisualSeed >> 5) & 1023u) / 1023.0;
            return TrajectoryTiltSign(c) * Lerp(2.5, 3.5, unit);
        }

        public static double StartX(ReplayDanmakuComment c)
            => EntrySign(c) * (0.55 + c.WidthRatio * 0.5);

        private ReplayDanmakuComment Spawn(double spawnT, int actor, string text, double elapsed,
            Func<ReplayDanmakuComment, double> measureWidth)
        {
            (string line1, string line2) = ReplayChatText.Wrap(text ?? "");
            var c = new ReplayDanmakuComment
            {
                Actor = actor,
                Text = text ?? "",
                Line1 = line1,
                Line2 = line2,
                DisplayChars = ReplayChatText.DisplayLength(text ?? ""),
                SpawnT = spawnT,
                Elapsed = elapsed,
                Depth = 0,
                DepthFrom = 0,
                HandoffElapsed = HandoffSec,
            };
            c.DwellSec = ReplayChatText.DwellSeconds(c.DisplayChars);
            c.VisualSeed = VisualSeedFor(actor, spawnT, c.DisplayChars);
            c.Profile = ProfileForClaim(c.Text, c.DisplayChars);
            double width = measureWidth != null ? measureWidth(c) : 0.2;
            if (width < 0.01) width = 0.01;
            if (width > 2.0) width = 2.0;
            c.WidthRatio = width;

            DemoteActive();
            c.Slot = AssignSlot(c);
            _active.Add(c);
            return c;
        }

        private void DemoteActive()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                ReplayDanmakuComment c = _active[i];
                if (c.Depth >= MaxVisibleShots) continue;
                c.DepthFrom = VisualDepthAt(c);
                c.Depth++;
                c.HandoffElapsed = 0;
            }
        }

        private int AssignSlot(ReplayDanmakuComment c)
        {
            int preferred = HomeSlotFor(c.Actor);
            for (int order = 0; order < SlotCount; order++)
            {
                int slot = SlotCandidate(preferred, order);
                bool used = false;
                for (int i = 0; i < _active.Count; i++)
                {
                    if (_active[i].Depth < MaxVisibleShots && _active[i].Slot == slot)
                    {
                        used = true;
                        break;
                    }
                }
                if (!used) return slot;
            }
            return preferred;
        }

        private static int SlotCandidate(int preferred, int order)
        {
            switch (preferred)
            {
                case 0: return order;
                case 1: return order == 0 ? 1 : (order == 1 ? 0 : order);
                case 2: return order == 0 ? 2 : (order == 1 ? 3 : 3 - order);
                default: return 3 - order;
            }
        }

        private static double ScaleForDepth(double depth)
        {
            depth = Clamp01(depth / 3) * 3;
            if (depth <= 1) return Lerp(1.0, 0.82, depth);
            if (depth <= 2) return Lerp(0.82, 0.68, depth - 1);
            return Lerp(0.68, 0.56, depth - 2);
        }

        private static double SmoothStep(double t)
        {
            t = Clamp01(t);
            return t * t * (3 - 2 * t);
        }

        private static double EaseOutCubic(double t)
        {
            double rest = 1 - Clamp01(t);
            return 1 - rest * rest * rest;
        }

        private static double Clamp01(double t) => t < 0 ? 0 : (t > 1 ? 1 : t);

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private static bool EndsWithImpactMark(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = text.Length - 1; i >= 0; i--)
            {
                char c = text[i];
                if (char.IsWhiteSpace(c)) continue;
                return c == '!' || c == '?' || c == '！' || c == '？';
            }
            return false;
        }

        private static uint Mix(uint hash, uint value)
        {
            unchecked { return (hash ^ value) * 16777619u; }
        }

        private List<double> _chatTimesCache;

        private List<double> CollectChatTimes()
        {
            if (_chatTimesCache == null)
            {
                _chatTimesCache = new List<double>(_chats.Count);
                for (int i = 0; i < _chats.Count; i++) _chatTimesCache.Add(_chats[i].T);
            }
            return _chatTimesCache;
        }
    }
}
