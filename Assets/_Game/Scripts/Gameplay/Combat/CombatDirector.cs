using UnityEngine;
using Game.Core;
using Game.Core.Events;
using Game.Core.Gates;

namespace Game.Gameplay
{
    public class CombatDirector : MonoBehaviour
    {
        [SerializeField] private SquadController squad;
        [SerializeField] private TrackScroller scroller;
        [SerializeField] private float victoryDistance = 120f;
        [SerializeField] private HordeSpawner spawner;
        [SerializeField] private MeleeEngagementManager meleeManager;

        public MeleeEngagementManager MeleeManager => meleeManager;
        public void SetMeleeManager(MeleeEngagementManager manager) => meleeManager = manager;

        private SquadController _squad;
        private bool _squadExplicitlySet;
        public SquadController Squad
        {
            get
            {
                if (!_squadExplicitlySet && _squad == null)
                {
                    _squad = squad != null ? squad : (GetComponent<SquadController>() ?? GetComponentInParent<SquadController>());
                }
                return _squad;
            }
            private set
            {
                _squad = value;
                _squadExplicitlySet = true;
            }
        }

        private TrackScroller _scroller;
        private bool _scrollerExplicitlySet;
        public TrackScroller Scroller
        {
            get
            {
                if (!_scrollerExplicitlySet && _scroller == null)
                {
                    _scroller = scroller != null ? scroller : (GetComponent<TrackScroller>() ?? GetComponentInParent<TrackScroller>());
                }
                return _scroller;
            }
            private set
            {
                _scroller = value;
                _scrollerExplicitlySet = true;
            }
        }

        public IGameStateMachine StateMachine { get; private set; }

        public float VictoryDistance
        {
            get => victoryDistance;
            set => victoryDistance = value;
        }

        private HordeSpawner _spawner;
        private bool _spawnerExplicitlySet;
        public HordeSpawner Spawner
        {
            get
            {
                if (!_spawnerExplicitlySet && _spawner == null)
                {
                    _spawner = spawner != null ? spawner : FindFirstObjectByType<HordeSpawner>();
                }
                return _spawner;
            }
            private set
            {
                _spawner = value;
                _spawnerExplicitlySet = true;
            }
        }

        public bool IsResolved { get; private set; }
        public IEventBus EventBus { get; set; }

        private StageModifierController _stageModifiers;
        public StageModifierController StageModifiers
        {
            get => _stageModifiers != null ? _stageModifiers : (_stageModifiers = FindAnyObjectByType<LevelDirector>()?.StageModifierController);
            set
            {
                _stageModifiers = value;
                UpdateWeaponRangeMultiplier();
            }
        }

        public void SetStageModifiers(StageModifierController modifiers)
        {
            StageModifiers = modifiers;
        }

        public float EffectiveSquadRangeMultiplier => StageModifiers != null ? StageModifiers.CurrentSquadRangeMultiplier : 1f;

        public float GetModifiedRange(float baseRange)
        {
            return baseRange * EffectiveSquadRangeMultiplier;
        }

        public void UpdateWeaponRangeMultiplier()
        {
            var weapon = GetComponent<WeaponController>() ?? GetComponentInParent<WeaponController>() ?? GetComponentInChildren<WeaponController>();
            if (weapon != null)
            {
                weapon.RangeMultiplier = EffectiveSquadRangeMultiplier;
            }
        }

        public void Initialize(SquadController squad, TrackScroller scroller, IGameStateMachine stateMachine, float victoryDistance, HordeSpawner spawner = null, IEventBus eventBus = null, StageModifierController stageModifiers = null)
        {
            this.squad = squad;
            Squad = squad;
            this.scroller = scroller;
            Scroller = scroller;
            StateMachine = stateMachine;
            VictoryDistance = victoryDistance;
            this.spawner = spawner;
            Spawner = spawner;
            EventBus = eventBus;
            _stageModifiers = stageModifiers;
            UpdateWeaponRangeMultiplier();
            IsResolved = false;
        }

        private void Awake()
        {
            if (Squad == null && squad != null)
            {
                Squad = squad;
            }

            if (Scroller == null && scroller != null)
            {
                Scroller = scroller;
            }

            if (Spawner == null && spawner != null)
            {
                Spawner = spawner;
            }
        }

        private void Start()
        {
            if (Squad == null)
            {
                Squad = GetComponent<SquadController>() ?? GetComponentInParent<SquadController>();
            }

            if (Scroller == null)
            {
                Scroller = GetComponent<TrackScroller>() ?? GetComponentInParent<TrackScroller>();
            }

            if (Spawner == null)
            {
                Spawner = FindFirstObjectByType<HordeSpawner>();
            }

            if (meleeManager == null)
            {
                meleeManager = GetComponent<MeleeEngagementManager>() ?? GetComponentInChildren<MeleeEngagementManager>();
            }

            if (StateMachine == null)
            {
                var sm = new GameStateMachine(null, GameState.Boot);
                sm.TryTransition(GameState.Run);
                StateMachine = sm;
            }
        }

        public bool ResolveEnemyContact(EnemyController enemy)
        {
            if (IsResolved || enemy == null || !enemy.IsActiveInPool)
            {
                return false;
            }

            if (enemy.IsEngaged)
            {
                return false;
            }

            if (meleeManager != null)
            {
                return meleeManager.Engage(enemy);
            }

            if (Squad != null && Squad.SquadCount > 0)
            {
                int cost = enemy != null ? enemy.ContactCost : 1;
                int lost = Mathf.Min(cost, Squad.SquadCount);
                Squad.Remove(cost);
                EventBus?.Publish(new EnemyConsumedEvent(enemy?.ArchetypeId ?? "walker", lost));
                enemy.Recycle();
                return true;
            }

            enemy.Recycle();
            TriggerDefeat();
            return true;
        }

        public void TriggerDefeat()
        {
            if (IsResolved)
            {
                return;
            }

            IsResolved = true;

            if (Scroller != null)
            {
                Scroller.IsPaused = true;
            }

            StopCombat();
            StateMachine?.TryTransition(GameState.Defeat);
        }

        public bool CheckVictory()
        {
            if (IsResolved || StateMachine == null || StateMachine.CurrentState != GameState.Run)
            {
                return false;
            }

            if (Scroller != null && Scroller.DistanceTraveled >= VictoryDistance)
            {
                TriggerVictory();
                return true;
            }

            return false;
        }

        public void TriggerVictory()
        {
            if (IsResolved)
            {
                return;
            }

            IsResolved = true;

            if (Scroller != null)
            {
                Scroller.IsPaused = true;
            }

            StopCombat();
            StateMachine?.TryTransition(GameState.Victory);
        }

        private void StopCombat()
        {
            var weapon = GetComponent<WeaponController>() ?? GetComponentInParent<WeaponController>();
            if (weapon != null)
            {
                weapon.IsFiring = false;
            }

            Spawner?.ClearActiveEnemies();
            meleeManager?.ClearAll();
        }

        // Victory check runs on Update tick based on distance traveled by TrackScroller
        public void Tick(float deltaTime)
        {
            UpdateWeaponRangeMultiplier();
            if (!IsResolved && StateMachine?.CurrentState == GameState.Run)
            {
                CheckVictory();
            }
        }

        private void Update() => Tick(Time.deltaTime);

        // Physical zombie contacts are resolved asynchronously by the physics trigger cycle
        public void OnTriggerEnter(Collider other)
        {
            if (other == null)
            {
                return;
            }

            var enemy = other.GetComponent<EnemyController>()
                ?? other.GetComponentInParent<EnemyController>()
                ?? (other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<EnemyController>() : null);

            if (enemy != null)
            {
                ResolveEnemyContact(enemy);
            }
        }
    }
}
