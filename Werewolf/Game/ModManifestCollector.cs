using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using Werewolf.Core;
using Werewolf.Net;

namespace Werewolf.Game
{
    internal enum CollectorState
    {
        NotStarted,
        Collecting,
        Ready,
        Failed,
    }

    internal sealed class ModManifestCollector
    {
        private sealed class PluginSnapshot
        {
            public string Guid;
            public string Name;
            public string Version;
            public string Location;
            public Guid ModuleVersionId;
            public string AssemblyFullName;
        }

        private sealed class CollectionResult
        {
            public ModManifest Manifest;
            public string Error;
        }

        private Task<CollectionResult> _task;

        public CollectorState State { get; private set; }
        public ModManifest Result { get; private set; }
        public string Error { get; private set; }

        public void EnsureStarted()
        {
            if (State == CollectorState.Ready || State == CollectorState.Failed) return;
            if (State == CollectorState.Collecting)
            {
                CompleteIfReady();
                return;
            }

            try
            {
                var snapshots = new List<PluginSnapshot>();
                foreach (PluginInfo info in Chainloader.PluginInfos.Values)
                {
                    if (info?.Metadata == null) continue;
                    var assembly = info.Instance != null ? info.Instance.GetType().Assembly : null;
                    snapshots.Add(new PluginSnapshot
                    {
                        Guid = info.Metadata.GUID,
                        Name = info.Metadata.Name,
                        Version = info.Metadata.Version != null ? info.Metadata.Version.ToString() : string.Empty,
                        Location = info.Location,
                        ModuleVersionId = assembly != null ? assembly.ManifestModule.ModuleVersionId : Guid.Empty,
                        AssemblyFullName = assembly?.FullName ?? string.Empty,
                    });
                }

                State = CollectorState.Collecting;
                _task = Task.Run(() => Collect(snapshots));
                WLog.Line("mod_integrity_collect", secret: false, ("state", "started"), ("plugins", snapshots.Count));
            }
            catch (Exception e)
            {
                Fail(e.GetType().Name);
            }
        }

        public void ResetSessionView()
        {
        }

        private void CompleteIfReady()
        {
            if (_task == null || !_task.IsCompleted) return;
            try
            {
                CollectionResult result = _task.GetAwaiter().GetResult();
                if (result.Manifest == null)
                {
                    Fail(result.Error ?? "manifest");
                    return;
                }
                Result = result.Manifest;
                Error = null;
                State = CollectorState.Ready;
                WLog.Line("mod_integrity_collect", secret: false,
                    ("state", "ready"), ("plugins", Result.Entries.Count),
                    ("fingerprint", Result.Fingerprint.Substring(0, 8)));
            }
            catch (Exception e)
            {
                Fail(e.GetType().Name);
            }
        }

        private void Fail(string error)
        {
            Result = null;
            Error = string.IsNullOrEmpty(error) ? "failed" : error;
            State = CollectorState.Failed;
            WLog.Line("mod_integrity_collect", secret: false, ("state", "failed"), ("reason", Error));
        }

        private static CollectionResult Collect(IReadOnlyList<PluginSnapshot> snapshots)
        {
            var entries = new List<ModManifestEntry>(snapshots.Count);
            for (int i = 0; i < snapshots.Count; i++)
            {
                PluginSnapshot plugin = snapshots[i];
                entries.Add(new ModManifestEntry(
                    Bound(plugin.Guid, ModIntegrityWire.MaxGuidLength),
                    Bound(plugin.Name, ModIntegrityWire.MaxNameLength),
                    Bound(plugin.Version, ModIntegrityWire.MaxVersionLength),
                    BuildContentId(plugin)));
            }

            if (!ModManifestComparer.TryCreateManifest(entries, out ModManifest manifest, out string error))
                return new CollectionResult { Error = error };
            return new CollectionResult { Manifest = manifest };
        }

        private static string BuildContentId(PluginSnapshot plugin)
        {
            if (!string.IsNullOrEmpty(plugin.Location))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(plugin.Location))
                    using (SHA256 sha = SHA256.Create())
                    {
                        byte[] hash = sha.ComputeHash(stream);
                        var hex = new StringBuilder(hash.Length * 2 + 7);
                        hex.Append("sha256:");
                        for (int i = 0; i < hash.Length; i++) hex.Append(hash[i].ToString("x2"));
                        return hex.ToString();
                    }
                }
                catch
                {
                }
            }

            if (plugin.ModuleVersionId != Guid.Empty)
                return "mvid:" + plugin.ModuleVersionId.ToString("D").ToLowerInvariant();

            string metadata = "metadata:" + (plugin.AssemblyFullName ?? string.Empty);
            return Bound(metadata, ModIntegrityWire.MaxContentIdLength);
        }

        private static string Bound(string value, int maxLength)
        {
            value ??= string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
