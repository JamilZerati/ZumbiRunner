using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.Core;
using Game.Core.Events;
using Game.Core.State;
using Game.Data;
using Game.Gameplay;

namespace Game.Tests.EditMode
{
    public class LevelDirectorTests
    {
        private List<GameObject> _objectsToDestroy;
        private List<ScriptableObject> _assetsToDestroy;

        [SetUp]
        public void SetUp()
        {
            _objectsToDestroy = new List<GameObject>();
            _assetsToDestroy = new List<ScriptableObject>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_objectsToDestroy != null)
            {
                for (int i = 0; i < _objectsToDestroy.Count; i++)
                {
                    if (_objectsToDestroy[i] != null)
                    {
                        Object.DestroyImmediate(_objectsToDestroy[i]);
                    }
                }
                _objectsToDestroy.Clear();
            }

            if (_assetsToDestroy != null)
            {
                for (int i = 0; i < _assetsToDestroy.Count; i++)
                {
                    if (_assetsToDestroy[i] != null)
                    {
                        Object.DestroyImmediate(_assetsToDestroy[i]);
                    }
                }
                _assetsToDestroy.Clear();
            }
        }

        private GameObject CreateGameObject(string name = "GameObject")
        {
            var go = new GameObject(name);
            _objectsToDestroy.Add(go);
            return go;
        }

        private T CreateScriptableObject<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            _assetsToDestroy.Add(so);
            return so;
        }

        [Test]
        public void Tick_ZumbiEscapes_At5mBehindGeneral_ShouldEmitEvent()
        {
            var eventBus = new EventBus();
            EnemyEscapedEvent? escapedEvent = null;
            eventBus.Subscribe<EnemyEscapedEvent>(e => escapedEvent = e);

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "test_level" };
            director.Initialize(config, null, eventBus);

            float generalZ = 20f;

            director.NotifyEnemyPosition("walker", 0, 16f, generalZ);
            Assert.IsNull(escapedEvent);

            director.NotifyEnemyPosition("walker", 0, 15f, generalZ);
            Assert.IsNotNull(escapedEvent);
            Assert.AreEqual("walker", escapedEvent.Value.ArchetypeId);
            Assert.AreEqual(0, escapedEvent.Value.Lane);
            Assert.AreEqual(15f, escapedEvent.Value.ZPosition);
        }

        [Test]
        public void Tick_SpawnsHorde_At40m()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_horde_test";
            levelDef.TotalDistance = 200f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Horde,
                    StartDistance = 100f,
                    Length = 50f
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "level_horde_test" };
            director.Initialize(config, levelDef, eventBus);

            director.Tick(59f);
            Assert.AreEqual(0, director.SpawnedHordeCount);

            director.Tick(60f);
            Assert.AreEqual(1, director.SpawnedHordeCount);
        }

        [Test]
        public void Tick_RunEnded_ShouldEmitRunResult()
        {
            var eventBus = new EventBus();
            RunEndedEvent? endedEvent = null;
            eventBus.Subscribe<RunEndedEvent>(e => endedEvent = e);

            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_end_test";
            levelDef.TotalDistance = 100f;

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "level_end_test", InitialSquad = 5 };
            director.Initialize(config, levelDef, eventBus);

            director.Tick(50f);
            Assert.IsNull(endedEvent);

            director.Tick(100f);
            Assert.IsNotNull(endedEvent);
            Assert.AreEqual("level_end_test", endedEvent.Value.Result.LevelId);
            Assert.IsTrue(endedEvent.Value.Result.Victory);
            Assert.AreEqual(100f, endedEvent.Value.Result.Distance);
        }

        [Test]
        public void SurvivorsRescuedEvent_AccumulatesInRunResult()
        {
            var eventBus = new EventBus();
            RunEndedEvent? endedEvent = null;
            eventBus.Subscribe<RunEndedEvent>(e => endedEvent = e);

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "rescue_test", InitialSquad = 10 };
            director.Initialize(config, null, eventBus);

            eventBus.Publish(new SurvivorsRescuedEvent(count: 3, laneIndex: 0, cageId: "cage_01"));
            eventBus.Publish(new SurvivorsRescuedEvent(count: 4, laneIndex: 1, cageId: "cage_02"));

            Assert.AreEqual(7, director.RescuedSurvivorsCount);

            director.EndRun(victory: true);

            Assert.IsNotNull(endedEvent);
            Assert.AreEqual(7, endedEvent.Value.Result.Rescued);
        }

        [Test]
        public void SurvivorsLostEvent_EnqueuesPendingBonusZombiesPerLane()
        {
            var eventBus = new EventBus();
            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "loss_test", InitialSquad = 10 };
            director.Initialize(config, null, eventBus);

            eventBus.Publish(new SurvivorsLostEvent(count: 2, laneIndex: 0, cageId: "cage_lost_1"));
            eventBus.Publish(new SurvivorsLostEvent(count: 3, laneIndex: 0, cageId: "cage_lost_2"));
            eventBus.Publish(new SurvivorsLostEvent(count: 4, laneIndex: 1, cageId: "cage_lost_3"));

            Assert.AreEqual(5, director.GetPendingBonusZombies(0));
            Assert.AreEqual(4, director.GetPendingBonusZombies(1));
            Assert.AreEqual(0, director.GetPendingBonusZombies(2));

            int consumedLane0 = director.ConsumePendingBonusZombies(0);
            Assert.AreEqual(5, consumedLane0);
            Assert.AreEqual(0, director.GetPendingBonusZombies(0));
            Assert.AreEqual(4, director.GetPendingBonusZombies(1));
        }
    }
}
