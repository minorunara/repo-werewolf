using System.Linq;
using Werewolf.Core;
using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayChatTextTests
    {

        [Theory]
        [InlineData(GamePhase.Meeting, true, true, true)]
        [InlineData(GamePhase.Meeting, true, false, false)]
        [InlineData(GamePhase.Meeting, false, true, false)]
        [InlineData(GamePhase.Play, true, true, false)]
        [InlineData(GamePhase.GameOver, true, true, false)]
        [InlineData(GamePhase.Lobby, true, true, false)]
        public void Gate_RecordsOnlyAliveSpeakersAfterDiscussionOpens(
            GamePhase phase, bool discussionOpen, bool alive, bool expected)
        {
            Assert.Equal(expected, ReplayChatGate.ShouldRecord(phase, discussionOpen, alive));
        }

        [Fact]
        public void Sanitize_SharesMeetingChatLogRules()
        {
            Assert.Equal("a b＜c", ReplayChatText.SanitizeForRecord(" a\nb<c "));
            Assert.Equal("t a b", ReplayChatText.SanitizeForRecord("\tt\ra\tb"));
            Assert.Equal(string.Empty, ReplayChatText.SanitizeForRecord(null));
            Assert.Equal(string.Empty, ReplayChatText.SanitizeForRecord(""));
        }

        [Fact]
        public void Sanitize_TruncatesAt50DisplayChars_WithoutEllipsis()
        {
            string exactly50 = new string('あ', 50);
            Assert.Equal(exactly50, ReplayChatText.SanitizeForRecord(exactly50));

            string over = new string('あ', 51);
            Assert.Equal(new string('あ', 50), ReplayChatText.SanitizeForRecord(over));
        }

        [Fact]
        public void Truncate_DoesNotSplitSurrogatePair()
        {
            string s = new string('あ', 49) + "\U0001F410" + "xyz";
            string cut = ReplayChatText.TruncateDisplay(s, 50);
            Assert.Equal(49 * 1 + 2, cut.Length);
            Assert.EndsWith("\U0001F410", cut);
        }

        [Fact]
        public void DisplayLength_CountsCombinedSequencesAsOne()
        {
            Assert.Equal(1, ReplayChatText.DisplayLength("か" + (char)0x3099));
            Assert.Equal(1, ReplayChatText.DisplayLength("☺" + (char)0xFE0F));
            Assert.Equal(1, ReplayChatText.DisplayLength(char.ConvertFromUtf32(0x1F410)));
            Assert.Equal(1, ReplayChatText.DisplayLength(
                char.ConvertFromUtf32(0x1F468) + (char)0x200D + char.ConvertFromUtf32(0x1F469)
                + (char)0x200D + char.ConvertFromUtf32(0x1F467)));
            Assert.Equal(3, ReplayChatText.DisplayLength("abc"));
            Assert.Equal(0, ReplayChatText.DisplayLength(""));
        }

        [Fact]
        public void Wrap_30CharsOrLess_SingleLine()
        {
            string s30 = new string('い', 30);
            (string l1, string l2) = ReplayChatText.Wrap(s30);
            Assert.Equal(s30, l1);
            Assert.Null(l2);
        }

        [Fact]
        public void Wrap_31Chars_SplitsAt25Plus6()
        {
            string s31 = new string('う', 31);
            (string l1, string l2) = ReplayChatText.Wrap(s31);
            Assert.Equal(25, ReplayChatText.DisplayLength(l1));
            Assert.Equal(6, ReplayChatText.DisplayLength(l2));
        }

        [Fact]
        public void Wrap_50Chars_SplitsAt25Plus25()
        {
            string s50 = new string('え', 50);
            (string l1, string l2) = ReplayChatText.Wrap(s50);
            Assert.Equal(25, ReplayChatText.DisplayLength(l1));
            Assert.Equal(25, ReplayChatText.DisplayLength(l2));
        }

        [Fact]
        public void Wrap_LineHeadPunctuation_ShiftsSplitTo26()
        {
            string s = new string('お', 25) + "、" + new string('か', 10);
            (string l1, string l2) = ReplayChatText.Wrap(s);
            Assert.Equal(26, ReplayChatText.DisplayLength(l1));
            Assert.EndsWith("、", l1);
            Assert.StartsWith("か", l2);
        }

        [Fact]
        public void Wrap_OpenBracketAtLineTail_ShiftsSplit()
        {
            string s = new string('き', 24) + "「" + new string('く', 15);
            (string l1, string l2) = ReplayChatText.Wrap(s);
            Assert.False(l1.EndsWith("「"));
            Assert.Equal(40, ReplayChatText.DisplayLength(l1) + ReplayChatText.DisplayLength(l2));
        }

        [Fact]
        public void Wrap_AllCandidatesViolate_FallsBackTo25()
        {
            string s = new string('け', 24) + "、、、" + new string('こ', 9);
            (string l1, string l2) = ReplayChatText.Wrap(s);
            Assert.Equal(25, ReplayChatText.DisplayLength(l1));
            Assert.StartsWith("、", l2);
        }

        [Fact]
        public void Wrap_DoesNotSplitInsideDisplayChar()
        {
            string cluster = "\U0001F410" + (char)0xFE0F;
            string s = new string('さ', 24) + cluster + new string('し', 10);
            (string l1, string l2) = ReplayChatText.Wrap(s);
            Assert.EndsWith(cluster, l1);
            Assert.Equal('し', l2[0]);
        }

        [Fact]
        public void DwellSeconds_ClampsBetweenBaseAndMax()
        {
            Assert.Equal(1.25, ReplayChatText.DwellSeconds(0), 3);
            Assert.Equal(1.875, ReplayChatText.DwellSeconds(25), 3);
            Assert.Equal(2.5, ReplayChatText.DwellSeconds(50), 3);
            Assert.Equal(2.5, ReplayChatText.DwellSeconds(999), 3);
        }

        [Fact]
        public void TotalSeconds_AddsSlideInAndOut()
        {
            Assert.Equal(0.22 + 1.25 + 0.28, ReplayChatText.TotalSeconds(0), 3);
            Assert.Equal(0.22 + 2.5 + 0.28, ReplayChatText.TotalSeconds(50), 3);
        }

        [Fact]
        public void Recorder_ChatAndMeetingResult_SerializeAsGenericEvents()
        {
            var rec = new ReplayRecorder();
            rec.BeginSegment(new ReplaySegmentHeader
            {
                LevelName = "L",
                StartedAtIso = "2026-08-11T00:00:00+09:00",
                IsHost = true,
                LocalActor = 1,
            }, 100.0);
            rec.NoteEvent(101.5, "chat", ("a", 3), ("text", "て\"st"));
            rec.NoteEvent(102.0, "meeting_result", ("a", -1));
            rec.EndSegment(110.0);

            string[] lines = rec.ToJsonLines().ToArray();
            Assert.Contains(lines, l =>
                l.Contains("\"e\":\"chat\"") && l.Contains("\"a\":3") && l.Contains("\"text\":\"て\\\"st\""));
            Assert.Contains(lines, l =>
                l.Contains("\"e\":\"meeting_result\"") && l.Contains("\"a\":-1"));
        }
    }
}
