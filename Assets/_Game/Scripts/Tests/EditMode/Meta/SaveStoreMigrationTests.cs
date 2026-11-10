using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Meta.Save;
using Game.Meta.Save.Migrations;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class SaveStoreMigrationTests
    {
        private string _testSavePath;

        [SetUp]
        public void SetUp()
        {
            _testSavePath = Path.Combine(Path.GetTempPath(), $"save_migration_test_{Guid.NewGuid():N}.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_testSavePath))
            {
                File.Delete(_testSavePath);
            }
        }

        [Test]
        public void ValidateIntegrity_V1SaveFileOnDisk_PassesWithoutCorruption()
        {
            var storeV1 = new SaveStore(null, null, currentVersion: 1);
            var v1State = new MetaState
            {
                SaveVersion = 1,
                LastSeenUtcTicks = 638000000000000000L,
                Currencies = new Dictionary<string, int>
                {
                    { "coins", 2500 },
                    { "medals", 30 }
                },
                ClaimedGrantIds = new HashSet<string> { "grant_v1_legacy" },
                Entitlements = new HashSet<string> { "founder_pack" },
                StageStars = new Dictionary<string, int> { { "stage_1", 3 } },
                UnlockedStages = new HashSet<string> { "stage_1", "stage_2" }
            };

            storeV1.Save(v1State, _testSavePath);

            Assert.IsTrue(File.Exists(_testSavePath));
            Assert.IsTrue(storeV1.ValidateIntegrity(_testSavePath));

            var storeV2 = new SaveStore(null, null, currentVersion: 2);
            Assert.IsTrue(storeV2.ValidateIntegrity(_testSavePath), "Validação de integridade V1 em store V2 deve passar sem corrupção.");
        }

        [Test]
        public void Load_V1SaveFile_WithV2Store_AppliesSaveMigrationV1ToV2_PreservingDataAndInitializingBarracks()
        {
            var storeV1 = new SaveStore(null, null, currentVersion: 1);
            var v1State = new MetaState
            {
                SaveVersion = 1,
                LastSeenUtcTicks = 638100000000000000L,
                Currencies = new Dictionary<string, int>
                {
                    { "coins", 5000 },
                    { "medals", 100 }
                },
                ClaimedGrantIds = new HashSet<string> { "grant_alpha" },
                Entitlements = new HashSet<string> { "vip_status" }
            };

            storeV1.Save(v1State, _testSavePath);

            var storeV2 = new SaveStore(null, null, currentVersion: 2);
            MetaState loadedV2 = storeV2.Load(_testSavePath);

            Assert.IsNotNull(loadedV2);
            Assert.AreEqual(2, loadedV2.SaveVersion);
            Assert.AreEqual(638100000000000000L, loadedV2.LastSeenUtcTicks);
            Assert.AreEqual(5000, loadedV2.Currencies["coins"]);
            Assert.AreEqual(100, loadedV2.Currencies["medals"]);
            Assert.IsTrue(loadedV2.ClaimedGrantIds.Contains("grant_alpha"));
            Assert.IsTrue(loadedV2.Entitlements.Contains("vip_status"));

            Assert.IsNotNull(loadedV2.Barracks);
            Assert.AreEqual(1, loadedV2.Barracks.GetLevel("Recruitment"));
            Assert.AreEqual(1, loadedV2.Barracks.GetLevel("Armory"));
            Assert.AreEqual(1, loadedV2.Barracks.GetLevel("Canteen"));
            Assert.AreEqual(1, loadedV2.Barracks.GetLevel("Infirmary"));
            Assert.AreEqual(1, loadedV2.Barracks.GetLevel("Ammunition"));
            Assert.IsNull(loadedV2.Barracks.ActiveConstructionType);
        }

        [Test]
        public void Save_V2State_RoundTrip_PreservesBarracksState()
        {
            var storeV2 = new SaveStore(null, null, currentVersion: 2);
            var state = new MetaState
            {
                SaveVersion = 2,
                LastSeenUtcTicks = 638200000000000000L,
                Currencies = new Dictionary<string, int>
                {
                    { "coins", 10000 },
                    { "recruits", 50 },
                    { "medals", 20 }
                }
            };
            state.Barracks.SetLevel("Recruitment", 2);
            state.Barracks.SetLevel("Armory", 3);
            state.Barracks.ActiveConstructionType = "Armory";
            state.Barracks.ConstructionEndUtcTicks = 638250000000000000L;
            state.Barracks.TargetLevel = 4;
            state.Barracks.AdSpeedupsUsedToday = 2;
            state.Barracks.LastAdUsageDateUtcTicks = 638190000000000000L;
            state.Barracks.LastCanteenCollectionUtcTicks = 638195000000000000L;

            storeV2.Save(state, _testSavePath);

            Assert.IsTrue(storeV2.ValidateIntegrity(_testSavePath));

            MetaState loaded = storeV2.Load(_testSavePath);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(2, loaded.SaveVersion);
            Assert.AreEqual(10000, loaded.Currencies["coins"]);
            Assert.AreEqual(50, loaded.Currencies["recruits"]);
            Assert.AreEqual(2, loaded.Barracks.GetLevel("Recruitment"));
            Assert.AreEqual(3, loaded.Barracks.GetLevel("Armory"));
            Assert.AreEqual("Armory", loaded.Barracks.ActiveConstructionType);
            Assert.AreEqual(638250000000000000L, loaded.Barracks.ConstructionEndUtcTicks);
            Assert.AreEqual(4, loaded.Barracks.TargetLevel);
            Assert.AreEqual(2, loaded.Barracks.AdSpeedupsUsedToday);
        }

        [Test]
        public void ValidateIntegrity_WhenTampered_ReturnsFalse()
        {
            var storeV2 = new SaveStore(null, null, currentVersion: 2);
            var state = new MetaState { SaveVersion = 2, LastSeenUtcTicks = 11111 };
            storeV2.Save(state, _testSavePath);

            Assert.IsTrue(storeV2.ValidateIntegrity(_testSavePath));

            string json = File.ReadAllText(_testSavePath);
            string tampered = json.Replace("11111", "99999");
            File.WriteAllText(_testSavePath, tampered);

            Assert.IsFalse(storeV2.ValidateIntegrity(_testSavePath));
        }

        [Test]
        public void SaveMigrationV1ToV2_DirectInvocation_TransformsDeterministicJson()
        {
            string authenticV1JsonWithoutBarracks = "{\"SaveVersion\":1,\"LastSeenUtcTicks\":638000000000000000,\"Currencies\":{\"coins\":1000,\"medals\":10},\"ClaimedGrantIds\":[],\"Entitlements\":[],\"StageStars\":{},\"UnlockedStages\":[],\"TrackStates\":{},\"IntegrityHash\":null}";
            string v1Hash = SaveStore.ComputeHash(authenticV1JsonWithoutBarracks);
            string fullV1Json = authenticV1JsonWithoutBarracks.Replace("\"IntegrityHash\":null", $"\"IntegrityHash\":\"{v1Hash}\"");

            File.WriteAllText(_testSavePath, fullV1Json, Encoding.UTF8);

            var store = new SaveStore(null, null, currentVersion: 1);
            Assert.IsTrue(store.ValidateIntegrity(_testSavePath), "V1 estático autêntico sem campo Barracks deve validar.");

            var migration = new SaveMigrationV1ToV2();
            string v2Json = migration.MigrateJson(fullV1Json);

            Assert.That(v2Json, Does.Contain("\"SaveVersion\": 2"));
            Assert.That(v2Json, Does.Contain("\"Barracks\":"));
            Assert.That(v2Json, Does.Contain("\"IntegrityHash\":"));

            File.WriteAllText(_testSavePath, v2Json, Encoding.UTF8);
            var storeV2 = new SaveStore(null, null, currentVersion: 2);
            Assert.IsTrue(storeV2.ValidateIntegrity(_testSavePath), "V2 migrado deve validar integridade.");
        }
    }
}
