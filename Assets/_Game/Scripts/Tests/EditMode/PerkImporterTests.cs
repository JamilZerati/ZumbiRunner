using System;
using Game.Core;
using Game.Core.Perks;
using Game.Data;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.EditMode
{
    public class PerkImporterTests
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
                    return false;
                }

                SquadCount = SquadCount / divisor;
                return true;
            }

            public bool SetCount(int newCount)
            {
                if (newCount < 0)
                {
                    return false;
                }

                SquadCount = newCount;
                return true;
            }
        }

        [Test]
        public void ImportAll_DefaultPath_CreatesAllFivePerkScriptableObjects()
        {
            int count = PerkImporter.ImportAll();

            Assert.GreaterOrEqual(count, 5);

            string[] expectedPerkIds = { "add_5", "add_10", "multiply_2", "subtract_3", "divide_2" };
            foreach (string perkId in expectedPerkIds)
            {
                string assetPath = $"Assets/_Game/Data/Perks/{perkId}.asset";
                var perk = AssetDatabase.LoadAssetAtPath<PerkDefinition>(assetPath);

                Assert.IsNotNull(perk, $"Perk asset should exist at '{assetPath}'.");
                Assert.AreEqual(perkId, perk.Id);
                Assert.IsNotNull(perk.Effects);
                Assert.AreEqual(1, perk.Effects.Count);
            }
        }

        [Test]
        public void ImportedPerks_ApplyExpectedSquadModifications()
        {
            PerkImporter.ImportAll();

            var add5 = AssetDatabase.LoadAssetAtPath<PerkDefinition>("Assets/_Game/Data/Perks/add_5.asset");
            Assert.IsNotNull(add5);
            Assert.AreEqual("+5", add5.DisplayName);
            var squad = new FakeSquad(10);
            add5.Apply(new PerkContext(squad));
            Assert.AreEqual(15, squad.SquadCount);

            var add10 = AssetDatabase.LoadAssetAtPath<PerkDefinition>("Assets/_Game/Data/Perks/add_10.asset");
            Assert.IsNotNull(add10);
            Assert.AreEqual("+10", add10.DisplayName);
            squad = new FakeSquad(10);
            add10.Apply(new PerkContext(squad));
            Assert.AreEqual(20, squad.SquadCount);

            var multiply2 = AssetDatabase.LoadAssetAtPath<PerkDefinition>("Assets/_Game/Data/Perks/multiply_2.asset");
            Assert.IsNotNull(multiply2);
            Assert.AreEqual("x2", multiply2.DisplayName);
            squad = new FakeSquad(10);
            multiply2.Apply(new PerkContext(squad));
            Assert.AreEqual(20, squad.SquadCount);

            var subtract3 = AssetDatabase.LoadAssetAtPath<PerkDefinition>("Assets/_Game/Data/Perks/subtract_3.asset");
            Assert.IsNotNull(subtract3);
            Assert.AreEqual("-3", subtract3.DisplayName);
            squad = new FakeSquad(10);
            subtract3.Apply(new PerkContext(squad));
            Assert.AreEqual(7, squad.SquadCount);

            var divide2 = AssetDatabase.LoadAssetAtPath<PerkDefinition>("Assets/_Game/Data/Perks/divide_2.asset");
            Assert.IsNotNull(divide2);
            Assert.AreEqual("÷2", divide2.DisplayName);
            squad = new FakeSquad(10);
            divide2.Apply(new PerkContext(squad));
            Assert.AreEqual(5, squad.SquadCount);
        }

        [Test]
        public void ImportAll_IsIdempotent_DoesNotDuplicateOrCorruptAssets()
        {
            int firstCount = PerkImporter.ImportAll();
            var firstAdd5 = AssetDatabase.LoadAssetAtPath<PerkDefinition>("Assets/_Game/Data/Perks/add_5.asset");
            string firstPath = AssetDatabase.GetAssetPath(firstAdd5);

            int secondCount = PerkImporter.ImportAll();
            var secondAdd5 = AssetDatabase.LoadAssetAtPath<PerkDefinition>("Assets/_Game/Data/Perks/add_5.asset");
            string secondPath = AssetDatabase.GetAssetPath(secondAdd5);

            Assert.AreEqual(firstCount, secondCount);
            Assert.IsNotNull(secondAdd5);
            Assert.AreEqual(firstPath, secondPath);
            Assert.AreEqual("add_5", secondAdd5.Id);
            Assert.AreEqual("+5", secondAdd5.DisplayName);
            Assert.AreEqual(1, secondAdd5.Effects.Count);
        }

        [Test]
        public void ImportAll_NonExistentSource_ReturnsZero()
        {
            int count = PerkImporter.ImportAll("Content/NonExistentSourcePath");
            Assert.AreEqual(0, count);
        }
    }
}
