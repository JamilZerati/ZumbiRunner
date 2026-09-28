using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    public class EnemyController : MonoBehaviour, IDamageable
    {
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
            if (!IsActiveInPool || !IsAlive)
            {
                return;
            }

            Health.TakeDamage(damage);

            if (!Health.IsAlive)
            {
                Die();
            }
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
