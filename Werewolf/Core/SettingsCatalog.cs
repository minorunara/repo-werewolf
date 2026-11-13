#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public static bool TryDecodeBlob(string? blob,
            [NotNullWhen(true)] out IReadOnlyDictionary<string, string>? values)
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
                    Label = e.Label,
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

            List<string>? names = null;
            IReadOnlyDictionary<string, int>? map = null;

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

            if (names == null || map == null)
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
            var list = new List<SettingEntry>();
            list.AddRange(BuildGeneralEntries());
            list.AddRange(BuildMeetingEntries());
            list.AddRange(BuildRoleAssignmentEntries());
            list.AddRange(BuildShamanEntries());
            list.AddRange(BuildWerewolfEntries());
            list.AddRange(BuildBlackCatEntries());
            list.AddRange(BuildBomberEntries());
            list.AddRange(BuildWorldgenEntries());
            return list;
        }

        private static SettingEntry[] BuildGeneralEntries() => new[]
        {
            new SettingEntry
            {
                SettingId = "WerewolfModeEnabled",
                SectionId = TextId.SettingsSectionGeneral,
                LabelId = TextId.SettingsLabelWerewolfModeEnabled,
                RawValue = c => c.WerewolfModeEnabled ? "1" : "0",
                Display = DisplayBoolEnable,
            },
            new SettingEntry
            {
                SettingId = "RoundSeconds",
                SectionId = TextId.SettingsSectionGeneral,
                LabelId = TextId.SettingsLabelRoundSeconds,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.RoundSeconds.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "MinimapHideEnabled",
                SectionId = TextId.SettingsSectionGeneral,
                LabelId = TextId.SettingsLabelMinimapHideEnabled,
                RawValue = c => c.MinimapHideEnabled ? "1" : "0",
                Display = DisplayBoolEnable,
            },
            new SettingEntry
            {
                SettingId = "OutfitChangeAllowed",
                SectionId = TextId.SettingsSectionGeneral,
                LabelId = TextId.SettingsLabelOutfitChangeAllowed,
                RawValue = c => c.OutfitChangeAllowed ? "1" : "0",
                Display = DisplayBoolEnable,
            },
            new SettingEntry
            {
                SettingId = "ValuableMapMode",
                SectionId = TextId.SettingsSectionGeneral,
                LabelId = TextId.SettingsLabelValuableMapMode,
                RawValue = c => ((int)c.ValuableMapMode).ToString(CultureInfo.InvariantCulture),
                Display = DisplayValuableMapMode,
            },
            new SettingEntry
            {
                SettingId = "OrbGaugeEnabled",
                SectionId = TextId.SettingsSectionGeneral,
                LabelId = TextId.SettingsLabelOrbGaugeEnabled,
                RawValue = c => c.OrbGaugeEnabled ? "1" : "0",
                Display = DisplayBoolEnable,
            },
            new SettingEntry
            {
                SettingId = "NecroVoiceMode",
                SectionId = TextId.SettingsSectionGeneral,
                LabelId = TextId.SettingsLabelNecroVoiceMode,
                RawValue = c => ((byte)c.NecroVoiceMode).ToString(CultureInfo.InvariantCulture),
                Display = DisplayNecroVoiceMode,
            },
            new SettingEntry
            {
                SettingId = "GameOverAutoReturnSec",
                SectionId = TextId.SettingsSectionGeneral,
                LabelId = TextId.SettingsLabelGameOverAutoReturnSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.GameOverAutoReturnSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
        };

        private static SettingEntry[] BuildMeetingEntries() => new[]
        {
            new SettingEntry
            {
                SettingId = "MeetingRightsPerPlayer",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelMeetingRightsPerPlayer,
                UnitId = TextId.SettingsUnitTimes,
                RawValue = c => c.MeetingRightsPerPlayer.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "ConveneSuppressStartSec",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelConveneSuppressStartSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.ConveneSuppressStartSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "ConveneSuppressAfterSec",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelConveneSuppressAfterSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.ConveneSuppressAfterSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "MeetingCountdownSec",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelMeetingCountdownSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.MeetingCountdownSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "MeetingDurationSec",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelMeetingDurationSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.MeetingDurationSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "VoteTimeCutEnabled",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelVoteTimeCutEnabled,
                RawValue = c => c.VoteTimeCutEnabled ? "1" : "0",
                Display = DisplayBoolEnable,
            },
            new SettingEntry
            {
                SettingId = "ResultDisplaySec",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelResultDisplaySec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.ResultDisplaySec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "MeetingScatterEnabled",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelMeetingScatterEnabled,
                RawValue = c => c.MeetingScatterEnabled ? "1" : "0",
                Display = DisplayBoolEnable,
            },
            new SettingEntry
            {
                SettingId = "ScatterGuardSec",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelScatterGuardSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.ScatterGuardSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "MeetingEnemyRespawnScalePct",
                SectionId = TextId.SettingsSectionMeeting,
                LabelId = TextId.SettingsLabelMeetingEnemyRespawnScalePct,
                UnitId = TextId.SettingsUnitPercent,
                RawValue = c => c.MeetingEnemyRespawnScalePct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
        };

        private static SettingEntry[] BuildRoleAssignmentEntries() => new[]
        {
            new SettingEntry
            {
                SettingId = "ShamanChancePercent",
                SectionId = TextId.SettingsSectionRoleAssignment,
                LabelId = TextId.SettingsLabelShamanChancePercent,
                UnitId = TextId.SettingsUnitPercent,
                RawValue = c => c.ShamanChancePercent.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "WerewolfCount",
                SectionId = TextId.SettingsSectionRoleAssignment,
                LabelId = TextId.SettingsLabelWerewolfCount,
                UnitId = TextId.SettingsUnitPeople,
                RawValue = c => c.WerewolfCount.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BlackCatChancePercent",
                SectionId = TextId.SettingsSectionRoleAssignment,
                LabelId = TextId.SettingsLabelBlackCatChancePercent,
                UnitId = TextId.SettingsUnitPercent,
                RawValue = c => c.BlackCatChancePercent.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BomberChancePercent",
                SectionId = TextId.SettingsSectionRoleAssignment,
                LabelId = TextId.SettingsLabelBomberChancePercent,
                UnitId = TextId.SettingsUnitPercent,
                RawValue = c => c.BomberChancePercent.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "PeacefulVillageChancePercent",
                SectionId = TextId.SettingsSectionRoleAssignment,
                LabelId = TextId.SettingsLabelPeacefulVillageChancePercent,
                UnitId = TextId.SettingsUnitPercent,
                RawValue = c => c.PeacefulVillageChancePercent.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
        };

        private static SettingEntry[] BuildShamanEntries() => new[]
        {
            new SettingEntry
            {
                SettingId = "ShamanGazeFullSec",
                SectionId = TextId.SettingsSectionShaman,
                LabelId = TextId.SettingsLabelShamanGazeFullSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.ShamanGazeFullSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "ShamanGhostCooldownSec",
                SectionId = TextId.SettingsSectionShaman,
                LabelId = TextId.SettingsLabelShamanGhostCooldownSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.ShamanGhostCooldownSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "ShamanStormWeakMeters",
                SectionId = TextId.SettingsSectionShaman,
                LabelId = TextId.SettingsLabelShamanStormWeakMeters,
                UnitId = TextId.SettingsUnitMeters,
                RawValue = c => c.ShamanStormWeakMeters.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "ShamanStormMediumMeters",
                SectionId = TextId.SettingsSectionShaman,
                LabelId = TextId.SettingsLabelShamanStormMediumMeters,
                UnitId = TextId.SettingsUnitMeters,
                RawValue = c => c.ShamanStormMediumMeters.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "ShamanStormStrongMeters",
                SectionId = TextId.SettingsSectionShaman,
                LabelId = TextId.SettingsLabelShamanStormStrongMeters,
                UnitId = TextId.SettingsUnitMeters,
                RawValue = c => c.ShamanStormStrongMeters.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
        };

        private static SettingEntry[] BuildWerewolfEntries() => new[]
        {
            new SettingEntry
            {
                SettingId = "StaminaUnlockPct",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelStaminaUnlockPct,
                UnitLiteral = "%",
                RawValue = c => c.StaminaUnlockPct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "JumpUnlockPct",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelJumpUnlockPct,
                UnitLiteral = "%",
                RawValue = c => c.JumpUnlockPct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "EnemyIgnoreUnlockPct",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelEnemyIgnoreUnlockPct,
                UnitLiteral = "%",
                RawValue = c => c.EnemyIgnoreUnlockPct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "HealUnlockPct",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelHealUnlockPct,
                UnitLiteral = "%",
                RawValue = c => c.HealUnlockPct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "HealIntervalSec",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelHealIntervalSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.HealIntervalSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BeaconChargePct",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelBeaconChargePct,
                UnitLiteral = "%",
                RawValue = c => c.BeaconChargePct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "ExtraJumpCount",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelExtraJumpCount,
                RawValue = c => c.ExtraJumpCount.ToString(CultureInfo.InvariantCulture),
                Display = DisplayAutoOr(Texts.Get(TextId.SettingsUnitTimes)),
            },
            new SettingEntry
            {
                SettingId = "BeaconCooldownSec",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelBeaconCooldownSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.BeaconCooldownSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BeaconSuppressStartSec",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelBeaconSuppressStartSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.BeaconSuppressStartSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BeaconSuppressAfterMeetingSec",
                SectionId = TextId.SettingsSectionRoles,
                LabelId = TextId.SettingsLabelBeaconSuppressAfterMeetingSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.BeaconSuppressAfterMeetingSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
        };

        private static SettingEntry[] BuildBlackCatEntries() => new[]
        {
            new SettingEntry
            {
                SettingId = "BlackCatRevealDelaySec",
                SectionId = TextId.SettingsSectionBlackCat,
                LabelId = TextId.SettingsLabelBlackCatRevealDelaySec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.BlackCatRevealDelaySec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BlackCatCurseEnabled",
                SectionId = TextId.SettingsSectionBlackCat,
                LabelId = TextId.SettingsLabelBlackCatCurseEnabled,
                RawValue = c => c.BlackCatCurseEnabled ? "1" : "0",
                Display = DisplayBoolEnable,
            },
            new SettingEntry
            {
                SettingId = "InformantThresholdPct",
                SectionId = TextId.SettingsSectionBlackCat,
                LabelId = TextId.SettingsLabelInformantThresholdPct,
                UnitLiteral = "%",
                RawValue = c => c.InformantThresholdPct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "CatGaugeSyncIntervalSec",
                SectionId = TextId.SettingsSectionBlackCat,
                LabelId = TextId.SettingsLabelCatGaugeSyncIntervalSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.CatGaugeSyncIntervalSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
        };

        private static SettingEntry[] BuildBomberEntries() => new[]
        {
            new SettingEntry
            {
                SettingId = "BomberProximityMeters",
                SectionId = TextId.SettingsSectionBomber,
                LabelId = TextId.SettingsLabelBomberProximityMeters,
                UnitId = TextId.SettingsUnitMeters,
                RawValue = c => c.BomberProximityMeters.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BomberGaugeFullSec",
                SectionId = TextId.SettingsSectionBomber,
                LabelId = TextId.SettingsLabelBomberGaugeFullSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.BomberGaugeFullSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BomberInitialCooldownSec",
                SectionId = TextId.SettingsSectionBomber,
                LabelId = TextId.SettingsLabelBomberInitialCooldownSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.BomberInitialCooldownSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BomberCooldownSec",
                SectionId = TextId.SettingsSectionBomber,
                LabelId = TextId.SettingsLabelBomberCooldownSec,
                UnitId = TextId.SettingsUnitSeconds,
                RawValue = c => c.BomberCooldownSec.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BomberBlastRadiusMeters",
                SectionId = TextId.SettingsSectionBomber,
                LabelId = TextId.SettingsLabelBomberBlastRadiusMeters,
                UnitId = TextId.SettingsUnitMeters,
                RawValue = c => c.BomberBlastRadiusMeters.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BomberBlastPlayerDamage",
                SectionId = TextId.SettingsSectionBomber,
                LabelId = TextId.SettingsLabelBomberBlastPlayerDamage,
                RawValue = c => c.BomberBlastPlayerDamage.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BomberBlastEnemyDamage",
                SectionId = TextId.SettingsSectionBomber,
                LabelId = TextId.SettingsLabelBomberBlastEnemyDamage,
                RawValue = c => c.BomberBlastEnemyDamage.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "BomberAmmoRefillPct",
                SectionId = TextId.SettingsSectionBomber,
                LabelId = TextId.SettingsLabelBomberAmmoRefillPct,
                UnitId = TextId.SettingsUnitPercent,
                RawValue = c => c.BomberAmmoRefillPct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
        };

        private static SettingEntry[] BuildWorldgenEntries() => new[]
        {
            new SettingEntry
            {
                SettingId = "StartLevelNumber",
                SectionId = TextId.SettingsSectionWorldgen,
                LabelId = TextId.SettingsLabelStartLevelNumber,
                RawValue = c => c.StartLevelNumber.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "StartMapName",
                SectionId = TextId.SettingsSectionWorldgen,
                LabelId = TextId.SettingsLabelStartMapName,
                RawValue = c => SanitizeMapNameRaw(c.StartMapName),
                Display = raw => string.IsNullOrEmpty(raw) ? Texts.Get(TextId.SettingsRandom) : raw,
            },
            new SettingEntry
            {
                SettingId = "StartItemsSpec",
                SectionId = TextId.SettingsSectionWorldgen,
                LabelId = TextId.SettingsLabelStartItemsSpec,
                RawValue = c => c.StartItemsSpec ?? "",
                Display = DisplayItemsAggregate,
            },
            new SettingEntry
            {
                SettingId = "StartEnergyPct",
                SectionId = TextId.SettingsSectionWorldgen,
                LabelId = TextId.SettingsLabelStartEnergyPct,
                UnitLiteral = "%",
                RawValue = c => c.StartEnergyPct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "StartUpgradesSpec",
                SectionId = TextId.SettingsSectionWorldgen,
                LabelId = TextId.SettingsLabelStartUpgradesSpec,
                RawValue = c => c.StartUpgradesSpec ?? "",
                Display = DisplayUpgradesAggregate,
            },
            new SettingEntry
            {
                SettingId = "OrbDropMax",
                SectionId = TextId.SettingsSectionWorldgen,
                LabelId = TextId.SettingsLabelOrbDropMax,
                UnitId = TextId.SettingsUnitItems,
                RawValue = c => c.OrbDropMax.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw,
            },
            new SettingEntry
            {
                SettingId = "StartHaulGoalPct",
                SectionId = TextId.SettingsSectionWorldgen,
                LabelId = TextId.SettingsLabelStartHaulGoalPct,
                UnitId = TextId.SettingsUnitPercent,
                RawValue = c => c.StartHaulGoalPct.ToString(CultureInfo.InvariantCulture),
                Display = raw => raw == "0" ? Texts.Get(TextId.SettingsValueVanilla) : raw,
            },
            EnemySetsEntry("EnemySetsDanger1", TextId.SettingsLabelEnemySetsDanger1, c => c.EnemySetsDanger1),
            EnemySetsEntry("EnemySetsDanger2", TextId.SettingsLabelEnemySetsDanger2, c => c.EnemySetsDanger2),
            EnemySetsEntry("EnemySetsDanger3", TextId.SettingsLabelEnemySetsDanger3, c => c.EnemySetsDanger3),
        };

        private static SettingEntry EnemySetsEntry(string id, TextId label, Func<GameConfig, int> sets) => new SettingEntry
        {
            SettingId = id,
            SectionId = TextId.SettingsSectionWorldgen,
            LabelId = label,
            RawValue = c => sets(c).ToString(CultureInfo.InvariantCulture),
            Display = raw => raw == "-1"
                ? Texts.Get(TextId.SettingsValueVanilla)
                : raw + Texts.Get(TextId.SettingsUnitSets),
        };
    }

    public sealed class SettingEntry
    {
        public string SettingId = "";

        public TextId SectionId;

        public TextId LabelId;

        public TextId? UnitId;

        public string UnitLiteral = "";

        public string Section => Texts.Get(SectionId);

        public string Label => Texts.Get(LabelId);

        public string Unit => UnitId is TextId id ? Texts.Get(id) : UnitLiteral;

        public Func<GameConfig, string> RawValue = _ => "";

        public Func<string, string> Display = _ => "";
    }

    public sealed class SettingRow
    {
        public string Section = "";
        public string Label = "";
        public string Value = "";
    }
}
