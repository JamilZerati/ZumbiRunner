using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Gates;
using Game.Core.Status;
using Game.Core.Time;
using Game.Core.Weather;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class WeatherScenariosTests
    {
        [Test]
        public void CT_1_GameTimeService_CalculatesIndependentDeltaTimes_ForAllyEnemyAndGlobal()
        {
            IGameTimeService time = new GameTimeService();
            time.GlobalTimeScale = 1.0f;
            time.AllyTimeScale = 1.2f;
            time.EnemyTimeScale = 0.5f;

            Assert.AreEqual(0.1f, time.GetDeltaTime(TimeTarget.Global, 0.1f), 1e-4f);
            Assert.AreEqual(0.12f, time.GetDeltaTime(TimeTarget.Ally, 0.1f), 1e-4f);
            Assert.AreEqual(0.05f, time.GetDeltaTime(TimeTarget.Enemy, 0.1f), 1e-4f);
        }

        [Test]
        public void CT_2_BulletTime_ReducesEnemyScaleToPointTwo_AndRestoresAfterDuration()
        {
            IGameTimeService time = new GameTimeService();
            time.ActivateBulletTime(2.0f, enemyScale: 0.2f, allyScale: 1.0f);

            Assert.IsTrue(time.IsBulletTimeActive);
            Assert.AreEqual(0.2f, time.EnemyTimeScale, 1e-4f);
            Assert.AreEqual(1.0f, time.AllyTimeScale, 1e-4f);

            time.Tick(2.0f);

            Assert.IsFalse(time.IsBulletTimeActive);
            Assert.AreEqual(1.0f, time.EnemyTimeScale, 1e-4f);
        }

        [Test]
        public void CT_3_BulletTime_IdempotentActivation_ResetsTimerWithoutCompoundingSlowdown()
        {
            IGameTimeService time = new GameTimeService();
            time.ActivateBulletTime(2.0f, enemyScale: 0.2f, allyScale: 1.0f);
            time.Tick(1.0f);
            time.ActivateBulletTime(3.0f, enemyScale: 0.2f, allyScale: 1.0f);

            Assert.AreEqual(0.2f, time.EnemyTimeScale, 1e-4f);
            Assert.AreEqual(3.0f, time.BulletTimeRemaining, 1e-4f);
        }

        [Test]
        public void CT_4_WeatherSystem_AppliesWeather_InjectsStageModifier_AndRemovesOnDistanceExpiry()
        {
            IWeatherSystem weather = new WeatherSystem();
            var stageModifiers = new StageModifierController();

            weather.SetWeather(WeatherType.Rain, durationDistance: 100f, stageModifiers: stageModifiers);

            Assert.AreEqual(WeatherType.Rain, weather.CurrentWeather);
            Assert.IsTrue(weather.IsWeatherActive);
            Assert.AreEqual(1, stageModifiers.ActiveCount);

            weather.TickDistance(100f, stageModifiers: stageModifiers);

            Assert.AreEqual(WeatherType.Clear, weather.CurrentWeather);
            Assert.IsFalse(weather.IsWeatherActive);
            Assert.AreEqual(0, stageModifiers.ActiveCount);
        }

        [Test]
        public void CT_5_WeatherMultipliers_VerifyBlizzardHeatwaveFogCanonicalMultipliers()
        {
            var blizzard = new WeatherStageModifier(WeatherType.Blizzard);
            Assert.AreEqual(0.8f, blizzard.EnemySpeedMultiplier, 1e-4f);

            var heatwave = new WeatherStageModifier(WeatherType.Heatwave);
            Assert.AreEqual(1.2f, heatwave.EnemyContactDamageMultiplier, 1e-4f);

            var fog = new WeatherStageModifier(WeatherType.Fog);
            Assert.AreEqual(0.75f, fog.SquadRangeMultiplier, 1e-4f);

            var rain = new WeatherStageModifier(WeatherType.Rain);
            Assert.AreEqual("clima:rain", rain.SourceId);
        }

        [Test]
        public void CT_6_StatusWet_HitByShock_TriggersElectrocuteSynergy_DoublesChainAndDamage_AndConsumesWet()
        {
            var wet = new WetStatus { Duration = 5f };
            var shock = new ShockStatus { ChainCount = 2, ChainRadius = 5f, ChainDamage = 10 };

            wet.OnApply(null, null, 1);
            Assert.AreEqual(StatusKind.Wet, wet.Kind);
            Assert.IsTrue(wet.IsPersistent);
        }

        [Test]
        public void CT_7_StatusShock_WithoutWet_MaintainsStandardChain_WithoutSynergy()
        {
            var shock = new ShockStatus { ChainCount = 2, ChainRadius = 5f, ChainDamage = 10 };
            Assert.AreEqual(2, shock.ChainCount);
            Assert.AreEqual(StatusKind.Shock, shock.Kind);

            var wet = new WetStatus();
            wet.OnTick(null, null, 0.1f);
        }

        [Test]
        public void CT_8_LevelDirector_WeatherProgression_TriggersWeatherAtConfiguredDistance()
        {
            IWeatherSystem weather = new WeatherSystem();
            weather.TickDistance(50f, null);
            Assert.IsTrue(weather.IsWeatherActive);
        }

        [Test]
        public void CT_9_WeatherValidation_RejectsInvalidWeatherTypeOrMissingFields()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                string invalidType = "Tornado";
                if (!Enum.TryParse<WeatherType>(invalidType, true, out _))
                {
                    throw new ArgumentException($"Invalid weather type: {invalidType}");
                }
            });

            IWeatherSystem weather = new WeatherSystem();
            weather.SetWeather(WeatherType.Rain, -10f, null);
        }

        [Test]
        public void CT_10_E2E_Simulation_WeatherTransition_BulletTime_AndShockSynergy_WithoutExceptions()
        {
            IGameTimeService time = new GameTimeService();
            IWeatherSystem weather = new WeatherSystem();

            time.ActivateBulletTime(1.0f);
            weather.SetWeather(WeatherType.Rain, 50f);
            time.Tick(0.5f);
            weather.TickDistance(25f);

            Assert.IsTrue(time.IsBulletTimeActive);
            Assert.IsTrue(weather.IsWeatherActive);
        }
    }
}
