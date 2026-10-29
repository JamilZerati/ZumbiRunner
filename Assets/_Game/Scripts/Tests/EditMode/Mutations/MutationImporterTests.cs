using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Modifiers;
using Game.Data.Mutations;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.EditMode
{
    public class MutationImporterTests
    {
        private const string TempAssetParent = "Assets";
        private const string TempAssetFolderName = "_TempTests_Mutations";
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
        public void ImportAll_DefaultPath_ImportsCanonicalMutationCatalogAndDefinitions()
        {
            var errors = new List<string>();
            int count = MutationImporter.ImportAll(errors: errors);

            Assert.IsEmpty(errors);
            Assert.AreEqual(5, count);

            var catalog = MutationImporter.LoadCatalog();
            Assert.IsNotNull(catalog, "MutationCatalog deve existir no caminho padrao.");
            Assert.AreEqual(5, catalog.Entries.Count);
            Assert.AreEqual(5, catalog.Mutations.Count);

            Assert.IsTrue(catalog.TryGet("armored", out var armored));
            Assert.AreEqual("Blindados", armored.DisplayName);
            Assert.AreEqual("Inimigos possuem +50% de vida.", armored.Description);
            Assert.AreEqual(0.5f, armored.RewardBonus, 1e-4f);
            Assert.AreEqual(1, armored.Modifiers.Count);
            Assert.AreEqual("mutacao:armored", armored.Modifiers[0].SourceId);
            Assert.AreEqual(1.5f, armored.Modifiers[0].EnemyHealthMultiplier, 1e-4f);

            Assert.IsTrue(catalog.TryGet("frenzy", out var frenzy));
            Assert.AreEqual("Frenesi", frenzy.DisplayName);
            Assert.AreEqual("Inimigos movem-se 30% mais rápido.", frenzy.Description);
            Assert.AreEqual(0.5f, frenzy.RewardBonus, 1e-4f);
            Assert.AreEqual(1, frenzy.Modifiers.Count);
            Assert.AreEqual("mutacao:frenzy", frenzy.Modifiers[0].SourceId);
            Assert.AreEqual(1.3f, frenzy.Modifiers[0].EnemySpeedMultiplier, 1e-4f);

            Assert.IsTrue(catalog.TryGet("fog", out var fog));
            Assert.AreEqual("Neblina", fog.DisplayName);
            Assert.AreEqual("Alcance de engajamento da tropa reduzido em 40%.", fog.Description);
            Assert.AreEqual(0.25f, fog.RewardBonus, 1e-4f);
            Assert.AreEqual(1, fog.Modifiers.Count);
            Assert.AreEqual("mutacao:fog", fog.Modifiers[0].SourceId);
            Assert.AreEqual(0.6f, fog.Modifiers[0].SquadRangeMultiplier, 1e-4f);

            Assert.IsTrue(catalog.TryGet("last_soldier", out var lastSoldier));
            Assert.AreEqual("Último Soldado", lastSoldier.DisplayName);
            Assert.AreEqual("Comece a fase com apenas 1 soldado.", lastSoldier.Description);
            Assert.AreEqual(1.0f, lastSoldier.RewardBonus, 1e-4f);
            Assert.AreEqual(1, lastSoldier.Modifiers.Count);
            Assert.AreEqual("mutacao:last_soldier", lastSoldier.Modifiers[0].SourceId);
            Assert.AreEqual(1, lastSoldier.Modifiers[0].InitialSquadOverride);

            Assert.IsTrue(catalog.TryGet("no_multiplication", out var noMult));
            Assert.AreEqual("Sem Multiplicação", noMult.DisplayName);
            Assert.AreEqual("Portões de multiplicação viram adição e divisão viram subtração.", noMult.Description);
            Assert.AreEqual(0.5f, noMult.RewardBonus, 1e-4f);
            Assert.AreEqual(1, noMult.Modifiers.Count);
            Assert.AreEqual("mutacao:no_multiplication", noMult.Modifiers[0].SourceId);
            Assert.IsTrue(noMult.Modifiers[0].RemapGateMultiplication);
            Assert.IsTrue(noMult.Modifiers[0].RemapGateDivision);

            var bonuses = catalog.GetRewardBonuses();
            Assert.AreEqual(5, bonuses.Count);
            Assert.AreEqual(0.5f, bonuses["armored"], 1e-4f);
            Assert.AreEqual(0.5f, bonuses["frenzy"], 1e-4f);
            Assert.AreEqual(0.25f, bonuses["fog"], 1e-4f);
            Assert.AreEqual(1.0f, bonuses["last_soldier"], 1e-4f);
            Assert.AreEqual(0.5f, bonuses["no_multiplication"], 1e-4f);
        }

        [Test]
        public void ImportAll_InvalidJson_CollectsErrors()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "MutationImporterTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            File.WriteAllText(Path.Combine(_tempSourceFolder, "mutations.json"), "{ invalid_json: 123");

            var errors = new List<string>();
            int count = MutationImporter.ImportAll(sourceFolder: _tempSourceFolder, targetFolder: TempTargetFolder, errors: errors);

            Assert.AreEqual(0, count);
            Assert.GreaterOrEqual(errors.Count, 1);
            StringAssert.Contains("invalido", errors[0]);
        }

        [Test]
        public void ImportAll_DuplicateId_CollectsErrors()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "MutationImporterTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string duplicateJson = @"{
                ""mutations"": [
                    { ""id"": ""dup"", ""displayName"": ""Dup 1"", ""description"": """", ""rewardBonus"": 0.1, ""modifiers"": [] },
                    { ""id"": ""dup"", ""displayName"": ""Dup 2"", ""description"": """", ""rewardBonus"": 0.2, ""modifiers"": [] }
                ]
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "mutations.json"), duplicateJson);

            var errors = new List<string>();
            int count = MutationImporter.ImportAll(sourceFolder: _tempSourceFolder, targetFolder: TempTargetFolder, errors: errors);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("duplicado", errors[0]);
        }

        [Test]
        public void ImportAll_NegativeRewardBonus_CollectsErrors()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "MutationImporterTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string negativeJson = @"{
                ""mutations"": [
                    { ""id"": ""bad_bonus"", ""displayName"": ""Bad"", ""description"": """", ""rewardBonus"": -0.5, ""modifiers"": [] }
                ]
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "mutations.json"), negativeJson);

            var errors = new List<string>();
            int count = MutationImporter.ImportAll(sourceFolder: _tempSourceFolder, targetFolder: TempTargetFolder, errors: errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("nao-negativo", errors[0]);
        }
    }
}
