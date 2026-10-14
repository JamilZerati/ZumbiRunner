using System;
using Game.Core;
using Game.Core.Events;
using Game.Core.Status;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class WeatherStatusTests
    {
        [Test]
        public void WetStatus_Lifecycle_OnApply_OnTick_AndExpiration()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            var wet = new WetStatus { Duration = 3f };
            Assert.AreEqual(StatusKind.Wet, wet.Kind);
            Assert.IsTrue(wet.IsPersistent);
            Assert.AreEqual(1f, wet.MoveSpeedMultiplier(null));

            controller.Apply(new StatusApplication(StatusKind.Wet, 1, this));
            Assert.IsTrue(controller.Has(StatusKind.Wet));
            Assert.AreEqual(1, controller.GetStacks(StatusKind.Wet));

            controller.Tick(2f);
            Assert.IsTrue(controller.Has(StatusKind.Wet));

            controller.Tick(3.5f);
            Assert.IsFalse(controller.Has(StatusKind.Wet));
        }

        [Test]
        public void WetStatus_NullArguments_DoNotThrowExceptions()
        {
            var wet = new WetStatus { Duration = 5f };
            Assert.DoesNotThrow(() => wet.OnApply(null, null, 1));
            Assert.DoesNotThrow(() => wet.OnTick(null, null, 0.5f));
            Assert.DoesNotThrow(() => wet.OnHostDied(null, null));
            Assert.AreEqual(1f, wet.MoveSpeedMultiplier(null));
        }

        [Test]
        public void Electrocute_DirectLightningDamage_DoublesDamage_ConsumesWet_AndPublishesSynergyEvent()
        {
            var health = new FakeHealth(50);
            var receiver = new FakeReceiver(50);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = InMemoryInteractionTable.CreateDefault();
            var bus = new EventBus();

            SynergyTriggeredEvent? captured = null;
            bus.Subscribe<SynergyTriggeredEvent>(e => captured = e);

            var controller = new StatusEffectController(health, receiver, catalog, table, null, bus);
            receiver.Controller = controller;

            controller.Apply(new StatusApplication(StatusKind.Wet, 1, this));
            Assert.IsTrue(controller.Has(StatusKind.Wet));

            int dealt = controller.ResolveHit(new DamageInfo(15, DamageType.Lightning, this), null);

            Assert.AreEqual(30, dealt, "15 lightning damage * 2.0x electrocute synergy = 30.");
            Assert.AreEqual(20, health.CurrentHealth, "50 - 30 = 20.");
            Assert.IsFalse(controller.Has(StatusKind.Wet), "Wet status must be consumed by electrocute.");

            Assert.IsTrue(captured.HasValue, "SynergyTriggeredEvent must be published.");
            Assert.AreEqual("electrocute", captured.Value.InteractionId);
            Assert.AreSame(receiver, captured.Value.Target);
            Assert.AreEqual(15, captured.Value.HitDamage);
            Assert.AreEqual(30, captured.Value.FinalDamage);
        }

        [Test]
        public void Electrocute_ShockOnHit_DoublesHitDamage_DoublesChainCountAndDamage_AndConsumesWet()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(50);
            var neighborA = new FakeReceiver(50);
            var neighborB = new FakeReceiver(50);
            var neighborC = new FakeReceiver(50);
            var neighborD = new FakeReceiver(50);

            neighborhood.Register(origin, 0f);
            neighborhood.Register(neighborA, 1f);
            neighborhood.Register(neighborB, 2f);
            neighborhood.Register(neighborC, 3f);
            neighborhood.Register(neighborD, 4f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = InMemoryInteractionTable.CreateDefault();
            var bus = new EventBus();

            SynergyTriggeredEvent? captured = null;
            bus.Subscribe<SynergyTriggeredEvent>(e => captured = e);

            var controller = new StatusEffectController(origin.Health, origin, catalog, table, neighborhood, bus);
            origin.Controller = controller;

            controller.Apply(new StatusApplication(StatusKind.Wet, 1, this));
            Assert.IsTrue(controller.Has(StatusKind.Wet));

            var onHit = new[] { new StatusApplication(StatusKind.Shock, 1, this) };
            int dealt = controller.ResolveHit(new DamageInfo(10, DamageType.Physical, this), onHit);

            Assert.AreEqual(20, dealt, "10 direct damage * 2.0x electrocute synergy = 20.");
            Assert.AreEqual(30, origin.Health.CurrentHealth);
            Assert.IsFalse(controller.Has(StatusKind.Wet), "Wet status must be consumed on origin.");

            Assert.AreEqual(1, neighborA.ReceivedDamage.Count);
            Assert.AreEqual(12, neighborA.ReceivedDamage[0].Amount, "Chain damage doubled to 12.");
            Assert.AreEqual(DamageType.Lightning, neighborA.ReceivedDamage[0].Type);

            Assert.AreEqual(1, neighborB.ReceivedDamage.Count);
            Assert.AreEqual(12, neighborB.ReceivedDamage[0].Amount);

            Assert.AreEqual(1, neighborC.ReceivedDamage.Count, "Neighbor C hit because ChainCount doubled to 4.");
            Assert.AreEqual(12, neighborC.ReceivedDamage[0].Amount);

            Assert.AreEqual(1, neighborD.ReceivedDamage.Count, "Neighbor D hit because ChainCount doubled to 4.");
            Assert.AreEqual(12, neighborD.ReceivedDamage[0].Amount);

            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual("electrocute", captured.Value.InteractionId);
        }

        [Test]
        public void Shock_WithoutWet_KeepsBaseDamageAndChainCount()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(50);
            var neighborA = new FakeReceiver(50);
            var neighborB = new FakeReceiver(50);
            var neighborC = new FakeReceiver(50);

            neighborhood.Register(origin, 0f);
            neighborhood.Register(neighborA, 1f);
            neighborhood.Register(neighborB, 2f);
            neighborhood.Register(neighborC, 3f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = InMemoryInteractionTable.CreateDefault();
            var bus = new EventBus();

            bool eventFired = false;
            bus.Subscribe<SynergyTriggeredEvent>(_ => eventFired = true);

            var controller = new StatusEffectController(origin.Health, origin, catalog, table, neighborhood, bus);
            origin.Controller = controller;

            var onHit = new[] { new StatusApplication(StatusKind.Shock, 1, this) };
            int dealt = controller.ResolveHit(new DamageInfo(10, DamageType.Physical, this), onHit);

            Assert.AreEqual(10, dealt, "Direct damage not multiplied without Wet.");
            Assert.AreEqual(40, origin.Health.CurrentHealth);

            Assert.AreEqual(1, neighborA.ReceivedDamage.Count);
            Assert.AreEqual(6, neighborA.ReceivedDamage[0].Amount);

            Assert.AreEqual(1, neighborB.ReceivedDamage.Count);
            Assert.AreEqual(6, neighborB.ReceivedDamage[0].Amount);

            Assert.AreEqual(0, neighborC.ReceivedDamage.Count, "Neighbor C not hit because ChainCount is 2.");
            Assert.IsFalse(eventFired, "No synergy event when Wet is not present.");
        }

        [Test]
        public void Electrocute_DirectShockApply_DoublesChainAndConsumesWet()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(50);
            var neighborA = new FakeReceiver(50);
            var neighborB = new FakeReceiver(50);
            var neighborC = new FakeReceiver(50);
            var neighborD = new FakeReceiver(50);

            neighborhood.Register(origin, 0f);
            neighborhood.Register(neighborA, 1f);
            neighborhood.Register(neighborB, 2f);
            neighborhood.Register(neighborC, 3f);
            neighborhood.Register(neighborD, 4f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(origin.Health, origin, catalog, null, neighborhood);
            origin.Controller = controller;

            controller.Apply(new StatusApplication(StatusKind.Wet, 1, this));
            Assert.IsTrue(controller.Has(StatusKind.Wet));

            controller.Apply(new StatusApplication(StatusKind.Shock, 1, this));

            Assert.IsFalse(controller.Has(StatusKind.Wet), "Wet consumed by ShockStatus.OnApply.");
            Assert.AreEqual(1, neighborA.ReceivedDamage.Count);
            Assert.AreEqual(12, neighborA.ReceivedDamage[0].Amount);
            Assert.AreEqual(1, neighborD.ReceivedDamage.Count, "4 targets reached due to doubled chain count.");
        }

        [Test]
        public void Electrocute_AtomicConsumption_WetNeighborReceivesElectrocuteDamageWithoutRecursiveChain()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(50);
            var neighborWet = new FakeReceiver(50);
            var neighborFar = new FakeReceiver(50);

            neighborhood.Register(origin, 0f);
            neighborhood.Register(neighborWet, 1f);
            neighborhood.Register(neighborFar, 2f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = InMemoryInteractionTable.CreateDefault();
            var bus = new EventBus();

            int synergyCount = 0;
            bus.Subscribe<SynergyTriggeredEvent>(_ => synergyCount++);

            var originController = new StatusEffectController(origin.Health, origin, catalog, table, neighborhood, bus);
            origin.Controller = originController;

            var neighborController = new StatusEffectController(neighborWet.Health, neighborWet, catalog, table, neighborhood, bus);
            neighborWet.Controller = neighborController;

            originController.Apply(new StatusApplication(StatusKind.Wet, 1, this));
            neighborController.Apply(new StatusApplication(StatusKind.Wet, 1, this));

            var onHit = new[] { new StatusApplication(StatusKind.Shock, 1, this) };
            originController.ResolveHit(new DamageInfo(10, DamageType.Physical, this), onHit);

            Assert.IsFalse(originController.Has(StatusKind.Wet));

            Assert.AreEqual(1, neighborWet.ReceivedDamage.Count);
            Assert.AreEqual(26, neighborWet.Health.CurrentHealth, "50 - 24 = 26.");
            Assert.IsFalse(neighborController.Has(StatusKind.Wet), "Neighbor's Wet consumed.");

            Assert.AreEqual(1, neighborFar.ReceivedDamage.Count);
            Assert.AreEqual(2, synergyCount, "Two electrocute synergies fired (one on origin, one on wet neighbor).");
        }

        [Test]
        public void EffectInteraction_Constructor_SupportsShockHitTrigger()
        {
            var interaction = new EffectInteraction("electrocute", StatusKind.Wet, InteractionTrigger.ShockHit, 1, 2f);
            Assert.AreEqual("electrocute", interaction.Id);
            Assert.AreEqual(StatusKind.Wet, interaction.RequiredStatus);
            Assert.AreEqual(InteractionTrigger.ShockHit, interaction.Trigger);
            Assert.AreEqual(1, interaction.MinHitDamage);
            Assert.AreEqual(2f, interaction.DamageMultiplier);
        }
    }
}
