using System;

namespace Game.Core.Allies
{
    public class AllyInstance : IAllyInstance
    {
        public string Id { get; }
        public string DisplayName { get; }
        public AllyArchetype Archetype { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public bool IsPet => Archetype == AllyArchetype.Pet;
        public bool HasActiveAbility => Ability != null;
        public AbilityRunner Ability { get; }

        public AllyInstance(string id, string displayName, AllyArchetype archetype, int maxHealth, AbilityRunner ability = null)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Archetype = archetype;
            MaxHealth = Math.Max(1, maxHealth);
            CurrentHealth = MaxHealth;
            Ability = ability;
        }

        public bool TakeDamage(int damage)
        {
            if (IsPet || damage <= 0 || !IsAlive)
            {
                return false;
            }

            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            return true;
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        public void RestoreFullHealth()
        {
            CurrentHealth = MaxHealth;
        }
    }
}
