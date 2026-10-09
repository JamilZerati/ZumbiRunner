using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Effects;
using Game.Core.Abilities.Policies;
using Game.Core.Events;
using Game.Core.State;
using Game.Data;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class GeneralAbilityAimPlayModeTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private GeneralAbilityDefinition _definition;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1.0f;

            if (_definition != null)
            {
                Object.DestroyImmediate(_definition);
                _definition = null;
            }

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

        private class FakeAbilitySettings : IAbilitySettings
        {
            public HeroAbilityTriggerMode TriggerMode { get; set; }

            public FakeAbilitySettings(HeroAbilityTriggerMode mode)
            {
                TriggerMode = mode;
            }

            public HeroAbilityTriggerMode LoadTriggerMode() => TriggerMode;
            public void SaveTriggerMode(HeroAbilityTriggerMode mode) => TriggerMode = mode;
        }

        [UnityTest]
        public IEnumerator AimDrag_DesaceleraTempo_RestauraTempo_EAplicaDanoNoAlvoMirado()
        {
            var eventBus = new EventBus();

            var controllerGo = CreateGameObject("General");
            controllerGo.transform.position = Vector3.zero;
            var controller = controllerGo.AddComponent<GeneralAbilityController>();

            var indicatorGo = CreateGameObject("AimIndicator");
            var indicator = indicatorGo.AddComponent<AbilityAimIndicator>();
            indicator.Initialize();

            var hudGo = CreateGameObject("HUD");
            var hud = hudGo.AddComponent<GeneralAbilityHud>();
            hud.AimIndicator = indicator;

            _definition = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            var targeting = new AbilityTargetingConfig
            {
                Type = AbilityTargetingType.GroundTarget,
                MaxRange = 20f,
                Radius = 4f
            };
            _definition.SetData("grenade", "Granada", "Dano em área com mira", 1, new GrenadeAbilityEffect(), targeting);

            controller.Initialize(_definition, eventBus, new ManualHeroAbilityTriggerPolicy(), new RunConfig { BonusAbilityCharges = 1 });
            hud.Initialize(controller, eventBus, new FakeAbilitySettings(HeroAbilityTriggerMode.Manual));

            var targetEnemyGo = CreateGameObject("TargetEnemy");
            targetEnemyGo.transform.position = new Vector3(0f, 0f, 15f);
            var colTarget = targetEnemyGo.AddComponent<SphereCollider>();
            colTarget.radius = 0.5f;
            var targetEnemy = targetEnemyGo.AddComponent<EnemyController>();
            targetEnemy.Initialize(laneIndex: 0, maxHealth: 150, moveSpeed: 0f, onDeath: null, eventBus: eventBus);

            var outsideEnemyGo = CreateGameObject("OutsideEnemy");
            outsideEnemyGo.transform.position = new Vector3(0f, 0f, 2f);
            var colOutside = outsideEnemyGo.AddComponent<SphereCollider>();
            colOutside.radius = 0.5f;
            var outsideEnemy = outsideEnemyGo.AddComponent<EnemyController>();
            outsideEnemy.Initialize(laneIndex: 0, maxHealth: 150, moveSpeed: 0f, onDeath: null, eventBus: eventBus);

            yield return new WaitForFixedUpdate();

            var pointerDown = new PointerEventData(EventSystem.current) { position = new Vector2(100, 100) };
            hud.OnPointerDown(pointerDown);

            Assert.AreEqual(0.3f, Time.timeScale, 0.01f);
            Assert.IsTrue(indicator.IsVisible);

            var drag = new PointerEventData(EventSystem.current) { position = new Vector2(100, 250) };
            hud.OnDrag(drag);

            var pointerUp = new PointerEventData(EventSystem.current) { position = new Vector2(100, 250) };
            hud.OnPointerUp(pointerUp);

            Assert.AreEqual(1f, Time.timeScale, 0.01f);
            Assert.IsFalse(indicator.IsVisible);

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0, controller.CurrentCharges);
            Assert.IsFalse(targetEnemy.IsAlive);
            Assert.IsTrue(outsideEnemy.IsAlive);
        }

        [UnityTest]
        public IEnumerator AimDrag_SoltarNaZonaDeCancelamento_CancelaSemGastarCarga()
        {
            var eventBus = new EventBus();

            var controllerGo = CreateGameObject("General");
            controllerGo.transform.position = Vector3.zero;
            var controller = controllerGo.AddComponent<GeneralAbilityController>();

            var indicatorGo = CreateGameObject("AimIndicator");
            var indicator = indicatorGo.AddComponent<AbilityAimIndicator>();
            indicator.Initialize();

            var hudGo = CreateGameObject("HUD");
            var hud = hudGo.AddComponent<GeneralAbilityHud>();
            hud.AimIndicator = indicator;

            _definition = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            var targeting = new AbilityTargetingConfig
            {
                Type = AbilityTargetingType.GroundTarget,
                MaxRange = 20f,
                Radius = 4f
            };
            _definition.SetData("grenade", "Granada", "Dano em área com mira", 1, new GrenadeAbilityEffect(), targeting);

            controller.Initialize(_definition, eventBus, new ManualHeroAbilityTriggerPolicy(), new RunConfig { BonusAbilityCharges = 1 });
            hud.Initialize(controller, eventBus, new FakeAbilitySettings(HeroAbilityTriggerMode.Manual));

            var enemyGo = CreateGameObject("Enemy");
            enemyGo.transform.position = new Vector3(0f, 0f, 15f);
            var col = enemyGo.AddComponent<SphereCollider>();
            col.radius = 0.5f;
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(laneIndex: 0, maxHealth: 150, moveSpeed: 0f, onDeath: null, eventBus: eventBus);

            yield return new WaitForFixedUpdate();

            var pointerDown = new PointerEventData(EventSystem.current) { position = new Vector2(100, 100) };
            hud.OnPointerDown(pointerDown);

            Assert.AreEqual(0.3f, Time.timeScale, 0.01f);

            var dragCancel = new PointerEventData(EventSystem.current) { position = new Vector2(100, 120) };
            hud.OnDrag(dragCancel);

            var pointerUp = new PointerEventData(EventSystem.current) { position = new Vector2(100, 120) };
            hud.OnPointerUp(pointerUp);

            Assert.AreEqual(1f, Time.timeScale, 0.01f);
            Assert.IsFalse(indicator.IsVisible);

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, controller.CurrentCharges);
            Assert.IsTrue(enemy.IsAlive);
        }

        [UnityTest]
        public IEnumerator AimDrag_TimeoutDe3Segundos_ForcaODisparoNoPontoMirado()
        {
            var eventBus = new EventBus();

            var controllerGo = CreateGameObject("General");
            controllerGo.transform.position = Vector3.zero;
            var controller = controllerGo.AddComponent<GeneralAbilityController>();

            var indicatorGo = CreateGameObject("AimIndicator");
            var indicator = indicatorGo.AddComponent<AbilityAimIndicator>();
            indicator.Initialize();

            var hudGo = CreateGameObject("HUD");
            var hud = hudGo.AddComponent<GeneralAbilityHud>();
            hud.AimIndicator = indicator;

            _definition = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            var targeting = new AbilityTargetingConfig
            {
                Type = AbilityTargetingType.GroundTarget,
                MaxRange = 20f,
                Radius = 4f
            };
            _definition.SetData("grenade", "Granada", "Dano em área com mira", 1, new GrenadeAbilityEffect(), targeting);

            controller.Initialize(_definition, eventBus, new ManualHeroAbilityTriggerPolicy(), new RunConfig { BonusAbilityCharges = 1 });
            hud.Initialize(controller, eventBus, new FakeAbilitySettings(HeroAbilityTriggerMode.Manual));

            var targetEnemyGo = CreateGameObject("TargetEnemy");
            targetEnemyGo.transform.position = new Vector3(0f, 0f, 15f);
            var colTarget = targetEnemyGo.AddComponent<SphereCollider>();
            colTarget.radius = 0.5f;
            var targetEnemy = targetEnemyGo.AddComponent<EnemyController>();
            targetEnemy.Initialize(laneIndex: 0, maxHealth: 150, moveSpeed: 0f, onDeath: null, eventBus: eventBus);

            yield return new WaitForFixedUpdate();

            var pointerDown = new PointerEventData(EventSystem.current) { position = new Vector2(100, 100) };
            hud.OnPointerDown(pointerDown);

            var drag = new PointerEventData(EventSystem.current) { position = new Vector2(100, 250) };
            hud.OnDrag(drag);

            // Simula passagem de mais de 3 segundos na mira
            var field = typeof(GeneralAbilityHud).GetField("_aimStartTime", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(hud, Time.unscaledTime - 4f);

            var updateMethod = typeof(GeneralAbilityHud).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            updateMethod.Invoke(hud, null);

            Assert.AreEqual(1f, Time.timeScale, 0.01f);
            Assert.IsFalse(indicator.IsVisible);

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0, controller.CurrentCharges);
            Assert.IsFalse(targetEnemy.IsAlive);
        }
    }
}
