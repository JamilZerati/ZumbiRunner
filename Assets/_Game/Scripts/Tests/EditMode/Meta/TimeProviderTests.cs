using System;
using Game.Meta.Time;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class TimeProviderTests
    {
        [Test]
        public void InitialState_WhenCreatedWithDefaultProvider_NotManipulated()
        {
            var provider = new MonotonicTimeProvider();

            Assert.IsFalse(provider.IsClockManipulated);
            Assert.AreEqual(0, provider.LastSeenUtcTicks);
            Assert.That((DateTime.UtcNow - provider.UtcNow).TotalSeconds, Is.LessThan(2));
        }

        [Test]
        public void RecordCurrentTime_AdvancesLastSeenTicks()
        {
            var fixedTime = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
            var provider = new MonotonicTimeProvider(0, () => fixedTime);

            provider.RecordCurrentTime();

            Assert.AreEqual(fixedTime.Ticks, provider.LastSeenUtcTicks);
            Assert.IsFalse(provider.IsClockManipulated);
        }

        [Test]
        public void ForwardClockMovement_UpdatesTicks_AndClearsManipulation()
        {
            var baseTime = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
            DateTime currentTime = baseTime;
            var provider = new MonotonicTimeProvider(baseTime.Ticks, () => currentTime);

            currentTime = baseTime.AddHours(1);
            provider.RecordCurrentTime();

            Assert.AreEqual(currentTime.Ticks, provider.LastSeenUtcTicks);
            Assert.IsFalse(provider.IsClockManipulated);
        }

        [Test]
        public void SmallBackwardDrift_Within10Minutes_DoesNotFlagManipulation()
        {
            var baseTime = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
            DateTime currentTime = baseTime;
            var provider = new MonotonicTimeProvider(baseTime.Ticks, () => currentTime);

            currentTime = baseTime.Subtract(TimeSpan.FromMinutes(5));
            provider.RecordCurrentTime();

            Assert.IsFalse(provider.IsClockManipulated);
            Assert.AreEqual(baseTime.Ticks, provider.LastSeenUtcTicks);
        }

        [Test]
        public void BackwardJump_Exceeding10Minutes_FlagsManipulation()
        {
            var baseTime = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
            DateTime currentTime = baseTime;
            var provider = new MonotonicTimeProvider(baseTime.Ticks, () => currentTime);

            currentTime = baseTime.Subtract(TimeSpan.FromMinutes(15));

            Assert.IsTrue(provider.IsClockManipulated);

            provider.RecordCurrentTime();

            Assert.IsTrue(provider.IsClockManipulated);
            Assert.AreEqual(baseTime.Ticks, provider.LastSeenUtcTicks);
        }
    }
}
