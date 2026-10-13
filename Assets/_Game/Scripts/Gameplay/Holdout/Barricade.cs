using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Holdout;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay
{
    public class Barricade : MonoBehaviour
    {
        [SerializeField] private HoldoutDefinition definition;
        [SerializeField] private int initialMaxHp = 200;
        [SerializeField] private int initialSurvivors = 5;
        [SerializeField] private float initialDuration = 15f;
        [SerializeField] private Collider barricadeCollider;

        private HoldoutRuntimeState _runtimeState;
        private IEventBus _eventBus;
        private readonly HashSet<EnemyController> _contactEnemies = new();
        private readonly List<EnemyController> _staleEnemies = new();
        private float _damageAccumulator;

        public event Action<Barricade> OnStateChanged;
        public event Action<Barricade, int> OnDamaged;
        public event Action<Barricade> OnDestroyed;

        public HoldoutDefinition Definition
        {
            get => definition;
            set
            {
                definition = value;
                _runtimeState = null;
            }
        }

        public Collider BarricadeCollider
        {
            get => barricadeCollider;
            set => barricadeCollider = value;
        }

        public IHoldoutState State => EnsureRuntimeState();
        public string HoldoutId => State.HoldoutId;
        public int CurrentHp => State.CurrentHp;
        public int MaxHp => State.MaxHp;
        public int TotalSurvivors => State.TotalSurvivors;
        public int SurvivorsSaved => State.SurvivorsSaved;
        public bool IsActive => State.IsActive;
        public bool IsDestroyed => State.IsBarricadeDestroyed;
        public int GuardedLane { get; private set; } = 1;

        public IReadOnlyCollection<EnemyController> ContactEnemies => _contactEnemies;

        public void EnsurePhysicsSetup()
        {
            gameObject.layer = CollisionLayers.PickupLayer;
            if (!TryGetComponent<Rigidbody>(out var rb))
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            Physics.IgnoreLayerCollision(CollisionLayers.EnemyLayer, CollisionLayers.PickupLayer, false);

            if (barricadeCollider == null)
            {
                barricadeCollider = GetComponent<Collider>();
            }
            if (barricadeCollider == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(10f, 2f, 2f);
                barricadeCollider = box;
            }
            if (barricadeCollider != null)
            {
                barricadeCollider.isTrigger = true;
            }
        }

        private void Awake()
        {
            EnsurePhysicsSetup();
            EnsureRuntimeState();
        }

        private void Update()
        {
            if (_runtimeState != null && _runtimeState.IsActive)
            {
                TickDamage(Time.deltaTime);
            }
        }

        public void Initialize(
            HoldoutDefinition def,
            HoldoutRuntimeState state = null,
            int squadLane = 1,
            IEventBus eventBus = null)
        {
            definition = def;
            _eventBus = eventBus;
            GuardedLane = squadLane;
            _contactEnemies.Clear();
            _damageAccumulator = 0f;

            if (state != null)
            {
                _runtimeState = state;
            }
            else
            {
                _runtimeState = null;
                EnsureRuntimeState();
            }

            EnsurePhysicsSetup();
            OnStateChanged?.Invoke(this);
        }

        public void SetGuardedLane(int laneIndex)
        {
            GuardedLane = laneIndex;
        }

        public void RegisterZombieContact(EnemyController enemy)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                return;
            }

            _contactEnemies.Add(enemy);
        }

        public void UnregisterZombieContact(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }

            _contactEnemies.Remove(enemy);
        }

        public void TickDamage(float deltaTime)
        {
            if (_runtimeState == null || !_runtimeState.IsActive || deltaTime <= 0f)
            {
                return;
            }

            _staleEnemies.Clear();
            float totalDps = 0f;

            foreach (var enemy in _contactEnemies)
            {
                if (enemy == null || !enemy.IsAlive || !enemy.IsActiveInPool)
                {
                    _staleEnemies.Add(enemy);
                    continue;
                }

                if (enemy.LaneIndex != GuardedLane)
                {
                    float dps = enemy.ContactDPS > 0f
                        ? enemy.ContactDPS
                        : (definition != null ? definition.ZombieContactDps : 10f);
                    totalDps += dps;
                }
            }

            for (int i = 0; i < _staleEnemies.Count; i++)
            {
                _contactEnemies.Remove(_staleEnemies[i]);
            }

            if (totalDps > 0f)
            {
                _damageAccumulator += totalDps * deltaTime;
                if (_damageAccumulator >= 1f)
                {
                    int intDamage = (int)_damageAccumulator;
                    _damageAccumulator -= intDamage;
                    TakeDamage(intDamage);
                }
            }
        }

        public bool TakeDamage(int damage)
        {
            if (_runtimeState == null || damage <= 0)
            {
                return false;
            }

            bool destroyed = _runtimeState.TakeDamage(damage);
            OnDamaged?.Invoke(this, damage);
            OnStateChanged?.Invoke(this);

            if (destroyed)
            {
                OnDestroyed?.Invoke(this);
            }

            return destroyed;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null && other.TryGetComponent<EnemyController>(out var enemy))
            {
                RegisterZombieContact(enemy);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other != null && other.TryGetComponent<EnemyController>(out var enemy))
            {
                UnregisterZombieContact(enemy);
            }
        }

        private HoldoutRuntimeState EnsureRuntimeState()
        {
            if (_runtimeState == null)
            {
                string id = definition != null ? definition.Id : "holdout_runtime";
                int hp = definition != null ? definition.MaxHp : initialMaxHp;
                int survivors = definition != null ? definition.TotalSurvivors : initialSurvivors;
                float duration = definition != null ? definition.DurationSeconds : initialDuration;

                _runtimeState = new HoldoutRuntimeState(id, hp, survivors, duration);
            }

            return _runtimeState;
        }
    }
}
