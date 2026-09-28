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
    public class HordeSpawnerTests
    {
        private GameObject spawnerObject;
        private HordeSpawner spawner;
        private List<GameObject> spawnedObjects;
        private ObjectPool<EnemyController> pool;
        private LaneLayout layout;

        [SetUp]
        public void SetUp()
        {
            spawnedObjects = new List<GameObject>();
            spawnerObject = new GameObject("TestHordeSpawner");
            spawnedObjects.Add(spawnerObject);
            spawner = spawnerObject.AddComponent<HordeSpawner>();

            layout = new LaneLayout(3, 2.0f);

            pool = new ObjectPool<EnemyController>(() =>
            {
                var go = new GameObject("PooledEnemy");
                spawnedObjects.Add(go);
                var enemy = go.AddComponent<EnemyController>();
                return enemy;
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
        public void Initialize_SetsLayoutAndPool_AndExposesDefaultValues()
        {
            spawner.Initialize(layout, pool);

            Assert.AreEqual(layout.LaneCount, spawner.Layout.LaneCount);
            Assert.AreEqual(layout.LaneWidth, spawner.Layout.LaneWidth);
            Assert.AreSame(pool, spawner.Pool);
            Assert.AreEqual(20, spawner.DefaultEnemyHealth);
            Assert.AreEqual(2f, spawner.DefaultEnemySpeed);
            Assert.AreEqual(0, spawner.ActiveEnemies.Count);
        }

        [Test]
        public void SpawnWave_WithValidLane_SpawnsEnemiesAtCorrectPositionsAndInitializesThem()
        {
            spawner.Initialize(layout, pool);

            float expectedX = layout.GetLaneCenterX(0);
            var enemies = spawner.SpawnWave(laneIndex: 0, count: 3, startZ: 10f, spacing: 2.5f);

            Assert.AreEqual(3, enemies.Count);
            Assert.AreEqual(3, spawner.ActiveEnemies.Count);

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                Assert.IsTrue(enemy.gameObject.activeSelf);
                Assert.IsTrue(enemy.IsActiveInPool);
                Assert.AreEqual(0, enemy.LaneIndex);
                Assert.AreEqual(20, enemy.CurrentHealth);
                Assert.AreEqual(2f, enemy.MoveSpeed);

                Vector3 pos = enemy.transform.position;
                Assert.AreEqual(expectedX, pos.x, 0.0001f);
                Assert.AreEqual(0f, pos.y, 0.0001f);
                Assert.AreEqual(10f + i * 2.5f, pos.z, 0.0001f);
            }
        }

        [Test]
        public void SpawnWave_WhenLaneIndexInvalid_ThrowsArgumentOutOfRangeException()
        {
            spawner.Initialize(layout, pool);

            Assert.Throws<ArgumentOutOfRangeException>(() => spawner.SpawnWave(laneIndex: -1, count: 2, startZ: 5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => spawner.SpawnWave(laneIndex: 3, count: 2, startZ: 5f));
        }

        [Test]
        public void SpawnWave_WhenCountZeroOrNegative_ReturnsEmptyList()
        {
            spawner.Initialize(layout, pool);

            var zeroResult = spawner.SpawnWave(laneIndex: 1, count: 0, startZ: 5f);
            var negResult = spawner.SpawnWave(laneIndex: 1, count: -3, startZ: 5f);

            Assert.AreEqual(0, zeroResult.Count);
            Assert.AreEqual(0, negResult.Count);
            Assert.AreEqual(0, spawner.ActiveEnemies.Count);
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void SpawnWave_WhenPoolIsNull_ReturnsEmptyList()
        {
            spawner.Initialize(layout, null);

            var result = spawner.SpawnWave(laneIndex: 1, count: 2, startZ: 5f);

            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(0, spawner.ActiveEnemies.Count);
        }

        [Test]
        public void OnEnemyRecycled_RemovesFromActiveEnemies_AndReturnsToPool()
        {
            spawner.Initialize(layout, pool);
            var enemies = spawner.SpawnWave(laneIndex: 1, count: 2, startZ: 5f);

            Assert.AreEqual(2, spawner.ActiveEnemies.Count);
            Assert.AreEqual(2, pool.CountActive);

            enemies[0].Recycle();

            Assert.AreEqual(1, spawner.ActiveEnemies.Count);
            Assert.AreSame(enemies[1], spawner.ActiveEnemies[0]);
            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
        }

        [Test]
        public void ClearActiveEnemies_RecyclesAllActiveEnemies_AndEmptiesList()
        {
            spawner.Initialize(layout, pool);
            var enemies = spawner.SpawnWave(laneIndex: 1, count: 3, startZ: 5f);

            Assert.AreEqual(3, spawner.ActiveEnemies.Count);

            spawner.ClearActiveEnemies();

            Assert.AreEqual(0, spawner.ActiveEnemies.Count);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(3, pool.CountInactive);

            for (int i = 0; i < enemies.Count; i++)
            {
                Assert.IsFalse(enemies[i].IsActiveInPool);
                Assert.IsFalse(enemies[i].gameObject.activeSelf);
            }
        }
    }
}
