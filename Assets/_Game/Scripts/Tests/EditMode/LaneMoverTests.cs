using Game.Core;
using Game.Core.Events;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class LaneMoverTests
    {
        private GameObject testObject;
        private LaneMover mover;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestMover");
            mover = testObject.AddComponent<LaneMover>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void Initialize_SetsPositionToCenterOfInitialLane()
        {
            var layout = new LaneLayout(2, 2.0f);
            mover.Initialize(layout, initialLane: 0);

            Assert.AreEqual(0, mover.CurrentLane);
            Assert.AreEqual(0, mover.TargetLane);
            Assert.AreEqual(-1.0f, mover.transform.position.x, 1e-5f);
        }

        [Test]
        public void RequestMove_ClampsAtLeftBoundary()
        {
            var layout = new LaneLayout(2, 2.0f);
            mover.Initialize(layout, initialLane: 0);

            bool moved = mover.RequestMove(-1);

            Assert.IsFalse(moved);
            Assert.AreEqual(0, mover.TargetLane);
        }

        [Test]
        public void RequestMove_ClampsAtRightBoundary()
        {
            var layout = new LaneLayout(2, 2.0f);
            mover.Initialize(layout, initialLane: 1);

            bool moved = mover.RequestMove(1);

            Assert.IsFalse(moved);
            Assert.AreEqual(1, mover.TargetLane);
        }

        [Test]
        public void RequestMove_TransitionsBetweenLanes_AndFiresEvent()
        {
            var bus = new EventBus();
            LaneChangedEvent receivedEvent = default;
            bool eventFired = false;
            bus.Subscribe<LaneChangedEvent>(evt =>
            {
                receivedEvent = evt;
                eventFired = true;
            });

            var layout = new LaneLayout(2, 2.0f);
            mover.Initialize(layout, bus: bus, initialLane: 0);

            bool moved = mover.RequestMove(1);

            Assert.IsTrue(moved);
            Assert.AreEqual(1, mover.TargetLane);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(0, receivedEvent.PreviousLane);
            Assert.AreEqual(1, receivedEvent.NewLane);
        }

        [Test]
        public void RequestMove_WorksWithThreeLanes()
        {
            var layout = new LaneLayout(3, 2.0f);
            mover.Initialize(layout, initialLane: 1);

            Assert.AreEqual(0.0f, mover.transform.position.x, 1e-5f);

            Assert.IsTrue(mover.RequestMove(-1));
            Assert.AreEqual(0, mover.TargetLane);

            Assert.IsTrue(mover.RequestMove(1));
            Assert.AreEqual(1, mover.TargetLane);

            Assert.IsTrue(mover.RequestMove(1));
            Assert.AreEqual(2, mover.TargetLane);
        }

        [Test]
        public void UpdatePosition_InterpolatesTowardsTargetLane()
        {
            var layout = new LaneLayout(2, 2.0f);
            mover.Initialize(layout, initialLane: 0); // X = -1.0
            mover.RequestMove(1); // Target X = 1.0

            // Speed is 12 m/s. Delta = 0.1s -> moves 1.2 units -> from -1.0 to 0.2
            mover.UpdatePosition(0.1f);
            Assert.AreEqual(0.2f, mover.transform.position.x, 1e-4f);
            Assert.AreEqual(0, mover.CurrentLane);

            // Another 0.1s -> moves another 1.2 units -> reaches target 1.0
            mover.UpdatePosition(0.1f);
            Assert.AreEqual(1.0f, mover.transform.position.x, 1e-4f);
            Assert.AreEqual(1, mover.CurrentLane);
        }
    }
}
