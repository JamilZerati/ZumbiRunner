using System;
using Game.Core;
using Game.Core.Status;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class StatusEffectControllerTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void StatusApplication_ThrowsOnNullSource()
        {
            Assert.Throws<ArgumentNullException>(() => new StatusApplication(StatusKind.Burn, 1, null));
        }

        [Test]
        public void StatusApplication_ThrowsOnZeroOrNegativeStacks()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatusApplication(StatusKind.Burn, 0, this));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatusApplication(StatusKind.Burn, -1, this));
        }

        [Test]
        public void Burn_SixTicksOfHalfSecond_DealsEighteenDamageAndExpires()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Burn, 1, this));

            for (int i = 0; i < 6; i++)
            {
                controller.Tick(0.5f);
            }

            Assert.AreEqual(22, health.CurrentHealth, "6 ticks of 3 damage should reduce health from 40 to 22.");
            Assert.IsFalse(controller.Has(StatusKind.Burn), "Burn should expire after 3 seconds.");
        }

        [Test]
        public void Burn_SingleLargeTick_DealsExactDurationDamage()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Burn, 1, this));
            controller.Tick(3f);

            Assert.AreEqual(22, health.CurrentHealth, "Single 3s tick should trigger 6 ticks (18 damage).");
            Assert.IsFalse(controller.Has(StatusKind.Burn));
        }

        [Test]
        public void Burn_TickExceedingDuration_DoesNotExceedMaxTicks()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Burn, 1, this));
            controller.Tick(10f);

            Assert.AreEqual(22, health.CurrentHealth, "Tick(10s) must not deal more than 6 ticks (18 damage).");
            Assert.IsFalse(controller.Has(StatusKind.Burn));
        }

        [Test]
        public void Burn_RefreshingEveryQuarterSecond_PreservesTickPhase()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Burn, 1, this));

            // Over 2 seconds, reapply every 0.25s (8 times).
            // With interval 0.5s and phase preserved, ticks fire at t=0.5, t=1.0, t=1.5, t=2.0 (4 ticks = 12 damage).
            for (int i = 0; i < 8; i++)
            {
                controller.Tick(0.25f);
                controller.Apply(new StatusApplication(StatusKind.Burn, 1, this));
            }

            Assert.AreEqual(28, health.CurrentHealth, "4 ticks should have fired over 2s despite frequent refresh.");
        }

        [Test]
        public void Burn_BeforeFirstInterval_DealsZeroDamage()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Burn, 1, this));
            controller.Tick(0.49f);

            Assert.AreEqual(40, health.CurrentHealth, "No damage should occur before 0.5s interval.");
            Assert.IsTrue(controller.Has(StatusKind.Burn));
        }

        [Test]
        public void Burn_ZeroOrNegativeInterval_DoesNotLoopOrDealDamage()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = new InMemoryStatusCatalog();
            catalog.Register(new BurnStatus { Duration = 3f, TickInterval = 0f, DamagePerTick = 3 });
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Burn, 1, this));
            controller.Tick(1f);

            Assert.AreEqual(40, health.CurrentHealth, "TickInterval <= 0 must be treated as inactive.");
        }

        [Test]
        public void Slow_HigherPotencyWins_AndExpiresAfterDuration()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Slow, 1, this));
            Assert.AreEqual(0.6f, controller.MoveSpeedMultiplier, Tolerance, "Slow 0.4 should result in 0.6 speed multiplier.");

            // Apply a weaker slow from test catalog
            var weakerSlowCatalog = new InMemoryStatusCatalog();
            weakerSlowCatalog.Register(new SlowStatus { Duration = 2f, SlowPercent = 0.2f });
            var controller2 = new StatusEffectController(health, receiver, weakerSlowCatalog);
            controller2.Apply(new StatusApplication(StatusKind.Slow, 1, this));
            Assert.AreEqual(0.8f, controller2.MoveSpeedMultiplier, Tolerance);

            // Reapplying weaker on first controller should keep 0.6
            controller.Apply(new StatusApplication(StatusKind.Slow, 1, "second_source"));
            Assert.AreEqual(0.6f, controller.MoveSpeedMultiplier, Tolerance, "Stronger slow must prevail.");

            controller.Tick(2f);
            Assert.AreEqual(1f, controller.MoveSpeedMultiplier, Tolerance, "Slow should expire after duration.");
            Assert.IsFalse(controller.Has(StatusKind.Slow));
        }
    }
}
