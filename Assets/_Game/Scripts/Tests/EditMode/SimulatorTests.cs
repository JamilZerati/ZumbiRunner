using NUnit.Framework;
using Game.Editor.Tools;

namespace Game.Tests.EditMode
{
    public class SimulatorTests
    {
        [Test]
        public void Simulate_BotFujao_WinsUnder20Percent()
        {
            var summary = SimulateCommand.RunSimulation("level_01", BotType.Fujao, runs: 10, seed: 42);

            Assert.IsNotNull(summary);
            Assert.AreEqual(10, summary.TotalRuns);
            Assert.LessOrEqual(summary.WinRate, 0.20f);
        }

        [Test]
        public void Simulate_BotGuloso_RunsProperly()
        {
            var summary = SimulateCommand.RunSimulation("level_01", BotType.Guloso, runs: 10, seed: 42);

            Assert.IsNotNull(summary);
            Assert.AreEqual(10, summary.TotalRuns);
            Assert.Greater(summary.WinRate, 0.50f);
            Assert.Greater(summary.AverageDistance, 0f);
        }

        [Test]
        public void CalculateHpMultiplier_ScalesCorrectlyByPhase()
        {
            Assert.AreEqual(1.00f, Game.Data.LevelDefinition.CalculateHpMultiplier(1), 0.001f);
            Assert.AreEqual(1.08f, Game.Data.LevelDefinition.CalculateHpMultiplier(2), 0.001f);
            Assert.AreEqual(1.32f, Game.Data.LevelDefinition.CalculateHpMultiplier(5), 0.001f);
            Assert.AreEqual(1.72f, Game.Data.LevelDefinition.CalculateHpMultiplier(10), 0.001f);
        }

        [Test]
        public void LevelDefinition_GetHpMultiplier_UsesPhaseProperty()
        {
            var levelDef = UnityEngine.ScriptableObject.CreateInstance<Game.Data.LevelDefinition>();
            levelDef.Phase = 3;

            Assert.AreEqual(1.16f, levelDef.GetHpMultiplier(), 0.001f);
            UnityEngine.Object.DestroyImmediate(levelDef);
        }
    }
}
