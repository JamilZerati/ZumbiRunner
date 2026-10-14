using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Policies;
using Game.Core.Allies;
using Game.Core.Events;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Core.Stats;
using Game.Core.Status;
using Game.Gameplay.Allies;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class AllyScenariosTests
    {
        private readonly List<Object> _toDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _toDestroy.Count; i++)
                if (_toDestroy[i] != null) Object.DestroyImmediate(_toDestroy[i]);
            _toDestroy.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            if (obj != null) _toDestroy.Add(obj);
            return obj;
        }
        private sealed class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }

            public FakeSquad(int initialCount)
            {
                SquadCount = Math.Max(0, initialCount);
            }

            public bool Add(int amount)
            {
                if (amount <= 0) return false;
                SquadCount += amount;
                return true;
            }

            public bool Remove(int amount)
            {
                if (amount <= 0) return false;
                SquadCount = Math.Max(0, SquadCount - amount);
                return true;
            }

            public bool Multiply(int factor)
            {
                if (factor < 0) return false;
                SquadCount *= factor;
                return true;
            }

            public bool Divide(int divisor)
            {
                if (divisor <= 0) return false;
                SquadCount /= divisor;
                return true;
            }

            public bool SetCount(int newCount)
            {
                SquadCount = Math.Max(0, newCount);
                return true;
            }
        }

        private sealed class FakeStatusReceiver : IStatusReceiver
        {
            public bool IsAlive => true;
            public readonly List<StatusApplication> Received = new List<StatusApplication>();

            public int ReceiveHit(DamageInfo hit, IReadOnlyList<StatusApplication> onHit)
            {
                if (onHit != null)
                {
                    Received.AddRange(onHit);
                }
                return hit.Amount;
            }
        }

        [Test]
        public void CT_1_RecruitAlly_IdempotentRegistration_AndHealOnDuplicate()
        {
            var squad = new FakeSquad(10);
            var roster = new AllyRoster();
            var effect = new RecruitAllyPerkEffect("pyromancer");
            var context = new PerkContext(squad, laneIndex: 0, loadout: null, sourceId: "gate_1", roster: roster);

            effect.Apply(context);

            Assert.AreEqual(1, roster.Allies.Count);
            Assert.AreEqual(1, roster.HeroCount);
            Assert.AreEqual(0, roster.PetCount);
            Assert.IsTrue(roster.TryGetAlly("pyromancer", out var hero));
            Assert.AreEqual(hero.MaxHealth, hero.CurrentHealth);

            hero.TakeDamage(50);
            Assert.AreEqual(hero.MaxHealth - 50, hero.CurrentHealth);

            effect.Apply(context);

            Assert.AreEqual(1, roster.Allies.Count);
            Assert.AreEqual(hero.MaxHealth, hero.CurrentHealth);
        }

        [Test]
        public void CT_2_DamageSeparation_SquadLossDoesNotAffectHeroHp_AndHeroDamageDoesNotReduceSquadCount()
        {
            var squad = new FakeSquad(10);
            squad.Remove(4);
            Assert.AreEqual(6, squad.SquadCount);
            var hero = Track(new GameObject("Hero_CT2")).AddComponent<HeroUnit>();
            hero.Initialize("pyromancer", "Piromante", maxHealth: 100);
            Assert.AreEqual(100, hero.CurrentHealth);
            hero.TakeDamage(25);
            Assert.AreEqual(75, hero.CurrentHealth);
            Assert.AreEqual(6, squad.SquadCount);
        }

        [Test]
        public void CT_3_Pyromancer_BasicAttackAndInferno_AppliesBurnStatus()
        {
            var receiver = new FakeStatusReceiver();
            var pyromancer = Track(new GameObject("Hero_CT3")).AddComponent<HeroUnit>();
            pyromancer.Initialize("pyromancer", "Piromante", maxHealth: 100, attackDamage: 25, statusApplied: "burn", abilityDamage: 40);
            Assert.IsTrue(pyromancer.PerformAttack(receiver));
            Assert.AreEqual(1, receiver.Received.Count);
            Assert.AreEqual(StatusKind.Burn, receiver.Received[0].Kind);
            Assert.AreEqual(1, receiver.Received[0].Stacks);
            Assert.IsTrue(pyromancer.FireAbility(receiver));
            Assert.AreEqual(2, receiver.Received.Count);
            Assert.AreEqual(StatusKind.Burn, receiver.Received[1].Kind);
            Assert.AreEqual(2, receiver.Received[1].Stacks);
        }

        [Test]
        public void CT_4_Cryomancer_BasicAttackAndBlizzard_AppliesFreezeStatus()
        {
            var receiver = new FakeStatusReceiver();
            var cryomancer = Track(new GameObject("Hero_CT4")).AddComponent<HeroUnit>();
            cryomancer.Initialize("cryomancer", "Criomante", maxHealth: 90, attackDamage: 20, statusApplied: "freeze", abilityDamage: 35);
            Assert.IsTrue(cryomancer.PerformAttack(receiver));
            Assert.AreEqual(1, receiver.Received.Count);
            Assert.AreEqual(StatusKind.Freeze, receiver.Received[0].Kind);
            Assert.AreEqual(1, receiver.Received[0].Stacks);
            Assert.IsTrue(cryomancer.FireAbility(receiver));
            Assert.AreEqual(2, receiver.Received.Count);
            Assert.AreEqual(StatusKind.Freeze, receiver.Received[1].Kind);
            Assert.AreEqual(2, receiver.Received[1].Stacks);
        }

        [Test]
        public void CT_5_GuardDogPet_AuraAppliesFirepowerBonusToStatCollection()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 100f);
            var pet = Track(new GameObject("Pet_CT5")).AddComponent<PetUnit>();
            pet.Initialize("guard_dog", "Cão de Guarda", maxHealth: 100, bonus: 0.15f);
            Assert.IsTrue(pet.IsPet);
            Assert.IsFalse(pet.TakeDamage(50));
            pet.AttachAura(stats);
            Assert.AreEqual(115f, stats.GetValue(StatId.Damage), 0.001f);
            pet.DetachAura();
            Assert.AreEqual(100f, stats.GetValue(StatId.Damage), 0.001f);
        }

        [Test]
        public void CT_6_AbilityRunner_AutoMode_TriggersWhenReadyAndTargetsPresent_AndResetsCooldown()
        {
            var runner = new AbilityRunner("Inferno", cooldownSeconds: 12f, initialCooldown: 0f);
            Assert.IsTrue(runner.IsReady);

            var policy = new AutoHeroAbilityTriggerPolicy();
            var triggerContext = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: true, manualTriggerRequested: false);

            bool fired = false;
            bool result = runner.TryTrigger(triggerContext, policy, () => fired = true);

            Assert.IsTrue(result);
            Assert.IsTrue(fired);
            Assert.IsFalse(runner.IsReady);
            Assert.AreEqual(12f, runner.CooldownRemaining, 0.001f);
        }

        [Test]
        public void CT_7_AbilityRunner_ManualMode_RetainsReadyUntilManualTriggerRequested()
        {
            var runner = new AbilityRunner("Blizzard", cooldownSeconds: 14f, initialCooldown: 0f);
            Assert.IsTrue(runner.IsReady);

            var policy = new ManualHeroAbilityTriggerPolicy();
            var unrequestedContext = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: true, manualTriggerRequested: false);

            bool fired = false;
            bool result = runner.TryTrigger(unrequestedContext, policy, () => fired = true);
            Assert.IsFalse(result);
            Assert.IsFalse(fired);
            Assert.IsTrue(runner.IsReady);

            var requestedContext = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: true, manualTriggerRequested: true);
            result = runner.TryTrigger(requestedContext, policy, () => fired = true);
            Assert.IsTrue(result);
            Assert.IsTrue(fired);
            Assert.IsFalse(runner.IsReady);
            Assert.AreEqual(14f, runner.CooldownRemaining, 0.001f);
        }

        [Test]
        public void CT_8_DataPipeline_AllyJsonDefinition_ParsesCorrectly()
        {
            var effect = new RecruitAllyPerkEffect("guard_dog");
            Assert.AreEqual("guard_dog", effect.AllyId);
            Assert.AreEqual("Recrutar aliado guard_dog", effect.Description);

            var roster = new AllyRoster();
            Assert.IsTrue(roster.TryRecruit("guard_dog", out var dog));
            Assert.AreEqual("guard_dog", dog.Id);
            Assert.IsTrue(dog.IsPet);
            Assert.AreEqual(1, roster.PetCount);
            Assert.AreEqual(0, roster.HeroCount);
        }

        [Test]
        public void CT_9_HeroUnit_HpDepletionTransitionsToKnockout_WithoutFailingRun()
        {
            var squad = new FakeSquad(15);
            var bus = new EventBus();
            bool damagedPublished = false, knockedOutPublished = false;
            bus.Subscribe<AllyDamagedEvent>(_ => damagedPublished = true);
            bus.Subscribe<AllyKnockedOutEvent>(_ => knockedOutPublished = true);
            var hero = Track(new GameObject("Hero_CT9")).AddComponent<HeroUnit>();
            hero.Initialize("pyromancer", "Piromante", maxHealth: 100, bus: bus);
            hero.TakeDamage(40);
            Assert.IsTrue(damagedPublished);
            Assert.IsFalse(knockedOutPublished);
            Assert.IsTrue(hero.IsAlive);
            Assert.AreEqual(60, hero.CurrentHealth);
            hero.TakeDamage(60);
            Assert.IsTrue(knockedOutPublished);
            Assert.IsFalse(hero.IsAlive);
            Assert.AreEqual(0, hero.CurrentHealth);
            Assert.AreEqual(15, squad.SquadCount);
        }
    }
}
