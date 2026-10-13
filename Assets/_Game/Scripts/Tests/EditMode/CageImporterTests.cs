using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Data;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.EditMode
{
    public class CageImporterTests
    {
        private const string TempAssetParent = "Assets";
        private const string TempAssetFolderName = "_TempTests_Cages";
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
        public void ImportAll_DefaultPath_CreatesCanonicalCageDefinitionAndCatalog()
        {
            EnemyImporter.ImportAll();

            var errors = new List<string>();
            int count = CageImporter.ImportAll(errors: errors);

            Assert.IsEmpty(errors);
            Assert.GreaterOrEqual(count, 1);

            var catalog = CageImporter.LoadCatalog();
            Assert.IsNotNull(catalog, "CageCatalog deve existir no caminho padrão.");
            Assert.GreaterOrEqual(catalog.Cages.Count, 1);

            Assert.IsTrue(catalog.TryGet("cage_standard_3", out var cage));
            Assert.AreEqual("cage_standard_3", cage.Id);
            Assert.AreEqual("Jaula de Sobreviventes", cage.DisplayName);
            Assert.AreEqual(60, cage.MaxHp);
            Assert.AreEqual(3, cage.SurvivorCount);
            Assert.AreEqual("walker", cage.LostEnemyArchetypeId);
        }

        [Test]
        public void ImportAll_InvalidParameters_CollectsErrorsAndSkipsCreation()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "HordeRunner_CageTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            File.WriteAllText(Path.Combine(_tempSourceFolder, "cage_zero_hp.json"), @"{
                ""id"": ""cage_zero_hp"",
                ""maxHp"": 0,
                ""survivorCount"": 3,
                ""lostEnemyArchetypeId"": ""walker""
            }");

            File.WriteAllText(Path.Combine(_tempSourceFolder, "cage_zero_survivors.json"), @"{
                ""id"": ""cage_zero_survivors"",
                ""maxHp"": 50,
                ""survivorCount"": 0,
                ""lostEnemyArchetypeId"": ""walker""
            }");

            File.WriteAllText(Path.Combine(_tempSourceFolder, "cage_no_id.json"), @"{
                ""id"": """",
                ""maxHp"": 50,
                ""survivorCount"": 2,
                ""lostEnemyArchetypeId"": ""walker""
            }");

            var errors = new List<string>();
            int count = CageImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(3, errors.Count);
        }

        [Test]
        public void ImportAll_DuplicateId_ReportsErrorAndImportsOnlyFirst()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "HordeRunner_CageTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            File.WriteAllText(Path.Combine(_tempSourceFolder, "a_cage.json"), @"{
                ""id"": ""duplicate_cage"",
                ""maxHp"": 40,
                ""survivorCount"": 2
            }");

            File.WriteAllText(Path.Combine(_tempSourceFolder, "b_cage.json"), @"{
                ""id"": ""duplicate_cage"",
                ""maxHp"": 80,
                ""survivorCount"": 4
            }");

            var errors = new List<string>();
            int count = CageImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("Duplicate cage id 'duplicate_cage'", errors[0]);
        }

        [Test]
        public void ImportAll_MalformedJson_ReportsErrorAndSkips()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "HordeRunner_CageTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            File.WriteAllText(Path.Combine(_tempSourceFolder, "corrupt.json"), "{ invalid_json: true, ");

            var errors = new List<string>();
            int count = CageImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("Failed to parse JSON", errors[0]);
        }

        [Test]
        public void ImportAll_InvalidEnemyArchetype_ReportsErrorAndSkips()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "HordeRunner_CageTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            File.WriteAllText(Path.Combine(_tempSourceFolder, "unknown_enemy_cage.json"), @"{
                ""id"": ""unknown_enemy_cage"",
                ""maxHp"": 50,
                ""survivorCount"": 2,
                ""lostEnemyArchetypeId"": ""non_existent_archetype_xyz""
            }");

            var errors = new List<string>();
            int count = CageImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.GreaterOrEqual(errors.Count, 1);
            Assert.IsTrue(errors.Any(e => e.Contains("non_existent_archetype_xyz") || e.Contains("EnemyCatalog")));
        }

        [Test]
        public void ImportAll_MissingSourceFolder_ReportsErrorAndReturnsZero()
        {
            string nonExistentSource = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid().ToString("N"));
            var errors = new List<string>();

            int count = CageImporter.ImportAll(nonExistentSource, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("Source directory does not exist", errors[0]);
        }
    }
}
