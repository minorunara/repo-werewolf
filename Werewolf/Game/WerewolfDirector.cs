using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.Scenarios.Core;
using Werewolf.Scenarios.Game;
using Werewolf.Scenarios.UI;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector : MonoBehaviour, IReplayDirectorView
    {
        public static WerewolfDirector? Instance { get; private set; }

        public PlayerRegistry Registry => Runtime.Registry;

        int IReplayDirectorView.LocalActor => LocalActor;
        string IReplayDirectorView.DisplayNameForActor(int actorNumber) => DisplayNameForActor(actorNumber);

        private readonly RoomState _roomState = new RoomState();

        private readonly AutoStartWaitGate _autoStartWait = new AutoStartWaitGate();

        private readonly ResultSequence _resultSequence = new ResultSequence();

        private HostRoundState? _hostRound;
        private INetBus? _bus;

        private readonly Dictionary<(int Actor, string Stage), BombDenyReason> _lastBombDeny =
            new Dictionary<(int Actor, string Stage), BombDenyReason>();

        private readonly StartReservations _reservations = new StartReservations();

        private readonly EnemyIgnoreRoster _enemyIgnoreRoster = new EnemyIgnoreRoster();
        private readonly BeaconEffect _beaconEffect = new BeaconEffect();

        private readonly CursorMirror _cursorMirror = new CursorMirror { SortingOrder = WerewolfUIManager.CursorSortingOrder };

        private readonly ReplayViewerPanel _replayViewer = new ReplayViewerPanel();

        private readonly SfxPlayer _sfxPlayer = new SfxPlayer();

        private readonly ResultScreen _resultScreen = new ResultScreen();

        private GamePhase _clientPhase = GamePhase.Lobby;
        private readonly ClientRoundMirror _roundMirror = new ClientRoundMirror();
        private readonly ClientSettingsMirror _cfgMirror = new ClientSettingsMirror();

        private readonly MeetingClientState _meetingClient = new MeetingClientState();
        private readonly ManualOverlay _manualOverlay = new ManualOverlay();
        private readonly ScenarioSelectPanel _scenarioSelect = new ScenarioSelectPanel();

        private bool _resetArmed;

        public bool IsHostSessionActive => _hostRound != null;

        public bool IsHostMatchLive =>
            _hostRound != null && CombatRules.IsMatchLive(_hostRound.Session.Phase);

        public GamePhase HostPhase => _hostRound?.Session.Phase ?? GamePhase.Lobby;

        public RolesClientState RolesClient { get; } = new RolesClientState();

        public IdRosterClient IdRoster { get; } = new IdRosterClient();

        public int ParticipantIdFor(int actorNumber) => IdRoster.IdOf(actorNumber);

        public Role? LocalRoleClient => _roundMirror.LocalRole;

        public Role? MarkedTeammateRole(int actorNumber)
        {
            if (_roundMirror.LocalRole != Role.BlackCat && _roundMirror.LocalRole != Role.Werewolf && _roundMirror.LocalRole != Role.Bomber) return null;
            int[]? set = _roundMirror.KnownWerewolves;
            if (set == null || set.Length == 0) return null;
            byte[]? roles = _roundMirror.KnownTeammateRoles;
            for (int i = 0; i < set.Length; i++)
            {
                if (set[i] != actorNumber) continue;
                if (roles != null && i < roles.Length && (Role)roles[i] == Role.Bomber) return Role.Bomber;
                return Role.Werewolf;
            }
            return null;
        }

        public Role? MarkedTeammateRoleForActor(int actor)
        {
            if (actor < 0) return null;
            if (_roundMirror.LocalRole != Role.BlackCat && _roundMirror.LocalRole != Role.Werewolf && _roundMirror.LocalRole != Role.Bomber) return null;
            if (_roundMirror.KnownWerewolves == null || _roundMirror.KnownWerewolves.Length == 0) return null;
            return MarkedTeammateRole(actor);
        }

        public bool IsFellowWerewolfActor(int actorNumber)
        {
            if (_roundMirror.LocalRole != Role.Werewolf) return false;
            int[]? set = _roundMirror.KnownWerewolves;
            if (set == null || set.Length == 0) return false;
            byte[]? roles = _roundMirror.KnownTeammateRoles;
            for (int i = 0; i < set.Length; i++)
            {
                if (set[i] != actorNumber) continue;
                if (roles != null && i < roles.Length && (Role)roles[i] == Role.Bomber) return false;
                return true;
            }
            return false;
        }

        public bool IsDeadActorClient(int actorNumber)
        {
            return _roundMirror.DeathMirror.ContainsKey(actorNumber);
        }

        public bool ShouldShowDeadCuesClient(int speakerActor)
        {
            bool localAlive = !IsDeadActorClient(LocalActor);
            VoicePlanKind kind = VoiceRules.DecideKind(
                ClientPhase, LocalRoleClient, localAlive, _cfgMirror.NecroVoiceMode);
            bool eavesdropAudible = kind == VoicePlanKind.Eavesdrop
                && Runtime.VoiceIsEavesdropTarget(speakerActor);
            return VoiceRules.ShouldShowDeadCues(
                ClientPhase, localAlive, IsDeadActorClient(speakerActor), eavesdropAudible);
        }

        public bool ShouldShowDeadTextClient(int speakerActor)
        {
            bool localAlive = !IsDeadActorClient(LocalActor);
            return VoiceRules.ShouldShowDeadText(
                ClientPhase, localAlive, IsDeadActorClient(speakerActor));
        }

        public GamePhase ClientPhase => _hostRound?.Session.Phase ?? _clientPhase;

        public bool IsRoundActiveClient
        {
            get
            {
                GamePhase phase = ClientPhase;
                return CombatRules.IsMatchLive(phase);
            }
        }

        public bool ClientMinimapHideEnabled => _cfgMirror.MinimapHideEnabled;

        public ValuableMapMode ClientValuableMapMode => _cfgMirror.ValuableMapMode;

        public NecroVoiceMode ClientNecroVoiceMode => _cfgMirror.NecroVoiceMode;

        public int ClientExtraJumpCount =>
            _cfgMirror.ExtraJumpCount ?? (Plugin.GameConfig != null ? Plugin.GameConfig.ExtraJumpCount : 0);

        public int ClientConveneSuppressStartSec =>
            _cfgMirror.ConveneSuppressStartSec ?? (Plugin.GameConfig != null ? Plugin.GameConfig.ConveneSuppressStartSec : 0);

        public int ClientConveneSuppressAfterSec =>
            _cfgMirror.ConveneSuppressAfterSec ?? (Plugin.GameConfig != null ? Plugin.GameConfig.ConveneSuppressAfterSec : 0);

        public int ClientHealIntervalSec =>
            _cfgMirror.HealIntervalSec ?? (Plugin.GameConfig != null ? Plugin.GameConfig.HealIntervalSec : 3);

        public bool ClientOutfitChangeAllowed => _cfgMirror.OutfitChangeAllowed ?? false;

        private int ClientBombPackValue(int index)
        {
            if (_cfgMirror.BombPack != null && index >= 0 && index < _cfgMirror.BombPack.Length)
                return _cfgMirror.BombPack[index];
            GameConfig? cfg = Plugin.GameConfig;
            if (cfg == null) return 0;
            return RoomStateKeys.EncodeBomb(cfg, int.MaxValue)[index];
        }

        public bool ClientBomberPossible => ClientBombPackValue(RoomStateKeys.BombIndex.BomberPossible) != 0;

        public float ClientBomberProximityMeters => ClientBombPackValue(RoomStateKeys.BombIndex.ProximityCm) / 100f;

        public float ClientBomberGaugeFullSec => ClientBombPackValue(RoomStateKeys.BombIndex.GaugeFullMs) / 1000f;

        public int ClientBomberCooldownSec => ClientBombPackValue(RoomStateKeys.BombIndex.CooldownMs) / 1000;

        public int ClientBomberInitialCooldownSec => ClientBombPackValue(RoomStateKeys.BombIndex.InitialCooldownMs) / 1000;

        public float ClientBomberBlastRadiusMeters => ClientBombPackValue(RoomStateKeys.BombIndex.BlastRadiusCm) / 100f;

        public int ClientBomberBlastPlayerDamage => ClientBombPackValue(RoomStateKeys.BombIndex.BlastPlayerDamage);

        public int ClientBomberBlastEnemyDamage => ClientBombPackValue(RoomStateKeys.BombIndex.BlastEnemyDamage);

        private int ClientShamanPackValue(int index)
        {
            if (_cfgMirror.ShamanPack != null && index >= 0 && index < _cfgMirror.ShamanPack.Length)
                return _cfgMirror.ShamanPack[index];
            GameConfig? cfg = Plugin.GameConfig;
            if (cfg == null) return 0;
            return RoomStateKeys.EncodeShaman(cfg)[index];
        }

        public float ClientShamanGazeFullSec => ClientShamanPackValue(RoomStateKeys.ShamanIndex.GazeFullMs) / 1000f;

        public float ClientShamanGhostCooldownSec => ClientShamanPackValue(RoomStateKeys.ShamanIndex.GhostCooldownMs) / 1000f;

        public float ClientShamanStormWeakMeters => ClientShamanPackValue(RoomStateKeys.ShamanIndex.StormWeakCm) / 100f;

        public float ClientShamanStormMediumMeters => ClientShamanPackValue(RoomStateKeys.ShamanIndex.StormMediumCm) / 100f;

        public float ClientShamanStormStrongMeters => ClientShamanPackValue(RoomStateKeys.ShamanIndex.StormStrongCm) / 100f;

        public int HostValuablesDestroyed { get; private set; }

        public void HostFreezeGaugeBase(float totalDollars)
        {
            HostPlayState? play = _hostRound?.Play;
            if (play == null || !Session.IsHost) return;
            HostValuablesDestroyed = 0;
            play.Roles.FreezeBase(totalDollars);
        }

        public void HostAddValueLoss(float lostDollars, bool isOrb, int vid, bool destroyed)
        {
            HostPlayState? play = _hostRound?.Play;
            if (play == null || !Session.IsHost || !IsHostMatchLive) return;
            if (destroyed) HostValuablesDestroyed++;
            play.Roles.AddValueLoss(lostDollars, isOrb);
            ReplaySampler.NoteValueLoss(lostDollars, isOrb, vid, destroyed);
            HostRequestCheckmateScan();
        }

        private void Awake()
        {
            try
            {
                DirectorRuntime runtime = DirectorRuntime.Create(this);
                _runtime = runtime;
                Instance = this;
            }
            catch (Exception e)
            {
                _runtime = null;
                enabled = false;
                WLog.Exception("director_awake_error", e, secret: false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _runtime?.LobbySettingsFeature.Dispose();
            _runtime?.ModIntegrityFeature.Dispose();
            _runtime = null;
            TeardownBus();
        }

        private void Update()
        {
            DirectorRuntime? runtime = _runtime;
            if (runtime == null) return;
            try
            {
                long now = SessionClock.NowMs();
                runtime.UpdatePipeline.Tick(now);
            }
            catch (Exception e)
            {
                WLog.Exception("tick_error", e, secret: false, ("stage", "pipeline"));
            }
        }

        private void LateUpdate()
        {
            try
            {
                EnemyMapIcons.LateTick();
            }
            catch (Exception e)
            {
                WLog.Exception("latetick_error", e, secret: false, ("stage", "map_icons"));
            }
        }

        private void OnApplicationQuit()
        {
            try
            {
                ClearCosmeticGrantState("app_quit");
            }
            catch (Exception e)
            {
                WLog.Exception("app_quit_flush_error", e);
            }

            try
            {
                Debugging.StructuredLog.FlushDeferredSecrets("app_quit");
            }
            catch (Exception e)
            {
                WLog.Exception("app_quit_secretlog_flush_error", e);
            }
        }

        private void TickVoice()
        {
            bool localAlive = !IsDeadActorClient(LocalActor);
            GamePhase effectivePhase = Runtime.WinCeremonyFeature.WinCeremonyActive ? GamePhase.GameOver : ClientPhase;
            VoicePlanKind kind = VoiceRules.DecideKind(
                effectivePhase, LocalRoleClient, localAlive, _cfgMirror.NecroVoiceMode);
            bool deadCueMute = VoiceRules.IsDeadCueMuteActive(effectivePhase, localAlive);
            Runtime.VoiceDriver.Tick(kind, Runtime.VoiceIsEavesdropTarget, Runtime.VoiceIsDeadActor, deadCueMute);
        }

        private void TickCursorMirror()
        {
            var bindings = Plugin.Bindings;
            if (bindings != null) _cursorMirror.SizeScale = bindings.CursorMirrorScale.Value;

            bool panelOpen = Runtime.UiManager.IsLayerVisible(WerewolfUIManager.MeetingLayer)
                             || Runtime.LobbySettingsFeature.IsPanelVisible
                             || Runtime.ModIntegrityFeature.IsAnyUiVisible
                             || _manualOverlay.IsOpen
                             || _scenarioSelect.IsOpen
                             || _resultScreen.Visible
                             || _replayViewer.DemoActive;
            bool active = panelOpen && Cursor.lockState == CursorLockMode.None;
            if (active) EnsurePanelBuilt(_cursorMirror);
            _cursorMirror.Tick(active);
        }

        private void TickManualOverlay()
        {
            EnsurePanelBuilt(_manualOverlay);
            if (!_manualOverlay.Exists) return;

            var uiBindings = Plugin.Bindings;
            if (uiBindings != null)
            {
                _manualOverlay.PositionOffset = new Vector2(
                    uiBindings.HudOffsetX.Value, uiBindings.HudOffsetY.Value);
            }

            KeyCode key = Plugin.ManualKey != null ? Plugin.ManualKey.Value : KeyCode.None;
            bool lobbyMenu = IsLobbyMenu();
            bool available = lobbyMenu || !IsMenuLevel();
            _manualOverlay.Tick(key, available, Runtime.RoleRevealFeature.CinematicVisible, lobbyMenu);
        }

        private void TickScenarioSelect()
        {
            if (!IsMainMenu())
            {
                if (_scenarioSelect.IsOpen) _scenarioSelect.Close("left_title");
                return;
            }

            EnsurePanelBuilt(_scenarioSelect);
            if (!_scenarioSelect.Exists) return;

            if (!_scenarioSelect.IsOpen && IsTitleScreenBuilt() && ScenarioSelection.ConsumeOpenRequest())
            {
                ConfigBindings? bindings = Plugin.Bindings;
                _scenarioSelect.Open(
                    bindings != null ? (Func<ScenarioId, bool>)bindings.IsScenarioCleared : null,
                    id => ScenarioRun.TryStart(id));
            }

            _scenarioSelect.Tick();
        }

        private static readonly HashSet<string> _sessionProbeErrorsLogged = new HashSet<string>();

        private static void LogSessionProbeError(string probe, Exception e)
        {
            if (!_sessionProbeErrorsLogged.Add(probe)) return;
            WLog.Exception("session_probe_error", e, secret: false, ("probe", probe));
        }

        private static bool IsLobbyMenu()
        {
            try
            {
                return Session.InLobbyMenu;
            }
            catch (Exception e)
            {
                LogSessionProbeError("lobby_menu", e);
                return false;
            }
        }

        private static bool IsTitleScreenBuilt()
        {
            try
            {
                return Session.IsTitleScreenBuilt;
            }
            catch (Exception e)
            {
                LogSessionProbeError("title_built", e);
                return false;
            }
        }

        private static bool IsMainMenu()
        {
            try
            {
                return Session.InMainMenu;
            }
            catch (Exception e)
            {
                LogSessionProbeError("main_menu", e);
                return false;
            }
        }

        private static bool IsMenuLevel()
        {
            try
            {
                return Session.InMenuLevel;
            }
            catch (Exception e)
            {
                LogSessionProbeError("menu_level", e);
                return true;
            }
        }

        internal void EnsurePanelBuilt(IClientPanel panel)
        {
            if (panel.Exists) return;
            if (!Runtime.UiManager.EnsureCreated(gameObject)) return;
            Transform? root = Runtime.UiManager.GetLayerRoot(panel.LayerName);
            if (root == null) return;
            try
            {
                panel.Build(root);
            }
            catch (Exception e)
            {
                WLog.Exception("panel_build_error", e, secret: false,
                    ("layer", panel.LayerName));
                try
                {
                    panel.Destroy();
                }
                catch (Exception cleanupError)
                {
                    WLog.Exception("panel_build_cleanup_error", cleanupError, secret: false,
                        ("layer", panel.LayerName));
                }
            }
        }

        private string BusMode() => _bus is PhotonRpcBus ? "photon" : "loopback";
    }
}
