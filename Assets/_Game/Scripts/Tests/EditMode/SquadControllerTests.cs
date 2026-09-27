using System;
using System.Reflection;
using Game.Core;
using Game.Core.Events;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class SquadControllerTests
    {
        private GameObject testObject;
        private SquadController controller;
        private EventBus bus;
        private SquadSizeChangedEvent lastEvent;
        private int eventCount;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestSquadController");
            controller = testObject.AddComponent<SquadController>();
            bus = new EventBus();
            lastEvent = default;
            eventCount = 0;
            bus.Subscribe<SquadSizeChangedEvent>(evt =>
            {
                lastEvent = evt;
                eventCount++;
            });
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
        public void Awake_InitializesSquadCountWithDefaultInitialCount()
        {
            var go = new GameObject("AwakeTest");
            try
            {
                var comp = go.AddComponent<SquadController>();
                typeof(SquadController)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(comp, null);
                Assert.AreEqual(1, comp.SquadCount);
                Assert.IsTrue(comp.IsAlive);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Initialize_SetsValidSquadCount_AndIsAliveIsTrue()
        {
            controller.Initialize(10, bus);

            Assert.AreEqual(10, controller.SquadCount);
            Assert.IsTrue(controller.IsAlive);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Initialize_ClampsNegativeCountToZero_AndIsAliveIsFalse()
        {
            controller.Initialize(-5, bus);

            Assert.AreEqual(0, controller.SquadCount);
            Assert.IsFalse(controller.IsAlive);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Add_PositiveAmount_IncrementsSquadCountAndPublishesEvent()
        {
            controller.Initialize(5, bus);

            Assert.IsTrue(controller.Add(3));
            Assert.AreEqual(8, controller.SquadCount);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(new SquadSizeChangedEvent(5, 8), lastEvent);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-10)]
        public void Add_ZeroOrNegativeAmount_ReturnsFalseAndDoesNotPublishEvent(int amount)
        {
            controller.Initialize(5, bus);

            Assert.IsFalse(controller.Add(amount));
            Assert.AreEqual(5, controller.SquadCount);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Remove_PositiveAmount_DecrementsSquadCountAndPublishesEvent()
        {
            controller.Initialize(5, bus);

            Assert.IsTrue(controller.Remove(2));
            Assert.AreEqual(3, controller.SquadCount);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(new SquadSizeChangedEvent(5, 3), lastEvent);
        }

        [Test]
        public void Remove_AmountGreaterThanSquadCount_ClampsToZeroAndPublishesEvent()
        {
            controller.Initialize(5, bus);

            Assert.IsTrue(controller.Remove(10));
            Assert.AreEqual(0, controller.SquadCount);
            Assert.IsFalse(controller.IsAlive);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(new SquadSizeChangedEvent(5, 0), lastEvent);
        }

        [Test]
        public void Remove_WhenAlreadyZero_ReturnsFalseAndDoesNotPublishEvent()
        {
            controller.Initialize(0, bus);

            Assert.IsFalse(controller.Remove(5));
            Assert.AreEqual(0, controller.SquadCount);
            Assert.AreEqual(0, eventCount);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Remove_ZeroOrNegativeAmount_ReturnsFalseAndDoesNotPublishEvent(int amount)
        {
            controller.Initialize(5, bus);

            Assert.IsFalse(controller.Remove(amount));
            Assert.AreEqual(5, controller.SquadCount);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Multiply_PositiveFactor_UpdatesCountAndPublishesEvent()
        {
            controller.Initialize(4, bus);

            Assert.IsTrue(controller.Multiply(3));
            Assert.AreEqual(12, controller.SquadCount);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(new SquadSizeChangedEvent(4, 12), lastEvent);
        }

        [Test]
        public void Multiply_ZeroFactor_ReducesToZeroAndPublishesEvent()
        {
            controller.Initialize(5, bus);

            Assert.IsTrue(controller.Multiply(0));
            Assert.AreEqual(0, controller.SquadCount);
            Assert.IsFalse(controller.IsAlive);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(new SquadSizeChangedEvent(5, 0), lastEvent);
        }

        [Test]
        public void Multiply_FactorOne_ReturnsFalseAndDoesNotPublishEvent()
        {
            controller.Initialize(5, bus);

            Assert.IsFalse(controller.Multiply(1));
            Assert.AreEqual(5, controller.SquadCount);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Multiply_NegativeFactor_ReturnsFalseAndDoesNotPublishEvent()
        {
            controller.Initialize(5, bus);

            Assert.IsFalse(controller.Multiply(-2));
            Assert.AreEqual(5, controller.SquadCount);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Divide_PositiveDivisor_UpdatesCountAndPublishesEvent()
        {
            controller.Initialize(9, bus);

            Assert.IsTrue(controller.Divide(2));
            Assert.AreEqual(4, controller.SquadCount);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(new SquadSizeChangedEvent(9, 4), lastEvent);
        }

        [Test]
        public void Divide_DivisorOne_ReturnsFalseAndDoesNotPublishEvent()
        {
            controller.Initialize(5, bus);

            Assert.IsFalse(controller.Divide(1));
            Assert.AreEqual(5, controller.SquadCount);
            Assert.AreEqual(0, eventCount);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-5)]
        public void Divide_ZeroOrNegativeDivisor_ThrowsArgumentOutOfRangeException(int divisor)
        {
            controller.Initialize(5, bus);

            Assert.Throws<ArgumentOutOfRangeException>(() => controller.Divide(divisor));
            Assert.AreEqual(5, controller.SquadCount);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void SetCount_ClampsToZeroAndPublishesEventIfChanged()
        {
            controller.Initialize(5, bus);

            Assert.IsTrue(controller.SetCount(15));
            Assert.AreEqual(15, controller.SquadCount);
            Assert.AreEqual(new SquadSizeChangedEvent(5, 15), lastEvent);

            Assert.IsTrue(controller.SetCount(-8));
            Assert.AreEqual(0, controller.SquadCount);
            Assert.AreEqual(new SquadSizeChangedEvent(15, 0), lastEvent);

            Assert.IsFalse(controller.SetCount(0));
            Assert.AreEqual(2, eventCount);
        }

        [Test]
        public void OperationsWithoutEventBus_SucceedWithoutException()
        {
            controller.Initialize(5, null);

            Assert.DoesNotThrow(() =>
            {
                Assert.IsTrue(controller.Add(3));
                Assert.IsTrue(controller.Remove(2));
                Assert.IsTrue(controller.Multiply(2));
                Assert.IsTrue(controller.Divide(2));
                Assert.IsTrue(controller.SetCount(10));
            });

            Assert.AreEqual(10, controller.SquadCount);
        }

        [Test]
        public void SquadSizeChangedEvent_ImplementsEqualityCorrectly()
        {
            var evt1 = new SquadSizeChangedEvent(1, 5);
            var evt2 = new SquadSizeChangedEvent(1, 5);
            var evt3 = new SquadSizeChangedEvent(2, 5);
            var evt4 = new SquadSizeChangedEvent(1, 6);

            Assert.IsTrue(evt1.Equals(evt2));
            Assert.IsTrue(evt1 == evt2);
            Assert.IsFalse(evt1 != evt2);
            Assert.AreEqual(evt1.GetHashCode(), evt2.GetHashCode());

            Assert.IsFalse(evt1.Equals(evt3));
            Assert.IsTrue(evt1 != evt3);
            Assert.IsFalse(evt1.Equals(evt4));
            Assert.IsTrue(evt1 != evt4);

            Assert.IsFalse(evt1.Equals((object)"other"));
            Assert.AreEqual("SquadSizeChangedEvent(1 -> 5)", evt1.ToString());
        }
    }
}
