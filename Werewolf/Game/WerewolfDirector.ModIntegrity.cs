using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Werewolf.Core;
using Werewolf.Net;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private readonly ModManifestCollector _modManifestCollector = new ModManifestCollector();
        private readonly ModIntegrityHostState _modIntegrityHostState = new ModIntegrityHostState();
        private readonly ModIntegrityClientState _modIntegrityClientState = new ModIntegrityClientState();
        private readonly Dictionary<int, ModManifestChunkAssembler> _modManifestAssemblies =
            new Dictionary<int, ModManifestChunkAssembler>();
        private readonly ModIntegrityHeader _modIntegrityHeader = new ModIntegrityHeader();
        private readonly ModIntegrityPanel _modIntegrityPanel = new ModIntegrityPanel();

        private bool _modIntegritySessionActive;
        private int _modIntegrityHostActor;
        private int _modIntegrityEpochSeed;
        private long _modIntegrityRosterTickAtMs;
        private ModManifestRequestMessage _modIntegrityPendingRequest;
        private int _modIntegrityUiEpoch;
        private int _modIntegrityUiRevision = -1;

        private bool _modIntegrityHostSignal;

        private void TickModIntegrity(long nowUnixMs)
        {
            if (!IsModIntegrityLobbyScope()) _modIntegrityHostSignal = false;
            bool active = IsModIntegrityLobbyActive();
            if (!active)
            {
                if (_modIntegritySessionActive) ResetModIntegrity("inactive");
                TickModIntegrityUi(false);
                return;
            }

            int masterActor = CurrentMasterActor();
            if (masterActor <= 0)
            {
                ResetModIntegrity("no_master");
                return;
            }
            if (_modIntegritySessionActive && _modIntegrityHostActor != masterActor)
            {
                ResetModIntegrity("master_changed");
            }
            if (!_modIntegritySessionActive)
            {
                _modIntegritySessionActive = true;
                _modIntegrityHostActor = masterActor;
                WLog.Line("mod_integrity_epoch", secret: false,
                    ("state", "session_active"), ("master", masterActor));
            }

            EnsureModIntegrityUiBuilt();
            _modManifestCollector.EnsureStarted();

            if (IsCurrentMaster()) TickModIntegrityHost(nowUnixMs, masterActor);
            else TickModIntegrityGuest(nowUnixMs, masterActor);

            if (_modIntegrityClientState.TickDetailTimeout(nowUnixMs, out int timedOutActor))
            {
                _modIntegrityPanel.SetDetailTimedOut(timedOutActor);
                WLog.Line("mod_integrity_drop", secret: false,
                    ("reason", "detail_timeout"), ("actor", timedOutActor));
            }

            RefreshModIntegrityUi();
            TickLobbyStartHold(nowUnixMs);
            TickModIntegrityUi(true);
        }

        private void TickModIntegrityHost(long nowUnixMs, int masterActor)
        {
            if (_modManifestCollector.State == CollectorState.Failed) return;
            if (_modManifestCollector.State != CollectorState.Ready) return;

            IReadOnlyList<int> roster = CurrentRoomActors();
            if (roster.Count == 0) return;

            if (!_modIntegrityHostState.IsInitialized)
            {
                int epoch = NextModIntegrityEpoch(nowUnixMs);
                _modIntegrityHostState.BeginEpoch(
                    epoch, masterActor, roster, _modManifestCollector.Result, nowUnixMs);
                ApplyHostSnapshot();
                SendManifestRequest(roster, nowUnixMs, force: true);
                WLog.Line("mod_integrity_epoch", secret: false,
                    ("epoch", epoch), ("master", masterActor), ("actors", roster.Count),
                    ("fingerprint", _modManifestCollector.Result.Fingerprint.Substring(0, 8)));
            }
            else if (nowUnixMs - _modIntegrityRosterTickAtMs >= 500L)
            {
                _modIntegrityRosterTickAtMs = nowUnixMs;
                var previous = SnapshotActors(_modIntegrityHostState.BuildSnapshot());
                var current = new HashSet<int>(roster);
                var staleAssemblies = new List<int>();
                foreach (int actor in _modManifestAssemblies.Keys)
                    if (!current.Contains(actor)) staleAssemblies.Add(actor);
                for (int i = 0; i < staleAssemblies.Count; i++)
                    _modManifestAssemblies.Remove(staleAssemblies[i]);

                _modIntegrityHostState.SyncRoster(roster, nowUnixMs);
                var joined = new List<int>();
                for (int i = 0; i < roster.Count; i++)
                {
                    int actor = roster[i];
                    if (actor == masterActor || previous.Contains(actor)) continue;
                    _modManifestAssemblies.Remove(actor);
                    joined.Add(actor);
                }
                if (joined.Count > 0) SendManifestRequest(joined, nowUnixMs, force: true);
            }

            _modIntegrityHostState.TickTimeouts(nowUnixMs);
            IReadOnlyList<int> retries = _modIntegrityHostState.GetRetryTargets(nowUnixMs, false);
            if (retries.Count > 0) SendManifestRequest(retries, nowUnixMs, force: false);

            ApplyHostSnapshot();
            if (_bus != null && _modIntegrityHostState.ShouldPublishSnapshot(nowUnixMs, false))
            {
                ModIntegritySnapshot snapshot = _modIntegrityHostState.BuildSnapshot();
                if (_bus.SendToAll(EventCodes.ModIntegritySnapshot, ModIntegrityWire.BuildSnapshot(snapshot)))
                {
                    _modIntegrityHostState.MarkSnapshotPublished(nowUnixMs);
                    WLog.Line("mod_integrity_status", secret: false,
                        ("epoch", snapshot.Epoch), ("revision", snapshot.Revision),
                        ("actors", snapshot.Records.Count));
                }
            }
        }

        private void TickModIntegrityGuest(long nowUnixMs, int masterActor)
        {
            if (_modIntegrityPendingRequest == null) return;
            if (_modManifestCollector.State == CollectorState.Collecting ||
                _modManifestCollector.State == CollectorState.NotStarted)
                return;

            ModManifestRequestMessage request = _modIntegrityPendingRequest;
            _modIntegrityPendingRequest = null;
            if (_modManifestCollector.State == CollectorState.Ready)
            {
                SendManifestReport(request);
            }
            else if (_bus != null)
            {
                WLog.Line("mod_integrity_report", secret: false,
                    ("epoch", request.Epoch), ("state", "collection_failed"));
            }
        }

        private bool TryHandleModIntegrityInbound(InboundMessage message, long nowUnixMs)
        {
            if (message == null || message.Code < EventCodes.ModManifestRequest ||
                message.Code > EventCodes.ModIntegrityDetailResponse)
                return false;
            if (!IsModIntegrityLobbyScope()) return true;

            int masterActor = CurrentMasterActor();
            if (ModIntegrityGate.IsHostSignal(IsCurrentMaster(), masterActor, message.SenderActor))
                _modIntegrityHostSignal = true;
            if (!IsModIntegrityLobbyActive()) return true;

            switch (message.Code)
            {
                case EventCodes.ModManifestRequest:
                    if (message.SenderActor != masterActor || IsCurrentMaster()) return true;
                    if (!ModIntegrityWire.TryReadManifestRequest(message.Payload, out ModManifestRequestMessage request, out string requestError))
                    {
                        LogModIntegrityDrop(message, requestError);
                        return true;
                    }
                    _modIntegrityPendingRequest = request;
                    _modManifestCollector.EnsureStarted();
                    if (_modManifestCollector.State == CollectorState.Ready)
                    {
                        _modIntegrityPendingRequest = null;
                        SendManifestReport(request);
                    }
                    return true;

                case EventCodes.ModManifestReport:
                    if (!IsCurrentMaster() || !_modIntegrityHostState.IsInitialized ||
                        !IsCurrentRoomActor(message.SenderActor)) return true;
                    HandleManifestReport(message, nowUnixMs);
                    return true;

                case EventCodes.ModIntegritySnapshot:
                    if (message.SenderActor != masterActor) return true;
                    if (!ModIntegrityWire.TryReadSnapshot(message.Payload, out ModIntegritySnapshot snapshot, out string snapshotError))
                    {
                        LogModIntegrityDrop(message, snapshotError);
                        return true;
                    }
                    _modIntegrityClientState.TryApplySnapshot(snapshot, masterActor);
                    return true;

                case EventCodes.ModIntegrityDetailRequest:
                    if (!IsCurrentMaster() || !_modIntegrityHostState.IsInitialized ||
                        !IsCurrentRoomActor(message.SenderActor)) return true;
                    HandleDetailRequest(message);
                    return true;

                case EventCodes.ModIntegrityDetailResponse:
                    if (message.SenderActor != masterActor) return true;
                    if (!ModIntegrityWire.TryReadDetailResponse(message.Payload, out ModIntegrityDetailChunk chunk, out string detailError))
                    {
                        LogModIntegrityDrop(message, detailError);
                        return true;
                    }
                    _modIntegrityClientState.TryApplyDetailChunk(chunk, out _);
                    return true;
            }
            return true;
        }

        private void HandleManifestReport(InboundMessage message, long nowUnixMs)
        {
            if (!ModIntegrityWire.TryReadManifestReport(message.Payload, out ModManifestReportChunk report, out string error))
            {
                ModUnavailableReason reason = error == "protocol"
                    ? ModUnavailableReason.UnsupportedProtocol
                    : IsOversizedManifestPayload(message.Payload)
                        ? ModUnavailableReason.TooLarge
                        : ModUnavailableReason.InvalidPayload;
                _modIntegrityHostState.MarkUnavailable(message.SenderActor, reason);
                _modManifestAssemblies.Remove(message.SenderActor);
                LogModIntegrityDrop(message, error);
                return;
            }
            if (report.Epoch != _modIntegrityHostState.Epoch) return;

            if (report.IsFingerprintOnly)
            {
                FingerprintReportOutcome outcome = _modIntegrityHostState.ApplyFingerprintReport(
                    message.SenderActor, report.ManifestFingerprint, nowUnixMs);
                if (outcome == FingerprintReportOutcome.RetryCurrentRequest)
                    SendManifestRequest(new[] { message.SenderActor }, nowUnixMs, force: true);
                WLog.Line("mod_integrity_report", secret: false,
                    ("epoch", report.Epoch), ("actor", message.SenderActor), ("mode", "fingerprint"),
                    ("outcome", outcome));
                return;
            }

            if (!_modManifestAssemblies.TryGetValue(message.SenderActor, out ModManifestChunkAssembler assembler) ||
                assembler.Epoch != report.Epoch)
            {
                assembler = new ModManifestChunkAssembler(report);
                _modManifestAssemblies[message.SenderActor] = assembler;
            }

            ModManifestChunkAddOutcome add = assembler.Add(report);
            switch (add)
            {
                case ModManifestChunkAddOutcome.Completed:
                    _modIntegrityHostState.ApplyManifest(message.SenderActor, assembler.Manifest, nowUnixMs);
                    _modManifestAssemblies.Remove(message.SenderActor);
                    break;
                case ModManifestChunkAddOutcome.Conflict:
                case ModManifestChunkAddOutcome.Invalid:
                    _modIntegrityHostState.MarkUnavailable(message.SenderActor, ModUnavailableReason.InvalidPayload);
                    _modManifestAssemblies.Remove(message.SenderActor);
                    break;
            }
            WLog.Line("mod_integrity_report", secret: false,
                ("epoch", report.Epoch), ("actor", message.SenderActor),
                ("chunk", report.ChunkIndex), ("chunks", report.ChunkCount), ("outcome", add));
        }

        private void HandleDetailRequest(InboundMessage message)
        {
            if (!ModIntegrityWire.TryReadDetailRequest(message.Payload, out ModIntegrityDetailRequestMessage request, out string error))
            {
                LogModIntegrityDrop(message, error);
                return;
            }
            if (request.Epoch != _modIntegrityHostState.Epoch) return;
            ModIntegritySnapshot snapshot = _modIntegrityHostState.BuildSnapshot();
            if (!snapshot.TryGetRecord(request.Actor, out _)) return;
            IReadOnlyList<object[]> payloads = ModIntegrityWire.BuildDetailResponse(
                snapshot.Epoch, snapshot.Revision, request.Actor,
                _modIntegrityHostState.GetDifferences(request.Actor));
            for (int i = 0; i < payloads.Count; i++)
                _bus?.SendToActors(EventCodes.ModIntegrityDetailResponse, payloads[i], new[] { message.SenderActor });
        }

        private void RequestModIntegrityDetail(int actor)
        {
            ModIntegritySnapshot current = _modIntegrityClientState.Current;
            if (current == null || !_modIntegrityClientState.BeginDetailRequest(actor, NowUnixMs())) return;

            if (IsCurrentMaster() && _modIntegrityHostState.IsInitialized)
            {
                IReadOnlyList<object[]> payloads = ModIntegrityWire.BuildDetailResponse(
                    current.Epoch, current.Revision, actor, _modIntegrityHostState.GetDifferences(actor));
                for (int i = 0; i < payloads.Count; i++)
                {
                    if (ModIntegrityWire.TryReadDetailResponse(payloads[i], out ModIntegrityDetailChunk chunk, out _))
                        _modIntegrityClientState.TryApplyDetailChunk(chunk, out _);
                }
                return;
            }

            _bus?.SendToMaster(EventCodes.ModIntegrityDetailRequest,
                ModIntegrityWire.BuildDetailRequest(current.Epoch, actor));
        }

        internal bool TryGetModIntegrityStatus(int actor, out ModParticipantRecord record)
        {
            ModIntegritySnapshot snapshot = _modIntegrityClientState.Current;
            if (snapshot != null) return snapshot.TryGetRecord(actor, out record);
            record = null;
            return false;
        }

        internal int ModIntegrityRevision => _modIntegrityClientState.Current?.Revision ?? -1;

        private void EnsureModIntegrityUiBuilt()
        {
            if (_uiManager == null || !_uiManager.EnsureCreated(gameObject)) return;
            if (!_modIntegrityHeader.Exists)
            {
                _modIntegrityHeader.Build(_uiManager.GetLayerRoot(WerewolfUIManager.ModIntegrityLayer));
                _modIntegrityHeader.OnOpenPanel = () => _modIntegrityPanel.Open(preferNeedsReview: true);
            }
            if (!_modIntegrityPanel.Exists)
            {
                _modIntegrityPanel.Build(_uiManager.GetLayerRoot(WerewolfUIManager.ModIntegrityLayer));
                _modIntegrityPanel.OnDetailRequested = RequestModIntegrityDetail;
            }
            if (!_lobbyStartWarning.Exists)
                _lobbyStartWarning.Build(_uiManager.GetLayerRoot(WerewolfUIManager.ModIntegrityModalLayer));
        }

        private void RefreshModIntegrityUi()
        {
            ModIntegritySnapshot snapshot = _modIntegrityClientState.Current;
            if (snapshot == null) return;
            if (_modIntegrityUiEpoch == snapshot.Epoch && _modIntegrityUiRevision == snapshot.Revision) return;
            _modIntegrityUiEpoch = snapshot.Epoch;
            _modIntegrityUiRevision = snapshot.Revision;
            int localActor = LocalActorNumber();
            _modIntegrityHeader.SetSnapshot(snapshot, localActor);
            _modIntegrityPanel.SetRows(BuildParticipantViews(snapshot, localActor));
        }

        private void TickModIntegrityUi(bool active)
        {
            _modIntegrityHeader.Tick(active && _modIntegrityClientState.Current != null);
            _modIntegrityPanel.Tick(_modIntegrityClientState);
            try
            {
                _lobbyStartWarning.Tick();
            }
            catch (Exception e)
            {
                WLog.Line("lobby_start_warning", secret: false,
                    ("state", "callback_failed"), ("err", e.Message));
                if (_lobbyStartHeldPage != null) FailOpenLobbyStart(NowUnixMs());
            }
        }

        private void ApplyHostSnapshot()
        {
            ModIntegritySnapshot snapshot = _modIntegrityHostState.BuildSnapshot();
            if (snapshot != null)
                _modIntegrityClientState.TryApplySnapshot(snapshot, _modIntegrityHostState.BaselineActor);
        }

        private void SendManifestRequest(IReadOnlyList<int> actors, long nowUnixMs, bool force)
        {
            if (_bus == null || !_modIntegrityHostState.IsInitialized || actors == null || actors.Count == 0) return;
            var targets = new List<int>();
            for (int i = 0; i < actors.Count; i++)
            {
                int actor = actors[i];
                if (actor > 0 && actor != _modIntegrityHostState.BaselineActor) targets.Add(actor);
            }
            if (targets.Count == 0) return;
            object[] payload = ModIntegrityWire.BuildManifestRequest(
                _modIntegrityHostState.Epoch, _modIntegrityHostState.Baseline.Fingerprint);
            if (_bus.SendToActors(EventCodes.ModManifestRequest, payload, targets.ToArray()))
            {
                WLog.Line("mod_integrity_request", secret: false,
                    ("epoch", _modIntegrityHostState.Epoch), ("targets", targets.Count), ("force", force));
            }
        }

        private void SendManifestReport(ModManifestRequestMessage request)
        {
            if (_bus == null || request == null || _modManifestCollector.State != CollectorState.Ready) return;
            IReadOnlyList<object[]> payloads = ModIntegrityWire.BuildManifestReport(
                request.Epoch, request.BaselineFingerprint, _modManifestCollector.Result);
            for (int i = 0; i < payloads.Count; i++)
                _bus.SendToMaster(EventCodes.ModManifestReport, payloads[i]);
            WLog.Line("mod_integrity_report", secret: false,
                ("epoch", request.Epoch), ("chunks", payloads.Count),
                ("mode", _modManifestCollector.Result.Fingerprint == request.BaselineFingerprint ? "fingerprint" : "manifest"));
        }

        private void ResetModIntegrity(string reason)
        {
            _modIntegritySessionActive = false;
            _modIntegrityHostActor = 0;
            _modIntegrityRosterTickAtMs = 0;
            _modIntegrityPendingRequest = null;
            _modIntegrityHostSignal = false;
            _lobbyStartHeldPage = null;
            _modManifestAssemblies.Clear();
            _modIntegrityHostState.Clear();
            _modIntegrityClientState.Clear();
            _lobbyStartAttempt.Clear();
            _modIntegrityUiEpoch = 0;
            _modIntegrityUiRevision = -1;
            _lobbyStartWarning.Hide();
            _modIntegrityPanel.Close();
            _modIntegrityHeader.Tick(false);
            _modManifestCollector.ResetSessionView();
            WLog.Line("mod_integrity_epoch", secret: false, ("state", "cleared"), ("reason", reason));
        }

        private static bool IsModIntegrityLobbyScope()
        {
            return ModIntegrityGate.IsInScope(PhotonNetwork.InRoom, SemiFunc.RunIsLobbyMenu());
        }

        private bool IsModIntegrityLobbyActive()
        {
            bool isMaster = IsCurrentMaster();
            return ModIntegrityGate.IsActive(
                PhotonNetwork.InRoom, SemiFunc.RunIsLobbyMenu(), isMaster,
                isMaster && LiveWerewolfModeEnabled(), _modIntegrityHostSignal);
        }

        private static bool IsCurrentMaster()
        {
            return PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;
        }

        private static int CurrentMasterActor()
        {
            return PhotonNetwork.InRoom && PhotonNetwork.MasterClient != null
                ? PhotonNetwork.MasterClient.ActorNumber : 0;
        }

        private int LocalActorNumber()
        {
            if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
                return PhotonNetwork.LocalPlayer.ActorNumber;
            return _bus?.LocalActorNumber ?? 1;
        }

        private static IReadOnlyList<int> CurrentRoomActors()
        {
            var result = new List<int>();
            Player[] players = PhotonNetwork.PlayerList;
            if (players != null)
            {
                for (int i = 0; i < players.Length && result.Count < ModIntegrityWire.MaxParticipants; i++)
                {
                    int actor = players[i]?.ActorNumber ?? 0;
                    if (actor > 0 && !result.Contains(actor)) result.Add(actor);
                }
            }
            result.Sort();
            return result.AsReadOnly();
        }

        private static bool IsCurrentRoomActor(int actor)
        {
            Player[] players = PhotonNetwork.PlayerList;
            if (players == null) return false;
            for (int i = 0; i < players.Length; i++)
                if (players[i] != null && players[i].ActorNumber == actor) return true;
            return false;
        }

        private IReadOnlyList<ModParticipantView> BuildParticipantViews(ModIntegritySnapshot snapshot, int localActor)
        {
            var result = new List<ModParticipantView>(snapshot.Records.Count);
            for (int i = 0; i < snapshot.Records.Count; i++)
            {
                ModParticipantRecord record = snapshot.Records[i];
                result.Add(new ModParticipantView(
                    record, ResolveRoomPlayerName(record.Actor),
                    record.Actor == localActor, record.Actor == snapshot.BaselineActor));
            }
            return result.AsReadOnly();
        }

        private static string ResolveRoomPlayerName(int actor)
        {
            Player[] players = PhotonNetwork.PlayerList;
            if (players != null)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    Player player = players[i];
                    if (player != null && player.ActorNumber == actor)
                        return string.IsNullOrEmpty(player.NickName) ? $"Actor {actor}" : player.NickName;
                }
            }
            return $"Actor {actor}";
        }

        private int NextModIntegrityEpoch(long nowUnixMs)
        {
            int candidate = (int)(nowUnixMs & 0x7fffffff);
            if (candidate <= 0) candidate = 1;
            if (candidate <= _modIntegrityEpochSeed) candidate = _modIntegrityEpochSeed + 1;
            _modIntegrityEpochSeed = candidate;
            return candidate;
        }

        private static HashSet<int> SnapshotActors(ModIntegritySnapshot snapshot)
        {
            var result = new HashSet<int>();
            if (snapshot == null) return result;
            for (int i = 0; i < snapshot.Records.Count; i++) result.Add(snapshot.Records[i].Actor);
            return result;
        }

        private static bool IsOversizedManifestPayload(object[] payload)
        {
            if (payload == null || payload.Length != 9) return false;
            string[] guids = payload[5] as string[];
            return guids != null && guids.Length > ModIntegrityWire.ManifestEntriesPerChunk;
        }

        private static void LogModIntegrityDrop(InboundMessage message, string reason)
        {
            WLog.Line("mod_integrity_drop", secret: false,
                ("code", (int)message.Code), ("actor", message.SenderActor), ("reason", reason ?? "invalid"));
        }
    }
}
