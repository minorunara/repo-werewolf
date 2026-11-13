#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Werewolf.Core;
using Werewolf.Net;

namespace Werewolf.Game
{
    internal sealed class ModManifestPluginSnapshot
    {
        public ModManifestPluginSnapshot(
            string? guid,
            string? name,
            string? version,
            string? location,
            Guid moduleVersionId,
            string? assemblyFullName)
        {
            Guid = guid ?? string.Empty;
            Name = name ?? string.Empty;
            Version = version ?? string.Empty;
            Location = location;
            ModuleVersionId = moduleVersionId;
            AssemblyFullName = assemblyFullName ?? string.Empty;
        }

        public string Guid { get; }
        public string Name { get; }
        public string Version { get; }
        public string? Location { get; }
        public Guid ModuleVersionId { get; }
        public string AssemblyFullName { get; }
    }

    internal abstract class ModManifestCollectionOutcome
    {
        private ModManifestCollectionOutcome(
            IReadOnlyList<(string Guid, string Error)> contentIdErrors)
        {
            ContentIdErrors = contentIdErrors ??
                throw new ArgumentNullException(nameof(contentIdErrors));
        }

        public IReadOnlyList<(string Guid, string Error)> ContentIdErrors { get; }

        public static ModManifestCollectionOutcome Succeeded(
            ModManifest manifest,
            IReadOnlyList<(string Guid, string Error)> contentIdErrors)
        {
            return new Success(manifest, contentIdErrors);
        }

        public static ModManifestCollectionOutcome Failed(
            string error,
            IReadOnlyList<(string Guid, string Error)> contentIdErrors)
        {
            return new Failure(error, contentIdErrors);
        }

        internal sealed class Success : ModManifestCollectionOutcome
        {
            internal Success(
                ModManifest manifest,
                IReadOnlyList<(string Guid, string Error)> contentIdErrors)
                : base(contentIdErrors)
            {
                Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            }

            public ModManifest Manifest { get; }
        }

        internal sealed class Failure : ModManifestCollectionOutcome
        {
            internal Failure(
                string error,
                IReadOnlyList<(string Guid, string Error)> contentIdErrors)
                : base(contentIdErrors)
            {
                Error = string.IsNullOrEmpty(error) ? "failed" : error;
            }

            public string Error { get; }
        }
    }

    internal sealed class ModManifestCollector
    {
        private abstract class CollectionState
        {
        }

        private sealed class NotStartedState : CollectionState
        {
            public static readonly NotStartedState Instance = new NotStartedState();

            private NotStartedState()
            {
            }
        }

        private sealed class CollectingState : CollectionState
        {
            public CollectingState(Task<ModManifestCollectionOutcome> task)
            {
                Task = task ?? throw new ArgumentNullException(nameof(task));
            }

            public Task<ModManifestCollectionOutcome> Task { get; }
        }

        private sealed class ReadyState : CollectionState
        {
            public ReadyState(ModManifest manifest)
            {
                Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            }

            public ModManifest Manifest { get; }
        }

        private sealed class FailedState : CollectionState
        {
            public FailedState(string error)
            {
                Error = string.IsNullOrEmpty(error) ? "failed" : error;
            }

            public string Error { get; }
        }

        private readonly Func<IReadOnlyList<ModManifestPluginSnapshot>> _captureSnapshots;
        private readonly Func<IReadOnlyList<ModManifestPluginSnapshot>,
            Task<ModManifestCollectionOutcome>> _collectAsync;
        private CollectionState _state = NotStartedState.Instance;

        internal ModManifestCollector(
            Func<IReadOnlyList<ModManifestPluginSnapshot>> captureSnapshots,
            Func<IReadOnlyList<ModManifestPluginSnapshot>,
                Task<ModManifestCollectionOutcome>>? collectAsync = null)
        {
            _captureSnapshots = captureSnapshots ??
                throw new ArgumentNullException(nameof(captureSnapshots));
            _collectAsync = collectAsync ?? StartCollection;
        }

        public bool IsPending => _state is NotStartedState || _state is CollectingState;

        public bool IsFailed => _state is FailedState;

        public bool TryGetResult([NotNullWhen(true)] out ModManifest? manifest)
        {
            if (_state is ReadyState ready)
            {
                manifest = ready.Manifest;
                return true;
            }

            manifest = null;
            return false;
        }

        public void EnsureStarted()
        {
            if (_state is ReadyState || _state is FailedState) return;
            if (_state is CollectingState collecting)
            {
                CompleteIfReady(collecting);
                return;
            }

            try
            {
                IReadOnlyList<ModManifestPluginSnapshot> snapshots = _captureSnapshots();
                if (snapshots == null)
                    throw new InvalidOperationException("snapshot_source_returned_null");

                Task<ModManifestCollectionOutcome> task = _collectAsync(snapshots);
                if (task == null)
                    throw new InvalidOperationException("collector_returned_null_task");

                _state = new CollectingState(task);
                WLog.Line("mod_integrity_collect", secret: false,
                    ("state", "started"), ("plugins", snapshots.Count));
            }
            catch (Exception e)
            {
                Fail(e.GetType().Name);
            }
        }

        private void CompleteIfReady(CollectingState collecting)
        {
            if (!ReferenceEquals(_state, collecting) || !collecting.Task.IsCompleted) return;
            try
            {
                ModManifestCollectionOutcome outcome = collecting.Task.GetAwaiter().GetResult();
                foreach (var (guid, err) in outcome.ContentIdErrors)
                    WLog.Line("mod_integrity_contentid_error", secret: false,
                        ("guid", guid), ("err", err));

                if (outcome is ModManifestCollectionOutcome.Failure failure)
                {
                    Fail(failure.Error);
                    return;
                }

                if (outcome is not ModManifestCollectionOutcome.Success success)
                {
                    Fail("outcome");
                    return;
                }

                ModManifest manifest = success.Manifest;
                _state = new ReadyState(manifest);
                WLog.Line("mod_integrity_collect", secret: false,
                    ("state", "ready"), ("plugins", manifest.Entries.Count),
                    ("fingerprint", manifest.Fingerprint.Substring(0, 8)));
            }
            catch (Exception e)
            {
                Fail(e.GetType().Name);
            }
        }

        private void Fail(string error)
        {
            var failed = new FailedState(error);
            _state = failed;
            WLog.Line("mod_integrity_collect", secret: false,
                ("state", "failed"), ("reason", failed.Error));
        }

        private static Task<ModManifestCollectionOutcome> StartCollection(
            IReadOnlyList<ModManifestPluginSnapshot> snapshots)
        {
            return Task.Run(() => Collect(snapshots));
        }

        private static ModManifestCollectionOutcome Collect(
            IReadOnlyList<ModManifestPluginSnapshot> snapshots)
        {
            var entries = new List<ModManifestEntry>(snapshots.Count);
            var contentIdErrors = new List<(string Guid, string Error)>();
            for (int i = 0; i < snapshots.Count; i++)
            {
                ModManifestPluginSnapshot plugin = snapshots[i];
                entries.Add(new ModManifestEntry(
                    Bound(plugin.Guid, ModIntegrityWire.MaxGuidLength),
                    Bound(plugin.Name, ModIntegrityWire.MaxNameLength),
                    Bound(plugin.Version, ModIntegrityWire.MaxVersionLength),
                    BuildContentId(plugin, contentIdErrors)));
            }

            if (!ModManifestComparer.TryCreateManifest(
                    entries, out ModManifest? manifest, out string? error))
                return ModManifestCollectionOutcome.Failed(
                    error ?? "manifest", contentIdErrors.AsReadOnly());
            return ModManifestCollectionOutcome.Succeeded(
                manifest, contentIdErrors.AsReadOnly());
        }

        private static string BuildContentId(
            ModManifestPluginSnapshot plugin,
            List<(string Guid, string Error)> errors)
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
                catch (Exception e)
                {
                    errors.Add((plugin.Guid, e.Message));
                }
            }

            if (plugin.ModuleVersionId != Guid.Empty)
                return "mvid:" + plugin.ModuleVersionId.ToString("D").ToLowerInvariant();

            string metadata = "metadata:" + plugin.AssemblyFullName;
            return Bound(metadata, ModIntegrityWire.MaxContentIdLength);
        }

        private static string Bound(string? value, int maxLength)
        {
            value ??= string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
