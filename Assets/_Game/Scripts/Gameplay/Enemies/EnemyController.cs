using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Status;
using UnityEngine;

namespace Game.Gameplay
{
    public class EnemyController : MonoBehaviour, IDamageable, IStatusReceiver
    {
        [SerializeField] private StatusEffectDirector statusDirector;
        private StatusEffectController _status;

        public StatusEffectController Status
        {
            get
            {
                if (_status == null)
                {
                    IStatusCatalog catalog = statusDirector != null ? statusDirector.Catalog : DefaultFallbackCatalog;
                    IEffectInteractionTable table = statusDirector != null ? statusDirector.Interactions : null;
                    IStatusNeighborhood neighborhood = statusDirector;
                    IEventBus bus = statusDirector != null ? statusDirector.EventBus : null;
                    _status = new StatusEffectController(Health, this, catalog, table, neighborhood, bus);
                }
                return _status;
            }
        }

        public void AttachStatusDirector(StatusEffectDirector director)
        {
            statusDirector = director;
            _status = null;
        }

        public int ReceiveHit(DamageInfo hit, IReadOnlyList<StatusApplication> onHit)
        {
            if (!IsActiveInPool || !IsAlive)
            {
                return 0;
            }

            int dealt = Status.ResolveHit(hit, onHit);
            if (!IsAlive)
            {
                Die();
            }
            return dealt;
        }

        private static IStatusCatalog s_fallbackCatalog;
        private static IStatusCatalog DefaultFallbackCatalog
        {
            get
            {
                if (s_fallbackCatalog == null)
                {
                    var list = new List<IStatusEffect>
                    {
                        new BurnStatus { Duration = 3f, TickInterval = 0.5f, DamagePerTick = 3 },
                        new FreezeStatus { Duration = 3f, Threshold = 2 },
                        new FrozenStatus { Duration = 1.5f },
                        new SlowStatus { Duration = 2f, SlowPercent = 0.4f },
                        new ShockStatus { ChainCount = 2, ChainRadius = 4f, ChainDamage = 6 },
                        new PoisonStatus { Duration = 4f, TickInterval = 1f, DamagePerTickPerStack = 1, MaxStacks = 5, ExplosionDamagePerStack = 4, ExplosionRadius = 2.5f }
                    };
                    var cat = ScriptableObject.CreateInstance<Game.Data.StatusCatalog>();
                    cat.SetEffects(list);
                    s_fallbackCatalog = cat;
                }
                return s_fallbackCatalog;
            }
        }

        public int LaneIndex { get; set; }
        public float MoveSpeed { get; set; } = 2f;
        private HealthComponent _health;
        public HealthComponent Health
        {
            get
            {
                if (_health == null)
                {
                    _health = GetComponent<HealthComponent>();
                }
                return _health;
            }
            private set => _health = value;
        }
        private bool _isActiveInPool;
        private bool _hasBeenDeactivated;
        public bool IsActiveInPool
        {
            get
            {
                if (!_hasBeenDeactivated && (gameObject.activeInHierarchy || gameObject.activeSelf))
                {
                    return true;
                }
                return _isActiveInPool;
            }
            private set
            {
                _isActiveInPool = value;
                _hasBeenDeactivated = !value;
            }
        }
        public Action<EnemyController> OnDeath { get; private set; }

        public int CurrentHealth => Health != null ? Health.CurrentHealth : 0;
        public int MaxHealth => Health != null ? Health.MaxHealth : 0;
        public bool IsAlive => Health != null && Health.IsAlive;

        private void Awake()
        {
            if (Health == null)
            {
                Health = GetComponent<HealthComponent>();
            }

            if (gameObject.activeInHierarchy || gameObject.activeSelf)
            {
                IsActiveInPool = true;
                if (TryGetComponent<Collider>(out var col))
                {
                    col.enabled = true;
                }
            }
        }

        public void Initialize(int laneIndex, int maxHealth, float moveSpeed, Action<EnemyController> onDeath)
        {
            Health = GetComponent<HealthComponent>();
            if (Health == null)
            {
                Health = gameObject.AddComponent<HealthComponent>();
            }
            Health.Initialize(maxHealth);

            LaneIndex = laneIndex;
            MoveSpeed = moveSpeed;
            OnDeath = onDeath;
            IsActiveInPool = true;

            if (TryGetComponent<Collider>(out var col))
            {
                col.enabled = true;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!IsActiveInPool || !gameObject.activeSelf || !IsAlive)
            {
                return;
            }

            transform.position += Vector3.back * (MoveSpeed * deltaTime);
        }

        public void TakeDamage(DamageInfo damage)
        {
            ReceiveHit(damage, null);
        }

        public void Die()
        {
            Recycle();
        }

        public void Recycle()
        {
            if (!IsActiveInPool)
            {
                return;
            }

            IsActiveInPool = false;

            if (TryGetComponent<Collider>(out var col))
            {
                col.enabled = false;
            }

            gameObject.SetActive(false);
            OnDeath?.Invoke(this);
        }
    }
}
