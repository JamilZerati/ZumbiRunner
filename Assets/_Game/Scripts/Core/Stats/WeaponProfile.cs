using System;

namespace Game.Core.Stats
{
    public readonly struct WeaponProfile
    {
        public string Id { get; }
        public float FireRate { get; }
        public int Damage { get; }
        public float ProjectileSpeed { get; }
        public float Range { get; }
        public int ProjectilesPerShot { get; }
        public float SpreadWidth { get; }

        public WeaponProfile(string id, float fireRate, int damage, float projectileSpeed,
                             float range, int projectilesPerShot, float spreadWidth)
        {
            throw new NotImplementedException();
        }

        public void ApplyAsBase(StatCollection stats) => throw new NotImplementedException();
    }
}
