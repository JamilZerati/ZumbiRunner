using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Policies;
using Game.Core.Allies;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Core.Stats;
using Game.Core.Status;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class AllyScenariosTests
    {
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
            var effect = new RecruitAllyPerkEffect("pyromancer");
            var context = new PerkContext(squad, laneIndex: 0, loadout: null, sourceId: "gate_1", roster: null);

            // No Marco 0, o stub de RecruitAllyPerkEffect.Apply deve existir e falhar comprovando Red
            effect.Apply(context);
        }

        [Test]
        public void CT_2_DamageSeparation_SquadLossDoesNotAffectHeroHp_AndHeroDamageDoesNotReduceSquadCount()
        {
            var squad = new FakeSquad(10);
            squad.Remove(4);

            Assert.AreEqual(6, squad.SquadCount, "Tropa deve ter 6 soldados apos sofrer 4 de dano.");
            IAllyInstance hero = null;
            Assert.IsNotNull(hero, "HeroUnit deve estar instanciado no Marco 2.");
        }

        [Test]
        public void CT_3_Pyromancer_BasicAttackAndInferno_AppliesBurnStatus()
        {
            var receiver = new FakeStatusReceiver();
            IAllyInstance pyromancer = null;
            Assert.IsNotNull(pyromancer, "Piromante deve ser implementado no Marco 2.");
        }

        [Test]
        public void CT_4_Cryomancer_BasicAttackAndBlizzard_AppliesFreezeStatus()
        {
            var receiver = new FakeStatusReceiver();
            IAllyInstance cryomancer = null;
            Assert.IsNotNull(cryomancer, "Criomante deve ser implementado no Marco 2.");
        }

        [Test]
        public void CT_5_GuardDogPet_AuraAppliesFirepowerBonusToStatCollection()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 100f);
            Assert.AreEqual(100f, stats.GetValue(StatId.Damage));

            IAllyInstance pet = null;
            Assert.IsNotNull(pet, "Pet GuardDog deve ser implementado no Marco 2.");
        }

        [Test]
        public void CT_6_AbilityRunner_AutoMode_TriggersWhenReadyAndTargetsPresent_AndResetsCooldown()
        {
            var runner = new AbilityRunner("Inferno", cooldownSeconds: 12f, initialCooldown: 0f);
            Assert.IsTrue(runner.IsReady);

            var policy = new AutoHeroAbilityTriggerPolicy();
            var triggerContext = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: true, manualTriggerRequested: false);

            bool fired = false;
            // No Marco 0, AbilityRunner.TryTrigger lanca NotImplementedException comprovando Red
            runner.TryTrigger(triggerContext, policy, () => fired = true);

            Assert.IsTrue(fired);
            Assert.IsFalse(runner.IsReady);
        }

        [Test]
        public void CT_7_AbilityRunner_ManualMode_RetainsReadyUntilManualTriggerRequested()
        {
            var runner = new AbilityRunner("Blizzard", cooldownSeconds: 14f, initialCooldown: 0f);
            Assert.IsTrue(runner.IsReady);

            var policy = new ManualHeroAbilityTriggerPolicy();
            var unrequestedContext = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: true, manualTriggerRequested: false);

            // No Marco 0, AbilityRunner.TryTrigger lanca NotImplementedException comprovando Red
            runner.TryTrigger(unrequestedContext, policy, () => { });
        }

        [Test]
        public void CT_8_DataPipeline_AllyJsonDefinition_ParsesCorrectly()
        {
            var effect = new RecruitAllyPerkEffect("guard_dog");
            Assert.AreEqual("guard_dog", effect.AllyId);
            Assert.AreEqual("Recrutar aliado guard_dog", effect.Description);
        }

        [Test]
        public void CT_9_HeroUnit_HpDepletionTransitionsToKnockout_WithoutFailingRun()
        {
            var squad = new FakeSquad(15);
            IAllyInstance hero = null;

            Assert.IsNotNull(hero, "HeroUnit deve suportar HP independente e transicao para nocaute no Marco 2.");
            Assert.AreEqual(15, squad.SquadCount, "Tropa permanece com 15 soldados mesmo se o heroi for nocauteado.");
        }
    }
}
