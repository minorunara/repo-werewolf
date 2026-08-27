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
    public sealed partial class WerewolfDirector : MonoBehaviour
    {
        public static WerewolfDirector Instance { get; private set; }

        public PlayerRegistry Registry { get; private set; }

        private readonly RoomState _roomState = new RoomState();

        private readonly AutoStartWaitGate _autoStartWait = new AutoStartWaitGate();

        private readonly ResultSequence _resultSequence = new ResultSequence();

        private GameSession _session;
        private MeetingSession _meeting;
        private RolesSession _roles;
        private BombSession _bomber;
        private INetBus _bus;

        private readonly Dictionary<(int Actor, string Stage), BombDenyReason> _lastBombDeny =
            new Dictionary<(int Actor, string Stage), BombDenyReason>();

        private readonly StartReservations _reservations = new StartReservations();

        private readonly EnemyIgnoreRoster _enemyIgnoreRoster = new EnemyIgnoreRoster();
        private readonly BeaconEffect _beaconEffect = new BeaconEffect();

        private readonly CursorMirror _cursorMirror = new CursorMirror();

        private readonly ReplayViewerPanel _replayViewer = new ReplayViewerPanel();

        private readonly SfxPlayer _sfxPlayer = new SfxPlayer();

        private readonly ResultScreen _resultScreen = new ResultScreen();

        private GamePhase _clientPhase = GamePhase.Lobby;
        private readonly ClientRoundMirror _roundMirror = new ClientRoundMirror();
        private readonly ClientSettingsMirror _cfgMirror = new ClientSettingsMirror();

        private BomberClientController _bomberFeature;
        private ShamanClientController _shamanFeature;
        private ModIntegrityController _modIntegrityFeature;
        private TutorialController _tutorialFeature;
        private ScenarioController _scenarioFeature;
        private MeetingChatController _chatFeature;
        private ResultFlowController _resultFlowFeature;
        private LobbySettingsController _lobbySettingsFeature;
        private RoleRevealController _roleRevealFeature;
        private WinCeremonyController _winCeremonyFeature;
        private MeetingScatterController _scatterFeature;
        private RolesClientController _rolesClientFeature;
        private MeetingClientController _meetingClientFeature;

        private VoiceMixerDriver _voiceDriver;

        private Func<int, bool> _voiceIsEavesdropTarget;
        private Func<int, bool> _voiceIsDeadActor;

        private readonly MeetingClientState _meetingClient = new MeetingClientState();
        private TruckWarper _truckWarper;
        private MovementFreezer _movementFreezer;
        private EnemyFreezer _enemyFreezer;
        private WerewolfUIManager _uiManager;
        private VotePanel _votePanel;
        private readonly ManualOverlay _manualOverlay = new ManualOverlay();
        private readonly ScenarioSelectPanel _scenarioSelect = new ScenarioSelectPanel();

        private IClientPanel[] _roundPanels;

        private bool _resetArmed;

        public bool IsHostSessionActive => _session != null;

        public GamePhase HostPhase => _session?.Phase ?? GamePhase.Lobby;

        public RolesClientState RolesClient { get; } = new RolesClientState();

        public IdRosterClient IdRoster { get; } = new IdRosterClient();

        public int ParticipantIdFor(int actorNumber) => IdRoster.IdOf(actorNumber);

        public Role? LocalRoleClient => _roundMirror.LocalRole;

        public Role? MarkedTeammateRole(int actorNumber)
        {
            if (_roundMirror.LocalRole != Role.BlackCat && _roundMirror.LocalRole != Role.Werewolf && _roundMirror.LocalRole != Role.Bomber) return null;
            int[] set = _roundMirror.KnownWerewolves;
            if (set == null || set.Length == 0) return null;
            byte[] roles = _roundMirror.KnownTeammateRoles;
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
            if (actor < 0 || Registry == null || !Registry.Available) return null;
            if (_roundMirror.LocalRole != Role.BlackCat && _roundMirror.LocalRole != Role.Werewolf && _roundMirror.LocalRole != Role.Bomber) return null;
            if (_roundMirror.KnownWerewolves == null || _roundMirror.KnownWerewolves.Length == 0) return null;
            return MarkedTeammateRole(actor);
        }

        public bool IsFellowWerewolfActor(int actorNumber)
        {
            if (_roundMirror.LocalRole != Role.Werewolf) return false;
            int[] set = _roundMirror.KnownWerewolves;
            if (set == null || set.Length == 0) return false;
            byte[] roles = _roundMirror.KnownTeammateRoles;
            for (int i = 0; i < set.Length; i++)
            {
                if (set[i] != actorNumber) continue;
                if (roles != null && (Role)roles[i] == Role.Bomber) return false;
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
                && _voiceIsEavesdropTarget != null && _voiceIsEavesdropTarget(speakerActor);
            return VoiceRules.ShouldShowDeadCues(
                ClientPhase, localAlive, IsDeadActorClient(speakerActor), eavesdropAudible);
        }

        public bool ShouldShowDeadTextClient(int speakerActor)
        {
            bool localAlive = !IsDeadActorClient(LocalActor);
            return VoiceRules.ShouldShowDeadText(
                ClientPhase, localAlive, IsDeadActorClient(speakerActor));
        }

        public GamePhase ClientPhase => _session != null ? _session.Phase : _clientPhase;

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
            GameConfig cfg = Plugin.GameConfig;
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
            GameConfig cfg = Plugin.GameConfig;
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
            if (_roles == null || !Session.IsHost) return;
            HostValuablesDestroyed = 0;
            _roles.FreezeBase(totalDollars);
        }

        public void HostAddValueLoss(float lostDollars, bool isOrb, int vid, bool destroyed)
        {
            if (_roles == null || !Session.IsHost) return;
            if (destroyed) HostValuablesDestroyed++;
            _roles.AddValueLoss(lostDollars, isOrb);
            ReplaySampler.NoteValueLoss(lostDollars, isOrb, vid, destroyed);
            HostRequestCheckmateScan();
        }

        private void Awake()
        {
            Instance = this;
            WLog.Line("director_awake", secret: false,
                ("host", gameObject.name),
                ("scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            Registry = new PlayerRegistry();
            Registry.Initialize();

            _voiceDriver = new VoiceMixerDriver(Registry);
            _voiceIsEavesdropTarget = actor => VoiceRules.IsEavesdropTarget(
                IsDeadActorClient(actor), IsFellowWerewolfActor(actor), _cfgMirror.NecroVoiceMode);
            _voiceIsDeadActor = IsDeadActorClient;

            _truckWarper = new TruckWarper();
            _movementFreezer = new MovementFreezer();
            _enemyFreezer = new EnemyFreezer();
            _uiManager = new WerewolfUIManager();
            _votePanel = new VotePanel();
            _votePanel.OnVoteSubmit += SendVote;
            _votePanel.PlaySfx = (key, volume) =>
            {
                EnsureSfxBuilt();
                _sfxPlayer.Play(key, volume);
            };
            _votePanel.SetTeamMarkerProvider(MarkedTeammateRole);
            VoteRow.SetDeadCueProvider(actor =>
            {
                WerewolfDirector dir = Instance;
                return dir == null || dir.ShouldShowDeadCuesClient(actor);
            });
            VoteRow.SetParticipantIdProvider(actor =>
            {
                WerewolfDirector dir = Instance;
                return dir != null ? dir.ParticipantIdFor(actor) : 0;
            });

            _bomberFeature = new BomberClientController(this);
            _shamanFeature = new ShamanClientController(this);
            _modIntegrityFeature = new ModIntegrityController(this);
            _tutorialFeature = new TutorialController(this);
            _scenarioFeature = new ScenarioController(this);
            _chatFeature = new MeetingChatController(this);
            _resultFlowFeature = new ResultFlowController(this);
            _lobbySettingsFeature = new LobbySettingsController(this);
            _roleRevealFeature = new RoleRevealController(this);
            _winCeremonyFeature = new WinCeremonyController(this);
            _scatterFeature = new MeetingScatterController(this);
            _rolesClientFeature = new RolesClientController(this);
            _meetingClientFeature = new MeetingClientController(this);

            _roundPanels = new IClientPanel[]
            {
                _votePanel,
                _resultScreen, _cursorMirror,
                _startHoldOverlay,
                _replayViewer,
                _idBadgePresenter,
            };
        }

        private void OnDestroy()
        {
            WLog.Line("director_destroyed", secret: false,
                ("hadSession", _session != null),
                ("host", gameObject != null ? gameObject.name : "?"),
                ("scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            if (Instance == this) Instance = null;
            TeardownBus();
        }

        private void Update()
        {
            try
            {
                EnsureClientBus();

                long now = SessionClock.NowMs();

                _modIntegrityFeature.Tick(now);

                WorldgenItemBindings.TryInitialize();

                WorldgenMapBinding.TryInitialize();

                TickAutoStartWait(now);

                _session?.Tick(now);
                _meeting?.Tick(now);
                TickCorpseReportCancel(now);
                _roles?.Tick(now);
                _winCeremonyFeature.TickHost(now);
                _beaconEffect.Tick(now);

                if (Session.IsHost
                    && _resultSequence.TickShouldReturn(now))
                {
                    InvokeReturnToLobby();
                }

                _chatFeature.Tick(now);

                _resultFlowFeature.Tick(now);

                _lobbySettingsFeature.Tick(now);

                TickManualOverlay();

                TickScenarioSelect();

                TickStartHold(now);

                _meetingClientFeature.Tick(now);

                _scatterFeature.Tick(now);

                _enemyFreezer?.Tick(
                    Plugin.GameConfig != null ? Plugin.GameConfig.MeetingEnemyRespawnScalePct
                                              : EnemyRespawnScale.MaxPercent,
                    Time.deltaTime);

                _rolesClientFeature.Tick(now);

                _bomberFeature.Tick(now);

                _shamanFeature.Tick(now);

                TickIdBadges();

                TickVoice();

                _roleRevealFeature.Tick(now);

                TickCursorMirror();

                _tutorialFeature.Tick(now);

                _scenarioFeature.Tick(now);

                _meetingClientFeature.TickDeathRevealGuard();

                _winCeremonyFeature.Tick(now);

                EnemyMapIcons.Tick();

                ReplaySampler.Tick();

                MapHidePatch.Tick();

                Patches.ValuableMapSyncPatch.Tick();

                bool inLobby = Session.InMenuLevel || Session.InLobby;
                if (_resetArmed && inLobby)
                {
                    ResetToLobby("level_left");
                }
                else if (!inLobby && (_session != null || _clientPhase != GamePhase.Lobby))
                {
                    _resetArmed = true;
                }
            }
            catch (Exception e)
            {
                WLog.Line("tick_error", secret: false, ("err", e.Message));
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
                WLog.Line("latetick_error", secret: false, ("err", e.Message));
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
                WLog.Line("app_quit_flush_error", secret: false, ("err", e.Message));
            }
        }

        private void TickVoice()
        {
            if (_voiceDriver == null) return;
            bool localAlive = !IsDeadActorClient(LocalActor);
            GamePhase effectivePhase = _winCeremonyFeature.WinCeremonyActive ? GamePhase.GameOver : ClientPhase;
            VoicePlanKind kind = VoiceRules.DecideKind(
                effectivePhase, LocalRoleClient, localAlive, _cfgMirror.NecroVoiceMode);
            bool deadCueMute = VoiceRules.IsDeadCueMuteActive(effectivePhase, localAlive);
            _voiceDriver.Tick(kind, _voiceIsEavesdropTarget, _voiceIsDeadActor, deadCueMute);
        }

        private void TickCursorMirror()
        {
            var bindings = Plugin.Bindings;
            if (bindings != null) _cursorMirror.SizeScale = bindings.CursorMirrorScale.Value;

            bool panelOpen = _uiManager.IsLayerVisible(WerewolfUIManager.MeetingLayer)
                             || _lobbySettingsFeature.IsPanelVisible
                             || _modIntegrityFeature.IsAnyUiVisible
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
            _manualOverlay.Tick(key, available, _roleRevealFeature.CinematicVisible, lobbyMenu);
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
                ConfigBindings bindings = Plugin.Bindings;
                _scenarioSelect.Open(
                    bindings != null ? (Func<ScenarioId, bool>)bindings.IsScenarioCleared : null,
                    id => ScenarioRun.TryStart(id));
            }

            _scenarioSelect.Tick();
        }

        private static bool IsLobbyMenu()
        {
            try
            {
                return Session.InLobbyMenu;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTitleScreenBuilt()
        {
            try
            {
                return Session.IsTitleScreenBuilt;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMainMenu()
        {
            try
            {
                return Session.InMainMenu;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMenuLevel()
        {
            try
            {
                return Session.InMenuLevel;
            }
            catch
            {
                return true;
            }
        }

        internal void EnsurePanelBuilt(IClientPanel panel)
        {
            if (panel.Exists) return;
            if (!_uiManager.EnsureCreated(gameObject)) return;
            Transform root = _uiManager.GetLayerRoot(panel.LayerName);
            if (root == null) return;
            try
            {
                panel.Build(root);
            }
            catch (Exception e)
            {
                WLog.Line("panel_build_error", secret: false,
                    ("layer", panel.LayerName), ("err", e.Message));
            }
        }

        private string BusMode() => _bus is PhotonRpcBus ? "photon" : "loopback";
    }
}
