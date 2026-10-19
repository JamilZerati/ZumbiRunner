using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Editor.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class BossSimulationAndValidationTests
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

        [Test]
        public void Simulate_Level05Boss_BotGuloso_AchievesBalancedWinRateAround30Percent()
        {
            var summary = SimulateCommand.RunSimulation("level_05_boss", BotType.Guloso, runs: 10, seed: 42);

            Assert.IsNotNull(summary);
            Assert.AreEqual(10, summary.TotalRuns);
            string runDetails = string.Join(", ", summary.Runs.Select(r => $"r{r.RunIndex}(v={r.Victory},sq={r.FinalSquad},d={r.DistanceReached:F0})"));
            Assert.GreaterOrEqual(summary.WinRate, 0.20f, $"Victories was {summary.Victories}/10. Runs: {runDetails}");
            Assert.LessOrEqual(summary.WinRate, 0.40f, $"Victories was {summary.Victories}/10. Runs: {runDetails}");
        }

        [Test]
        public void Simulate_Level05Boss_BotFujao_AchievesLowWinRateUnder20Percent()
        {
            var summary = SimulateCommand.RunSimulation("level_05_boss", BotType.Fujao, runs: 10, seed: 42);

            Assert.IsNotNull(summary);
            Assert.AreEqual(10, summary.TotalRuns);
            Assert.LessOrEqual(summary.WinRate, 0.20f);
        }

        [Test]
        public void Simulate_Level01_BotGuloso_MaintainsStandardWinRateAround70Percent()
        {
            var summary = SimulateCommand.RunSimulation("level_01", BotType.Guloso, runs: 10, seed: 42);

            Assert.IsNotNull(summary);
            Assert.AreEqual(10, summary.TotalRuns);
            Assert.GreaterOrEqual(summary.WinRate, 0.50f);
        }

        [Test]
        public void LevelValidator_BossSegmentWithValidSpec_Passes()
        {
            var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            level.LevelId = "level_boss_valid";
            level.TotalDistance = 800f;
            level.Speed = 8f;
            level.LaneCount = 2;
            level.Phase = 5;
            level.IsBossStage = true;
            level.BossId = "boss_colossus";
            level.Segments = new[]
            {
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

            var result = LevelValidator.Validate(level);
            Assert.IsTrue(result.IsValid, string.Join("; ", result.Errors));
            Assert.AreEqual(0, result.Errors.Count);
        }

        [Test]
        public void LevelValidator_BossSegmentWithoutSpec_ReportsError()
        {
            var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            level.LevelId = "level_boss_nospec";
            level.TotalDistance = 500f;
            level.Speed = 8f;
            level.LaneCount = 2;
            level.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 100f,
                    Length = 300f,
                    Boss = null
                }
            };

            var result = LevelValidator.Validate(level);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Boss segment at")));
        }

        [Test]
        public void LevelValidator_BossSegmentWithInvalidStats_ReportsErrors()
        {
            var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            level.LevelId = "level_boss_invalid";
            level.TotalDistance = 500f;
            level.Speed = 8f;
            level.LaneCount = 2;
            level.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 100f,
                    Length = 300f,
                    Boss = new BossSegmentSpec
                    {
                        BossId = "   ",
                        CustomMaxHp = -10,
                        InitialLane = 5, // Invalid for LaneCount = 2
                        ArenaLength = -50f
                    }
                }
            };

            var result = LevelValidator.Validate(level);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("empty BossId")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("negative CustomMaxHp")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("invalid InitialLane")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("negative ArenaLength")));
        }

        [Test]
        public void SimulateCommand_BossArena_RegistersKillsAndDistance()
        {
            var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            level.LevelId = "level_boss_test_arena";
            level.TotalDistance = 400f;
            level.Speed = 8f;
            level.LaneCount = 2;
            level.Phase = 1;
            level.InitialTroops = 50;
            level.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 50f,
                    Length = 300f,
                    Boss = new BossSegmentSpec
                    {
                        BossId = "boss_colossus",
                        CustomMaxHp = 100, // Low HP to guarantee kill
                        InitialLane = 0,
                        ArenaLength = 300f
                    }
                }
            };

            var runResult = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0);

            Assert.IsNotNull(runResult);
            Assert.IsTrue(runResult.Victory);
            Assert.GreaterOrEqual(runResult.Kills, 1);
            Assert.Greater(runResult.DistanceReached, 50f);
        }
    }
}
