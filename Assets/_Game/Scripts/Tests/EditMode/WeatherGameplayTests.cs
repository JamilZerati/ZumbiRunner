using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.Core;
using Game.Core.Events;
using Game.Core.Gates;
using Game.Core.State;
using Game.Core.Status;
using Game.Core.Time;
using Game.Core.Weather;
using Game.Data;
using Game.Gameplay;
using Game.Presentation;

namespace Game.Tests.EditMode
{
    public class WeatherGameplayTests
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
        public void LevelDirector_WeatherIntegration_ActivatesWeatherOnConfiguredSegment()
        {
            var eventBus = new EventBus();
            WeatherChangedEvent? weatherEvent = null;
            eventBus.Subscribe<WeatherChangedEvent>(e => weatherEvent = e);

            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_weather_test";
            levelDef.TotalDistance = 200f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Warmup,
                    StartDistance = 0f,
                    Length = 50f,
                    Weather = WeatherType.Clear
                },
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Horde,
                    StartDistance = 50f,
                    Length = 50f,
                    Weather = WeatherType.Rain
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "test_weather" };
            director.Initialize(config, levelDef, eventBus);

            director.Tick(20f);
            Assert.AreEqual(WeatherType.Clear, director.WeatherSystem.CurrentWeather);

            director.Tick(50f);
            Assert.AreEqual(WeatherType.Rain, director.WeatherSystem.CurrentWeather);
            Assert.IsTrue(director.WeatherSystem.IsWeatherActive);
            Assert.IsTrue(weatherEvent.HasValue);
            Assert.AreEqual(WeatherType.Rain, weatherEvent.Value.NewWeather);

            director.Tick(105f);
            Assert.AreEqual(WeatherType.Clear, director.WeatherSystem.CurrentWeather);
            Assert.IsFalse(director.WeatherSystem.IsWeatherActive);
        }

        [Test]
        public void LevelDirector_WeatherSegmentEvent_TriggersWeatherByEvent()
        {
            var eventBus = new EventBus();
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "level_weather_event_test";
            levelDef.TotalDistance = 200f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Warmup,
                    StartDistance = 0f,
                    Length = 100f,
                    Events = new[]
                    {
                        new SegmentEvent
                        {
                            DistanceOffset = 60f,
                            Type = "Weather",
                            Data = "Blizzard"
                        }
                    }
                }
            };

            var directorGo = CreateGameObject("LevelDirector");
            var director = directorGo.AddComponent<LevelDirector>();
            var config = new RunConfig { LevelId = "test_event" };
            director.Initialize(config, levelDef, eventBus);

            director.Tick(25f);
            Assert.AreEqual(WeatherType.Blizzard, director.WeatherSystem.CurrentWeather);
            Assert.IsTrue(director.WeatherSystem.IsWeatherActive);
        }

        [Test]
        public void CombatDirector_FogWeather_ReducesSquadRangeBy25Percent()
        {
            var directorGo = CreateGameObject("CombatDirector");
            var combat = directorGo.AddComponent<CombatDirector>();

            var stageModifiers = new StageModifierController();
            combat.SetStageModifiers(stageModifiers);

            Assert.AreEqual(1f, combat.EffectiveSquadRangeMultiplier, 1e-4f);
            Assert.AreEqual(20f, combat.GetModifiedRange(20f), 1e-4f);

            var fogModifier = new WeatherStageModifier(WeatherType.Fog);
            stageModifiers.Activate(fogModifier, 0f, 100f);

            Assert.AreEqual(0.75f, combat.EffectiveSquadRangeMultiplier, 1e-4f);
            Assert.AreEqual(15f, combat.GetModifiedRange(20f), 1e-4f);
        }

        [Test]
        public void EnemyController_GameTimeService_BulletTime_ScalesDeltaTime()
        {
            var enemyGo = CreateGameObject("Enemy");
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(0, 100, 5f, null);

            var timeService = new GameTimeService();
            enemy.SetTimeService(timeService);

            float normalDelta = timeService.GetDeltaTime(TimeTarget.Enemy, 1.0f);
            Assert.AreEqual(1.0f, normalDelta, 1e-4f);

            timeService.ActivateBulletTime(2.0f, enemyScale: 0.2f, allyScale: 1.0f);

            float bulletTimeDelta = timeService.GetDeltaTime(TimeTarget.Enemy, 1.0f);
            Assert.AreEqual(0.2f, bulletTimeDelta, 1e-4f);

            float allyDelta = timeService.GetDeltaTime(TimeTarget.Ally, 1.0f);
            Assert.AreEqual(1.0f, allyDelta, 1e-4f);
        }

        [Test]
        public void WeatherVisualController_TogglesVisualEffects_OnWeatherChangedEvent()
        {
            var eventBus = new EventBus();
            var rainGo = CreateGameObject("RainVfx");
            var blizzardGo = CreateGameObject("BlizzardVfx");
            var heatwaveGo = CreateGameObject("HeatwaveVfx");
            var fogGo = CreateGameObject("FogVfx");

            var visualGo = CreateGameObject("WeatherVisual");
            var visual = visualGo.AddComponent<WeatherVisualController>();
            visual.Initialize(eventBus, null, rainGo, blizzardGo, heatwaveGo, fogGo);

            Assert.IsFalse(rainGo.activeSelf);
            Assert.IsFalse(blizzardGo.activeSelf);
            Assert.IsFalse(heatwaveGo.activeSelf);
            Assert.IsFalse(fogGo.activeSelf);

            eventBus.Publish(new WeatherChangedEvent(WeatherType.Clear, WeatherType.Rain, 50f));
            Assert.IsTrue(rainGo.activeSelf);
            Assert.IsFalse(blizzardGo.activeSelf);

            eventBus.Publish(new WeatherChangedEvent(WeatherType.Rain, WeatherType.Blizzard, 50f));
            Assert.IsFalse(rainGo.activeSelf);
            Assert.IsTrue(blizzardGo.activeSelf);

            eventBus.Publish(new WeatherChangedEvent(WeatherType.Blizzard, WeatherType.Clear, 0f));
            Assert.IsFalse(rainGo.activeSelf);
            Assert.IsFalse(blizzardGo.activeSelf);
        }

        [Test]
        public void E2E_Weather_BulletTime_And_ShockSynergy_Simulation()
        {
            var eventBus = new EventBus();
            var timeService = new GameTimeService();
            var weatherSystem = new WeatherSystem(eventBus);
            var stageModifiers = new StageModifierController();

            timeService.ActivateBulletTime(3f, enemyScale: 0.2f, allyScale: 1.0f);
            Assert.IsTrue(timeService.IsBulletTimeActive);

            weatherSystem.SetWeather(WeatherType.Rain, 100f, stageModifiers);
            Assert.AreEqual(WeatherType.Rain, weatherSystem.CurrentWeather);

            var origin = new FakeReceiver(50, _objectsToDestroy);
            var neighbor = new FakeReceiver(50, _objectsToDestroy);
            var neighborhood = new FakeNeighborhood();
            neighborhood.Register(origin, 0f);
            neighborhood.Register(neighbor, 1f);

            var controller = new StatusEffectController(origin.Health, origin,
                InMemoryStatusCatalog.CreateDefault(),
                InMemoryInteractionTable.CreateDefault(),
                neighborhood, eventBus);

            controller.Apply(new StatusApplication(StatusKind.Wet, 1, this));
            Assert.IsTrue(controller.Has(StatusKind.Wet));

            int damageDealt = controller.ResolveHit(new DamageInfo(10, DamageType.Physical, this),
                new[] { new StatusApplication(StatusKind.Shock, 1, this) });

            Assert.AreEqual(20, damageDealt);
            Assert.IsFalse(controller.Has(StatusKind.Wet));
            Assert.AreEqual(1, neighbor.ReceivedDamage.Count);
            Assert.AreEqual(12, neighbor.ReceivedDamage[0].Amount);

            timeService.Tick(3.0f);
            Assert.IsFalse(timeService.IsBulletTimeActive);

            weatherSystem.TickDistance(100f, stageModifiers);
            Assert.IsFalse(weatherSystem.IsWeatherActive);
        }

        private sealed class FakeReceiver : IStatusReceiver
        {
            public HealthComponent Health { get; }
            public bool IsAlive => Health.IsAlive;
            public List<DamageInfo> ReceivedDamage { get; } = new List<DamageInfo>();

            public FakeReceiver(int maxHp, List<GameObject> tracker = null)
            {
                var go = new GameObject("FakeReceiver");
                tracker?.Add(go);
                Health = go.AddComponent<HealthComponent>();
                Health.Initialize(maxHp);
            }

            public int ReceiveHit(DamageInfo hit, IReadOnlyList<StatusApplication> onHit)
            {
                ReceivedDamage.Add(hit);
                Health.TakeDamage(hit);
                return hit.Amount;
            }
        }

        private sealed class FakeNeighborhood : IStatusNeighborhood
        {
            private readonly List<IStatusReceiver> _receivers = new List<IStatusReceiver>();

            public void Register(IStatusReceiver receiver, float positionZ)
            {
                _receivers.Add(receiver);
            }

            public IReadOnlyList<IStatusReceiver> FindNearby(IStatusReceiver origin, float radius, int maxTargets)
            {
                var list = new List<IStatusReceiver>();
                for (int i = 0; i < _receivers.Count; i++)
                {
                    if (_receivers[i] != origin && _receivers[i].IsAlive)
                    {
                        list.Add(_receivers[i]);
                        if (list.Count >= maxTargets) break;
                    }
                }
                return list;
            }
        }
    }
}
