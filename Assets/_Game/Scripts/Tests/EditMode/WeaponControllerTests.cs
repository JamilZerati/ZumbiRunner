using System;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using Game.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class WeaponControllerTests
    {
        private GameObject weaponObject;
        private WeaponController weapon;
        private List<GameObject> spawnedObjects;
        private ObjectPool<Projectile> pool;

        [SetUp]
        public void SetUp()
        {
            spawnedObjects = new List<GameObject>();
            weaponObject = new GameObject("TestWeaponController");
            spawnedObjects.Add(weaponObject);
            weapon = weaponObject.AddComponent<WeaponController>();

            pool = new ObjectPool<Projectile>(() =>
            {
                var go = new GameObject("PooledProjectile");
                spawnedObjects.Add(go);
                var p = go.AddComponent<Projectile>();
                return p;
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

        [Test]
        public void Initialize_StoresPoolAndSquad_AndResetsFireTimer()
        {
            weapon.Initialize(pool, null);

            Assert.AreSame(pool, weapon.Pool);
            Assert.IsNull(weapon.Squad);
            Assert.AreEqual(0f, weapon.FireTimer);
        }

        [Test]
        public void Fire_WhenPoolIsNull_ReturnsNull()
        {
            weapon.Initialize(null);

            var fired = weapon.Fire();

            Assert.IsNull(fired);
        }

        [Test]
        public void Fire_RentsProjectileFromPool_PositionsFrontOfWeapon_AndInitializes()
        {
            weaponObject.transform.position = new Vector3(3f, 0f, 10f);
            weapon.DamagePerShot = 20;
            weapon.ProjectileSpeed = 25f;
            weapon.MaxDistance = 50f;
            weapon.Initialize(pool);

            var proj = weapon.Fire();

            Assert.IsNotNull(proj);
            Assert.AreEqual(new Vector3(3f, 0f, 10.5f), proj.transform.position);
            Assert.IsTrue(proj.gameObject.activeSelf);
            Assert.AreEqual(20, proj.Damage);
            Assert.AreEqual(25f, proj.Speed);
            Assert.AreEqual(50f, proj.MaxDistance);
            Assert.IsTrue(proj.IsActiveInPool);
            Assert.AreEqual(1, pool.CountActive);
        }

        [Test]
        public void Tick_WhenFiringAndTimerReachesInterval_FiresProjectile()
        {
            weapon.FireRate = 2f; // interval = 0.5s
            weapon.Initialize(pool);

            weapon.Tick(0.3f);
            Assert.AreEqual(0.3f, weapon.FireTimer, 0.0001f);
            Assert.AreEqual(0, pool.CountActive);

            weapon.Tick(0.2f);
            // 0.3 + 0.2 = 0.5 >= 0.5 -> fires, timer -= 0.5
            Assert.AreEqual(0f, weapon.FireTimer, 0.0001f);
            Assert.AreEqual(1, pool.CountActive);
        }

        [Test]
        public void Tick_WhenIsFiringIsFalse_DoesNotAccumulateTimerOrFire()
        {
            weapon.FireRate = 2f;
            weapon.IsFiring = false;
            weapon.Initialize(pool);

            weapon.Tick(1.0f);

            Assert.AreEqual(0f, weapon.FireTimer);
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void Tick_WhenFireRateZeroOrNegative_DoesNotAccumulateOrFire()
        {
            weapon.FireRate = 0f;
            weapon.Initialize(pool);

            weapon.Tick(1.0f);

            Assert.AreEqual(0f, weapon.FireTimer);
            Assert.AreEqual(0, pool.CountActive);

            weapon.FireRate = -1f;
            weapon.Tick(1.0f);

            Assert.AreEqual(0f, weapon.FireTimer);
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void FullCombatCycle_WeaponFires_ProjectileHitsTarget_ReturnsToPool()
        {
            weaponObject.transform.position = Vector3.zero;
            weapon.DamagePerShot = 30;
            weapon.Initialize(pool);

            var targetGo = new GameObject("Target");
            spawnedObjects.Add(targetGo);
            var health = targetGo.AddComponent<HealthComponent>();
            health.Initialize(100);

            var proj = weapon.Fire();
            Assert.AreEqual(1, pool.CountActive);

            // Projectile hits target
            proj.HandleTrigger(targetGo);

            Assert.AreEqual(70, health.CurrentHealth);
            Assert.IsFalse(proj.IsActiveInPool);
            Assert.IsFalse(proj.gameObject.activeSelf);
            Assert.AreEqual(0, pool.CountActive);

            // Second fire reuses recycled projectile from pool
            var reusedProj = weapon.Fire();
            Assert.AreSame(proj, reusedProj);
            Assert.IsTrue(reusedProj.IsActiveInPool);
            Assert.IsTrue(reusedProj.gameObject.activeSelf);
            Assert.AreEqual(0f, reusedProj.TraveledDistance);
            Assert.AreEqual(1, pool.CountActive);
        }
    }
}
