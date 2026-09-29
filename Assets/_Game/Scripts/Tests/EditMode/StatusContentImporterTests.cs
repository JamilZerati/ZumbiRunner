using System.Collections.Generic;
using System.IO;
using Game.Core.Status;
using Game.Data;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class StatusContentImporterTests
    {
        private string tempSourceFolder;
        private string tempTargetFolder;

        [SetUp]
        public void SetUp()
        {
            tempSourceFolder = Path.Combine(Application.temporaryCachePath, "TestStatuses_" + System.Guid.NewGuid().ToString("N"));
            tempTargetFolder = Path.Combine(Application.temporaryCachePath, "TestTarget_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempSourceFolder);
            Directory.CreateDirectory(tempTargetFolder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempSourceFolder)) Directory.Delete(tempSourceFolder, true);
            if (Directory.Exists(tempTargetFolder)) Directory.Delete(tempTargetFolder, true);
        }

        [Test]
        public void ImportStatuses_BurnWithoutTickInterval_ReturnsErrorAndFailsImport()
        {
            File.WriteAllText(Path.Combine(tempSourceFolder, "burn.json"),
                "{ \"kind\": \"Burn\", \"duration\": 3, \"damagePerTick\": 3 }");

            var errors = new List<string>();
            int count = StatusContentImporter.ImportStatuses(tempSourceFolder, tempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.IsTrue(errors.Count > 0);
            StringAssert.Contains("tickInterval", errors[0]);
        }

        [Test]
        public void ImportStatuses_SlowPercentOutOfRange_ReturnsError()
        {
            File.WriteAllText(Path.Combine(tempSourceFolder, "slow.json"),
                "{ \"kind\": \"Slow\", \"duration\": 2, \"slowPercent\": 1.0 }");

            var errors = new List<string>();
            int count = StatusContentImporter.ImportStatuses(tempSourceFolder, tempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.IsTrue(errors.Count > 0);
            StringAssert.Contains("slowPercent", errors[0]);
        }

        [Test]
        public void ImportStatuses_InvalidEnumStrings_ReturnsError()
        {
            File.WriteAllText(Path.Combine(tempSourceFolder, "bad_kind.json"),
                "{ \"kind\": \"7\", \"duration\": 3 }");

            var errors = new List<string>();
            StatusContentImporter.ImportStatuses(tempSourceFolder, tempTargetFolder, errors);

            Assert.IsTrue(errors.Count > 0);

            errors.Clear();
            File.WriteAllText(Path.Combine(tempSourceFolder, "bad_kind2.json"),
                "{ \"kind\": \"Burn, Slow\", \"duration\": 3 }");
            StatusContentImporter.ImportStatuses(tempSourceFolder, tempTargetFolder, errors);

            Assert.IsTrue(errors.Count > 0);
        }

        [Test]
        public void ImportStatuses_DuplicateKind_ReturnsError()
        {
            File.WriteAllText(Path.Combine(tempSourceFolder, "burn1.json"),
                "{ \"kind\": \"Burn\", \"duration\": 3, \"tickInterval\": 0.5, \"damagePerTick\": 3 }");
            File.WriteAllText(Path.Combine(tempSourceFolder, "burn2.json"),
                "{ \"kind\": \"Burn\", \"duration\": 3, \"tickInterval\": 0.5, \"damagePerTick\": 3 }");

            var errors = new List<string>();
            int count = StatusContentImporter.ImportStatuses(tempSourceFolder, tempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.IsTrue(errors.Count > 0);
            StringAssert.Contains("duplicad", errors[0].ToLowerInvariant());
        }

        [Test]
        public void ImportStatuses_FreezeWithoutFrozen_ReturnsError()
        {
            File.WriteAllText(Path.Combine(tempSourceFolder, "freeze.json"),
                "{ \"kind\": \"Freeze\", \"duration\": 3, \"threshold\": 2 }");

            var errors = new List<string>();
            int count = StatusContentImporter.ImportStatuses(tempSourceFolder, tempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.IsTrue(errors.Count > 0);
            StringAssert.Contains("Frozen", errors[0]);
        }

        [Test]
        public void ImportInteractions_RequiresOutsideCatalog_ReturnsError()
        {
            File.WriteAllText(Path.Combine(tempSourceFolder, "interaction.json"),
                "{ \"id\": \"shatter\", \"requires\": \"Wet\", \"trigger\": \"HeavyHit\", \"minHitDamage\": 12, \"damageMultiplier\": 2 }");

            var errors = new List<string>();
            int count = StatusContentImporter.ImportInteractions(tempSourceFolder, tempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.IsTrue(errors.Count > 0);
        }

        [Test]
        public void ImportInteractions_DamageMultiplierLessOrEqualToOne_ReturnsError()
        {
            File.WriteAllText(Path.Combine(tempSourceFolder, "interaction.json"),
                "{ \"id\": \"shatter\", \"requires\": \"Frozen\", \"trigger\": \"HeavyHit\", \"minHitDamage\": 12, \"damageMultiplier\": 1.0 }");

            var errors = new List<string>();
            int count = StatusContentImporter.ImportInteractions(tempSourceFolder, tempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.IsTrue(errors.Count > 0);
            StringAssert.Contains("damageMultiplier", errors[0]);
        }
    }
}
