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

        private WeaponStats(float fireRate, int damage, float projectileSpeed, float range,
                            int projectileCount, float spreadWidth)
        {
            FireRate = fireRate;
            Damage = damage;
            ProjectileSpeed = projectileSpeed;
            Range = range;
            ProjectileCount = projectileCount;
            SpreadWidth = spreadWidth;
        }

        public static WeaponStats Resolve(StatCollection stats, float spreadWidth)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            // Velocidade e alcance abaixo de 1 deixariam o projétil sem atingir MaxDistance e fora do pool para sempre.
            return new WeaponStats(
                fireRate: Math.Max(0f, stats.GetValue(StatId.FireRate)),
                damage: Math.Max(1, RoundToInt(stats.GetValue(StatId.Damage))),
                projectileSpeed: Math.Max(1f, stats.GetValue(StatId.ProjectileSpeed)),
                range: Math.Max(1f, stats.GetValue(StatId.Range)),
                projectileCount: Clamp(RoundToInt(stats.GetValue(StatId.ProjectileCount)), 1, MaxProjectilesPerShot),
                spreadWidth: Math.Max(0f, spreadWidth));
        }

        // Math.Round usa banker's rounding por padrão (12,5 -> 12); o design pede 12,5 -> 13.
        private static int RoundToInt(float value)
        {
            double rounded = Math.Round((double)value, MidpointRounding.AwayFromZero);
            if (rounded >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return rounded <= int.MinValue ? int.MinValue : (int)rounded;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
