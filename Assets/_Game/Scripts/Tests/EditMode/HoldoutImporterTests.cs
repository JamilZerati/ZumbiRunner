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
    public class HoldoutImporterTests
    {
        private const string TempAssetParent = "Assets";
        private const string TempAssetFolderName = "_TempTests_Holdouts";
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
        public void ImportAll_DefaultPath_CreatesCanonicalHoldoutDefinitionAndCatalog()
        {
            var errors = new List<string>();
            int count = HoldoutImporter.ImportAll(errors: errors);

            Assert.IsEmpty(errors);
            Assert.GreaterOrEqual(count, 1);

            var catalog = HoldoutImporter.LoadCatalog();
            Assert.IsNotNull(catalog, "HoldoutCatalog deve existir no caminho padrão.");
            Assert.GreaterOrEqual(catalog.Holdouts.Count, 1);

            Assert.IsTrue(catalog.TryGet("holdout_standard", out var holdout));
            Assert.AreEqual("holdout_standard", holdout.Id);
            Assert.AreEqual("Barricada Padrão", holdout.DisplayName);
            Assert.AreEqual(200, holdout.MaxHp);
            Assert.AreEqual(5, holdout.TotalSurvivors);
            Assert.AreEqual(15f, holdout.DurationSeconds);
            Assert.AreEqual(10f, holdout.ZombieContactDps);
            Assert.AreEqual(3f, holdout.WaveIntervalSeconds);
            Assert.AreEqual(2, holdout.ZombiesPerWavePerLane);
        }

        [Test]
        public void ImportAll_InvalidParameters_CollectsErrorsAndSkipsCreation()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "HordeRunner_HoldoutTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            File.WriteAllText(Path.Combine(_tempSourceFolder, "holdout_zero_hp.json"), @"{
                ""id"": ""holdout_zero_hp"",
                ""maxHp"": 0,
                ""totalSurvivors"": 3,
                ""durationSeconds"": 10.0,
                ""zombieContactDps"": 5.0,
                ""waveIntervalSeconds"": 2.0,
                ""zombiesPerWavePerLane"": 1
            }");

            File.WriteAllText(Path.Combine(_tempSourceFolder, "holdout_negative_survivors.json"), @"{
                ""id"": ""holdout_negative_survivors"",
                ""maxHp"": 100,
                ""totalSurvivors"": -1,
                ""durationSeconds"": 10.0,
                ""zombieContactDps"": 5.0,
                ""waveIntervalSeconds"": 2.0,
                ""zombiesPerWavePerLane"": 1
            }");

            File.WriteAllText(Path.Combine(_tempSourceFolder, "holdout_no_id.json"), @"{
                ""id"": """",
                ""maxHp"": 100,
                ""totalSurvivors"": 2,
                ""durationSeconds"": 10.0,
                ""zombieContactDps"": 5.0,
                ""waveIntervalSeconds"": 2.0,
                ""zombiesPerWavePerLane"": 1
            }");

            File.WriteAllText(Path.Combine(_tempSourceFolder, "holdout_zero_duration.json"), @"{
                ""id"": ""holdout_zero_duration"",
                ""maxHp"": 100,
                ""totalSurvivors"": 2,
                ""durationSeconds"": 0.0,
                ""zombieContactDps"": 5.0,
                ""waveIntervalSeconds"": 2.0,
                ""zombiesPerWavePerLane"": 1
            }");

            var errors = new List<string>();
            int count = HoldoutImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(4, errors.Count);
        }

        [Test]
        public void ImportAll_DuplicateId_ReportsErrorAndImportsOnlyFirst()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "HordeRunner_HoldoutTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            File.WriteAllText(Path.Combine(_tempSourceFolder, "a_holdout.json"), @"{
                ""id"": ""duplicate_holdout"",
                ""maxHp"": 150,
                ""totalSurvivors"": 4,
                ""durationSeconds"": 12.0,
                ""zombieContactDps"": 8.0,
                ""waveIntervalSeconds"": 2.5,
                ""zombiesPerWavePerLane"": 2
            }");

            File.WriteAllText(Path.Combine(_tempSourceFolder, "b_holdout.json"), @"{
                ""id"": ""duplicate_holdout"",
                ""maxHp"": 300,
                ""totalSurvivors"": 8,
                ""durationSeconds"": 25.0,
                ""zombieContactDps"": 15.0,
                ""waveIntervalSeconds"": 4.0,
                ""zombiesPerWavePerLane"": 3
            }");

            var errors = new List<string>();
            int count = HoldoutImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("Duplicate holdout id 'duplicate_holdout'", errors[0]);
        }

        [Test]
        public void ImportAll_MalformedJson_ReportsErrorAndSkips()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "HordeRunner_HoldoutTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            File.WriteAllText(Path.Combine(_tempSourceFolder, "corrupt.json"), "{ invalid_json: true, ");

            var errors = new List<string>();
            int count = HoldoutImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("Failed to parse JSON", errors[0]);
        }

        [Test]
        public void ImportAll_MissingSourceFolder_ReportsErrorAndReturnsZero()
        {
            string nonExistentSource = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid().ToString("N"));
            var errors = new List<string>();

            int count = HoldoutImporter.ImportAll(nonExistentSource, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("Source directory does not exist", errors[0]);
        }

        [Test]
        public void HoldoutSegmentSpec_CanStoreAndReadFields()
        {
            var spec = new HoldoutSegmentSpec
            {
                HoldoutId = "holdout_custom",
                CustomMaxHp = 250,
                CustomSurvivors = 6,
                CustomDuration = 18f
            };

            Assert.AreEqual("holdout_custom", spec.HoldoutId);
            Assert.AreEqual(250, spec.CustomMaxHp);
            Assert.AreEqual(6, spec.CustomSurvivors);
            Assert.AreEqual(18f, spec.CustomDuration);
        }
    }
}
