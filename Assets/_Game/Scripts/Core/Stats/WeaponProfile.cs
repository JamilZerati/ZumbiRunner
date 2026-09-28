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
            Id = id;
            FireRate = fireRate;
            Damage = damage;
            ProjectileSpeed = projectileSpeed;
            Range = range;
            ProjectilesPerShot = projectilesPerShot;
            SpreadWidth = spreadWidth;
        }

        public void ApplyAsBase(StatCollection stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            stats.SetBase(StatId.FireRate, FireRate);
            stats.SetBase(StatId.Damage, Damage);
            stats.SetBase(StatId.ProjectileSpeed, ProjectileSpeed);
            stats.SetBase(StatId.Range, Range);
            stats.SetBase(StatId.ProjectileCount, ProjectilesPerShot);
        }
    }
}
