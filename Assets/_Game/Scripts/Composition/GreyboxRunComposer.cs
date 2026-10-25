using System.Collections.Generic;
using Game.Core;
using Game.Core.Analytics;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using Game.Gameplay.Tutorial;
using Game.Meta.Debut;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Stages;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using Game.Presentation;
using Game.Presentation.Settings;
using Game.Presentation.UI;
using UnityEngine;
using AudioSettings = Game.Presentation.Settings.AudioSettings;

namespace Game.Composition
{
    // Roda antes de todo consumidor: o EventBus que o SceneBuilder usa só existe em tempo de edição
    // e não é serializado, então a cena carregada do disco precisa do próprio barramento (GH #84).
    [DefaultExecutionOrder(-1000)]
    public class GreyboxRunComposer : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private HordeSpawner hordeSpawner;
        [SerializeField] private StatusEffectDirector statusDirector;
        [SerializeField] private GeneralAbilityController abilityController;
        [SerializeField] private GeneralAbilityHud abilityHud;
        [SerializeField] private GeneralAbilityAudio abilityAudio;
        [SerializeField] private GeneralAbilityExplosionVfx abilityVfx;
        [SerializeField] private DebugTextOverlay debugOverlay;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private PauseController pauseController;

        public IEventBus EventBus { get; private set; }
        public IGameStateMachine StateMachine { get; private set; }
        public UIManager UIManager => uiManager;
        public PauseController PauseController => pauseController;

        public void Configure(
            CombatDirector director,
            HordeSpawner spawner,
            StatusEffectDirector status,
            GeneralAbilityController controller,
            GeneralAbilityHud hud,
            GeneralAbilityAudio audio = null,
            GeneralAbilityExplosionVfx vfx = null,
            DebugTextOverlay overlay = null)
        {
            combatDirector = director;
            hordeSpawner = spawner;
            statusDirector = status;
            abilityController = controller;
            abilityHud = hud;
            abilityAudio = audio;
            abilityVfx = vfx;
            debugOverlay = overlay;
        }

        public void ConfigureUi(UIManager manager, PauseController pause = null)
        {
            uiManager = manager;
            pauseController = pause;
        }

        public void Compose(IEventBus eventBus)
        {
            EventBus = eventBus;
            StateMachine = new GameStateMachine(eventBus, GameState.Hub);

            if (combatDirector != null)
            {
                combatDirector.EventBus = eventBus;
                combatDirector.Initialize(
                    combatDirector.Squad,
                    combatDirector.Scroller,
                    StateMachine,
                    combatDirector.VictoryDistance,
                    combatDirector.Spawner,
                    eventBus,
                    combatDirector.StageModifiers);
            }

            if (hordeSpawner != null)
            {
                hordeSpawner.EventBus = eventBus;
            }

            if (statusDirector != null)
            {
                statusDirector.Initialize(statusDirector.Catalog, statusDirector.Interactions, eventBus);
            }

            foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                enemy.SetEventBus(eventBus);
            }

            if (abilityController != null)
            {
                abilityController.Initialize(abilityController.Definition, eventBus, generalTransform: abilityController.transform);
            }

            if (abilityHud != null)
            {
                abilityHud.Initialize(abilityController, eventBus);
            }

            if (abilityAudio != null)
            {
                abilityAudio.Initialize(eventBus);
            }

            if (abilityVfx != null)
            {
                abilityVfx.Initialize(eventBus);
            }

            if (debugOverlay != null)
            {
                var squad = combatDirector != null ? combatDirector.Squad : FindFirstObjectByType<SquadController>();
                var scroller = combatDirector != null ? combatDirector.Scroller : FindFirstObjectByType<TrackScroller>();
                var weapon = combatDirector != null ? combatDirector.GetComponent<WeaponController>() : FindFirstObjectByType<WeaponController>();
                debugOverlay.Initialize(eventBus, squad, combatDirector, scroller, weapon, abilityController);
            }

            if (uiManager == null)
            {
                uiManager = FindFirstObjectByType<UIManager>();
            }

            if (pauseController == null)
            {
                pauseController = FindFirstObjectByType<PauseController>();
            }

            if (uiManager != null)
            {
                var analytics = new MockAnalyticsService();
                uiManager.Initialize(StateMachine, analytics, eventBus);

                if (pauseController != null)
                {
                    pauseController.Initialize(
                        StateMachine,
                        uiManager,
                        uiManager.GetWindow<PauseView>(),
                        uiManager.GetWindow<OptionsView>());
                }

                var wallet = new Wallet(eventBus);
                wallet.Add(CurrencyKind.Coins, 100, "initial");
                var stageMapService = new StageMapService();
                var sink = new RewardSink(wallet);

                var hubView = uiManager.GetWindow<HubView>();
                if (hubView != null)
                {
                    hubView.Initialize(
                        wallet,
                        stageMapService,
                        StateMachine,
                        uiManager,
                        eventBus,
                        analytics);
                    hubView.DebutJourneyHook = () => uiManager.OpenWindow<DebutJourneyView>();
                }

                var resultView = uiManager.GetWindow<ResultView>();
                if (resultView != null)
                {
                    var presenter = new ResultRewardPresenter(
                        new RewardCalculator(),
                        wallet,
                        sink,
                        StateMachine,
                        analytics,
                        resultView);
                    resultView.Initialize(presenter);
                }

                var optionsView = uiManager.GetWindow<OptionsView>();
                if (optionsView != null)
                {
                    optionsView.Initialize(
                        new AudioSettings(),
                        new LocalizationSettings(),
                        new PlayerPrefsAbilitySettings());
                }

                var debutView = uiManager.GetWindow<DebutJourneyView>();
                if (debutView != null)
                {
                    var debutService = new DebutJourneyService();
                    var track = new ProgressTrack(debutService.Definition);
                    debutView.Initialize(debutService, track, currentLoginDay: 1, sink);
                }

                var tutorialView = uiManager.GetWindow<TutorialGuideView>();
                if (tutorialView != null)
                {
                    var tutorialController = new TutorialGuideController(new FtueStepProgress(), analytics);
                    tutorialView.Initialize(tutorialController);
                }

                uiManager.OpenWindow<HubView>();
            }
        }

        private void Awake()
        {
            Compose(new EventBus());
        }
    }
}
