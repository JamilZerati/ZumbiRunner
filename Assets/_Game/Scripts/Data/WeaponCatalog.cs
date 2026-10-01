using System;
using System.Collections.Generic;
using Game.Core.Stats;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "Horde Runner/Data/Weapon Catalog")]
    public class WeaponCatalog : ScriptableObject, IWeaponCatalog
    {
        [SerializeField] private List<WeaponDefinition> weapons = new();

        public IReadOnlyList<WeaponDefinition> Weapons => weapons;

        public bool TryGet(string weaponId, out WeaponProfile profile)
        {
            if (!string.IsNullOrEmpty(weaponId))
            {
                for (int i = 0; i < weapons.Count; i++)
                {
                    var weapon = weapons[i];
                    if (weapon != null && string.Equals(weapon.Id, weaponId, StringComparison.Ordinal))
                    {
                        profile = weapon.ToProfile();
                        return true;
                    }
                }
            }

            profile = default;
            return false;
        }

        public void SetWeapons(IEnumerable<WeaponDefinition> weapons)
        {
            this.weapons = new List<WeaponDefinition>();
            if (weapons == null)
            {
                return;
            }

            foreach (var weapon in weapons)
            {
                if (weapon != null)
                {
                    this.weapons.Add(weapon);
                }
            }
        }
    }
}
