using NUnit.Framework;
using Game.Gameplay;
using Game.Core;
using Game.Core.Events;
using System.Collections.Generic;

namespace Game.Tests.EditMode
{
    public class MultiplierLaneTests
    {
        private class MockSquad : ISquad
        {
            public int SquadCount { get; private set; }

            public MockSquad(int initialCount)
            {
                SquadCount = initialCount;
            }

            public bool Add(int amount) { SquadCount += amount; return true; }
            public bool Remove(int amount) 
            { 
                if (SquadCount >= amount) 
                {
                    SquadCount -= amount; 
                    return true; 
                }
                return false;
            }
            public bool Multiply(int factor) { SquadCount *= factor; return true; }
            public bool Divide(int divisor) { SquadCount /= divisor; return true; }
            public bool SetCount(int newCount) { SquadCount = newCount; return true; }
        }

        private class MockEventBus : IEventBus
        {
            public List<object> Events = new List<object>();

            public void Publish<T>(T eventData)
            {
                Events.Add(eventData);
            }

            public System.IDisposable Subscribe<T>(System.Action<T> handler)
            {
                return null;
            }
        }

        private List<UnityEngine.GameObject> _spawnedObjects = new List<UnityEngine.GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawnedObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            _spawnedObjects.Clear();
        }

        private MultiplierLane CreateLane()
        {
            var go = new UnityEngine.GameObject("Test_MultiplierLane");
            _spawnedObjects.Add(go);
            return go.AddComponent<MultiplierLane>();
        }

        [Test]
        public void ReachMultiplier_PayTroops_Every15m_ShouldSucceed()
        {
            var lane = CreateLane();
            var bus = new MockEventBus();
            lane.Initialize(bus);

            var squad = new MockSquad(15); // enough for 5 (x1) and 10 (x2)

            bool adv1 = lane.TryAdvance(squad);
            Assert.IsTrue(adv1);
            Assert.AreEqual(1, lane.CurrentMultiplier);
            Assert.AreEqual(10, squad.SquadCount);
            
            bool adv2 = lane.TryAdvance(squad);
            Assert.IsTrue(adv2);
            Assert.AreEqual(2, lane.CurrentMultiplier);
            Assert.AreEqual(0, squad.SquadCount);

            Assert.AreEqual(2, bus.Events.Count);
            var ev1 = (MultiplierReachedEvent)bus.Events[0];
            Assert.AreEqual(1, ev1.Multiplier);
            Assert.AreEqual(5, ev1.SoldiersSacrificed);
            
            var ev2 = (MultiplierReachedEvent)bus.Events[1];
            Assert.AreEqual(2, ev2.Multiplier);
            Assert.AreEqual(10, ev2.SoldiersSacrificed);
        }
        
        [Test]
        public void ReachMultiplier_InsufficientTroops_ShouldFail()
        {
            var lane = CreateLane();
            var bus = new MockEventBus();
            lane.Initialize(bus);

            var squad = new MockSquad(3); // less than 5

            bool adv1 = lane.TryAdvance(squad);
            Assert.IsFalse(adv1);
            Assert.AreEqual(0, lane.CurrentMultiplier);
            Assert.AreEqual(3, squad.SquadCount); // No troops consumed
            Assert.AreEqual(0, bus.Events.Count);
        }

        [Test]
        public void ReachMultiplier_BeyondMaxMilestones_ShouldReturnFalseAndNotConsumeTroops()
        {
            var lane = CreateLane();
            var bus = new MockEventBus();
            lane.Initialize(bus);

            // 5 + 10 + 15 + 20 + 25 = 75 tropas para atingir x5
            var squad = new MockSquad(100);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(lane.TryAdvance(squad));
            }
            Assert.AreEqual(5, lane.CurrentMultiplier);
            Assert.AreEqual(25, squad.SquadCount); // 100 - 75 = 25

            // Tentativa além do máximo (x5)
            bool advBeyond = lane.TryAdvance(squad);
            Assert.IsFalse(advBeyond);
            Assert.AreEqual(5, lane.CurrentMultiplier);
            Assert.AreEqual(25, squad.SquadCount); // nenhuma tropa adicional consumida
        }
    }
}
