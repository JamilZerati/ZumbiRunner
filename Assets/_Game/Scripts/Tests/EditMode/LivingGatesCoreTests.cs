using System;
using Game.Core;
using Game.Core.Events;
using Game.Core.Gates;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class LivingGatesCoreTests
    {
        private sealed class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }

            public FakeSquad(int initialCount)
            {
                SquadCount = initialCount;
            }

            public bool Add(int amount)
            {
                if (amount <= 0)
                {
                    return false;
                }

                SquadCount += amount;
                return true;
            }

            public bool Remove(int amount)
            {
                if (amount <= 0)
                {
                    return false;
                }

                SquadCount = Math.Max(0, SquadCount - amount);
                return true;
            }

            public bool Multiply(int factor)
            {
                if (factor < 0)
                {
                    return false;
                }

                SquadCount = Math.Max(0, SquadCount * factor);
                return true;
            }

            public bool Divide(int divisor)
            {
                if (divisor <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(divisor));
                }

                SquadCount = Math.Max(0, SquadCount / divisor);
                return true;
            }

            public bool SetCount(int newCount)
            {
                SquadCount = Math.Max(0, newCount);
                return true;
            }
        }

        [Test]
        public void ShootToUpgrade_RegisterHit_AdvancesValueByStepAndClampsAtCap()
        {
            var variant = new ShootToUpgradeGateVariant(initialValue: -5, step: 1, hitsPerStep: 1, cap: 15);
            GateRuntimeState state = variant.CreateInitialState();

            Assert.AreEqual(GateVariantType.ShootToUpgrade, state.VariantType);
            Assert.IsTrue(state.AcceptsProjectiles);
            Assert.AreEqual(-5, state.CurrentValue);
            Assert.AreEqual("-5", state.DisplayText);
            Assert.IsTrue(state.IsNegative);

            variant.RegisterHit(state, 5);
            Assert.AreEqual(5, state.CurrentHits);
            Assert.AreEqual(0, state.CurrentValue);
            Assert.AreEqual("+0", state.DisplayText);
            Assert.IsFalse(state.IsNegative);

            variant.RegisterHit(state, 15);
            Assert.AreEqual(20, state.CurrentHits);
            Assert.AreEqual(15, state.CurrentValue);
            Assert.AreEqual("+15", state.DisplayText);

            variant.RegisterHit(state, 5);
            Assert.AreEqual(25, state.CurrentHits);
            Assert.AreEqual(15, state.CurrentValue);
            Assert.AreEqual("+15", state.DisplayText);
        }

        [Test]
        public void ShootToUpgrade_HitsPerStepGreaterThanOne_AdvancesOnlyOnCompletedSteps()
        {
            var variant = new ShootToUpgradeGateVariant(initialValue: -4, step: 2, hitsPerStep: 2, cap: 6);
            GateRuntimeState state = variant.CreateInitialState();

            variant.RegisterHit(state, 1);
            Assert.AreEqual(1, state.CurrentHits);
            Assert.AreEqual(-4, state.CurrentValue);
            Assert.AreEqual("-4", state.DisplayText);

            variant.RegisterHit(state, 1);
            Assert.AreEqual(2, state.CurrentHits);
            Assert.AreEqual(-2, state.CurrentValue);
            Assert.AreEqual("-2", state.DisplayText);

            variant.RegisterHit(state, 1);
            Assert.AreEqual(3, state.CurrentHits);
            Assert.AreEqual(-2, state.CurrentValue);

            variant.RegisterHit(state, 1);
            Assert.AreEqual(4, state.CurrentHits);
            Assert.AreEqual(0, state.CurrentValue);
            Assert.AreEqual("+0", state.DisplayText);
        }

        [Test]
        public void ShootToUpgrade_ApplyToSquad_WhenPositive_AddsEvolvedSoldierCount()
        {
            var variant = new ShootToUpgradeGateVariant(initialValue: -5, step: 1, hitsPerStep: 1, cap: 15);
            GateRuntimeState state = variant.CreateInitialState();
            variant.RegisterHit(state, 13);

            var squad = new FakeSquad(10);
            int appliedValue = variant.ApplyToSquad(squad, state);

            Assert.AreEqual(8, appliedValue);
            Assert.AreEqual(18, squad.SquadCount);
        }

        [Test]
        public void ShootToUpgrade_ApplyToSquad_WhenNegative_RemovesSoldiersButRespectsFloorOfOne()
        {
            var variant = new ShootToUpgradeGateVariant(initialValue: -5, step: 1, hitsPerStep: 1, cap: 15);
            GateRuntimeState state = variant.CreateInitialState();

            var squad = new FakeSquad(3);
            int appliedValue = variant.ApplyToSquad(squad, state);

            Assert.AreEqual(-5, appliedValue);
            Assert.AreEqual(1, squad.SquadCount);
        }

        [Test]
        public void CursedGateVariant_ActivateModifier_AppliesMultiplierUntilExactDurationDistanceAndThenExpires()
        {
            var modifierSpec = new StageModifierSpec("cursed_frenzy", enemySpeedMultiplier: 1.35f, enemyContactDamageMultiplier: 1.2f);
            var cursedVariant = new CursedGateVariant("perk_triple", modifierSpec, durationDistance: 100f);
            var controller = new StageModifierController();

            cursedVariant.ActivateModifier(controller, currentDistance: 50f);

            Assert.AreEqual(1, controller.ActiveCount);
            Assert.AreEqual(1.35f, controller.CurrentEnemySpeedMultiplier, 0.0001f);
            Assert.AreEqual(1.2f, controller.CurrentEnemyContactDamageMultiplier, 0.0001f);

            controller.TickDistance(149.9f);
            Assert.AreEqual(1, controller.ActiveCount);
            Assert.AreEqual(1.35f, controller.CurrentEnemySpeedMultiplier, 0.0001f);

            controller.TickDistance(150.0f);
            Assert.AreEqual(0, controller.ActiveCount);
            Assert.AreEqual(1.0f, controller.CurrentEnemySpeedMultiplier, 0.0001f);
            Assert.AreEqual(1.0f, controller.CurrentEnemyContactDamageMultiplier, 0.0001f);
        }

        [Test]
        public void NonUpgradeVariants_CreateInitialState_DoNotAcceptProjectiles_And_EventBus_DeliversGatePassedEvent()
        {
            var staticVariant = new StaticGateVariant();
            var cursedVariant = new CursedGateVariant("perk_triple", new StageModifierSpec("frenzy", 1.35f, 1.0f), 100f);
            var guardedVariant = new GuardedGateVariant("wave_guard_1", "zombie_runner", 6, 8f);

            GateRuntimeState staticState = staticVariant.CreateInitialState("+5");
            GateRuntimeState cursedState = cursedVariant.CreateInitialState("x3");
            GateRuntimeState guardedState = guardedVariant.CreateInitialState("x2");

            Assert.IsFalse(staticState.AcceptsProjectiles);
            Assert.IsFalse(cursedState.AcceptsProjectiles);
            Assert.IsTrue(cursedState.IsCursed);
            Assert.IsFalse(guardedState.AcceptsProjectiles);
            Assert.AreEqual("+5", staticState.DisplayText);
            Assert.AreEqual("x3", cursedState.DisplayText);
            Assert.AreEqual("x2", guardedState.DisplayText);

            var bus = new EventBus();
            GatePassedEvent? received = null;
            bus.Subscribe<GatePassedEvent>(evt => received = evt);

            bus.Publish(new GatePassedEvent("gate_upgrade_minus5_to_15", "perk_add_5", GateVariantType.ShootToUpgrade, 0, 8));

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual("gate_upgrade_minus5_to_15", received.Value.GateId);
            Assert.AreEqual("perk_add_5", received.Value.PerkId);
            Assert.AreEqual(GateVariantType.ShootToUpgrade, received.Value.Variant);
            Assert.AreEqual(0, received.Value.LaneIndex);
            Assert.AreEqual(8, received.Value.AppliedValue);
        }
    }
}
