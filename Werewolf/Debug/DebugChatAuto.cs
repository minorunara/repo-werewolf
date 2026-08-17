namespace Werewolf.Debugging
{
    public sealed class DebugChatAuto
    {
        public readonly struct Step
        {
            public Step(int gapMs, int textIndex, int speakerAdvance)
            {
                GapMs = gapMs;
                TextIndex = textIndex;
                SpeakerAdvance = speakerAdvance;
            }

            public int GapMs { get; }

            public int TextIndex { get; }

            public int SpeakerAdvance { get; }
        }

        public const int MaxPostsPerFrame = 4;

        public static readonly string[] Texts =
        {
            "あ",
            "了解",
            "3番が怪しいと思う",
            "こっちは金庫を取ったから先に抽出ポイントへ運んでおくね",
            "さっき3番が抽出ポイントの裏で誰かと一緒にいたのを見たんだよ",
            "いま考えると最初の会議で5番がずっと黙っていたのはかなり怪しい",
            "私は今日最初からずっと2番と一緒に行動していたので、2番が狼でないことは証明できます",
            "みんな落ち着いて聞いてほしいんだけど、さっきの抽出のときに誰が誰と一緒だったかを順番に確認していこう",
            "そろそろ結論を出したいので、まだ一度も発言していない人は今のうちに自分の行動を全部話してほしいんだけどどうかな",
            "<color=#ff0000>タグは無効化される</color>",
            "改行を\n含む\tタブ入り",
            "\U0001F44D\U0001F3FB 家族\U0001F468\u200D\U0001F469\u200D\U0001F467 が証人",
        };

        private static readonly Step[] Script =
        {
            new Step(0, 0, 0),
            new Step(1600, 4, 1),
            new Step(2400, 3, 1),
            new Step(200, 1, 1),
            new Step(0, 2, 1),
            new Step(150, 5, 1),
            new Step(6500, 6, 1),
            new Step(1800, 7, 0),
            new Step(2600, 9, 1),
            new Step(1400, 10, 1),
            new Step(7000, 11, 1),
            new Step(1200, 8, 1),
            new Step(300, 0, 1),
            new Step(2000, 2, 2),
            new Step(5200, 3, 1),
            new Step(4800, 4, 1),
        };

        private bool _active;
        private int _index;
        private int _slot;
        private int _remaining = -1;
        private int _posted;
        private long _nextDueMs;

        public static int StepCount => Script.Length;

        public static Step StepAt(int index)
        {
            int i = index % Script.Length;
            if (i < 0) i += Script.Length;
            return Script[i];
        }

        public bool Active => _active;

        public int Posted => _posted;

        public int Remaining => _remaining;

        public int NextStep => _index;

        public void Start(long nowMs, int count)
        {
            _active = true;
            _index = 0;
            _slot = 0;
            _posted = 0;
            _remaining = count > 0 ? count : -1;
            _nextDueMs = nowMs + StepAt(0).GapMs;
        }

        public void Stop() => _active = false;

        public bool IsDue(long nowMs) => _active && nowMs >= _nextDueMs;

        public bool TryTakeDue(long nowMs, int speakerCount, out int slot, out string text)
        {
            slot = 0;
            text = null;
            if (!_active || speakerCount <= 0 || nowMs < _nextDueMs) return false;

            Step step = StepAt(_index);
            _slot += step.SpeakerAdvance;
            slot = _slot % speakerCount;
            if (slot < 0) slot += speakerCount;
            text = Texts[step.TextIndex];

            _index++;
            _posted++;
            _nextDueMs = nowMs + StepAt(_index).GapMs;
            if (_remaining > 0 && --_remaining == 0) _active = false;
            return true;
        }
    }
}
