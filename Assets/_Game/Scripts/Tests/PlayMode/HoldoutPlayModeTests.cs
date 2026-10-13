using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Holdout;
using Game.Data;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class HoldoutPlayModeTests
    {
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
        private readonly List<ScriptableObject> _spawnedAssets = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (_spawnedObjects[i] != null)
                {
                    Object.Destroy(_spawnedObjects[i]);
                }
            }
            _spawnedObjects.Clear();

            for (int i = _spawnedAssets.Count - 1; i >= 0; i--)
            {
                if (_spawnedAssets[i] != null)
                {
                    Object.Destroy(_spawnedAssets[i]);
                }
            }
            _spawnedAssets.Clear();
        }

        private T CreateScriptableObject<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            _spawnedAssets.Add(so);
            return so;
        }

        private Barricade CreateBarricade(
            Vector3 position,
            int hp = 100,
            int survivors = 4,
            float duration = 10f,
            float contactDps = 20f,
            int guardedLane = 1)
        {
            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("holdout_playmode", "Barricada PlayMode", hp, survivors, duration, contactDps, 2f, 1);

            var go = new GameObject("TestBarricade");
            _spawnedObjects.Add(go);
            go.transform.position = position;

            var barricade = go.AddComponent<Barricade>();
            barricade.Initialize(def, squadLane: guardedLane);
            return barricade;
        }

        private EnemyController CreateEnemy(Vector3 position, int laneIndex, int initialHealth = 100, float contactDps = 20f)
        {
            var enemyGo = new GameObject($"TestEnemy_Lane{laneIndex}");
            _spawnedObjects.Add(enemyGo);
            enemyGo.transform.position = position;
            enemyGo.layer = CollisionLayers.EnemyLayer;

            var col = enemyGo.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            col.height = 2f;

            var rb = enemyGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            enemyGo.AddComponent<HealthComponent>();
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(laneIndex, initialHealth, moveSpeed: 0f, onDeath: null);
            enemy.ContactDPS = contactDps;
            return enemy;
        }

        [UnityTest]
        public IEnumerator Zombie_PhysicalTriggerWithBarricade_RegistersContactAndDealsContinuousDamageInUnguardedLane()
        {
            var barricade = CreateBarricade(new Vector3(0f, 0f, 5f), hp: 100, survivors: 4, duration: 10f, contactDps: 30f, guardedLane: 1);
            var enemy = CreateEnemy(new Vector3(0f, 0f, 5f), laneIndex: 0, initialHealth: 50, contactDps: 30f);

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, barricade.ContactEnemies.Count, "Zumbi na mesma posicao da Barricada deve registrar contato fisico via trigger.");

            int initialHp = barricade.CurrentHp;
            yield return new WaitForSeconds(0.4f);

            Assert.Less(barricade.CurrentHp, initialHp, "Barricada em lane desguarnecida deve sofrer dano continuo por segundo dos zumbis.");
            Assert.IsTrue(barricade.IsActive);
        }

        [UnityTest]
        public IEnumerator Barricade_TroopMovingToGuardedLane_CeasesDamageFromZombiesInThatLane()
        {
            var barricade = CreateBarricade(new Vector3(0f, 0f, 5f), hp: 100, survivors: 4, duration: 10f, contactDps: 30f, guardedLane: 1);
            var enemy = CreateEnemy(new Vector3(0f, 0f, 5f), laneIndex: 0, initialHealth: 50, contactDps: 30f);

            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.2f);

            int hpAfterUnguarded = barricade.CurrentHp;
            Assert.Less(hpAfterUnguarded, 100, "Zumbi em lane 0 desguarnecida deve aplicar dano.");

            // Tropa move para lane 0 (guarnecendo-a)
            barricade.SetGuardedLane(0);

            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(hpAfterUnguarded, barricade.CurrentHp, "Dano a barricada deve cessar imediatamente quando a tropa guarnece a lane.");
        }

        [UnityTest]
        public IEnumerator HoldoutController_CompleteDefenseCycle_PausesScrollerAndRescuesSurvivorsOnVictory()
        {
            var eventBus = new TestEventBus();
            var squad = new TestSquad(initialCount: 8);

            var scrollerGo = new GameObject("TestScroller");
            _spawnedObjects.Add(scrollerGo);
            var scroller = scrollerGo.AddComponent<TrackScroller>();

            var barricade = CreateBarricade(new Vector3(0f, 0f, 5f), hp: 100, survivors: 5, duration: 0.3f, contactDps: 10f, guardedLane: 1);

            var controllerGo = new GameObject("TestHoldoutController");
            _spawnedObjects.Add(controllerGo);
            var controller = controllerGo.AddComponent<HoldoutController>();

            controller.Initialize(barricade.Definition, barricade, scroller, squad, laneMover: null, eventBus: eventBus);

            Assert.IsFalse(scroller.IsPaused);
            controller.StartHoldout();

            Assert.IsTrue(scroller.IsPaused, "Inicio do Holdout DEVE pausar o avanco longitudinal da pista (TrackScroller.IsPaused).");
            Assert.IsTrue(controller.IsActive);

            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(controller.IsCompleted, "Holdout deve ser concluido apos esgotar a duracao com HP > 0.");
            Assert.IsTrue(controller.State.IsVictory, "Conclusao com sucesso resulta em Victory.");
            Assert.IsFalse(scroller.IsPaused, "Apos a vitoria do trecho, a pista DEVE voltar a rolar.");
            Assert.AreEqual(13, squad.SquadCount, "Todos os 5 sobreviventes protegidos devem ser somados a tropa.");
            Assert.AreEqual(1, eventBus.StartedCount, "HoldoutStartedEvent deve ser publicado.");
            Assert.AreEqual(1, eventBus.EndedCount, "HoldoutEndedEvent deve ser publicado.");
        }

        [UnityTest]
        public IEnumerator Zombie_ExitingTrigger_UnregistersFromContact()
        {
            var barricade = CreateBarricade(new Vector3(0f, 0f, 5f), hp: 100, survivors: 4, duration: 10f, contactDps: 20f, guardedLane: 1);
            var enemy = CreateEnemy(new Vector3(0f, 0f, 5f), laneIndex: 0, initialHealth: 50, contactDps: 20f);

            yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, barricade.ContactEnemies.Count);

            // Move o zumbi para longe da barricada
            enemy.transform.position = new Vector3(0f, 0f, 50f);

            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, barricade.ContactEnemies.Count, "Zumbi que sai do colisor trigger da Barricada deve ser desregistrado.");
        }

        private sealed class TestSquad : ISquad
        {
            public int SquadCount { get; private set; }
            public TestSquad(int initialCount) => SquadCount = initialCount;
            public bool Add(int amount) { SquadCount += Mathf.Max(0, amount); return true; }
            public bool Remove(int amount) { SquadCount = Mathf.Max(0, SquadCount - amount); return true; }
            public bool Multiply(int factor) { SquadCount = Mathf.Max(0, SquadCount * factor); return true; }
            public bool Divide(int divisor) { SquadCount = divisor > 0 ? Mathf.Max(0, SquadCount / divisor) : SquadCount; return true; }
            public bool SetCount(int newCount) { SquadCount = Mathf.Max(0, newCount); return true; }
        }

        private sealed class TestEventBus : IEventBus
        {
            public int StartedCount { get; private set; }
            public int EndedCount { get; private set; }

            public void Publish<T>(T eventData)
            {
                if (eventData is HoldoutStartedEvent)
                {
                    StartedCount++;
                }
                else if (eventData is HoldoutEndedEvent)
                {
                    EndedCount++;
                }
            }

            public System.IDisposable Subscribe<T>(System.Action<T> handler) => null;
        }
    }
}
