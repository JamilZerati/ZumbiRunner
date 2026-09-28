using System;
using Game.Core;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay
{
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        private IEventBus eventBus;

        public int CurrentHealth { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        public void Initialize(int maxHealth, IEventBus eventBus = null)
        {
            MaxHealth = Math.Max(0, maxHealth);
            CurrentHealth = MaxHealth;
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
