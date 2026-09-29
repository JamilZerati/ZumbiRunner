using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Status;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class FreezeShockPoisonTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Freeze_AccumulatesUntilThreshold_ThenConvertsToFrozenStun()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Freeze, 1, this));
            Assert.AreEqual(1, controller.GetStacks(StatusKind.Freeze));
            Assert.AreEqual(1f, controller.MoveSpeedMultiplier, Tolerance);
            Assert.IsFalse(controller.IsStunned);

            controller.Apply(new StatusApplication(StatusKind.Freeze, 1, this));
            Assert.IsFalse(controller.Has(StatusKind.Freeze), "Freeze stacks must be removed when Frozen triggers.");
            Assert.IsTrue(controller.Has(StatusKind.Frozen), "Frozen status must be applied.");
            Assert.AreEqual(0f, controller.MoveSpeedMultiplier, Tolerance, "Frozen target cannot move.");
            Assert.IsTrue(controller.IsStunned);
        }

        [Test]
        public void Freeze_AppliedWhileFrozen_IsIgnored()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Freeze, 2, this));
            Assert.IsTrue(controller.Has(StatusKind.Frozen));

            controller.Apply(new StatusApplication(StatusKind.Freeze, 1, this));
            Assert.IsFalse(controller.Has(StatusKind.Freeze), "Freeze application during Frozen must be ignored.");

            controller.Tick(1.5f);
            Assert.IsFalse(controller.Has(StatusKind.Frozen), "Frozen must expire after 1.5s.");
            Assert.IsFalse(controller.Has(StatusKind.Freeze), "No lingering Freeze stacks after Frozen expires.");
            Assert.AreEqual(1f, controller.MoveSpeedMultiplier, Tolerance);
        }

        [Test]
        public void Freeze_SingleStack_ExpiresAfterDuration()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Freeze, 1, this));
            controller.Tick(3f);

            Assert.IsFalse(controller.Has(StatusKind.Freeze));
            Assert.AreEqual(0, controller.GetStacks(StatusKind.Freeze));
        }

        [Test]
        public void Frozen_WithSlowApplied_KeepsZeroSpeedMultiplier()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            controller.Apply(new StatusApplication(StatusKind.Freeze, 2, this));
            controller.Apply(new StatusApplication(StatusKind.Slow, 1, this));

            Assert.AreEqual(0f, controller.MoveSpeedMultiplier, Tolerance, "Frozen stun must dominate slow.");
        }

        [Test]
        public void Shock_ChainsToNearestLivingNeighborsWithoutRecursion()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(40);
            var neighborA = new FakeReceiver(40);
            var neighborB = new FakeReceiver(40);
            var neighborC = new FakeReceiver(40);
            var neighborD = new FakeReceiver(40);

            neighborhood.Register(origin, 0f);
            neighborhood.Register(neighborA, 1f);
            neighborhood.Register(neighborB, 2f);
            neighborhood.Register(neighborC, 3f);
            neighborhood.Register(neighborD, 5f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(origin.Health, origin, catalog, null, neighborhood);
            origin.Controller = controller;

            var onHit = new[] { new StatusApplication(StatusKind.Shock, 1, this) };
            int dealt = controller.ResolveHit(new DamageInfo(10, DamageType.Physical, this), onHit);

            Assert.AreEqual(10, dealt, "Origin takes the direct hit damage.");
            Assert.AreEqual(30, origin.Health.CurrentHealth);

            Assert.AreEqual(1, neighborA.ReceivedDamage.Count, "A is in chain count (closest).");
            Assert.AreEqual(6, neighborA.ReceivedDamage[0].Amount);
            Assert.AreEqual(DamageType.Lightning, neighborA.ReceivedDamage[0].Type);
            Assert.IsNull(neighborA.ReceivedOnHit[0], "Chain hit must not carry on-hit status.");

            Assert.AreEqual(1, neighborB.ReceivedDamage.Count, "B is in chain count.");
            Assert.AreEqual(6, neighborB.ReceivedDamage[0].Amount);

            Assert.AreEqual(0, neighborC.ReceivedDamage.Count, "C exceeds chain count limit of 2.");
            Assert.AreEqual(0, neighborD.ReceivedDamage.Count, "D exceeds radius and count.");
        }

        [Test]
        public void Shock_SkipsDeadNeighborAndChainsToNext()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(40);
            var deadA = new FakeReceiver(0);
            var livingB = new FakeReceiver(40);
            var livingC = new FakeReceiver(40);

            neighborhood.Register(origin, 0f);
            neighborhood.Register(deadA, 1f);
            neighborhood.Register(livingB, 2f);
            neighborhood.Register(livingC, 3f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(origin.Health, origin, catalog, null, neighborhood);

            var onHit = new[] { new StatusApplication(StatusKind.Shock, 1, this) };
            controller.ResolveHit(new DamageInfo(10, DamageType.Physical, this), onHit);

            Assert.AreEqual(0, deadA.ReceivedDamage.Count, "Dead neighbor must be ignored.");
            Assert.AreEqual(1, livingB.ReceivedDamage.Count);
            Assert.AreEqual(1, livingC.ReceivedDamage.Count);
        }

        [Test]
        public void Shock_HitThatKillsOrigin_DoesNotChain()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(10);
            var neighborA = new FakeReceiver(40);

            neighborhood.Register(origin, 0f);
            neighborhood.Register(neighborA, 1f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(origin.Health, origin, catalog, null, neighborhood);

            var onHit = new[] { new StatusApplication(StatusKind.Shock, 1, this) };
            controller.ResolveHit(new DamageInfo(10, DamageType.Physical, this), onHit);

            Assert.AreEqual(0, origin.Health.CurrentHealth);
            Assert.AreEqual(0, neighborA.ReceivedDamage.Count, "Lethal hit on origin does not chain shock.");
        }

        [Test]
        public void Poison_StacksCappedAtMaxStacks_AndTickingMultiplied()
        {
            var health = new FakeHealth(40);
            var receiver = new FakeReceiver(40);
            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(health, receiver, catalog);

            for (int i = 0; i < 7; i++)
            {
                controller.Apply(new StatusApplication(StatusKind.Poison, 1, this));
            }

            Assert.AreEqual(5, controller.GetStacks(StatusKind.Poison), "Poison stacks must cap at 5.");

            controller.Tick(1f);
            Assert.AreEqual(35, health.CurrentHealth, "5 stacks * 1 damage/tick = 5 damage.");

            controller.Tick(4f);
            Assert.IsFalse(controller.Has(StatusKind.Poison), "All stacks must expire after duration.");
        }

        [Test]
        public void Poison_HostDeathWithStacks_ExplodesToNearbyNeighbors()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(40);
            var inRadius = new FakeReceiver(40);
            var outOfRadius = new FakeReceiver(40);

            neighborhood.Register(origin, 0f);
            neighborhood.Register(inRadius, 2f);
            neighborhood.Register(outOfRadius, 3f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var controller = new StatusEffectController(origin.Health, origin, catalog, null, neighborhood);
            origin.Controller = controller;

            for (int i = 0; i < 5; i++)
            {
                controller.Apply(new StatusApplication(StatusKind.Poison, 1, this));
            }

            // Fatal blow to origin
            controller.ResolveHit(new DamageInfo(40, DamageType.Physical, this), null);

            Assert.IsFalse(origin.IsAlive);
            Assert.AreEqual(1, inRadius.ReceivedDamage.Count, "Neighbor in 2.5 radius must receive explosion.");
            Assert.AreEqual(20, inRadius.ReceivedDamage[0].Amount, "5 stacks * 4 = 20 explosion damage.");
            Assert.AreEqual(0, outOfRadius.ReceivedDamage.Count, "Neighbor at distance 3 is outside 2.5 radius.");
        }

        [Test]
        public void Poison_ExplosionCascade_TerminatesSafely()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(40);
            var neighborA = new FakeReceiver(20);
            var neighborB = new FakeReceiver(40);

            neighborhood.Register(origin, 0f);
            neighborhood.Register(neighborA, 2f);
            neighborhood.Register(neighborB, 4f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var originCtrl = new StatusEffectController(origin.Health, origin, catalog, null, neighborhood);
            origin.Controller = originCtrl;

            var aCtrl = new StatusEffectController(neighborA.Health, neighborA, catalog, null, neighborhood);
            neighborA.Controller = aCtrl;

            // Origin has 5 poison stacks
            for (int i = 0; i < 5; i++) originCtrl.Apply(new StatusApplication(StatusKind.Poison, 1, this));

            // NeighborA has 2 poison stacks and 20 HP
            for (int i = 0; i < 2; i++) aCtrl.Apply(new StatusApplication(StatusKind.Poison, 1, this));

            // Kill origin -> explodes 20 on A -> kills A -> A explodes 8 on B
            origin.ReceiveHit(new DamageInfo(40, DamageType.Physical, this), null);

            Assert.IsFalse(neighborA.IsAlive, "NeighborA must die from origin's explosion.");
            Assert.AreEqual(1, neighborB.ReceivedDamage.Count, "NeighborB receives cascaded explosion from A.");
            Assert.AreEqual(8, neighborB.ReceivedDamage[0].Amount, "2 stacks * 4 = 8 explosion damage.");
            Assert.AreEqual(1, origin.ReceivedDamage.Count, "Dead origin must NOT be hit back by A's explosion.");
        }

        [Test]
        public void Reentrancy_ShockKillsPoisonedNeighbor_ExplosionHitsOriginSafely()
        {
            var neighborhood = new FakeNeighborhood();
            var origin = new FakeReceiver(40);
            var neighbor = new FakeReceiver(6); // exactly 6 HP, dies from shock

            neighborhood.Register(origin, 0f);
            neighborhood.Register(neighbor, 1f);

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var originCtrl = new StatusEffectController(origin.Health, origin, catalog, null, neighborhood);
            origin.Controller = originCtrl;

            var neighborCtrl = new StatusEffectController(neighbor.Health, neighbor, catalog, null, neighborhood);
            neighbor.Controller = neighborCtrl;

            // Neighbor has 2 stacks of poison
            neighborCtrl.Apply(new StatusApplication(StatusKind.Poison, 2, this));

            // Shock on origin -> chains 6 to neighbor -> neighbor dies and explodes 8 onto living origin
            var onHit = new[] { new StatusApplication(StatusKind.Shock, 1, this) };
            Assert.DoesNotThrow(() => originCtrl.ResolveHit(new DamageInfo(10, DamageType.Physical, this), onHit));

            Assert.IsFalse(neighbor.IsAlive);
            // Origin took 10 direct + 8 from neighbor's explosion = 18 total damage (40 - 18 = 22)
            Assert.AreEqual(22, origin.Health.CurrentHealth, "Origin must take direct hit and neighbor's reentrant explosion damage.");
        }
    }
}
