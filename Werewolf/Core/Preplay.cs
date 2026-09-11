using System.Collections.Generic;

namespace Werewolf.Core
{
    public static class Preplay
    {
        public const int RoleRevealDurationMs = 10_000;

        public const int PageBoundaryMs = 5_000;
    }

    public readonly struct ParticipantLoadState
    {
        public ParticipantLoadState(int actorNumber, bool completed)
        {
            ActorNumber = actorNumber;
            Completed = completed;
        }

        public int ActorNumber { get; }
        public bool Completed { get; }
    }

    public static class PreplayRosterGate
    {
        public static bool AllCurrentParticipantsLoaded(
            IReadOnlyCollection<int> currentActors,
            IReadOnlyList<ParticipantLoadState> avatars)
        {
            if (currentActors == null || currentActors.Count == 0 || avatars == null) return false;

            var expected = new HashSet<int>();
            foreach (int actor in currentActors)
            {
                if (actor > 0) expected.Add(actor);
            }
            if (expected.Count != currentActors.Count) return false;

            var loaded = new HashSet<int>();
            foreach (ParticipantLoadState avatar in avatars)
            {
                if (!expected.Contains(avatar.ActorNumber)) continue;
                if (!avatar.Completed) return false;
                loaded.Add(avatar.ActorNumber);
            }
            return loaded.SetEquals(expected);
        }
    }

    public readonly struct RoleRevealFrame
    {
        public int PageIndex { get; }
        public bool ShowBack { get; }
        public float BackdropAlpha { get; }
        public float CardDropRatio { get; }
        public float CardFlipScaleX { get; }
        public float TextAlpha { get; }
        public float BodyAlpha { get; }
        public float StowRatio { get; }

        internal RoleRevealFrame(int pageIndex, bool showBack, float backdropAlpha,
            float cardDropRatio, float cardFlipScaleX, float textAlpha, float bodyAlpha,
            float stowRatio)
        {
            PageIndex = pageIndex;
            ShowBack = showBack;
            BackdropAlpha = backdropAlpha;
            CardDropRatio = cardDropRatio;
            CardFlipScaleX = cardFlipScaleX;
            TextAlpha = textAlpha;
            BodyAlpha = bodyAlpha;
            StowRatio = stowRatio;
        }
    }

    public static class RoleRevealTimeline
    {
        public const int DropEndMs = 650;
        public const int FlipStartMs = 1_050;
        public const int FlipMidMs = 1_300;
        public const int FlipEndMs = 1_550;
        public const int TextInEndMs = 1_650;
        public const int CrossfadeHalfMs = 180;
        public const int StowStartMs = 9_000;
        public const int TextOutEndMs = 9_350;

        public static RoleRevealFrame At(long elapsedMs)
        {
            float backdrop = elapsedMs < 250 ? Ratio(elapsedMs, 0, 250) : 1f;
            float drop = Smooth(Ratio(elapsedMs, 0, DropEndMs));
            bool showBack = elapsedMs < FlipMidMs;
            float flipX;
            if (elapsedMs < FlipStartMs) flipX = 1f;
            else if (elapsedMs < FlipMidMs) flipX = 1f - 0.98f * Smooth(Ratio(elapsedMs, FlipStartMs, FlipMidMs));
            else if (elapsedMs < FlipEndMs) flipX = 0.02f + 0.98f * Smooth(Ratio(elapsedMs, FlipMidMs, FlipEndMs));
            else flipX = 1f;

            float text = Smooth(Ratio(elapsedMs, FlipMidMs, TextInEndMs));
            float stow = Smooth(Ratio(elapsedMs, StowStartMs, Preplay.RoleRevealDurationMs));
            if (elapsedMs >= StowStartMs)
                text *= 1f - Smooth(Ratio(elapsedMs, StowStartMs, TextOutEndMs));

            int page = elapsedMs < Preplay.PageBoundaryMs ? 0 : 1;
            long crossDistance = elapsedMs - Preplay.PageBoundaryMs;
            if (crossDistance < 0) crossDistance = -crossDistance;
            float body = crossDistance >= CrossfadeHalfMs
                ? 1f
                : (float)crossDistance / CrossfadeHalfMs;
            body *= text;

            if (elapsedMs >= 9_500)
                backdrop *= 1f - Smooth(Ratio(elapsedMs, 9_500, Preplay.RoleRevealDurationMs));

            return new RoleRevealFrame(page, showBack, Clamp01(backdrop), drop,
                Clamp01(flipX), Clamp01(text), Clamp01(body), stow);
        }

        private static float Ratio(long value, long from, long to)
        {
            if (to <= from) return value >= to ? 1f : 0f;
            return Clamp01((float)(value - from) / (to - from));
        }

        private static float Smooth(float value) => value * value * (3f - 2f * value);
        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }

    public enum PreplayAbortReason : byte
    {
        None = 0,

        TooFewPlayersAfterLoad = 1,

        InvalidConfigAfterLoad = 2,

        RoleRevealRosterInvalid = 3,
    }

    public enum BeginPlayResult : byte
    {
        NotReady = 0,
        Started = 1,
        Aborted = 2,
    }
}
