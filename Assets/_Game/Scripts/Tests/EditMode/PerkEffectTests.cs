using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Data;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class PerkEffectTests
    {
        private class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }

            public FakeSquad(int initialCount = 0)
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
        public void AddSoldiersEffect_PositiveAmount_IncreasesSquad()
        {
            var squad = new FakeSquad(5);
            var context = new PerkContext(squad, 0);
            var effect = new AddSoldiersEffect(3);

            effect.Apply(context);

            Assert.AreEqual(8, squad.SquadCount);
            Assert.AreEqual("+3", effect.Description);
        }

        [Test]
        public void AddSoldiersEffect_NegativeAmount_DecreasesSquad()
        {
            var squad = new FakeSquad(10);
            var context = new PerkContext(squad, 0);
            var effect = new AddSoldiersEffect(-4);

            effect.Apply(context);

            Assert.AreEqual(6, squad.SquadCount);
            Assert.AreEqual("-4", effect.Description);
        }

        [Test]
        public void MultiplySoldiersEffect_MultipliesSquadCorrectly()
        {
            var squad = new FakeSquad(4);
            var context = new PerkContext(squad, 1);
            var effect = new MultiplySoldiersEffect(3);

            effect.Apply(context);

            Assert.AreEqual(12, squad.SquadCount);
            Assert.AreEqual("x3", effect.Description);
        }

        [Test]
        public void DivideSoldiersEffect_DividesWithTruncation()
        {
            var squad = new FakeSquad(7);
            var context = new PerkContext(squad, 0);
            var effect = new DivideSoldiersEffect(2);

            effect.Apply(context);

            Assert.AreEqual(3, squad.SquadCount);
            Assert.AreEqual("÷2", effect.Description);
        }

        [Test]
        public void DivideSoldiersEffect_ZeroOrNegativeDivisor_ThrowsArgumentOutOfRangeException()
        {
            var squad = new FakeSquad(10);
            var context = new PerkContext(squad, 0);
            var zeroEffect = new DivideSoldiersEffect(0);
            var negativeEffect = new DivideSoldiersEffect(-2);

            Assert.Throws<ArgumentOutOfRangeException>(() => zeroEffect.Apply(context));
            Assert.Throws<ArgumentOutOfRangeException>(() => negativeEffect.Apply(context));
        }

        [Test]
        public void PerkDefinition_AppliesMultipleEffectsInSequence()
        {
            var perk = ScriptableObject.CreateInstance<PerkDefinition>();
            try
            {
                var effects = new List<IPerkEffect>
                {
                    new AddSoldiersEffect(5),
                    new MultiplySoldiersEffect(2),
                    new DivideSoldiersEffect(4)
                };
                perk.SetData("combo_perk", "Combo Perk", effects);

                var squad = new FakeSquad(5);
                var context = new PerkContext(squad, 0);

                perk.Apply(context);

                // Initial: 5 -> +5 = 10 -> x2 = 20 -> ÷4 = 5
                Assert.AreEqual(5, squad.SquadCount);
                Assert.AreEqual("combo_perk", perk.Id);
                Assert.AreEqual("Combo Perk", perk.DisplayName);
                Assert.AreEqual(3, perk.Effects.Count);
            }
            finally
            {
                Object.DestroyImmediate(perk);
            }
        }

        [Test]
        public void SquadController_SatisfiesISquadContract()
        {
            var go = new GameObject("SquadControllerContractTest");
            try
            {
                var controller = go.AddComponent<SquadController>();
                controller.Initialize(10);

                ISquad squad = controller;
                Assert.AreEqual(10, squad.SquadCount);

                squad.Add(5);
                Assert.AreEqual(15, squad.SquadCount);

                squad.Remove(3);
                Assert.AreEqual(12, squad.SquadCount);

                squad.Multiply(2);
                Assert.AreEqual(24, squad.SquadCount);

                squad.Divide(3);
                Assert.AreEqual(8, squad.SquadCount);

                squad.SetCount(50);
                Assert.AreEqual(50, squad.SquadCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
