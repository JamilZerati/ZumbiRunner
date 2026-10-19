using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.State;
using Game.Data;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class BossGameplayTests
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

        [Test]
        public void LevelDirector_SpawnBossSegment_ActivatesArena_AndPublishesBossSpawnedEvent()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_05_boss";
            levelDef.TotalDistance = 800f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Warmup,
                    StartDistance = 0f,
                    Length = 200f
                },
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 200f,
                    Length = 500f,
                    Boss = new BossSegmentSpec
                    {
                        BossId = "boss_colossus",
                        CustomMaxHp = 1000,
                        InitialLane = 0,
                        ArenaLength = 500f
                    }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "level_05_boss" };
            director.Initialize(config, levelDef, eventBus);

            SegmentDefinition spawnedSegment = null;
            BossSpawnedEvent spawnedEvent = null;
            director.OnBossSpawned += seg => spawnedSegment = seg;
            eventBus.Subscribe<BossSpawnedEvent>(evt => spawnedEvent = evt);

            director.Tick(199f);
            Assert.AreEqual(0, director.SpawnedBossCount);
            Assert.IsFalse(director.IsBossFightActive);
            Assert.IsNull(spawnedSegment);
            Assert.IsNull(spawnedEvent);

            director.Tick(200f);
            Assert.AreEqual(1, director.SpawnedBossCount);
            Assert.IsTrue(director.IsBossFightActive);
            Assert.AreEqual("boss_colossus", director.ActiveBossId);
            Assert.IsNotNull(director.BossCombatState);
            Assert.AreEqual(1000, director.BossCombatState.CurrentHp);
            Assert.AreEqual(1000, director.BossCombatState.MaxHp);
            Assert.IsTrue(director.BossCombatState.IsArenaActive);

            Assert.IsNotNull(spawnedSegment);
            Assert.AreEqual(SegmentType.Boss, spawnedSegment.SegmentType);
            Assert.IsNotNull(spawnedEvent);
            Assert.AreEqual("boss_colossus", spawnedEvent.BossId);
            Assert.AreEqual(200f, spawnedEvent.Distance);
            Assert.AreEqual(1000, spawnedEvent.MaxHp);
        }

        [Test]
        public void LevelDirector_BossInArena_IsProtectedFromEscapeBehindGeneral()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_05_boss";
            levelDef.TotalDistance = 800f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 0f,
                    Length = 500f,
                    Boss = new BossSegmentSpec { BossId = "boss_colossus", CustomMaxHp = 1000 }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new RunConfig { LevelId = "level_05_boss" }, levelDef, eventBus);
            director.Tick(0f); // activate boss arena

            Assert.IsTrue(director.IsBossFightActive);

            float generalZ = 50f;
            float bossZ = 40f; // 10m behind general, beyond default escape distance 5.0f

            // Boss cannot escape while fight is active
            Assert.IsFalse(director.CanEnemyEscape("boss_colossus", bossZ, generalZ));
            bool escapedBoss = director.NotifyEnemyPosition("boss_colossus", 0, bossZ, generalZ);
            Assert.IsFalse(escapedBoss);
            Assert.AreEqual(0, director.EscapedEnemyCount);

            // Normal enemy can escape
            Assert.IsTrue(director.CanEnemyEscape("walker", bossZ, generalZ));
            bool escapedWalker = director.NotifyEnemyPosition("walker", 0, bossZ, generalZ);
            Assert.IsTrue(escapedWalker);
            Assert.AreEqual(1, director.EscapedEnemyCount);
        }

        [Test]
        public void LevelDirector_BossDefeatedViaEnemyKilledEvent_ConcludesArena_AndPublishesBossDefeatedEvent()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_05_boss";
            levelDef.TotalDistance = 800f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 0f,
                    Length = 500f,
                    Boss = new BossSegmentSpec { BossId = "boss_colossus", CustomMaxHp = 1000 }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new RunConfig { LevelId = "level_05_boss" }, levelDef, eventBus);
            director.Tick(100f);

            BossDefeatedEvent defeatedEvent = null;
            string fightEndedBossId = null;
            eventBus.Subscribe<BossDefeatedEvent>(evt => defeatedEvent = evt);
            director.OnBossFightEnded += id => fightEndedBossId = id;

            Assert.IsTrue(director.IsBossFightActive);

            // Publish killed event for boss
            eventBus.Publish(new EnemyKilledEvent("boss_colossus", 0, null));

            Assert.IsFalse(director.IsBossFightActive);
            Assert.IsFalse(director.BossCombatState.IsArenaActive);
            Assert.IsNotNull(defeatedEvent);
            Assert.AreEqual("boss_colossus", defeatedEvent.BossId);
            Assert.AreEqual(100f, defeatedEvent.Distance);
            Assert.AreEqual("boss_colossus", fightEndedBossId);
        }

        [Test]
        public void LevelDirector_BossDefeatedViaBossCombatStateHpDepletion_ConcludesArena()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_05_boss";
            levelDef.TotalDistance = 800f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 0f,
                    Length = 500f,
                    Boss = new BossSegmentSpec { BossId = "boss_colossus", CustomMaxHp = 500 }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new RunConfig { LevelId = "level_05_boss" }, levelDef, eventBus);
            director.Tick(50f);

            BossDefeatedEvent defeatedEvent = null;
            eventBus.Subscribe<BossDefeatedEvent>(evt => defeatedEvent = evt);

            Assert.IsTrue(director.IsBossFightActive);

            director.BossCombatState.ApplyDamage(500);

            Assert.IsFalse(director.BossCombatState.IsAlive);
            Assert.IsFalse(director.IsBossFightActive);
            Assert.IsNotNull(defeatedEvent);
            Assert.AreEqual("boss_colossus", defeatedEvent.BossId);
        }

        [Test]
        public void LevelDirector_AfterBossConcluded_BossCanEscape()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_05_boss";
            levelDef.TotalDistance = 800f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 0f,
                    Length = 500f,
                    Boss = new BossSegmentSpec { BossId = "boss_colossus", CustomMaxHp = 1000 }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new RunConfig { LevelId = "level_05_boss" }, levelDef, eventBus);
            director.Tick(0f);

            director.ConcludeBossFight();
            Assert.IsFalse(director.IsBossFightActive);

            float generalZ = 50f;
            float bossZ = 40f;

            Assert.IsTrue(director.CanEnemyEscape("boss_colossus", bossZ, generalZ));
            bool escaped = director.NotifyEnemyPosition("boss_colossus", 0, bossZ, generalZ);
            Assert.IsTrue(escaped);
            Assert.AreEqual(1, director.EscapedEnemyCount);
        }

        [Test]
        public void LevelDirector_BlocksRunVictoryWhileBossFightActive_AllowsVictoryAfterDefeat()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_05_boss";
            levelDef.TotalDistance = 500f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 200f,
                    Length = 300f,
                    Boss = new BossSegmentSpec { BossId = "boss_colossus", CustomMaxHp = 1000 }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new RunConfig { LevelId = "level_05_boss" }, levelDef, eventBus);

            RunResult runEndedResult = null;
            eventBus.Subscribe<RunEndedEvent>(evt => runEndedResult = evt.Result);

            director.Tick(200f);
            Assert.IsTrue(director.IsBossFightActive);

            // Distance reaches TotalDistance (500m), but boss is still alive
            director.Tick(500f);
            Assert.IsTrue(director.IsRunActive);
            Assert.IsNull(runEndedResult);

            // Boss is defeated
            director.ConcludeBossFight();
            Assert.IsFalse(director.IsBossFightActive);

            // Advance again, victory is now permitted
            director.Tick(500f);
            Assert.IsFalse(director.IsRunActive);
            Assert.IsNotNull(runEndedResult);
            Assert.IsTrue(runEndedResult.Victory);
        }

        [Test]
        public void LevelDirector_CheckTrackedEnemies_ProtectsBossFromRecyclingWhileArenaActive()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_05_boss";
            levelDef.TotalDistance = 800f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 0f,
                    Length = 500f,
                    Boss = new BossSegmentSpec { BossId = "boss_colossus", CustomMaxHp = 1000 }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new RunConfig { LevelId = "level_05_boss" }, levelDef, eventBus);
            director.Tick(0f); // activate boss arena

            var bossGo = CreateGameObject("Boss_Colossus");
            bossGo.transform.position = new Vector3(0, 0, 40f);
            var bossEnemy = bossGo.AddComponent<EnemyController>();
            bossEnemy.Initialize(laneIndex: 0, maxHealth: 1000, moveSpeed: 0f, onDeath: null);
            bossEnemy.ArchetypeId = "boss_colossus";

            director.TrackEnemy(bossEnemy);

            // General moves to 50m (boss is 10m behind)
            director.Tick(50f);
            Assert.IsTrue(bossEnemy.IsActiveInPool, "Boss must not be recycled while fight is active.");
            Assert.AreEqual(0, director.EscapedEnemyCount);

            // Conclude boss fight
            director.ConcludeBossFight();

            // General ticks at 50m again
            director.Tick(50f);
            Assert.IsFalse(bossEnemy.IsActiveInPool, "Boss can be recycled once fight is concluded.");
            Assert.AreEqual(1, director.EscapedEnemyCount);
        }

        [Test]
        public void LevelDirector_TickBossCombat_TicksBossCombatRuntimeState()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_05_boss";
            levelDef.TotalDistance = 800f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 0f,
                    Length = 500f,
                    Boss = new BossSegmentSpec { BossId = "boss_colossus", CustomMaxHp = 1000 }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            director.Initialize(new RunConfig { LevelId = "level_05_boss" }, levelDef, eventBus);
            director.Tick(0f);

            director.BossCombatState.TriggerTelegraph(1, 1.0f);
            Assert.IsTrue(director.BossCombatState.IsTelegraphing);
            Assert.AreEqual(1.0f, director.BossCombatState.TelegraphRemaining);

            director.TickBossCombat(0.4f);
            Assert.IsTrue(director.BossCombatState.IsTelegraphing);
            Assert.AreEqual(0.6f, director.BossCombatState.TelegraphRemaining, 1e-4f);

            director.TickBossCombat(0.7f);
            Assert.IsFalse(director.BossCombatState.IsTelegraphing);
        }
    }
}
