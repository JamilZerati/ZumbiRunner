using System;
using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class EnemyControllerTests
    {
        private GameObject enemyObject;
        private EnemyController enemy;

        [SetUp]
        public void SetUp()
        {
            enemyObject = new GameObject("TestEnemy");
            enemy = enemyObject.AddComponent<EnemyController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (enemyObject != null)
            {
                Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void Initialize_SetsProperties_AddsHealthComponent_AndSetsActiveInPool()
        {
            Action<EnemyController> deathCallback = _ => { };
            enemy.Initialize(laneIndex: 1, maxHealth: 25, moveSpeed: 3.5f, onDeath: deathCallback);

            Assert.AreEqual(1, enemy.LaneIndex);
            Assert.AreEqual(3.5f, enemy.MoveSpeed);
            Assert.AreSame(deathCallback, enemy.OnDeath);
            Assert.IsTrue(enemy.IsActiveInPool);
            Assert.IsNotNull(enemy.Health);
            Assert.AreEqual(25, enemy.CurrentHealth);
            Assert.AreEqual(25, enemy.MaxHealth);
            Assert.IsTrue(enemy.IsAlive);
        }

        [Test]
        public void Initialize_WhenColliderPresent_EnablesCollider()
        {
            var col = enemyObject.AddComponent<BoxCollider>();
            col.enabled = false;

            enemy.Initialize(laneIndex: 0, maxHealth: 20, moveSpeed: 2f, onDeath: null);

            Assert.IsTrue(col.enabled);
        }

        [Test]
        public void Tick_AdvancesPositionTowardsNegativeZ_ProportionalToSpeedAndDeltaTime()
        {
            enemyObject.transform.position = new Vector3(2f, 0f, 10f);
            enemy.Initialize(laneIndex: 0, maxHealth: 20, moveSpeed: 4f, onDeath: null);

            enemy.Tick(0.5f);

            Assert.AreEqual(new Vector3(2f, 0f, 8f), enemyObject.transform.position);
        }

        [Test]
        public void Tick_WhenInactiveInPool_DoesNotMove()
        {
            enemyObject.transform.position = new Vector3(0f, 0f, 10f);
            enemy.Initialize(laneIndex: 0, maxHealth: 20, moveSpeed: 4f, onDeath: null);
            enemy.Recycle();

            enemy.Tick(1.0f);

            Assert.AreEqual(new Vector3(0f, 0f, 10f), enemyObject.transform.position);
        }

        [Test]
        public void Tick_WhenGameObjectInactive_DoesNotMove()
        {
            enemyObject.transform.position = new Vector3(0f, 0f, 10f);
            enemy.Initialize(laneIndex: 0, maxHealth: 20, moveSpeed: 4f, onDeath: null);
            enemyObject.SetActive(false);

            enemy.Tick(1.0f);

            Assert.AreEqual(new Vector3(0f, 0f, 10f), enemyObject.transform.position);
        }

        [Test]
        public void Tick_WhenDead_DoesNotMove()
        {
            enemyObject.transform.position = new Vector3(0f, 0f, 10f);
            enemy.Initialize(laneIndex: 0, maxHealth: 10, moveSpeed: 4f, onDeath: null);
            enemy.TakeDamage(new DamageInfo(10));
            enemyObject.SetActive(true);

            enemy.Tick(1.0f);

            Assert.AreEqual(new Vector3(0f, 0f, 10f), enemyObject.transform.position);
        }

        [Test]
        public void TakeDamage_ReducesHealth_AndKeepsAliveWhenHealthAboveZero()
        {
            enemy.Initialize(laneIndex: 0, maxHealth: 30, moveSpeed: 2f, onDeath: null);

            enemy.TakeDamage(new DamageInfo(10));

            Assert.AreEqual(20, enemy.CurrentHealth);
            Assert.IsTrue(enemy.IsAlive);
            Assert.IsTrue(enemy.IsActiveInPool);
            Assert.IsTrue(enemyObject.activeSelf);
        }

        [Test]
        public void TakeDamage_WhenFatalDamage_CallsDie_DeactivatesObject_AndInvokesOnDeath()
        {
            EnemyController deadEnemy = null;
            enemy.Initialize(laneIndex: 0, maxHealth: 20, moveSpeed: 2f, onDeath: e => deadEnemy = e);

            enemy.TakeDamage(new DamageInfo(20));

            Assert.AreEqual(0, enemy.CurrentHealth);
            Assert.IsFalse(enemy.IsAlive);
            Assert.IsFalse(enemy.IsActiveInPool);
            Assert.IsFalse(enemyObject.activeSelf);
            Assert.AreSame(enemy, deadEnemy);
        }

        [Test]
        public void TakeDamage_WhenInactiveOrDead_IgnoresDamage()
        {
            enemy.Initialize(laneIndex: 0, maxHealth: 20, moveSpeed: 2f, onDeath: null);
            enemy.Recycle();

            enemy.TakeDamage(new DamageInfo(10));

            Assert.AreEqual(20, enemy.CurrentHealth);
        }

        [Test]
        public void Die_WhenColliderPresent_DisablesCollider()
        {
            var col = enemyObject.AddComponent<BoxCollider>();
            enemy.Initialize(laneIndex: 0, maxHealth: 20, moveSpeed: 2f, onDeath: null);
            Assert.IsTrue(col.enabled);

            enemy.Die();

            Assert.IsFalse(col.enabled);
        }

        [Test]
        public void Recycle_PreventsDoubleInvocation()
        {
            int deathCount = 0;
            enemy.Initialize(laneIndex: 0, maxHealth: 20, moveSpeed: 2f, onDeath: _ => deathCount++);

            enemy.Recycle();
            enemy.Recycle();
            enemy.Die();

            Assert.AreEqual(1, deathCount);
        }
    }
}
