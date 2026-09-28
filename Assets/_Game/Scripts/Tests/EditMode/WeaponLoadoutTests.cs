using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Core.Events;
using Game.Core.Stats;
using Game.Gameplay;
using Game.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class WeaponLoadoutTests
    {
        private const float Tolerance = 0.0001f;

        private GameObject weaponObject;
        private WeaponController weapon;
        private List<GameObject> spawnedObjects;
        private ObjectPool<Projectile> pool;

        [SetUp]
        public void SetUp()
        {
            spawnedObjects = new List<GameObject>();
            weaponObject = new GameObject("TestWeaponLoadout");
            spawnedObjects.Add(weaponObject);
            weapon = weaponObject.AddComponent<WeaponController>();

            pool = new ObjectPool<Projectile>(() =>
            {
                var go = new GameObject("PooledProjectile");
                spawnedObjects.Add(go);
                return go.AddComponent<Projectile>();
            });
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null)
                {
                    Object.DestroyImmediate(spawnedObjects[i]);
                }
            }
            spawnedObjects.Clear();
        }

        private void InitializeWithCatalog(IEventBus bus = null)
        {
            weapon.Initialize(pool, null, WeaponTestCatalog.CreateDefault(), bus);
        }

        [Test]
        public void EquippedWeaponId_BeforeFirstEquip_IsEmpty()
        {
            InitializeWithCatalog();

            Assert.AreEqual(string.Empty, weapon.EquippedWeaponId);
        }

        [Test]
        public void DefaultBases_WithoutEquip_MatchPistolSoM4SceneKeepsBehaviour()
        {
            weapon.Initialize(pool);

            Assert.AreEqual(2f, weapon.FireRate, Tolerance);
            Assert.AreEqual(10, weapon.DamagePerShot);
            Assert.AreEqual(15f, weapon.ProjectileSpeed, Tolerance);
            Assert.AreEqual(40f, weapon.MaxDistance, Tolerance);
            Assert.AreEqual(1, weapon.CurrentStats.ProjectileCount);
        }

        [Test]
        public void TryEquip_KnownWeapon_WritesProfileAsBases()
        {
            InitializeWithCatalog();

            Assert.IsTrue(weapon.TryEquip("shotgun"));

            Assert.AreEqual("shotgun", weapon.EquippedWeaponId);
            Assert.AreEqual(1.2f, weapon.Stats.GetBase(StatId.FireRate), Tolerance);
            Assert.AreEqual(8f, weapon.Stats.GetBase(StatId.Damage), Tolerance);
            Assert.AreEqual(14f, weapon.Stats.GetBase(StatId.ProjectileSpeed), Tolerance);
            Assert.AreEqual(22f, weapon.Stats.GetBase(StatId.Range), Tolerance);
            Assert.AreEqual(3f, weapon.Stats.GetBase(StatId.ProjectileCount), Tolerance);
            Assert.AreEqual(1.2f, weapon.CurrentStats.SpreadWidth, Tolerance);
        }

        [Test]
        public void TryEquip_SwitchingWeapon_KeepsPerkModifiersAndReResolvesDamage()
        {
            InitializeWithCatalog();
            weapon.TryEquip("pistol");
            weapon.Stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.25f, "damage_up_25"));
            Assert.AreEqual(13, weapon.CurrentStats.Damage);

            Assert.IsTrue(weapon.TryEquip("shotgun"));

            Assert.AreEqual(10, weapon.CurrentStats.Damage);
            var modifiers = weapon.Stats.GetModifiers(StatId.Damage);
            Assert.AreEqual(1, modifiers.Count);
            Assert.AreEqual("damage_up_25", modifiers[0].Source);
        }

        [Test]
        public void TryEquip_UnknownWeapon_ReturnsFalseAndKeepsWeaponAndBases()
        {
            InitializeWithCatalog();
            weapon.TryEquip("pistol");

            Assert.IsFalse(weapon.TryEquip("inexistente"));

            Assert.AreEqual("pistol", weapon.EquippedWeaponId);
            Assert.AreEqual(2f, weapon.Stats.GetBase(StatId.FireRate), Tolerance);
            Assert.AreEqual(10f, weapon.Stats.GetBase(StatId.Damage), Tolerance);
            Assert.AreEqual(1f, weapon.Stats.GetBase(StatId.ProjectileCount), Tolerance);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Shotgun")]
        public void TryEquip_NullEmptyOrWrongCaseId_ReturnsFalseWithoutThrowing(string weaponId)
        {
            InitializeWithCatalog();
            weapon.TryEquip("pistol");

            Assert.IsFalse(weapon.TryEquip(weaponId));
            Assert.AreEqual("pistol", weapon.EquippedWeaponId);
        }

        [Test]
        public void TryEquip_WithoutCatalog_ReturnsFalse()
        {
            weapon.Initialize(pool);

            Assert.IsFalse(weapon.TryEquip("shotgun"));
            Assert.AreEqual(string.Empty, weapon.EquippedWeaponId);
        }

        [Test]
        public void TryEquip_PublishesWeaponEquippedEventWithPreviousWeapon()
        {
            var bus = new EventBus();
            var received = new List<WeaponEquippedEvent>();
            bus.Subscribe<WeaponEquippedEvent>(received.Add);
            InitializeWithCatalog(bus);

            weapon.TryEquip("pistol");
            weapon.TryEquip("shotgun");

            Assert.AreEqual(2, received.Count);
            Assert.AreEqual("pistol", received[0].WeaponId);
            Assert.AreEqual(string.Empty, received[0].PreviousWeaponId);
            Assert.AreEqual("shotgun", received[1].WeaponId);
            Assert.AreEqual("pistol", received[1].PreviousWeaponId);
        }

        [Test]
        public void TryEquip_Failed_DoesNotPublishEvent()
        {
            var bus = new EventBus();
            int received = 0;
            bus.Subscribe<WeaponEquippedEvent>(_ => received++);
            InitializeWithCatalog(bus);

            weapon.TryEquip("inexistente");

            Assert.AreEqual(0, received);
        }

        [Test]
        public void TryEquip_AfterCombatStopped_DoesNotResumeFiring()
        {
            InitializeWithCatalog();
            weapon.IsFiring = false;

            weapon.TryEquip("smg");

            Assert.IsFalse(weapon.IsFiring);
        }

        [Test]
        public void TryEquip_FasterWeapon_ClampsAccumulatedTimerToTwoNewIntervals()
        {
            InitializeWithCatalog();
            weapon.TryEquip("pistol");
            weapon.Tick(0.4f);
            Assert.AreEqual(0.4f, weapon.FireTimer, Tolerance);

            weapon.TryEquip("smg");

            Assert.AreEqual(2f / 6f, weapon.FireTimer, Tolerance);
        }

        [Test]
        public void LegacySetter_WritesBase_AndGetterReturnsResolvedValue()
        {
            InitializeWithCatalog();
            weapon.TryEquip("pistol");
            weapon.Stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.25f, "damage_up_25"));

            weapon.DamagePerShot = 20;

            Assert.AreEqual(20f, weapon.Stats.GetBase(StatId.Damage), Tolerance);
            Assert.AreEqual(25, weapon.DamagePerShot);
        }

        [Test]
        public void Tick_ResolvedFireRateClampedToZero_NeverFires()
        {
            InitializeWithCatalog();
            weapon.TryEquip("pistol");
            weapon.Stats.AddModifier(new StatModifier(StatId.FireRate, ModifierKind.Flat, -5f, "curse"));

            weapon.Tick(1f);
            weapon.Tick(1f);

            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void Tick_WithFireRateBonus_UsesResolvedInterval()
        {
            InitializeWithCatalog();
            weapon.TryEquip("pistol");
            weapon.Stats.AddModifier(new StatModifier(StatId.FireRate, ModifierKind.Flat, 2f, "fire_rate_up_2"));

            weapon.Tick(0.25f);

            Assert.AreEqual(1, pool.CountActive);
        }

        [Test]
        public void FireVolley_Shotgun_RentsThreeProjectilesSpreadSymmetricallyAroundShooter()
        {
            weaponObject.transform.position = new Vector3(3f, 0f, 10f);
            InitializeWithCatalog();
            weapon.TryEquip("shotgun");

            var volley = weapon.FireVolley();

            Assert.AreEqual(3, volley.Count);
            Assert.AreEqual(3, pool.CountActive);
            var offsets = volley.Select(p => p.transform.position.x - 3f).OrderBy(x => x).ToArray();
            Assert.AreEqual(-0.6f, offsets[0], Tolerance);
            Assert.AreEqual(0f, offsets[1], Tolerance);
            Assert.AreEqual(0.6f, offsets[2], Tolerance);
            foreach (var projectile in volley)
            {
                Assert.AreEqual(10.5f, projectile.transform.position.z, Tolerance);
            }
        }

        [Test]
        public void FireVolley_Pistol_RentsOneProjectileAtShooterXWithoutNaN()
        {
            weaponObject.transform.position = new Vector3(3f, 0f, 10f);
            InitializeWithCatalog();
            weapon.TryEquip("pistol");

            var volley = weapon.FireVolley();

            Assert.AreEqual(1, volley.Count);
            Vector3 position = volley[0].transform.position;
            Assert.IsFalse(float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z));
            Assert.AreEqual(3f, position.x, Tolerance);
            Assert.AreEqual(10.5f, position.z, Tolerance);
        }

        [Test]
        public void FireVolley_ShotgunWithOnlyOneProjectileAfterModifier_HasNoSpreadAndNoNaN()
        {
            InitializeWithCatalog();
            weapon.TryEquip("shotgun");
            weapon.Stats.AddModifier(new StatModifier(StatId.ProjectileCount, ModifierKind.Flat, -2f, "narrow"));

            var volley = weapon.FireVolley();

            Assert.AreEqual(1, volley.Count);
            Assert.IsFalse(float.IsNaN(volley[0].transform.position.x));
            Assert.AreEqual(0f, volley[0].transform.position.x, Tolerance);
        }

        [Test]
        public void FireVolley_ProjectileCountAboveCap_FiresNineWithinSpreadWidth()
        {
            InitializeWithCatalog();
            weapon.TryEquip("shotgun");
            weapon.Stats.AddModifier(new StatModifier(StatId.ProjectileCount, ModifierKind.Flat, 20f, "bullet_hell"));

            var volley = weapon.FireVolley();

            Assert.AreEqual(9, volley.Count);
            var xs = volley.Select(p => p.transform.position.x).OrderBy(x => x).ToArray();
            Assert.AreEqual(-0.6f, xs[0], Tolerance);
            Assert.AreEqual(0f, xs[4], Tolerance);
            Assert.AreEqual(0.6f, xs[8], Tolerance);
        }

        [Test]
        public void FireVolley_EveryProjectileGetsResolvedDamageSpeedAndRange()
        {
            InitializeWithCatalog();
            weapon.TryEquip("shotgun");
            weapon.Stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.25f, "damage_up_25"));

            var volley = weapon.FireVolley();

            foreach (var projectile in volley)
            {
                Assert.AreEqual(10, projectile.Damage);
                Assert.AreEqual(14f, projectile.Speed, Tolerance);
                Assert.AreEqual(22f, projectile.MaxDistance, Tolerance);
                Assert.IsTrue(projectile.IsActiveInPool);
                Assert.AreEqual(0f, projectile.TraveledDistance, Tolerance);
            }
        }

        [Test]
        public void FireVolley_RecycledProjectile_ReturnsToPoolAndIsReusedFullyReinitialized()
        {
            InitializeWithCatalog();
            weapon.TryEquip("pistol");
            var first = weapon.FireVolley()[0];
            first.Tick(100f);
            Assert.AreEqual(0, pool.CountActive);

            weapon.TryEquip("smg");
            var reused = weapon.FireVolley()[0];

            Assert.AreSame(first, reused);
            Assert.AreEqual(4, reused.Damage);
            Assert.AreEqual(20f, reused.Speed, Tolerance);
            Assert.AreEqual(35f, reused.MaxDistance, Tolerance);
            Assert.AreEqual(0f, reused.TraveledDistance, Tolerance);
        }

        [Test]
        public void Fire_WithShotgun_ReturnsFirstOfVolleyAndRentsThree()
        {
            InitializeWithCatalog();
            weapon.TryEquip("shotgun");

            var first = weapon.Fire();

            Assert.IsNotNull(first);
            Assert.AreEqual(3, pool.CountActive);
        }

        [Test]
        public void Loadout_IsReachableAsIWeaponLoadoutFromChildCollider()
        {
            var child = new GameObject("GeneralCollider");
            spawnedObjects.Add(child);
            child.transform.SetParent(weaponObject.transform, false);
            var collider = child.AddComponent<BoxCollider>();

            Assert.AreSame(weapon, collider.GetComponentInParent<IWeaponLoadout>());
        }
    }
}
