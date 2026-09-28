using System;

namespace Game.Core.Stats
{
    public readonly struct WeaponStats
    {
        public const int MaxProjectilesPerShot = 9;

        public float FireRate { get; }
        public int Damage { get; }
        public float ProjectileSpeed { get; }
        public float Range { get; }
        public int ProjectileCount { get; }
        public float SpreadWidth { get; }

        public static WeaponStats Resolve(StatCollection stats, float spreadWidth) => throw new NotImplementedException();
    }
}
