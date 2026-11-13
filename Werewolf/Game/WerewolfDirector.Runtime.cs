#nullable enable
using System;
using Werewolf.Core;
using Werewolf.Scenarios.Game;
using Werewolf.UI;
using Werewolf.Vanilla;

namespace Werewolf.Game
{
    public sealed partial class WerewolfDirector
    {
        private sealed class DirectorRuntime
        {
            public PlayerRegistry Registry { get; }
            public VoiceMixerDriver VoiceDriver { get; }
            public Func<int, bool> VoiceIsEavesdropTarget { get; }
            public Func<int, bool> VoiceIsDeadActor { get; }

            public TruckWarper TruckWarper { get; }
            public MovementFreezer MovementFreezer { get; }
            public EnemyFreezer EnemyFreezer { get; }
            public WerewolfUIManager UiManager { get; }
            public VotePanel VotePanel { get; }

            public BomberClientController BomberFeature { get; }
            public ShamanClientController ShamanFeature { get; }
            public ModIntegrityController ModIntegrityFeature { get; }
            public TutorialController TutorialFeature { get; }
            public ScenarioController ScenarioFeature { get; }
            public MeetingChatController ChatFeature { get; }
            public ResultFlowController ResultFlowFeature { get; }
            public LobbySettingsController LobbySettingsFeature { get; }
            public RoleRevealController RoleRevealFeature { get; }
            public WinCeremonyController WinCeremonyFeature { get; }
            public MeetingScatterController ScatterFeature { get; }
            public RolesClientController RolesClientFeature { get; }
            public MeetingClientController MeetingClientFeature { get; }

            public IClientPanel[] RoundPanels { get; }
            public FaultIsolatedTickPipeline UpdatePipeline { get; }

            private DirectorRuntime(WerewolfDirector director)
            {
                Registry = new PlayerRegistry();

                VoiceDriver = new VoiceMixerDriver(Registry);
                VoiceIsEavesdropTarget = actor => VoiceRules.IsEavesdropTarget(
                    director.IsDeadActorClient(actor), director.IsFellowWerewolfActor(actor),
                    director._cfgMirror.NecroVoiceMode);
                VoiceIsDeadActor = director.IsDeadActorClient;

                TruckWarper = new TruckWarper();
                MovementFreezer = new MovementFreezer();
                EnemyFreezer = new EnemyFreezer();
                UiManager = new WerewolfUIManager();
                VotePanel = new VotePanel();
                VotePanel.OnVoteSubmit += director.SendVote;
                VotePanel.PlaySfx = (key, volume) =>
                {
                    director.EnsureSfxBuilt();
                    director._sfxPlayer.Play(key, volume);
                };
                VotePanel.SetTeamMarkerProvider(director.MarkedTeammateRole);

                VoteRow.SetDeadCueProvider(actor =>
                {
                    WerewolfDirector? current = Instance;
                    return current == null || current.ShouldShowDeadCuesClient(actor);
                });
                VoteRow.SetParticipantIdProvider(actor =>
                {
                    WerewolfDirector? current = Instance;
                    return current != null ? current.ParticipantIdFor(actor) : 0;
                });

                BomberFeature = new BomberClientController(director);
                ShamanFeature = new ShamanClientController(director);
                ModIntegrityFeature = new ModIntegrityController(director);
                TutorialFeature = new TutorialController(director);
                ScenarioFeature = new ScenarioController(director);
                ChatFeature = new MeetingChatController(director);
                ResultFlowFeature = new ResultFlowController(director);
                LobbySettingsFeature = new LobbySettingsController(director);
                RoleRevealFeature = new RoleRevealController(director);
                WinCeremonyFeature = new WinCeremonyController(director);
                ScatterFeature = new MeetingScatterController(director);
                RolesClientFeature = new RolesClientController(director);
                MeetingClientFeature = new MeetingClientController(director);

                RoundPanels = new IClientPanel[]
                {
                    VotePanel,
                    director._resultScreen,
                    director._cursorMirror,
                    director._startHoldOverlay,
                    director._replayViewer,
                    director._idBadgePresenter,
                };

                UpdatePipeline = director.CreateUpdatePipeline();
            }

            public static DirectorRuntime Create(WerewolfDirector director)
                => new DirectorRuntime(director);
        }

        private DirectorRuntime? _runtime;

        internal bool IsReady => _runtime != null;

        private DirectorRuntime Runtime => _runtime
            ?? throw new InvalidOperationException("WerewolfDirector is not initialized");
    }
}
