using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Werewolf.Core;
using Werewolf.Scenarios.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ScenarioGameConfigTests
    {
        [Fact]
        public void Build_DisablesTheMeetingScatterForEveryScenario()
        {
            foreach (var entry in ScenarioCatalog.Entries.Where(e => e.Implemented))
            {
                var config = ScenarioGameConfig.Build(
                    new GameConfig { MeetingScatterEnabled = true }, entry.Definition);
                Assert.False(config.MeetingScatterEnabled, entry.Id.ToString());
            }
        }

        private const int PlayerActor = 1;

        private static ScenarioDefinition Def => MeetingScenarioScript.Definition;

        private static List<WPlayer> BuildRoster()
        {
            var players = new List<WPlayer>
            {
                new WPlayer { ActorNumber = PlayerActor, Name = "Player" },
            };
            players.AddRange(Def.Cast.Bots.Select(b => new WPlayer
            {
                ActorNumber = b.ActorNumber,
                Name = b.Name,
                IsBot = true,
            }));
            return players;
        }

        private static Dictionary<int, Role> BuildForcedRoles()
        {
            var forced = Def.Cast.Bots.ToDictionary(b => b.ActorNumber, b => b.Role);
            forced[PlayerActor] = Def.Cast.PlayerRole;
            return forced;
        }

        [Fact]
        public void Build_ClearsVariantChancesAndTimeLimits()
        {
            var local = new GameConfig
            {
                WerewolfModeEnabled = false,
                DebugMode = true,
                WerewolfCount = 3,
                BlackCatChancePercent = 100,
                BomberChancePercent = 100,
                ShamanChancePercent = 100,
                RoundSeconds = 60,
                MeetingDurationSec = 30,
                VoteTimeCutEnabled = true,
                ConveneSuppressStartSec = 60,
                ConveneSuppressAfterSec = 60,
                GameOverAutoReturnSec = 30,
            };

            var config = ScenarioGameConfig.Build(local, Def);

            Assert.True(config.WerewolfModeEnabled);
            Assert.False(config.DebugMode);
            Assert.Equal(1, config.WerewolfCount);
            Assert.Equal(0, config.BlackCatChancePercent);
            Assert.Equal(0, config.BomberChancePercent);
            Assert.Equal(0, config.ShamanChancePercent);
            Assert.Equal(ScenarioGameConfig.RoundSeconds, config.RoundSeconds);
            Assert.Equal(ScenarioGameConfig.MeetingSeconds, config.MeetingDurationSec);
            Assert.False(config.VoteTimeCutEnabled);
            Assert.Equal(0, config.ConveneSuppressStartSec);
            Assert.Equal(0, config.ConveneSuppressAfterSec);
            Assert.Equal(0, config.GameOverAutoReturnSec);
        }

        private static readonly HashSet<string> LocalSettingFields = new HashSet<string>
        {
            nameof(GameConfig.ButtonOffsetX), nameof(GameConfig.ButtonOffsetY),
            nameof(GameConfig.ButtonOffsetZ), nameof(GameConfig.ButtonYaw),
            nameof(GameConfig.ButtonPitch), nameof(GameConfig.ToastDurationSec),
            nameof(GameConfig.NecroVoiceVolume), nameof(GameConfig.NecroVoiceLowPassCutoffHz),
            nameof(GameConfig.NecroVoiceEchoDelayMs), nameof(GameConfig.NecroVoiceEchoDecay),
            nameof(GameConfig.NecroVoiceReverbRoom), nameof(GameConfig.NecroVoiceReverbRoomHF),
            nameof(GameConfig.NecroVoiceReverbDecayTime), nameof(GameConfig.NecroVoiceReverbDecayHFRatio),
            nameof(GameConfig.NecroVoiceReverbReflections), nameof(GameConfig.NecroVoiceReverbReflectionsDelay),
            nameof(GameConfig.NecroVoiceReverbLevel), nameof(GameConfig.NecroVoiceReverbDelay),
            nameof(GameConfig.NecroVoiceReverbDiffusion), nameof(GameConfig.NecroVoiceReverbDensity),
            nameof(GameConfig.NecroVoiceReverbHFReference),
        };

        private static IEnumerable<FieldInfo> ConfigFields()
            => typeof(GameConfig).GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly && !f.IsLiteral);

        private static object Perturb(FieldInfo field, object current)
        {
            Type t = field.FieldType;
            if (t == typeof(int)) return (int)current + 7;
            if (t == typeof(float)) return (float)current + 7f;
            if (t == typeof(bool)) return !(bool)current;
            if (t == typeof(string)) return (string)current + "x";
            if (t.IsEnum)
            {
                return Enum.GetValues(t).Cast<object>().First(v => !v.Equals(current));
            }
            throw new InvalidOperationException($"unsupported field type: {field.Name} ({t.Name})");
        }

        [Fact]
        public void Build_TakesRoomSettingsFromTheDefaultsNotTheLocalSnapshot()
        {
            var local = new GameConfig();
            foreach (FieldInfo field in ConfigFields())
            {
                field.SetValue(local, Perturb(field, field.GetValue(local)));
            }

            foreach (var entry in ScenarioCatalog.Entries.Where(e => e.Implemented))
            {
                GameConfig fromLocal = ScenarioGameConfig.Build(local, entry.Definition);
                GameConfig fromDefaults = ScenarioGameConfig.Build(new GameConfig(), entry.Definition);

                foreach (FieldInfo field in ConfigFields())
                {
                    object expected = LocalSettingFields.Contains(field.Name)
                        ? field.GetValue(local)
                        : field.GetValue(fromDefaults);
                    Assert.Equal(expected, field.GetValue(fromLocal));
                }
            }
        }

        [Fact]
        public void Build_StartsFromTheDistributionDefaultsAndDoesNotTouchTheLocalSnapshot()
        {
            var local = new GameConfig { HealIntervalSec = 1, StartLevelNumber = 30 };
            var defaults = new GameConfig();

            GameConfig config = ScenarioGameConfig.Build(local, Def);

            Assert.NotSame(local, config);
            Assert.Equal(1, local.HealIntervalSec);
            Assert.Equal(defaults.HealIntervalSec, config.HealIntervalSec);
            Assert.Equal(defaults.StartLevelNumber, config.StartLevelNumber);
        }

        [Fact]
        public void Build_WithoutALocalSnapshotUsesTheDefaultsForLocalSettingsToo()
        {
            var defaults = new GameConfig();
            GameConfig config = ScenarioGameConfig.Build(null, Def);

            Assert.Equal(defaults.ToastDurationSec, config.ToastDurationSec);
            Assert.Equal(defaults.NecroVoiceVolume, config.NecroVoiceVolume);
            Assert.True(config.WerewolfModeEnabled);
        }

        [Fact]
        public void Build_KeepsVariantsOutOfReachOfTheEligibilityGate()
        {
            var config = ScenarioGameConfig.Build(new GameConfig
            {
                BlackCatChancePercent = 100,
                BomberChancePercent = 100,
            }, Def);

            Assert.False(config.BlackCatPossible(BuildRoster().Count));
            Assert.False(config.BomberPossible(BuildRoster().Count));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(12345)]
        [InlineData(987654)]
        public void Assign_IsDeterministicRegardlessOfSeed(int seed)
        {
            var players = BuildRoster();
            var config = ScenarioGameConfig.Build(new GameConfig(), Def);

            RoleAssigner.Assign(players, config, new Random(seed), BuildForcedRoles());

            Assert.Equal(Def.Cast.PlayerRole, players.Single(p => p.ActorNumber == PlayerActor).Role);
            foreach (ScenarioParticipant bot in Def.Cast.Bots)
            {
                Assert.Equal(bot.Role, players.Single(p => p.ActorNumber == bot.ActorNumber).Role);
            }
            Assert.Single(players, p => p.Role == Role.Werewolf);
            Assert.DoesNotContain(players, p => p.Role == Role.BlackCat);
            Assert.DoesNotContain(players, p => p.Role == Role.Bomber);
            Assert.DoesNotContain(players, p => p.Role == Role.Shaman);
        }

        [Fact]
        public void TimeLimitDisabled_RoundTimerNeverExpires()
        {
            var config = ScenarioGameConfig.Build(new GameConfig(), Def);
            var timer = new RoundTimer();
            timer.Start(0L, config.RoundSeconds, config.TimeLimitDisabled);

            Assert.True(timer.Unlimited);
            Assert.Equal(config.RoundSeconds * 1000L, timer.EndUnixMs);
            Assert.False(timer.CheckExpiry(timer.EndUnixMs));
            Assert.False(timer.CheckExpiry(timer.EndUnixMs * 100));
            Assert.False(timer.Expired);
        }

        [Fact]
        public void TimeLimitDisabled_MeetingTimerNeitherExpiresNorShrinks()
        {
            var config = ScenarioGameConfig.Build(new GameConfig(), Def);
            var timer = new MeetingTimer();
            timer.Start(0L, config.MeetingDurationSec, config.TimeLimitDisabled);
            long end = timer.EndUnixMs;

            Assert.True(timer.Unlimited);
            Assert.False(timer.IsExpired(end));
            Assert.False(timer.IsExpired(end * 100));
            Assert.Equal(end, timer.ReduceByVote(5, 1_000L));
            Assert.Equal(end, timer.EndUnixMs);
        }

        [Fact]
        public void TimeLimitEnabled_KeepsNormalExpiry()
        {
            var round = new RoundTimer();
            round.Start(0L, 10);
            Assert.False(round.Unlimited);
            Assert.True(round.CheckExpiry(10_000L));

            var meeting = new MeetingTimer();
            meeting.Start(0L, 60);
            Assert.False(meeting.Unlimited);
            Assert.True(meeting.IsExpired(60_000L));
            Assert.True(meeting.ReduceByVote(2, 0L) < 60_000L);
        }

        [Fact]
        public void Hud_ShowsNoTimeLimitBannerInsteadOfTheCountdown()
        {
            var model = new HudModel();
            long now = 1_000_000L;
            long roundEnd = now + ScenarioGameConfig.RoundSeconds * 1000L;

            HudState tutorial = model.Compute(new HudInput(
                GamePhase.Play, Role.Villager, roundEnd, null, false, 0, now,
                scenarioSession: true));

            Assert.False(tutorial.ShowTimer);
            Assert.True(tutorial.ShowScenarioBanner);
            Assert.False(tutorial.TimerAlert);
            Assert.Equal(0, tutorial.BellMarkSec);
        }

        [Fact]
        public void Hud_KeepsTheCountdownForNormalRuns()
        {
            var model = new HudModel();
            long now = 1_000_000L;

            HudState normal = model.Compute(new HudInput(
                GamePhase.Play, Role.Villager, now + 60_000L, null, false, 0, now));

            Assert.True(normal.ShowTimer);
            Assert.False(normal.ShowScenarioBanner);
        }

        [Fact]
        public void Hud_ScenarioSessionNeverRingsTheDeadlineBell()
        {
            var model = new HudModel();
            long now = 1_000_000L;
            long roundEnd = now + BellSchedule.AlertThresholdSec * 1000L;

            HudState state = model.Compute(new HudInput(
                GamePhase.Play, Role.Villager, roundEnd, null, false, 0, now,
                scenarioSession: true));

            Assert.Equal(0, state.BellMarkSec);
            Assert.Equal(0f, state.BellVolumeScale);
        }

        [Fact]
        public void ParticipantIds_MatchTheScriptRoster()
        {
            int[] order = ParticipantIds.AssignOrder(BuildRoster());

            Assert.Equal(PlayerActor, order[MeetingScenarioScript.PlayerParticipantId - 1]);
            foreach (ScenarioParticipant bot in Def.Cast.Bots)
            {
                Assert.Equal(bot.ActorNumber, order[bot.ParticipantId - 1]);
            }
        }
    }
}
