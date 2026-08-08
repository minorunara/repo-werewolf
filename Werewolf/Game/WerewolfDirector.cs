using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Werewolf.Core;
using Werewolf.Game.Patches;
using Werewolf.Net;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector : MonoBehaviour
    {
        public static WerewolfDirector Instance { get; private set; }

        public PlayerRegistry Registry { get; private set; }

        private readonly RoomState _roomState = new RoomState();

        private readonly LifecycleGate _lifecycleGate = new LifecycleGate();

        private readonly AutoStartWaitGate _autoStartWait = new AutoStartWaitGate();

        private readonly ResultSequence _resultSequence = new ResultSequence();

        private GameSession _session;
        private MeetingSession _meeting;
        private RolesSession _roles;
        private BombSession _bomber;
        private INetBus _bus;

        private readonly List<WPlayer> _pendingBots = new List<WPlayer>();
        private readonly Dictionary<int, Role> _pendingForcedRoles = new Dictionary<int, Role>();

        private readonly EnemyIgnoreRoster _enemyIgnoreRoster = new EnemyIgnoreRoster();
        private readonly BeaconEffect _beaconEffect = new BeaconEffect();

        private readonly PerkEffects _perkEffects = new PerkEffects();
        private readonly MeetingGaugePanel _gaugePanel = new MeetingGaugePanel();

        private readonly MeetingGaugePanel _playGaugePanel = new MeetingGaugePanel(
            "WW_PlayGaugePanel",
            panelSize: new Vector2(1560f, 200f), pos: new Vector2(-25f, 50f),
            anchor: new Vector2(1f, 0f), pivot: new Vector2(1f, 0f),
            scale: 0.45f, markerScale: 2.5f);

        private readonly HudPanel _hudPanel = new HudPanel();
        private readonly HudModel _hudModel = new HudModel();

        private readonly ToastPanel _toastPanel = new ToastPanel();
        private ToastQueue _toastQueue;

        private readonly TutorialPresenter _tutorialPresenter = new TutorialPresenter();

        private readonly CursorMirror _cursorMirror = new CursorMirror();

        private readonly RoleRevealCinematic _revealCinematic = new RoleRevealCinematic();

        private readonly BlackCatAwakeningToast _catAwakenToast = new BlackCatAwakeningToast();
        private Coroutine _catAwakenToastCoroutine;

        private readonly WolfStatusPanel _wolfStatusPanel = new WolfStatusPanel();
        private readonly WolfStatusModel _wolfStatusModel = new WolfStatusModel();

        private readonly CorpseReportHudPanel _corpseReportHud = new CorpseReportHudPanel();

        private readonly ValuableRecordHudPanel _valuableRecordHud = new ValuableRecordHudPanel();
        private readonly ValuableRecordHold _valuableRecordHold = new ValuableRecordHold();

        private readonly DeathRevealPanel _deathReveal = new DeathRevealPanel();
        private Coroutine _deathRevealCoroutine;
        private bool _deathRevealPending;

        private int _pendingBeaconAudit = -1;

        private readonly MeetingChatLog _chatLog = new MeetingChatLog();

        private const string MeetingChatSfxClipKey = "sfx_chat_message";
        private const float MeetingChatSfxVolumeScale = 1.5f;

        private bool _chatVoteBaselinePending;

        private readonly MeetingChatPanel _chatPanel = new MeetingChatPanel();
        private readonly List<string> _chatRecapDeaths = new List<string>();
        private int _chatRecapBeaconUses = MeetingRecap.Unknown;

        private int _chatRecapLostBaseline;

        private bool _chatSystemPosted;

        private List<List<int>> _lastScatterGroups;

        private bool _pendingMeetingTutorial;

        private readonly ConveneCountdown _conveneCountdown = new ConveneCountdown();
        private Coroutine _conveneTweenCoroutine;

        private readonly SfxPlayer _sfxPlayer = new SfxPlayer();

        private readonly ResultScreen _resultScreen = new ResultScreen();
        private readonly ResultCountdown _resultCountdown = new ResultCountdown();
        private int _roundGameOverAutoReturnSec;
        private int _lastResultCountdownSecond = -1;

        private readonly VoidMatchHold _voidMatchHold = new VoidMatchHold();
        private readonly VoidMatchPanel _voidMatchPanel = new VoidMatchPanel();

        private long _resultReturnArmedAtUnixMs;
        private bool _revealStarted;
        private bool _awakeningRevealStarted;
        private Coroutine _revealCoroutine;

        private CurseTargetSource _curseSource;

        private Role? _localRole;
        private int[] _knownWerewolves;
        private byte[] _knownTeammateRoles;
        private GamePhase _clientPhase = GamePhase.Lobby;
        private long _clientRoundEndUnixMs;
        private int _clientWerewolfCount;
        private readonly Dictionary<int, DeathCause> _deathMirror = new Dictionary<int, DeathCause>();
        private Dictionary<int, string> _displayNameCache;

        private string _lastPublishedBlob;
        private long _lobbyTickAtMs;

        private readonly LobbySettingsPanel _lobbySettingsPanel = new LobbySettingsPanel();
        private string _lastPanelBlob;
        private bool _lastPanelModeEnabled;
        private bool _lobbyPanelUserHidden;
        private string _debugInjectedBlob;

        private string _lobbyBlobMirror;

        private bool _clientMinimapHideEnabled;
        private bool _clientCatPossible;
        private bool _clientDebugSession;
        private ValuableMapMode _clientValuableMapMode = ValuableMapMode.MeetingSync;

        private NecroVoiceMode _clientNecroVoiceMode = NecroVoiceMode.NonWerewolfDead;

        private int? _clientExtraJumpCount;
        private int? _clientConveneSuppressStartSec;
        private int? _clientConveneSuppressAfterSec;
        private int? _clientHealIntervalSec;
        private bool? _clientOutfitChangeAllowed;

        private int[] _clientBombPack;

        private int[] _clientShamanPack;

        private VoiceMixerDriver _voiceDriver;

        private Func<int, bool> _voiceIsEavesdropTarget;
        private Func<int, bool> _voiceIsDeadActor;

        private readonly MeetingClientState _meetingClient = new MeetingClientState();
        private MeetingButton _meetingButton;
        private long _nextConveneHoldHintUnixMs;
        private TruckWarper _truckWarper;
        private ExtractionScatter _extractionScatter;
        private MovementFreezer _movementFreezer;
        private EnemyFreezer _enemyFreezer;
        private WerewolfUIManager _uiManager;
        private VotePanel _votePanel;
        private readonly MeetingMapOverlay _meetingMapOverlay = new MeetingMapOverlay();
        private readonly ManualOverlay _manualOverlay = new ManualOverlay();

        private IClientPanel[] _roundPanels;
        private long _clientWarpUnixMs;
        private bool _warpExecuted;
        private bool _meetingUiActive;

        private bool _votePendulumPlayed;

        private int _executionSfxWaitTicks = -1;
        private const int ExecutionSfxCurseWaitTicks = 3;

        private long _gameStartUnixMsClient;
        private long _lastMeetingEndUnixMsClient;
        private int _clientRevealDelaySec;

        private readonly CatAwakenToastGate _catAwakenGate = new CatAwakenToastGate();

        private Vector3? _warpVerifyTarget;
        private long _warpVerifyDeadlineMs;

        private bool _resetArmed;

        public bool IsHostSessionActive => _session != null;

        public GamePhase HostPhase => _session?.Phase ?? GamePhase.Lobby;

        public RolesClientState RolesClient { get; } = new RolesClientState();

        public IdRosterClient IdRoster { get; } = new IdRosterClient();

        public int ParticipantIdFor(int actorNumber) => IdRoster.IdOf(actorNumber);

        public Role? LocalRoleClient => _localRole;

        public Role? MarkedTeammateRole(int actorNumber)
        {
            if (_localRole != Role.BlackCat && _localRole != Role.Werewolf && _localRole != Role.Bomber) return null;
            int[] set = _knownWerewolves;
            if (set == null || set.Length == 0) return null;
            byte[] roles = _knownTeammateRoles;
            for (int i = 0; i < set.Length; i++)
            {
                if (set[i] != actorNumber) continue;
                if (roles != null && i < roles.Length && (Role)roles[i] == Role.Bomber) return Role.Bomber;
                return Role.Werewolf;
            }
            return null;
        }

        public Role? MarkedTeammateRoleForAvatar(PlayerAvatar avatar)
        {
            if (avatar == null || Registry == null || !Registry.Available) return null;
            if (_localRole != Role.BlackCat && _localRole != Role.Werewolf && _localRole != Role.Bomber) return null;
            if (_knownWerewolves == null || _knownWerewolves.Length == 0) return null;
            int actor = Registry.ResolveActor(avatar);
            return MarkedTeammateRole(actor);
        }

        public bool IsFellowWerewolfActor(int actorNumber)
        {
            if (_localRole != Role.Werewolf) return false;
            int[] set = _knownWerewolves;
            if (set == null || set.Length == 0) return false;
            byte[] roles = _knownTeammateRoles;
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
            return _deathMirror.ContainsKey(actorNumber);
        }

        public bool ShouldShowDeadCuesClient(int speakerActor)
        {
            bool localAlive = !IsDeadActorClient(LocalActor);
            VoicePlanKind kind = VoiceRules.DecideKind(
                ClientPhase, LocalRoleClient, localAlive, _clientNecroVoiceMode);
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

        public bool ClientMinimapHideEnabled => _clientMinimapHideEnabled;

        public ValuableMapMode ClientValuableMapMode => _clientValuableMapMode;

        public NecroVoiceMode ClientNecroVoiceMode => _clientNecroVoiceMode;

        public int ClientExtraJumpCount =>
            _clientExtraJumpCount ?? (Plugin.GameConfig != null ? Plugin.GameConfig.ExtraJumpCount : 0);

        public int ClientConveneSuppressStartSec =>
            _clientConveneSuppressStartSec ?? (Plugin.GameConfig != null ? Plugin.GameConfig.ConveneSuppressStartSec : 0);

        public int ClientConveneSuppressAfterSec =>
            _clientConveneSuppressAfterSec ?? (Plugin.GameConfig != null ? Plugin.GameConfig.ConveneSuppressAfterSec : 0);

        public int ClientHealIntervalSec =>
            _clientHealIntervalSec ?? (Plugin.GameConfig != null ? Plugin.GameConfig.HealIntervalSec : 3);

        public bool ClientOutfitChangeAllowed => _clientOutfitChangeAllowed ?? false;

        private int ClientBombPackValue(int index)
        {
            if (_clientBombPack != null && index >= 0 && index < _clientBombPack.Length)
                return _clientBombPack[index];
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
            if (_clientShamanPack != null && index >= 0 && index < _clientShamanPack.Length)
                return _clientShamanPack[index];
            GameConfig cfg = Plugin.GameConfig;
            if (cfg == null) return 0;
            return RoomStateKeys.EncodeShaman(cfg)[index];
        }

        public float ClientShamanGazeFullSec => ClientShamanPackValue(RoomStateKeys.ShamanIndex.GazeFullMs) / 1000f;

        public float ClientShamanGhostCooldownSec => ClientShamanPackValue(RoomStateKeys.ShamanIndex.GhostCooldownMs) / 1000f;

        public float ClientShamanStormWeakMeters => ClientShamanPackValue(RoomStateKeys.ShamanIndex.StormWeakCm) / 100f;

        public float ClientShamanStormMediumMeters => ClientShamanPackValue(RoomStateKeys.ShamanIndex.StormMediumCm) / 100f;

        public float ClientShamanStormStrongMeters => ClientShamanPackValue(RoomStateKeys.ShamanIndex.StormStrongCm) / 100f;

        public void HostFreezeGaugeBase(float totalDollars)
        {
            if (_roles == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            _roles.FreezeBase(totalDollars);
        }

        public void HostAddValueLoss(float lostDollars, bool isOrb)
        {
            if (_roles == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            _roles.AddValueLoss(lostDollars, isOrb);
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
                IsDeadActorClient(actor), IsFellowWerewolfActor(actor), _clientNecroVoiceMode);
            _voiceIsDeadActor = IsDeadActorClient;

            _meetingButton = new MeetingButton();
            _meetingButton.OnConvene = SendConveneRequest;
            _meetingButton.OnIncompleteHold = ShowConveneHoldHint;
            _truckWarper = new TruckWarper();
            _movementFreezer = new MovementFreezer();
            _enemyFreezer = new EnemyFreezer();
            _uiManager = new WerewolfUIManager();
            _votePanel = new VotePanel();
            _votePanel.OnVoteSubmit += SendVote;
            _votePanel.SetTeamMarkerProvider(MarkedTeammateRole);
            _gaugePanel.OnRevealBreak = () =>
            {
                EnsureSfxBuilt();
                _sfxPlayer.Play("sfx_gauge_break");
            };
            _gaugePanel.OnDeliveryReveal = () =>
            {
                EnsureSfxBuilt();
                _sfxPlayer.Play("sfx_delivery_register");
            };
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

            _roundPanels = new IClientPanel[]
            {
                _votePanel, _gaugePanel, _playGaugePanel, _hudPanel, _toastPanel,
                _revealCinematic, _catAwakenToast, _wolfStatusPanel, _conveneCountdown,
                _resultScreen, _deathReveal, _cursorMirror,
                _bomberHud, _bombIconPresenter, _bombWarningPresenter,
                _corpseReportHud,
                _valuableRecordHud,
                _checkmateReveal,
                _startHoldOverlay,
                _conveneHoldGauge,
                _chatPanel,
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

                long now = NowUnixMs();

                TickModIntegrity(now);

                WorldgenItemBindings.TryInitialize();

                WorldgenMapBinding.TryInitialize();

                TickAutoStartWait(now);

                _session?.Tick(now);
                _meeting?.Tick(now);
                TickCorpseReportCancel(now);
                _roles?.Tick(now);
                TickCheckmateHost(now);
                _beaconEffect.Tick(now);

                if (SemiFunc.IsMasterClientOrSingleplayer()
                    && _resultSequence.TickShouldReturn(now))
                {
                    InvokeReturnToLobby();
                }

                TickChatDebug(now);

                TickResultReturn();

                TickVoidMatch(now);

                TickLobbySettings(now);
                TickLobbySettingsPanelVisibility();

                TickManualOverlay();

                TickStartHold(now);

                TickMeetingClient(now);
                TickWarpVerify(now);

                TickRolesClient(now);

                TickBomberClient(now);

                TickShamanClient(now);

                TickIdBadges();

                TickVoice();

                TickCatAwakenToast(now);

                TickCursorMirror();

                TickTutorialPresenter();
                Plugin.Bindings?.TickTutorialReset();
                TickTutorialTriggers();

                _deathReveal.Tick();

                TickCheckmateClient();

                EnemyMapIcons.Tick();

                MapHidePatch.Tick();

                Patches.ValuableMapSyncPatch.Tick();

                bool inLobby = SemiFunc.MenuLevel() || SemiFunc.RunIsLobby();
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
            GamePhase effectivePhase = _checkmateVoiceOpen ? GamePhase.GameOver : ClientPhase;
            VoicePlanKind kind = VoiceRules.DecideKind(
                effectivePhase, LocalRoleClient, localAlive, _clientNecroVoiceMode);
            bool deadCueMute = VoiceRules.IsDeadCueMuteActive(effectivePhase, localAlive);
            _voiceDriver.Tick(kind, _voiceIsEavesdropTarget, _voiceIsDeadActor, deadCueMute);
        }

        private void TickCursorMirror()
        {
            var bindings = Plugin.Bindings;
            if (bindings != null) _cursorMirror.SizeScale = bindings.CursorMirrorScale.Value;

            bool panelOpen = _uiManager.IsLayerVisible(WerewolfUIManager.MeetingLayer)
                             || _lobbySettingsPanel.Visible
                             || _modIntegrityHeader.Visible
                             || _modIntegrityPanel.Visible
                             || _lobbyStartWarning.Visible
                             || _manualOverlay.IsOpen;
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
            _manualOverlay.Tick(key, available, _revealCinematic.Visible, lobbyMenu);
        }

        private static bool IsLobbyMenu()
        {
            try
            {
                return SemiFunc.RunIsLobbyMenu();
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
                return SemiFunc.MenuLevel();
            }
            catch
            {
                return true;
            }
        }

        private void EnsurePanelBuilt(IClientPanel panel)
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

        private static long NowUnixMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
