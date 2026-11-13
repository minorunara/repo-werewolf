#nullable enable
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;

namespace Werewolf.Game
{
    internal static class BepInExModManifestSnapshotSource
    {
        public static IReadOnlyList<ModManifestPluginSnapshot> Capture()
        {
            var snapshots = new List<ModManifestPluginSnapshot>();
            foreach (PluginInfo info in Chainloader.PluginInfos.Values)
            {
                if (info?.Metadata == null) continue;
                var assembly = info.Instance != null ? info.Instance.GetType().Assembly : null;
                snapshots.Add(new ModManifestPluginSnapshot(
                    info.Metadata.GUID,
                    info.Metadata.Name,
                    info.Metadata.Version?.ToString(),
                    info.Location,
                    assembly?.ManifestModule.ModuleVersionId ?? Guid.Empty,
                    assembly?.FullName));
            }

            return snapshots.AsReadOnly();
        }
    }
}
