using Game.Core.Stats;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class WeaponStatsTests
    {
        private const float Tolerance = 0.0001f;

        private static StatCollection PistolBases()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.FireRate, 2f);
            stats.SetBase(StatId.Damage, 2f);
            stats.SetBase(StatId.ProjectileSpeed, 15f);
            stats.SetBase(StatId.Range, 40f);
            stats.SetBase(StatId.ProjectileCount, 1f);
            return stats;
        }

        [Test]
        public void Resolve_PistolBasesWithoutModifiers_ReturnsPistolNumbers()
        {
            var resolved = WeaponStats.Resolve(PistolBases(), 0f);

            Assert.AreEqual(2f, resolved.FireRate, Tolerance);
            Assert.AreEqual(2, resolved.Damage);
            Assert.AreEqual(15f, resolved.ProjectileSpeed, Tolerance);
            Assert.AreEqual(40f, resolved.Range, Tolerance);
            Assert.AreEqual(1, resolved.ProjectileCount);
            Assert.AreEqual(0f, resolved.SpreadWidth, Tolerance);
        }

        [Test]
        public void Resolve_DamageTwelveAndAHalf_RoundsAwayFromZeroTo13()
        {
            var stats = PistolBases();
            stats.SetBase(StatId.Damage, 10f);
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.25f, "damage_up_25"));

            Assert.AreEqual(13, WeaponStats.Resolve(stats, 0f).Damage);
        }

        [TestCase(10.5f, 11)]
        [TestCase(14.5f, 15)]
        [TestCase(14.49f, 14)]
        public void Resolve_DamageMidpoint_IsNotBankersRounding(float baseDamage, int expected)
        {
            var stats = PistolBases();
            stats.SetBase(StatId.Damage, baseDamage);

            Assert.AreEqual(expected, WeaponStats.Resolve(stats, 0f).Damage);
        }

        [TestCase(0f)]
        [TestCase(0.4f)]
        [TestCase(-5f)]
        public void Resolve_DamageBelowOne_ClampsToOne(float baseDamage)
        {
            var stats = PistolBases();
            stats.SetBase(StatId.Damage, baseDamage);

            Assert.AreEqual(1, WeaponStats.Resolve(stats, 0f).Damage);
        }

        [TestCase(0.4f, 1)]
        [TestCase(0f, 1)]
        [TestCase(-3f, 1)]
        [TestCase(2.5f, 3)]
        [TestCase(8.5f, 9)]
        [TestCase(9f, 9)]
        [TestCase(9.4f, 9)]
        [TestCase(10f, 9)]
        [TestCase(50f, 9)]
        public void Resolve_ProjectileCount_RoundsAwayFromZeroAndClampsToOneThroughNine(float baseCount, int expected)
        {
            var stats = PistolBases();
            stats.SetBase(StatId.ProjectileCount, baseCount);

            Assert.AreEqual(expected, WeaponStats.Resolve(stats, 0f).ProjectileCount);
        }

        [Test]
        public void MaxProjectilesPerShot_IsNine()
        {
            Assert.AreEqual(9, WeaponStats.MaxProjectilesPerShot);
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(-10f)]
        public void Resolve_ProjectileSpeedBelowOne_ClampsToOne(float baseSpeed)
        {
            var stats = PistolBases();
            stats.SetBase(StatId.ProjectileSpeed, baseSpeed);

            Assert.AreEqual(1f, WeaponStats.Resolve(stats, 0f).ProjectileSpeed, Tolerance);
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(-1f)]
        public void Resolve_RangeBelowOne_ClampsToOne(float baseRange)
        {
            var stats = PistolBases();
            stats.SetBase(StatId.Range, baseRange);

            Assert.AreEqual(1f, WeaponStats.Resolve(stats, 0f).Range, Tolerance);
        }

        [Test]
        public void Resolve_NegativeFireRate_ClampsToZero()
        {
            var stats = PistolBases();
            stats.AddModifier(new StatModifier(StatId.FireRate, ModifierKind.Flat, -5f, "curse"));

            Assert.AreEqual(0f, WeaponStats.Resolve(stats, 0f).FireRate, Tolerance);
        }

        [Test]
        public void Resolve_FireRateFlatBonus_IsAdded()
        {
            var stats = PistolBases();
            stats.AddModifier(new StatModifier(StatId.FireRate, ModifierKind.Flat, 1f, "fire_rate_up_1"));

            Assert.AreEqual(3f, WeaponStats.Resolve(stats, 0f).FireRate, Tolerance);
        }

        [Test]
        public void Resolve_SpreadWidth_ComesFromArgument()
        {
            Assert.AreEqual(1.2f, WeaponStats.Resolve(PistolBases(), 1.2f).SpreadWidth, Tolerance);
        }

        [Test]
        public void Resolve_NegativeSpreadWidth_ClampsToZero()
        {
            Assert.AreEqual(0f, WeaponStats.Resolve(PistolBases(), -1f).SpreadWidth, Tolerance);
        }

        [Test]
        public void WeaponProfile_Constructor_ExposesEveryField()
        {
            var profile = new WeaponProfile("shotgun", 1.2f, 8, 14f, 22f, 3, 1.2f);

            Assert.AreEqual("shotgun", profile.Id);
            Assert.AreEqual(1.2f, profile.FireRate, Tolerance);
            Assert.AreEqual(8, profile.Damage);
            Assert.AreEqual(14f, profile.ProjectileSpeed, Tolerance);
            Assert.AreEqual(22f, profile.Range, Tolerance);
            Assert.AreEqual(3, profile.ProjectilesPerShot);
            Assert.AreEqual(1.2f, profile.SpreadWidth, Tolerance);
        }

        [Test]
        public void WeaponProfile_ApplyAsBase_WritesAllFiveBases()
        {
            var stats = new StatCollection();
            var shotgun = new WeaponProfile("shotgun", 1.2f, 8, 14f, 22f, 3, 1.2f);

            shotgun.ApplyAsBase(stats);

            Assert.AreEqual(1.2f, stats.GetBase(StatId.FireRate), Tolerance);
            Assert.AreEqual(8f, stats.GetBase(StatId.Damage), Tolerance);
            Assert.AreEqual(14f, stats.GetBase(StatId.ProjectileSpeed), Tolerance);
            Assert.AreEqual(22f, stats.GetBase(StatId.Range), Tolerance);
            Assert.AreEqual(3f, stats.GetBase(StatId.ProjectileCount), Tolerance);
        }

        [Test]
        public void WeaponProfile_ApplyAsBase_KeepsModifiersAndReResolves()
        {
            var stats = PistolBases();
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.25f, "damage_up_25"));

            new WeaponProfile("shotgun", 1.2f, 8, 14f, 22f, 3, 1.2f).ApplyAsBase(stats);

            Assert.AreEqual(1, stats.GetModifiers(StatId.Damage).Count);
            Assert.AreEqual(10, WeaponStats.Resolve(stats, 1.2f).Damage);
        }
    }
}
