using System;

namespace Werewolf.Core
{
    public enum ChatBlockKind : byte
    {
        Speaker = 0,

        Bubble = 1,

        Vote = 2,
    }

    public readonly struct ChatBlockSize
    {
        public ChatBlockSize(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public float Width { get; }
        public float Height { get; }
    }

    public readonly struct ChatLayoutMetrics
    {
        public ChatLayoutMetrics(float speakerWidth, float speakerHeight,
                                 float voteWidth, float voteHeight,
                                 float blockGap, float groupGap)
        {
            SpeakerWidth = speakerWidth;
            SpeakerHeight = speakerHeight;
            VoteWidth = voteWidth;
            VoteHeight = voteHeight;
            BlockGap = blockGap;
            GroupGap = groupGap;
        }

        public float SpeakerWidth { get; }
        public float SpeakerHeight { get; }
        public float VoteWidth { get; }
        public float VoteHeight { get; }

        public float BlockGap { get; }

        public float GroupGap { get; }
    }

    public readonly struct ChatLayoutBlock
    {
        public ChatLayoutBlock(long entrySeq, ChatBlockKind kind, float top, float width, float height)
        {
            EntrySeq = entrySeq;
            Kind = kind;
            Top = top;
            Width = width;
            Height = height;
        }

        public long EntrySeq { get; }

        public ChatBlockKind Kind { get; }

        public float Top { get; }

        public float Width { get; }
        public float Height { get; }

        public float Bottom => Top + Height;
    }

    public sealed class ChatLayout
    {
        private ChatLayoutBlock[] _blocks = new ChatLayoutBlock[16];
        private int _blockHead;
        private int _blockCount;
        private readonly ChatLayoutMetrics _metrics;

        private long _syncedAppended;
        private long _syncedDropped;
        private int _lastActor = int.MinValue;

        private float _cursor;
        private float _origin;

        public ChatLayout(ChatLayoutMetrics metrics)
        {
            _metrics = metrics;
        }

        public int Count => _blockCount;

        public ChatLayoutBlock this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_blockCount) throw new ArgumentOutOfRangeException(nameof(index));
                return _blocks[(_blockHead + index) % _blocks.Length];
            }
        }

        public float TotalHeight => _cursor - _origin;

        public float Origin => _origin;

        public int Epoch { get; private set; }

        public int Version { get; private set; }

        public void Sync(MeetingChatLog log, Func<ChatLogEntry, ChatBlockSize> measureBubble)
        {
            if (log == null || measureBubble == null) return;
            if (log.DroppedTotal != _syncedDropped) DropLeading(log.DroppedTotal);
            if (log.AppendedTotal != _syncedAppended) AppendTail(log, measureBubble);
        }

        public void Reset()
        {
            _blockHead = 0;
            _blockCount = 0;
            _syncedAppended = 0L;
            _syncedDropped = 0L;
            _lastActor = int.MinValue;
            _cursor = 0f;
            _origin = 0f;
            Epoch++;
            Version++;
        }

        public float ContentTop(int index) => this[index].Top - _origin;

        public bool IsGroupHead(int index) => index == 0 || this[index - 1].Kind == ChatBlockKind.Speaker;

        public void GetVisibleRange(float from, float to, out int first, out int end)
        {
            first = 0;
            end = 0;
            if (_blockCount == 0 || to <= from) return;

            float top = from + _origin;
            float bottom = to + _origin;

            int lo = 0;
            int hi = _blockCount;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (this[mid].Bottom <= top) lo = mid + 1;
                else hi = mid;
            }
            first = lo;

            hi = _blockCount;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (this[mid].Top < bottom) lo = mid + 1;
                else hi = mid;
            }
            end = lo;
        }

        private void DropLeading(long dropped)
        {
            _syncedDropped = dropped;

            int removed = 0;
            while (removed < _blockCount && this[removed].EntrySeq < dropped) removed++;
            if (removed == 0) return;

            RemoveFirst(removed);
            if (_blockCount == 0)
            {
                _cursor = 0f;
                _origin = 0f;
                _lastActor = int.MinValue;
            }
            else
            {
                ChatLayoutBlock first = this[0];
                if (first.Kind == ChatBlockKind.Bubble)
                {
                    float headerTop = first.Top - (_metrics.SpeakerHeight + _metrics.BlockGap);
                    AddFirst(new ChatLayoutBlock(first.EntrySeq, ChatBlockKind.Speaker, headerTop,
                                                 _metrics.SpeakerWidth, _metrics.SpeakerHeight));
                }
                _origin = this[0].Top;
            }
            Epoch++;
            Version++;
        }

        private void AppendTail(MeetingChatLog log, Func<ChatLogEntry, ChatBlockSize> measureBubble)
        {
            long from = _syncedAppended > log.DroppedTotal ? _syncedAppended : log.DroppedTotal;
            for (int i = (int)(from - log.DroppedTotal); i < log.Count; i++)
            {
                ChatLogEntry entry = log.Entries[i];
                long seq = log.DroppedTotal + i;

                if (entry.Kind == ChatEntryKind.Vote)
                {
                    Add(seq, ChatBlockKind.Vote, _metrics.VoteWidth, _metrics.VoteHeight, group: true);
                    _lastActor = int.MinValue;
                    continue;
                }

                if (entry.Actor != _lastActor)
                {
                    Add(seq, ChatBlockKind.Speaker, _metrics.SpeakerWidth, _metrics.SpeakerHeight, group: true);
                }
                ChatBlockSize size = measureBubble(entry);
                Add(seq, ChatBlockKind.Bubble, size.Width, size.Height, group: false);
                _lastActor = entry.Actor;
            }

            _syncedAppended = log.AppendedTotal;
            Version++;
        }

        private void Add(long seq, ChatBlockKind kind, float width, float height, bool group)
        {
            if (group && _blockCount > 0) _cursor += _metrics.GroupGap;
            AddLast(new ChatLayoutBlock(seq, kind, _cursor, width, height));
            _cursor += height + _metrics.BlockGap;
        }

        private void AddFirst(ChatLayoutBlock block)
        {
            EnsureCapacity();
            _blockHead = (_blockHead - 1 + _blocks.Length) % _blocks.Length;
            _blocks[_blockHead] = block;
            _blockCount++;
        }

        private void AddLast(ChatLayoutBlock block)
        {
            EnsureCapacity();
            int tail = (_blockHead + _blockCount) % _blocks.Length;
            _blocks[tail] = block;
            _blockCount++;
        }

        private void RemoveFirst(int count)
        {
            if (count <= 0) return;
            if (count > _blockCount) throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i++)
            {
                _blocks[(_blockHead + i) % _blocks.Length] = default;
            }
            _blockHead = (_blockHead + count) % _blocks.Length;
            _blockCount -= count;
            if (_blockCount == 0) _blockHead = 0;
        }

        private void EnsureCapacity()
        {
            if (_blockCount < _blocks.Length) return;

            var expanded = new ChatLayoutBlock[_blocks.Length * 2];
            for (int i = 0; i < _blockCount; i++) expanded[i] = this[i];
            _blocks = expanded;
            _blockHead = 0;
        }
    }
}
