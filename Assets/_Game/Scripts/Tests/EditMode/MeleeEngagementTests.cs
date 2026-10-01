using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Status;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class MeleeEngagementTests
    {
        private GameObject rootObject;
        private MeleeEngagementManager meleeManager;
        private SquadController squad;
        private CombatDirector combatDirector;
        private EventBus eventBus;
        private List<GameObject> spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            spawnedObjects = new List<GameObject>();

            rootObject = new GameObject("TestRoot");
            spawnedObjects.Add(rootObject);

            squad = rootObject.AddComponent<SquadController>();
            squad.Initialize(3);

            combatDirector = rootObject.AddComponent<CombatDirector>();
            var stateMachine = new GameStateMachine(null, GameState.Boot);
            stateMachine.TryTransition(GameState.Run);
            combatDirector.Initialize(squad, null, stateMachine, 100f);

            eventBus = new EventBus();
            combatDirector.EventBus = eventBus;

            meleeManager = rootObject.AddComponent<MeleeEngagementManager>();
            meleeManager.Initialize(squad, combatDirector, eventBus);
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

        private EnemyController CreateEnemy(Vector3 initialPosition, float contactDps = 5f)
        {
            var go = new GameObject("TestEnemy");
            spawnedObjects.Add(go);
            go.transform.position = initialPosition;
            var enemy = go.AddComponent<EnemyController>();
            enemy.Initialize(laneIndex: 0, maxHealth: 20, moveSpeed: 2f, onDeath: null);
            enemy.ContactDPS = contactDps;
            return enemy;
        }

        [Test]
        public void Engage_PositionsEnemyAtOffset_MarksEngaged_AndAddsToEngagedEnemies()
        {
            rootObject.transform.position = new Vector3(0f, 0f, 10f);
            var enemy = CreateEnemy(new Vector3(1.5f, 0f, 12f));

            bool result = meleeManager.Engage(enemy);

            Assert.IsTrue(result);
            Assert.AreEqual(1, meleeManager.EngagedEnemies.Count);
            Assert.AreSame(enemy, meleeManager.EngagedEnemies[0]);
            Assert.IsTrue(enemy.IsEngaged);
            Assert.AreSame(meleeManager.transform, enemy.FollowTarget);

            Vector3 expectedPos = new Vector3(1.5f, 0f, 10.8f);
            Assert.AreEqual(expectedPos.x, enemy.transform.position.x, 0.001f);
            Assert.AreEqual(expectedPos.y, enemy.transform.position.y, 0.001f);
            Assert.AreEqual(expectedPos.z, enemy.transform.position.z, 0.001f);
        }

        [Test]
        public void Engage_PublishesEnemyEngagedEvent()
        {
            var enemy = CreateEnemy(Vector3.zero);
            enemy.ArchetypeId = "runner";
            enemy.LaneIndex = 2;

            bool eventReceived = false;
            string receivedArchetype = null;
            int receivedLane = -1;

            using (eventBus.Subscribe<EnemyEngagedEvent>(e =>
            {
                eventReceived = true;
                receivedArchetype = e.ArchetypeId;
                receivedLane = e.LaneIndex;
            }))
            {
                meleeManager.Engage(enemy);
            }

            Assert.IsTrue(eventReceived);
            Assert.AreEqual("runner", receivedArchetype);
            Assert.AreEqual(2, receivedLane);
        }

        [Test]
        public void Engage_WhenInvalidOrDuplicate_ReturnsFalse()
        {
            Assert.IsFalse(meleeManager.Engage(null));

            var enemy = CreateEnemy(Vector3.zero);
            enemy.Recycle();
            Assert.IsFalse(meleeManager.Engage(enemy));

            var activeEnemy = CreateEnemy(Vector3.zero);
            Assert.IsTrue(meleeManager.Engage(activeEnemy));
            Assert.IsFalse(meleeManager.Engage(activeEnemy));

            var otherEnemy = CreateEnemy(Vector3.zero);
            otherEnemy.Engage(rootObject.transform, Vector3.forward);
            Assert.IsFalse(meleeManager.Engage(otherEnemy));
        }

        [Test]
        public void Disengage_RemovesFromList_ResetsEnemy_AndPublishesEvent()
        {
            var enemy = CreateEnemy(Vector3.zero);
            meleeManager.Engage(enemy);

            bool eventReceived = false;
            bool wasKilledFlag = true;

            using (eventBus.Subscribe<EnemyDisengagedEvent>(e =>
            {
                eventReceived = true;
                wasKilledFlag = e.WasKilled;
            }))
            {
                bool result = meleeManager.Disengage(enemy, wasKilled: false);
                Assert.IsTrue(result);
            }

            Assert.IsTrue(eventReceived);
            Assert.IsFalse(wasKilledFlag);
            Assert.IsFalse(enemy.IsEngaged);
            Assert.AreEqual(0, meleeManager.EngagedEnemies.Count);
        }

        [Test]
        public void Disengage_WhenNotEngaged_ReturnsFalse()
        {
            var enemy = CreateEnemy(Vector3.zero);
            Assert.IsFalse(meleeManager.Disengage(enemy));
            Assert.IsFalse(meleeManager.Disengage(null));
        }

        [Test]
        public void ClearAll_DisengagesAllEnemies_AndClearsList()
        {
            var enemyA = CreateEnemy(new Vector3(-1f, 0f, 5f));
            var enemyB = CreateEnemy(new Vector3(1f, 0f, 5f));

            meleeManager.Engage(enemyA);
            meleeManager.Engage(enemyB);
            Assert.AreEqual(2, meleeManager.EngagedEnemies.Count);

            meleeManager.ClearAll();

            Assert.AreEqual(0, meleeManager.EngagedEnemies.Count);
            Assert.IsFalse(enemyA.IsEngaged);
            Assert.IsFalse(enemyB.IsEngaged);
        }

        [Test]
        public void Tick_AccumulatesDps_ReducesSquadWhenBufferDepleted()
        {
            var enemy = CreateEnemy(Vector3.zero, contactDps: 10f);
            meleeManager.Engage(enemy);

            meleeManager.Tick(0.5f);
            Assert.AreEqual(3, squad.SquadCount);
            Assert.AreEqual(5f, meleeManager.HealthBuffer.CurrentSoldierHealth, 0.001f);

            meleeManager.Tick(0.5f);
            Assert.AreEqual(2, squad.SquadCount);
            Assert.AreEqual(10f, meleeManager.HealthBuffer.CurrentSoldierHealth, 0.001f);
        }

        [Test]
        public void Tick_PublishesSoldierDamagedEvent()
        {
            var enemy = CreateEnemy(Vector3.zero, contactDps: 10f);
            meleeManager.Engage(enemy);

            float reportedCurrentHealth = -1f;
            float reportedMaxHealth = -1f;

            using (eventBus.Subscribe<SoldierDamagedEvent>(e =>
            {
                reportedCurrentHealth = e.CurrentHealth;
                reportedMaxHealth = e.MaxHealth;
            }))
            {
                meleeManager.Tick(0.4f);
            }

            Assert.AreEqual(6f, reportedCurrentHealth, 0.001f);
            Assert.AreEqual(10f, reportedMaxHealth, 0.001f);
        }

        [Test]
        public void Tick_WhenAllSoldiersDie_OverflowDamagesGeneralAndTriggersDefeat()
        {
            squad.Initialize(1);
            var enemy = CreateEnemy(Vector3.zero, contactDps: 25f);
            meleeManager.Engage(enemy);

            meleeManager.Tick(1.0f);

            Assert.AreEqual(0, squad.SquadCount);
            Assert.AreEqual(0f, meleeManager.HealthBuffer.CurrentGeneralHealth, 0.001f);
            Assert.IsTrue(combatDirector.IsResolved);
        }

        [Test]
        public void Tick_WhenEnemyFrozen_DealsNoDamage()
        {
            var enemy = CreateEnemy(Vector3.zero, contactDps: 10f);
            meleeManager.Engage(enemy);

            enemy.Status.Apply(new StatusApplication(StatusKind.Frozen, 1, this));
            Assert.IsTrue(enemy.Status.IsFrozen);

            meleeManager.Tick(1.0f);

            Assert.AreEqual(3, squad.SquadCount);
            Assert.AreEqual(10f, meleeManager.HealthBuffer.CurrentSoldierHealth, 0.001f);
        }

        [Test]
        public void Tick_WhenEngagedEnemyRecycled_RemovesFromListAndPublishesDisengaged()
        {
            var enemy = CreateEnemy(Vector3.zero, contactDps: 5f);
            meleeManager.Engage(enemy);
            Assert.AreEqual(1, meleeManager.EngagedEnemies.Count);

            enemy.Recycle();

            bool eventReceived = false;
            bool wasKilledFlag = false;

            using (eventBus.Subscribe<EnemyDisengagedEvent>(e =>
            {
                eventReceived = true;
                wasKilledFlag = e.WasKilled;
            }))
            {
                meleeManager.Tick(0.1f);
            }

            Assert.IsTrue(eventReceived);
            Assert.IsTrue(wasKilledFlag);
            Assert.AreEqual(0, meleeManager.EngagedEnemies.Count);
        }

        [Test]
        public void Tick_WhenCombatDirectorResolved_ClearsAllEnemies()
        {
            var enemy = CreateEnemy(Vector3.zero, contactDps: 5f);
            meleeManager.Engage(enemy);

            combatDirector.TriggerDefeat();
            Assert.IsTrue(combatDirector.IsResolved);

            meleeManager.Tick(0.1f);

            Assert.AreEqual(0, meleeManager.EngagedEnemies.Count);
            Assert.IsFalse(enemy.IsEngaged);
        }

        [Test]
        public void CombatDirector_ResolveEnemyContact_DelegatesToMeleeManagerWithoutRecycle()
        {
            var enemy = CreateEnemy(Vector3.zero, contactDps: 5f);

            bool result = combatDirector.ResolveEnemyContact(enemy);

            Assert.IsTrue(result);
            Assert.IsTrue(enemy.IsEngaged);
            Assert.IsTrue(enemy.IsActiveInPool);
            Assert.AreEqual(3, squad.SquadCount);
            Assert.AreEqual(1, meleeManager.EngagedEnemies.Count);

            bool duplicateResult = combatDirector.ResolveEnemyContact(enemy);
            Assert.IsFalse(duplicateResult);
        }

        [Test]
        public void CombatDirector_StopCombat_ClearsMeleeManagerEngagedEnemies()
        {
            var enemy = CreateEnemy(Vector3.zero, contactDps: 5f);
            combatDirector.ResolveEnemyContact(enemy);
            Assert.AreEqual(1, meleeManager.EngagedEnemies.Count);

            combatDirector.TriggerDefeat();

            Assert.AreEqual(0, meleeManager.EngagedEnemies.Count);
            Assert.IsFalse(enemy.IsEngaged);
        }

        [Test]
        public void Awake_FallbackInitialization_ProvidesValidHealthBuffer()
        {
            var standaloneGo = new GameObject("Standalone");
            spawnedObjects.Add(standaloneGo);

            var standaloneManager = standaloneGo.AddComponent<MeleeEngagementManager>();

            Assert.IsNotNull(standaloneManager.HealthBuffer);
            Assert.AreEqual(10f, standaloneManager.HealthBuffer.SoldierMaxHealth);
            Assert.AreEqual(10f, standaloneManager.HealthBuffer.GeneralMaxHealth);
        }
    }
}
