using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Policies;
using Game.Core.Events;
using Game.Core.State;
using Game.Data;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class GeneralAbilityGameplayTests
    {
        private List<GameObject> _createdObjects;
        private EventBus _eventBus;

        private sealed class SpyAbilityEffect : IAbilityEffect
        {
            public int ExecuteCount { get; private set; }
            public AbilityExecutionContext LastContext { get; private set; }
            public string Description => "Spy Ability Effect";

            public void Execute(AbilityExecutionContext context)
            {
                ExecuteCount++;
                LastContext = context;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<GameObject>();
            _eventBus = new EventBus();
        }

        [TearDown]
        public void TearDown()
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

        private GameObject Track(GameObject go)
        {
            _createdObjects.Add(go);
            return go;
        }

        private GeneralAbilityDefinition CreateDefinition(string id, int chargeKills, IAbilityEffect effect)
        {
            var def = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            def.SetData(id, id, "Test Description", chargeKills, effect);
            return def;
        }

        [Test]
        public void CicloDeCarga_AbatesNormaisAumentamContador_AbatesPorHabilidadeSaoIgnorados()
        {
            var effect = new SpyAbilityEffect();
            var def = CreateDefinition("grenade", 25, effect);
            var go = Track(new GameObject("General"));
            var controller = go.AddComponent<GeneralAbilityController>();
            controller.Initialize(def, _eventBus);

            for (int i = 0; i < 10; i++)
            {
                _eventBus.Publish(new EnemyKilledEvent("walker", 0, null, byAbility: false));
            }

            Assert.AreEqual(10, controller.CurrentKills);
            Assert.AreEqual(0, controller.CurrentCharges);

            for (int i = 0; i < 15; i++)
            {
                _eventBus.Publish(new EnemyKilledEvent("walker", 0, null, byAbility: true));
            }

            Assert.AreEqual(10, controller.CurrentKills);
            Assert.AreEqual(0, controller.CurrentCharges);
        }

        [Test]
        public void DisparoAutomatico_AoAtingir25AbatesComAlvos_DisparaEEsgotaCarga()
        {
            var effect = new SpyAbilityEffect();
            var def = CreateDefinition("grenade", 25, effect);
            var go = Track(new GameObject("General"));
            var controller = go.AddComponent<GeneralAbilityController>();
            controller.Initialize(def, _eventBus, new AutoHeroAbilityTriggerPolicy());
            controller.SetTargetsDetector(() => true);

            AbilityUsedEvent lastUsedEvent = default;
            bool usedReceived = false;
            using var usedSub = _eventBus.Subscribe<AbilityUsedEvent>(evt =>
            {
                lastUsedEvent = evt;
                usedReceived = true;
            });

            for (int i = 0; i < 25; i++)
            {
                _eventBus.Publish(new EnemyKilledEvent("walker", 0, null, byAbility: false));
            }

            Assert.AreEqual(1, effect.ExecuteCount);
            Assert.IsTrue(usedReceived);
            Assert.AreEqual("grenade", lastUsedEvent.AbilityId);
            Assert.AreEqual(0, lastUsedEvent.RemainingCharges);
            Assert.AreEqual(0, controller.CurrentCharges);
            Assert.AreEqual(0, controller.CurrentKills);
        }

        [Test]
        public void DisparoManual_AcumulaCarga_NaoDisparaAutomatico_ConsomeApenasAoAcionarManualmente()
        {
            var effect = new SpyAbilityEffect();
            var def = CreateDefinition("grenade", 25, effect);
            var go = Track(new GameObject("General"));
            var controller = go.AddComponent<GeneralAbilityController>();
            controller.Initialize(def, _eventBus, new ManualHeroAbilityTriggerPolicy());
            controller.SetTargetsDetector(() => true);

            for (int i = 0; i < 25; i++)
            {
                _eventBus.Publish(new EnemyKilledEvent("walker", 0, null, byAbility: false));
            }

            Assert.AreEqual(0, effect.ExecuteCount);
            Assert.AreEqual(1, controller.CurrentCharges);

            bool success = controller.TriggerAbility(manual: true);

            Assert.IsTrue(success);
            Assert.AreEqual(1, effect.ExecuteCount);
            Assert.AreEqual(0, controller.CurrentCharges);
        }

        [Test]
        public void SetTriggerPolicy_PermiteAlternarEntreAutoEManualEmTempoDeExecucao()
        {
            var effect = new SpyAbilityEffect();
            var def = CreateDefinition("grenade", 25, effect);
            var go = Track(new GameObject("General"));
            var controller = go.AddComponent<GeneralAbilityController>();
            controller.Initialize(def, _eventBus, new ManualHeroAbilityTriggerPolicy());
            controller.SetTargetsDetector(() => true);

            for (int i = 0; i < 25; i++)
            {
                _eventBus.Publish(new EnemyKilledEvent("walker", 0, null, byAbility: false));
            }

            Assert.AreEqual(0, effect.ExecuteCount);
            Assert.AreEqual(1, controller.CurrentCharges);

            controller.SetTriggerPolicy(new AutoHeroAbilityTriggerPolicy());
            Assert.AreEqual(HeroAbilityTriggerMode.Auto, controller.TriggerPolicy.Mode);

            controller.TriggerAbility(manual: false);
            Assert.AreEqual(1, effect.ExecuteCount);
            Assert.AreEqual(0, controller.CurrentCharges);
        }

        [Test]
        public void EnemyController_AoMorrerPorDanoNormal_PublicaEnemyKilledEventComByAbilityFalse()
        {
            var enemyGo = Track(new GameObject("Enemy"));
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(laneIndex: 1, maxHealth: 50, moveSpeed: 2f, onDeath: null, eventBus: _eventBus);

            EnemyKilledEvent capturedEvent = default;
            bool eventFired = false;
            using var sub = _eventBus.Subscribe<EnemyKilledEvent>(evt =>
            {
                capturedEvent = evt;
                eventFired = true;
            });

            enemy.ReceiveHit(new DamageInfo(50, DamageType.Physical, "soldier_bullet"), null);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, capturedEvent.Lane);
            Assert.IsFalse(capturedEvent.ByAbility);
            Assert.IsFalse(enemy.IsAlive);
        }

        [Test]
        public void EnemyController_AoMorrerPorDanoDeHabilidade_PublicaEnemyKilledEventComByAbilityTrue()
        {
            var enemyGo = Track(new GameObject("Enemy"));
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(laneIndex: 0, maxHealth: 50, moveSpeed: 2f, onDeath: null, eventBus: _eventBus);

            EnemyKilledEvent capturedEvent = default;
            bool eventFired = false;
            using var sub = _eventBus.Subscribe<EnemyKilledEvent>(evt =>
            {
                capturedEvent = evt;
                eventFired = true;
            });

            var effect = new SpyAbilityEffect();
            enemy.ReceiveHit(new DamageInfo(50, DamageType.Area, effect), null);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(0, capturedEvent.Lane);
            Assert.IsTrue(capturedEvent.ByAbility);
            Assert.IsFalse(enemy.IsAlive);
        }

        [Test]
        public void RunConfig_BonusAbilityCharges_InicializaComCargasExtras()
        {
            var effect = new SpyAbilityEffect();
            var def = CreateDefinition("grenade", 25, effect);
            var runConfig = new RunConfig { BonusAbilityCharges = 2 };

            var go = Track(new GameObject("General"));
            var controller = go.AddComponent<GeneralAbilityController>();
            controller.Initialize(def, _eventBus, runConfig: runConfig);

            Assert.AreEqual(2, controller.CurrentCharges);
            Assert.AreEqual(0, controller.CurrentKills);
        }
    }
}
