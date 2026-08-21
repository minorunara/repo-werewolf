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
        public void AppendSystem_StoresTitleAndUsesTheFixedSystemSpeaker()
        {
            var log = new MeetingChatLog();

            Assert.True(log.AppendSystem("Taxman", "ここまでの経過", "死亡: なし"));

            ChatLogEntry entry = log.Entries[0];
            Assert.Equal(ChatEntryKind.System, entry.Kind);
            Assert.Equal(MeetingChatLog.SystemActor, entry.Actor);
            Assert.Equal("Taxman", entry.Name);
            Assert.Equal("ここまでの経過", entry.Title);
            Assert.Equal("死亡: なし", entry.Text);
        }

        [Fact]
        public void AppendSystem_KeepsLineBreaks()
        {
            var log = new MeetingChatLog();

            log.AppendSystem("Taxman", "前回の組分け", "【A】1番・3番\r\n【B】2番・4番");

            Assert.Equal("【A】1番・3番\n【B】2番・4番", log.Entries[0].Text);
        }

        [Fact]
        public void AppendSystem_EscapesRichTextOpeningBracket()
        {
            var log = new MeetingChatLog();

            log.AppendSystem("Taxman", "<b>title", "死亡: <color=#ff0000>Evil");

            Assert.DoesNotContain("<", log.Entries[0].Text, StringComparison.Ordinal);
            Assert.DoesNotContain("<", log.Entries[0].Title, StringComparison.Ordinal);
        }

        [Fact]
        public void AppendSystem_EmptyBody_IsRejected()
        {
            var log = new MeetingChatLog();

            Assert.False(log.AppendSystem("Taxman", "見出し", "  \n  "));
            Assert.Empty(log);
        }

        [Fact]
        public void AppendSystem_CarriesTheChosenSpeakerIcon()
        {
            var log = new MeetingChatLog();

            log.AppendSystem("Taxman", "ここまでの経過", "死亡: Alice", "img_taxman_system");
            log.AppendSystem("Taxman", "ここまでの経過", "死亡: なし", "img_taxman_nodeath");

            Assert.Equal("img_taxman_system", log.Entries[0].Icon);
            Assert.Equal("img_taxman_nodeath", log.Entries[1].Icon);
        }

        [Fact]
        public void AppendSystem_Section_RecordsTheEntrySequence()
        {
            var log = new MeetingChatLog();
            log.Append(1, "Alice", "hi", ChatSpeaker.Alive);
            log.AppendSystem("Taxman", "🔔 1", "死亡: なし", section: true);
            log.Append(2, "Bob", "yo", ChatSpeaker.Alive);
            log.AppendSystem("Taxman", "🔔 2", "死亡: なし", section: true);
            log.AppendSystem("Taxman", "前回の組分け", "【A】1番");

            Assert.Equal(new long[] { 1L, 3L }, log.SectionSeqs);
        }

        [Fact]
        public void AppendSystem_SectionWithEmptyBody_IsNotRecorded()
        {
            var log = new MeetingChatLog();

            Assert.False(log.AppendSystem("Taxman", "🔔 1", "  \n  ", section: true));
            Assert.Empty(log.SectionSeqs);
        }

        [Fact]
        public void SectionSeqs_DroppedByOverflow_AreRemoved()
        {
            var log = new MeetingChatLog();
            log.AppendSystem("Taxman", "🔔 1", "死亡: なし", section: true);
            for (int i = 0; i < MeetingChatLog.MaxEntries; i++)
            {
                log.Append(1, "Alice", $"msg{i}", ChatSpeaker.Alive);
            }

            Assert.Equal(1L, log.DroppedTotal);
            Assert.Empty(log.SectionSeqs);
        }

        [Fact]
        public void Clear_EmptiesSectionSeqs()
        {
            var log = new MeetingChatLog();
            log.AppendSystem("Taxman", "🔔 1", "死亡: なし", section: true);

            log.Clear();

            Assert.Empty(log.SectionSeqs);
        }

        [Fact]
        public void Message_HasNoTitleOrIcon()
        {
            var log = new MeetingChatLog();

            log.Append(1, "Alice", "hi", ChatSpeaker.Alive);

            Assert.Equal(string.Empty, log.Entries[0].Title);
            Assert.Equal(string.Empty, log.Entries[0].Icon);
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
        public void BuildLines_DeathLabels_KeepIdPrefix()
        {
            List<string> lines = MeetingRecap.BuildLines(Data(deaths: new[]
            {
                ParticipantLabel.Format(3, "Alice"), ParticipantLabel.Format(12, "Bob"),
            }));

            Assert.Contains("3. Alice", lines[0], StringComparison.Ordinal);
            Assert.Contains("12. Bob", lines[0], StringComparison.Ordinal);
        }

        [Fact]
        public void BuildLines_DeathNames_AreSanitized()
        {
            List<string> lines = MeetingRecap.BuildLines(Data(deaths: new[] { "<b>Evil" }));

            Assert.DoesNotContain("<", lines[0], StringComparison.Ordinal);
        }

        [Fact]
        public void LostSince_ReportsDifferenceFromPreviousMeeting()
        {
            Assert.Equal(5000, MeetingRecap.LostSince(20000, 15000));
        }

        [Fact]
        public void LostSince_FirstMeeting_ReportsWholeTotal()
        {
            Assert.Equal(20000, MeetingRecap.LostSince(20000, 0));
        }

        [Fact]
        public void LostSince_NoNewLoss_ReportsZero()
        {
            Assert.Equal(0, MeetingRecap.LostSince(15000, 15000));
        }

        [Fact]
        public void LostSince_BelowBaseline_ClampsToZero()
        {
            Assert.Equal(0, MeetingRecap.LostSince(9000, 15000));
        }

        [Fact]
        public void LostSince_UnknownTotal_StaysUnknown()
        {
            Assert.Equal(MeetingRecap.Unknown, MeetingRecap.LostSince(MeetingRecap.Unknown, 15000));
        }

        [Fact]
        public void BuildLines_LostDelta_IsRenderedAsTheLossLine()
        {
            List<string> lines = MeetingRecap.BuildLines(
                Data(lost: MeetingRecap.LostSince(20000, 15000)));

            Assert.Equal(2, lines.Count);
            Assert.Contains("5000", lines[1], StringComparison.Ordinal);
            Assert.DoesNotContain("20000", lines[1], StringComparison.Ordinal);
        }

        [Fact]
        public void BuildLines_Emoji_ReplacesLabelsAndKeepsValues()
        {
            List<string> lines = MeetingRecap.BuildLines(
                Data(deaths: new[] { "Alice" }, lost: 1200), emoji: true);

            Assert.Equal(ChatEmoji.Format(TextId.RecapDeathsFormat, true, "Alice"), lines[0]);
            Assert.Contains("1200", lines[1]);
            Assert.DoesNotContain(Texts.Get(TextId.RecapLostFormat).Split('{')[0], lines[1]);
        }
    }

    public class ParticipantLabelTests
    {
        [Fact]
        public void Format_WithId_PrefixesNumber()
        {
            Assert.Equal(Texts.Format(TextId.IdNameFormat, 3, "Alice"), ParticipantLabel.Format(3, "Alice"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Format_WithoutRoster_FallsBackToNameOnly(int id)
        {
            Assert.Equal("Alice", ParticipantLabel.Format(id, "Alice"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Format_EmptyName_UsesUnknownName(string name)
        {
            Assert.Equal(Texts.Format(TextId.IdNameFormat, 3, MeetingChatLog.UnknownName),
                ParticipantLabel.Format(3, name));
        }

        [Fact]
        public void Format_NeutralizesRichTextInName()
        {
            Assert.DoesNotContain("<", ParticipantLabel.Format(3, "<b>Evil"), StringComparison.Ordinal);
        }

        [Fact]
        public void Format_TruncatesNamePartOnly()
        {
            string name = new string('あ', MeetingChatLog.MaxNameLength + 10);

            string label = ParticipantLabel.Format(12, name);

            Assert.StartsWith(Texts.Format(TextId.IdNameFormat, 12, string.Empty).TrimEnd(), label,
                StringComparison.Ordinal);
            Assert.True(label.Length <= ParticipantLabel.MaxLength);
        }
    }
}
