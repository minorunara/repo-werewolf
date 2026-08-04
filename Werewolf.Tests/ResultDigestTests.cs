using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ResultDigestTests
    {
        private const byte BombDetonation = 180;

        private static ResultDigest StartedDigest(long startMs = 1_000_000L)
        {
            var d = new ResultDigest();
            d.Observe(WWEventCodes.GameStart, new object[0], startMs);
            return d;
        }

        [Fact]
        public void Observe_BeforeGameStart_IsIgnored()
        {
            var d = new ResultDigest();
            d.Observe(WWEventCodes.PlayerDied, new object[] { 2, (byte)DeathCause.Other }, 500L);

            Assert.False(d.Started);
            Assert.Empty(d.Entries);
        }

        [Fact]
        public void GameStart_ResetsAndRecordsMatchStart()
        {
            var d = StartedDigest();
            d.Observe(WWEventCodes.PlayerDied, new object[] { 2, (byte)DeathCause.Other }, 1_030_000L);

            d.Observe(WWEventCodes.GameStart, new object[0], 2_000_000L);

            Assert.True(d.Started);
            DigestEntry only = Assert.Single(d.Entries);
            Assert.Equal(DigestKind.MatchStart, only.Kind);
            Assert.Equal(0, only.AtSec);
        }

        [Fact]
        public void AtSec_IsElapsedFromGameStart()
        {
            var d = StartedDigest(startMs: 1_000_000L);
            d.Observe(WWEventCodes.PlayerDied, new object[] { 2, (byte)DeathCause.Other }, 1_095_500L);

            Assert.Equal(95, d.Entries[1].AtSec);
        }

        [Fact]
        public void StartMeeting_RecordsCallerAndConveneKind()
        {
            var d = StartedDigest();
            d.Observe(WWMeetingCodes.StartMeeting, new object[] { 3, 0L, 0L, (byte)1 }, 1_060_000L);

            DigestEntry e = d.Entries[1];
            Assert.Equal(DigestKind.MeetingConvened, e.Kind);
            Assert.Equal(3, e.Actor);
            Assert.Equal(1, e.ArgA);
        }

        [Fact]
        public void MeetingResult_MapsExecutedAndSkip()
        {
            var d = StartedDigest();
            d.Observe(WWMeetingCodes.MeetingResult,
                new object[] { 4, new[] { 4, -1 }, new[] { 3, 1 } }, 1_060_000L);
            d.Observe(WWMeetingCodes.MeetingResult,
                new object[] { -1, new[] { -1 }, new[] { 4 } }, 1_120_000L);

            Assert.Equal(DigestKind.Executed, d.Entries[1].Kind);
            Assert.Equal(4, d.Entries[1].Actor);
            Assert.Equal(DigestKind.NoExecution, d.Entries[2].Kind);
        }

        [Fact]
        public void PlayerDied_RecordsOtherOnly_SuppressesVote()
        {
            var d = StartedDigest();
            d.Observe(WWEventCodes.PlayerDied, new object[] { 2, (byte)DeathCause.Other }, 1_030_000L);
            d.Observe(WWEventCodes.PlayerDied, new object[] { 4, (byte)DeathCause.Vote }, 1_040_000L);

            Assert.Equal(2, d.Entries.Count);
            Assert.Equal(DigestKind.Death, d.Entries[1].Kind);
            Assert.Equal(2, d.Entries[1].Actor);
        }

        [Fact]
        public void RoleState_MapsCurseSubtypes_IgnoresGaugeSnapshot()
        {
            var d = StartedDigest();
            d.Observe(WWRolesCodes.RoleState, new object[] { (byte)0, new[] { 5 }, 0L }, 1_060_000L);
            d.Observe(WWRolesCodes.RoleState, new object[] { (byte)2, new[] { 1, 2, 3 }, 0L }, 1_061_000L);
            d.Observe(WWRolesCodes.RoleState, new object[] { (byte)1, new[] { 6 }, 0L }, 1_070_000L);

            Assert.Equal(3, d.Entries.Count);
            Assert.Equal(DigestKind.CurseStarted, d.Entries[1].Kind);
            Assert.Equal(5, d.Entries[1].Actor);
            Assert.Equal(DigestKind.CurseFollow, d.Entries[2].Kind);
            Assert.Equal(6, d.Entries[2].Actor);
        }

        [Fact]
        public void BombDetonation_RecordsTarget()
        {
            var d = StartedDigest();
            d.Observe(BombDetonation, new object[] { 7, 9_999L }, 1_030_000L);

            Assert.Equal(DigestKind.BombDetonated, d.Entries[1].Kind);
            Assert.Equal(7, d.Entries[1].Actor);
        }

        [Fact]
        public void CheckmateReveal_Recorded()
        {
            var d = StartedDigest();
            d.Observe(WWCheckmateCodes.CheckmateReveal, new object[] { new int[0], 0L }, 1_030_000L);

            Assert.Equal(DigestKind.Checkmate, d.Entries[1].Kind);
        }

        [Fact]
        public void GameOver_RecordsMatchEnd_WithWinnerReason()
        {
            var d = StartedDigest();
            d.Observe(WWEventCodes.GameOver,
                new object[] { (byte)Team.Werewolves, new int[0], new byte[0] }, 1_600_000L,
                new WinResult(Team.Werewolves, WinReason.TimerExpired));

            DigestEntry e = d.Entries[1];
            Assert.Equal(DigestKind.MatchEnd, e.Kind);
            Assert.Equal((int)Team.Werewolves, e.ArgA);
            Assert.Equal((int)WinReason.TimerExpired, e.ArgB);
        }

        [Fact]
        public void GameOver_WithoutWinner_UsesReasonUnknown()
        {
            var d = StartedDigest();
            d.Observe(WWEventCodes.GameOver,
                new object[] { (byte)Team.Villagers, new int[0], new byte[0] }, 1_600_000L);

            Assert.Equal(ResultDigest.ReasonUnknown, d.Entries[1].ArgB);
        }

        [Fact]
        public void MalformedPayload_IsIgnored()
        {
            var d = StartedDigest();
            d.Observe(WWEventCodes.PlayerDied, new object[] { "broken" }, 1_030_000L);
            d.Observe(WWMeetingCodes.StartMeeting, null, 1_031_000L);
            d.Observe(WWRolesCodes.RoleState, new object[] { (byte)1, new int[0], 0L }, 1_032_000L);

            Assert.Single(d.Entries);
        }

        [Fact]
        public void Record_BeforeGameStart_IsIgnored()
        {
            var d = new ResultDigest();
            d.RecordExtractionDone(1, 4, 500L);
            d.RecordPerkUnlocked((byte)PerkId.InfiniteStamina, 500L);
            d.RecordInformant(500L);
            d.RecordFinalBalance(10_000, 5_000, 3_000, 500L);

            Assert.Empty(d.Entries);
        }

        [Fact]
        public void RecordExtractionDone_CarriesCompletedAndTotal()
        {
            var d = StartedDigest(startMs: 1_000_000L);
            d.RecordExtractionDone(2, 4, 1_492_000L);

            DigestEntry e = d.Entries[1];
            Assert.Equal(DigestKind.ExtractionDone, e.Kind);
            Assert.Equal(492, e.AtSec);
            Assert.Equal(2, e.ArgA);
            Assert.Equal(4, e.ArgB);
        }

        [Fact]
        public void RecordPerkUnlocked_CarriesPerkId()
        {
            var d = StartedDigest();
            d.RecordPerkUnlocked((byte)PerkId.EnemyIgnore, 1_231_000L);

            DigestEntry e = d.Entries[1];
            Assert.Equal(DigestKind.PerkUnlocked, e.Kind);
            Assert.Equal((int)PerkId.EnemyIgnore, e.ArgA);
        }

        [Fact]
        public void RecordInformant_Recorded()
        {
            var d = StartedDigest();
            d.RecordInformant(1_100_000L);

            Assert.Equal(DigestKind.InformantEstablished, d.Entries[1].Kind);
        }

        [Fact]
        public void RecordFinalBalance_CarriesDollars_ActorFieldIsObtainable()
        {
            var d = StartedDigest();
            d.RecordFinalBalance(12_000, 8_000, 9_500, 1_600_000L);

            DigestEntry e = d.Entries[1];
            Assert.Equal(DigestKind.FinalBalance, e.Kind);
            Assert.Equal(12_000, e.ArgA);
            Assert.Equal(8_000, e.ArgB);
            Assert.Equal(9_500, e.Actor);
        }

        [Fact]
        public void RecordFinalBalance_BypassesEntryCap_LikeMatchEnd()
        {
            var d = StartedDigest();
            for (int i = 0; i < ResultDigest.MaxEntries + 10; i++)
            {
                d.RecordExtractionDone(1, 4, 1_030_000L);
            }
            Assert.Equal(ResultDigest.MaxEntries, d.Entries.Count);

            d.RecordFinalBalance(1, 2, 3, 1_600_000L);
            Assert.Equal(DigestKind.FinalBalance, d.Entries.Last().Kind);
        }

        [Fact]
        public void EntryCap_DropsOverflow_ButAlwaysKeepsMatchEnd()
        {
            var d = StartedDigest();
            for (int i = 0; i < ResultDigest.MaxEntries + 50; i++)
            {
                d.Observe(WWEventCodes.PlayerDied,
                    new object[] { i + 2, (byte)DeathCause.Other }, 1_030_000L);
            }
            Assert.Equal(ResultDigest.MaxEntries, d.Entries.Count);

            d.Observe(WWEventCodes.GameOver,
                new object[] { (byte)Team.Villagers, new int[0], new byte[0] }, 1_600_000L,
                new WinResult(Team.Villagers, WinReason.ExtractionCompleted));
            Assert.Equal(DigestKind.MatchEnd, d.Entries.Last().Kind);
        }

        [Fact]
        public void WireRoundTrip_PreservesEntries()
        {
            var d = StartedDigest();
            d.Observe(WWMeetingCodes.StartMeeting, new object[] { 3, 0L, 0L, (byte)0 }, 1_060_000L);
            d.Observe(WWEventCodes.GameOver,
                new object[] { (byte)Team.Werewolves, new int[0], new byte[0] }, 1_600_000L,
                new WinResult(Team.Werewolves, WinReason.ValueCheckmate));

            IReadOnlyList<DigestEntry> restored = ResultDigest.FromWire(d.ToWire());

            Assert.NotNull(restored);
            Assert.Equal(d.Entries.Count, restored.Count);
            for (int i = 0; i < restored.Count; i++)
            {
                Assert.Equal(d.Entries[i].Kind, restored[i].Kind);
                Assert.Equal(d.Entries[i].AtSec, restored[i].AtSec);
                Assert.Equal(d.Entries[i].Actor, restored[i].Actor);
                Assert.Equal(d.Entries[i].ArgA, restored[i].ArgA);
                Assert.Equal(d.Entries[i].ArgB, restored[i].ArgB);
            }
        }

        [Fact]
        public void FromWire_MissingArrays_ReturnsNull()
        {
            Assert.Null(ResultDigest.FromWire(null));
            Assert.Null(ResultDigest.FromWire(new object[0]));
            Assert.Null(ResultDigest.FromWire(new object[] { new byte[0], null, null, null, null }));
        }

        [Fact]
        public void FromWire_LengthMismatch_ReturnsNull()
        {
            object[] payload =
            {
                new byte[] { 0, 6 }, new[] { 0, 10 }, new[] { 0 }, new[] { 0, 0 }, new[] { 0, 0 },
            };
            Assert.Null(ResultDigest.FromWire(payload));
        }

        [Fact]
        public void FormatTime_MinutesSeconds_AndHours()
        {
            Assert.Equal("00:00", ResultDigestText.FormatTime(0));
            Assert.Equal("01:35", ResultDigestText.FormatTime(95));
            Assert.Equal("1:01:05", ResultDigestText.FormatTime(3665));
            Assert.Equal("00:00", ResultDigestText.FormatTime(-10));
        }

        [Fact]
        public void FormatLines_UsesResolvedNames_AndTimePrefix()
        {
            var entries = new List<DigestEntry>
            {
                new DigestEntry(DigestKind.MatchStart, 0, 0, 0, 0),
                new DigestEntry(DigestKind.Death, 95, 2, 0, 0),
            };
            List<string> lines = ResultDigestText.FormatLines(entries, a => "P" + a);

            Assert.Equal(2, lines.Count);
            Assert.StartsWith("00:00", lines[0]);
            Assert.StartsWith("01:35", lines[1]);
            Assert.Contains("P2", lines[1]);
        }

        [Fact]
        public void FormatLines_FallsBackToActorNumber_WhenNameUnresolved()
        {
            var entries = new List<DigestEntry> { new DigestEntry(DigestKind.Death, 5, -101, 0, 0) };
            List<string> lines = ResultDigestText.FormatLines(entries, a => null);

            Assert.Contains("Actor-101", lines[0]);
        }

        [Fact]
        public void FormatLines_MatchEnd_IncludesTeamAndReason()
        {
            var entries = new List<DigestEntry>
            {
                new DigestEntry(DigestKind.MatchEnd, 300, 0,
                    (int)Team.Werewolves, (int)WinReason.TimerExpired),
            };
            List<string> lines = ResultDigestText.FormatLines(entries, a => "P" + a);

            string line = Assert.Single(lines);
            Assert.Contains(Texts.Get(TextId.ResultBannerWerewolfWin), line);
            Assert.Contains(Texts.Get(TextId.DigestReasonTimerExpired), line);
        }

        [Fact]
        public void FormatLines_ExtractionDone_ShowsCompletedOverTotal()
        {
            var entries = new List<DigestEntry>
            {
                new DigestEntry(DigestKind.ExtractionDone, 492, 0, 2, 4),
            };
            List<string> lines = ResultDigestText.FormatLines(entries, a => "P" + a);

            string line = Assert.Single(lines);
            Assert.StartsWith("08:12", line);
            Assert.Contains("2", line);
            Assert.Contains("4", line);
        }

        [Fact]
        public void FormatLines_PerkUnlocked_UsesPerkLabel_UnknownIdDegrades()
        {
            var entries = new List<DigestEntry>
            {
                new DigestEntry(DigestKind.PerkUnlocked, 231, 0, (int)PerkId.InfiniteStamina, 0),
                new DigestEntry(DigestKind.PerkUnlocked, 232, 0, 200, 0),
            };
            List<string> lines = ResultDigestText.FormatLines(entries, a => "P" + a);

            Assert.Equal(2, lines.Count);
            Assert.Contains(Texts.Get(TextId.GaugePerkStaminaLabel), lines[0]);
            Assert.Contains("?", lines[1]);
        }

        [Fact]
        public void FormatLines_InformantAndFinalBalance_Formatted()
        {
            var entries = new List<DigestEntry>
            {
                new DigestEntry(DigestKind.InformantEstablished, 100, 0, 0, 0),
                new DigestEntry(DigestKind.FinalBalance, 300, 9_500, 12_000, 8_000),
            };
            List<string> lines = ResultDigestText.FormatLines(entries, a => "P" + a);

            Assert.Equal(2, lines.Count);
            Assert.Equal(Texts.Get(TextId.DigestInformant), lines[0].Substring("01:40  ".Length));
            Assert.Contains("12000", lines[1].Replace(",", ""));
            Assert.Contains("8000", lines[1].Replace(",", ""));
            Assert.Contains("9500", lines[1].Replace(",", ""));
        }

        [Fact]
        public void FormatLines_UnknownKindOrReason_DegradesSafely()
        {
            var entries = new List<DigestEntry>
            {
                new DigestEntry((DigestKind)200, 10, 0, 0, 0),
                new DigestEntry(DigestKind.MatchEnd, 20, 0,
                    (int)Team.Villagers, ResultDigest.ReasonUnknown),
            };
            List<string> lines = ResultDigestText.FormatLines(entries, a => "P" + a);

            string line = Assert.Single(lines);
            Assert.Contains(Texts.Get(TextId.ResultBannerVillagerWin), line);
        }
    }
}
