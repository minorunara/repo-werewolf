using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class SettingsCatalogTests
    {

        private static readonly string[] ExpectedRegisteredIds =
        {
            "WerewolfModeEnabled", "RoundSeconds", "MinimapHideEnabled", "OutfitChangeAllowed", "ValuableMapMode",
            "OrbGaugeEnabled", "NecroVoiceMode", "GameOverAutoReturnSec",
            "MeetingRightsPerPlayer", "ConveneSuppressStartSec", "ConveneSuppressAfterSec",
            "MeetingCountdownSec", "MeetingDurationSec", "VoteTimeCutEnabled", "ResultDisplaySec",
            "MeetingScatterEnabled", "ScatterGuardSec",
            "ShamanChancePercent", "WerewolfCount",
            "BlackCatChancePercent", "BomberChancePercent",
            "ShamanGazeFullSec", "ShamanGhostCooldownSec",
            "ShamanStormWeakMeters", "ShamanStormMediumMeters", "ShamanStormStrongMeters",
            "StaminaUnlockPct", "JumpUnlockPct", "EnemyIgnoreUnlockPct",
            "HealUnlockPct", "HealIntervalSec", "BeaconChargePct",
            "ExtraJumpCount", "BeaconCooldownSec", "BeaconSuppressStartSec",
            "BeaconSuppressAfterMeetingSec",
            "BlackCatRevealDelaySec", "BlackCatCurseEnabled", "InformantThresholdPct",
            "CatGaugeSyncIntervalSec",
            "BomberProximityMeters", "BomberGaugeFullSec", "BomberInitialCooldownSec", "BomberCooldownSec",
            "BomberBlastRadiusMeters",
            "BomberBlastPlayerDamage", "BomberBlastEnemyDamage",
            "BomberAmmoRefillPct",
            "StartLevelNumber", "StartMapName", "StartItemsSpec",
            "StartEnergyPct", "StartUpgradesSpec", "OrbDropMax",
        };

        private static readonly string[] ExpectedExcludedFields =
        {
            "DebugMode",
            "ButtonOffsetX", "ButtonOffsetY", "ButtonOffsetZ", "ButtonYaw", "ButtonPitch",
            "ToastDurationSec",
            "NecroVoiceVolume",
            "NecroVoiceLowPassCutoffHz",
            "NecroVoiceEchoDelayMs", "NecroVoiceEchoDecay",
            "NecroVoiceReverbRoom", "NecroVoiceReverbRoomHF",
            "NecroVoiceReverbDecayTime", "NecroVoiceReverbDecayHFRatio",
            "NecroVoiceReverbReflections", "NecroVoiceReverbReflectionsDelay",
            "NecroVoiceReverbLevel", "NecroVoiceReverbDelay",
            "NecroVoiceReverbDiffusion", "NecroVoiceReverbDensity",
            "NecroVoiceReverbHFReference",
            "BomberWarningSec",
            "BomberTruckSafeRadiusMeters",
            "CurseWaitSec",
        };

        [Fact]
        public void Entries_HasExactRegisteredIds_InSectionCounts()
        {
            var ids = SettingsCatalog.Entries.Select(e => e.SettingId).ToArray();
            Assert.Equal(ExpectedRegisteredIds.Length, ids.Length);
            Assert.Equal(ExpectedRegisteredIds, ids);

            var bySection = SettingsCatalog.Entries
                .GroupBy(e => e.Section)
                .ToDictionary(g => g.Key, g => g.Count());
            Assert.Equal(8, bySection["基本"]);
            Assert.Equal(9, bySection["会議"]);
            Assert.Equal(4, bySection["役職 - 役職配分"]);
            Assert.Equal(10, bySection["役職 - 人狼"]);
            Assert.Equal(4, bySection["役職 - 黒猫"]);
            Assert.Equal(8, bySection["役職 - 爆弾魔"]);
            Assert.Equal(5, bySection["役職 - 祈祷師"]);
            Assert.Equal(6, bySection["開始環境"]);
        }

        [Fact]
        public void Entries_ExcludeRemovedLegacyIds()
        {
            var ids = SettingsCatalog.Entries.Select(e => e.SettingId).ToHashSet();
            Assert.DoesNotContain("WerewolfCountOverride", ids);
            Assert.DoesNotContain("BlackCatOverride", ids);
        }

        [Fact]
        public void GameConfigPublicFields_Equals_RegisteredPlusExcluded_ReflectionParity()
        {
            var fields = typeof(GameConfig)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => f.Name)
                .ToArray();

            var registered = new HashSet<string>(SettingsCatalog.Entries.Select(e => e.SettingId));
            var excluded = new HashSet<string>(ExpectedExcludedFields);
            var union = new HashSet<string>(registered);
            union.UnionWith(excluded);

            Assert.Empty(registered.Intersect(excluded));

            var missing = fields.Where(n => !union.Contains(n)).ToArray();
            var extra = union.Where(n => !fields.Contains(n)).ToArray();
            Assert.True(missing.Length == 0,
                "GameConfig に新規フィールドが増えている（カタログ登録 or 明示除外のどちらかへ追記が必要）: "
                + string.Join(",", missing));
            Assert.True(extra.Length == 0,
                "カタログ登録 or 除外リストが GameConfig に存在しないフィールドを参照している: "
                + string.Join(",", extra));

            Assert.Equal(ExpectedRegisteredIds.Length + ExpectedExcludedFields.Length, fields.Length);
        }

        [Fact]
        public void Entries_LabelJa_And_Section_AreNonEmpty()
        {
            foreach (var e in SettingsCatalog.Entries)
            {
                Assert.False(string.IsNullOrEmpty(e.SettingId));
                Assert.False(string.IsNullOrEmpty(e.Section));
                Assert.False(string.IsNullOrEmpty(e.LabelJa));
                Assert.NotNull(e.Unit);
                Assert.NotNull(e.RawValue);
                Assert.NotNull(e.Display);
            }
        }

        [Fact]
        public void EncodeBlob_StartsWithVersionAndSeparator()
        {
            var blob = SettingsCatalog.EncodeBlob(new GameConfig());
            Assert.StartsWith(SettingsCatalog.BlobVersion + "|", blob);
        }

        [Fact]
        public void EncodeThenDecode_DefaultConfig_RestoresAllRegisteredIds()
        {
            var config = new GameConfig();
            var blob = SettingsCatalog.EncodeBlob(config);
            Assert.True(SettingsCatalog.TryDecodeBlob(blob, out var values));
            AssertValuesMatchConfig(values, config);
        }

        [Fact]
        public void EncodeThenDecode_NonDefaultConfig_RestoresAllRegisteredIds()
        {
            var config = BuildNonDefaultConfig();
            AssertAllRegisteredFieldsDifferFromDefault(config);

            var blob = SettingsCatalog.EncodeBlob(config);
            Assert.True(SettingsCatalog.TryDecodeBlob(blob, out var values));
            AssertValuesMatchConfig(values, config);
        }

        [Fact]
        public void EncodeBlob_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SettingsCatalog.EncodeBlob(null));
        }

        [Fact]
        public void TryDecodeBlob_UnknownIds_Accepted_And_BuildRows_HasNoUnknownRow()
        {
            string blob = "1|RoundSeconds=1500;FutureSettingA=hello;VoteTimeCutEnabled=0;FutureSettingB=42";
            Assert.True(SettingsCatalog.TryDecodeBlob(blob, out var values));

            Assert.Equal("1500", values["RoundSeconds"]);
            Assert.Equal("0", values["VoteTimeCutEnabled"]);
            Assert.True(values.ContainsKey("FutureSettingA"));
            Assert.True(values.ContainsKey("FutureSettingB"));

            var rows = SettingsCatalog.BuildRows(values);
            Assert.All(rows, r => Assert.Contains(r.Label,
                SettingsCatalog.Entries.Select(e => e.LabelJa).ToArray()));
            Assert.Equal(2, rows.Count);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("2|RoundSeconds=1800")]
        [InlineData("0|RoundSeconds=1800")]
        [InlineData("abc|RoundSeconds=1800")]
        [InlineData("|RoundSeconds=1800")]
        [InlineData("1")]
        [InlineData("nope")]
        [InlineData("1|nokvhere")]
        [InlineData("1|=orphanvalue")]
        [InlineData("1|RoundSeconds=1800;=oops")]
        [InlineData("1|RoundSeconds=1800;broken")]
        public void TryDecodeBlob_InvalidBlob_ReturnsFalse(string blob)
        {
            Assert.False(SettingsCatalog.TryDecodeBlob(blob, out var values));
            Assert.Null(values);
        }

        [Fact]
        public void TryDecodeBlob_EmptyBody_IsAcceptedAsEmptyDictionary()
        {
            Assert.True(SettingsCatalog.TryDecodeBlob("1|", out var values));
            Assert.NotNull(values);
            Assert.Empty(values);

            var rows = SettingsCatalog.BuildRows(values);
            Assert.Empty(rows);
        }

        [Fact]
        public void TryDecodeBlob_EmptyTokensAndTrailingSep_AreSkipped()
        {
            Assert.True(SettingsCatalog.TryDecodeBlob(
                "1|RoundSeconds=1800;;VoteTimeCutEnabled=1;", out var values));
            Assert.Equal("1800", values["RoundSeconds"]);
            Assert.Equal("1", values["VoteTimeCutEnabled"]);
            Assert.Equal(2, values.Count);
        }

        [Fact]
        public void TryDecodeBlob_EmptyValue_IsAccepted()
        {
            Assert.True(SettingsCatalog.TryDecodeBlob("1|RoundSeconds=", out var values));
            Assert.Equal("", values["RoundSeconds"]);
        }

        [Fact]
        public void TryDecodeBlob_DuplicateId_LastWins()
        {
            Assert.True(SettingsCatalog.TryDecodeBlob(
                "1|RoundSeconds=100;RoundSeconds=200;RoundSeconds=300", out var values));
            Assert.Equal("300", values["RoundSeconds"]);
        }

        [Fact]
        public void BuildRows_NullValues_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SettingsCatalog.BuildRows(null));
        }

        [Fact]
        public void BuildRows_DefaultConfig_PreservesEntriesOrder_AndConcatenatesUnits()
        {
            var config = new GameConfig();
            var blob = SettingsCatalog.EncodeBlob(config);
            Assert.True(SettingsCatalog.TryDecodeBlob(blob, out var values));
            var rows = SettingsCatalog.BuildRows(values);

            Assert.Equal(SettingsCatalog.Entries.Count + 2, rows.Count);
            for (int i = 0; i < SettingsCatalog.Entries.Count; i++)
            {
                Assert.Equal(SettingsCatalog.Entries[i].LabelJa, rows[i].Label);
                Assert.Equal(SettingsCatalog.Entries[i].Section, rows[i].Section);
            }
            Assert.Equal("能力強化（全員）", rows[SettingsCatalog.Entries.Count].Section);
            Assert.Equal("なし", rows[SettingsCatalog.Entries.Count].Label);
            Assert.Equal("持ち込みアイテム一覧", rows[SettingsCatalog.Entries.Count + 1].Section);
            Assert.Equal("なし", rows[SettingsCatalog.Entries.Count + 1].Label);

            var round = rows.Single(r => r.Label == "ラウンド制限時間");
            Assert.Equal("1200秒", round.Value);

            var stamina = rows.Single(r => r.Label == "無限スタミナ解禁閾値");
            Assert.Equal("10%", stamina.Value);
        }

        [Fact]
        public void BuildRows_OmitsRowsForIdsNotInValues()
        {
            var values = new Dictionary<string, string>
            {
                { "VoteTimeCutEnabled", "1" },
                { "RoundSeconds", "1800" },
            };
            var rows = SettingsCatalog.BuildRows(values);

            Assert.Equal(2, rows.Count);
            Assert.Equal("ラウンド制限時間", rows[0].Label);
            Assert.Equal("投票による会議時間短縮", rows[1].Label);
            Assert.Equal("1800秒", rows[0].Value);
            Assert.Equal("有効", rows[1].Value);
        }

        [Theory]
        [InlineData("ExtraJumpCount", "-1", "自動")]
        [InlineData("ExtraJumpCount", "3", "3回")]
        public void Display_MinusOneMeansAuto_ForCountItems(string id, string raw, string expected)
        {
            var entry = SettingsCatalog.Entries.Single(e => e.SettingId == id);
            Assert.Equal(expected, entry.Display(raw));
            Assert.Equal("", entry.Unit);
        }

        [Theory]
        [InlineData("0", "リアルタイム")]
        [InlineData("1", "会議同期")]
        [InlineData("2", "非表示")]
        public void Display_ValuableMapMode_MapsThreeValues(string raw, string expected)
        {
            var entry = SettingsCatalog.Entries.Single(e => e.SettingId == "ValuableMapMode");
            Assert.Equal(expected, entry.Display(raw));
        }

        [Theory]
        [InlineData("0", "OFF")]
        [InlineData("1", "人狼以外の死者")]
        [InlineData("2", "全死者")]
        public void Display_NecroVoiceMode_MapsThreeValues(string raw, string expected)
        {
            var entry = SettingsCatalog.Entries.Single(e => e.SettingId == "NecroVoiceMode");
            Assert.Equal(expected, entry.Display(raw));
            Assert.Equal("", entry.Unit);
            Assert.Equal("基本", entry.Section);
        }

        [Fact]
        public void RawValue_NecroVoiceMode_EncodesAsByteInteger()
        {
            var entry = SettingsCatalog.Entries.Single(e => e.SettingId == "NecroVoiceMode");
            Assert.Equal("0", entry.RawValue(new GameConfig { NecroVoiceMode = NecroVoiceMode.Off }));
            Assert.Equal("1", entry.RawValue(new GameConfig { NecroVoiceMode = NecroVoiceMode.NonWerewolfDead }));
            Assert.Equal("2", entry.RawValue(new GameConfig { NecroVoiceMode = NecroVoiceMode.AllDead }));
        }

        [Theory]
        [InlineData("VoteTimeCutEnabled")]
        [InlineData("OrbGaugeEnabled")]
        [InlineData("WerewolfModeEnabled")]
        [InlineData("MinimapHideEnabled")]
        public void Display_BoolItems_MapZeroOneToDisabledEnabled(string id)
        {
            var entry = SettingsCatalog.Entries.Single(e => e.SettingId == id);
            Assert.Equal("有効", entry.Display("1"));
            Assert.Equal("無効", entry.Display("0"));
            Assert.Equal("", entry.Unit);
        }

        [Fact]
        public void BuildRows_AutoAndUnitConcatenation_WorkTogether()
        {
            var values = new Dictionary<string, string>
            {
                { "WerewolfCount", "3" },
                { "ExtraJumpCount", "-1" },
            };
            var rows = SettingsCatalog.BuildRows(values);
            var wolves = rows.Single(r => r.Label == "人狼の人数");
            var jumps = rows.Single(r => r.Label == "滞空中の追加ジャンプ回数");
            Assert.Equal("3人", wolves.Value);
            Assert.Equal("自動", jumps.Value);
        }

        [Fact]
        public void Worldgen_RawValue_IsCanonicalSpecPassThrough()
        {
            var config = new GameConfig
            {
                StartLevelNumber = 5,
                StartMapName = "Headman Manor",
                StartItemsSpec = "Gun:2,Med Kit:1",
                StartEnergyPct = 50,
                StartUpgradesSpec = "Speed:1,Strength:2",
                OrbDropMax = 6,
            };
            string Raw(string id) =>
                SettingsCatalog.Entries.Single(e => e.SettingId == id).RawValue(config);

            Assert.Equal("5", Raw("StartLevelNumber"));
            Assert.Equal("Headman Manor", Raw("StartMapName"));
            Assert.Equal("Gun:2,Med Kit:1", Raw("StartItemsSpec"));
            Assert.Equal("50", Raw("StartEnergyPct"));
            Assert.Equal("Speed:1,Strength:2", Raw("StartUpgradesSpec"));
            Assert.Equal("6", Raw("OrbDropMax"));
        }

        [Theory]
        [InlineData("", "ランダム")]
        [InlineData("Headman Manor", "Headman Manor")]
        public void Display_StartMapName_EmptyMeansRandom(string raw, string expected)
        {
            var entry = SettingsCatalog.Entries.Single(e => e.SettingId == "StartMapName");
            Assert.Equal(expected, entry.Display(raw));
            Assert.Equal("", entry.Unit);
        }

        [Theory]
        [InlineData("", "なし")]
        [InlineData("Gun:2", "1種2個（Gun）")]
        [InlineData("Gun:2,Med Kit:1", "2種3個（Gun、Med Kit）")]
        [InlineData("Axe:1,Gun:2,Med Kit:3", "3種6個（Axe、Gun、Med Kit）")]
        [InlineData("Axe:1,Bat:2,Gun:3,Med Kit:4", "4種10個（Axe、Bat、Gun…）")]
        public void Display_StartItemsSpec_AggregatesKindsAndTotals(string raw, string expected)
        {
            var entry = SettingsCatalog.Entries.Single(e => e.SettingId == "StartItemsSpec");
            Assert.Equal(expected, entry.Display(raw));
            Assert.Equal("", entry.Unit);
        }

        [Theory]
        [InlineData("", "なし")]
        [InlineData("Strength:2", "Strength+2")]
        [InlineData("Speed:1,Strength:2", "Speed+1、Strength+2")]
        public void Display_StartUpgradesSpec_AggregatesNamePlusStage(string raw, string expected)
        {
            var entry = SettingsCatalog.Entries.Single(e => e.SettingId == "StartUpgradesSpec");
            Assert.Equal(expected, entry.Display(raw));
            Assert.Equal("", entry.Unit);
        }

        [Fact]
        public void BuildRows_WorldgenEntries_FormatWithUnits()
        {
            var values = new Dictionary<string, string>
            {
                { "StartLevelNumber", "5" },
                { "StartMapName", "" },
                { "StartItemsSpec", "" },
                { "StartEnergyPct", "50" },
                { "StartUpgradesSpec", "Strength:2" },
                { "OrbDropMax", "6" },
            };
            var rows = SettingsCatalog.BuildRows(values);
            Assert.Equal(8, rows.Count);

            string Value(string label) => rows.First(r => r.Section == "開始環境" && r.Label == label).Value;
            Assert.Equal("5", Value("レベル"));
            Assert.Equal("ランダム", Value("マップ"));
            Assert.Equal("なし", Value("持ち込みアイテム"));
            Assert.Equal("50%", Value("トラックの充電器"));
            Assert.Equal("Strength+2", Value("開始時の能力強化"));
            Assert.Equal("6個", Value("敵が落とすオーブの数"));

            var upgradeRow = rows.Single(r => r.Section == "能力強化（全員）");
            Assert.Equal("Strength", upgradeRow.Label);
            Assert.Equal("+2", upgradeRow.Value);

            var itemRow = rows.Single(r => r.Section == "持ち込みアイテム一覧");
            Assert.Equal("なし", itemRow.Label);
            Assert.Equal("", itemRow.Value);
        }

        [Theory]
        [InlineData("Head|man Manor")]
        [InlineData("Head;man Manor")]
        [InlineData("Head=man Manor")]
        [InlineData("|;=")]
        public void Worldgen_RawValue_StartMapName_WithForbiddenChars_CollapsesToEmpty(string hostile)
        {
            var config = new GameConfig { StartMapName = hostile };
            var entry = SettingsCatalog.Entries.Single(e => e.SettingId == "StartMapName");
            Assert.Equal("", entry.RawValue(config));
            Assert.Equal("ランダム", entry.Display(entry.RawValue(config)));
        }

        [Fact]
        public void EncodeThenDecode_HostileMapName_BlobStaysDecodable_AndMapCollapsesToRandom()
        {
            var config = new GameConfig { StartMapName = "Evil;Manor=x|y" };
            var blob = SettingsCatalog.EncodeBlob(config);
            Assert.True(SettingsCatalog.TryDecodeBlob(blob, out var values));
            Assert.Equal(SettingsCatalog.Entries.Count, values.Count);
            Assert.Equal("", values["StartMapName"]);
        }

        [Theory]
        [InlineData('|')]
        [InlineData(';')]
        [InlineData('=')]
        public void Invariant_SettingId_DoesNotContainSeparator(char separator)
        {
            foreach (var e in SettingsCatalog.Entries)
            {
                Assert.False(e.SettingId.IndexOf(separator) >= 0,
                    $"SettingId '{e.SettingId}' に区切り文字 '{separator}' が含まれている");
            }
        }

        [Fact]
        public void Invariant_RawValue_DefaultConfig_DoesNotContainSeparators()
        {
            AssertNoSeparatorInAllRawValues(new GameConfig());
        }

        [Fact]
        public void Invariant_RawValue_NonDefaultConfig_DoesNotContainSeparators()
        {
            AssertNoSeparatorInAllRawValues(BuildNonDefaultConfig());
        }

        [Fact]
        public void Invariant_RawValue_HostileMapName_DoesNotContainSeparators()
        {
            AssertNoSeparatorInAllRawValues(new GameConfig { StartMapName = "a|b;c=d" });
        }

        private static GameConfig BuildNonDefaultConfig()
        {
            return new GameConfig
            {
                WerewolfCount = 5,
                BlackCatChancePercent = 30,
                RoundSeconds = 900,
                BlackCatRevealDelaySec = 45,
                BlackCatCurseEnabled = false,
                MeetingRightsPerPlayer = 2,
                ConveneSuppressStartSec = 10,
                ConveneSuppressAfterSec = 25,
                MeetingCountdownSec = 3,
                MeetingDurationSec = 90,
                VoteTimeCutEnabled = false,
                ResultDisplaySec = 4,
                MeetingScatterEnabled = false,
                ScatterGuardSec = 25,
                StaminaUnlockPct = 20,
                JumpUnlockPct = 35,
                EnemyIgnoreUnlockPct = 55,
                HealUnlockPct = 65,
                HealIntervalSec = 5,
                BeaconChargePct = 12,
                InformantThresholdPct = 70,
                ExtraJumpCount = 5,
                BeaconCooldownSec = 45,
                BeaconSuppressStartSec = 30,
                BeaconSuppressAfterMeetingSec = 20,
                CatGaugeSyncIntervalSec = 150,
                OrbGaugeEnabled = false,
                WerewolfModeEnabled = true,
                MinimapHideEnabled = false,
                OutfitChangeAllowed = true,
                ValuableMapMode = ValuableMapMode.Realtime,
                GameOverAutoReturnSec = 120,
                NecroVoiceMode = NecroVoiceMode.AllDead,
                StartLevelNumber = 5,
                StartMapName = "Headman Manor",
                StartItemsSpec = "Gun:2,Med Kit:1",
                StartEnergyPct = 50,
                StartUpgradesSpec = "Speed:1,Strength:2",
                OrbDropMax = 9,
                BomberChancePercent = 60,
                BomberProximityMeters = 3,
                BomberGaugeFullSec = 25,
                BomberInitialCooldownSec = 90,
                BomberCooldownSec = 40,
                BomberBlastRadiusMeters = 5f,
                BomberBlastPlayerDamage = 90,
                BomberBlastEnemyDamage = 80,
                BomberAmmoRefillPct = 40,
                ShamanChancePercent = 45,
                ShamanGazeFullSec = 8,
                ShamanGhostCooldownSec = 25,
                ShamanStormWeakMeters = 40,
                ShamanStormMediumMeters = 25,
                ShamanStormStrongMeters = 12,
            };
        }

        private static void AssertAllRegisteredFieldsDifferFromDefault(GameConfig custom)
        {
            var defaults = new GameConfig();
            foreach (var e in SettingsCatalog.Entries)
            {
                Assert.NotEqual(e.RawValue(defaults), e.RawValue(custom));
            }
        }

        private static void AssertValuesMatchConfig(
            IReadOnlyDictionary<string, string> values, GameConfig config)
        {
            Assert.Equal(SettingsCatalog.Entries.Count, values.Count);
            foreach (var e in SettingsCatalog.Entries)
            {
                Assert.True(values.TryGetValue(e.SettingId, out var raw),
                    $"復号された辞書に SettingId={e.SettingId} が含まれない");
                Assert.Equal(e.RawValue(config), raw);
            }
        }

        private static void AssertNoSeparatorInAllRawValues(GameConfig config)
        {
            foreach (var e in SettingsCatalog.Entries)
            {
                string raw = e.RawValue(config);
                Assert.False(raw.IndexOf('|') >= 0,
                    $"{e.SettingId} の RawValue '{raw}' に '|' が含まれている");
                Assert.False(raw.IndexOf(';') >= 0,
                    $"{e.SettingId} の RawValue '{raw}' に ';' が含まれている");
                Assert.False(raw.IndexOf('=') >= 0,
                    $"{e.SettingId} の RawValue '{raw}' に '=' が含まれている");
            }
        }
    }
}
