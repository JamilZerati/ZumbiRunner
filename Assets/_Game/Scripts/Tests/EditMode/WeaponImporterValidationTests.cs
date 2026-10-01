using System;
using System.Collections.Generic;
using System.IO;
using Game.Data;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    public class WeaponImporterValidationTests
    {
        private const string TempAssetParent = "Assets/_Game";
        private const string TempAssetFolderName = "NEX569_ImporterTestTmp";
        private const string TargetFolder = TempAssetParent + "/" + TempAssetFolderName;

        private string sourceFolder;
        private List<string> errors;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            sourceFolder = Path.Combine(Path.GetTempPath(), "nex569_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sourceFolder);
            errors = new List<string>();

            if (AssetDatabase.IsValidFolder(TargetFolder))
            {
                AssetDatabase.DeleteAsset(TargetFolder);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(sourceFolder))
            {
                Directory.Delete(sourceFolder, true);
            }

            if (AssetDatabase.IsValidFolder(TargetFolder))
            {
                AssetDatabase.DeleteAsset(TargetFolder);
            }
        }

        private void WriteSource(string fileName, string singleQuotedJson)
        {
            File.WriteAllText(Path.Combine(sourceFolder, fileName), singleQuotedJson.Replace('\'', '"'));
        }

        private int Import(string singleQuotedJson)
        {
            WriteSource("w.json", singleQuotedJson);
            return WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);
        }

        [TestCase("fireRate", "{ 'id': 'w', 'fireRate': 1e39, 'damage': 1, 'projectileSpeed': 1, 'range': 1, 'projectilesPerShot': 1 }")]
        [TestCase("range", "{ 'id': 'w', 'fireRate': 1, 'damage': 1, 'projectileSpeed': 1, 'range': -1e39, 'projectilesPerShot': 1 }")]
        [TestCase("spreadWidth", "{ 'id': 'w', 'fireRate': 1, 'damage': 1, 'projectileSpeed': 1, 'range': 1, 'projectilesPerShot': 1, 'spreadWidth': 1e39 }")]
        public void ImportAll_NonFiniteNumber_IsRejected(string field, string json)
        {
            Assert.AreEqual(0, Import(json));
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(field, errors[0]);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{TargetFolder}/w.asset"));
        }

        [TestCase("")]
        [TestCase("WeaponCatalog")]
        [TestCase("a/b")]
        public void ImportAll_InvalidId_IsRejected(string id)
        {
            int count = Import("{ 'id': '" + id + "', 'fireRate': 1, 'damage': 1, 'projectileSpeed': 1, 'range': 1, 'projectilesPerShot': 1 }");

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("w.json: id inválido", errors[0]);
            Assert.AreEqual(0, WeaponImporter.LoadCatalog(TargetFolder).Weapons.Count);
        }

        [Test]
        public void ImportAll_SeveralInvalidFields_ReportsOneErrorPerField()
        {
            Import("{ 'id': 'w', 'damage': 1, 'projectileSpeed': 1, 'projectilesPerShot': 1 }");

            CollectionAssert.AreEquivalent(new[] { "w.json: fireRate inválido", "w.json: range inválido" }, errors);
        }

        [Test]
        public void ImportAll_IdsDifferingOnlyByCase_AreDuplicates()
        {
            const string numbers = "'fireRate': 1, 'damage': 1, 'projectileSpeed': 1, 'range': 1, 'projectilesPerShot': 1 }";
            WriteSource("a.json", "{ 'id': 'shotgun', " + numbers);
            WriteSource("b.json", "{ 'id': 'Shotgun', " + numbers);

            Assert.AreEqual(1, WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors));

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("b.json", errors[0]);
            Assert.AreEqual("shotgun", AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{TargetFolder}/shotgun.asset").Id);
        }

        [Test]
        public void ImportAll_MissingTargetFolder_IsCreatedWithCatalog()
        {
            Import("{ 'id': 'w', 'fireRate': 1, 'damage': 1, 'projectileSpeed': 1, 'range': 1, 'projectilesPerShot': 1 }");

            Assert.IsTrue(AssetDatabase.IsValidFolder(TargetFolder));
            Assert.IsTrue(WeaponImporter.LoadCatalog(TargetFolder).TryGet("w", out _));
        }
    }
}
