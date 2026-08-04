using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ShamanSenseTests
    {
        private const float Dt = 0.1f;

        private static bool TickFor(ShamanSense sense, float seconds, bool inView,
            bool suspend = false, bool stationary = true)
        {
            bool fired = false;
            int steps = (int)Math.Round(seconds / Dt);
            for (int i = 0; i < steps; i++)
            {
                fired |= sense.TickGaze(inView, stationary, Dt, suspend, out _);
            }
            return fired;
        }

        private static int DripsDuring(ShamanSense sense, float seconds, bool inView,
            bool suspend = false, bool stationary = true)
        {
            int drips = 0;
            int steps = (int)Math.Round(seconds / Dt);
            for (int i = 0; i < steps; i++)
            {
                sense.TickGaze(inView, stationary, Dt, suspend, out bool drip);
                if (drip) drips++;
            }
            return drips;
        }

        [Fact]
        public void Gaze_FiresOnceWhenHeldFullSeconds()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);

            Assert.False(TickFor(sense, ShamanSense.TranceEntrySec + 4.6f, inView: true));
            Assert.False(sense.GhostVisible);
            Assert.True(TickFor(sense, 0.6f, inView: true));
            Assert.True(sense.GhostVisible);
        }

        [Fact]
        public void Gaze_MovingWithCorpseInView_DoesNotAccumulate()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);
            Assert.False(TickFor(sense, 30f, inView: true, stationary: false));
            Assert.False(sense.GhostVisible);
            Assert.False(sense.TranceActive);
        }

        [Fact]
        public void Trance_EntryRequiresContinuousStillness()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);

            TickFor(sense, 0.6f, inView: false);
            Assert.False(sense.TranceActive);
            TickFor(sense, 0.2f, inView: false, stationary: false);
            TickFor(sense, 0.8f, inView: false);
            Assert.False(sense.TranceActive);
            TickFor(sense, 0.4f, inView: false);
            Assert.True(sense.TranceActive);
        }

        [Fact]
        public void Gaze_DecaysAtDoubleSpeedWhileOutOfView()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);

            Assert.False(TickFor(sense, ShamanSense.TranceEntrySec + 3f, inView: true));
            Assert.False(TickFor(sense, 1f, inView: false));
            Assert.False(TickFor(sense, 3.8f, inView: true));
            Assert.True(TickFor(sense, 0.4f, inView: true));
        }

        [Fact]
        public void Gaze_SweepingGlances_DoNotAccumulate()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);
            for (int i = 0; i < 200; i++)
            {
                Assert.False(sense.TickGaze(corpseInView: true, stationary: true, Dt, suspend: false, out _));
                Assert.False(TickFor(sense, 0.3f, inView: false));
            }
            Assert.False(sense.GhostVisible);
        }

        [Fact]
        public void Gaze_CooldownStartsAfterDisplayEnds_ThenRecharges()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);
            Assert.True(TickFor(sense, ShamanSense.TranceEntrySec + 5.2f, inView: true));

            Assert.False(TickFor(sense, ShamanSense.GhostDisplaySec, inView: true));
            Assert.False(sense.GhostVisible);

            Assert.False(TickFor(sense, 9.8f, inView: true));

            Assert.True(TickFor(sense, 5.6f, inView: true));
        }

        [Fact]
        public void Gaze_Suspend_ClearsGhostAndGauge()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);

            Assert.False(TickFor(sense, ShamanSense.TranceEntrySec + 4.5f, inView: true));
            Assert.False(TickFor(sense, 1f, inView: true, suspend: true));
            Assert.False(TickFor(sense, ShamanSense.TranceEntrySec + 4.8f, inView: true));
            Assert.True(TickFor(sense, 0.4f, inView: true));

            Assert.True(sense.GhostVisible);
            sense.TickGaze(corpseInView: false, stationary: false, Dt, suspend: true, out _);
            Assert.False(sense.GhostVisible);
        }

        [Fact]
        public void Gaze_SuppressedWhileStormTierActive()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);
            Assert.Equal(ShamanStormTier.Strong, sense.TickStorm(5f, false, 30f, 20f, 10f));

            Assert.False(TickFor(sense, 20f, inView: true));
            Assert.False(sense.GhostVisible);

            Assert.Equal(ShamanStormTier.None, sense.TickStorm(40f, false, 30f, 20f, 10f));
            Assert.True(TickFor(sense, 5.6f, inView: true));
        }

        [Fact]
        public void Gaze_GaugeDecaysWhileInStorm()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);

            Assert.False(TickFor(sense, ShamanSense.TranceEntrySec + 4f, inView: true));
            Assert.Equal(ShamanStormTier.Weak, sense.TickStorm(25f, false, 30f, 20f, 10f));
            Assert.False(TickFor(sense, 2.5f, inView: true));
            Assert.Equal(ShamanStormTier.None, sense.TickStorm(40f, false, 30f, 20f, 10f));
            Assert.False(TickFor(sense, 4.8f, inView: true));
            Assert.True(TickFor(sense, 0.4f, inView: true));
        }

        [Fact]
        public void Gaze_GhostMidDisplay_ContinuesInsideStorm()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);
            Assert.True(TickFor(sense, ShamanSense.TranceEntrySec + 5.2f, inView: true));
            Assert.True(sense.GhostVisible);

            Assert.Equal(ShamanStormTier.Strong, sense.TickStorm(3f, false, 30f, 20f, 10f));
            Assert.False(TickFor(sense, ShamanSense.GhostDisplaySec - 0.5f, inView: true));
            Assert.True(sense.GhostVisible);
            Assert.False(TickFor(sense, 0.6f, inView: true));
            Assert.False(sense.GhostVisible);
        }

        [Fact]
        public void Drip_FirstFiresOneFullPeriodAfterEntry_ThenEveryGazeFullSec()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);

            Assert.Equal(0, DripsDuring(sense, ShamanSense.TranceEntrySec + 4.5f, inView: false));
            Assert.Equal(1, DripsDuring(sense, 1.0f, inView: false));
            Assert.Equal(0, DripsDuring(sense, 4.2f, inView: false));
            Assert.Equal(1, DripsDuring(sense, 1.0f, inView: false));
        }

        [Fact]
        public void Drip_MovementResets_RequiresFullPeriodAgain()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);

            Assert.Equal(1, DripsDuring(sense, ShamanSense.TranceEntrySec + 5.5f, inView: false));
            Assert.Equal(0, DripsDuring(sense, 0.3f, inView: false, stationary: false));
            Assert.Equal(0, DripsDuring(sense, ShamanSense.TranceEntrySec + 4.5f, inView: false));
            Assert.Equal(1, DripsDuring(sense, 1.0f, inView: false));
        }

        [Fact]
        public void Drip_SilentInsideStormTier()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);
            Assert.Equal(ShamanStormTier.Strong, sense.TickStorm(5f, false, 30f, 20f, 10f));

            Assert.Equal(0, DripsDuring(sense, 3f, inView: false));

            Assert.Equal(ShamanStormTier.None, sense.TickStorm(40f, false, 30f, 20f, 10f));
            Assert.Equal(0, DripsDuring(sense, 4.5f, inView: false));
            Assert.Equal(1, DripsDuring(sense, 1.0f, inView: false));
        }

        [Fact]
        public void Drip_SilentDuringGhostDisplayAndCooldown_ResumesAfterFullPeriod()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);
            Assert.True(TickFor(sense, ShamanSense.TranceEntrySec + 5.4f, inView: true));

            Assert.Equal(0, DripsDuring(sense, ShamanSense.GhostDisplaySec + 9.5f, inView: false));

            Assert.Equal(0, DripsDuring(sense, 4.8f, inView: false));
            Assert.True(DripsDuring(sense, 1.0f, inView: false) >= 1);
        }

        [Fact]
        public void GazeArmed_TracksTranceStormDisplayAndCooldown()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);

            Assert.False(sense.GazeArmed);
            TickFor(sense, ShamanSense.TranceEntrySec + 0.2f, inView: false);
            Assert.True(sense.GazeArmed);

            sense.TickStorm(25f, false, 30f, 20f, 10f);
            Assert.False(sense.GazeArmed);
            sense.TickStorm(40f, false, 30f, 20f, 10f);
            Assert.True(sense.GazeArmed);

            Assert.True(TickFor(sense, 5.6f, inView: true));
            Assert.False(sense.GazeArmed);
            TickFor(sense, ShamanSense.GhostDisplaySec + 0.2f, inView: false);
            Assert.False(sense.GazeArmed);
            TickFor(sense, 10f, inView: false);
            Assert.True(sense.GazeArmed);
        }

        [Fact]
        public void GazeArmed_MovementDisarms()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);
            TickFor(sense, ShamanSense.TranceEntrySec + 0.2f, inView: false);
            Assert.True(sense.GazeArmed);

            sense.TickGaze(corpseInView: false, stationary: false, Dt, suspend: false, out _);
            Assert.False(sense.GazeArmed);
        }

        [Fact]
        public void BeginCooldown_BlocksGazeAndDripAndArm_UntilElapsed()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);
            sense.BeginCooldown();

            Assert.Equal(0, DripsDuring(sense, 9.5f, inView: true));
            Assert.False(sense.GhostVisible);
            Assert.False(sense.GazeArmed);

            Assert.True(TickFor(sense, 5.6f, inView: true));
        }

        [Fact]
        public void GhostVisible_RectangularWindow_OnImmediatelyOffAfterDisplaySec()
        {
            var sense = new ShamanSense(gazeFullSec: 5f, cooldownSec: 10f);

            TickFor(sense, ShamanSense.TranceEntrySec + 0.2f, inView: false);

            Assert.True(TickFor(sense, 5.2f, inView: true));
            Assert.True(sense.GhostVisible);

            Assert.False(TickFor(sense, ShamanSense.GhostDisplaySec * 0.8f, inView: false));
            Assert.True(sense.GhostVisible);
            Assert.False(TickFor(sense, ShamanSense.GhostDisplaySec * 0.3f, inView: false));
            Assert.False(sense.GhostVisible);
        }

        [Theory]
        [InlineData(35f, ShamanStormTier.None)]
        [InlineData(25f, ShamanStormTier.Weak)]
        [InlineData(15f, ShamanStormTier.Medium)]
        [InlineData(5f, ShamanStormTier.Strong)]
        public void Storm_TierByDistance(float distance, ShamanStormTier expected)
        {
            var sense = new ShamanSense(5f, 10f);
            Assert.Equal(expected, sense.TickStorm(distance, suspend: false, 30f, 20f, 10f));
        }

        [Fact]
        public void Storm_ExitHysteresis_KeepsTierWithinMargin()
        {
            var sense = new ShamanSense(5f, 10f);
            Assert.Equal(ShamanStormTier.Medium, sense.TickStorm(19f, false, 30f, 20f, 10f));

            Assert.Equal(ShamanStormTier.Medium, sense.TickStorm(20.9f, false, 30f, 20f, 10f));
            Assert.Equal(ShamanStormTier.Weak, sense.TickStorm(21.2f, false, 30f, 20f, 10f));
        }

        [Fact]
        public void Storm_PromotionIsImmediate_NoCorpseOrSuspendIsNone()
        {
            var sense = new ShamanSense(5f, 10f);
            Assert.Equal(ShamanStormTier.Strong, sense.TickStorm(3f, false, 30f, 20f, 10f));
            Assert.Equal(ShamanStormTier.None, sense.TickStorm(null, false, 30f, 20f, 10f));
            Assert.Equal(ShamanStormTier.Strong, sense.TickStorm(3f, false, 30f, 20f, 10f));
            Assert.Equal(ShamanStormTier.None, sense.TickStorm(3f, suspend: true, 30f, 20f, 10f));
        }

        [Fact]
        public void EncodeShaman_ConvertsSecondsToMs_MetersToCm()
        {
            var config = new GameConfig
            {
                ShamanGazeFullSec = 7,
                ShamanGhostCooldownSec = 12,
                ShamanStormWeakMeters = 33,
                ShamanStormMediumMeters = 22,
                ShamanStormStrongMeters = 11,
            };

            int[] packed = RoomStateKeys.EncodeShaman(config);

            Assert.Equal(RoomStateKeys.ShamanIndex.Length, packed.Length);
            Assert.Equal(7000, packed[RoomStateKeys.ShamanIndex.GazeFullMs]);
            Assert.Equal(12000, packed[RoomStateKeys.ShamanIndex.GhostCooldownMs]);
            Assert.Equal(3300, packed[RoomStateKeys.ShamanIndex.StormWeakCm]);
            Assert.Equal(2200, packed[RoomStateKeys.ShamanIndex.StormMediumCm]);
            Assert.Equal(1100, packed[RoomStateKeys.ShamanIndex.StormStrongCm]);
        }

        [Fact]
        public void WinJudge_AliveShaman_PreventsVillagersEradicated()
        {
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = 1, Role = Role.Werewolf },
                new WPlayer { ActorNumber = 2, Role = Role.Shaman },
                new WPlayer { ActorNumber = 3, Role = Role.Villager, Alive = false },
            };

            Assert.Null(WinJudge.Judge(players));

            players[1].Alive = false;
            WinResult result = WinJudge.Judge(players);
            Assert.NotNull(result);
            Assert.Equal(Team.Werewolves, result.WinningTeam);
            Assert.Equal(WinReason.VillagersEradicated, result.Reason);
        }

        [Fact]
        public void RevealScript_Shaman_TwoPages_WithAbilityHeading()
        {
            RevealContent content = RevealScript.Build(
                Role.Shaman, Array.Empty<string>(), blackCatPossible: true,
                ValuableMapMode.MeetingSync);

            Assert.Equal(RoleIcon.Shaman, content.Icon);
            Assert.Equal(2, content.Pages.Length);
            Assert.Contains(content.Pages[0].BodyLines,
                l => l == Texts.Get(TextId.RevealHeadingWinCondition));
            Assert.Contains(content.Pages[1].BodyLines,
                l => l == Texts.Get(TextId.RevealHeadingAbility));
            Assert.Equal(3, content.Pages[1].BodyLines.Length);
        }
    }
}
