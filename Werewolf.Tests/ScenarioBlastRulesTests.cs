using System.Collections.Generic;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioBlastRulesTests
    {
        private const float R = 9f;

        [Fact]
        public void Resolve_HoldsEveryDetonationWhileTheBombSitsOnTheWrongTarget()
        {
            Assert.Equal(BombDenyReason.ScenarioTarget,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.OutsideBlast, 100f, R, false, false, true));
            Assert.Equal(BombDenyReason.ScenarioTarget,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.PointBlank, 0f, R, false, true, true));

            Assert.Equal(BombDenyReason.ScenarioTarget,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.None, 0f, R, false, false, true));

            Assert.Equal(BombDenyReason.None,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.None, 0f, R, false, false, false));
        }

        [Fact]
        public void Resolve_HoldsEveryDetonationWhileTheTargetIsStillWalking()
        {
            Assert.Equal(BombDenyReason.ScenarioTargetMoving,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.OutsideBlast, 100f, R, false, true));
            Assert.Equal(BombDenyReason.ScenarioTargetMoving,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.PointBlank, 0f, R, false, true));

            Assert.Equal(BombDenyReason.ScenarioDistance,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.AwaitTruckZone, 100f, R, true, true));

            Assert.Equal(BombDenyReason.None,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.None, 0f, R, true, true));
        }

        [Fact]
        public void Resolve_FallsBackToTheDistanceRuleOnceTheTargetHasStopped()
        {
            Assert.Equal(BombDenyReason.ScenarioDistance,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.OutsideBlast, R, R, false, false));
            Assert.Equal(BombDenyReason.None,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.OutsideBlast, R + 0.01f, R, false, false));

            Assert.Equal(BombDenyReason.None,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.AwaitTruckZone, 100f, R, true, false));
            Assert.Equal(BombDenyReason.ScenarioDistance,
                ScenarioBlastRules.Resolve(ScenarioBlastRule.AwaitTruckZone, 100f, R, false, false));
        }

        [Fact]
        public void OutsideBlast_RejectsAtOrInsideTheRadiusAndPassesBeyond()
        {
            Assert.True(ScenarioBlastRules.Denies(ScenarioBlastRule.OutsideBlast, R, R, false));
            Assert.True(ScenarioBlastRules.Denies(ScenarioBlastRule.OutsideBlast, R - 0.01f, R, false));
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.OutsideBlast, R + 0.01f, R, false));
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.OutsideBlast, 100f, R, false));
        }

        [Fact]
        public void PointBlank_RejectsBeyondTheCloseRangeEvenInsideTheBlast()
        {
            const float P = ScenarioBlastRules.PointBlankMeters;

            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.PointBlank, P, R, false));
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.PointBlank, P - 0.01f, R, false));
            Assert.True(ScenarioBlastRules.Denies(ScenarioBlastRule.PointBlank, P + 0.01f, R, false));
            Assert.True(ScenarioBlastRules.Denies(ScenarioBlastRule.PointBlank, R, R, false));
            Assert.True(ScenarioBlastRules.Denies(ScenarioBlastRule.PointBlank, 100f, R, false));
        }

        [Fact]
        public void PointBlank_NeverExceedsTheBlastRadius()
        {
            const float Narrow = 2f;

            Assert.Equal(Narrow, ScenarioBlastRules.PointBlankLimit(Narrow));
            Assert.Equal(ScenarioBlastRules.PointBlankMeters, ScenarioBlastRules.PointBlankLimit(R));
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.PointBlank, Narrow, Narrow, false));
            Assert.True(ScenarioBlastRules.Denies(ScenarioBlastRule.PointBlank, Narrow + 0.01f, Narrow, false));
        }

        [Fact]
        public void AwaitTruckZone_RejectsOnlyWhileTheTargetIsOutsideTheTruck()
        {
            Assert.True(ScenarioBlastRules.Denies(ScenarioBlastRule.AwaitTruckZone, 0f, R, false));
            Assert.True(ScenarioBlastRules.Denies(ScenarioBlastRule.AwaitTruckZone, 100f, R, false));
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.AwaitTruckZone, 0f, R, true));
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.AwaitTruckZone, 100f, R, true));
        }

        [Fact]
        public void None_NeverIntervenes()
        {
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.None, 0f, R, false));
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.None, 100f, R, true));
        }

        [Fact]
        public void NonPositiveRadius_DisablesTheDistanceRules()
        {
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.OutsideBlast, 1f, 0f, false));
            Assert.False(ScenarioBlastRules.Denies(ScenarioBlastRule.PointBlank, 1f, 0f, false));
            Assert.True(ScenarioBlastRules.Denies(ScenarioBlastRule.AwaitTruckZone, 1f, 0f, false));
        }

        [Fact]
        public void EachRuleIsSpokenByTheGuideWithItsOwnLines()
        {
            var seen = new HashSet<TextId>();
            foreach (ScenarioBlastRule rule in new[]
                     {
                         ScenarioBlastRule.OutsideBlast, ScenarioBlastRule.PointBlank,
                         ScenarioBlastRule.AwaitTruckZone,
                     })
            {
                IReadOnlyList<TextId> lines = ScenarioBlastRules.DenyLines(rule);
                Assert.True(lines.Count >= 2, rule.ToString());
                foreach (TextId id in lines)
                {
                    Assert.True(seen.Add(id), id.ToString());
                    foreach (Language language in new[] { Language.Japanese, Language.English })
                    {
                        Assert.False(string.IsNullOrWhiteSpace(Texts.TableFor(language)[id]));
                    }
                }
            }

            Assert.NotEmpty(ScenarioBlastRules.DenyLines(ScenarioBlastRule.None));
        }

        [Fact]
        public void DenyLines_NameTheTargetSoTheGuideCanFillItIn()
        {
            foreach (ScenarioBlastRule rule in new[]
                     {
                         ScenarioBlastRule.OutsideBlast, ScenarioBlastRule.PointBlank,
                         ScenarioBlastRule.AwaitTruckZone,
                     })
            {
                foreach (Language language in new[] { Language.Japanese, Language.English })
                {
                    var table = Texts.TableFor(language);
                    Assert.Contains(ScenarioBlastRules.DenyLines(rule),
                        id => table[id].Contains("{0}"));
                }
            }
        }
    }
}
