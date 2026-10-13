using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Editor.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class HoldoutSimulationAndValidationTests
    {
        private readonly List<ScriptableObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }
            _created.Clear();
        }

        private T Track<T>(T obj) where T : ScriptableObject
        {
            _created.Add(obj);
            return obj;
        }

        private HoldoutDefinition CreateHoldout(
            string id,
            string name,
            int maxHp,
            int survivors,
            float duration,
            float contactDps = 5f,
            float waveInterval = 3f,
            int zombiesPerLane = 2)
        {
            var def = Track(ScriptableObject.CreateInstance<HoldoutDefinition>());
            def.SetData(id, name, maxHp, survivors, duration, contactDps, waveInterval, zombiesPerLane);
            return def;
        }

        [Test]
        public void ValidateHoldouts_CanonicalHoldoutsFolder_ReportsZeroErrors()
        {
            int errors = ValidateCommand.ValidateHoldouts("Assets/_Game/Data/Holdouts", out int count);
            Assert.AreEqual(0, errors);
            Assert.GreaterOrEqual(count, 1);
        }

        [Test]
        public void ValidateHoldout_NullOrMissingFields_ReturnsErrors()
        {
            var nullErrors = ValidateCommand.ValidateHoldout(null);
            Assert.AreEqual(1, nullErrors.Count);

            var emptyHoldout = CreateHoldout(string.Empty, string.Empty, 100, 4, 10f);
            var errors = ValidateCommand.ValidateHoldout(emptyHoldout);
            Assert.IsTrue(errors.Any(e => e.Contains("Id")));
            Assert.IsTrue(errors.Any(e => e.Contains("DisplayName")));
        }

        [Test]
        public void ValidateHoldout_InvalidStats_ReportsErrors()
        {
            var invalidHoldout = CreateHoldout("h_bad", "Bad", 0, -1, 0f, 0f, 0f, 0);
            var errors = ValidateCommand.ValidateHoldout(invalidHoldout);

            Assert.IsTrue(errors.Any(e => e.Contains("MaxHp")));
            Assert.IsTrue(errors.Any(e => e.Contains("TotalSurvivors")));
            Assert.IsTrue(errors.Any(e => e.Contains("DurationSeconds")));
            Assert.IsTrue(errors.Any(e => e.Contains("ZombieContactDps")));
            Assert.IsTrue(errors.Any(e => e.Contains("WaveIntervalSeconds")));
            Assert.IsTrue(errors.Any(e => e.Contains("ZombiesPerWavePerLane")));
        }

        [Test]
        public void LevelValidator_HoldoutSegmentWithValidSpec_Passes()
        {
            var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            level.LevelId = "level_holdout_valid";
            level.TotalDistance = 100f;
            level.Speed = 8f;
            level.LaneCount = 3;
            level.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Holdout,
                    StartDistance = 20f,
                    Length = 30f,
                    Holdout = new HoldoutSegmentSpec
                    {
                        HoldoutId = "holdout_standard",
                        CustomMaxHp = 150,
                        CustomSurvivors = 4,
                        CustomDuration = 12f
                    }
                }
            };

            var result = LevelValidator.Validate(level);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [Test]
        public void LevelValidator_HoldoutSegmentWithMissingOrNegativeSpec_ReportsErrors()
        {
            var levelMissingSpec = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            levelMissingSpec.LevelId = "level_missing_spec";
            levelMissingSpec.TotalDistance = 100f;
            levelMissingSpec.Speed = 8f;
            levelMissingSpec.LaneCount = 3;
            levelMissingSpec.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Holdout,
                    StartDistance = 20f,
                    Length = 30f,
                    Holdout = null
                }
            };

            var missingResult = LevelValidator.Validate(levelMissingSpec);
            Assert.IsFalse(missingResult.IsValid);
            Assert.IsTrue(missingResult.Errors.Any(e => e.Contains("no Holdout spec")));

            var levelNegativeSpec = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            levelNegativeSpec.LevelId = "level_negative_spec";
            levelNegativeSpec.TotalDistance = 100f;
            levelNegativeSpec.Speed = 8f;
            levelNegativeSpec.LaneCount = 3;
            levelNegativeSpec.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Holdout,
                    StartDistance = 20f,
                    Length = 30f,
                    Holdout = new HoldoutSegmentSpec
                    {
                        HoldoutId = "holdout_standard",
                        CustomMaxHp = -10,
                        CustomSurvivors = -2,
                        CustomDuration = -5f
                    }
                }
            };

            var negativeResult = LevelValidator.Validate(levelNegativeSpec);
            Assert.IsFalse(negativeResult.IsValid);
            Assert.IsTrue(negativeResult.Errors.Any(e => e.Contains("CustomMaxHp")));
            Assert.IsTrue(negativeResult.Errors.Any(e => e.Contains("CustomSurvivors")));
            Assert.IsTrue(negativeResult.Errors.Any(e => e.Contains("CustomDuration")));
        }

        [Test]
        public void Simulate_HoldoutSegment_GulosoDefendsAndRescues_FujaoBypasses()
        {
            var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            level.LevelId = "level_sim_holdout";
            level.TotalDistance = 100f;
            level.Speed = 8f;
            level.InitialTroops = 10;
            level.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Holdout,
                    StartDistance = 10f,
                    Length = 20f,
                    Holdout = new HoldoutSegmentSpec
                    {
                        HoldoutId = "holdout_standard",
                        CustomMaxHp = 150,
                        CustomSurvivors = 4,
                        CustomDuration = 12f
                    }
                }
            };

            var guloso = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0);
            var fujao = SimulateCommand.SimulateSingleRun(level, BotType.Fujao, 42, 0);

            Assert.IsTrue(guloso.victory);
            Assert.Greater(guloso.rescued, 0);
            Assert.Greater(guloso.finalSquad, 10);

            Assert.IsTrue(fujao.victory);
            Assert.AreEqual(0, fujao.rescued);
            Assert.AreEqual(10, fujao.finalSquad);
        }

        [Test]
        public void Simulate_HoldoutWithEvent_ProcessedCorrectly()
        {
            var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            level.LevelId = "level_sim_holdout_event";
            level.TotalDistance = 100f;
            level.Speed = 8f;
            level.InitialTroops = 10;
            level.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Warmup,
                    StartDistance = 0f,
                    Length = 50f,
                    Events = new[]
                    {
                        new SegmentEvent
                        {
                            Type = "Holdout",
                            Data = "holdoutId=holdout_standard,hp=200,survivors=6,duration=15"
                        }
                    }
                }
            };

            var guloso = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0);
            Assert.IsTrue(guloso.victory);
            Assert.AreEqual(6, guloso.rescued);
            Assert.AreEqual(16, guloso.finalSquad);
        }

        [Test]
        public void Simulate_RunSimulation_CanonicalLevel01_CompletesWithoutExceptions()
        {
            var gSummary = SimulateCommand.RunSimulation("level_01", BotType.Guloso, 2, 42);
            var fSummary = SimulateCommand.RunSimulation("level_01", BotType.Fujao, 2, 42);

            Assert.IsNotNull(gSummary);
            Assert.IsNotNull(fSummary);
            Assert.Greater(gSummary.totalRuns, 0);
            Assert.Greater(fSummary.totalRuns, 0);
        }
    }
}
