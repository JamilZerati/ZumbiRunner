using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class EventBusTests
    {
        private struct TestEventA
        {
            public int Value;
            public TestEventA(int value) => Value = value;
        }

        private struct TestEventB
        {
            public string Message;
            public TestEventB(string message) => Message = message;
        }

        [Test]
        public void Publish_InvokesSubscribedHandler()
        {
            var bus = new EventBus();
            int received = 0;

            using (bus.Subscribe<TestEventA>(e => received = e.Value))
            {
                bus.Publish(new TestEventA(42));
            }

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_StopsReceivingEvents()
        {
            var bus = new EventBus();
            int received = 0;

            var subscription = bus.Subscribe<TestEventA>(e => received = e.Value);
            bus.Publish(new TestEventA(10));
            Assert.AreEqual(10, received);

            subscription.Dispose();
            bus.Publish(new TestEventA(20));
            Assert.AreEqual(10, received);
        }

        [Test]
        public void Publish_DoesNotTriggerOtherEventTypes()
        {
            var bus = new EventBus();
            bool eventBTriggered = false;

            using (bus.Subscribe<TestEventB>(_ => eventBTriggered = true))
            {
                bus.Publish(new TestEventA(1));
            }

            Assert.IsFalse(eventBTriggered);
        }
    }
}
