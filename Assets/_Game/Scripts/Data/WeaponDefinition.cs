using System;
using Game.Core.Stats;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Horde Runner/Data/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public string Id => throw new NotImplementedException();
        public string DisplayName => throw new NotImplementedException();

        public WeaponProfile ToProfile() => throw new NotImplementedException();

        public void SetData(string id, string displayName, float fireRate, int damage, float projectileSpeed,
                            float range, int projectilesPerShot, float spreadWidth)
        {
            throw new NotImplementedException();
        }
    }
}
