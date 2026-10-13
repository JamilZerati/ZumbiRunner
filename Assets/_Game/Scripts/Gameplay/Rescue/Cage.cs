using System;
using Game.Core;
using Game.Core.Events;
using Game.Core.Rescue;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay
{
    public class Cage : MonoBehaviour, IDamageable
    {
        [SerializeField] private CageDefinition definition;
        [SerializeField] private int laneIndex;
        [SerializeField] private int customHp;
        [SerializeField] private int customSurvivors;
        [SerializeField] private Collider cageCollider;

        private CageRuntimeState _runtimeState;
        private ISquad _squad;
        private IEventBus _eventBus;

        public event Action<Cage> OnStateChanged;
        public event Action<Cage> OnOpened;
        public event Action<Cage> OnLost;

        public CageDefinition Definition
        {
            get => definition;
            set
            {
                definition = value;
                _runtimeState = null;
            }
        }

        public int LaneIndex
        {
            get => laneIndex;
            set => laneIndex = value;
        }

        public string CageId => definition != null && !string.IsNullOrEmpty(definition.Id)
            ? definition.Id
            : (_runtimeState != null ? _runtimeState.CageId : "cage_default");

        public int SurvivorCount => _runtimeState != null
            ? _runtimeState.SurvivorCount
            : (customSurvivors > 0 ? customSurvivors : (definition != null ? definition.SurvivorCount : 3));

        public string LostEnemyArchetypeId => _runtimeState != null
            ? _runtimeState.LostEnemyArchetypeId
            : (definition != null && !string.IsNullOrEmpty(definition.LostEnemyArchetypeId) ? definition.LostEnemyArchetypeId : "walker");

        public CageRuntimeState RuntimeState => EnsureRuntimeState();

        public int CurrentHealth => EnsureRuntimeState().CurrentHp;
        public int MaxHealth => EnsureRuntimeState().MaxHp;
        public bool IsAlive => !EnsureRuntimeState().IsOpen && !EnsureRuntimeState().IsLost && EnsureRuntimeState().CurrentHp > 0;
        public bool IsOpen => EnsureRuntimeState().IsOpen;
        public bool IsLost => EnsureRuntimeState().IsLost;

        public Collider CageCollider
        {
            get => cageCollider;
            set => cageCollider = value;
        }

        private void Awake()
        {
            ConfigureComponents();
        }

        public void Initialize(CageDefinition def, int lane, ISquad squad = null, IEventBus eventBus = null, int hpOverride = 0, int survivorsOverride = 0)
        {
            definition = def;
            laneIndex = lane;
            customHp = hpOverride;
            customSurvivors = survivorsOverride;
            _squad = squad;
            _eventBus = eventBus;
            _runtimeState = null;
            EnsureRuntimeState();
            ConfigureComponents();
        }

        public void Initialize(CageRuntimeState state, ISquad squad = null, IEventBus eventBus = null)
        {
            _runtimeState = state;
            if (state != null)
            {
                laneIndex = state.LaneIndex;
            }
            _squad = squad;
            _eventBus = eventBus;
            ConfigureComponents();
        }

        public CageRuntimeState EnsureRuntimeState()
        {
            if (_runtimeState == null)
            {
                string id = definition != null && !string.IsNullOrEmpty(definition.Id) ? definition.Id : "cage_default";
                int hp = customHp > 0 ? customHp : (definition != null ? definition.MaxHp : 100);
                int survivors = customSurvivors > 0 ? customSurvivors : (definition != null ? definition.SurvivorCount : 3);
                string archetype = definition != null && !string.IsNullOrEmpty(definition.LostEnemyArchetypeId) ? definition.LostEnemyArchetypeId : "walker";
                _runtimeState = new CageRuntimeState(id, laneIndex, hp, survivors, archetype);
            }
            return _runtimeState;
        }

        public void ConfigureComponents()
        {
            gameObject.layer = CollisionLayers.PickupLayer;
            if (!TryGetComponent<Rigidbody>(out var rb))
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            if (cageCollider == null)
            {
                cageCollider = GetComponent<Collider>();
            }
            if (cageCollider == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(1.5f, 1.5f, 1.5f);
                cageCollider = box;
            }
            if (cageCollider != null)
            {
                cageCollider.enabled = IsAlive;
            }
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive)
            {
                return;
            }

            var state = EnsureRuntimeState();
            bool opened = state.TakeDamage(damage.Amount);

            if (opened)
            {
                if (cageCollider != null)
                {
                    cageCollider.enabled = false;
                }

                if (_squad != null)
                {
                    _squad.Add(state.SurvivorCount);
                }

                if (_eventBus != null)
                {
                    _eventBus.Publish(new SurvivorsRescuedEvent(state.SurvivorCount, state.LaneIndex, state.CageId));
                }

                OnOpened?.Invoke(this);
            }

            OnStateChanged?.Invoke(this);
        }

        public bool CheckEscape(float generalZ)
        {
            if (!IsAlive)
            {
                return false;
            }

            if (generalZ >= transform.position.z)
            {
                var state = EnsureRuntimeState();
                bool lost = state.MarkLost();
                if (lost)
                {
                    if (cageCollider != null)
                    {
                        cageCollider.enabled = false;
                    }

                    if (_eventBus != null)
                    {
                        _eventBus.Publish(new SurvivorsLostEvent(state.SurvivorCount, state.LaneIndex, state.CageId));
                    }

                    OnLost?.Invoke(this);
                    OnStateChanged?.Invoke(this);
                    return true;
                }
            }

            return false;
        }
    }
}
