using System.Collections.Generic;
using Game.Core;
using Game.Core.Status;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class EnemyStatusIntegrationTests
    {
        private List<Object> toDestroy;

        [SetUp]
        public void SetUp()
        {
            toDestroy = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = toDestroy.Count - 1; i >= 0; i--)
            {
                if (toDestroy[i] != null)
                {
                    Object.DestroyImmediate(toDestroy[i]);
                }
            }
            toDestroy.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            toDestroy.Add(obj);
            return obj;
        }

        private EnemyController CreateEnemy(int health = 40, float moveSpeed = 2f)
        {
            var go = Track(new GameObject("Enemy"));
            var h = go.AddComponent<HealthComponent>();
            h.Initialize(health);
            var enemy = go.AddComponent<EnemyController>();
            enemy.MoveSpeed = moveSpeed;
            return enemy;
        }

        [Test]
        public void Enemy_SlowStatus_MultipliesDisplacementWithoutModifyingBaseMoveSpeed()
        {
            var enemy = CreateEnemy(40, 2f);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var directorGo = Track(new GameObject("Director"));
            var director = directorGo.AddComponent<StatusEffectDirector>();
            director.Initialize(catalog, null);
            enemy.AttachStatusDirector(director);

            enemy.Status.Apply(new StatusApplication(StatusKind.Slow, 1, this));

            Vector3 startPos = enemy.transform.position;
            enemy.Tick(1f);

            // With speed 2 and slow 0.4 -> effective speed = 1.2 -> displacement = 1.2
            float displacement = Vector3.Distance(startPos, enemy.transform.position);
            Assert.AreEqual(1.2f, displacement, 1e-4f, "Enemy displacement should reflect 40% slow (1.2 per second).");
            Assert.AreEqual(2f, enemy.MoveSpeed, "Base MoveSpeed property must remain unchanged at 2.0.");
        }

        [Test]
        public void Enemy_FrozenStatus_PreventsDisplacement()
        {
            var enemy = CreateEnemy(40, 2f);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var directorGo = Track(new GameObject("Director"));
            var director = directorGo.AddComponent<StatusEffectDirector>();
            director.Initialize(catalog, null);
            enemy.AttachStatusDirector(director);

            enemy.Status.Apply(new StatusApplication(StatusKind.Freeze, 2, this));
            Vector3 startPos = enemy.transform.position;
            enemy.Tick(1f);

            float displacement = Vector3.Distance(startPos, enemy.transform.position);
            Assert.AreEqual(0f, displacement, 1e-4f, "Frozen enemy must not move.");
        }

        [Test]
        public void Enemy_BurnDeathInTick_InvokesOnDeathOnceAndRecycles()
        {
            var enemy = CreateEnemy(3, 2f);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var directorGo = Track(new GameObject("Director"));
            var director = directorGo.AddComponent<StatusEffectDirector>();
            director.Initialize(catalog, null);
            enemy.AttachStatusDirector(director);

            int deathCount = 0;
            enemy.Initialize(0, 3, 2f, e => deathCount++);

            // Apply burn (3 damage per tick)
            enemy.Status.Apply(new StatusApplication(StatusKind.Burn, 1, this));
            enemy.Tick(0.5f);

            Assert.IsFalse(enemy.IsAlive, "Enemy should be dead from burn DoT.");
            Assert.IsFalse(enemy.IsActiveInPool, "Enemy should be deactivated/recycled.");
            Assert.AreEqual(1, deathCount, "OnDeath should be invoked exactly once.");
        }

        [Test]
        public void Enemy_RecycledAndReinitialized_HasStatusCleared()
        {
            var enemy = CreateEnemy(40, 2f);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var directorGo = Track(new GameObject("Director"));
            var director = directorGo.AddComponent<StatusEffectDirector>();
            director.Initialize(catalog, null);
            enemy.AttachStatusDirector(director);

            enemy.Status.Apply(new StatusApplication(StatusKind.Burn, 1, this));
            Assert.IsTrue(enemy.Status.Has(StatusKind.Burn));

            enemy.Recycle();
            enemy.Initialize(0, 40, 2f, null);

            Assert.IsFalse(enemy.Status.Has(StatusKind.Burn), "Recycled enemy must have clean status state.");
        }

        [Test]
        public void StatusEffectDirector_FindNearby_ExcludesOriginAndDead_OrdersByDistance()
        {
            var directorGo = Track(new GameObject("Director"));
            var director = directorGo.AddComponent<StatusEffectDirector>();

            var origin = CreateEnemy(40);
            origin.transform.position = new Vector3(0, 0, 0);

            var enemyA = CreateEnemy(40);
            enemyA.transform.position = new Vector3(0, 0, 1);

            var enemyB = CreateEnemy(40);
            enemyB.transform.position = new Vector3(0, 0, 3);

            var enemyDead = CreateEnemy(0);
            enemyDead.transform.position = new Vector3(0, 0, 2);

            director.Register(origin);
            director.Register(enemyA);
            director.Register(enemyB);
            director.Register(enemyDead);

            // Registering enemyA again is idempotent
            director.Register(enemyA);
            Assert.AreEqual(4, director.Registered.Count);

            var nearby = director.FindNearby(origin, 4f, 2);
            Assert.AreEqual(2, nearby.Count);
            Assert.AreSame(enemyA, nearby[0]);
            Assert.AreSame(enemyB, nearby[1]);
        }
    }
}
