using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Time;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class GameTimeServiceTests
    {
        private class TestEventBus : IEventBus
        {
            public readonly List<object> PublishedEvents = new List<object>();

            public void Publish<T>(T eventData)
            {
                PublishedEvents.Add(eventData);
            }

            public System.IDisposable Subscribe<T>(System.Action<T> handler)
            {
                return null;
            }
        }

        [Test]
        public void GetDeltaTime_DefaultScales_ReturnsUnscaledDeltaTime()
        {
            var service = new GameTimeService();

            Assert.AreEqual(0.016f, service.GetDeltaTime(TimeTarget.Global, 0.016f), 1e-4f);
            Assert.AreEqual(0.016f, service.GetDeltaTime(TimeTarget.Ally, 0.016f), 1e-4f);
            Assert.AreEqual(0.016f, service.GetDeltaTime(TimeTarget.Enemy, 0.016f), 1e-4f);
        }

        [Test]
        public void GetDeltaTime_GlobalScaleModified_ScalesAllTargets()
        {
            var service = new GameTimeService { GlobalTimeScale = 0.5f };

            Assert.AreEqual(0.05f, service.GetDeltaTime(TimeTarget.Global, 0.1f), 1e-4f);
            Assert.AreEqual(0.05f, service.GetDeltaTime(TimeTarget.Ally, 0.1f), 1e-4f);
            Assert.AreEqual(0.05f, service.GetDeltaTime(TimeTarget.Enemy, 0.1f), 1e-4f);
        }

        [Test]
        public void GetDeltaTime_DecoupledScales_ScalesIndependently()
        {
            var service = new GameTimeService
            {
                GlobalTimeScale = 2.0f,
                AllyTimeScale = 1.5f,
                EnemyTimeScale = 0.25f
            };

            Assert.AreEqual(0.2f, service.GetDeltaTime(TimeTarget.Global, 0.1f), 1e-4f);
            Assert.AreEqual(0.3f, service.GetDeltaTime(TimeTarget.Ally, 0.1f), 1e-4f);
            Assert.AreEqual(0.05f, service.GetDeltaTime(TimeTarget.Enemy, 0.1f), 1e-4f);
        }

        [Test]
        public void ActivateBulletTime_AppliesScales_AndPublishesStartedEvent()
        {
            var bus = new TestEventBus();
            var service = new GameTimeService(bus);

            service.ActivateBulletTime(2.5f, enemyScale: 0.15f, allyScale: 1.1f);

            Assert.IsTrue(service.IsBulletTimeActive);
            Assert.AreEqual(2.5f, service.BulletTimeRemaining, 1e-4f);
            Assert.AreEqual(0.15f, service.EnemyTimeScale, 1e-4f);
            Assert.AreEqual(1.1f, service.AllyTimeScale, 1e-4f);

            Assert.AreEqual(1, bus.PublishedEvents.Count);
            Assert.IsInstanceOf<BulletTimeStartedEvent>(bus.PublishedEvents[0]);
            var evt = (BulletTimeStartedEvent)bus.PublishedEvents[0];
            Assert.AreEqual(2.5f, evt.DurationSeconds, 1e-4f);
            Assert.AreEqual(0.15f, evt.EnemyScale, 1e-4f);
            Assert.AreEqual(1.1f, evt.AllyScale, 1e-4f);
        }

        [Test]
        public void ActivateBulletTime_ZeroOrNegativeDuration_CancelsBulletTime()
        {
            var bus = new TestEventBus();
            var service = new GameTimeService(bus);

            service.ActivateBulletTime(2.0f);
            Assert.IsTrue(service.IsBulletTimeActive);

            service.ActivateBulletTime(0f);
            Assert.IsFalse(service.IsBulletTimeActive);
            Assert.AreEqual(0f, service.BulletTimeRemaining, 1e-4f);
            Assert.AreEqual(1.0f, service.EnemyTimeScale, 1e-4f);
        }

        [Test]
        public void Tick_WhenBulletTimeActive_DecrementsRemainingDuration()
        {
            var service = new GameTimeService();
            service.ActivateBulletTime(2.0f);

            service.Tick(0.5f);

            Assert.IsTrue(service.IsBulletTimeActive);
            Assert.AreEqual(1.5f, service.BulletTimeRemaining, 1e-4f);
        }

        [Test]
        public void Tick_WhenRemainingReachesZero_RestoresBaseScales_AndPublishesEndedEvent()
        {
            var bus = new TestEventBus();
            var service = new GameTimeService(bus);
            service.ActivateBulletTime(1.0f, enemyScale: 0.2f, allyScale: 1.0f);

            service.Tick(1.0f);

            Assert.IsFalse(service.IsBulletTimeActive);
            Assert.AreEqual(0f, service.BulletTimeRemaining, 1e-4f);
            Assert.AreEqual(1.0f, service.EnemyTimeScale, 1e-4f);
            Assert.AreEqual(1.0f, service.AllyTimeScale, 1e-4f);

            Assert.AreEqual(2, bus.PublishedEvents.Count);
            Assert.IsInstanceOf<BulletTimeStartedEvent>(bus.PublishedEvents[0]);
            Assert.IsInstanceOf<BulletTimeEndedEvent>(bus.PublishedEvents[1]);
        }

        [Test]
        public void Tick_WhenBulletTimeNotActive_DoesNothing()
        {
            var bus = new TestEventBus();
            var service = new GameTimeService(bus);

            service.Tick(1.0f);

            Assert.IsFalse(service.IsBulletTimeActive);
            Assert.AreEqual(0, bus.PublishedEvents.Count);
        }

        [Test]
        public void CancelBulletTime_WhenActive_RestoresBaseScales_AndPublishesEndedEvent()
        {
            var bus = new TestEventBus();
            var service = new GameTimeService(bus);
            service.ActivateBulletTime(3.0f, enemyScale: 0.3f);

            service.CancelBulletTime();

            Assert.IsFalse(service.IsBulletTimeActive);
            Assert.AreEqual(0f, service.BulletTimeRemaining, 1e-4f);
            Assert.AreEqual(1.0f, service.EnemyTimeScale, 1e-4f);

            Assert.AreEqual(2, bus.PublishedEvents.Count);
            Assert.IsInstanceOf<BulletTimeStartedEvent>(bus.PublishedEvents[0]);
            Assert.IsInstanceOf<BulletTimeEndedEvent>(bus.PublishedEvents[1]);
        }

        [Test]
        public void CancelBulletTime_WhenNotActive_DoesNotPublishEvent()
        {
            var bus = new TestEventBus();
            var service = new GameTimeService(bus);

            service.CancelBulletTime();

            Assert.AreEqual(0, bus.PublishedEvents.Count);
        }

        [Test]
        public void ActivateBulletTime_Idempotent_PreservesOriginalBaseScales()
        {
            var service = new GameTimeService
            {
                EnemyTimeScale = 0.8f,
                AllyTimeScale = 1.2f
            };

            service.ActivateBulletTime(2.0f, enemyScale: 0.2f, allyScale: 1.0f);
            service.Tick(1.0f);
            service.ActivateBulletTime(3.0f, enemyScale: 0.2f, allyScale: 1.0f);

            Assert.AreEqual(0.2f, service.EnemyTimeScale, 1e-4f);
            Assert.AreEqual(3.0f, service.BulletTimeRemaining, 1e-4f);

            service.Tick(3.0f);

            Assert.IsFalse(service.IsBulletTimeActive);
            Assert.AreEqual(0.8f, service.EnemyTimeScale, 1e-4f);
            Assert.AreEqual(1.2f, service.AllyTimeScale, 1e-4f);
        }
    }
}
