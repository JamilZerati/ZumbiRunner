using System;
using Game.Core;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay
{
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 20;

        private IEventBus eventBus;
        private int _maxHealth;
        private int _currentHealth;
        private bool _isInitialized;

        public int CurrentHealth
        {
            get
            {
                EnsureInitialized();
                return _currentHealth;
            }
            private set => _currentHealth = value;
        }

        public int MaxHealth
        {
            get
            {
                EnsureInitialized();
                return _maxHealth;
            }
            private set => _maxHealth = value;
        }

        public bool IsAlive => CurrentHealth > 0;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized && maxHealth > 0)
            {
                _isInitialized = true;
                _maxHealth = maxHealth;
                _currentHealth = maxHealth;
            }
        }

        public void Initialize(int maxHealth, IEventBus eventBus = null)
        {
            this.maxHealth = Math.Max(0, maxHealth);
            _isInitialized = true;
            _maxHealth = this.maxHealth;
            _currentHealth = _maxHealth;
            this.eventBus = eventBus;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0)
            {
                return;
            }

            CurrentHealth = Math.Max(0, CurrentHealth - damage.Amount);
            eventBus?.Publish(new DamageTakenEvent(this, damage, CurrentHealth));

            if (CurrentHealth == 0)
            {
                eventBus?.Publish(new EntityDiedEvent(this, damage.Source));
            }
        }

        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
        }
    }
}
