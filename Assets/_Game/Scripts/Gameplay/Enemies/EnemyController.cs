using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    public class EnemyController : MonoBehaviour, IDamageable
    {
        public int LaneIndex { get; set; }
        public float MoveSpeed { get; set; } = 2f;
        public HealthComponent Health { get; private set; }
        public bool IsActiveInPool { get; private set; }
        public Action<EnemyController> OnDeath { get; private set; }

        public int CurrentHealth => Health != null ? Health.CurrentHealth : 0;
        public int MaxHealth => Health != null ? Health.MaxHealth : 0;
        public bool IsAlive => Health != null && Health.IsAlive;

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
