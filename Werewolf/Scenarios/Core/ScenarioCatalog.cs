using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public readonly struct ScenarioEntry
    {
        private ScenarioEntry(ScenarioId id, TextId nameId, TextId? descriptionId,
                              string iconKey, ScenarioDefinition definition)
        {
            Id = id;
            NameId = nameId;
            DescriptionId = descriptionId;
            IconKey = iconKey;
            Definition = definition;
        }

        public static ScenarioEntry Ready(ScenarioId id, TextId nameId, TextId descriptionId,
                                          string iconKey, ScenarioDefinition definition)
            => new ScenarioEntry(id, nameId, descriptionId, iconKey, definition);

        public static ScenarioEntry Planned(ScenarioId id, TextId nameId, string iconKey)
            => new ScenarioEntry(id, nameId, null, iconKey, null);

        public ScenarioId Id { get; }

        public TextId NameId { get; }

        public TextId? DescriptionId { get; }

        public string IconKey { get; }

        public ScenarioDefinition Definition { get; }

        public bool Implemented => Definition != null;
    }

    public static class ScenarioCatalog
    {
        private static readonly ScenarioEntry[] All =
        {
            ScenarioEntry.Ready(ScenarioId.MeetingVote, TextId.ScenarioNameMeeting,
                TextId.ScenarioDescMeeting, "role_villager", MeetingScenarioScript.Definition),
            ScenarioEntry.Ready(ScenarioId.WerewolfPerks, TextId.ScenarioNameWolf,
                TextId.ScenarioDescWolf, "role_werewolf", WerewolfScenarioScript.Definition),

            ScenarioEntry.Ready(ScenarioId.ShamanSense, TextId.ScenarioNameShaman,
                TextId.ScenarioDescShaman, "role_shaman", ShamanScenarioScript.Definition),
            ScenarioEntry.Ready(ScenarioId.Bomber, TextId.ScenarioNameBomber,
                TextId.ScenarioDescBomber, "role_bomber", BomberScenarioScript.Definition),
        };

        public static IReadOnlyList<ScenarioEntry> Entries => All;

        public static bool TryGet(ScenarioId id, out ScenarioEntry entry)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id != id) continue;
                entry = All[i];
                return true;
            }
            entry = default;
            return false;
        }

        public static bool CanStart(ScenarioId id)
            => TryGet(id, out var entry) && entry.Implemented;

        public static bool TryGetDefinition(ScenarioId id, out ScenarioDefinition definition)
        {
            definition = TryGet(id, out var entry) ? entry.Definition : null;
            return definition != null;
        }
    }
}
