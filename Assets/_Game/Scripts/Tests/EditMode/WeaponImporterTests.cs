using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Core.Perks.Effects;
using Game.Core.Stats;
using Game.Data;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    public class WeaponImporterTests
    {
        private const float Tolerance = 0.0001f;
        private const string TempAssetParent = "Assets/_Game";
        private const string TempAssetFolderName = "NEX567_ImporterTestTmp";
        private const string TargetFolder = TempAssetParent + "/" + TempAssetFolderName;

        private const string ValidShotgunJson =
            "{ 'id': 'shotgun', 'displayName': 'Escopeta', 'fireRate': 1.2, 'damage': 8, " +
            "'projectileSpeed': 14, 'range': 22, 'projectilesPerShot': 3, 'spreadWidth': 1.2 }";

        private string sourceFolder;
        private List<string> errors;

        [SetUp]
        public void SetUp()
        {
            // O importador pode registrar erro no console; o contrato verificado aqui é a lista `errors`.
            LogAssert.ignoreFailingMessages = true;

            sourceFolder = Path.Combine(Path.GetTempPath(), "nex567_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sourceFolder);
            errors = new List<string>();

            if (AssetDatabase.IsValidFolder(TargetFolder))
            {
                AssetDatabase.DeleteAsset(TargetFolder);
            }
            AssetDatabase.CreateFolder(TempAssetParent, TempAssetFolderName);
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

        private static WeaponDefinition LoadWeaponAsset(string id)
        {
            return AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{TargetFolder}/{id}.asset");
        }

        private static PerkDefinition LoadPerkAsset(string id)
        {
            return AssetDatabase.LoadAssetAtPath<PerkDefinition>($"{TargetFolder}/{id}.asset");
        }

        [Test]
        public void ImportAll_ValidWeapon_CreatesAssetAndCatalogEntryWithJsonNumbers()
        {
            WriteSource("shotgun.json", ValidShotgunJson);

            int count = WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(1, count);
            CollectionAssert.IsEmpty(errors);
            var asset = LoadWeaponAsset("shotgun");
            Assert.IsNotNull(asset);
            Assert.AreEqual("shotgun", asset.Id);
            Assert.AreEqual("Escopeta", asset.DisplayName);

            var catalog = WeaponImporter.LoadCatalog(TargetFolder);
            Assert.IsNotNull(catalog);
            Assert.AreEqual($"{TargetFolder}/{WeaponImporter.CatalogAssetName}.asset", AssetDatabase.GetAssetPath(catalog));
            Assert.IsTrue(catalog.TryGet("shotgun", out WeaponProfile profile));
            Assert.AreEqual(1.2f, profile.FireRate, Tolerance);
            Assert.AreEqual(8, profile.Damage);
            Assert.AreEqual(14f, profile.ProjectileSpeed, Tolerance);
            Assert.AreEqual(22f, profile.Range, Tolerance);
            Assert.AreEqual(3, profile.ProjectilesPerShot);
            Assert.AreEqual(1.2f, profile.SpreadWidth, Tolerance);
        }

        [Test]
        public void ImportAll_WeaponWithoutFireRate_ReportsErrorWithFileAndFieldAndCreatesNoAsset()
        {
            WriteSource("sem_fire_rate.json",
                "{ 'id': 'broken', 'displayName': 'X', 'damage': 8, 'projectileSpeed': 14, " +
                "'range': 22, 'projectilesPerShot': 1, 'spreadWidth': 0 }");

            int count = WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("sem_fire_rate", errors[0]);
            StringAssert.Contains("fireRate", errors[0]);
            Assert.IsNull(LoadWeaponAsset("broken"));
            Assert.IsNull(LoadWeaponAsset("sem_fire_rate"));
        }

        [TestCase("damage", "{ 'id': 'w', 'displayName': 'W', 'fireRate': 1, 'damage': 0, 'projectileSpeed': 1, 'range': 1, 'projectilesPerShot': 1 }")]
        [TestCase("projectileSpeed", "{ 'id': 'w', 'displayName': 'W', 'fireRate': 1, 'damage': 1, 'projectileSpeed': -1, 'range': 1, 'projectilesPerShot': 1 }")]
        [TestCase("range", "{ 'id': 'w', 'displayName': 'W', 'fireRate': 1, 'damage': 1, 'projectileSpeed': 1, 'range': 0, 'projectilesPerShot': 1 }")]
        [TestCase("projectilesPerShot", "{ 'id': 'w', 'displayName': 'W', 'fireRate': 1, 'damage': 1, 'projectileSpeed': 1, 'range': 1 }")]
        [TestCase("fireRate", "{ 'id': 'w', 'displayName': 'W', 'fireRate': -2, 'damage': 1, 'projectileSpeed': 1, 'range': 1, 'projectilesPerShot': 1 }")]
        [TestCase("spreadWidth", "{ 'id': 'w', 'displayName': 'W', 'fireRate': 1, 'damage': 1, 'projectileSpeed': 1, 'range': 1, 'projectilesPerShot': 1, 'spreadWidth': -0.1 }")]
        public void ImportAll_RequiredNumberMissingZeroOrNegative_IsRejected(string field, string json)
        {
            WriteSource("w.json", json);

            int count = WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(field, errors[0]);
            Assert.IsNull(LoadWeaponAsset("w"));
        }

        [Test]
        public void ImportAll_SpreadWidthAbsent_IsValidAndDefaultsToZero()
        {
            WriteSource("smg.json",
                "{ 'id': 'smg', 'displayName': 'SMG', 'fireRate': 6, 'damage': 4, 'projectileSpeed': 20, " +
                "'range': 35, 'projectilesPerShot': 1 }");

            int count = WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(1, count);
            CollectionAssert.IsEmpty(errors);
            Assert.AreEqual(0f, LoadWeaponAsset("smg").ToProfile().SpreadWidth, Tolerance);
        }

        [Test]
        public void ImportAll_InvalidFileAmongValid_ImportsOnlyValidAndCatalogIgnoresInvalid()
        {
            WriteSource("shotgun.json", ValidShotgunJson);
            WriteSource("broken.json", "{ 'id': 'broken', 'displayName': 'B', 'fireRate': 1, 'damage': 0, 'projectileSpeed': 1, 'range': 1, 'projectilesPerShot': 1 }");

            int count = WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, errors.Count);
            var catalog = WeaponImporter.LoadCatalog(TargetFolder);
            Assert.IsTrue(catalog.TryGet("shotgun", out _));
            Assert.IsFalse(catalog.TryGet("broken", out _));
            Assert.AreEqual(1, catalog.Weapons.Count);
        }

        [Test]
        public void ImportAll_DuplicateIdAcrossFiles_ReportsErrorAndCatalogHasSingleEntry()
        {
            WriteSource("a.json", ValidShotgunJson);
            WriteSource("b.json", ValidShotgunJson);

            WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.GreaterOrEqual(errors.Count, 1);
            StringAssert.Contains("shotgun", string.Join("\n", errors));
            var catalog = WeaponImporter.LoadCatalog(TargetFolder);
            Assert.AreEqual(1, catalog.Weapons.Count(w => w != null && w.Id == "shotgun"));
        }

        [Test]
        public void ImportAll_MalformedJson_ReportsErrorWithoutThrowing()
        {
            WriteSource("garbage.json", "this is not json {");

            int count = -1;
            Assert.DoesNotThrow(() => count = WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors));

            Assert.AreEqual(0, count);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("garbage", errors[0]);
        }

        [Test]
        public void ImportAll_Twice_IsIdempotent()
        {
            WriteSource("shotgun.json", ValidShotgunJson);

            int first = WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);
            string firstPath = AssetDatabase.GetAssetPath(LoadWeaponAsset("shotgun"));
            int second = WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(first, second);
            Assert.AreEqual(firstPath, AssetDatabase.GetAssetPath(LoadWeaponAsset("shotgun")));
            Assert.AreEqual(1, WeaponImporter.LoadCatalog(TargetFolder).Weapons.Count);
            CollectionAssert.IsEmpty(errors);
        }

        [Test]
        public void ImportAll_NonExistentSource_ReturnsZero()
        {
            Assert.AreEqual(0, WeaponImporter.ImportAll("Content/NonExistentWeaponsPath", TargetFolder, errors));
        }

        [Test]
        public void ImportAll_DefaultContent_ProducesPistolShotgunAndSmgFromPlanTable()
        {
            int count = WeaponImporter.ImportAll(errors: errors);

            Assert.GreaterOrEqual(count, 3);
            CollectionAssert.IsEmpty(errors);
            var catalog = WeaponImporter.LoadCatalog();
            Assert.IsNotNull(catalog);

            AssertProfile(catalog, "pistol", 2f, 2, 15f, 40f, 1, 0f);
            AssertProfile(catalog, "shotgun", 1.2f, 2, 14f, 22f, 3, 1.2f);
            AssertProfile(catalog, "smg", 6f, 1, 20f, 35f, 1, 0f);
        }

        private static void AssertProfile(WeaponCatalog catalog, string id, float fireRate, int damage,
                                          float speed, float range, int projectiles, float spread)
        {
            Assert.IsTrue(catalog.TryGet(id, out WeaponProfile profile), $"Catalog must contain '{id}'.");
            Assert.AreEqual(id, profile.Id);
            Assert.AreEqual(fireRate, profile.FireRate, Tolerance, id);
            Assert.AreEqual(damage, profile.Damage, id);
            Assert.AreEqual(speed, profile.ProjectileSpeed, Tolerance, id);
            Assert.AreEqual(range, profile.Range, Tolerance, id);
            Assert.AreEqual(projectiles, profile.ProjectilesPerShot, id);
            Assert.AreEqual(spread, profile.SpreadWidth, Tolerance, id);
        }

        [TestCase("Shotgun")]
        [TestCase("")]
        [TestCase(null)]
        public void Catalog_TryGet_IsOrdinalCaseSensitiveAndRejectsEmpty(string weaponId)
        {
            WriteSource("shotgun.json", ValidShotgunJson);
            WeaponImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.IsFalse(WeaponImporter.LoadCatalog(TargetFolder).TryGet(weaponId, out _));
        }

        [TestCase("Armor")]
        [TestCase("7")]
        [TestCase("")]
        public void PerkImporter_StatEffectWithUnknownStat_ReportsErrorAndCreatesNoAsset(string stat)
        {
            WriteSource("bad_stat.json",
                "{ 'id': 'bad_stat', 'displayName': 'X', 'effects': [ { 'type': 'stat', 'stat': '" + stat +
                "', 'kind': 'PercentAdd', 'value': 0.25 } ] }");

            PerkImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("bad_stat", errors[0]);
            Assert.IsNull(LoadPerkAsset("bad_stat"));
        }

        [TestCase("Percent")]
        [TestCase("5")]
        public void PerkImporter_StatEffectWithUnknownKind_ReportsError(string kind)
        {
            WriteSource("bad_kind.json",
                "{ 'id': 'bad_kind', 'displayName': 'X', 'effects': [ { 'type': 'stat', 'stat': 'Damage', 'kind': '" + kind +
                "', 'value': 0.25 } ] }");

            PerkImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(1, errors.Count);
            Assert.IsNull(LoadPerkAsset("bad_kind"));
        }

        [Test]
        public void PerkImporter_PercentMultiplyMinusOne_ReportsError()
        {
            WriteSource("zero_out.json",
                "{ 'id': 'zero_out', 'displayName': 'X', 'effects': [ { 'type': 'stat', 'stat': 'Damage', 'kind': 'PercentMultiply', 'value': -1 } ] }");

            PerkImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(1, errors.Count);
            Assert.IsNull(LoadPerkAsset("zero_out"));
        }

        [Test]
        public void PerkImporter_WeaponEffectWithIdOutsideCatalog_ReportsError()
        {
            WeaponImporter.ImportAll();
            WriteSource("weapon_bazooka.json",
                "{ 'id': 'weapon_bazooka', 'displayName': 'Bazuca', 'effects': [ { 'type': 'weapon', 'weaponId': 'bazooka' } ] }");

            PerkImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("bazooka", errors[0]);
            Assert.IsNull(LoadPerkAsset("weapon_bazooka"));
        }

        [Test]
        public void PerkImporter_ValidStatEffect_CaseInsensitiveEnums_CreatesModifyStatEffect()
        {
            WriteSource("damage_up_25.json",
                "{ 'id': 'damage_up_25', 'displayName': '+25% Dano', 'effects': [ { 'type': 'stat', 'stat': 'damage', 'kind': 'percentadd', 'value': 0.25 } ] }");

            int count = PerkImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(1, count);
            CollectionAssert.IsEmpty(errors);
            var perk = LoadPerkAsset("damage_up_25");
            Assert.IsNotNull(perk);
            Assert.AreEqual(1, perk.Effects.Count);
            var effect = perk.Effects[0] as ModifyStatEffect;
            Assert.IsNotNull(effect, "Effect must be a ModifyStatEffect.");
            Assert.AreEqual(StatId.Damage, effect.Stat);
            Assert.AreEqual(ModifierKind.PercentAdd, effect.Kind);
            Assert.AreEqual(0.25f, effect.Value, Tolerance);
        }

        [Test]
        public void PerkImporter_ValidWeaponEffect_CreatesEquipWeaponEffect()
        {
            WeaponImporter.ImportAll();
            WriteSource("weapon_shotgun.json",
                "{ 'id': 'weapon_shotgun', 'displayName': 'Escopeta', 'effects': [ { 'type': 'weapon', 'weaponId': 'shotgun' } ] }");

            int count = PerkImporter.ImportAll(sourceFolder, TargetFolder, errors);

            Assert.AreEqual(1, count);
            CollectionAssert.IsEmpty(errors);
            var effect = LoadPerkAsset("weapon_shotgun").Effects[0] as EquipWeaponEffect;
            Assert.IsNotNull(effect, "Effect must be an EquipWeaponEffect.");
            Assert.AreEqual("shotgun", effect.WeaponId);
        }

        [Test]
        public void PerkImporter_DefaultContent_HasNoErrorsAndIncludesWeaponAndStatPerks()
        {
            WeaponImporter.ImportAll();

            PerkImporter.ImportAll(errors: errors);

            CollectionAssert.IsEmpty(errors);
            AssertDefaultPerkEffect<EquipWeaponEffect>("weapon_shotgun", e => Assert.AreEqual("shotgun", e.WeaponId));
            AssertDefaultPerkEffect<EquipWeaponEffect>("weapon_smg", e => Assert.AreEqual("smg", e.WeaponId));
            AssertDefaultPerkEffect<ModifyStatEffect>("damage_up_25", e =>
            {
                Assert.AreEqual(StatId.Damage, e.Stat);
                Assert.AreEqual(ModifierKind.PercentAdd, e.Kind);
                Assert.AreEqual(0.25f, e.Value, Tolerance);
            });
            AssertDefaultPerkEffect<ModifyStatEffect>("fire_rate_up_1", e =>
            {
                Assert.AreEqual(StatId.FireRate, e.Stat);
                Assert.AreEqual(ModifierKind.Flat, e.Kind);
                Assert.AreEqual(1f, e.Value, Tolerance);
            });
        }

        private static void AssertDefaultPerkEffect<T>(string perkId, Action<T> assertEffect) where T : class
        {
            var perk = AssetDatabase.LoadAssetAtPath<PerkDefinition>($"{PerkImporter.DefaultTargetPath}/{perkId}.asset");
            Assert.IsNotNull(perk, $"Perk '{perkId}' must be imported from Content/Source/Perks.");
            Assert.AreEqual(1, perk.Effects.Count, perkId);
            var effect = perk.Effects[0] as T;
            Assert.IsNotNull(effect, $"Perk '{perkId}' must hold a {typeof(T).Name}.");
            assertEffect(effect);
        }

        [Test]
        public void ComputeImportExitCode_WithErrors_IsOne()
        {
            Assert.AreEqual(1, Cli.ComputeImportExitCode(new[] { "shotgun.json: fireRate inválido" }));
        }

        [Test]
        public void ComputeImportExitCode_WithoutErrors_IsZero()
        {
            Assert.AreEqual(0, Cli.ComputeImportExitCode(Array.Empty<string>()));
            Assert.AreEqual(0, Cli.ComputeImportExitCode(null));
        }
    }
}
