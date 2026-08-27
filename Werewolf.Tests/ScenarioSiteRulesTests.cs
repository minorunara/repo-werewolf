using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioSiteRulesTests
    {
        private const float TruckZone = 20f;

        [Fact]
        public void StandSite_PicksTheNearestFloorBeyondTheTruckZone()
        {
            var truck = new List<float> { 10f, 18f, 20f, 22f, 40f };
            Assert.Equal(3, ScenarioSiteRules.PickStandSite(truck, TruckZone));
        }

        [Fact]
        public void StandSite_FallsBackToTheFarthestWhenEverythingIsInsideTheTruckZone()
        {
            Assert.Equal(1, ScenarioSiteRules.PickStandSite(new List<float> { 5f, 20f, 10f }, TruckZone));
        }

        [Fact]
        public void StandSite_ReturnsNoneOnlyWhenThereAreNoCandidates()
        {
            Assert.Equal(-1, ScenarioSiteRules.PickStandSite(new List<float>(), TruckZone));
            Assert.Equal(-1, ScenarioSiteRules.PickStandSite(null, TruckZone));
        }

        [Fact]
        public void FarSite_PicksTheFarthestFromTheStandSite()
        {
            Assert.Equal(3, ScenarioSiteRules.PickFarSite(new List<float> { 5f, 17f, 19f, 30f }));
            Assert.Equal(1, ScenarioSiteRules.PickFarSite(new List<float> { 0f, 2f }));
        }

        [Fact]
        public void FarSite_ReturnsNoneOnlyWhenThereAreNoCandidates()
        {
            Assert.Equal(-1, ScenarioSiteRules.PickFarSite(new List<float>()));
            Assert.Equal(-1, ScenarioSiteRules.PickFarSite(null));
        }

        [Fact]
        public void GroupOffset_KeepsMembersWithinThreeMetersAndNeverOverlaps()
        {
            var offsets = new List<(float, float)>();
            foreach (int id in new[] { 2, 3, 4, 5, 6 }) offsets.Add(ScenarioSiteRules.GroupOffset(id));

            for (int i = 0; i < offsets.Count; i++)
            {
                Assert.Equal(ScenarioSiteRules.GroupSpreadMeters,
                    (float)System.Math.Sqrt(offsets[i].Item1 * offsets[i].Item1
                                            + offsets[i].Item2 * offsets[i].Item2), 3);

                for (int j = i + 1; j < offsets.Count; j++)
                {
                    float dx = offsets[i].Item1 - offsets[j].Item1;
                    float dz = offsets[i].Item2 - offsets[j].Item2;
                    double gap = System.Math.Sqrt(dx * dx + dz * dz);
                    Assert.True(gap <= 3f, $"{i},{j} が3mを超えた: {gap}");
                    Assert.True(gap > 0.1f, $"{i},{j} が重なった: {gap}");
                }
            }

            Assert.Equal(ScenarioSiteRules.GroupOffset(4), ScenarioSiteRules.GroupOffset(4));
        }
    }
}
