using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "AbilityCatalog", menuName = "Horde Runner/Data/Ability Catalog")]
    public class AbilityCatalog : ScriptableObject
    {
        [SerializeField] private List<GeneralAbilityDefinition> abilities = new();

        public IReadOnlyList<GeneralAbilityDefinition> Abilities => abilities;

        public bool TryGet(string id, out GeneralAbilityDefinition definition)
        {
            if (!string.IsNullOrEmpty(id) && abilities != null)
            {
                for (int i = 0; i < abilities.Count; i++)
                {
                    var ability = abilities[i];
                    if (ability != null && string.Equals(ability.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        definition = ability;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public void SetAbilities(IEnumerable<GeneralAbilityDefinition> newAbilities)
        {
            abilities = new List<GeneralAbilityDefinition>();
            if (newAbilities == null)
            {
                return;
            }

            foreach (var ability in newAbilities)
            {
                if (ability != null)
                {
                    abilities.Add(ability);
                }
            }
        }
    }
}
