using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Rescue;
using Game.Data;
using Game.Gameplay;
using Game.Infrastructure;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class CageGameplayTests
    {
        private List<Object> _trackedObjects;
        private EventBus _eventBus;

        [SetUp]
        public void SetUp()
        {
            _trackedObjects = new List<Object>();
            _eventBus = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _trackedObjects.Count - 1; i >= 0; i--)
            {
                if (_trackedObjects[i] != null)
                {
                    Object.DestroyImmediate(_trackedObjects[i]);
                }
            }
            _trackedObjects.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            if (obj != null)
            {
                _trackedObjects.Add(obj);
            }
            return obj;
        }

        private CageDefinition CreateDefinition(string id = "cage_test", int hp = 60, int survivors = 3, string lostArchetype = "walker")
        {
            var def = Track(ScriptableObject.CreateInstance<CageDefinition>());
            def.SetData(id, "Test Cage", hp, survivors, lostArchetype);
            return def;
        }

        private Cage CreateCage(CageDefinition def = null, int lane = 0, ISquad squad = null, Vector3? position = null)
        {
            var go = Track(new GameObject("TestCage"));
            if (position.HasValue)
            {
                go.transform.position = position.Value;
            }
            var cage = go.AddComponent<Cage>();
            cage.Initialize(def ?? CreateDefinition(), lane, squad, _eventBus);
            return cage;
        }

        private class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }
            public bool IsAlive => SquadCount > 0;

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
                if (amount <= 0 || SquadCount <= 0) return false;
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
                if (newCount < 0) return false;
                SquadCount = newCount;
                return true;
            }
        }

        [Test]
        public void Cage_Initialization_ConfiguresLayerAndCollider()
        {
            var cage = CreateCage();

            Assert.AreEqual(CollisionLayers.PickupLayer, cage.gameObject.layer);
            Assert.IsNotNull(cage.CageCollider);
            Assert.IsTrue(cage.CageCollider.enabled);
            Assert.IsTrue(cage.CageCollider.isTrigger);
            Assert.IsTrue(cage.IsAlive);
            Assert.IsFalse(cage.IsOpen);
            Assert.IsFalse(cage.IsLost);
            Assert.AreEqual(60, cage.CurrentHealth);
            Assert.AreEqual(60, cage.MaxHealth);
            Assert.AreEqual(3, cage.SurvivorCount);
        }

        [Test]
        public void Cage_TakeDamage_ReducesHealth_StaysAliveWhenAboveZero()
        {
            var cage = CreateCage(CreateDefinition(hp: 80));

            cage.TakeDamage(new DamageInfo(30));

            Assert.AreEqual(50, cage.CurrentHealth);
            Assert.IsTrue(cage.IsAlive);
            Assert.IsFalse(cage.IsOpen);
            Assert.IsTrue(cage.CageCollider.enabled);
        }

        [Test]
        public void Cage_TakeDamage_LethalDamage_OpensCage_AddsSurvivorsToSquad_PublishesRescuedEvent_DisablesCollider()
        {
            var squad = new FakeSquad(5);
            var cage = CreateCage(CreateDefinition(hp: 50, survivors: 4), lane: 1, squad: squad);

            SurvivorsRescuedEvent receivedEvent = default;
            bool eventFired = false;
            _eventBus.Subscribe<SurvivorsRescuedEvent>(evt =>
            {
                receivedEvent = evt;
                eventFired = true;
            });

            bool openedCallbackFired = false;
            cage.OnOpened += c => openedCallbackFired = true;

            cage.TakeDamage(new DamageInfo(50));

            Assert.AreEqual(0, cage.CurrentHealth);
            Assert.IsTrue(cage.IsOpen);
            Assert.IsFalse(cage.IsAlive);
            Assert.IsFalse(cage.CageCollider.enabled);
            Assert.AreEqual(9, squad.SquadCount);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(4, receivedEvent.Count);
            Assert.AreEqual(1, receivedEvent.LaneIndex);
            Assert.IsTrue(openedCallbackFired);
        }

        [Test]
        public void Cage_TakeDamage_WhenAlreadyOpenOrLost_DoesNotApplyDamageOrSquadChange()
        {
            var squad = new FakeSquad(5);
            var cage = CreateCage(CreateDefinition(hp: 20, survivors: 2), lane: 0, squad: squad);

            cage.TakeDamage(new DamageInfo(20));
            Assert.AreEqual(7, squad.SquadCount);
            Assert.IsTrue(cage.IsOpen);

            cage.TakeDamage(new DamageInfo(20));
            Assert.AreEqual(7, squad.SquadCount);
            Assert.AreEqual(0, cage.CurrentHealth);
        }

        [Test]
        public void Cage_TakeDamage_WhenAlreadyLost_DoesNotApplyDamageOrSquadChange()
        {
            var squad = new FakeSquad(5);
            var cage = CreateCage(CreateDefinition(hp: 50, survivors: 3), lane: 0, squad: squad, position: new Vector3(0, 0, 10f));

            bool rescuedEventFired = false;
            _eventBus.Subscribe<SurvivorsRescuedEvent>(_ => rescuedEventFired = true);

            bool escaped = cage.CheckEscape(cage.transform.position.z + 1f);
            Assert.IsTrue(escaped);
            Assert.IsTrue(cage.IsLost);
            Assert.IsFalse(cage.IsOpen);

            cage.TakeDamage(new DamageInfo(100));

            Assert.IsFalse(cage.IsOpen);
            Assert.IsTrue(cage.IsLost);
            Assert.AreEqual(5, squad.SquadCount);
            Assert.IsFalse(rescuedEventFired);
        }

        [Test]
        public void Cage_CheckEscape_WhenGeneralPasses_MarksLost_PublishesLostEvent_DisablesCollider()
        {
            var cage = CreateCage(CreateDefinition(hp: 50, survivors: 3), lane: 2, position: new Vector3(0, 0, 20f));

            SurvivorsLostEvent receivedEvent = default;
            bool eventFired = false;
            _eventBus.Subscribe<SurvivorsLostEvent>(evt =>
            {
                receivedEvent = evt;
                eventFired = true;
            });

            bool lostCallbackFired = false;
            cage.OnLost += c => lostCallbackFired = true;

            bool escapedBefore = cage.CheckEscape(15f);
            Assert.IsFalse(escapedBefore);
            Assert.IsFalse(cage.IsLost);
            Assert.IsTrue(cage.IsAlive);

            bool escapedAfter = cage.CheckEscape(20f);
            Assert.IsTrue(escapedAfter);
            Assert.IsTrue(cage.IsLost);
            Assert.IsFalse(cage.IsAlive);
            Assert.IsFalse(cage.CageCollider.enabled);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(3, receivedEvent.Count);
            Assert.AreEqual(2, receivedEvent.LaneIndex);
            Assert.IsTrue(lostCallbackFired);

            bool escapedAgain = cage.CheckEscape(25f);
            Assert.IsFalse(escapedAgain);
        }

        [Test]
        public void Cage_CheckEscape_WhenAlreadyOpen_ReturnsFalse()
        {
            var cage = CreateCage(CreateDefinition(hp: 10), position: new Vector3(0, 0, 10f));
            cage.TakeDamage(new DamageInfo(10));
            Assert.IsTrue(cage.IsOpen);

            bool escaped = cage.CheckEscape(15f);
            Assert.IsFalse(escaped);
            Assert.IsFalse(cage.IsLost);
        }

        [Test]
        public void Projectile_HandleTrigger_DamagesCage_AndRecyclesProjectile()
        {
            var cage = CreateCage(CreateDefinition(hp: 50));

            var projGo = Track(new GameObject("TestProjectile"));
            var proj = projGo.AddComponent<Projectile>();
            proj.Initialize(20, 10f, 50f, null);

            Assert.IsTrue(proj.IsActiveInPool);
            proj.HandleTrigger(cage.gameObject);

            Assert.AreEqual(30, cage.CurrentHealth);
            Assert.IsFalse(proj.IsActiveInPool);
        }

        [Test]
        public void HordeSpawner_SpawnWave_ConsumesPendingBonusZombies_FromLevelDirector()
        {
            var spawnerGo = Track(new GameObject("TestSpawner"));
            var spawner = spawnerGo.AddComponent<HordeSpawner>();
            var layout = new LaneLayout(3, 2.0f);

            var pool = new ObjectPool<EnemyController>(() =>
            {
                var go = Track(new GameObject("Enemy"));
                return go.AddComponent<EnemyController>();
            });
            spawner.Initialize(layout, pool);

            var directorGo = Track(new GameObject("Director"));
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new Game.Core.State.RunConfig { InitialSquad = 5 }, null, _eventBus);
            spawner.LevelDirector = director;

            _eventBus.Publish(new SurvivorsLostEvent(count: 3, laneIndex: 1, cageId: "cage_lost"));
            Assert.AreEqual(3, director.GetPendingBonusZombies(1));

            var wave1 = spawner.SpawnWave(laneIndex: 1, count: 2, startZ: 10f);
            Assert.AreEqual(5, wave1.Count);
            Assert.AreEqual(0, director.GetPendingBonusZombies(1));

            var wave2 = spawner.SpawnWave(laneIndex: 1, count: 2, startZ: 10f);
            Assert.AreEqual(2, wave2.Count);
        }

        [Test]
        public void LevelDirector_TrackCage_CheckEscapeOnTick_MarksCageLost_AndIncrementsPendingBonus()
        {
            var directorGo = Track(new GameObject("Director"));
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new Game.Core.State.RunConfig { InitialSquad = 5 }, null, _eventBus);

            var cage = CreateCage(CreateDefinition(hp: 40, survivors: 3), lane: 0, position: new Vector3(0, 0, 15f));
            director.TrackCage(cage);

            director.Tick(10f);
            Assert.IsFalse(cage.IsLost);
            Assert.AreEqual(0, director.GetPendingBonusZombies(0));

            director.Tick(15f);
            Assert.IsTrue(cage.IsLost);
            Assert.AreEqual(3, director.GetPendingBonusZombies(0));
        }

        [Test]
        public void LevelDirector_TrackCage_WhenCageAlreadyOpen_CheckTrackedCagesRemovesWithoutLostEvent()
        {
            var directorGo = Track(new GameObject("Director"));
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new Game.Core.State.RunConfig { InitialSquad = 5 }, null, _eventBus);

            var cage = CreateCage(CreateDefinition(hp: 40, survivors: 3), lane: 0, position: new Vector3(0, 0, 15f));
            director.TrackCage(cage);

            bool lostEventFired = false;
            _eventBus.Subscribe<SurvivorsLostEvent>(_ => lostEventFired = true);

            cage.TakeDamage(new DamageInfo(100));
            Assert.IsTrue(cage.IsOpen);

            director.Tick(20f);

            Assert.IsFalse(lostEventFired);
            Assert.AreEqual(0, director.GetPendingBonusZombies(0));

            director.Tick(30f);
            Assert.IsFalse(lostEventFired);
            Assert.AreEqual(0, director.GetPendingBonusZombies(0));
        }

        [Test]
        public void LevelDirector_RescuedSurvivors_AccumulatedInRunResult_OnEndRun()
        {
            var directorGo = Track(new GameObject("Director"));
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new Game.Core.State.RunConfig { InitialSquad = 5 }, null, _eventBus);

            Game.Core.State.RunResult endResult = null;
            director.OnRunEnded += res => endResult = res;

            _eventBus.Publish(new SurvivorsRescuedEvent(3, 0, "cage_a"));
            _eventBus.Publish(new SurvivorsRescuedEvent(2, 1, "cage_b"));

            director.EndRun(victory: true);

            Assert.IsNotNull(endResult);
            Assert.AreEqual(5, endResult.Rescued);
            Assert.AreEqual(5, director.RescuedSurvivorsCount);
        }

        [Test]
        public void CageView_BindsToCage_AndUpdatesVisualTexts()
        {
            var cage = CreateCage(CreateDefinition(hp: 60, survivors: 3));

            var viewGo = Track(new GameObject("CageView"));
            var view = viewGo.AddComponent<CageView>();

            var hpTextGo = Track(new GameObject("HpText"));
            hpTextGo.transform.SetParent(viewGo.transform, false);
            var hpText = hpTextGo.AddComponent<TextMeshProUGUI>();

            var survivorTextGo = Track(new GameObject("SurvivorText"));
            survivorTextGo.transform.SetParent(viewGo.transform, false);
            var survivorText = survivorTextGo.AddComponent<TextMeshProUGUI>();

            view.HpText = hpText;
            view.SurvivorText = survivorText;
            view.Bind(cage);

            Assert.AreEqual("60/60", hpText.text);
            Assert.AreEqual("+3", survivorText.text);

            cage.TakeDamage(new DamageInfo(20));
            Assert.AreEqual("40/60", hpText.text);

            cage.TakeDamage(new DamageInfo(40));
            Assert.AreEqual("0", hpText.text);
            Assert.AreEqual("RESCUED", survivorText.text);
        }
    }
}
