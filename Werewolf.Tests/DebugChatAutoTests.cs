using System.Collections.Generic;
using System.Linq;
using Werewolf.Core.Replay;
using Werewolf.Debugging;
using Xunit;

namespace Werewolf.Tests
{
    public class DebugChatAutoTests
    {
        private const char Zwj = (char)0x200D;

        private static IEnumerable<string> Recorded()
            => DebugChatAuto.Texts.Select(ReplayChatText.SanitizeForRecord);

        private static IEnumerable<DebugChatAuto.Step> Steps()
            => Enumerable.Range(0, DebugChatAuto.StepCount).Select(DebugChatAuto.StepAt);

        [Theory]
        [InlineData(1)]
        [InlineData(30)]
        [InlineData(31)]
        [InlineData(50)]
        public void Texts_CoverDisplayLengthBoundaries(int displayChars)
        {
            Assert.Contains(Recorded(), t => ReplayChatText.DisplayLength(t) == displayChars);
        }

        [Fact]
        public void Texts_IncludeOneThatTruncatesToTheLimit()
        {
            string over = DebugChatAuto.Texts.First(
                t => ReplayChatText.DisplayLength(t) > ReplayChatText.MaxDisplayChars);
            Assert.Equal(
                ReplayChatText.MaxDisplayChars,
                ReplayChatText.DisplayLength(ReplayChatText.SanitizeForRecord(over)));
        }

        [Fact]
        public void Texts_IncludeTagAndControlCharacters()
        {
            Assert.Contains(DebugChatAuto.Texts, t => t.Contains('<'));
            Assert.DoesNotContain(Recorded(), t => t.Contains('<'));

            Assert.Contains(DebugChatAuto.Texts, t => t.Contains('\n') || t.Contains('\t'));
            Assert.DoesNotContain(Recorded(), t => t.Contains('\n') || t.Contains('\t'));
        }

        [Fact]
        public void Texts_IncludeCombinedSequence()
        {
            string combined = ReplayChatText.SanitizeForRecord(
                DebugChatAuto.Texts.First(t => t.Contains(Zwj)));

            int codePoints = 0;
            for (int i = 0; i < combined.Length; i += char.IsSurrogatePair(combined, i) ? 2 : 1)
            {
                codePoints++;
            }
            Assert.True(ReplayChatText.DisplayLength(combined) < codePoints);
        }

        [Fact]
        public void Texts_IncludeOneWhoseWrapShiftsForKinsoku()
        {
            Assert.Contains(Recorded(), t =>
                ReplayChatText.DisplayLength(t) > ReplayChatText.SingleLineMax &&
                ReplayChatText.DisplayLength(ReplayChatText.Wrap(t).Line1) != ReplayChatText.WrapTarget);
        }

        [Fact]
        public void Script_CoversBothSidesOfTheSilenceBoundary()
        {
            int windowMs = (int)(ReplayPace.TalkWindowSec * 1000);
            Assert.Contains(Steps(), s => s.GapMs > windowMs);
            Assert.Contains(Steps(), s => s.GapMs > windowMs / 2 && s.GapMs < windowMs);
        }

        [Fact]
        public void Script_CoversBurstAndSameFramePosts()
        {
            Assert.Contains(Steps(), s => s.GapMs == 0);
            Assert.Contains(Steps(), s => s.GapMs > 0 && s.GapMs <= 500);
        }

        [Fact]
        public void Script_UsesEverySpeakerAndRepeatsOne()
        {
            const int speakers = 5;
            var auto = new DebugChatAuto();
            auto.Start(0, 0);

            var slots = new List<int>();
            long now = 0;
            for (int i = 0; i < DebugChatAuto.StepCount; i++)
            {
                now += DebugChatAuto.StepAt(i).GapMs;
                Assert.True(auto.TryTakeDue(now, speakers, out int slot, out _));
                slots.Add(slot);
            }

            Assert.Equal(speakers, slots.Distinct().Count());
            Assert.Contains(Enumerable.Range(1, slots.Count - 1), i => slots[i] == slots[i - 1]);
        }

        [Fact]
        public void FirstPostGoesToTheLocalSlot()
        {
            var auto = new DebugChatAuto();
            auto.Start(0, 0);
            Assert.True(auto.TryTakeDue(DebugChatAuto.StepAt(0).GapMs, 4, out int slot, out _));
            Assert.Equal(0, slot);
        }

        [Fact]
        public void TryTakeDue_YieldsNothingBeforeStart()
        {
            var auto = new DebugChatAuto();
            Assert.False(auto.Active);
            Assert.False(auto.TryTakeDue(10_000, 4, out _, out _));
        }

        [Fact]
        public void TryTakeDue_WaitsForTheGap()
        {
            var auto = new DebugChatAuto();
            auto.Start(1000, 0);

            long due = 1000 + DebugChatAuto.StepAt(0).GapMs;
            Assert.False(auto.TryTakeDue(due - 1, 4, out _, out _));
            Assert.True(auto.TryTakeDue(due, 4, out _, out string text));
            Assert.False(string.IsNullOrEmpty(text));

            int nextGap = DebugChatAuto.StepAt(1).GapMs;
            Assert.False(auto.TryTakeDue(due + nextGap - 1, 4, out _, out _));
            Assert.True(auto.TryTakeDue(due + nextGap, 4, out _, out _));
        }

        [Fact]
        public void TryTakeDue_EmitsZeroGapStepsInTheSameFrame()
        {
            int zeroAt = Enumerable.Range(1, DebugChatAuto.StepCount - 1)
                .First(i => DebugChatAuto.StepAt(i).GapMs == 0);

            var auto = new DebugChatAuto();
            auto.Start(0, 0);

            long now = 0;
            for (int i = 0; i < zeroAt; i++)
            {
                now += DebugChatAuto.StepAt(i).GapMs;
                Assert.True(auto.TryTakeDue(now, 4, out _, out _));
            }

            Assert.True(auto.TryTakeDue(now, 4, out _, out _));
            Assert.Equal(
                DebugChatAuto.StepAt(zeroAt + 1).GapMs == 0,
                auto.TryTakeDue(now, 4, out _, out _));
        }

        [Fact]
        public void TryTakeDue_DoesNotCatchUpAfterAStall()
        {
            var auto = new DebugChatAuto();
            auto.Start(0, 0);

            long late = 600_000;
            Assert.True(auto.TryTakeDue(late, 4, out _, out _));
            Assert.False(auto.TryTakeDue(late, 4, out _, out _));
            Assert.True(auto.TryTakeDue(late + DebugChatAuto.StepAt(1).GapMs, 4, out _, out _));
        }

        [Fact]
        public void Count_StopsAfterTheRequestedPosts()
        {
            var auto = new DebugChatAuto();
            auto.Start(0, 3);

            long now = 600_000;
            for (int i = 0; i < 3; i++)
            {
                Assert.True(auto.TryTakeDue(now, 4, out _, out _));
                now += 60_000;
            }

            Assert.False(auto.Active);
            Assert.Equal(3, auto.Posted);
            Assert.False(auto.TryTakeDue(now, 4, out _, out _));
        }

        [Fact]
        public void Stop_HaltsAndKeepsTheCount()
        {
            var auto = new DebugChatAuto();
            auto.Start(0, 0);
            Assert.True(auto.TryTakeDue(600_000, 4, out _, out _));

            auto.Stop();
            Assert.False(auto.Active);
            Assert.Equal(1, auto.Posted);
            Assert.False(auto.TryTakeDue(600_000, 4, out _, out _));

            auto.Start(600_000, 0);
            Assert.Equal(0, auto.Posted);
            Assert.Equal(-1, auto.Remaining);
        }

        [Fact]
        public void TryTakeDue_IgnoresEmptyRoster()
        {
            var auto = new DebugChatAuto();
            auto.Start(0, 0);
            Assert.False(auto.TryTakeDue(600_000, 0, out _, out _));
            Assert.True(auto.Active);
        }
    }
}
