using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Werewolf.Core
{
    public static class SettingsCatalog
    {
        public const int BlobVersion = 1;

        private const char VersionSep = '|';
        private const char PairSep = ';';
        private const char KvSep = '=';

        private static string SectionGeneral => Texts.Get(TextId.SettingsSectionGeneral);
        private static string SectionMeeting => Texts.Get(TextId.SettingsSectionMeeting);
        private static string SectionRoleAssignment => Texts.Get(TextId.SettingsSectionRoleAssignment);
        private static string SectionRoles => Texts.Get(TextId.SettingsSectionRoles);
        private static string SectionBlackCat => Texts.Get(TextId.SettingsSectionBlackCat);
        private static string SectionBomber => Texts.Get(TextId.SettingsSectionBomber);
        private static string SectionShaman => Texts.Get(TextId.SettingsSectionShaman);
        private static string SectionWorldgen => Texts.Get(TextId.SettingsSectionWorldgen);
        private static string SectionStartItemList => Texts.Get(TextId.SettingsSectionStartItemList);
        private static string SectionStartUpgradeList => Texts.Get(TextId.SettingsSectionStartUpgradeList);

        private static readonly Func<string, string> DisplayBoolEnable =
            raw => raw == "1" ? Texts.Get(TextId.SettingsBoolEnabled) : Texts.Get(TextId.SettingsBoolDisabled);

        private static readonly Func<string, string> DisplayValuableMapMode = raw =>
        {
            switch (raw)
            {
                case "0": return Texts.Get(TextId.SettingsValuableMapRealtime);
                case "1": return Texts.Get(TextId.SettingsValuableMapMeetingSync);
                case "2": return Texts.Get(TextId.SettingsValuableMapHidden);
                default: return raw;
            }
        };

        private static readonly Func<string, string> DisplayNecroVoiceMode = raw =>
        {
            switch (raw)
            {
                case "0": return Texts.Get(TextId.SettingsNecroVoiceOff);
                case "1": return Texts.Get(TextId.SettingsNecroVoiceNonWerewolfDead);
                case "2": return Texts.Get(TextId.SettingsNecroVoiceAllDead);
                default: return raw;
            }
        };

        private static readonly char[] BlobForbiddenChars = { VersionSep, PairSep, KvSep };

        private static string SanitizeMapNameRaw(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.IndexOfAny(BlobForbiddenChars) >= 0 ? "" : name;
        }

        private const int ItemNameDisplayCap = 3;

        private static string DisplayItemsAggregate(string raw)
        {
            var map = WorldgenSpec.Decode(raw);
            if (map.Count == 0) return Texts.Get(TextId.SettingsListEmpty);

            int total = 0;
            var names = new List<string>(map.Count);
            foreach (var pair in map)
            {
                total += pair.Value;
                names.Add(pair.Key);
            }
            names.Sort(StringComparer.Ordinal);

            var namesSb = new StringBuilder();
            int shown = names.Count < ItemNameDisplayCap ? names.Count : ItemNameDisplayCap;
            for (int i = 0; i < shown; i++)
            {
                if (i > 0) namesSb.Append(Texts.Get(TextId.SettingsListSeparator));
                namesSb.Append(names[i]);
            }
            if (names.Count > shown) namesSb.Append(Texts.Get(TextId.SettingsAggregateMoreSuffix));

            return Texts.Format(TextId.SettingsItemsAggregateFormat,
                map.Count.ToString(CultureInfo.InvariantCulture),
                total.ToString(CultureInfo.InvariantCulture),
                namesSb.ToString());
        }

        private static string DisplayUpgradesAggregate(string raw)
        {
            var map = WorldgenSpec.Decode(raw);
            if (map.Count == 0) return Texts.Get(TextId.SettingsListEmpty);

            var names = new List<string>(map.Count);
            foreach (var pair in map) names.Add(pair.Key);
            names.Sort(StringComparer.Ordinal);

            var sb = new StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0) sb.Append(Texts.Get(TextId.SettingsListSeparator));
                sb.Append(Texts.Format(TextId.SettingsUpgradeItemFormat,
                    names[i], map[names[i]].ToString(CultureInfo.InvariantCulture)));
            }
            return sb.ToString();
        }

        private static Func<string, string> DisplayAutoOr(string unit)
            => raw => raw == "-1" ? Texts.Get(TextId.SettingsAuto) : (raw + unit);

        public static IReadOnlyList<SettingEntry> Entries { get; } = BuildEntries();

        public static string EncodeBlob(GameConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var sb = new StringBuilder();
            sb.Append(BlobVersion.ToString(CultureInfo.InvariantCulture));
            sb.Append(VersionSep);
            var entries = Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (i > 0) sb.Append(PairSep);
                sb.Append(e.SettingId);
                sb.Append(KvSep);
                sb.Append(e.RawValue(config));
            }
            return sb.ToString();
        }

        public static bool TryDecodeBlob(string blob, out IReadOnlyDictionary<string, string> values)
        {
            values = null;
            if (string.IsNullOrEmpty(blob)) return false;

            int sepIdx = blob.IndexOf(VersionSep);
            if (sepIdx <= 0) return false;

            string versionPart = blob.Substring(0, sepIdx);
            if (!int.TryParse(versionPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int version))
                return false;
            if (version != BlobVersion) return false;

            string body = blob.Substring(sepIdx + 1);
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);

            if (body.Length > 0)
            {
                var tokens = body.Split(PairSep);
                for (int i = 0; i < tokens.Length; i++)
                {
                    var tok = tokens[i];
                    if (tok.Length == 0) continue;
                    int kv = tok.IndexOf(KvSep);
                    if (kv < 0) return false;
                    if (kv == 0) return false;
                    string id = tok.Substring(0, kv);
                    string val = tok.Substring(kv + 1);
                    dict[id] = val;
                }
            }

            values = dict;
            return true;
        }

        public static IReadOnlyList<SettingRow> BuildRows(IReadOnlyDictionary<string, string> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var rows = new List<SettingRow>(Entries.Count);
            foreach (var e in Entries)
            {
                if (!values.TryGetValue(e.SettingId, out var raw)) continue;
                string display = e.Display(raw);
                rows.Add(new SettingRow
                {
                    Section = e.Section,
                    Label = e.LabelJa,
                    Value = display + e.Unit,
                });
            }

            AppendSpecListSection(rows, values, "StartUpgradesSpec", SectionStartUpgradeList,
                formatCount: n => "+" + n.ToString(CultureInfo.InvariantCulture));
            AppendSpecListSection(rows, values, "StartItemsSpec", SectionStartItemList,
                formatCount: n => n.ToString(CultureInfo.InvariantCulture) + Texts.Get(TextId.SettingsUnitItems));

            return rows;
        }

        private static void AppendSpecListSection(
            List<SettingRow> rows,
            IReadOnlyDictionary<string, string> values,
            string settingId,
            string section,
            Func<int, string> formatCount)
        {
            if (!values.TryGetValue(settingId, out var raw)) return;

            List<string> names = null;
            IReadOnlyDictionary<string, int> map = null;

            if (!string.IsNullOrEmpty(raw))
            {
                var decoded = WorldgenSpec.Decode(raw);
                if (decoded.Count > 0)
                {
                    names = new List<string>(decoded.Count);
                    foreach (var pair in decoded)
                    {
                        if (pair.Value > 0) names.Add(pair.Key);
                    }
                    if (names.Count > 0)
                    {
                        names.Sort(StringComparer.Ordinal);
                        map = decoded;
                    }
                    else
                    {
                        names = null;
                    }
                }
            }

            if (names == null)
            {
                rows.Add(new SettingRow { Section = section, Label = Texts.Get(TextId.SettingsListEmpty), Value = "" });
                return;
            }

            foreach (var name in names)
            {
                rows.Add(new SettingRow
                {
                    Section = section,
                    Label = name,
                    Value = formatCount(map[name]),
                });
            }
        }

        private static IReadOnlyList<SettingEntry> BuildEntries()
        {
            var list = new List<SettingEntry>
            {
                new SettingEntry
                {
                    SettingId = "WerewolfModeEnabled",
                    Section = SectionGeneral,
                    LabelJa = Texts.Get(TextId.SettingsLabelWerewolfModeEnabled),
                    Unit = "",
                    RawValue = c => c.WerewolfModeEnabled ? "1" : "0",
                    Display = DisplayBoolEnable,
                },
                new SettingEntry
                {
                    SettingId = "RoundSeconds",
                    Section = SectionGeneral,
                    LabelJa = Texts.Get(TextId.SettingsLabelRoundSeconds),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.RoundSeconds.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "MinimapHideEnabled",
                    Section = SectionGeneral,
                    LabelJa = Texts.Get(TextId.SettingsLabelMinimapHideEnabled),
                    Unit = "",
                    RawValue = c => c.MinimapHideEnabled ? "1" : "0",
                    Display = DisplayBoolEnable,
                },
                new SettingEntry
                {
                    SettingId = "ValuableMapMode",
                    Section = SectionGeneral,
                    LabelJa = Texts.Get(TextId.SettingsLabelValuableMapMode),
                    Unit = "",
                    RawValue = c => ((int)c.ValuableMapMode).ToString(CultureInfo.InvariantCulture),
                    Display = DisplayValuableMapMode,
                },
                new SettingEntry
                {
                    SettingId = "OrbGaugeEnabled",
                    Section = SectionGeneral,
                    LabelJa = Texts.Get(TextId.SettingsLabelOrbGaugeEnabled),
                    Unit = "",
                    RawValue = c => c.OrbGaugeEnabled ? "1" : "0",
                    Display = DisplayBoolEnable,
                },
                new SettingEntry
                {
                    SettingId = "NecroVoiceMode",
                    Section = SectionGeneral,
                    LabelJa = Texts.Get(TextId.SettingsLabelNecroVoiceMode),
                    Unit = "",
                    RawValue = c => ((byte)c.NecroVoiceMode).ToString(CultureInfo.InvariantCulture),
                    Display = DisplayNecroVoiceMode,
                },
                new SettingEntry
                {
                    SettingId = "GameOverAutoReturnSec",
                    Section = SectionGeneral,
                    LabelJa = Texts.Get(TextId.SettingsLabelGameOverAutoReturnSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.GameOverAutoReturnSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },

                new SettingEntry
                {
                    SettingId = "MeetingRightsPerPlayer",
                    Section = SectionMeeting,
                    LabelJa = Texts.Get(TextId.SettingsLabelMeetingRightsPerPlayer),
                    Unit = Texts.Get(TextId.SettingsUnitTimes),
                    RawValue = c => c.MeetingRightsPerPlayer.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "ConveneSuppressStartSec",
                    Section = SectionMeeting,
                    LabelJa = Texts.Get(TextId.SettingsLabelConveneSuppressStartSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.ConveneSuppressStartSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "ConveneSuppressAfterSec",
                    Section = SectionMeeting,
                    LabelJa = Texts.Get(TextId.SettingsLabelConveneSuppressAfterSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.ConveneSuppressAfterSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "MeetingCountdownSec",
                    Section = SectionMeeting,
                    LabelJa = Texts.Get(TextId.SettingsLabelMeetingCountdownSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.MeetingCountdownSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "MeetingDurationSec",
                    Section = SectionMeeting,
                    LabelJa = Texts.Get(TextId.SettingsLabelMeetingDurationSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.MeetingDurationSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "VoteTimeCutEnabled",
                    Section = SectionMeeting,
                    LabelJa = Texts.Get(TextId.SettingsLabelVoteTimeCutEnabled),
                    Unit = "",
                    RawValue = c => c.VoteTimeCutEnabled ? "1" : "0",
                    Display = DisplayBoolEnable,
                },
                new SettingEntry
                {
                    SettingId = "ResultDisplaySec",
                    Section = SectionMeeting,
                    LabelJa = Texts.Get(TextId.SettingsLabelResultDisplaySec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.ResultDisplaySec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },

                new SettingEntry
                {
                    SettingId = "ShamanChancePercent",
                    Section = SectionRoleAssignment,
                    LabelJa = Texts.Get(TextId.SettingsLabelShamanChancePercent),
                    Unit = Texts.Get(TextId.SettingsUnitPercent),
                    RawValue = c => c.ShamanChancePercent.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "WerewolfCount",
                    Section = SectionRoleAssignment,
                    LabelJa = Texts.Get(TextId.SettingsLabelWerewolfCount),
                    Unit = Texts.Get(TextId.SettingsUnitPeople),
                    RawValue = c => c.WerewolfCount.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BlackCatChancePercent",
                    Section = SectionRoleAssignment,
                    LabelJa = Texts.Get(TextId.SettingsLabelBlackCatChancePercent),
                    Unit = Texts.Get(TextId.SettingsUnitPercent),
                    RawValue = c => c.BlackCatChancePercent.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BomberChancePercent",
                    Section = SectionRoleAssignment,
                    LabelJa = Texts.Get(TextId.SettingsLabelBomberChancePercent),
                    Unit = Texts.Get(TextId.SettingsUnitPercent),
                    RawValue = c => c.BomberChancePercent.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },

                new SettingEntry
                {
                    SettingId = "ShamanGazeFullSec",
                    Section = SectionShaman,
                    LabelJa = Texts.Get(TextId.SettingsLabelShamanGazeFullSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.ShamanGazeFullSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "ShamanGhostCooldownSec",
                    Section = SectionShaman,
                    LabelJa = Texts.Get(TextId.SettingsLabelShamanGhostCooldownSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.ShamanGhostCooldownSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "ShamanStormWeakMeters",
                    Section = SectionShaman,
                    LabelJa = Texts.Get(TextId.SettingsLabelShamanStormWeakMeters),
                    Unit = Texts.Get(TextId.SettingsUnitMeters),
                    RawValue = c => c.ShamanStormWeakMeters.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "ShamanStormMediumMeters",
                    Section = SectionShaman,
                    LabelJa = Texts.Get(TextId.SettingsLabelShamanStormMediumMeters),
                    Unit = Texts.Get(TextId.SettingsUnitMeters),
                    RawValue = c => c.ShamanStormMediumMeters.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "ShamanStormStrongMeters",
                    Section = SectionShaman,
                    LabelJa = Texts.Get(TextId.SettingsLabelShamanStormStrongMeters),
                    Unit = Texts.Get(TextId.SettingsUnitMeters),
                    RawValue = c => c.ShamanStormStrongMeters.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },

                new SettingEntry
                {
                    SettingId = "StaminaUnlockPct",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelStaminaUnlockPct),
                    Unit = "%",
                    RawValue = c => c.StaminaUnlockPct.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "JumpUnlockPct",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelJumpUnlockPct),
                    Unit = "%",
                    RawValue = c => c.JumpUnlockPct.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "EnemyIgnoreUnlockPct",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelEnemyIgnoreUnlockPct),
                    Unit = "%",
                    RawValue = c => c.EnemyIgnoreUnlockPct.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "HealUnlockPct",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelHealUnlockPct),
                    Unit = "%",
                    RawValue = c => c.HealUnlockPct.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "HealIntervalSec",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelHealIntervalSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.HealIntervalSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BeaconChargePct",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelBeaconChargePct),
                    Unit = "%",
                    RawValue = c => c.BeaconChargePct.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "ExtraJumpCount",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelExtraJumpCount),
                    Unit = "",
                    RawValue = c => c.ExtraJumpCount.ToString(CultureInfo.InvariantCulture),
                    Display = DisplayAutoOr(Texts.Get(TextId.SettingsUnitTimes)),
                },
                new SettingEntry
                {
                    SettingId = "BeaconCooldownSec",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelBeaconCooldownSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.BeaconCooldownSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BeaconSuppressStartSec",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelBeaconSuppressStartSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.BeaconSuppressStartSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BeaconSuppressAfterMeetingSec",
                    Section = SectionRoles,
                    LabelJa = Texts.Get(TextId.SettingsLabelBeaconSuppressAfterMeetingSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.BeaconSuppressAfterMeetingSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BlackCatRevealDelaySec",
                    Section = SectionBlackCat,
                    LabelJa = Texts.Get(TextId.SettingsLabelBlackCatRevealDelaySec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.BlackCatRevealDelaySec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BlackCatCurseEnabled",
                    Section = SectionBlackCat,
                    LabelJa = Texts.Get(TextId.SettingsLabelBlackCatCurseEnabled),
                    Unit = "",
                    RawValue = c => c.BlackCatCurseEnabled ? "1" : "0",
                    Display = DisplayBoolEnable,
                },
                new SettingEntry
                {
                    SettingId = "InformantThresholdPct",
                    Section = SectionBlackCat,
                    LabelJa = Texts.Get(TextId.SettingsLabelInformantThresholdPct),
                    Unit = "%",
                    RawValue = c => c.InformantThresholdPct.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "CatGaugeSyncIntervalSec",
                    Section = SectionBlackCat,
                    LabelJa = Texts.Get(TextId.SettingsLabelCatGaugeSyncIntervalSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.CatGaugeSyncIntervalSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },

                new SettingEntry
                {
                    SettingId = "BomberProximityMeters",
                    Section = SectionBomber,
                    LabelJa = Texts.Get(TextId.SettingsLabelBomberProximityMeters),
                    Unit = Texts.Get(TextId.SettingsUnitMeters),
                    RawValue = c => c.BomberProximityMeters.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BomberGaugeFullSec",
                    Section = SectionBomber,
                    LabelJa = Texts.Get(TextId.SettingsLabelBomberGaugeFullSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.BomberGaugeFullSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BomberInitialCooldownSec",
                    Section = SectionBomber,
                    LabelJa = Texts.Get(TextId.SettingsLabelBomberInitialCooldownSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.BomberInitialCooldownSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BomberCooldownSec",
                    Section = SectionBomber,
                    LabelJa = Texts.Get(TextId.SettingsLabelBomberCooldownSec),
                    Unit = Texts.Get(TextId.SettingsUnitSeconds),
                    RawValue = c => c.BomberCooldownSec.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BomberBlastRadiusMeters",
                    Section = SectionBomber,
                    LabelJa = Texts.Get(TextId.SettingsLabelBomberBlastRadiusMeters),
                    Unit = Texts.Get(TextId.SettingsUnitMeters),
                    RawValue = c => c.BomberBlastRadiusMeters.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BomberBlastPlayerDamage",
                    Section = SectionBomber,
                    LabelJa = Texts.Get(TextId.SettingsLabelBomberBlastPlayerDamage),
                    Unit = "",
                    RawValue = c => c.BomberBlastPlayerDamage.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BomberBlastEnemyDamage",
                    Section = SectionBomber,
                    LabelJa = Texts.Get(TextId.SettingsLabelBomberBlastEnemyDamage),
                    Unit = "",
                    RawValue = c => c.BomberBlastEnemyDamage.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "BomberAmmoRefillPct",
                    Section = SectionBomber,
                    LabelJa = Texts.Get(TextId.SettingsLabelBomberAmmoRefillPct),
                    Unit = Texts.Get(TextId.SettingsUnitPercent),
                    RawValue = c => c.BomberAmmoRefillPct.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },

                new SettingEntry
                {
                    SettingId = "StartLevelNumber",
                    Section = SectionWorldgen,
                    LabelJa = Texts.Get(TextId.SettingsLabelStartLevelNumber),
                    Unit = "",
                    RawValue = c => c.StartLevelNumber.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "StartMapName",
                    Section = SectionWorldgen,
                    LabelJa = Texts.Get(TextId.SettingsLabelStartMapName),
                    Unit = "",
                    RawValue = c => SanitizeMapNameRaw(c.StartMapName),
                    Display = raw => string.IsNullOrEmpty(raw) ? Texts.Get(TextId.SettingsRandom) : raw,
                },
                new SettingEntry
                {
                    SettingId = "StartItemsSpec",
                    Section = SectionWorldgen,
                    LabelJa = Texts.Get(TextId.SettingsLabelStartItemsSpec),
                    Unit = "",
                    RawValue = c => c.StartItemsSpec ?? "",
                    Display = DisplayItemsAggregate,
                },
                new SettingEntry
                {
                    SettingId = "StartEnergyPct",
                    Section = SectionWorldgen,
                    LabelJa = Texts.Get(TextId.SettingsLabelStartEnergyPct),
                    Unit = "%",
                    RawValue = c => c.StartEnergyPct.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },
                new SettingEntry
                {
                    SettingId = "StartUpgradesSpec",
                    Section = SectionWorldgen,
                    LabelJa = Texts.Get(TextId.SettingsLabelStartUpgradesSpec),
                    Unit = "",
                    RawValue = c => c.StartUpgradesSpec ?? "",
                    Display = DisplayUpgradesAggregate,
                },
                new SettingEntry
                {
                    SettingId = "OrbDropMax",
                    Section = SectionWorldgen,
                    LabelJa = Texts.Get(TextId.SettingsLabelOrbDropMax),
                    Unit = Texts.Get(TextId.SettingsUnitItems),
                    RawValue = c => c.OrbDropMax.ToString(CultureInfo.InvariantCulture),
                    Display = raw => raw,
                },

            };
            return list;
        }
    }

    public sealed class SettingEntry
    {
        public string SettingId;

        public string Section;

        public string LabelJa;

        public string Unit;

        public Func<GameConfig, string> RawValue;

        public Func<string, string> Display;
    }

    public sealed class SettingRow
    {
        public string Section;
        public string Label;
        public string Value;
    }
}
