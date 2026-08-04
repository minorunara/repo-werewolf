using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class GaugeMarkerLayoutTests
    {
        private static MeetingGaugeSnapshot Snap(
            int stamina = 0, int jump = 0, int enemyIgnore = 0, int informant = 0, int beacon = 0)
            => new MeetingGaugeSnapshot
            {
                RatioPermille = 0,
                BaseDollars = 10000,
                StaminaPct = stamina,
                JumpPct = jump,
                EnemyIgnorePct = enemyIgnore,
                InformantPct = informant,
                BeaconChargePct = beacon,
            };

        [Fact]
        public void Build_PerksAscendingWithIconKeys()
        {
            var markers = GaugeMarkerLayout.Build(
                Snap(stamina: 30, jump: 50, enemyIgnore: 70, informant: 90), permille: 0, minGapPct: 3);

            var perks = markers.Where(m => m.Kind == GaugeMarkerKind.Perk).ToList();
            Assert.Equal(new[] { 30, 50, 70, 90 }, perks.Select(m => m.Pct));
            Assert.Equal(
                new[] { "perk_stamina", "perk_jump", "perk_enemy_ignore", "perk_informant" },
                perks.Select(m => m.IconKey));
            Assert.All(perks, m => Assert.False(string.IsNullOrEmpty(m.Label)));
        }

        [Fact]
        public void Build_SamePctKeepsInsertionOrder()
        {
            var markers = GaugeMarkerLayout.Build(
                Snap(stamina: 40, jump: 40), permille: 0, minGapPct: 3);

            var perks = markers.Where(m => m.Kind == GaugeMarkerKind.Perk).ToList();
            Assert.Equal(new[] { "perk_stamina", "perk_jump" }, perks.Select(m => m.IconKey));
        }

        [Fact]
        public void Build_OutOfRangeThresholdsExcluded()
        {
            var markers = GaugeMarkerLayout.Build(
                Snap(stamina: 0, jump: 101, enemyIgnore: -5, informant: 100), permille: 0, minGapPct: 3);

            var perks = markers.Where(m => m.Kind == GaugeMarkerKind.Perk).ToList();
            Assert.Single(perks);
            Assert.Equal(100, perks[0].Pct);
        }

        [Fact]
        public void Build_UnlockedByPermille()
        {
            var markers = GaugeMarkerLayout.Build(
                Snap(stamina: 30, jump: 50, beacon: 20), permille: 400, minGapPct: 3);

            foreach (GaugeMarker m in markers.Where(m => m.Kind == GaugeMarkerKind.Perk))
            {
                Assert.Equal(m.Pct <= 40, m.Unlocked);
            }
            Assert.All(markers.Where(m => m.Kind == GaugeMarkerKind.Scale),
                m => Assert.False(m.Unlocked));
        }

        [Fact]
        public void Build_DensePerksBumpToTier1()
        {
            var markers = GaugeMarkerLayout.Build(
                Snap(stamina: 40, jump: 41), permille: 0, minGapPct: 3);

            var perks = markers.Where(m => m.Kind == GaugeMarkerKind.Perk).ToList();
            Assert.Equal(0, perks[0].Tier);
            Assert.Equal(1, perks[1].Tier);
        }

        [Fact]
        public void Build_ThirdDenseMarkerFallsBackToTier0()
        {
            var markers = GaugeMarkerLayout.Build(
                Snap(stamina: 40, jump: 41, enemyIgnore: 42), permille: 0, minGapPct: 3);

            var perks = markers.Where(m => m.Kind == GaugeMarkerKind.Perk).ToList();
            Assert.Equal(new[] { 0, 1, 0 }, perks.Select(m => m.Tier));
        }

        [Fact]
        public void Build_SeparatedPerksStayOnTier0()
        {
            var markers = GaugeMarkerLayout.Build(
                Snap(stamina: 20, jump: 50, enemyIgnore: 80), permille: 0, minGapPct: 3);

            Assert.All(markers.Where(m => m.Kind == GaugeMarkerKind.Perk),
                m => Assert.Equal(0, m.Tier));
        }

        [Fact]
        public void Build_ScaleTicksFixedTenPercentWithoutDecoration()
        {
            var markers = GaugeMarkerLayout.Build(Snap(beacon: 25), permille: 0, minGapPct: 3);

            var ticks = markers.Where(m => m.Kind == GaugeMarkerKind.Scale).ToList();
            Assert.Equal(new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 }, ticks.Select(m => m.Pct));
            Assert.All(ticks, m =>
            {
                Assert.Null(m.IconKey);
                Assert.Null(m.Label);
                Assert.Equal(0, m.Tier);
                Assert.False(m.Unlocked);
            });
        }

        [Fact]
        public void Build_ScaleTicksIndependentOfBeaconSetting()
        {
            var markers = GaugeMarkerLayout.Build(Snap(stamina: 30, beacon: 0), permille: 0, minGapPct: 3);

            Assert.Equal(9, markers.Count(m => m.Kind == GaugeMarkerKind.Scale));
        }

        [Fact]
        public void Build_ScaleTicksDoNotAffectPerkTiers()
        {
            var markers = GaugeMarkerLayout.Build(
                Snap(stamina: 50, beacon: 25), permille: 0, minGapPct: 3);

            GaugeMarker perk = markers.Single(m => m.Kind == GaugeMarkerKind.Perk);
            Assert.Equal(0, perk.Tier);
        }

        [Fact]
        public void Build_NullSnapshotReturnsEmpty()
        {
            List<GaugeMarker> markers = GaugeMarkerLayout.Build(null, permille: 0, minGapPct: 3);
            Assert.Empty(markers);
        }

        [Fact]
        public void UnlockedCount_CountsPerksAtOrBelowPermille()
        {
            var s = Snap(stamina: 30, jump: 50, enemyIgnore: 70, informant: 90, beacon: 25);

            Assert.Equal(0, GaugeMarkerLayout.UnlockedCount(s, 0));
            Assert.Equal(0, GaugeMarkerLayout.UnlockedCount(s, 299));
            Assert.Equal(1, GaugeMarkerLayout.UnlockedCount(s, 300));
            Assert.Equal(4, GaugeMarkerLayout.UnlockedCount(s, 1000));
        }

        [Fact]
        public void UnlockedCount_MatchesBuildUnlockedMarkers()
        {
            var s = Snap(stamina: 30, jump: 50, enemyIgnore: 70, informant: 90, beacon: 20);
            foreach (int permille in new[] { 0, 199, 200, 300, 501, 999, 1000 })
            {
                List<GaugeMarker> markers = GaugeMarkerLayout.Build(s, permille, minGapPct: 3);
                Assert.Equal(markers.Count(m => m.Unlocked), GaugeMarkerLayout.UnlockedCount(s, permille));
            }
        }

        [Fact]
        public void UnlockedCount_NullSnapshotIsZero()
        {
            Assert.Equal(0, GaugeMarkerLayout.UnlockedCount(null, 1000));
        }
    }
}
