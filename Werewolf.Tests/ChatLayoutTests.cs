using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ChatLayoutTests
    {
        private const float SpeakerHeight = 40f;
        private const float VoteHeight = 36f;
        private const float BlockGap = 5f;
        private const float GroupGap = 10f;
        private const float BubbleHeight = 30f;

        private static ChatLayout NewLayout()
            => new ChatLayout(new ChatLayoutMetrics(210f, SpeakerHeight, 314f, VoteHeight, BlockGap, GroupGap));

        private static ChatBlockSize FixedSize(ChatLogEntry entry) => new ChatBlockSize(200f, BubbleHeight);

        private static MeetingChatLog LogWith(params (int actor, string text)[] messages)
        {
            var log = new MeetingChatLog();
            foreach ((int actor, string text) in messages)
            {
                log.Append(actor, $"P{actor}", text, ChatSpeaker.Alive);
            }
            return log;
        }

        [Fact]
        public void Sync_FirstMessage_EmitsSpeakerHeaderThenBubble()
        {
            ChatLayout layout = NewLayout();

            layout.Sync(LogWith((1, "hi")), FixedSize);

            Assert.Equal(2, layout.Count);
            Assert.Equal(ChatBlockKind.Speaker, layout[0].Kind);
            Assert.Equal(ChatBlockKind.Bubble, layout[1].Kind);
        }

        [Fact]
        public void Sync_SameSpeakerRun_EmitsHeaderOnlyOnce()
        {
            ChatLayout layout = NewLayout();

            layout.Sync(LogWith((1, "a"), (1, "b"), (1, "c")), FixedSize);

            Assert.Equal(4, layout.Count);
            Assert.Equal(ChatBlockKind.Speaker, layout[0].Kind);
            for (int i = 1; i < 4; i++) Assert.Equal(ChatBlockKind.Bubble, layout[i].Kind);
        }

        [Fact]
        public void Sync_SpeakerChange_StartsNewHeader()
        {
            ChatLayout layout = NewLayout();

            layout.Sync(LogWith((1, "a"), (2, "b")), FixedSize);

            Assert.Equal(4, layout.Count);
            Assert.Equal(ChatBlockKind.Speaker, layout[2].Kind);
        }

        [Fact]
        public void Sync_VoteLine_BreaksTheSpeakerRun()
        {
            var log = new MeetingChatLog();
            log.Append(1, "P1", "a", ChatSpeaker.Alive);
            log.AppendVote(1, "P1", "投票しました。");
            log.Append(1, "P1", "b", ChatSpeaker.Alive);

            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            Assert.Equal(5, layout.Count);
            Assert.Equal(ChatBlockKind.Vote, layout[2].Kind);
            Assert.Equal(ChatBlockKind.Speaker, layout[3].Kind);
        }

        [Fact]
        public void Sync_SystemEntry_EmitsItsOwnHeaderEvenRightAfterAVoteLine()
        {
            var log = new MeetingChatLog();
            log.AppendVote(1, "P1", "投票しました。");
            log.AppendSystem("Taxman", "ここまでの経過", "死亡: なし");

            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            Assert.Equal(3, layout.Count);
            Assert.Equal(ChatBlockKind.Vote, layout[0].Kind);
            Assert.Equal(ChatBlockKind.Speaker, layout[1].Kind);
            Assert.Equal(ChatBlockKind.Bubble, layout[2].Kind);
        }

        [Fact]
        public void Sync_ConsecutiveSystemEntries_ShareOneHeader()
        {
            var log = new MeetingChatLog();
            log.AppendSystem("Taxman", "ここまでの経過", "死亡: なし");
            log.AppendSystem("Taxman", "前回の組分け", "【A】1番\n【B】2番");
            log.Append(1, "P1", "hi", ChatSpeaker.Alive);

            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            Assert.Equal(5, layout.Count);
            Assert.Equal(ChatBlockKind.Speaker, layout[0].Kind);
            Assert.Equal(ChatBlockKind.Bubble, layout[1].Kind);
            Assert.Equal(ChatBlockKind.Bubble, layout[2].Kind);
            Assert.Equal(ChatBlockKind.Speaker, layout[3].Kind);
        }

        [Fact]
        public void IsGroupHead_MarksOnlyTheBubbleUnderTheSpeakerHeader()
        {
            var log = new MeetingChatLog();
            log.Append(1, "P1", "a", ChatSpeaker.Alive);
            log.Append(1, "P1", "b", ChatSpeaker.Alive);
            log.Append(2, "P2", "c", ChatSpeaker.Alive);

            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            Assert.True(layout.IsGroupHead(1));
            Assert.False(layout.IsGroupHead(2));
            Assert.True(layout.IsGroupHead(4));
        }

        [Fact]
        public void IsGroupHead_ConsecutiveSystemEntries_OnlyTheFirstIsHead()
        {
            var log = new MeetingChatLog();
            log.AppendSystem("Taxman", "ここまでの経過", "死亡: なし");
            log.AppendSystem("Taxman", "前回の組分け", "【A】1番\n【B】2番");

            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            Assert.True(layout.IsGroupHead(1));
            Assert.False(layout.IsGroupHead(2));
        }

        [Fact]
        public void IsGroupHead_AfterLeadingTrim_FollowsTheSyntheticHeader()
        {
            var log = new MeetingChatLog();
            for (int i = 0; i < MeetingChatLog.MaxEntries; i++)
            {
                log.Append(1, "P1", $"msg{i}", ChatSpeaker.Alive);
            }
            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            log.Append(1, "P1", "overflow", ChatSpeaker.Alive);
            layout.Sync(log, FixedSize);

            Assert.Equal(ChatBlockKind.Speaker, layout[0].Kind);
            Assert.True(layout.IsGroupHead(1));
            Assert.False(layout.IsGroupHead(2));
        }

        [Fact]
        public void Sync_BlocksNeverOverlapAndStayOrdered()
        {
            var log = new MeetingChatLog();
            for (int i = 0; i < 20; i++)
            {
                if (i % 5 == 4) log.AppendVote(i, $"P{i}", "投票しました。");
                else log.Append(i % 3, $"P{i % 3}", $"msg{i}", ChatSpeaker.Alive);
            }

            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            for (int i = 1; i < layout.Count; i++)
            {
                Assert.True(layout[i].Top >= layout[i - 1].Bottom,
                    $"block {i} overlaps block {i - 1}");
            }
        }

        [Fact]
        public void Sync_FirstBlockSitsAtContentOrigin()
        {
            ChatLayout layout = NewLayout();

            layout.Sync(LogWith((1, "a")), FixedSize);

            Assert.Equal(0f, layout.ContentTop(0), 3);
        }

        [Fact]
        public void Sync_TotalHeightAccountsForGapsAndBlocks()
        {
            ChatLayout layout = NewLayout();

            layout.Sync(LogWith((1, "a"), (2, "b")), FixedSize);

            float expected = (SpeakerHeight + BlockGap) + (BubbleHeight + BlockGap)
                             + GroupGap
                             + (SpeakerHeight + BlockGap) + (BubbleHeight + BlockGap);
            Assert.Equal(expected, layout.TotalHeight, 3);
        }

        [Fact]
        public void Sync_IsIncremental_MeasuresEachEntryExactlyOnce()
        {
            var measured = new List<string>();
            ChatBlockSize Counting(ChatLogEntry e)
            {
                measured.Add(e.Text);
                return new ChatBlockSize(200f, BubbleHeight);
            }

            var log = new MeetingChatLog();
            ChatLayout layout = NewLayout();

            log.Append(1, "P1", "a", ChatSpeaker.Alive);
            layout.Sync(log, Counting);
            log.Append(1, "P1", "b", ChatSpeaker.Alive);
            layout.Sync(log, Counting);
            layout.Sync(log, Counting);

            Assert.Equal(new[] { "a", "b" }, measured);
        }

        [Fact]
        public void Sync_AppendingDoesNotMoveExistingBlocks()
        {
            var log = new MeetingChatLog();
            ChatLayout layout = NewLayout();
            log.Append(1, "P1", "a", ChatSpeaker.Alive);
            layout.Sync(log, FixedSize);
            float topBefore = layout[1].Top;
            int epochBefore = layout.Epoch;

            log.Append(2, "P2", "b", ChatSpeaker.Alive);
            layout.Sync(log, FixedSize);

            Assert.Equal(topBefore, layout[1].Top, 3);
            Assert.Equal(epochBefore, layout.Epoch);
        }

        [Fact]
        public void Sync_LeadingTrim_DropsOnlyTheOldestBlocksAndBumpsEpoch()
        {
            var log = new MeetingChatLog();
            for (int i = 0; i < MeetingChatLog.MaxEntries; i++)
            {
                log.Append(1, "P1", $"msg{i}", ChatSpeaker.Alive);
            }
            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);
            int countBefore = layout.Count;
            int epochBefore = layout.Epoch;

            log.Append(1, "P1", "overflow", ChatSpeaker.Alive);
            layout.Sync(log, FixedSize);

            Assert.Equal(countBefore, layout.Count);
            Assert.True(layout.Epoch > epochBefore);
            Assert.Equal(0f, layout.ContentTop(0), 3);
            Assert.Equal(ChatBlockKind.Speaker, layout[0].Kind);
            Assert.Equal(log.DroppedTotal, layout[0].EntrySeq);
            Assert.Equal(ChatBlockKind.Bubble, layout[1].Kind);
            Assert.Equal(log.DroppedTotal, layout[1].EntrySeq);
        }

        [Fact]
        public void Sync_RepeatedLeadingTrim_PreservesHeaderAcrossCircularWrap()
        {
            var log = new MeetingChatLog();
            for (int i = 0; i < MeetingChatLog.MaxEntries; i++)
            {
                log.Append(1, "P1", $"initial{i}", ChatSpeaker.Alive);
            }
            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            for (int i = 0; i < MeetingChatLog.MaxEntries + 17; i++)
            {
                log.Append(1, "P1", $"overflow{i}", ChatSpeaker.Alive);
                layout.Sync(log, FixedSize);
            }

            Assert.Equal(MeetingChatLog.MaxEntries + 1, layout.Count);
            Assert.Equal(ChatBlockKind.Speaker, layout[0].Kind);
            Assert.Equal(log.DroppedTotal, layout[0].EntrySeq);
            Assert.Equal(ChatBlockKind.Bubble, layout[1].Kind);
            Assert.Equal(log.DroppedTotal, layout[1].EntrySeq);
            Assert.Equal(log.AppendedTotal - 1L, layout[layout.Count - 1].EntrySeq);
        }

        [Fact]
        public void Sync_LogCleared_EmptiesLayoutAndKeepsFollowingAppends()
        {
            var log = new MeetingChatLog();
            log.Append(1, "P1", "a", ChatSpeaker.Alive);
            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            log.Clear();
            layout.Sync(log, FixedSize);
            Assert.Equal(0, layout.Count);
            Assert.Equal(0f, layout.TotalHeight, 3);

            log.Append(1, "P1", "b", ChatSpeaker.Alive);
            layout.Sync(log, FixedSize);

            Assert.Equal(2, layout.Count);
            Assert.Equal(ChatBlockKind.Speaker, layout[0].Kind);
            Assert.Equal(0f, layout.ContentTop(0), 3);
        }

        [Fact]
        public void Sync_StartingMidBuffer_SkipsAlreadyDroppedEntries()
        {
            var log = new MeetingChatLog();
            for (int i = 0; i < MeetingChatLog.MaxEntries + 5; i++)
            {
                log.Append(1, "P1", $"msg{i}", ChatSpeaker.Alive);
            }

            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            Assert.Equal(log.Count + 1, layout.Count);
        }

        [Fact]
        public void GetVisibleRange_ReturnsOnlyBlocksTouchingTheWindow()
        {
            ChatLayout layout = NewLayout();
            layout.Sync(LogWith((1, "a"), (1, "b"), (1, "c"), (1, "d")), FixedSize);

            layout.GetVisibleRange(layout.ContentTop(2), layout.ContentTop(3), out int first, out int end);

            Assert.Equal(2, first);
            Assert.Equal(3, end);
        }

        [Fact]
        public void GetVisibleRange_IncludesBlocksStraddlingTheEdges()
        {
            ChatLayout layout = NewLayout();
            layout.Sync(LogWith((1, "a"), (1, "b"), (1, "c"), (1, "d")), FixedSize);

            float from = layout.ContentTop(1) + 1f;
            float to = layout.ContentTop(3) + 1f;
            layout.GetVisibleRange(from, to, out int first, out int end);

            Assert.Equal(1, first);
            Assert.Equal(4, end);
        }

        [Fact]
        public void GetVisibleRange_WindowAboveOrBelowContent_IsEmpty()
        {
            ChatLayout layout = NewLayout();
            layout.Sync(LogWith((1, "a"), (1, "b")), FixedSize);

            layout.GetVisibleRange(-500f, -400f, out int firstAbove, out int endAbove);
            layout.GetVisibleRange(10000f, 10100f, out int firstBelow, out int endBelow);

            Assert.Equal(0, endAbove - firstAbove);
            Assert.Equal(0, endBelow - firstBelow);
        }

        [Fact]
        public void GetVisibleRange_EmptyLayout_IsEmpty()
        {
            ChatLayout layout = NewLayout();

            layout.GetVisibleRange(0f, 1000f, out int first, out int end);

            Assert.Equal(0, first);
            Assert.Equal(0, end);
        }

        [Fact]
        public void GetVisibleRange_ScalesToTenThousandEntries()
        {
            var log = new MeetingChatLog();
            for (int i = 0; i < MeetingChatLog.MaxEntries; i++)
            {
                log.Append(i % 4, $"P{i % 4}", $"msg{i}", ChatSpeaker.Alive);
            }
            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            layout.GetVisibleRange(layout.TotalHeight - 690f, layout.TotalHeight, out int first, out int end);

            Assert.Equal(MeetingChatLog.MaxEntries, log.Count);
            Assert.True(layout.Count > MeetingChatLog.MaxEntries, "上限件数ぶんのブロックが積まれている");
            Assert.InRange(end - first, 1, 64);
            Assert.Equal(layout.Count, end);
        }

        [Fact]
        public void Reset_ClearsEverythingAndResyncsFromScratch()
        {
            var log = new MeetingChatLog();
            log.Append(1, "P1", "a", ChatSpeaker.Alive);
            ChatLayout layout = NewLayout();
            layout.Sync(log, FixedSize);

            layout.Reset();
            Assert.Equal(0, layout.Count);

            layout.Sync(log, FixedSize);

            Assert.Equal(2, layout.Count);
            Assert.Equal(0f, layout.ContentTop(0), 3);
        }

        [Fact]
        public void Sync_NullArguments_AreIgnored()
        {
            ChatLayout layout = NewLayout();

            layout.Sync(null, FixedSize);
            layout.Sync(new MeetingChatLog(), null);

            Assert.Equal(0, layout.Count);
        }
    }
}
