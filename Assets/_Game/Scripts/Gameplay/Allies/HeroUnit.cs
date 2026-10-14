using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Allies;
using Game.Core.Events;
using Game.Core.Status;
using UnityEngine;

namespace Game.Gameplay.Allies
{
    public class HeroUnit : MonoBehaviour, IAllyInstance
    {
        [SerializeField] private string id, displayName, statusApplied = "burn";
        [SerializeField] private int maxHealth = 100, attackDamage = 25, abilityDamage = 40;
        [SerializeField] private float attackInterval = 1.2f;

        private IEventBus _eventBus;
        private int _currentHealth;
        private bool _isInitialized;
        private AbilityRunner _ability;

        public string Id => id ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public AllyArchetype Archetype => AllyArchetype.Hero;
        public int MaxHealth => Math.Max(1, maxHealth);
        public int CurrentHealth => _currentHealth;
        public bool IsAlive => _currentHealth > 0;
        public bool IsPet => false;
        public bool HasActiveAbility => Ability != null;
        public AbilityRunner Ability => _ability;
        public int AttackDamage => attackDamage;
        public float AttackInterval => attackInterval;
        public string StatusApplied => statusApplied ?? string.Empty;
        public int AbilityDamage => abilityDamage;

        private void Awake()
        {
            if (!_isInitialized) Initialize(id, displayName, maxHealth, attackDamage, attackInterval, statusApplied, _ability, abilityDamage, _eventBus);
        }

        private void Update()
        {
            if (IsAlive && _ability != null)
            {
                _ability.Tick(Time.deltaTime);
            }
        }

        public void SetAbility(AbilityRunner ability) => _ability = ability;

        public void TickAbility(float deltaTime)
        {
            if (IsAlive && _ability != null)
            {
                _ability.Tick(deltaTime);
            }
        }

        public void Initialize(string id, string displayName, int maxHealth, int attackDamage = 25,
                               float attackInterval = 1.2f, string statusApplied = "burn",
                               AbilityRunner ability = null, int abilityDamage = 40, IEventBus bus = null)
        {
            this.id = id ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.maxHealth = Math.Max(1, maxHealth);
            this.attackDamage = Math.Max(0, attackDamage);
            this.attackInterval = Math.Max(0.01f, attackInterval);
            this.statusApplied = statusApplied ?? string.Empty;
            this.abilityDamage = Math.Max(0, abilityDamage);
            _ability = ability;
            _eventBus = bus;
            _currentHealth = this.maxHealth;
            _isInitialized = true;
        }

        public bool TakeDamage(int damage)
        {
            if (damage <= 0 || !IsAlive) return false;
            _currentHealth = Math.Max(0, _currentHealth - damage);
            _eventBus?.Publish(new AllyDamagedEvent(Id, damage, _currentHealth));
            if (_currentHealth == 0) _eventBus?.Publish(new AllyKnockedOutEvent(Id));
            return true;
        }

        public void Heal(int amount)
        {
            if (amount > 0 && IsAlive) _currentHealth = Math.Min(MaxHealth, _currentHealth + amount);
        }

        public void RestoreFullHealth() => _currentHealth = MaxHealth;

        public bool PerformAttack(IStatusReceiver target)
        {
            if (target == null || !IsAlive || !target.IsAlive) return false;
            var damageType = GetDamageTypeForStatus(StatusApplied);
            target.ReceiveHit(new DamageInfo(AttackDamage, damageType, this), CreateStatusApplications(StatusApplied, 1));
            return true;
        }

        public bool FireAbility(IStatusReceiver target)
        {
            if (target == null || !IsAlive) return false;
            var damageType = GetDamageTypeForStatus(StatusApplied);
            target.ReceiveHit(new DamageInfo(AbilityDamage, damageType, this), CreateStatusApplications(StatusApplied, 2));
            _eventBus?.Publish(new AllyAbilityFiredEvent(Id, Ability?.AbilityName ?? Id));
            return true;
        }

        private static DamageType GetDamageTypeForStatus(string status)
        {
            if (string.Equals(status, "burn", StringComparison.OrdinalIgnoreCase)) return DamageType.Fire;
            if (string.Equals(status, "freeze", StringComparison.OrdinalIgnoreCase)) return DamageType.Ice;
            return DamageType.Physical;
        }

        private List<StatusApplication> CreateStatusApplications(string status, int stacks)
        {
            var list = new List<StatusApplication>(1);
            if (string.Equals(status, "burn", StringComparison.OrdinalIgnoreCase))
                list.Add(new StatusApplication(StatusKind.Burn, stacks, Id));
            else if (string.Equals(status, "freeze", StringComparison.OrdinalIgnoreCase))
                list.Add(new StatusApplication(StatusKind.Freeze, stacks, Id));
            return list;
        }
    }
}
