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

        [Test]
        public void ReachMultiplier_PayTroops_Every15m_ShouldSucceed()
        {
            var lane = new UnityEngine.GameObject().AddComponent<MultiplierLane>();
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
            var lane = new UnityEngine.GameObject().AddComponent<MultiplierLane>();
            var bus = new MockEventBus();
            lane.Initialize(bus);

            var squad = new MockSquad(3); // less than 5

            bool adv1 = lane.TryAdvance(squad);
            Assert.IsFalse(adv1);
            Assert.AreEqual(0, lane.CurrentMultiplier);
            Assert.AreEqual(3, squad.SquadCount); // No troops consumed
            Assert.AreEqual(0, bus.Events.Count);
        }
    }
}
