using System.Collections.Generic;
using NUnit.Framework;
using Game.Core.State;
using Game.Core.Events;
using Game.Core.Status;

namespace Game.Core.Tests
{
    public class RunCoreTests
    {
        [Test]
        public void RunConfig_DefaultValues_AreCorrect()
        {
            var config = new RunConfig();
            Assert.AreEqual(10, config.InitialSquad);
            Assert.AreEqual(1f, config.RewardMultiplier);
            Assert.IsNotNull(config.MutationIds);
        }

        [Test]
        public void RunResult_DefaultValues_AreCorrect()
        {
            var result = new RunResult();
            Assert.IsNotNull(result.KillsByArchetype);
            Assert.IsNotNull(result.MutationIds);
        }

        [Test]
        public void Events_MaintainProperties()
        {
            var config = new RunConfig("lvl1", 5, "w1", 1.5f, new List<string>());
            var runStarted = new RunStartedEvent("lvl1", config);
            Assert.AreEqual("lvl1", runStarted.LevelId);
            Assert.AreEqual(config, runStarted.Config);

            var result = new RunResult { LevelId = "lvl1" };
            var runEnded = new RunEndedEvent(result);
            Assert.AreEqual(result, runEnded.Result);

            var escaped = new EnemyEscapedEvent("z1", 2, 10.5f);
            Assert.AreEqual("z1", escaped.ArchetypeId);
            Assert.AreEqual(2, escaped.Lane);
            Assert.AreEqual(10.5f, escaped.ZPosition);

            var multiplier = new MultiplierReachedEvent(3, 5);
            Assert.AreEqual(3, multiplier.Multiplier);
            Assert.AreEqual(5, multiplier.SoldiersSacrificed);

            var killed = new EnemyKilledEvent("z1", 1, new List<StatusKind> { StatusKind.Burn }, true);
            Assert.AreEqual("z1", killed.ArchetypeId);
            Assert.AreEqual(1, killed.Lane);
            Assert.AreEqual(1, killed.StatusesAtDeath.Count);
            Assert.AreEqual(StatusKind.Burn, killed.StatusesAtDeath[0]);
            Assert.IsTrue(killed.ByAbility);
        }
        
        [Test]
        public void EventBus_CanPublishAndReceive_Events()
        {
            var bus = new EventBus();
            
            bool startedReceived = false;
            bus.Subscribe<RunStartedEvent>(e => startedReceived = true);
            bus.Publish(new RunStartedEvent("lvl1", new RunConfig()));
            Assert.IsTrue(startedReceived);

            bool endedReceived = false;
            bus.Subscribe<RunEndedEvent>(e => endedReceived = true);
            bus.Publish(new RunEndedEvent(new RunResult()));
            Assert.IsTrue(endedReceived);

            bool escapedReceived = false;
            bus.Subscribe<EnemyEscapedEvent>(e => escapedReceived = true);
            bus.Publish(new EnemyEscapedEvent("z1", 1, 0f));
            Assert.IsTrue(escapedReceived);

            bool multiplierReceived = false;
            bus.Subscribe<MultiplierReachedEvent>(e => multiplierReceived = true);
            bus.Publish(new MultiplierReachedEvent(2, 2));
            Assert.IsTrue(multiplierReceived);

            bool killedReceived = false;
            bus.Subscribe<EnemyKilledEvent>(e => killedReceived = true);
            bus.Publish(new EnemyKilledEvent("z1", 1));
            Assert.IsTrue(killedReceived);
        }
    }
}
