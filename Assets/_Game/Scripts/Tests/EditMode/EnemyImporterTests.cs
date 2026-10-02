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
    public class EnemyImporterTests
    {
        private const string TempAssetParent = "Assets";
        private const string TempAssetFolderName = "_TempTests_Enemies";
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
        public void ImportAll_ImportsAll7Archetypes()
        {
            var errors = new List<string>();
            int count = EnemyImporter.ImportAll(errors: errors);

            Assert.IsEmpty(errors);
            Assert.AreEqual(7, count);

            var catalog = EnemyImporter.LoadCatalog();
            Assert.IsNotNull(catalog);
            Assert.AreEqual(7, catalog.Enemies.Count);

            AssertArchetype(catalog, "walker", 20, 2.0f, 5.0f, 1, typeof(MoveStraightBehavior));
            AssertArchetype(catalog, "runner", 12, 5.0f, 4.0f, 1, typeof(MoveStraightBehavior), typeof(ChaseLaneBehavior));
            AssertArchetype(catalog, "brute", 300, 1.2f, 25.0f, 5, typeof(MoveStraightBehavior));
            AssertArchetype(catalog, "exploder", 30, 2.5f, 0.0f, 2, typeof(MoveStraightBehavior), typeof(ExplodeOnContactBehavior), typeof(ExplodeOnDeathBehavior));
            AssertArchetype(catalog, "spitter", 40, 1.0f, 5.0f, 2, typeof(StopAtBehavior), typeof(RangedSpitBehavior));
            AssertArchetype(catalog, "shielded", 80, 1.5f, 10.0f, 3, typeof(MoveStraightBehavior), typeof(FrontShieldBehavior));
            AssertArchetype(catalog, "shaman", 60, 1.0f, 5.0f, 4, typeof(StopAtBehavior), typeof(HealAuraBehavior), typeof(ResurrectBehavior));
        }

        [Test]
        public void ImportAll_IsolatedSource_ImportsCustomEnemy()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex752_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string json = @"{
                ""id"": ""custom_zombie"",
                ""baseHp"": 50,
                ""speed"": 3.0,
                ""contactDps"": 8.0,
                ""coinValue"": 2,
                ""behaviors"": [
                    { ""type"": ""MoveStraight"" }
                ]
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "custom_zombie.json"), json);

            var errors = new List<string>();
            int count = EnemyImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(1, count);
            Assert.IsEmpty(errors);

            var catalog = EnemyImporter.LoadCatalog(TempTargetFolder);
            Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGet("custom_zombie", out var enemy));
            Assert.AreEqual(50, enemy.BaseHp);
            Assert.AreEqual(3.0f, enemy.Speed, 0.001f);
            Assert.AreEqual(8.0f, enemy.ContactDps, 0.001f);
            Assert.AreEqual(2, enemy.CoinValue);
        }

        [Test]
        public void ImportAll_InvalidBaseHp_ReportsError()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex752_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string json = @"{
                ""id"": ""broken_zombie"",
                ""baseHp"": -5,
                ""speed"": 1.0,
                ""contactDps"": 1.0,
                ""coinValue"": 1
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "broken_zombie.json"), json);

            var errors = new List<string>();
            int count = EnemyImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("baseHp", errors[0]);
        }

        private static void AssertArchetype(EnemyCatalog catalog, string id, int hp, float speed, float dps, int coins, params Type[] expectedBehaviors)
        {
            Assert.IsTrue(catalog.TryGet(id, out var enemy), $"Catalog must contain '{id}'.");
            Assert.AreEqual(id, enemy.Id);
            Assert.AreEqual(hp, enemy.BaseHp, $"BaseHp mismatch for '{id}'");
            Assert.AreEqual(speed, enemy.Speed, 0.001f, $"Speed mismatch for '{id}'");
            Assert.AreEqual(dps, enemy.ContactDps, 0.001f, $"ContactDps mismatch for '{id}'");
            Assert.AreEqual(coins, enemy.CoinValue, $"CoinValue mismatch for '{id}'");

            Assert.IsNotNull(enemy.Behaviors, $"Behaviors null for '{id}'");
            Assert.AreEqual(expectedBehaviors.Length, enemy.Behaviors.Length, $"Behavior count mismatch for '{id}'");
            for (int i = 0; i < expectedBehaviors.Length; i++)
            {
                Assert.IsInstanceOf(expectedBehaviors[i], enemy.Behaviors[i], $"Behavior at index {i} mismatch for '{id}'");
            }
        }
    }
}
