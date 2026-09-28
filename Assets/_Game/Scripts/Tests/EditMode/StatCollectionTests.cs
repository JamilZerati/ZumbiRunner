using System;
using System.Collections.Generic;
using Game.Core.Stats;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class StatCollectionTests
    {
        private const float Tolerance = 0.0001f;
        private const string DamageUp = "damage_up_25";

        [Test]
        public void GetValue_AppliesFlatThenSummedPercentAddThenMultipliedPercentMultiply()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 10f);
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 2f, "a"));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.5f, "b"));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.5f, "c"));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentMultiply, 1f, "d"));

            Assert.AreEqual(48f, stats.GetValue(StatId.Damage), Tolerance);
        }

        [Test]
        public void GetValue_WithoutPercentMultiply_SumsPercentAddBeforeMultiplying()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 10f);
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 2f, "a"));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.5f, "b"));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.5f, "c"));

            Assert.AreEqual(24f, stats.GetValue(StatId.Damage), Tolerance);
        }

        [Test]
        public void GetValue_TwoPercentMultiply_CompoundInsteadOfSumming()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 10f);
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentMultiply, 1f, "a"));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentMultiply, 0.5f, "b"));

            Assert.AreEqual(30f, stats.GetValue(StatId.Damage), Tolerance);
        }

        [Test]
        public void GetValue_NegativeAndPositivePercentAdd_AreSummed()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.FireRate, 100f);
            stats.AddModifier(new StatModifier(StatId.FireRate, ModifierKind.PercentAdd, -0.5f, "a"));
            stats.AddModifier(new StatModifier(StatId.FireRate, ModifierKind.PercentAdd, 0.25f, "b"));

            Assert.AreEqual(75f, stats.GetValue(StatId.FireRate), Tolerance);
        }

        [Test]
        public void GetValue_PercentMultiplyBetweenMinusOneAndZero_ShrinksStat()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Range, 10f);
            stats.AddModifier(new StatModifier(StatId.Range, ModifierKind.PercentMultiply, -0.5f, "a"));

            Assert.AreEqual(5f, stats.GetValue(StatId.Range), Tolerance);
        }

        [Test]
        public void GetValue_DoesNotClamp_FlatCanDriveValueNegative()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 1f);
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, -5f, "a"));

            Assert.AreEqual(-4f, stats.GetValue(StatId.Damage), Tolerance);
        }

        [Test]
        public void GetValue_TenPercentAddOfTenPercent_DoublesWithinFloatTolerance()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.ProjectileSpeed, 10f);
            for (int i = 0; i < 10; i++)
            {
                stats.AddModifier(new StatModifier(StatId.ProjectileSpeed, ModifierKind.PercentAdd, 0.1f, $"p{i}"));
            }

            Assert.AreEqual(20f, stats.GetValue(StatId.ProjectileSpeed), 0.001f);
        }

        [Test]
        public void GetValue_ThousandFlatModifiers_AreAllCounted()
        {
            var stats = new StatCollection();
            for (int i = 0; i < 1000; i++)
            {
                stats.AddModifier(new StatModifier(StatId.ProjectileCount, ModifierKind.Flat, 1f, "stack"));
            }

            Assert.AreEqual(1000f, stats.GetValue(StatId.ProjectileCount), Tolerance);
        }

        [Test]
        public void GetBase_NeverSet_ReturnsZero_AndFlatStillApplies()
        {
            var stats = new StatCollection();

            Assert.AreEqual(0f, stats.GetBase(StatId.Range));

            stats.AddModifier(new StatModifier(StatId.Range, ModifierKind.Flat, 3f, "a"));
            Assert.AreEqual(3f, stats.GetValue(StatId.Range), Tolerance);
        }

        [Test]
        public void Modifier_OnlyAffectsItsOwnStat()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 10f);
            stats.SetBase(StatId.FireRate, 2f);
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 1f, DamageUp));

            Assert.AreEqual(20f, stats.GetValue(StatId.Damage), Tolerance);
            Assert.AreEqual(2f, stats.GetValue(StatId.FireRate), Tolerance);
            Assert.AreEqual(0, stats.GetModifiers(StatId.FireRate).Count);
        }

        [Test]
        public void SetBase_KeepsExistingModifiers()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 10f);
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.25f, DamageUp));

            stats.SetBase(StatId.Damage, 8f);

            Assert.AreEqual(8f, stats.GetBase(StatId.Damage), Tolerance);
            Assert.AreEqual(10f, stats.GetValue(StatId.Damage), Tolerance);
            Assert.AreEqual(1, stats.GetModifiers(StatId.Damage).Count);
        }

        [Test]
        public void RemoveAllFromSource_RemovesEveryModifierOfThatSource_AndReturnsCount()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 10f);
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.25f, DamageUp));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentAdd, 0.25f, DamageUp));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 5f, "other_perk"));

            int removed = stats.RemoveAllFromSource(DamageUp);

            Assert.AreEqual(2, removed);
            var remaining = stats.GetModifiers(StatId.Damage);
            Assert.AreEqual(1, remaining.Count);
            Assert.AreEqual("other_perk", remaining[0].Source);
            Assert.AreEqual(15f, stats.GetValue(StatId.Damage), Tolerance);
        }

        [Test]
        public void RemoveAllFromSource_ComparesStringSourcesByValue()
        {
            var stats = new StatCollection();
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 1f, DamageUp));
            string sameContentDifferentInstance = new string(DamageUp.ToCharArray());

            int removed = stats.RemoveAllFromSource(sameContentDifferentInstance);

            Assert.AreEqual(1, removed);
            Assert.AreEqual(0, stats.GetModifiers(StatId.Damage).Count);
        }

        [Test]
        public void RemoveAllFromSource_DistinctObjectSources_OnlyRemovesMatchingOne()
        {
            var stats = new StatCollection();
            var sourceA = new object();
            var sourceB = new object();
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 1f, sourceA));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 2f, sourceB));

            Assert.AreEqual(1, stats.RemoveAllFromSource(sourceA));
            Assert.AreEqual(2f, stats.GetValue(StatId.Damage), Tolerance);
        }

        [Test]
        public void RemoveAllFromSource_SpansEveryStat()
        {
            var stats = new StatCollection();
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 1f, "berserk"));
            stats.AddModifier(new StatModifier(StatId.FireRate, ModifierKind.Flat, 1f, "berserk"));

            Assert.AreEqual(2, stats.RemoveAllFromSource("berserk"));
            Assert.AreEqual(0, stats.GetModifiers(StatId.Damage).Count);
            Assert.AreEqual(0, stats.GetModifiers(StatId.FireRate).Count);
        }

        [Test]
        public void RemoveAllFromSource_UnknownSource_ReturnsZeroAndKeepsValue()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 10f);
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 2f, DamageUp));

            Assert.AreEqual(0, stats.RemoveAllFromSource("never_added"));
            Assert.AreEqual(12f, stats.GetValue(StatId.Damage), Tolerance);
        }

        [Test]
        public void RemoveModifier_RemovesExactlyOneOccurrence()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 10f);
            var modifier = new StatModifier(StatId.Damage, ModifierKind.Flat, 2f, DamageUp);
            stats.AddModifier(modifier);
            stats.AddModifier(modifier);

            Assert.IsTrue(stats.RemoveModifier(modifier));
            Assert.AreEqual(1, stats.GetModifiers(StatId.Damage).Count);
            Assert.AreEqual(12f, stats.GetValue(StatId.Damage), Tolerance);
        }

        [Test]
        public void RemoveModifier_NotPresent_ReturnsFalse()
        {
            var stats = new StatCollection();
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 2f, DamageUp));

            bool removed = stats.RemoveModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 3f, DamageUp));

            Assert.IsFalse(removed);
            Assert.AreEqual(1, stats.GetModifiers(StatId.Damage).Count);
        }

        [Test]
        public void GetModifiers_StatWithoutModifiers_ReturnsEmptyNotNull()
        {
            var stats = new StatCollection();

            var modifiers = stats.GetModifiers(StatId.ProjectileCount);

            Assert.IsNotNull(modifiers);
            Assert.AreEqual(0, modifiers.Count);
        }

        [Test]
        public void Changed_FiresWithAffectedStat_OnEveryMutation()
        {
            var stats = new StatCollection();
            var raised = new List<StatId>();
            stats.Changed += raised.Add;

            stats.SetBase(StatId.Damage, 10f);
            stats.AddModifier(new StatModifier(StatId.FireRate, ModifierKind.Flat, 1f, DamageUp));
            stats.AddModifier(new StatModifier(StatId.Range, ModifierKind.Flat, 1f, DamageUp));
            stats.RemoveAllFromSource(DamageUp);

            CollectionAssert.AreEqual(new[] { StatId.Damage, StatId.FireRate, StatId.Range }, raised.GetRange(0, 3));
            CollectionAssert.AreEquivalent(new[] { StatId.FireRate, StatId.Range }, raised.GetRange(3, raised.Count - 3));
        }

        [Test]
        public void StatModifier_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StatModifier(StatId.Damage, ModifierKind.Flat, 1f, null));
        }

        [Test]
        public void StatModifier_PercentMultiplyMinusOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StatModifier(StatId.Damage, ModifierKind.PercentMultiply, -1f, DamageUp));
        }

        [Test]
        public void StatModifier_PercentMultiplyBelowMinusOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StatModifier(StatId.Damage, ModifierKind.PercentMultiply, -1.5f, DamageUp));
        }

        [Test]
        public void StatModifier_FlatMinusOne_IsAccepted()
        {
            var modifier = new StatModifier(StatId.Damage, ModifierKind.Flat, -1f, DamageUp);

            Assert.AreEqual(StatId.Damage, modifier.Stat);
            Assert.AreEqual(ModifierKind.Flat, modifier.Kind);
            Assert.AreEqual(-1f, modifier.Value);
            Assert.AreEqual(DamageUp, modifier.Source);
        }
    }
}
