using System;
using System.Collections.Generic;
using Game.Core.Stats;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "Horde Runner/Data/Weapon Catalog")]
    public class WeaponCatalog : ScriptableObject, IWeaponCatalog
    {
        public IReadOnlyList<WeaponDefinition> Weapons => throw new NotImplementedException();

        public bool TryGet(string weaponId, out WeaponProfile profile) => throw new NotImplementedException();

        public void SetWeapons(IEnumerable<WeaponDefinition> weapons) => throw new NotImplementedException();
    }
}
