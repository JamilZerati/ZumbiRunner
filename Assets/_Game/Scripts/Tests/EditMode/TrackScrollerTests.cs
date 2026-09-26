using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class TrackScrollerTests
    {
        private GameObject testObject;
        private TrackScroller scroller;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestScroller");
            scroller = testObject.AddComponent<TrackScroller>();
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
        public void Step_AdvancesZPositionAndAccumulatesDistance()
        {
            scroller.ForwardSpeed = 10.0f;

            scroller.Step(0.5f);

            Assert.AreEqual(5.0f, scroller.DistanceTraveled, 1e-5f);
            Assert.AreEqual(5.0f, scroller.transform.position.z, 1e-5f);
        }

        [Test]
        public void Step_WhenPaused_DoesNotAdvance()
        {
            scroller.ForwardSpeed = 10.0f;
            scroller.IsPaused = true;

            scroller.Step(1.0f);

            Assert.AreEqual(0.0f, scroller.DistanceTraveled, 1e-5f);
            Assert.AreEqual(0.0f, scroller.transform.position.z, 1e-5f);
        }

        [Test]
        public void ResetDistance_ResetsDistanceTraveledToZero()
        {
            scroller.ForwardSpeed = 10.0f;
            scroller.Step(1.0f);
            Assert.AreEqual(10.0f, scroller.DistanceTraveled, 1e-5f);

            scroller.ResetDistance();

            Assert.AreEqual(0.0f, scroller.DistanceTraveled, 1e-5f);
        }
    }
}
