using System;
using Game.Core;
using Game.Core.Events;
using Game.Core.Status;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class ShatterInteractionTests
    {
        [Test]
        public void EffectInteraction_Constructor_ValidatesArguments()
        {
            Assert.Throws<ArgumentException>(() => new EffectInteraction("", StatusKind.Frozen, InteractionTrigger.HeavyHit, 12, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EffectInteraction("s", StatusKind.Frozen, InteractionTrigger.HeavyHit, 0, 2f));
            Assert.Throws<ArgumentException>(() => new EffectInteraction("s", StatusKind.Frozen, InteractionTrigger.HeavyHit, 12, 1f));
            Assert.Throws<ArgumentException>(() => new EffectInteraction("s", StatusKind.Frozen, InteractionTrigger.HeavyHit, 12, 0.5f));
            Assert.Throws<ArgumentException>(() => new EffectInteraction("s", StatusKind.Frozen, InteractionTrigger.HeavyHit, 12, float.NaN));
        }

        [Test]
        public void Shatter_TargetFrozenAndHitAboveThreshold_DoublesDamageConsumesFrozenAndPublishesEvent()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = InMemoryInteractionTable.CreateDefault();
            var bus = new EventBus();

            SynergyTriggeredEvent? captured = null;
            bus.Subscribe<SynergyTriggeredEvent>(e => captured = e);

            var controller = new StatusEffectController(health, receiver, catalog, table, null, bus);
            receiver.Controller = controller;

            // Apply Frozen directly
            controller.Apply(new StatusApplication(StatusKind.Freeze, 2, this));
            Assert.IsTrue(controller.Has(StatusKind.Frozen));

            int dealt = controller.ResolveHit(new DamageInfo(13, DamageType.Physical, this), null);

            Assert.AreEqual(26, dealt, "13 damage * 2 multiplier = 26.");
            Assert.AreEqual(14, health.CurrentHealth, "40 - 26 = 14.");
            Assert.IsFalse(controller.Has(StatusKind.Frozen), "Frozen status must be consumed by shatter.");

            Assert.IsTrue(captured.HasValue, "SynergyTriggeredEvent must be published.");
            Assert.AreEqual("shatter", captured.Value.InteractionId);
            Assert.AreSame(receiver, captured.Value.Target);
            Assert.AreEqual(13, captured.Value.HitDamage);
            Assert.AreEqual(26, captured.Value.FinalDamage);
        }

        [Test]
        public void Shatter_TargetFrozenHitBelowThreshold_DealsNormalDamageAndKeepsFrozen()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = InMemoryInteractionTable.CreateDefault();
            var bus = new EventBus();

            bool eventFired = false;
            bus.Subscribe<SynergyTriggeredEvent>(_ => eventFired = true);

            var controller = new StatusEffectController(health, receiver, catalog, table, null, bus);

            controller.Apply(new StatusApplication(StatusKind.Freeze, 2, this));
            int dealt = controller.ResolveHit(new DamageInfo(11, DamageType.Physical, this), null);

            Assert.AreEqual(11, dealt, "Hit < 12 does not trigger shatter.");
            Assert.AreEqual(29, health.CurrentHealth);
            Assert.IsTrue(controller.Has(StatusKind.Frozen), "Frozen must remain.");
            Assert.IsFalse(eventFired);
        }

        [Test]
        public void Shatter_TargetNotFrozen_DealsNormalDamage()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = InMemoryInteractionTable.CreateDefault();

            var controller = new StatusEffectController(health, receiver, catalog, table);
            int dealt = controller.ResolveHit(new DamageInfo(13, DamageType.Physical, this), null);

            Assert.AreEqual(13, dealt);
            Assert.AreEqual(27, health.CurrentHealth);
        }

        [Test]
        public void Shatter_DoesNotTriggerOnSameHitThatCompletesFreezeThreshold()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = InMemoryInteractionTable.CreateDefault();

            var controller = new StatusEffectController(health, receiver, catalog, table);

            // Alvo começa com 1 pilha de Freeze (limiar é 2)
            controller.Apply(new StatusApplication(StatusKind.Freeze, 1, this));
            Assert.AreEqual(1, controller.GetStacks(StatusKind.Freeze));

            // Golpe de 13 carregando Freeze on-hit (vai completar o 2º stack)
            var onHit = new[] { new StatusApplication(StatusKind.Freeze, 1, this) };
            int firstHitDealt = controller.ResolveHit(new DamageInfo(13, DamageType.Physical, this), onHit);

            // Sinergias são avaliadas ANTES dos status on-hit: no momento do golpe, alvo NÃO estava Frozen!
            Assert.AreEqual(13, firstHitDealt, "Hit completing freeze must NOT trigger shatter on the same blow.");
            Assert.AreEqual(27, health.CurrentHealth);
            Assert.IsTrue(controller.Has(StatusKind.Frozen), "Target ends up Frozen from the hit's onHit application.");

            // Próximo golpe de 13 atinge alvo já Frozen -> agora sim estilhaça!
            int secondHitDealt = controller.ResolveHit(new DamageInfo(13, DamageType.Physical, this), null);
            Assert.AreEqual(26, secondHitDealt, "Subsequent hit shatters the Frozen target.");
            Assert.AreEqual(1, health.CurrentHealth);
            Assert.IsFalse(controller.Has(StatusKind.Frozen));
        }

        [Test]
        public void Shatter_MultiplierUsesAwayFromZeroRounding()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = new InMemoryInteractionTable();
            table.List.Add(new EffectInteraction("shatter_1_5", StatusKind.Frozen, InteractionTrigger.HeavyHit, 10, 1.5f));

            var controller = new StatusEffectController(health, receiver, catalog, table);
            controller.Apply(new StatusApplication(StatusKind.Freeze, 2, this));

            // 13 * 1.5 = 19.5 -> AwayFromZero rounds to 20 (Banker's rounding would round to 18 or 20)
            int dealt = controller.ResolveHit(new DamageInfo(13, DamageType.Physical, this), null);
            Assert.AreEqual(20, dealt, "19.5 must round AwayFromZero to 20.");
        }

        [Test]
        public void Shatter_OnlyFirstMatchingInteractionApplies()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var table = new InMemoryInteractionTable();
            table.List.Add(new EffectInteraction("first", StatusKind.Frozen, InteractionTrigger.HeavyHit, 12, 2f));
            table.List.Add(new EffectInteraction("second", StatusKind.Frozen, InteractionTrigger.HeavyHit, 12, 3f));

            var controller = new StatusEffectController(health, receiver, catalog, table);
            controller.Apply(new StatusApplication(StatusKind.Freeze, 2, this));

            int dealt = controller.ResolveHit(new DamageInfo(13, DamageType.Physical, this), null);
            Assert.AreEqual(26, dealt, "Only first matching interaction applies; must not multiply by second interaction.");
        }

        [Test]
        public void Shatter_NullOrEmptyInteractionTable_KeepsHitDamageUnmodified()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();

            var controller = new StatusEffectController(health, receiver, catalog, null);
            controller.Apply(new StatusApplication(StatusKind.Freeze, 2, this));

            int dealt = controller.ResolveHit(new DamageInfo(13, DamageType.Physical, this), null);
            Assert.AreEqual(13, dealt, "Null interaction table must not alter damage.");
        }
    }
}
