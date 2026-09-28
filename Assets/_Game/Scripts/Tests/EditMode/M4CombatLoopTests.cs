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
    public class M4CombatLoopTests
    {
        private GameObject directorObject;
        private CombatDirector director;
        private GameObject squadObject;
        private SquadController squad;
        private GameObject scrollerObject;
        private TrackScroller scroller;
        private GameStateMachine stateMachine;
        private GameObject spawnerObject;
        private HordeSpawner spawner;
        private List<GameObject> spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            spawnedObjects = new List<GameObject>();

            directorObject = new GameObject("TestCombatDirector");
            spawnedObjects.Add(directorObject);
            director = directorObject.AddComponent<CombatDirector>();

            squadObject = new GameObject("TestSquad");
            spawnedObjects.Add(squadObject);
            squad = squadObject.AddComponent<SquadController>();
            squad.Initialize(3);

            scrollerObject = new GameObject("TestScroller");
            spawnedObjects.Add(scrollerObject);
            scroller = scrollerObject.AddComponent<TrackScroller>();
            scroller.ForwardSpeed = 10f;

            stateMachine = new GameStateMachine(null, GameState.Boot);
            stateMachine.TryTransition(GameState.Run);

            spawnerObject = new GameObject("TestSpawner");
            spawnedObjects.Add(spawnerObject);
            spawner = spawnerObject.AddComponent<HordeSpawner>();

            director.Initialize(squad, scroller, stateMachine, 120f, spawner);
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

        private EnemyController CreateEnemy(int laneIndex = 0, int health = 20, float speed = 2f)
        {
            var go = new GameObject("TestEnemy");
            spawnedObjects.Add(go);
            var enemy = go.AddComponent<EnemyController>();
            enemy.Initialize(laneIndex, health, speed, null);
            return enemy;
        }

        [Test]
        public void Initialize_StoresReferences_AndResetsIsResolved()
        {
            Assert.AreSame(squad, director.Squad);
            Assert.AreSame(scroller, director.Scroller);
            Assert.AreSame(stateMachine, director.StateMachine);
            Assert.AreEqual(120f, director.VictoryDistance);
            Assert.AreSame(spawner, director.Spawner);
            Assert.IsFalse(director.IsResolved);
        }

        [Test]
        public void ResolveEnemyContact_WhenSquadHasSoldiers_RemovesOneSoldierAndRecyclesEnemy()
        {
            var enemy = CreateEnemy();

            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsTrue(result);
            Assert.AreEqual(2, squad.SquadCount);
            Assert.IsFalse(enemy.IsActiveInPool);
            Assert.IsFalse(director.IsResolved);
            Assert.IsFalse(scroller.IsPaused);
            Assert.AreEqual(GameState.Run, stateMachine.CurrentState);
        }

        [Test]
        public void ResolveEnemyContact_WhenSquadCountIsZero_TriggersDefeat_PausesScroller_AndRecyclesEnemy()
        {
            squad.Initialize(0);
            var enemy = CreateEnemy();

            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsTrue(result);
            Assert.IsFalse(enemy.IsActiveInPool);
            Assert.IsTrue(director.IsResolved);
            Assert.IsTrue(scroller.IsPaused);
            Assert.AreEqual(GameState.Defeat, stateMachine.CurrentState);
        }

        [Test]
        public void ResolveEnemyContact_WhenSquadIsNull_TriggersDefeat_PausesScroller_AndRecyclesEnemy()
        {
            director.Initialize(null, scroller, stateMachine, 120f, spawner);
            var enemy = CreateEnemy();

            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsTrue(result);
            Assert.IsFalse(enemy.IsActiveInPool);
            Assert.IsTrue(director.IsResolved);
            Assert.IsTrue(scroller.IsPaused);
            Assert.AreEqual(GameState.Defeat, stateMachine.CurrentState);
        }

        [Test]
        public void ResolveEnemyContact_WhenEnemyIsNull_ReturnsFalse()
        {
            bool result = director.ResolveEnemyContact(null);

            Assert.IsFalse(result);
            Assert.AreEqual(3, squad.SquadCount);
            Assert.IsFalse(director.IsResolved);
        }

        [Test]
        public void ResolveEnemyContact_WhenEnemyInactiveInPool_ReturnsFalse()
        {
            var enemy = CreateEnemy();
            enemy.Recycle();

            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsFalse(result);
            Assert.AreEqual(3, squad.SquadCount);
            Assert.IsFalse(director.IsResolved);
        }

        [Test]
        public void ResolveEnemyContact_WhenAlreadyResolved_ReturnsFalse()
        {
            director.TriggerDefeat();
            Assert.IsTrue(director.IsResolved);

            var enemy = CreateEnemy();
            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsFalse(result);
            Assert.IsTrue(enemy.IsActiveInPool);
            Assert.AreEqual(3, squad.SquadCount);
        }

        [Test]
        public void CheckVictory_WhenDistanceReachedInRunState_TransitionsToVictoryAndPausesScroller()
        {
            scroller.Step(12.5f); // 10f/s * 12.5s = 125f >= 120f
            Assert.GreaterOrEqual(scroller.DistanceTraveled, 120f);

            bool won = director.CheckVictory();

            Assert.IsTrue(won);
            Assert.IsTrue(director.IsResolved);
            Assert.IsTrue(scroller.IsPaused);
            Assert.AreEqual(GameState.Victory, stateMachine.CurrentState);
        }

        [Test]
        public void CheckVictory_WhenDistanceNotReached_ReturnsFalse()
        {
            scroller.Step(5.0f); // 50f < 120f
            Assert.Less(scroller.DistanceTraveled, 120f);

            bool won = director.CheckVictory();

            Assert.IsFalse(won);
            Assert.IsFalse(director.IsResolved);
            Assert.IsFalse(scroller.IsPaused);
            Assert.AreEqual(GameState.Run, stateMachine.CurrentState);
        }

        [Test]
        public void CheckVictory_WhenNotInRunState_ReturnsFalse()
        {
            var sm = new GameStateMachine(null, GameState.Boot);
            director.Initialize(squad, scroller, sm, 120f, spawner);
            scroller.Step(15.0f); // 150f >= 120f

            bool won = director.CheckVictory();

            Assert.IsFalse(won);
            Assert.IsFalse(director.IsResolved);
            Assert.AreEqual(GameState.Boot, sm.CurrentState);
        }

        [Test]
        public void CheckVictory_WhenAlreadyResolved_ReturnsFalse()
        {
            scroller.Step(15.0f);
            director.TriggerDefeat();
            Assert.IsTrue(director.IsResolved);

            bool won = director.CheckVictory();

            Assert.IsFalse(won);
            Assert.AreEqual(GameState.Defeat, stateMachine.CurrentState);
        }

        [Test]
        public void Tick_WhenDistanceReachedInRunState_TriggersVictory()
        {
            scroller.Step(13.0f); // 130f >= 120f

            director.Tick(0.1f);

            Assert.IsTrue(director.IsResolved);
            Assert.IsTrue(scroller.IsPaused);
            Assert.AreEqual(GameState.Victory, stateMachine.CurrentState);
        }

        [Test]
        public void TriggerDefeat_WhenAlreadyResolved_DoesNotRetrigger()
        {
            director.TriggerVictory();
            Assert.AreEqual(GameState.Victory, stateMachine.CurrentState);

            director.TriggerDefeat();

            Assert.AreEqual(GameState.Victory, stateMachine.CurrentState);
        }

        [Test]
        public void TriggerVictory_WhenAlreadyResolved_DoesNotRetrigger()
        {
            director.TriggerDefeat();
            Assert.AreEqual(GameState.Defeat, stateMachine.CurrentState);

            director.TriggerVictory();

            Assert.AreEqual(GameState.Defeat, stateMachine.CurrentState);
        }

        [Test]
        public void OnTriggerEnter_WithEnemyController_ResolvesContact()
        {
            var enemyGo = new GameObject("EnemyColliderGo");
            spawnedObjects.Add(enemyGo);
            var col = enemyGo.AddComponent<BoxCollider>();
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(0, 20, 2f, null);

            director.OnTriggerEnter(col);

            Assert.AreEqual(2, squad.SquadCount);
            Assert.IsFalse(enemy.IsActiveInPool);
        }

        [Test]
        public void OnTriggerEnter_WithChildColliderOnEnemy_ResolvesContact()
        {
            var enemyRoot = new GameObject("EnemyRoot");
            spawnedObjects.Add(enemyRoot);
            var enemy = enemyRoot.AddComponent<EnemyController>();
            enemy.Initialize(0, 20, 2f, null);

            var childGo = new GameObject("EnemyChild");
            childGo.transform.SetParent(enemyRoot.transform, false);
            var col = childGo.AddComponent<BoxCollider>();

            director.OnTriggerEnter(col);

            Assert.AreEqual(2, squad.SquadCount);
            Assert.IsFalse(enemy.IsActiveInPool);
        }

        [Test]
        public void OnTriggerEnter_WithNonEnemyObject_DoesNothing()
        {
            var propGo = new GameObject("Prop");
            spawnedObjects.Add(propGo);
            var col = propGo.AddComponent<BoxCollider>();

            director.OnTriggerEnter(col);

            Assert.AreEqual(3, squad.SquadCount);
            Assert.IsFalse(director.IsResolved);
        }
    }
}
