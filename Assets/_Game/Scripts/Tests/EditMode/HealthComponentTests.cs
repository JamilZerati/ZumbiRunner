using System;
using Game.Core;
using Game.Core.Events;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class HealthComponentTests
    {
        private GameObject testObject;
        private HealthComponent health;
        private EventBus bus;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestHealthObject");
            health = testObject.AddComponent<HealthComponent>();
            bus = new EventBus();
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
        public void DamageInfo_ConstructorDefaultsAndProperties()
        {
            var defaultDamage = new DamageInfo(10);
            Assert.AreEqual(10, defaultDamage.Amount);
            Assert.AreEqual(DamageType.Physical, defaultDamage.Type);
            Assert.IsNull(defaultDamage.Source);

            var customSource = new object();
            var customDamage = new DamageInfo(25, DamageType.Fire, customSource);
            Assert.AreEqual(25, customDamage.Amount);
            Assert.AreEqual(DamageType.Fire, customDamage.Type);
            Assert.AreSame(customSource, customDamage.Source);
        }

        [Test]
        public void Initialize_SetsMaxHealthAndCurrentHealth_AndIsAliveIsTrue()
        {
            health.Initialize(100, bus);

            Assert.AreEqual(100, health.MaxHealth);
            Assert.AreEqual(100, health.CurrentHealth);
            Assert.IsTrue(health.IsAlive);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void Initialize_ZeroOrNegativeHealth_ClampsToZeroAndIsAliveIsFalse(int initialHealth)
        {
            health.Initialize(initialHealth, bus);

            Assert.AreEqual(0, health.MaxHealth);
            Assert.AreEqual(0, health.CurrentHealth);
            Assert.IsFalse(health.IsAlive);
        }

        [Test]
        public void TakeDamage_PositiveAmount_ReducesCurrentHealth()
        {
            health.Initialize(100, bus);

            health.TakeDamage(new DamageInfo(30));

            Assert.AreEqual(70, health.CurrentHealth);
            Assert.IsTrue(health.IsAlive);
        }

        [Test]
        public void TakeDamage_AmountExceedingCurrentHealth_ClampsToZeroAndIsAliveBecomesFalse()
        {
            health.Initialize(50, bus);

            health.TakeDamage(new DamageInfo(80));

            Assert.AreEqual(0, health.CurrentHealth);
            Assert.IsFalse(health.IsAlive);
        }

        [TestCase(0)]
        [TestCase(-5)]
        [TestCase(-50)]
        public void TakeDamage_ZeroOrNegativeAmount_IgnoredWithoutModifyingHealthOrPublishingEvents(int damageAmount)
        {
            health.Initialize(50, bus);
            int damageTakenCount = 0;
            int diedCount = 0;

            bus.Subscribe<DamageTakenEvent>(_ => damageTakenCount++);
            bus.Subscribe<EntityDiedEvent>(_ => diedCount++);

            health.TakeDamage(new DamageInfo(damageAmount));

            Assert.AreEqual(50, health.CurrentHealth);
            Assert.IsTrue(health.IsAlive);
            Assert.AreEqual(0, damageTakenCount);
            Assert.AreEqual(0, diedCount);
        }

        [Test]
        public void TakeDamage_PublishesDamageTakenEventWithAccuratePayload()
        {
            health.Initialize(100, bus);
            DamageTakenEvent? receivedEvent = null;
            bus.Subscribe<DamageTakenEvent>(evt => receivedEvent = evt);

            var source = new object();
            var damage = new DamageInfo(35, DamageType.Lightning, source);
            health.TakeDamage(damage);

            Assert.IsTrue(receivedEvent.HasValue);
            Assert.AreSame(health, receivedEvent.Value.Target);
            Assert.AreEqual(35, receivedEvent.Value.Damage.Amount);
            Assert.AreEqual(DamageType.Lightning, receivedEvent.Value.Damage.Type);
            Assert.AreSame(source, receivedEvent.Value.Damage.Source);
            Assert.AreEqual(65, receivedEvent.Value.RemainingHealth);
        }

        [Test]
        public void TakeDamage_LethalDamage_PublishesBothDamageTakenAndEntityDiedEvents()
        {
            health.Initialize(40, bus);
            DamageTakenEvent? damageEvent = null;
            EntityDiedEvent? deathEvent = null;

            bus.Subscribe<DamageTakenEvent>(evt => damageEvent = evt);
            bus.Subscribe<EntityDiedEvent>(evt => deathEvent = evt);

            var killer = new object();
            var damage = new DamageInfo(40, DamageType.Poison, killer);
            health.TakeDamage(damage);

            Assert.AreEqual(0, health.CurrentHealth);
            Assert.IsFalse(health.IsAlive);

            Assert.IsTrue(damageEvent.HasValue);
            Assert.AreEqual(0, damageEvent.Value.RemainingHealth);

            Assert.IsTrue(deathEvent.HasValue);
            Assert.AreSame(health, deathEvent.Value.Target);
            Assert.AreSame(killer, deathEvent.Value.Source);
        }

        [Test]
        public void TakeDamage_WhenAlreadyDead_DoesNotApplyDamageOrPublishEvents()
        {
            health.Initialize(30, bus);
            health.TakeDamage(new DamageInfo(30));

            int damageTakenCount = 0;
            int diedCount = 0;
            bus.Subscribe<DamageTakenEvent>(_ => damageTakenCount++);
            bus.Subscribe<EntityDiedEvent>(_ => diedCount++);

            health.TakeDamage(new DamageInfo(20));

            Assert.AreEqual(0, health.CurrentHealth);
            Assert.IsFalse(health.IsAlive);
            Assert.AreEqual(0, damageTakenCount);
            Assert.AreEqual(0, diedCount);
        }

        [Test]
        public void ResetHealth_RestoresHealthToMax_AndIsAliveReturnsToTrue()
        {
            health.Initialize(100, bus);
            health.TakeDamage(new DamageInfo(100));

            Assert.AreEqual(0, health.CurrentHealth);
            Assert.IsFalse(health.IsAlive);

            health.ResetHealth();

            Assert.AreEqual(100, health.CurrentHealth);
            Assert.AreEqual(100, health.MaxHealth);
            Assert.IsTrue(health.IsAlive);

            DamageTakenEvent? afterResetEvent = null;
            bus.Subscribe<DamageTakenEvent>(evt => afterResetEvent = evt);
            health.TakeDamage(new DamageInfo(15));

            Assert.AreEqual(85, health.CurrentHealth);
            Assert.IsTrue(afterResetEvent.HasValue);
            Assert.AreEqual(85, afterResetEvent.Value.RemainingHealth);
        }

        [Test]
        public void OperationsWithoutEventBus_SucceedWithoutException()
        {
            health.Initialize(50, null);

            Assert.DoesNotThrow(() =>
            {
                health.TakeDamage(new DamageInfo(20));
                health.TakeDamage(new DamageInfo(40));
                health.ResetHealth();
            });

            Assert.AreEqual(50, health.CurrentHealth);
            Assert.IsTrue(health.IsAlive);
        }

        [Test]
        public void IDamageable_InterfaceContract_ImplementedExplicitlyOrImplicitly()
        {
            IDamageable damageable = health;
            damageable.TakeDamage(new DamageInfo(10));
            Assert.AreEqual(damageable.CurrentHealth, health.CurrentHealth);
            Assert.AreEqual(damageable.MaxHealth, health.MaxHealth);
            Assert.AreEqual(damageable.IsAlive, health.IsAlive);
        }
    }
}
