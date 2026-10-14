using Game.Core.Allies;
using UnityEngine;

namespace Game.Data.Allies
{
    [CreateAssetMenu(fileName = "AllyDefinition", menuName = "Horde Runner/Data/Ally Definition")]
    public class AllyDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private AllyArchetype archetype;
        [SerializeField] private int maxHealth;
        [SerializeField] private float attackInterval;
        [SerializeField] private int attackDamage;
        [SerializeField] private string statusApplied;
        [SerializeField] private string abilityName;
        [SerializeField] private float abilityCooldown;
        [SerializeField] private int abilityDamage;
        [SerializeField] private float petStatModifierValue;

        public string Id => id;
        public string DisplayName => displayName;
        public AllyArchetype Archetype => archetype;
        public int MaxHealth => maxHealth;
        public float AttackInterval => attackInterval;
        public int AttackDamage => attackDamage;
        public string StatusApplied => statusApplied;
        public string AbilityName => abilityName;
        public float AbilityCooldown => abilityCooldown;
        public int AbilityDamage => abilityDamage;
        public float PetStatModifierValue => petStatModifierValue;

        public void SetData(string id, string displayName, AllyArchetype archetype, int maxHealth,
                            float attackInterval, int attackDamage, string statusApplied,
                            string abilityName, float abilityCooldown, int abilityDamage,
                            float petStatModifierValue)
        {
            this.id = id;
            this.displayName = displayName;
            this.archetype = archetype;
            this.maxHealth = maxHealth;
            this.attackInterval = attackInterval;
            this.attackDamage = attackDamage;
            this.statusApplied = statusApplied;
            this.abilityName = abilityName;
            this.abilityCooldown = abilityCooldown;
            this.abilityDamage = abilityDamage;
            this.petStatModifierValue = petStatModifierValue;
        }
    }
}
