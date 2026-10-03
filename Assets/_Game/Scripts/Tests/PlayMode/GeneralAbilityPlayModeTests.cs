using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Effects;
using Game.Core.Events;
using Game.Data;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class GeneralAbilityPlayModeTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null)
                {
                    Object.Destroy(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        [UnityTest]
        public IEnumerator ApplyAreaDamage_AtingeInimigosDentroDoRaioDe4Metros_EIgnoraInimigosForaDoRaio()
        {
            var controllerGo = CreateGameObject("GeneralAbilityController");
            var controller = controllerGo.AddComponent<GeneralAbilityController>();

            var enemyInGo = CreateGameObject("EnemyInside");
            enemyInGo.transform.position = new Vector3(0f, 0f, 2f);
            var colIn = enemyInGo.AddComponent<SphereCollider>();
            colIn.radius = 0.5f;
            var enemyIn = enemyInGo.AddComponent<EnemyController>();
            enemyIn.Initialize(laneIndex: 0, maxHealth: 150, moveSpeed: 0f, onDeath: null);

            var enemyOutGo = CreateGameObject("EnemyOutside");
            enemyOutGo.transform.position = new Vector3(0f, 0f, 10f);
            var colOut = enemyOutGo.AddComponent<SphereCollider>();
            colOut.radius = 0.5f;
            var enemyOut = enemyOutGo.AddComponent<EnemyController>();
            enemyOut.Initialize(laneIndex: 0, maxHealth: 150, moveSpeed: 0f, onDeath: null);

            yield return new WaitForFixedUpdate();

            int damageDealt = controller.ApplyAreaDamage(new AbilityPosition(0f, 0f, 0f), radius: 4f, damage: 150);

            Assert.AreEqual(150, damageDealt);
            Assert.IsFalse(enemyIn.IsAlive);
            Assert.AreEqual(0, enemyIn.CurrentHealth);
            Assert.IsTrue(enemyOut.IsAlive);
            Assert.AreEqual(150, enemyOut.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator TriggerAbility_ComGrenadeEffect_ExecutaDanoEmAreaEFiltraCamadaEnemy()
        {
            var eventBus = new EventBus();

            var controllerGo = CreateGameObject("General");
            controllerGo.transform.position = Vector3.zero;
            var controller = controllerGo.AddComponent<GeneralAbilityController>();

            var def = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            def.SetData("grenade", "Granada", "Explosão de área", 25, new GrenadeAbilityEffect());

            controller.Initialize(def, eventBus);
            controller.SetTargetsDetector(() => true);

            var enemyGo = CreateGameObject("TargetEnemy");
            enemyGo.transform.position = new Vector3(0f, 0f, 2f);
            var enemyCol = enemyGo.AddComponent<SphereCollider>();
            enemyCol.radius = 0.5f;
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(laneIndex: 0, maxHealth: 150, moveSpeed: 0f, onDeath: null, eventBus: eventBus);

            var killedEvents = new List<EnemyKilledEvent>();
            using var sub = eventBus.Subscribe<EnemyKilledEvent>(evt =>
            {
                killedEvents.Add(evt);
            });

            yield return new WaitForFixedUpdate();

            for (int i = 0; i < 25; i++)
            {
                eventBus.Publish(new EnemyKilledEvent("walker", 0, null, byAbility: false));
            }

            yield return null;

            Assert.IsTrue(killedEvents.Exists(e => e.ByAbility), "Deve haver evento EnemyKilledEvent com ByAbility == true após explosão da granada.");
            Assert.IsFalse(enemy.IsAlive);
        }
    }
}
