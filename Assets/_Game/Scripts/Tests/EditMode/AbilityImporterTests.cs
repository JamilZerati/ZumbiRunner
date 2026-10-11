using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Abilities;
using Game.Core.Abilities.Effects;
using Game.Data;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class AbilityImporterTests
    {
        private const string TempAssetParent = "Assets";
        private const string TempAssetFolderName = "_TempTests_Abilities";
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
        public void ImportAll_ImportsGrenadeAbility()
        {
            var errors = new List<string>();
            int count = AbilityImporter.ImportAll(errors: errors);

            Assert.IsEmpty(errors);
            Assert.GreaterOrEqual(count, 1);

            var catalog = AbilityImporter.LoadCatalog();
            Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGet("grenade", out var grenade));
            Assert.AreEqual("grenade", grenade.Id);
            Assert.AreEqual("Granada", grenade.DisplayName);
            Assert.AreEqual(25, grenade.ChargeKills);
            Assert.IsNotNull(grenade.Effect);
            Assert.IsInstanceOf<GrenadeAbilityEffect>(grenade.Effect);

            var directAsset = AssetDatabase.LoadAssetAtPath<GeneralAbilityDefinition>("Assets/_Game/Data/Abilities/grenade.asset");
            Assert.IsNotNull(directAsset);
            Assert.AreEqual("grenade", directAsset.Id);
        }

        [Test]
        public void ImportAll_IsolatedSource_ImportsCustomAbility()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex762_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string json = @"{
                ""id"": ""custom_grenade"",
                ""displayName"": ""Granada Custom"",
                ""description"": ""Granada de teste"",
                ""chargeKills"": 10,
                ""targeting"": {
                    ""type"": ""GroundTarget"",
                    ""maxRange"": 20.0,
                    ""radius"": 4.0
                },
                ""effect"": {
                    ""type"": ""GrenadeAbilityEffect""
                }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "custom_grenade.json"), json);

            var errors = new List<string>();
            int count = AbilityImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(1, count);
            Assert.IsEmpty(errors);

            var catalog = AbilityImporter.LoadCatalog(TempTargetFolder);
            Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGet("custom_grenade", out var ability));
            Assert.AreEqual("custom_grenade", ability.Id);
            Assert.AreEqual("Granada Custom", ability.DisplayName);
            Assert.AreEqual("Granada de teste", ability.Description);
            Assert.AreEqual(10, ability.ChargeKills);
            Assert.IsNotNull(ability.Targeting);
            Assert.AreEqual(AbilityTargetingType.GroundTarget, ability.Targeting.Type);
            Assert.AreEqual(20f, ability.Targeting.MaxRange);
            Assert.AreEqual(4f, ability.Targeting.Radius);
            Assert.IsNotNull(ability.Effect);
            Assert.IsInstanceOf<GrenadeAbilityEffect>(ability.Effect);
        }

        [Test]
        public void ImportAll_CorruptJson_ReportsError()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex762_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            File.WriteAllText(Path.Combine(_tempSourceFolder, "corrupt.json"), "{ invalid json content");

            var errors = new List<string>();
            int count = AbilityImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("JSON inválido", errors[0]);
        }

        [Test]
        public void ImportAll_MissingId_ReportsError()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex762_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string json = @"{
                ""displayName"": ""Sem ID"",
                ""chargeKills"": 25,
                ""effect"": { ""type"": ""GrenadeAbilityEffect"" }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "no_id.json"), json);

            var errors = new List<string>();
            int count = AbilityImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("id inválido", errors[0]);
        }

        [Test]
        public void ImportAll_InvalidChargeKills_ReportsError()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex762_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string json = @"{
                ""id"": ""invalid_charge"",
                ""displayName"": ""Carga Invalida"",
                ""chargeKills"": 0,
                ""effect"": { ""type"": ""GrenadeAbilityEffect"" }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "invalid_charge.json"), json);

            var errors = new List<string>();
            int count = AbilityImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("chargeKills inválido", errors[0]);
        }

        [Test]
        public void ImportAll_MissingEffect_ReportsError()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex762_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string json = @"{
                ""id"": ""no_effect"",
                ""displayName"": ""Sem Efeito"",
                ""chargeKills"": 20
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "no_effect.json"), json);

            var errors = new List<string>();
            int count = AbilityImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("effect ausente", errors[0]);
        }

        [Test]
        public void ImportAll_UnknownEffectType_ReportsError()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex762_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string json = @"{
                ""id"": ""unknown_effect"",
                ""displayName"": ""Efeito Desconhecido"",
                ""chargeKills"": 20,
                ""effect"": { ""type"": ""NonExistentEffect"" }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "unknown_effect.json"), json);

            var errors = new List<string>();
            int count = AbilityImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("desconhecido", errors[0]);
        }

        [Test]
        public void ImportAll_DuplicateId_ReportsError()
        {
            _tempSourceFolder = Path.Combine(Path.GetTempPath(), "nex762_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempSourceFolder);

            string jsonA = @"{
                ""id"": ""duplicate_ability"",
                ""displayName"": ""Primeira"",
                ""chargeKills"": 25,
                ""effect"": { ""type"": ""GrenadeAbilityEffect"" }
            }";
            string jsonB = @"{
                ""id"": ""duplicate_ability"",
                ""displayName"": ""Segunda"",
                ""chargeKills"": 30,
                ""effect"": { ""type"": ""GrenadeAbilityEffect"" }
            }";
            File.WriteAllText(Path.Combine(_tempSourceFolder, "a.json"), jsonA);
            File.WriteAllText(Path.Combine(_tempSourceFolder, "b.json"), jsonB);

            var errors = new List<string>();
            int count = AbilityImporter.ImportAll(_tempSourceFolder, TempTargetFolder, errors);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("duplicado", errors[0]);
        }

        [Test]
        public void AbilityCatalog_TryGet_NonExistentOrNullId_ReturnsFalse()
        {
            var catalog = ScriptableObject.CreateInstance<AbilityCatalog>();

            Assert.IsFalse(catalog.TryGet("non_existent", out var def1));
            Assert.IsNull(def1);

            Assert.IsFalse(catalog.TryGet(null, out var def2));
            Assert.IsNull(def2);

            Assert.IsFalse(catalog.TryGet(string.Empty, out var def3));
            Assert.IsNull(def3);
        }

        [Test]
        public void AbilityCatalog_SetAbilities_HandlesNullsGracefully()
        {
            var catalog = ScriptableObject.CreateInstance<AbilityCatalog>();
            catalog.SetAbilities(null);
            Assert.AreEqual(0, catalog.Abilities.Count);

            var validAbility = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            validAbility.SetData("valid", "Valid", "Desc", 10, new GrenadeAbilityEffect(), null);
            catalog.SetAbilities(new[] { validAbility, null });
            Assert.AreEqual(1, catalog.Abilities.Count);
            Assert.IsTrue(catalog.TryGet("valid", out _));
        }
    }
}
