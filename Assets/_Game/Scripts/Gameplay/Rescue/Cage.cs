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
                if (definition != value)
                {
                    if (_runtimeState != null && !_runtimeState.IsIntact)
                    {
                        Debug.LogWarning($"[Cage] Changing Definition on '{name}' while cage is not intact will not reset completed state.", this);
                        return;
                    }
                    definition = value;
                    _runtimeState = null;
                }
            }
        }

        public int LaneIndex
        {
            get => laneIndex;
            set
            {
                if (laneIndex != value)
                {
                    if (_runtimeState != null && !_runtimeState.IsIntact)
                    {
                        Debug.LogWarning($"[Cage] Changing LaneIndex on '{name}' while cage is not intact will not reset completed state.", this);
                        return;
                    }
                    laneIndex = value;
                    _runtimeState = null;
                }
            }
        }

        public ICageState RuntimeState => EnsureRuntimeState();
        public ICageState State => EnsureRuntimeState();
        public string CageId => RuntimeState.CageId;
        public int SurvivorCount => RuntimeState.SurvivorCount;
        public string LostEnemyArchetypeId => RuntimeState.LostEnemyArchetypeId;
        public int CurrentHealth => RuntimeState.CurrentHp;
        public int MaxHealth => RuntimeState.MaxHp;
        public bool IsOpen => RuntimeState.IsOpen;
        public bool IsLost => RuntimeState.IsLost;

        // Invariante: Projectile.HandleTrigger consome tiros apenas enquanto IsAlive for true. Apos aberta ou perdida, projeteis devem atravessar livremente.
        public bool IsAlive => (_runtimeState ?? EnsureRuntimeState()).IsIntact;

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
                if (definition == null && customHp <= 0)
                {
                    Debug.LogWarning($"[Cage] '{name}' has no CageDefinition assigned. Using fallback stats.", this);
                }

                string id = definition != null && !string.IsNullOrEmpty(definition.Id) ? definition.Id : "cage_default";
                int hp = customHp > 0 ? customHp : (definition != null ? definition.MaxHp : 60);
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
                cageCollider.isTrigger = true;
                cageCollider.enabled = _runtimeState == null || IsAlive;
            }
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0)
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
                else
                {
                    Debug.LogWarning($"[Cage] '{name}' opened but _squad reference is null. Rescued survivors ({state.SurvivorCount}) were not added to squad.", this);
                }

                if (_eventBus != null)
                {
                    _eventBus.Publish(new SurvivorsRescuedEvent(state.SurvivorCount, state.LaneIndex, state.CageId));
                }
                else
                {
                    Debug.LogWarning($"[Cage] '{name}' opened but _eventBus is null. SurvivorsRescuedEvent could not be published.", this);
                }

                OnOpened?.Invoke(this);
            }

            OnStateChanged?.Invoke(this);
        }

        // Invariante de resgate: a jaula e perdida no instante em que o General cruza seu Z (sem margem de 5m dos zumbis), pois a tropa nao atira para tras.
        public bool CheckEscape(float generalZ)
        {
            if (!IsAlive || generalZ < transform.position.z)
            {
                return false;
            }

            var state = _runtimeState ?? EnsureRuntimeState();
            if (!state.MarkLost())
            {
                return false;
            }

            if (cageCollider != null)
            {
                cageCollider.enabled = false;
            }

            if (_eventBus != null)
            {
                _eventBus.Publish(new SurvivorsLostEvent(state.SurvivorCount, state.LaneIndex, state.CageId, state.LostEnemyArchetypeId));
            }
            else
            {
                Debug.LogWarning($"[Cage] '{name}' escaped but _eventBus is null. SurvivorsLostEvent could not be published.", this);
            }

            OnLost?.Invoke(this);
            OnStateChanged?.Invoke(this);
            return true;
        }
    }
}
