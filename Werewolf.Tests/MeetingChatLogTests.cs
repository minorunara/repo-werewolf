using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MeetingChatLogTests
    {
        [Fact]
        public void Append_StoresEntryAndBumpsRevision()
        {
            var log = new MeetingChatLog();
            int before = log.Revision;

            Assert.True(log.Append(3, "Alice", "こんばんは", ChatSpeaker.Alive));

            Assert.Single(log);
            Assert.Equal(3, log.Entries[0].Actor);
            Assert.Equal("Alice", log.Entries[0].Name);
            Assert.Equal("こんばんは", log.Entries[0].Text);
            Assert.Equal(ChatSpeaker.Alive, log.Entries[0].Speaker);
            Assert.True(log.Revision > before);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\n\r\t")]
        public void Append_EmptyBody_IsRejected(string body)
        {
            var log = new MeetingChatLog();

            Assert.False(log.Append(1, "Alice", body, ChatSpeaker.Alive));
            Assert.Empty(log);
            Assert.Equal(0, log.Revision);
        }

        [Fact]
        public void Append_EmptyName_FallsBackToPlaceholder()
        {
            var log = new MeetingChatLog();

            log.Append(1, "", "hi", ChatSpeaker.Alive);

            Assert.Equal(MeetingChatLog.UnknownName, log.Entries[0].Name);
        }

        [Fact]
        public void Append_OverCapacity_DropsOldest()
        {
            var log = new MeetingChatLog();
            for (int i = 0; i < MeetingChatLog.MaxEntries + 5; i++)
            {
                log.Append(1, "Alice", $"msg{i}", ChatSpeaker.Alive);
            }

            Assert.Equal(MeetingChatLog.MaxEntries, log.Count);
            Assert.Equal("msg5", log.Entries[0].Text);
        }

        [Fact]
        public void Append_RepeatedWraparound_PreservesLogicalOrder()
        {
            var log = new MeetingChatLog();
            int appended = MeetingChatLog.MaxEntries * 2 + 17;
            for (int i = 0; i < appended; i++)
            {
                log.Append(i, "Alice", $"msg{i}", ChatSpeaker.Alive);
            }

            int first = appended - MeetingChatLog.MaxEntries;
            Assert.Equal(MeetingChatLog.MaxEntries, log.Count);
            Assert.Equal($"msg{first}", log.Entries[0].Text);
            Assert.Equal(first + MeetingChatLog.MaxEntries / 2,
                log.Entries[MeetingChatLog.MaxEntries / 2].Actor);
            Assert.Equal($"msg{appended - 1}", log.Entries[log.Count - 1].Text);
        }

        [Fact]
        public void Clear_EmptiesEntriesAndBumpsRevisionOnlyWhenNonEmpty()
        {
            var log = new MeetingChatLog();
            log.Clear();
            Assert.Equal(0, log.Revision);

            log.Append(1, "Alice", "hi", ChatSpeaker.Alive);
            int afterAppend = log.Revision;

            log.Clear();

            Assert.Empty(log);
            Assert.True(log.Revision > afterAppend);
        }

        [Fact]
        public void Append_DefaultsToMessageKind()
        {
            var log = new MeetingChatLog();

            log.Append(1, "Alice", "hi", ChatSpeaker.Alive);

            Assert.Equal(ChatEntryKind.Message, log.Entries[0].Kind);
        }

        [Fact]
        public void AppendVote_StoresSystemLineWithoutTarget()
        {
            var log = new MeetingChatLog();

            Assert.True(log.AppendVote(4, "Bob", Texts.Get(TextId.ChatLogVoted)));

            ChatLogEntry entry = log.Entries[0];
            Assert.Equal(ChatEntryKind.Vote, entry.Kind);
            Assert.Equal(4, entry.Actor);
            Assert.Equal("Bob", entry.Name);
            Assert.Equal(Texts.Get(TextId.ChatLogVoted), entry.Text);
        }

        [Fact]
        public void AppendVote_SharesBufferDisciplineWithMessages()
        {
            var log = new MeetingChatLog();

            log.AppendVote(1, new string('N', MeetingChatLog.MaxNameLength + 10), "投票しました。");

            Assert.Equal(MeetingChatLog.MaxNameLength + 1, log.Entries[0].Name.Length);
            Assert.Equal(1L, log.AppendedTotal);
        }

        [Fact]
        public void AppendedTotal_CountsOnlyAcceptedAppends()
        {
            var log = new MeetingChatLog();

            log.Append(1, "Alice", "hi", ChatSpeaker.Alive);
            log.Append(1, "Alice", "   ", ChatSpeaker.Alive);

            Assert.Equal(1L, log.AppendedTotal);
            Assert.Equal(0L, log.DroppedTotal);
        }

        [Fact]
        public void DroppedTotal_CountsTrimAndClear()
        {
            var log = new MeetingChatLog();
            for (int i = 0; i < MeetingChatLog.MaxEntries + 3; i++)
            {
                log.Append(1, "Alice", $"msg{i}", ChatSpeaker.Alive);
            }

            Assert.Equal(3L, log.DroppedTotal);

            log.Clear();

            Assert.Equal((long)MeetingChatLog.MaxEntries + 3L, log.DroppedTotal);
        }

        [Fact]
        public void AppendedMinusDropped_AlwaysEqualsCount()
        {
            var log = new MeetingChatLog();
            for (int i = 0; i < MeetingChatLog.MaxEntries + 7; i++)
            {
                log.Append(1, "Alice", $"msg{i}", ChatSpeaker.Alive);
                Assert.Equal(log.Count, (int)(log.AppendedTotal - log.DroppedTotal));
            }

            log.Clear();
            Assert.Equal(log.Count, (int)(log.AppendedTotal - log.DroppedTotal));
        }

        [Fact]
        public void EmptyClear_DoesNotMoveDroppedTotal()
        {
            var log = new MeetingChatLog();

            log.Clear();

            Assert.Equal(0L, log.DroppedTotal);
        }

        [Fact]
        public void Sanitize_EscapesRichTextOpeningBracket()
        {
            string result = MeetingChatLog.Sanitize("<color=#ff0000>red</color>", MeetingChatLog.MaxTextLength);

            Assert.DoesNotContain("<", result, StringComparison.Ordinal);
            Assert.Contains("＜color=#ff0000>red＜/color>", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Sanitize_CollapsesNewlinesToSingleLine()
        {
            string result = MeetingChatLog.Sanitize("a\nb\rc\td", MeetingChatLog.MaxTextLength);

            Assert.Equal("a b c d", result);
        }

        [Fact]
        public void Sanitize_TruncatesOverLimitWithEllipsis()
        {
            string raw = new string('あ', MeetingChatLog.MaxTextLength + 10);

            string result = MeetingChatLog.Sanitize(raw, MeetingChatLog.MaxTextLength);

            Assert.Equal(MeetingChatLog.MaxTextLength + 1, result.Length);
            Assert.EndsWith("…", result);
        }

        [Fact]
        public void Append_LongName_IsTruncated()
        {
            var log = new MeetingChatLog();

            log.Append(1, new string('N', MeetingChatLog.MaxNameLength + 10), "hi", ChatSpeaker.Alive);

            Assert.Equal(MeetingChatLog.MaxNameLength + 1, log.Entries[0].Name.Length);
        }

        [Fact]
        public void DeadSpeaker_IsPreservedForColouring()
        {
            var log = new MeetingChatLog();

            log.Append(7, "Ghost", "冥界から", ChatSpeaker.Dead);

            Assert.Equal(ChatSpeaker.Dead, log.Entries[0].Speaker);
        }
    }

    public class MeetingRecapTests
    {
        private static MeetingRecapData Data(
            IReadOnlyList<string> deaths = null,
            int lost = MeetingRecap.Unknown, int extracted = MeetingRecap.Unknown,
            int goal = MeetingRecap.Unknown, int beacon = MeetingRecap.Unknown)
            => new MeetingRecapData(deaths, lost, extracted, goal, beacon);

        [Fact]
        public void BuildLines_NoData_StillReportsDeathsLine()
        {
            List<string> lines = MeetingRecap.BuildLines(Data());

            Assert.Single(lines);
            Assert.Equal(Texts.Get(TextId.RecapDeathsNone), lines[0]);
        }

        [Fact]
        public void BuildLines_UnknownValues_AreOmitted()
        {
            List<string> lines = MeetingRecap.BuildLines(Data(lost: 1200));

            Assert.Equal(2, lines.Count);
            Assert.Contains("1200", lines[1]);
        }

        [Fact]
        public void BuildLines_HaulRequiresBothExtractedAndGoal()
        {
            Assert.Single(MeetingRecap.BuildLines(Data(extracted: 500)));
            Assert.Single(MeetingRecap.BuildLines(Data(goal: 9000)));
            Assert.Equal(2, MeetingRecap.BuildLines(Data(extracted: 500, goal: 9000)).Count);
        }

        [Fact]
        public void BuildLines_BeaconZero_UsesNoneWording()
        {
            List<string> lines = MeetingRecap.BuildLines(Data(beacon: 0));

            Assert.Equal(Texts.Get(TextId.RecapBeaconNone), lines[1]);
        }

        [Fact]
        public void BuildLines_JoinsDeathNames()
        {
            List<string> lines = MeetingRecap.BuildLines(Data(deaths: new[] { "Alice", "Bob" }));

            Assert.Contains("Alice", lines[0]);
            Assert.Contains("Bob", lines[0]);
        }

        [Fact]
        public void BuildLines_DeathNames_AreSanitized()
        {
            List<string> lines = MeetingRecap.BuildLines(Data(deaths: new[] { "<b>Evil" }));

            Assert.DoesNotContain("<", lines[0], StringComparison.Ordinal);
        }

        [Fact]
        public void Signature_ChangesWithEachInput()
        {
            long baseline = Data().Signature;

            Assert.NotEqual(baseline, Data(deaths: new[] { "A" }).Signature);
            Assert.NotEqual(baseline, Data(lost: 1).Signature);
            Assert.NotEqual(baseline, Data(extracted: 1).Signature);
            Assert.NotEqual(baseline, Data(goal: 1).Signature);
            Assert.NotEqual(baseline, Data(beacon: 1).Signature);
        }

        [Fact]
        public void Signature_IsStableForSameInput()
        {
            Assert.Equal(Data(lost: 10, beacon: 2).Signature, Data(lost: 10, beacon: 2).Signature);
        }
    }
}
