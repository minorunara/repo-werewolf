using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Werewolf.Core;
using Werewolf.Net;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    internal sealed class ModIntegrityController
    {
        private readonly WerewolfDirector _d;

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

        private readonly LobbyStartAttempt _lobbyStartAttempt = new LobbyStartAttempt();
        private readonly LobbyStartWarning _lobbyStartWarning = new LobbyStartWarning();
        private LobbyStartPage _lobbyStartHeldPage;

        internal ModIntegrityController(WerewolfDirector director)
        {
            _d = director;
        }

        internal bool IsAnyUiVisible =>
            _modIntegrityHeader.Visible || _modIntegrityPanel.Visible || _lobbyStartWarning.Visible;

        internal void Tick(long nowUnixMs)
        {
            if (!IsModIntegrityLobbyScope()) _modIntegrityHostSignal = false;
            bool active = IsModIntegrityLobbyActive();
            if (!active)
            {
                if (_modIntegritySessionActive) ResetSession("inactive");
                TickModIntegrityUi(false);
                return;
            }

            int masterActor = CurrentMasterActor();
            if (masterActor <= 0)
            {
                ResetSession("no_master");
                return;
            }
            if (_modIntegritySessionActive && _modIntegrityHostActor != masterActor)
            {
                ResetSession("master_changed");
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
            INetBus bus = _d.ClientBus;
            if (bus != null && _modIntegrityHostState.ShouldPublishSnapshot(nowUnixMs, false))
            {
                ModIntegritySnapshot snapshot = _modIntegrityHostState.BuildSnapshot();
                if (bus.SendToAll(MessageCodes.ModIntegritySnapshot, ModIntegrityWire.BuildSnapshot(snapshot)))
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
            else if (_d.ClientBus != null)
            {
                WLog.Line("mod_integrity_report", secret: false,
                    ("epoch", request.Epoch), ("state", "collection_failed"));
            }
        }

        internal bool TryHandleInbound(InboundMessage message, long nowUnixMs)
        {
            if (message == null || message.Code < MessageCodes.ModManifestRequest ||
                message.Code > MessageCodes.ModIntegrityDetailResponse)
                return false;
            if (!IsModIntegrityLobbyScope()) return true;

            int masterActor = CurrentMasterActor();
            if (ModIntegrityGate.IsHostSignal(IsCurrentMaster(), masterActor, message.SenderActor))
                _modIntegrityHostSignal = true;
            if (!IsModIntegrityLobbyActive()) return true;

            switch (message.Code)
            {
                case MessageCodes.ModManifestRequest:
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

                case MessageCodes.ModManifestReport:
                    if (!IsCurrentMaster() || !_modIntegrityHostState.IsInitialized ||
                        !IsCurrentRoomActor(message.SenderActor)) return true;
                    HandleManifestReport(message, nowUnixMs);
                    return true;

                case MessageCodes.ModIntegritySnapshot:
                    if (message.SenderActor != masterActor) return true;
                    if (!ModIntegrityWire.TryReadSnapshot(message.Payload, out ModIntegritySnapshot snapshot, out string snapshotError))
                    {
                        LogModIntegrityDrop(message, snapshotError);
                        return true;
                    }
                    _modIntegrityClientState.TryApplySnapshot(snapshot, masterActor);
                    return true;

                case MessageCodes.ModIntegrityDetailRequest:
                    if (!IsCurrentMaster() || !_modIntegrityHostState.IsInitialized ||
                        !IsCurrentRoomActor(message.SenderActor)) return true;
                    HandleDetailRequest(message);
                    return true;

                case MessageCodes.ModIntegrityDetailResponse:
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
                _d.ClientBus?.SendToActors(MessageCodes.ModIntegrityDetailResponse, payloads[i], new[] { message.SenderActor });
        }

        private void RequestModIntegrityDetail(int actor)
        {
            ModIntegritySnapshot current = _modIntegrityClientState.Current;
            if (current == null || !_modIntegrityClientState.BeginDetailRequest(actor, SessionClock.NowMs())) return;

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

            _d.ClientBus?.SendToMaster(MessageCodes.ModIntegrityDetailRequest,
                ModIntegrityWire.BuildDetailRequest(current.Epoch, actor));
        }

        internal bool TryGetStatus(int actor, out ModParticipantRecord record)
        {
            ModIntegritySnapshot snapshot = _modIntegrityClientState.Current;
            if (snapshot != null) return snapshot.TryGetRecord(actor, out record);
            record = null;
            return false;
        }

        internal int Revision => _modIntegrityClientState.Current?.Revision ?? -1;

        private void EnsureModIntegrityUiBuilt()
        {
            if (!_d.EnsureUiCreated()) return;
            if (!_modIntegrityHeader.Exists)
            {
                _modIntegrityHeader.Build(_d.GetUiLayerRoot(WerewolfUIManager.ModIntegrityLayer));
                _modIntegrityHeader.OnOpenPanel = () => _modIntegrityPanel.Open(preferNeedsReview: true);
            }
            if (!_modIntegrityPanel.Exists)
            {
                _modIntegrityPanel.Build(_d.GetUiLayerRoot(WerewolfUIManager.ModIntegrityLayer));
                _modIntegrityPanel.OnDetailRequested = RequestModIntegrityDetail;
            }
            if (!_lobbyStartWarning.Exists)
                _lobbyStartWarning.Build(_d.GetUiLayerRoot(WerewolfUIManager.ModIntegrityModalLayer));
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
                if (_lobbyStartHeldPage != null) FailOpenLobbyStart(SessionClock.NowMs());
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
            INetBus bus = _d.ClientBus;
            if (bus == null || !_modIntegrityHostState.IsInitialized || actors == null || actors.Count == 0) return;
            var targets = new List<int>();
            for (int i = 0; i < actors.Count; i++)
            {
                int actor = actors[i];
                if (actor > 0 && actor != _modIntegrityHostState.BaselineActor) targets.Add(actor);
            }
            if (targets.Count == 0) return;
            object[] payload = ModIntegrityWire.BuildManifestRequest(
                _modIntegrityHostState.Epoch, _modIntegrityHostState.Baseline.Fingerprint);
            if (bus.SendToActors(MessageCodes.ModManifestRequest, payload, targets.ToArray()))
            {
                WLog.Line("mod_integrity_request", secret: false,
                    ("epoch", _modIntegrityHostState.Epoch), ("targets", targets.Count), ("force", force));
            }
        }

        private void SendManifestReport(ModManifestRequestMessage request)
        {
            INetBus bus = _d.ClientBus;
            if (bus == null || request == null || _modManifestCollector.State != CollectorState.Ready) return;
            IReadOnlyList<object[]> payloads = ModIntegrityWire.BuildManifestReport(
                request.Epoch, request.BaselineFingerprint, _modManifestCollector.Result);
            for (int i = 0; i < payloads.Count; i++)
                bus.SendToMaster(MessageCodes.ModManifestReport, payloads[i]);
            WLog.Line("mod_integrity_report", secret: false,
                ("epoch", request.Epoch), ("chunks", payloads.Count),
                ("mode", _modManifestCollector.Result.Fingerprint == request.BaselineFingerprint ? "fingerprint" : "manifest"));
        }

        internal void NotifyPlayerLeft(int actorNumber)
        {
            _modManifestAssemblies.Remove(actorNumber);
        }

        internal void ResetSession(string reason)
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
        }

        private static bool IsModIntegrityLobbyScope()
        {
            return ModIntegrityGate.IsInScope(PhotonNetwork.InRoom, Session.InLobbyMenu);
        }

        private bool IsModIntegrityLobbyActive()
        {
            bool isMaster = IsCurrentMaster();
            return ModIntegrityGate.IsActive(
                PhotonNetwork.InRoom, Session.InLobbyMenu, isMaster,
                isMaster && LiveConfig.WerewolfModeEnabled(), _modIntegrityHostSignal);
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
            return _d.ClientBus?.LocalActorNumber ?? 1;
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

        internal bool TryInterceptLobbyStart(LobbyStartPage lobbyPage)
        {
            try
            {
                if (lobbyPage == null || !IsModIntegrityLobbyActive() || !IsCurrentMaster()) return true;
                int masterActor = CurrentMasterActor();
                long now = SessionClock.NowMs();
                if (_modIntegrityHostState.IsInitialized)
                {
                    _modIntegrityHostState.SyncRoster(CurrentRoomActors(), now);
                    ApplyHostSnapshot();
                }
                if (_lobbyStartAttempt.TryConsumeBypass(lobbyPage.InstanceId, masterActor, now))
                    return true;
                if (_lobbyStartAttempt.IsFinalCheckPending) return false;

                LobbyStartDecision decision = CurrentStartDecision();
                if (decision.HasPending)
                {
                    if (_lobbyStartAttempt.TryBeginFinalCheck(true, now))
                    {
                        _lobbyStartHeldPage = lobbyPage;
                        SendManifestRequest(_modIntegrityHostState.GetRetryTargets(now, true), now, force: true);
                        WLog.Line("lobby_start_warning", secret: false,
                            ("state", "final_check"), ("pending", decision.PendingCount));
                    }
                    return false;
                }
                if (decision.WarningKind == LobbyStartWarningKind.None) return true;

                _lobbyStartHeldPage = lobbyPage;
                ShowLobbyStartWarning(decision);
                return false;
            }
            catch (Exception e)
            {
                WLog.Line("lobby_start_warning", secret: false,
                    ("state", "prefix_failed"), ("err", e.Message));
                return true;
            }
        }

        private void TickLobbyStartHold(long nowUnixMs)
        {
            if (!_lobbyStartAttempt.IsFinalCheckPending) return;
            try
            {
                LobbyStartDecision current = CurrentStartDecision();
                if (!_lobbyStartAttempt.ShouldCompleteFinalCheck(current.HasPending, nowUnixMs)) return;
                if (_lobbyStartHeldPage == null) return;

                current = CurrentStartDecision();
                if (current.WarningKind == LobbyStartWarningKind.None)
                    ReinvokeLobbyStart(_lobbyStartHeldPage, nowUnixMs, "resolved");
                else
                    ShowLobbyStartWarning(current);
            }
            catch (Exception e)
            {
                WLog.Line("lobby_start_warning", secret: false,
                    ("state", "hold_failed"), ("err", e.Message));
                FailOpenLobbyStart(nowUnixMs);
            }
        }

        private void ShowLobbyStartWarning(LobbyStartDecision decision)
        {
            EnsureModIntegrityUiBuilt();
            if (!_lobbyStartWarning.Exists) throw new InvalidOperationException("warning_build");
            _lobbyStartWarning.Show(
                decision,
                onBack: CancelLobbyStartWarning,
                onDetails: OpenModIntegrityDetailsFromWarning,
                onContinue: ContinueLobbyStart);
            WLog.Line("lobby_start_warning", secret: false,
                ("state", "shown"), ("kind", decision.WarningKind),
                ("players", decision.PlayerCount), ("required", decision.RequiredPlayerCount),
                ("shortfall", decision.HasPlayerShortfall ? 1 : 0),
                ("werewolfCount", decision.WerewolfCount),
                ("overflow", decision.HasTeamOverflow ? 1 : 0),
                ("difference", decision.DifferenceCount),
                ("unavailable", decision.UnavailableCount), ("pending", decision.PendingCount));
        }

        private void CancelLobbyStartWarning()
        {
            _lobbyStartAttempt.Clear();
            _lobbyStartHeldPage = null;
        }

        private void OpenModIntegrityDetailsFromWarning()
        {
            LobbyStartPage page = _lobbyStartHeldPage;
            try
            {
                _lobbyStartAttempt.Clear();
                _modIntegrityPanel.Open(preferNeedsReview: true);
                _lobbyStartHeldPage = null;
            }
            catch (Exception e)
            {
                _lobbyStartHeldPage = page;
                WLog.Line("lobby_start_warning", secret: false,
                    ("state", "details_failed"), ("err", e.Message));
                FailOpenLobbyStart(SessionClock.NowMs());
            }
        }

        private void ContinueLobbyStart()
        {
            LobbyStartPage page = _lobbyStartHeldPage;
            try
            {
                if (page != null) ReinvokeLobbyStart(page, SessionClock.NowMs(), "explicit_continue");
            }
            catch (Exception e)
            {
                _lobbyStartHeldPage = page;
                WLog.Line("lobby_start_warning", secret: false,
                    ("state", "continue_failed"), ("err", e.Message));
                FailOpenLobbyStart(SessionClock.NowMs());
            }
        }

        private void ReinvokeLobbyStart(LobbyStartPage page, long nowUnixMs, string reason)
        {
            if (page == null || !PhotonNetwork.InRoom || !IsCurrentMaster())
            {
                _lobbyStartAttempt.Clear();
                _lobbyStartHeldPage = null;
                return;
            }
            int masterActor = CurrentMasterActor();
            _lobbyStartAttempt.ArmOneShotBypass(page.InstanceId, masterActor, nowUnixMs);
            _lobbyStartHeldPage = null;
            WLog.Line("lobby_start_warning", secret: false, ("state", "reinvoke"), ("reason", reason));
            page.ButtonStart();
        }

        private void FailOpenLobbyStart(long nowUnixMs)
        {
            LobbyStartPage page = _lobbyStartHeldPage;
            _lobbyStartHeldPage = null;
            if (page != null && PhotonNetwork.InRoom && IsCurrentMaster())
                ReinvokeLobbyStart(page, nowUnixMs, "fail_open");
            else
                _lobbyStartAttempt.Clear();
        }

        private LobbyStartDecision CurrentStartDecision()
        {
            ModIntegritySnapshot snapshot = _modIntegrityHostState.IsInitialized
                ? _modIntegrityHostState.BuildSnapshot()
                : null;
            return LobbyStartGate.Evaluate(
                snapshot,
                _modManifestCollector.State == CollectorState.Ready && _modIntegrityHostState.IsInitialized,
                LobbyStartPlayerCount(),
                LiveConfig.WerewolfCount(),
                LiveConfig.DebugMode());
        }

        private int LobbyStartPlayerCount()
        {
            int room = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
                ? PhotonNetwork.CurrentRoom.PlayerCount
                : 1;
            return room + _d.PendingBotCount;
        }
    }
}
