using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Ads;
using Game.Meta.Ads;
using Game.Meta.Save;
using Game.Meta.Save.Migrations;
using NUnit.Framework;

namespace Game.Tests.EditMode.Ads
{
    [TestFixture]
    public class SaveStoreV3MigrationTests
    {
        private string _testSavePath;

        [SetUp]
        public void SetUp()
        {
            _testSavePath = Path.Combine(Path.GetTempPath(), $"save_migration_v3_test_{Guid.NewGuid():N}.json");
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
        public void CT_10_ValidateIntegrity_V2SaveFileOnDisk_PassesWithoutCorruption()
        {
            var storeV2 = new SaveStore(null, null, currentVersion: 2);
            var v2State = new MetaState
            {
                SaveVersion = 2,
                LastSeenUtcTicks = 638200000000000000L,
                Currencies = new Dictionary<string, int>
                {
                    { "coins", 1500 },
                    { "medals", 20 }
                },
                ClaimedGrantIds = new HashSet<string> { "grant_v2" },
                Entitlements = new HashSet<string> { "starter_pack" }
            };

            storeV2.Save(v2State, _testSavePath);

            Assert.IsTrue(File.Exists(_testSavePath));
            Assert.IsTrue(storeV2.ValidateIntegrity(_testSavePath));

            var storeV3 = new SaveStore(null, null, currentVersion: 3);
            Assert.IsTrue(storeV3.ValidateIntegrity(_testSavePath));
        }

        [Test]
        public void CT_10_Load_V2SaveFile_WithV3Store_AppliesSaveMigrationV2ToV3_PreservingDataAndInitializingAdPolicy()
        {
            var storeV2 = new SaveStore(null, null, currentVersion: 2);
            var v2State = new MetaState
            {
                SaveVersion = 2,
                LastSeenUtcTicks = 638250000000000000L,
                Currencies = new Dictionary<string, int>
                {
                    { "coins", 4200 },
                    { "medals", 88 }
                },
                ClaimedGrantIds = new HashSet<string> { "beta_supporter" },
                Entitlements = new HashSet<string> { "no_ads" }
            };

            storeV2.Save(v2State, _testSavePath);

            var storeV3 = new SaveStore(null, null, currentVersion: 3);
            MetaState loadedV3 = storeV3.Load(_testSavePath);

            Assert.IsNotNull(loadedV3);
            Assert.AreEqual(3, loadedV3.SaveVersion);
            Assert.AreEqual(638250000000000000L, loadedV3.LastSeenUtcTicks);
            Assert.AreEqual(4200, loadedV3.Currencies["coins"]);
            Assert.AreEqual(88, loadedV3.Currencies["medals"]);
            Assert.IsTrue(loadedV3.ClaimedGrantIds.Contains("beta_supporter"));
            Assert.IsTrue(loadedV3.Entitlements.Contains("no_ads"));

            Assert.IsNotNull(loadedV3.AdPolicy);
            Assert.AreEqual(0, loadedV3.AdPolicy.DailyRewardedCount);
            Assert.AreEqual(0, loadedV3.AdPolicy.StagesCompletedSinceLastInterstitial);
            Assert.IsNotNull(loadedV3.AdPolicy.PlacementDailyCounts);
        }

        [Test]
        public void Load_V1SaveFile_WithV3Store_AppliesChainedMigrationsV1ToV2ToV3_Successfully()
        {
            var storeV1 = new SaveStore(null, null, currentVersion: 1);
            var v1State = new MetaState
            {
                SaveVersion = 1,
                LastSeenUtcTicks = 638000000000000000L,
                Currencies = new Dictionary<string, int> { { "coins", 100 } }
            };

            storeV1.Save(v1State, _testSavePath);

            var storeV3 = new SaveStore(null, null, currentVersion: 3);
            MetaState loadedV3 = storeV3.Load(_testSavePath);

            Assert.IsNotNull(loadedV3);
            Assert.AreEqual(3, loadedV3.SaveVersion);
            Assert.AreEqual(100, loadedV3.Currencies["coins"]);
            Assert.IsNotNull(loadedV3.Barracks);
            Assert.IsNotNull(loadedV3.AdPolicy);
        }

        [Test]
        public void Save_V3State_RoundTrip_PreservesAdPolicyState()
        {
            var storeV3 = new SaveStore(null, null, currentVersion: 3);
            var v3State = new MetaState
            {
                SaveVersion = 3,
                LastSeenUtcTicks = 638300000000000000L,
                Currencies = new Dictionary<string, int> { { "coins", 9999 } },
                AdPolicy = new AdPolicyState
                {
                    LastAdShownUtcTicks = 638300000000000100L,
                    LastRewardedOrPurchaseUtcTicks = 638300000000000200L,
                    LastInterstitialUtcTicks = 638300000000000300L,
                    StagesCompletedSinceLastInterstitial = 4,
                    StagesCompletedInCurrentSession = 2,
                    CurrentSessionDateUtcTicks = 638300000000000000L,
                    SessionsStartedInCurrentDay = 1,
                    DailyRewardedCount = 5,
                    DailyRewardedDateUtcTicks = 638300000000000000L,
                    PlacementDailyCounts = new Dictionary<string, int>
                    {
                        { AdPlacement.DoubleReward.ToString(), 3 },
                        { AdPlacement.BarracksSpeedup.ToString(), 2 }
                    }
                }
            };

            storeV3.Save(v3State, _testSavePath);

            Assert.IsTrue(storeV3.ValidateIntegrity(_testSavePath));

            MetaState loaded = storeV3.Load(_testSavePath);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(3, loaded.SaveVersion);
            Assert.AreEqual(638300000000000100L, loaded.AdPolicy.LastAdShownUtcTicks);
            Assert.AreEqual(638300000000000200L, loaded.AdPolicy.LastRewardedOrPurchaseUtcTicks);
            Assert.AreEqual(638300000000000300L, loaded.AdPolicy.LastInterstitialUtcTicks);
            Assert.AreEqual(4, loaded.AdPolicy.StagesCompletedSinceLastInterstitial);
            Assert.AreEqual(2, loaded.AdPolicy.StagesCompletedInCurrentSession);
            Assert.AreEqual(5, loaded.AdPolicy.DailyRewardedCount);
            Assert.AreEqual(3, loaded.AdPolicy.PlacementDailyCounts[AdPlacement.DoubleReward.ToString()]);
            Assert.AreEqual(2, loaded.AdPolicy.PlacementDailyCounts[AdPlacement.BarracksSpeedup.ToString()]);
        }

        [Test]
        public void MigrateJson_DirectCallV2ToV3_CalculatesValidIntegrityHash()
        {
            var migration = new SaveMigrationV2ToV3();
            string v2Json = "{\"SaveVersion\":2,\"LastSeenUtcTicks\":638200000000000000,\"Currencies\":{},\"ClaimedGrantIds\":[],\"Entitlements\":[],\"StageStars\":{},\"UnlockedStages\":[],\"TrackStates\":{},\"Barracks\":{\"Levels\":{\"Recruitment\":1,\"Armory\":1,\"Canteen\":1,\"Infirmary\":1,\"Ammunition\":1},\"ActiveConstructionType\":null,\"ConstructionCompletionUtcTicks\":0,\"LastCanteenCollectionUtcTicks\":0},\"IntegrityHash\":null}";
            string v2Hash = SaveStore.ComputeHash(v2Json);
            string authenticV2Json = v2Json.Replace("\"IntegrityHash\":null", $"\"IntegrityHash\":\"{v2Hash}\"");

            File.WriteAllText(_testSavePath, authenticV2Json);
            var storeV2 = new SaveStore(null, null, currentVersion: 2);
            Assert.IsTrue(storeV2.ValidateIntegrity(_testSavePath));

            string migratedV3Json = migration.MigrateJson(authenticV2Json);
            File.WriteAllText(_testSavePath, migratedV3Json);

            var storeV3 = new SaveStore(null, null, currentVersion: 3);
            Assert.IsTrue(storeV3.ValidateIntegrity(_testSavePath));

            MetaState loaded = storeV3.Load(_testSavePath);
            Assert.AreEqual(3, loaded.SaveVersion);
            Assert.IsNotNull(loaded.AdPolicy);
        }
    }
}
