using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Weather;
using Game.Data.Weather;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.EditMode
{
    public class WeatherImporterTests
    {
        private const string TempAssetParent = "Assets";
        private const string TempAssetFolderName = "_TempTests_Weather";
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
        public void ImportAll_DefaultPath_ImportsCanonicalWeatherCatalogAndDefinitions()
        {
            var errors = new List<string>();
            int count = WeatherImporter.ImportAll(errors: errors);

            Assert.IsEmpty(errors);
            Assert.AreEqual(4, count);

            var catalog = WeatherImporter.LoadCatalog();
            Assert.IsNotNull(catalog, "WeatherCatalog deve existir no caminho padrao.");
            Assert.AreEqual(4, catalog.Entries.Count);

            Assert.IsTrue(catalog.TryGet("rain", out var rain));
            Assert.AreEqual("rain", rain.Id);
            Assert.AreEqual(WeatherType.Rain, rain.Type);
            Assert.AreEqual("Chuva", rain.DisplayName);
            Assert.AreEqual("wet", rain.AppliedStatus);
            Assert.AreEqual(1.0f, rain.EnemySpeedMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, rain.EnemyContactDamageMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, rain.SquadRangeMultiplier, 1e-4f);

            Assert.IsTrue(catalog.TryGetByType(WeatherType.Blizzard, out var blizzard));
            Assert.AreEqual("blizzard", blizzard.Id);
            Assert.AreEqual(0.8f, blizzard.EnemySpeedMultiplier, 1e-4f);

            Assert.IsTrue(catalog.TryGetByType(WeatherType.Heatwave, out var heatwave));
            Assert.AreEqual("heatwave", heatwave.Id);
            Assert.AreEqual(1.2f, heatwave.EnemyContactDamageMultiplier, 1e-4f);

            Assert.IsTrue(catalog.TryGetByType(WeatherType.Fog, out var fog));
            Assert.AreEqual("fog", fog.Id);
            Assert.AreEqual(0.75f, fog.SquadRangeMultiplier, 1e-4f);
        }

        [Test]
        public void ImportAll_InvalidType_FailsValidationAndCollectsErrors()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "WeatherImporterTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string invalidJson = @"{
                ""id"": ""tornado"",
                ""type"": ""Tornado"",
                ""displayName"": ""Tornado"",
                ""enemySpeedMultiplier"": 1.0,
                ""enemyContactDamageMultiplier"": 1.0,
                ""squadRangeMultiplier"": 1.0,
                ""appliedStatus"": """"
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "tornado.json"), invalidJson);

            var errors = new List<string>();
            int count = WeatherImporter.ImportAll(sourceFolder: _tempSourceFolder, targetFolder: TempTargetFolder, errors: errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("invalido", errors[0]);
        }

        [Test]
        public void ImportAll_NonPositiveMultipliers_FailsValidationAndCollectsErrors()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "WeatherImporterTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string invalidJson = @"{
                ""id"": ""bad_speed"",
                ""type"": ""Blizzard"",
                ""displayName"": ""Bad Speed"",
                ""enemySpeedMultiplier"": -0.5,
                ""enemyContactDamageMultiplier"": 1.0,
                ""squadRangeMultiplier"": 1.0,
                ""appliedStatus"": """"
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "bad_speed.json"), invalidJson);

            var errors = new List<string>();
            int count = WeatherImporter.ImportAll(sourceFolder: _tempSourceFolder, targetFolder: TempTargetFolder, errors: errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("enemySpeedMultiplier deve ser positivo", errors[0]);
        }
    }
}
