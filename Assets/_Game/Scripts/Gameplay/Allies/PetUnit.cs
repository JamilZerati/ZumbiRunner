using System;
using Game.Core.Allies;
using Game.Core.Stats;
using UnityEngine;

namespace Game.Gameplay.Allies
{
    public class PetUnit : MonoBehaviour, IAllyInstance
    {
        [SerializeField] private string id = "guard_dog", displayName = "Cão de Guarda";
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float firepowerBonus = 0.15f;

        private StatCollection _attachedStats;
        private StatModifier? _currentModifier;

        public string Id => id ?? "guard_dog";
        public string DisplayName => displayName ?? "Cão de Guarda";
        public AllyArchetype Archetype => AllyArchetype.Pet;
        public int MaxHealth => Math.Max(1, maxHealth);
        public int CurrentHealth => MaxHealth;
        public bool IsAlive => true;
        public bool IsPet => true;
        public bool HasActiveAbility => false;
        public AbilityRunner Ability => null;
        public float FirepowerBonus => firepowerBonus;

        public void Initialize(string id = "guard_dog", string displayName = "Cão de Guarda", int maxHealth = 100, float bonus = 0.15f)
        {
            this.id = id ?? "guard_dog";
            this.displayName = displayName ?? "Cão de Guarda";
            this.maxHealth = Math.Max(1, maxHealth);
            this.firepowerBonus = bonus;
        }

        public bool TakeDamage(int damage) => false;
        public void Heal(int amount) {}
        public void RestoreFullHealth() {}

        public void AttachAura(StatCollection stats)
        {
            if (stats == null) return;
            DetachAura();
            _attachedStats = stats;
            var modifier = new StatModifier(StatId.Damage, ModifierKind.PercentAdd, firepowerBonus, this);
            _currentModifier = modifier;
            _attachedStats.AddModifier(modifier);
        }

        public void DetachAura()
        {
            if (_attachedStats != null && _currentModifier.HasValue)
            {
                _attachedStats.RemoveModifier(_currentModifier.Value);
                _attachedStats = null;
                _currentModifier = null;
            }
        }

        private void OnDisable() => DetachAura();
        private void OnDestroy() => DetachAura();
    }
}
