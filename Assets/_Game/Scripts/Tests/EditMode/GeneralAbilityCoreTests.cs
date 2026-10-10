using System;
using NUnit.Framework;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Effects;
using Game.Core.Abilities.Policies;
using Game.Core.State;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class GeneralAbilityCoreTests
    {
        private sealed class FakeDamageSink : IAbilityDamageSink
        {
            public int CallCount { get; private set; }
            public AbilityPosition LastCenter { get; private set; }
            public float LastRadius { get; private set; }
            public int LastDamage { get; private set; }
            public DamageType LastType { get; private set; }
            public object LastSource { get; private set; }
            public int DamageToReturn { get; set; } = 150;

            public int ApplyAreaDamage(AbilityPosition center, float radius, int damage, DamageType type = DamageType.Area, object source = null)
            {
                CallCount++;
                LastCenter = center;
                LastRadius = radius;
                LastDamage = damage;
                LastType = type;
                LastSource = source;
                return DamageToReturn;
            }
        }

        [Test]
        public void AbilityChargeTracker_Inicial_DeveIniciarComZeroAbatesECargasEspecificadas()
        {
            var tracker = new AbilityChargeTracker(killsPerCharge: 25, initialCharges: 0);

            Assert.AreEqual(0, tracker.CurrentKills);
            Assert.AreEqual(25, tracker.KillsPerCharge);
            Assert.AreEqual(0, tracker.CurrentCharges);
        }

        [Test]
        public void AbilityChargeTracker_AoAtingir25Abates_DeveConcederUmaCargaEResetarContador()
        {
            var tracker = new AbilityChargeTracker(killsPerCharge: 25);

            for (int i = 0; i < 25; i++)
            {
                tracker.RegisterKill(byAbility: false);
            }

            Assert.AreEqual(1, tracker.CurrentCharges);
            Assert.AreEqual(0, tracker.CurrentKills);
        }

        [Test]
        public void AbilityChargeTracker_TryConsumeCharge_ComCargaDisponivel_DeveConsumirERetornarTrue()
        {
            var tracker = new AbilityChargeTracker(killsPerCharge: 25, initialCharges: 1);

            bool consumed = tracker.TryConsumeCharge();

            Assert.IsTrue(consumed);
            Assert.AreEqual(0, tracker.CurrentCharges);
        }

        [Test]
        public void AbilityChargeTracker_TryConsumeCharge_SemCargaDisponivel_DeveRetornarFalse()
        {
            var tracker = new AbilityChargeTracker(killsPerCharge: 25, initialCharges: 0);

            bool consumed = tracker.TryConsumeCharge();

            Assert.IsFalse(consumed);
            Assert.AreEqual(0, tracker.CurrentCharges);
        }

        [Test]
        public void AutoHeroAbilityTriggerPolicy_ComCargasEAlvosNaLane_DeveDisparar()
        {
            var policy = new AutoHeroAbilityTriggerPolicy();
            var context = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: true, manualTriggerRequested: false);

            bool shouldTrigger = policy.ShouldTrigger(context);

            Assert.IsTrue(shouldTrigger);
            Assert.AreEqual(HeroAbilityTriggerMode.Auto, policy.Mode);
        }

        [Test]
        public void AutoHeroAbilityTriggerPolicy_ComCargasMasSemAlvosNaLane_NaoDeveDisparar()
        {
            var policy = new AutoHeroAbilityTriggerPolicy();
            var context = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: false, manualTriggerRequested: false);

            bool shouldTrigger = policy.ShouldTrigger(context);

            Assert.IsFalse(shouldTrigger);
        }

        [Test]
        public void ManualHeroAbilityTriggerPolicy_ComCargasESolicitacaoManual_DeveDispararMesmoSemAlvos()
        {
            var policy = new ManualHeroAbilityTriggerPolicy();
            var context = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: false, manualTriggerRequested: true);

            bool shouldTrigger = policy.ShouldTrigger(context);

            Assert.IsTrue(shouldTrigger);
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, policy.Mode);
        }

        [Test]
        public void GrenadeAbilityEffect_Execute_DeveAplicar150DanoEmRaioDe4MetrosComTipoArea()
        {
            var effect = new GrenadeAbilityEffect();
            var sink = new FakeDamageSink();
            var origin = new AbilityPosition(2.0f, 0f, 15.0f);
            var context = new AbilityExecutionContext
            {
                OriginPosition = origin,
                TargetLane = 1,
                DamageSink = sink
            };

            effect.Execute(context);

            Assert.AreEqual(1, sink.CallCount);
            Assert.AreEqual(4.0f, sink.LastRadius, 0.001f);
            Assert.AreEqual(150, sink.LastDamage);
            Assert.AreEqual(DamageType.Area, sink.LastType);
            Assert.AreEqual(origin, sink.LastCenter);
        }

        [Test]
        public void AbilityChargeTracker_ArmadilhaAutoRecargaInfinita_AbatesPorHabilidadeNaoDevemGerarCargas()
        {
            var tracker = new AbilityChargeTracker(killsPerCharge: 25);

            for (int i = 0; i < 30; i++)
            {
                tracker.RegisterKill(byAbility: true);
            }

            Assert.AreEqual(0, tracker.CurrentKills);
            Assert.AreEqual(0, tracker.CurrentCharges);
        }

        [Test]
        public void AbilityChargeTracker_Limite24Abates_NaoDeveConcederCargaAntesDoLimiarExato()
        {
            var tracker = new AbilityChargeTracker(killsPerCharge: 25);

            for (int i = 0; i < 24; i++)
            {
                tracker.RegisterKill(byAbility: false);
            }

            Assert.AreEqual(24, tracker.CurrentKills);
            Assert.AreEqual(0, tracker.CurrentCharges);
        }

        [Test]
        public void AbilityChargeTracker_AcumuloMultiplo_50AbatesDevemGerarDuasCargas()
        {
            var tracker = new AbilityChargeTracker(killsPerCharge: 25);

            for (int i = 0; i < 50; i++)
            {
                tracker.RegisterKill(byAbility: false);
            }

            Assert.AreEqual(2, tracker.CurrentCharges);
            Assert.AreEqual(0, tracker.CurrentKills);
        }

        [Test]
        public void AbilityChargeTracker_EsgotamentoDeCargas_MultiplosConsumosSucessivos()
        {
            var tracker = new AbilityChargeTracker(killsPerCharge: 25, initialCharges: 2);

            Assert.IsTrue(tracker.TryConsumeCharge());
            Assert.AreEqual(1, tracker.CurrentCharges);

            Assert.IsTrue(tracker.TryConsumeCharge());
            Assert.AreEqual(0, tracker.CurrentCharges);

            Assert.IsFalse(tracker.TryConsumeCharge());
            Assert.AreEqual(0, tracker.CurrentCharges);
        }

        [Test]
        public void AbilityChargeTracker_CargaBonusInicialDeRunConfig_DeveSomarComCargasDeAbate()
        {
            var config = new RunConfig { BonusAbilityCharges = 1 };
            var tracker = new AbilityChargeTracker(killsPerCharge: 25, initialCharges: config.BonusAbilityCharges);

            for (int i = 0; i < 25; i++)
            {
                tracker.RegisterKill(byAbility: false);
            }

            Assert.AreEqual(2, tracker.CurrentCharges);
        }

        [Test]
        public void AbilityChargeTracker_Construtor_ComLimiarInvalido_DeveLancarExcecao()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AbilityChargeTracker(killsPerCharge: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AbilityChargeTracker(killsPerCharge: -5));
        }

        [Test]
        public void GrenadeAbilityEffect_Execute_ComContextoOuSinkNulo_DeveLancarArgumentNullException()
        {
            var effect = new GrenadeAbilityEffect();

            Assert.Throws<ArgumentNullException>(() => effect.Execute(null));
            Assert.Throws<ArgumentNullException>(() => effect.Execute(new AbilityExecutionContext { DamageSink = null }));
        }
    }
}
