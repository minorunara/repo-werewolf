using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public enum ScenarioInterrupt : byte
    {
        None = 0,

        Broken = 1,

        AbortConfirm = 2,

        LeashNag = 3,

        ExtractionNag = 4,

        EarlyStart = 5,

        WrongTarget = 6,

        AlreadyOnTarget = 7,

        BlastDenied = 8,

        BlastDeniedMoving = 9,

        MapAside = 10,

        CorpseHint = 11,
    }

    public sealed class ScenarioInterruptState
    {
        private static readonly TextId[] NoLines = new TextId[0];

        public static readonly IReadOnlyList<ScenarioInterrupt> NoticeKinds = new[]
        {
            ScenarioInterrupt.LeashNag,
            ScenarioInterrupt.ExtractionNag,
            ScenarioInterrupt.EarlyStart,
            ScenarioInterrupt.WrongTarget,
            ScenarioInterrupt.AlreadyOnTarget,
            ScenarioInterrupt.BlastDenied,
            ScenarioInterrupt.BlastDeniedMoving,
            ScenarioInterrupt.CorpseHint,
        };

        public static readonly IReadOnlyList<ScenarioInterrupt> PressResponseKinds = new[]
        {
            ScenarioInterrupt.WrongTarget,
            ScenarioInterrupt.AlreadyOnTarget,
            ScenarioInterrupt.BlastDenied,
            ScenarioInterrupt.BlastDeniedMoving,
        };

        private bool _broken;
        private long _brokenExitAtMs;
        private ScenarioBreakKind _brokenKind;
        private IReadOnlyList<TextId> _brokenLines;

        private bool _abortPending;

        private readonly long[] _noticeUntilMs = new long[NoticeKinds.Count];
        private readonly bool[] _deferred = new bool[NoticeKinds.Count];
        private readonly IReadOnlyList<TextId>[] _noticeLines =
            new IReadOnlyList<TextId>[NoticeKinds.Count];
        private readonly int[] _noticeArgs = new int[NoticeKinds.Count];

        public bool Broken => _broken;

        public long BrokenExitAtMs => _brokenExitAtMs;

        public ScenarioBreakKind BrokenKind => _brokenKind;

        public IReadOnlyList<TextId> BrokenLines => _brokenLines ?? NoLines;

        public bool AbortPending => _abortPending;

        public static long NoticeDurationMs(ScenarioInterrupt kind)
        {
            switch (kind)
            {
                case ScenarioInterrupt.LeashNag: return 4000L;
                case ScenarioInterrupt.ExtractionNag: return 5000L;
                case ScenarioInterrupt.EarlyStart: return 4000L;
                case ScenarioInterrupt.WrongTarget: return 6000L;
                case ScenarioInterrupt.AlreadyOnTarget: return 5000L;
                case ScenarioInterrupt.BlastDenied: return 6000L;
                case ScenarioInterrupt.BlastDeniedMoving: return 6000L;
                case ScenarioInterrupt.CorpseHint: return 6000L;
                default: return 0L;
            }
        }

        public static bool IsNotice(ScenarioInterrupt kind) => IndexOfNotice(kind) >= 0;

        public static bool IsDismissable(ScenarioInterrupt kind)
            => kind == ScenarioInterrupt.AbortConfirm
               || (kind != ScenarioInterrupt.LeashNag && IndexOfNotice(kind) >= 0);

        public ScenarioInterrupt Resolve(long now)
        {
            if (_broken) return ScenarioInterrupt.Broken;
            if (_abortPending) return ScenarioInterrupt.AbortConfirm;
            for (int i = 0; i < NoticeKinds.Count; i++)
            {
                if (now < _noticeUntilMs[i]) return NoticeKinds[i];
            }
            return ScenarioInterrupt.None;
        }

        public bool Break(ScenarioBreakKind kind, IReadOnlyList<TextId> lines, long exitAtMs)
        {
            if (_broken) return false;

            _broken = true;
            _brokenExitAtMs = exitAtMs;
            _brokenKind = kind;
            _brokenLines = lines != null && lines.Count > 0 ? lines : null;
            _abortPending = false;
            ClearNotices();
            return true;
        }

        public void RequestAbort() => _abortPending = true;

        public void Notice(ScenarioInterrupt kind, long untilMs,
                           IReadOnlyList<TextId> lines = null, int arg = 0)
        {
            int i = IndexOfNotice(kind);
            if (i < 0) return;
            _noticeUntilMs[i] = untilMs;
            _noticeLines[i] = lines;
            _noticeArgs[i] = arg;
        }

        public void Raise(ScenarioInterrupt kind, long now,
                          IReadOnlyList<TextId> lines = null, int arg = 0)
            => Notice(kind, now + NoticeDurationMs(kind), lines, arg);

        public void Defer(ScenarioInterrupt kind, IReadOnlyList<TextId> lines = null, int arg = 0)
        {
            int i = IndexOfNotice(kind);
            if (i < 0) return;
            _deferred[i] = true;
            _noticeLines[i] = lines;
            _noticeArgs[i] = arg;
        }

        public void StartDeferred(long now)
        {
            for (int i = 0; i < NoticeKinds.Count; i++)
            {
                if (!_deferred[i]) continue;
                _deferred[i] = false;
                _noticeUntilMs[i] = now + NoticeDurationMs(NoticeKinds[i]);
            }
        }

        public IReadOnlyList<TextId> NoticeLines(ScenarioInterrupt kind)
        {
            int i = IndexOfNotice(kind);
            return i >= 0 && _noticeLines[i] != null ? _noticeLines[i] : NoLines;
        }

        public int NoticeArg(ScenarioInterrupt kind)
        {
            int i = IndexOfNotice(kind);
            return i >= 0 ? _noticeArgs[i] : 0;
        }

        public void DropPressResponses()
        {
            for (int k = 0; k < PressResponseKinds.Count; k++)
            {
                int i = IndexOfNotice(PressResponseKinds[k]);
                if (i >= 0) _noticeUntilMs[i] = 0;
            }
        }

        public bool Dismiss(ScenarioInterrupt kind)
        {
            if (!IsDismissable(kind)) return false;
            if (kind == ScenarioInterrupt.AbortConfirm)
            {
                bool abort = _abortPending;
                _abortPending = false;
                return abort;
            }

            int i = IndexOfNotice(kind);
            bool shown = _noticeUntilMs[i] > 0;
            _noticeUntilMs[i] = 0;
            return shown;
        }

        public void Reset()
        {
            _broken = false;
            _brokenExitAtMs = 0;
            _brokenKind = default;
            _brokenLines = null;
            _abortPending = false;
            ClearNotices();
        }

        private void ClearNotices()
        {
            for (int i = 0; i < NoticeKinds.Count; i++)
            {
                _noticeUntilMs[i] = 0;
                _deferred[i] = false;
                _noticeLines[i] = null;
                _noticeArgs[i] = 0;
            }
        }

        private static int IndexOfNotice(ScenarioInterrupt kind)
        {
            for (int i = 0; i < NoticeKinds.Count; i++)
            {
                if (NoticeKinds[i] == kind) return i;
            }
            return -1;
        }
    }
}
