using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Allies;
using Game.Core.Perks.Effects;
using Game.Data;
using Game.Data.Allies;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class AllyImporterTests
    {
        private const string TempAssetParent = "Assets";
        private const string TempAssetFolderName = "_TempTests_Allies";
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
        public void ImportAll_ImportsCanonicalAlliesAndCatalog()
        {
            var errors = new List<string>();
            int count = AllyImporter.ImportAll(errors: errors);

            Assert.IsEmpty(errors);
            Assert.AreEqual(3, count);

            var catalog = AllyImporter.LoadCatalog();
            Assert.IsNotNull(catalog);
            Assert.AreEqual(3, catalog.Allies.Count);

            Assert.IsTrue(catalog.TryGet("pyromancer", out var pyro));
            Assert.AreEqual("pyromancer", pyro.Id);
            Assert.AreEqual("Piromante", pyro.DisplayName);
            Assert.AreEqual(AllyArchetype.Hero, pyro.Archetype);
            Assert.AreEqual(150, pyro.MaxHealth);
            Assert.AreEqual(25, pyro.AttackDamage);
            Assert.AreEqual("burn", pyro.StatusApplied);
            Assert.AreEqual("Inferno", pyro.AbilityName);

            Assert.IsTrue(catalog.TryGet("cryomancer", out var cryo));
            Assert.AreEqual("cryomancer", cryo.Id);
            Assert.AreEqual("Criomante", cryo.DisplayName);
            Assert.AreEqual(AllyArchetype.Hero, cryo.Archetype);
            Assert.AreEqual(120, cryo.MaxHealth);
            Assert.AreEqual(18, cryo.AttackDamage);
            Assert.AreEqual("freeze", cryo.StatusApplied);
            Assert.AreEqual("Blizzard", cryo.AbilityName);

            Assert.IsTrue(catalog.TryGet("guard_dog", out var dog));
            Assert.AreEqual("guard_dog", dog.Id);
            Assert.AreEqual("Cão de Guarda", dog.DisplayName);
            Assert.AreEqual(AllyArchetype.Pet, dog.Archetype);
            Assert.AreEqual(0.15f, dog.PetStatModifierValue, 0.001f);
        }

        [Test]
        public void PerkImporter_ImportsRecruitAllyPerksSuccessfully()
        {
            var errors = new List<string>();
            AllyImporter.ImportAll(errors: errors);
            Assert.IsEmpty(errors);

            int perkCount = PerkImporter.ImportAll(errors: errors);
            Assert.IsEmpty(errors);
            Assert.GreaterOrEqual(perkCount, 3);

            var perk = AssetDatabase.LoadAssetAtPath<PerkDefinition>("Assets/_Game/Data/Perks/recruit_pyromancer.asset");
            Assert.IsNotNull(perk);
            Assert.AreEqual(1, perk.Effects.Count);
            Assert.IsInstanceOf<RecruitAllyPerkEffect>(perk.Effects[0]);
            var effect = (RecruitAllyPerkEffect)perk.Effects[0];
            Assert.AreEqual("pyromancer", effect.AllyId);
        }

        [Test]
        public void ImportAll_IsolatedSource_ReportsDuplicateAndInvalidArchetype()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex963_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string invalidJson = @"{
                ""id"": ""invalid_archetype"",
                ""displayName"": ""Invalido"",
                ""archetype"": ""Alien"",
                ""maxHealth"": 100
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "invalid.json"), invalidJson);

            var errors = new List<string>();
            int count = AllyImporter.ImportAll(sourceFolder: _tempSourceFolder, targetFolder: TempTargetFolder, errors: errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            Assert.IsTrue(errors[0].Contains("archetype 'Alien' invalido"));
        }
    }
}
