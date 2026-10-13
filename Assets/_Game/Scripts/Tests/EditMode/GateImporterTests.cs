using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Gates;
using Game.Data;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.EditMode
{
    public class GateImporterTests
    {
        private const string TempAssetParent = "Assets";
        private const string TempAssetFolderName = "_TempTests_Gates";
        private const string TempTargetFolder = TempAssetParent + "/" + TempAssetFolderName;

        private string _tempSourceFolder;

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_tempSourceFolder) && Directory.Exists(_tempSourceFolder))
            {
                Directory.Delete(_tempSourceFolder, true);
                _tempSourceFolder = null;
            }

            if (AssetDatabase.IsValidFolder(TempTargetFolder))
            {
                AssetDatabase.DeleteAsset(TempTargetFolder);
            }
        }

        [Test]
        public void ImportAll_DefaultPath_CreatesCanonicalGateDefinitionsAndCatalog()
        {
            PerkImporter.ImportAll();

            var errors = new List<string>();
            int count = GateImporter.ImportAll(errors: errors);

            Assert.IsEmpty(errors);
            Assert.GreaterOrEqual(count, 4);

            var catalog = GateImporter.LoadCatalog();
            Assert.IsNotNull(catalog, "GateCatalog should exist at default path.");
            Assert.GreaterOrEqual(catalog.Gates.Count, 4);

            Assert.IsTrue(catalog.TryGet("gate_static_add_5", out var staticGate));
            Assert.AreEqual("gate_static_add_5", staticGate.Id);
            Assert.AreEqual("+5", staticGate.DisplayName);
            Assert.AreEqual("add_5", staticGate.PerkId);
            Assert.IsNotNull(staticGate.Perk);
            Assert.AreEqual("add_5", staticGate.Perk.Id);
            Assert.IsInstanceOf<StaticGateVariant>(staticGate.Variant);

            Assert.IsTrue(catalog.TryGet("gate_upgrade_minus5_to_15", out var upgradeGate));
            Assert.AreEqual("gate_upgrade_minus5_to_15", upgradeGate.Id);
            Assert.AreEqual("-5", upgradeGate.DisplayName);
            Assert.AreEqual("add_5", upgradeGate.PerkId);
            Assert.IsNotNull(upgradeGate.Perk);
            Assert.IsInstanceOf<ShootToUpgradeGateVariant>(upgradeGate.Variant);
            var upgradeVariant = (ShootToUpgradeGateVariant)upgradeGate.Variant;
            Assert.AreEqual(-5, upgradeVariant.InitialValue);
            Assert.AreEqual(1, upgradeVariant.Step);
            Assert.AreEqual(1, upgradeVariant.HitsPerStep);
            Assert.AreEqual(15, upgradeVariant.Cap);

            Assert.IsTrue(catalog.TryGet("gate_cursed_triple_frenzy", out var cursedGate));
            Assert.AreEqual("gate_cursed_triple_frenzy", cursedGate.Id);
            Assert.AreEqual("multiply_2", cursedGate.PerkId);
            Assert.IsNotNull(cursedGate.Perk);
            Assert.IsInstanceOf<CursedGateVariant>(cursedGate.Variant);
            var cursedVariant = (CursedGateVariant)cursedGate.Variant;
            Assert.AreEqual("add_10", cursedVariant.BonusPerkId);
            Assert.AreEqual(150f, cursedVariant.DurationDistance, 0.001f);
            Assert.AreEqual(1.35f, cursedVariant.StageModifier.EnemySpeedMultiplier, 0.001f);
            Assert.AreEqual(1.2f, cursedVariant.StageModifier.EnemyContactDamageMultiplier, 0.001f);

            Assert.IsTrue(catalog.TryGet("gate_guarded_multiply_2", out var guardedGate));
            Assert.AreEqual("gate_guarded_multiply_2", guardedGate.Id);
            Assert.AreEqual("multiply_2", guardedGate.PerkId);
            Assert.IsNotNull(guardedGate.Perk);
            Assert.IsInstanceOf<GuardedGateVariant>(guardedGate.Variant);
            var guardedVariant = (GuardedGateVariant)guardedGate.Variant;
            Assert.AreEqual("wave_guard_1", guardedVariant.EnemyWaveId);
            Assert.AreEqual("runner", guardedVariant.GuardArchetypeId);
            Assert.AreEqual(6, guardedVariant.GuardCount);
            Assert.AreEqual(8.0f, guardedVariant.GuardDistanceOffset, 0.001f);
        }

        [Test]
        public void ImportAll_IsIdempotent_DoesNotDuplicateOrCorruptAssets()
        {
            PerkImporter.ImportAll();

            int firstCount = GateImporter.ImportAll();
            var firstGate = AssetDatabase.LoadAssetAtPath<GateDefinition>("Assets/_Game/Data/Gates/gate_static_add_5.asset");
            string firstPath = AssetDatabase.GetAssetPath(firstGate);

            int secondCount = GateImporter.ImportAll();
            var secondGate = AssetDatabase.LoadAssetAtPath<GateDefinition>("Assets/_Game/Data/Gates/gate_static_add_5.asset");
            string secondPath = AssetDatabase.GetAssetPath(secondGate);

            Assert.AreEqual(firstCount, secondCount);
            Assert.IsNotNull(secondGate);
            Assert.AreEqual(firstPath, secondPath);
            Assert.AreEqual("gate_static_add_5", secondGate.Id);
        }

        [Test]
        public void ImportAll_InvalidHitsPerStep_ReportsError()
        {
            SetupTempSource();
            string json = @"{
                ""id"": ""broken_upgrade_hits"",
                ""displayName"": ""Broken"",
                ""perkId"": ""add_5"",
                ""variant"": {
                    ""type"": ""ShootToUpgrade"",
                    ""initialValue"": -5,
                    ""step"": 1,
                    ""hitsPerStep"": 0,
                    ""cap"": 10
                }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "broken_upgrade_hits.json"), json);

            var errors = new List<string>();
            int count = GateImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("hitsPerStep", errors[0]);
        }

        [Test]
        public void ImportAll_InvalidStep_ReportsError()
        {
            SetupTempSource();
            string json = @"{
                ""id"": ""broken_upgrade_step"",
                ""displayName"": ""Broken"",
                ""perkId"": ""add_5"",
                ""variant"": {
                    ""type"": ""ShootToUpgrade"",
                    ""initialValue"": -5,
                    ""step"": 0,
                    ""hitsPerStep"": 1,
                    ""cap"": 10
                }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "broken_upgrade_step.json"), json);

            var errors = new List<string>();
            int count = GateImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("step", errors[0]);
        }

        [Test]
        public void ImportAll_InvalidDurationDistance_ReportsError()
        {
            SetupTempSource();
            string json = @"{
                ""id"": ""broken_cursed_duration"",
                ""displayName"": ""Broken"",
                ""perkId"": ""multiply_2"",
                ""variant"": {
                    ""type"": ""Cursed"",
                    ""durationDistance"": -10.0
                }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "broken_cursed_duration.json"), json);

            var errors = new List<string>();
            int count = GateImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("durationDistance", errors[0]);
        }

        [Test]
        public void ImportAll_InvalidGuardCount_ReportsError()
        {
            SetupTempSource();
            string json = @"{
                ""id"": ""broken_guarded_count"",
                ""displayName"": ""Broken"",
                ""perkId"": ""multiply_2"",
                ""variant"": {
                    ""type"": ""Guarded"",
                    ""guardCount"": 0
                }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "broken_guarded_count.json"), json);

            var errors = new List<string>();
            int count = GateImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("guardCount", errors[0]);
        }

        [Test]
        public void ImportAll_InexistentPerkId_ReportsError()
        {
            SetupTempSource();
            string json = @"{
                ""id"": ""broken_perk"",
                ""displayName"": ""Broken"",
                ""perkId"": ""nonexistent_perk_id_12345"",
                ""variant"": {
                    ""type"": ""Static""
                }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "broken_perk.json"), json);

            var errors = new List<string>();
            int count = GateImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("perkId", errors[0]);
        }

        [Test]
        public void ImportAll_InexistentBonusPerkId_ReportsError()
        {
            SetupTempSource();
            string json = @"{
                ""id"": ""broken_bonus_perk"",
                ""displayName"": ""Broken"",
                ""perkId"": ""multiply_2"",
                ""variant"": {
                    ""type"": ""Cursed"",
                    ""bonusPerkId"": ""nonexistent_bonus_perk_12345"",
                    ""durationDistance"": 100.0
                }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "broken_bonus_perk.json"), json);

            var errors = new List<string>();
            int count = GateImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("bonusPerkId", errors[0]);
        }

        private void SetupTempSource()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex884_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);
        }
    }
}
