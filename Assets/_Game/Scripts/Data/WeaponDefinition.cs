using Game.Core.Stats;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Horde Runner/Data/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private float fireRate;
        [SerializeField] private int damage;
        [SerializeField] private float projectileSpeed;
        [SerializeField] private float range;
        [SerializeField] private int projectilesPerShot;
        [SerializeField] private float spreadWidth;

        public string Id => id;
        public string DisplayName => displayName;

        public WeaponProfile ToProfile()
        {
            return new WeaponProfile(id, fireRate, damage, projectileSpeed, range, projectilesPerShot, spreadWidth);
        }

        public void SetData(string id, string displayName, float fireRate, int damage, float projectileSpeed,
                            float range, int projectilesPerShot, float spreadWidth)
        {
            this.id = id;
            this.displayName = displayName;
            this.fireRate = fireRate;
            this.damage = damage;
            this.projectileSpeed = projectileSpeed;
            this.range = range;
            this.projectilesPerShot = projectilesPerShot;
            this.spreadWidth = spreadWidth;
        }
    }
}
