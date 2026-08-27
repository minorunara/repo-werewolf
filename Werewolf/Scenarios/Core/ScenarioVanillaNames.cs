using System.Collections.Generic;
using Werewolf.Core;

namespace Werewolf.Scenarios.Core
{
    public enum ScenarioVanillaNameKind
    {
        Level,

        Upgrade,

        Resource,

        ChildPath,
    }

    public readonly struct ScenarioVanillaName
    {
        public ScenarioVanillaName(ScenarioVanillaNameKind kind, string name, string purpose, string parent = null)
        {
            Kind = kind;
            Name = name;
            Purpose = purpose;
            Parent = parent;
        }

        public ScenarioVanillaNameKind Kind { get; }

        public string Name { get; }

        public string Purpose { get; }

        public string Parent { get; }
    }

    public static class ScenarioVanillaNames
    {
        public const string MuseumLevel = "Level - Museum";

        public const string MenuAvatarResource = "PlayerAvatarMenu";

        public const string DeathHeadResource = "Player Death Head";

        public const string DeathHeadMeshChild = "Mesh";

        public static readonly IReadOnlyList<ScenarioVanillaName> All = BuildAll();

        public static bool Contains(ScenarioVanillaNameKind kind, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            for (int i = 0; i < All.Count; i++)
            {
                var entry = All[i];
                if (entry.Kind == kind && string.Equals(entry.Name, name, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static IReadOnlyList<ScenarioVanillaName> BuildAll()
        {
            var list = new List<ScenarioVanillaName>
            {
                new ScenarioVanillaName(ScenarioVanillaNameKind.Level, MuseumLevel,
                    "全シナリオの固定マップ"),
                new ScenarioVanillaName(ScenarioVanillaNameKind.Resource, MenuAvatarResource,
                    "セミボットの見た目"),
                new ScenarioVanillaName(ScenarioVanillaNameKind.Resource, DeathHeadResource,
                    "死体の見た目"),
                new ScenarioVanillaName(ScenarioVanillaNameKind.ChildPath, DeathHeadMeshChild,
                    "死体から複製するメッシュのサブツリー", DeathHeadResource),
            };

            foreach (string upgrade in WorldgenUpgrades.Names)
            {
                list.Add(new ScenarioVanillaName(ScenarioVanillaNameKind.Upgrade, upgrade,
                    "開始時に配る能力強化"));
            }

            return list;
        }
    }
}
