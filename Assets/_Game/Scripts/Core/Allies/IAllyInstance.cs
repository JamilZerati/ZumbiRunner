using System;

namespace Game.Core.Allies
{
    public interface IAllyInstance
    {
        string Id { get; }
        string DisplayName { get; }
        AllyArchetype Archetype { get; }
        int MaxHealth { get; }
        int CurrentHealth { get; }
        bool IsAlive { get; }
        bool IsPet { get; }
        bool HasActiveAbility { get; }
        AbilityRunner Ability { get; }
        bool TakeDamage(int damage);
        void Heal(int amount);
        void RestoreFullHealth();
    }
}
