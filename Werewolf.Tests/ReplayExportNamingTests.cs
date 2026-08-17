using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayExportNamingTests
    {
        [Fact]
        public void FileName_IsDeterministic_FromStartAndLevel()
        {
            var header = new ReplaySegmentHeader
            {
                LevelName = "Level - Museum",
                StartedAtIso = "2026-08-11T05:06:07+09:00",
            };
            Assert.Equal("repo_werewolf_replay_20260811_050607_Museum.jsonl",
                ReplayExportNaming.FileName(header));
            Assert.Equal(ReplayExportNaming.FileName(header), ReplayExportNaming.FileName(header));
        }

        [Fact]
        public void FormatStamp_IgnoresTimezoneDigits_And14DigitsOnly()
        {
            Assert.Equal("20260811_050607", ReplayExportNaming.FormatStamp("2026-08-11T05:06:07+09:00"));
            Assert.Equal("unknown", ReplayExportNaming.FormatStamp("no digits"));
            Assert.Equal("unknown", ReplayExportNaming.FormatStamp(null));
            Assert.Equal("unknown", ReplayExportNaming.FormatStamp("2026-08-11"));
        }

        [Fact]
        public void SanitizeLevel_StripsVanillaPrefix_AndUnsafeChars()
        {
            Assert.Equal("Museum", ReplayExportNaming.SanitizeLevel("Level - Museum"));
            Assert.Equal("McJannek_Station", ReplayExportNaming.SanitizeLevel("McJannek Station"));
            Assert.Equal("a_b", ReplayExportNaming.SanitizeLevel("a///b"));
            Assert.Equal("level", ReplayExportNaming.SanitizeLevel("///"));
            Assert.Equal("level", ReplayExportNaming.SanitizeLevel(null));
        }
    }
}
