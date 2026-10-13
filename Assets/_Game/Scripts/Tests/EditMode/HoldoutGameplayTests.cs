using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Holdout;
using Game.Core.State;
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

        private class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }

            public FakeSquad(int initialCount)
            {
                SquadCount = initialCount;
            }

            public bool Add(int amount)
            {
                if (amount <= 0) return false;
                SquadCount += amount;
                return true;
            }

            public bool Remove(int amount)
            {
                if (amount <= 0) return false;
                SquadCount = Mathf.Max(0, SquadCount - amount);
                return true;
            }

            public bool Multiply(int factor)
            {
                if (factor <= 0) return false;
                SquadCount *= factor;
                return true;
            }

            public bool Divide(int divisor)
            {
                if (divisor <= 0) return false;
                SquadCount /= divisor;
                return true;
            }

            public bool SetCount(int newCount)
            {
                SquadCount = Mathf.Max(0, newCount);
                return true;
            }
        }

        [Test]
        public void HoldoutController_Victory_PausesAndResumesTrack_RescuesSurvivors_MaintainsStars()
        {
            var eventBus = new EventBus();
            HoldoutStartedEvent? startedEvt = null;
            HoldoutEndedEvent? endedEvt = null;
            SurvivorsRescuedEvent? rescuedEvt = null;
            RunEndedEvent? runEndedEvt = null;

            eventBus.Subscribe<HoldoutStartedEvent>(e => startedEvt = e);
            eventBus.Subscribe<HoldoutEndedEvent>(e => endedEvt = e);
            eventBus.Subscribe<SurvivorsRescuedEvent>(e => rescuedEvt = e);
            eventBus.Subscribe<RunEndedEvent>(e => runEndedEvt = e);

            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("holdout_test", "Barricada Vitoria", hp: 100, survivors: 4, duration: 8f, contactDps: 10f, waveInterval: 2f, zombiesPerLane: 1);

            var scrollerGo = CreateGameObject("TrackScroller");
            var scroller = scrollerGo.AddComponent<TrackScroller>();

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();

            var controllerGo = CreateGameObject("HoldoutController");
            var controller = controllerGo.AddComponent<HoldoutController>();

            var squad = new FakeSquad(10);

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "test_holdout_level", InitialSquad = 10 };
            director.Initialize(config, null, eventBus);

            controller.Initialize(def, barricade, scroller, squad, laneMover: null, eventBus: eventBus);

            Assert.IsFalse(scroller.IsPaused);
            Assert.IsFalse(controller.IsActive);

            controller.StartHoldout();

            Assert.IsTrue(controller.IsActive);
            Assert.IsTrue(scroller.IsPaused);
            Assert.IsNotNull(startedEvt);
            Assert.AreEqual("holdout_test", startedEvt.Value.HoldoutId);
            Assert.AreEqual(100, startedEvt.Value.MaxHp);

            // Dano parcial de 50%
            barricade.TakeDamage(50);
            Assert.AreEqual(50, barricade.CurrentHp);

            // Simula passagem do tempo ate a vitoria
            controller.Tick(8f);

            Assert.IsTrue(controller.IsCompleted);
            Assert.IsFalse(controller.IsActive);
            Assert.IsFalse(scroller.IsPaused);
            Assert.IsTrue(controller.State.IsVictory);

            // Resgate proporcional arredondado para cima: 4 * 0.5 = 2 sobreviventes
            Assert.AreEqual(2, controller.State.SurvivorsSaved);
            Assert.AreEqual(12, squad.SquadCount);

            Assert.IsNotNull(endedEvt);
            Assert.IsTrue(endedEvt.Value.BarricadeHeld);
            Assert.AreEqual(2, endedEvt.Value.SurvivorsSaved);
            Assert.AreEqual(50, endedEvt.Value.RemainingHp);

            Assert.IsNotNull(rescuedEvt);
            Assert.AreEqual(2, rescuedEvt.Value.Count);

            Assert.AreEqual(1, director.HoldoutsAttempted);
            Assert.AreEqual(1, director.HoldoutsHeld);

            director.EndRun(victory: true);

            Assert.IsNotNull(runEndedEvt);
            Assert.IsTrue(runEndedEvt.Value.Result.Victory);
            Assert.AreEqual(3, runEndedEvt.Value.Result.Stars);
            Assert.AreEqual(1, runEndedEvt.Value.Result.HoldoutsAttempted);
            Assert.AreEqual(1, runEndedEvt.Value.Result.HoldoutsHeld);
            Assert.AreEqual(2, runEndedEvt.Value.Result.Rescued);
        }

        [Test]
        public void HoldoutController_Collapse_PausesAndResumesTrack_LosesSurvivors_CapsStarsAtTwo()
        {
            var eventBus = new EventBus();
            HoldoutStartedEvent? startedEvt = null;
            HoldoutEndedEvent? endedEvt = null;
            SurvivorsLostEvent? lostEvt = null;
            RunEndedEvent? runEndedEvt = null;

            eventBus.Subscribe<HoldoutStartedEvent>(e => startedEvt = e);
            eventBus.Subscribe<HoldoutEndedEvent>(e => endedEvt = e);
            eventBus.Subscribe<SurvivorsLostEvent>(e => lostEvt = e);
            eventBus.Subscribe<RunEndedEvent>(e => runEndedEvt = e);

            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("holdout_collapse", "Barricada Colapso", hp: 80, survivors: 5, duration: 10f, contactDps: 10f, waveInterval: 2f, zombiesPerLane: 1);

            var scrollerGo = CreateGameObject("TrackScroller");
            var scroller = scrollerGo.AddComponent<TrackScroller>();

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();

            var controllerGo = CreateGameObject("HoldoutController");
            var controller = controllerGo.AddComponent<HoldoutController>();

            var squad = new FakeSquad(10);

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "test_collapse_level", InitialSquad = 10 };
            director.Initialize(config, null, eventBus);

            controller.Initialize(def, barricade, scroller, squad, laneMover: null, eventBus: eventBus);

            controller.StartHoldout();
            Assert.IsTrue(scroller.IsPaused);

            // Dano letal na barricada
            barricade.TakeDamage(80);

            Assert.IsTrue(controller.IsCompleted);
            Assert.IsFalse(controller.IsActive);
            Assert.IsFalse(scroller.IsPaused);
            Assert.IsTrue(controller.State.IsBarricadeDestroyed);

            // Sobreviventes zerados e tropa mantem contagem intacta (sem soma)
            Assert.AreEqual(0, controller.State.SurvivorsSaved);
            Assert.AreEqual(10, squad.SquadCount);

            Assert.IsNotNull(endedEvt);
            Assert.IsFalse(endedEvt.Value.BarricadeHeld);
            Assert.AreEqual(0, endedEvt.Value.SurvivorsSaved);
            Assert.AreEqual(0, endedEvt.Value.RemainingHp);

            Assert.IsNotNull(lostEvt);
            Assert.AreEqual(5, lostEvt.Value.Count);

            Assert.AreEqual(1, director.HoldoutsAttempted);
            Assert.AreEqual(0, director.HoldoutsHeld);

            // General conclui a fase com vida, mas perde 3a estrela devido a barricada perdida
            director.EndRun(victory: true);

            Assert.IsNotNull(runEndedEvt);
            Assert.IsTrue(runEndedEvt.Value.Result.Victory);
            Assert.AreEqual(2, runEndedEvt.Value.Result.Stars);
            Assert.AreEqual(1, runEndedEvt.Value.Result.HoldoutsAttempted);
            Assert.AreEqual(0, runEndedEvt.Value.Result.HoldoutsHeld);
        }

        [Test]
        public void HoldoutController_LaneChangedEvent_UpdatesBarricadeGuardedLane()
        {
            var eventBus = new EventBus();
            var def = CreateScriptableObject<HoldoutDefinition>();
            def.SetData("holdout_lane", "Barricada Lane", hp: 100, survivors: 3, duration: 10f, contactDps: 10f, waveInterval: 2f, zombiesPerLane: 1);

            var barricadeGo = CreateGameObject("Barricade");
            var barricade = barricadeGo.AddComponent<Barricade>();

            var controllerGo = CreateGameObject("HoldoutController");
            var controller = controllerGo.AddComponent<HoldoutController>();

            controller.Initialize(def, barricade, scroller: null, squad: null, laneMover: null, eventBus: eventBus);
            controller.StartHoldout();

            Assert.AreEqual(1, barricade.GuardedLane);

            eventBus.Publish(new LaneChangedEvent(previousLane: 1, newLane: 0));
            Assert.AreEqual(0, barricade.GuardedLane);

            eventBus.Publish(new LaneChangedEvent(previousLane: 0, newLane: 2));
            Assert.AreEqual(2, barricade.GuardedLane);
        }

        [Test]
        public void LevelDirector_SpawnHoldoutSegment_TriggersOnHoldoutSpawned()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_holdout_test";
            levelDef.TotalDistance = 200f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Holdout,
                    StartDistance = 80f,
                    Length = 30f,
                    Holdout = new HoldoutSegmentSpec { HoldoutId = "holdout_standard" }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "level_holdout_test" };
            director.Initialize(config, levelDef, eventBus);

            SegmentDefinition spawnedSegment = null;
            director.OnHoldoutSpawned += seg => spawnedSegment = seg;

            director.Tick(79f);
            Assert.AreEqual(0, director.SpawnedHoldoutCount);
            Assert.IsNull(spawnedSegment);

            director.Tick(80f);
            Assert.AreEqual(1, director.SpawnedHoldoutCount);
            Assert.IsNotNull(spawnedSegment);
            Assert.AreEqual(SegmentType.Holdout, spawnedSegment.SegmentType);
            Assert.AreEqual("holdout_standard", spawnedSegment.Holdout.HoldoutId);
        }
    }
}
