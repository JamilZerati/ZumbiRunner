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
    }
}
