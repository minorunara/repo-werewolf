using System.Collections.Generic;
using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayPaceTests
    {
        private static ReplayPace Pace(
            List<(double, double)> meetings = null, List<double> chats = null)
            => new ReplayPace(
                meetings ?? new List<(double, double)> { (100.0, 200.0) },
                chats ?? new List<double> { 110.0, 130.0 });

        [Fact]
        public void ZoneAt_ExploreOutsideMeetings()
        {
            ReplayPace pace = Pace();
            Assert.Equal(ReplayPaceZone.Explore, pace.ZoneAt(50.0));
            Assert.Equal(ReplayPaceZone.Explore, pace.ZoneAt(200.0));
            Assert.Equal(ReplayPaceZone.Explore, pace.ZoneAt(500.0));
        }

        [Fact]
        public void ZoneAt_TalkWithin5SecOfMeetingStartOrLastChat()
        {
            ReplayPace pace = Pace();
            Assert.Equal(ReplayPaceZone.MeetingTalk, pace.ZoneAt(100.0));
            Assert.Equal(ReplayPaceZone.MeetingTalk, pace.ZoneAt(104.9));
            Assert.Equal(ReplayPaceZone.MeetingSilent, pace.ZoneAt(105.0));
            Assert.Equal(ReplayPaceZone.MeetingTalk, pace.ZoneAt(110.0));
            Assert.Equal(ReplayPaceZone.MeetingTalk, pace.ZoneAt(114.9));
            Assert.Equal(ReplayPaceZone.MeetingSilent, pace.ZoneAt(115.0));
            Assert.Equal(ReplayPaceZone.MeetingTalk, pace.ZoneAt(130.5));
            Assert.Equal(ReplayPaceZone.MeetingSilent, pace.ZoneAt(140.0));
        }

        [Fact]
        public void ZoneAt_ZeroChatMeeting_TurnsSilentAfter5Sec()
        {
            ReplayPace pace = Pace(chats: new List<double>());
            Assert.Equal(ReplayPaceZone.MeetingTalk, pace.ZoneAt(102.0));
            Assert.Equal(ReplayPaceZone.MeetingSilent, pace.ZoneAt(105.0));
            Assert.Equal(ReplayPaceZone.MeetingSilent, pace.ZoneAt(199.0));
        }

        [Theory]
        [InlineData(50.0, false, 8f)]
        [InlineData(50.0, true, 32f)]
        [InlineData(101.0, false, 4f)]
        [InlineData(101.0, true, 16f)]
        [InlineData(106.0, false, 16f)]
        [InlineData(106.0, true, 32f)]
        public void SpeedAt_MatchesAdrTable(double t, bool fast, float expected)
        {
            Assert.Equal(expected, Pace().SpeedAt(t, fast), 3);
        }

        [Fact]
        public void Advance_ClampsAtChatTime_AndDiscardsRemainder()
        {
            ReplayPace pace = Pace();
            Assert.Equal(110.0, pace.Advance(100.0, 10.0, fast: false), 6);
        }

        [Fact]
        public void Advance_DoesNotPassTwoChatsInOneCall()
        {
            ReplayPace pace = Pace();
            double t1 = pace.Advance(109.0, 100.0, fast: false);
            Assert.Equal(110.0, t1, 6);
            double t2 = pace.Advance(t1, 100.0, fast: false);
            Assert.Equal(130.0, t2, 6);
        }

        [Fact]
        public void Advance_SwitchesSpeedAtSilenceOnsetWithinOneCall()
        {
            ReplayPace pace = Pace(chats: new List<double>());
            Assert.Equal(109.0, pace.Advance(100.0, 1.5, fast: false), 6);
        }

        [Fact]
        public void Advance_CrossesMeetingEndWithoutStopping()
        {
            ReplayPace pace = Pace(chats: new List<double>());
            double result = pace.Advance(199.0, 1.0625, fast: false);
            Assert.Equal(208.0, result, 6);
        }

        [Fact]
        public void Advance_ExploreEntersMeetingAtTalkSpeed()
        {
            ReplayPace pace = Pace(chats: new List<double>());
            Assert.Equal(102.0, pace.Advance(96.0, 1.0, fast: false), 6);
        }

        [Fact]
        public void Advance_NonPositiveDt_NoOp()
        {
            ReplayPace pace = Pace();
            Assert.Equal(120.0, pace.Advance(120.0, 0.0, false), 6);
            Assert.Equal(120.0, pace.Advance(120.0, -1.0, false), 6);
        }

        [Fact]
        public void RealSecondsBetween_IntegratesPiecewise()
        {
            ReplayPace pace = Pace();
            Assert.Equal(1.5625, pace.RealSecondsBetween(100.0, 110.0, false), 6);
            Assert.Equal(1.5625, pace.RealSecondsBetween(110.0, 120.0, false), 6);
            Assert.Equal(0.0, pace.RealSecondsBetween(120.0, 110.0, false), 6);
            Assert.Equal(0.0, pace.RealSecondsBetween(110.0, 110.0, false), 6);
        }

        [Fact]
        public void RealSecondsBetween_FastTier()
        {
            ReplayPace pace = Pace();
            Assert.Equal(0.46875, pace.RealSecondsBetween(100.0, 110.0, true), 6);
        }

        [Fact]
        public void NoMeetings_AlwaysExplore()
        {
            var pace = new ReplayPace(new List<(double, double)>(), new List<double>());
            Assert.Equal(ReplayPaceZone.Explore, pace.ZoneAt(0.0));
            Assert.Equal(80.0, pace.Advance(0.0, 10.0, fast: false), 6);
            Assert.Equal(1.25, pace.RealSecondsBetween(0.0, 10.0, false), 6);
        }
    }
}
