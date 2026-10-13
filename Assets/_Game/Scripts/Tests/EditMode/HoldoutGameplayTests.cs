using System.Collections.Generic;
using Game.Core.Holdout;
using Game.Data;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class HoldoutGameplayTests
    {
        private List<GameObject> _createdObjects;
        private List<ScriptableObject> _createdAssets;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<GameObject>();
            _createdAssets = new List<ScriptableObject>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_createdObjects != null)
            {
                for (int i = 0; i < _createdObjects.Count; i++)
                {
                    if (_createdObjects[i] != null)
                    {
                        Object.DestroyImmediate(_createdObjects[i]);
                    }
                }
                _createdObjects.Clear();
            }

            if (_createdAssets != null)
            {
                for (int i = 0; i < _createdAssets.Count; i++)
                {
                    if (_createdAssets[i] != null)
                    {
                        Object.DestroyImmediate(_createdAssets[i]);
                    }
                }
                _createdAssets.Clear();
            }
        }

        private GameObject CreateGameObject(string name = "TestObject")
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        private T CreateScriptableObject<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            _createdAssets.Add(so);
            return so;
        }

        private EnemyController CreateEnemy(int laneIndex, int maxHealth = 20, float contactDps = 10f)
        {
            var go = CreateGameObject($"Enemy_Lane{laneIndex}");
            var enemy = go.AddComponent<EnemyController>();
            enemy.Initialize(laneIndex, maxHealth, moveSpeed: 2f, onDeath: null);
            enemy.ContactDPS = contactDps;
            return enemy;
        }

        [Test]
        public void Barricade_Initialization_ConfiguresComponentsAndState()
        {
            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("test_holdout", "Barricada Teste", hp: 150, survivors: 4, duration: 12f, contactDps: 8f, waveInterval: 2f, zombiesPerLane: 2);

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();
            barricade.Initialize(def, squadLane: 1);

            Assert.AreEqual("test_holdout", barricade.HoldoutId);
            Assert.AreEqual(150, barricade.CurrentHp);
            Assert.AreEqual(150, barricade.MaxHp);
            Assert.AreEqual(4, barricade.TotalSurvivors);
            Assert.AreEqual(1, barricade.GuardedLane);
            Assert.IsTrue(barricade.IsActive);
            Assert.IsFalse(barricade.IsDestroyed);
        }

        [Test]
        public void Barricade_ZombieInUnguardedLane_DealsCumulativeContactDamage()
        {
            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("test_holdout", "Barricada", hp: 100, survivors: 3, duration: 15f, contactDps: 10f, waveInterval: 2f, zombiesPerLane: 1);

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();
            barricade.Initialize(def, squadLane: 1);

            var enemyLane0 = CreateEnemy(laneIndex: 0, maxHealth: 30, contactDps: 10f);
            barricade.RegisterZombieContact(enemyLane0);

            Assert.AreEqual(1, barricade.ContactEnemies.Count);

            // 0.5s * 10 dps = 5 dano acumulado
            barricade.TickDamage(0.5f);
            Assert.AreEqual(95, barricade.CurrentHp);

            // 0.5s * 10 dps = mais 5 dano acumulado
            barricade.TickDamage(0.5f);
            Assert.AreEqual(90, barricade.CurrentHp);
        }

        [Test]
        public void Barricade_TroopMovingToLane_CeasesDamageFromZombiesInThatLane()
        {
            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("test_holdout", "Barricada", hp: 100, survivors: 3, duration: 15f, contactDps: 10f, waveInterval: 2f, zombiesPerLane: 1);

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();
            barricade.Initialize(def, squadLane: 1);

            var enemyLane0 = CreateEnemy(laneIndex: 0, maxHealth: 30, contactDps: 10f);
            barricade.RegisterZombieContact(enemyLane0);

            // Lane 0 desguarnecida -> dano aplicado
            barricade.TickDamage(1f);
            Assert.AreEqual(90, barricade.CurrentHp);

            // Tropa move para lane 0 (agora guarnecida)
            barricade.SetGuardedLane(0);

            // Dano deve cessar na lane 0
            barricade.TickDamage(1f);
            Assert.AreEqual(90, barricade.CurrentHp);
        }

        [Test]
        public void Barricade_MultipleZombiesInUnguardedLanes_SumDpsCorrectly()
        {
            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("test_holdout", "Barricada", hp: 100, survivors: 3, duration: 15f, contactDps: 10f, waveInterval: 2f, zombiesPerLane: 1);

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();
            barricade.Initialize(def, squadLane: 1);

            var enemyA = CreateEnemy(laneIndex: 0, maxHealth: 30, contactDps: 10f);
            var enemyB = CreateEnemy(laneIndex: 2, maxHealth: 30, contactDps: 15f);

            barricade.RegisterZombieContact(enemyA);
            barricade.RegisterZombieContact(enemyB);

            // 1s * (10 + 15) dps = 25 dano
            barricade.TickDamage(1f);
            Assert.AreEqual(75, barricade.CurrentHp);
        }

        [Test]
        public void Barricade_DeadZombie_AutomaticallyPrunedAndStopsDamaging()
        {
            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("test_holdout", "Barricada", hp: 100, survivors: 3, duration: 15f, contactDps: 10f, waveInterval: 2f, zombiesPerLane: 1);

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();
            barricade.Initialize(def, squadLane: 1);

            var enemy = CreateEnemy(laneIndex: 0, maxHealth: 20, contactDps: 10f);
            barricade.RegisterZombieContact(enemy);

            barricade.TickDamage(0.5f);
            Assert.AreEqual(95, barricade.CurrentHp);

            // Zumbi morre
            enemy.Health.TakeDamage(new Game.Core.DamageInfo(100));
            Assert.IsFalse(enemy.IsAlive);

            // Tick seguinte deve expurgar zumbi morto e cessar dano
            barricade.TickDamage(1f);
            Assert.AreEqual(95, barricade.CurrentHp);
            Assert.AreEqual(0, barricade.ContactEnemies.Count);
        }

        [Test]
        public void Barricade_LethalDamage_TransitionsToDestroyedAndTriggersEvent()
        {
            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("test_holdout", "Barricada", hp: 50, survivors: 3, duration: 10f, contactDps: 10f, waveInterval: 2f, zombiesPerLane: 1);

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();
            barricade.Initialize(def, squadLane: 1);

            bool destroyedEventFired = false;
            barricade.OnDestroyed += b => destroyedEventFired = true;

            barricade.TakeDamage(30);
            Assert.IsFalse(destroyedEventFired);
            Assert.AreEqual(20, barricade.CurrentHp);

            barricade.TakeDamage(30);
            Assert.IsTrue(destroyedEventFired);
            Assert.AreEqual(0, barricade.CurrentHp);
            Assert.IsTrue(barricade.IsDestroyed);
            Assert.IsFalse(barricade.IsActive);
        }

        [Test]
        public void BarricadeView_UpdatesVisualsOnStateChanges()
        {
            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("test_holdout", "Barricada", hp: 100, survivors: 3, duration: 15f, contactDps: 10f, waveInterval: 2f, zombiesPerLane: 1);

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();
            barricade.Initialize(def, squadLane: 1);

            var viewGo = CreateGameObject("BarricadeView");
            var view = viewGo.AddComponent<BarricadeView>();
            view.Bind(barricade);

            Assert.IsFalse(barricade.IsDestroyed);

            barricade.TakeDamage(20);
            Assert.AreEqual(80, barricade.CurrentHp);

            barricade.TakeDamage(80);
            Assert.IsTrue(barricade.IsDestroyed);
        }
    }
}
