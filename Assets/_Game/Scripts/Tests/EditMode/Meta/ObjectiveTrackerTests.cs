using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.State;
using Game.Meta.Objectives;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class ObjectiveTrackerTests
    {
        private EventBus _eventBus;
        private List<ObjectiveCompletedEvent> _completedEvents;
        private ObjectiveTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _completedEvents = new List<ObjectiveCompletedEvent>();
            _eventBus.Subscribe<ObjectiveCompletedEvent>(e => _completedEvents.Add(e));
            _tracker = new ObjectiveTracker(_eventBus);
        }

        [Test]
        public void RegisterObjective_InitializesZeroProgress()
        {
            var def = new ObjectiveDefinition("kill_walkers", ObjectiveScope.Cumulative, "kills", 10, "zombie_walker");
            _tracker.RegisterObjective(def);

            var progress = _tracker.GetProgress("kill_walkers");
            Assert.IsNotNull(progress);
            Assert.AreEqual("kill_walkers", progress.ObjectiveId);
            Assert.AreEqual(0, progress.CurrentValue);
            Assert.AreEqual(10, progress.TargetValue);
            Assert.IsFalse(progress.IsCompleted);
        }

        [Test]
        public void IngestEvent_IncrementsCumulativeMatchingEvents()
        {
            _tracker.RegisterObjective(new ObjectiveDefinition("kill_any", ObjectiveScope.Cumulative, "kills", 5));
            _tracker.RegisterObjective(new ObjectiveDefinition("kill_brute", ObjectiveScope.Cumulative, "kills", 2, "zombie_brute"));

            _tracker.IngestEvent(new EnemyKilledEvent("zombie_walker", 0));
            _tracker.IngestEvent(new EnemyKilledEvent("zombie_brute", 0));

            Assert.AreEqual(2, _tracker.GetProgress("kill_any").CurrentValue);
            Assert.AreEqual(1, _tracker.GetProgress("kill_brute").CurrentValue);
        }

        [Test]
        public void IngestEvent_SurvivorsRescued_IncrementsRescueMetric()
        {
            _tracker.RegisterObjective(new ObjectiveDefinition("rescue_survivors", ObjectiveScope.Cumulative, "rescues", 10));

            _tracker.IngestEvent(new SurvivorsRescuedEvent(4, 0));

            Assert.AreEqual(4, _tracker.GetProgress("rescue_survivors").CurrentValue);
        }

        [Test]
        public void IngestRunResult_EvaluatesRunScopeObjectives()
        {
            _tracker.RegisterObjective(new ObjectiveDefinition("reach_3_stars", ObjectiveScope.Run, "stars", 3));
            _tracker.RegisterObjective(new ObjectiveDefinition("earn_500_coins", ObjectiveScope.Run, "coins", 500));
            _tracker.RegisterObjective(new ObjectiveDefinition("win_run", ObjectiveScope.Run, "victory", 1));

            var result = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                Stars = 3,
                CoinsEarned = 600
            };

            _tracker.IngestRunResult(result);

            Assert.IsTrue(_tracker.GetProgress("reach_3_stars").IsCompleted);
            Assert.IsTrue(_tracker.GetProgress("earn_500_coins").IsCompleted);
            Assert.IsTrue(_tracker.GetProgress("win_run").IsCompleted);

            var completed = _tracker.CheckCompletedObjectives();
            CollectionAssert.Contains(completed, "reach_3_stars");
            CollectionAssert.Contains(completed, "earn_500_coins");
            CollectionAssert.Contains(completed, "win_run");
        }

        [Test]
        public void IngestRunResult_AccumulatesCumulativePostRunMetrics()
        {
            _tracker.RegisterObjective(new ObjectiveDefinition("accumulate_stars", ObjectiveScope.Cumulative, "stars", 5));

            _tracker.IngestRunResult(new RunResult { Stars = 3 });
            Assert.AreEqual(3, _tracker.GetProgress("accumulate_stars").CurrentValue);

            _tracker.IngestRunResult(new RunResult { Stars = 2 });
            Assert.AreEqual(5, _tracker.GetProgress("accumulate_stars").CurrentValue);
            Assert.IsTrue(_tracker.GetProgress("accumulate_stars").IsCompleted);
        }

        [Test]
        public void Completion_EmitsObjectiveCompletedEvent_OnlyOnce()
        {
            _tracker.RegisterObjective(new ObjectiveDefinition("quick_kill", ObjectiveScope.Cumulative, "kills", 2));

            _tracker.IngestEvent(new EnemyKilledEvent("zombie_walker", 0));
            Assert.AreEqual(0, _completedEvents.Count);

            _tracker.IngestEvent(new EnemyKilledEvent("zombie_walker", 0));
            Assert.AreEqual(1, _completedEvents.Count);
            Assert.AreEqual("quick_kill", _completedEvents[0].ObjectiveId);

            _tracker.IngestEvent(new EnemyKilledEvent("zombie_walker", 0));
            Assert.AreEqual(1, _completedEvents.Count);
        }

        [Test]
        public void GetProgress_ReturnsNull_ForUnregisteredObjective()
        {
            Assert.IsNull(_tracker.GetProgress("non_existent"));
        }
    }
}
