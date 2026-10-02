using System;
using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class ProjectileTests
    {
        private GameObject projectileObject;
        private Projectile projectile;

        [SetUp]
        public void SetUp()
        {
            projectileObject = new GameObject("TestProjectile");
            projectile = projectileObject.AddComponent<Projectile>();
        }

        [TearDown]
        public void TearDown()
        {
            if (projectileObject != null)
            {
                Object.DestroyImmediate(projectileObject);
            }
        }

        [Test]
        public void Initialize_EnsuresOwnKinematicRigidbody_WithoutGravity()
        {
            projectile.Initialize(damage: 10, speed: 15f, maxDistance: 40f, onRecycle: null);

            var body = projectileObject.GetComponent<Rigidbody>();

            Assert.IsNotNull(body, "Trigger-vs-trigger hits need a Rigidbody on one side; enemies have none.");
            Assert.IsTrue(body.isKinematic);
            Assert.IsFalse(body.useGravity);
        }

        [Test]
        public void Initialize_SetsProperties_ResetsTraveledDistanceAndSetsActiveInPool()
        {
            projectile.Initialize(damage: 15, speed: 20f, maxDistance: 50f, onRecycle: null, laneIndex: 2);

            Assert.AreEqual(15, projectile.Damage);
            Assert.AreEqual(20f, projectile.Speed);
            Assert.AreEqual(50f, projectile.MaxDistance);
            Assert.AreEqual(2, projectile.LaneIndex);
            Assert.AreEqual(0f, projectile.TraveledDistance);
            Assert.IsTrue(projectile.IsActiveInPool);
        }

        [Test]
        public void Tick_AdvancesPositionForward_AndIncreasesTraveledDistance()
        {
            projectileObject.transform.position = Vector3.zero;
            projectile.Initialize(damage: 10, speed: 12f, maxDistance: 40f, onRecycle: null);

            projectile.Tick(0.5f);

            Assert.AreEqual(new Vector3(0f, 0f, 6f), projectileObject.transform.position);
            Assert.AreEqual(6f, projectile.TraveledDistance, 0.0001f);
        }

        [Test]
        public void Tick_WhenInactiveInPool_DoesNotMove()
        {
            projectileObject.transform.position = Vector3.zero;
            projectile.Initialize(damage: 10, speed: 10f, maxDistance: 40f, onRecycle: null);
            projectile.Recycle();

            projectile.Tick(1f);

            Assert.AreEqual(Vector3.zero, projectileObject.transform.position);
            Assert.AreEqual(0f, projectile.TraveledDistance);
        }

        [Test]
        public void Tick_WhenGameObjectInactive_DoesNotMove()
        {
            projectileObject.transform.position = Vector3.zero;
            projectile.Initialize(damage: 10, speed: 10f, maxDistance: 40f, onRecycle: null);
            projectileObject.SetActive(false);

            projectile.Tick(1f);

            Assert.AreEqual(Vector3.zero, projectileObject.transform.position);
            Assert.AreEqual(0f, projectile.TraveledDistance);
        }

        [Test]
        public void Tick_WhenTraveledDistanceReachesOrExceedsMaxDistance_Recycles()
        {
            bool recycled = false;
            projectile.Initialize(damage: 10, speed: 20f, maxDistance: 30f, onRecycle: p => recycled = true);

            projectile.Tick(1.0f);
            Assert.IsFalse(recycled);
            Assert.IsTrue(projectile.IsActiveInPool);

            projectile.Tick(0.6f);
            Assert.IsTrue(recycled);
            Assert.IsFalse(projectile.IsActiveInPool);
            Assert.IsFalse(projectileObject.activeSelf);
        }

        [Test]
        public void Recycle_InvokesCallback_DeactivatesGameObject_AndPreventsDoubleRecycle()
        {
            int recycleCalls = 0;
            projectile.Initialize(damage: 10, speed: 10f, maxDistance: 30f, onRecycle: p => recycleCalls++);

            projectile.Recycle();

            Assert.AreEqual(1, recycleCalls);
            Assert.IsFalse(projectile.IsActiveInPool);
            Assert.IsFalse(projectileObject.activeSelf);

            projectile.Recycle();
            Assert.AreEqual(1, recycleCalls);
        }

        [Test]
        public void HandleTrigger_WithDamageableAlive_AppliesDamageAndRecycles()
        {
            var targetGo = new GameObject("Target");
            var health = targetGo.AddComponent<HealthComponent>();
            health.Initialize(100);

            bool recycled = false;
            projectile.Initialize(damage: 35, speed: 10f, maxDistance: 30f, onRecycle: p => recycled = true);

            projectile.HandleTrigger(targetGo);

            Assert.AreEqual(65, health.CurrentHealth);
            Assert.IsTrue(recycled);
            Assert.IsFalse(projectile.IsActiveInPool);
            Assert.IsFalse(projectileObject.activeSelf);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void HandleTrigger_WithDamageableAlive_PreventsMultipleHitsInSameFrame()
        {
            var target1 = new GameObject("Target1");
            var health1 = target1.AddComponent<HealthComponent>();
            health1.Initialize(100);

            var target2 = new GameObject("Target2");
            var health2 = target2.AddComponent<HealthComponent>();
            health2.Initialize(100);

            projectile.Initialize(damage: 40, speed: 10f, maxDistance: 30f, onRecycle: null);

            projectile.HandleTrigger(target1);
            projectile.HandleTrigger(target2);

            Assert.AreEqual(60, health1.CurrentHealth);
            Assert.AreEqual(100, health2.CurrentHealth);

            Object.DestroyImmediate(target1);
            Object.DestroyImmediate(target2);
        }

        [Test]
        public void HandleTrigger_WithNonDamageableObject_DoesNothing()
        {
            var plainGo = new GameObject("PlainObject");
            bool recycled = false;
            projectile.Initialize(damage: 20, speed: 10f, maxDistance: 30f, onRecycle: p => recycled = true);

            projectile.HandleTrigger(plainGo);

            Assert.IsFalse(recycled);
            Assert.IsTrue(projectile.IsActiveInPool);

            Object.DestroyImmediate(plainGo);
        }

        [Test]
        public void HandleTrigger_WithDeadDamageable_DoesNotApplyDamageOrRecycle()
        {
            var deadGo = new GameObject("DeadTarget");
            var health = deadGo.AddComponent<HealthComponent>();
            health.Initialize(0);

            bool recycled = false;
            projectile.Initialize(damage: 20, speed: 10f, maxDistance: 30f, onRecycle: p => recycled = true);

            projectile.HandleTrigger(deadGo);

            Assert.IsFalse(recycled);
            Assert.IsTrue(projectile.IsActiveInPool);

            Object.DestroyImmediate(deadGo);
        }

        [Test]
        public void HandleTrigger_WhenInactiveInPool_IgnoresCollision()
        {
            var targetGo = new GameObject("Target");
            var health = targetGo.AddComponent<HealthComponent>();
            health.Initialize(100);

            projectile.Initialize(damage: 30, speed: 10f, maxDistance: 30f, onRecycle: null);
            projectile.Recycle();

            projectile.HandleTrigger(targetGo);

            Assert.AreEqual(100, health.CurrentHealth);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void HandleTrigger_DisablesAttachedColliderImmediatelyOnHit()
        {
            var col = projectileObject.AddComponent<SphereCollider>();
            var targetGo = new GameObject("Target");
            var health = targetGo.AddComponent<HealthComponent>();
            health.Initialize(100);

            projectile.Initialize(damage: 25, speed: 10f, maxDistance: 30f, onRecycle: null);
            Assert.IsTrue(col.enabled);

            projectile.HandleTrigger(targetGo);

            Assert.IsFalse(col.enabled);

            Object.DestroyImmediate(targetGo);
        }
    }
}
