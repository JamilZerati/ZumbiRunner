using System;
using System.Collections.Generic;
using System.IO;
using Game.Meta.Save;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class SaveStoreTests
    {
        private string _testSavePath;

        [SetUp]
        public void SetUp()
        {
            _testSavePath = Path.Combine(Path.GetTempPath(), $"save_test_{Guid.NewGuid():N}.json");
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
        public void SaveAndLoad_RoundTrip_PreservesState()
        {
            var store = new SaveStore();
            var state = new MetaState
            {
                SaveVersion = 1,
                LastSeenUtcTicks = 638000000000000000L,
                Currencies = new Dictionary<string, int>
                {
                    { "coins", 1500 },
                    { "medals", 25 }
                },
                ClaimedGrantIds = new HashSet<string> { "grant_1", "grant_2" },
                Entitlements = new HashSet<string> { "battlepass_s1" },
                StageStars = new Dictionary<string, int> { { "stage_1", 3 } },
                UnlockedStages = new HashSet<string> { "stage_1", "stage_2" }
            };

            store.Save(state, _testSavePath);

            Assert.IsTrue(File.Exists(_testSavePath));
            Assert.IsTrue(store.ValidateIntegrity(_testSavePath));

            MetaState loaded = store.Load(_testSavePath);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.SaveVersion);
            Assert.AreEqual(638000000000000000L, loaded.LastSeenUtcTicks);
            Assert.AreEqual(1500, loaded.Currencies["coins"]);
            Assert.AreEqual(25, loaded.Currencies["medals"]);
            Assert.IsTrue(loaded.ClaimedGrantIds.Contains("grant_1"));
            Assert.IsTrue(loaded.ClaimedGrantIds.Contains("grant_2"));
            Assert.IsTrue(loaded.Entitlements.Contains("battlepass_s1"));
            Assert.AreEqual(3, loaded.StageStars["stage_1"]);
            Assert.IsTrue(loaded.UnlockedStages.Contains("stage_2"));
        }

        [Test]
        public void ValidateIntegrity_ReturnsFalse_WhenFileDoesNotExist()
        {
            var store = new SaveStore();
            Assert.IsFalse(store.ValidateIntegrity("non_existent_file_path.json"));
        }

        [Test]
        public void ValidateIntegrity_ReturnsFalse_WhenTampered()
        {
            var store = new SaveStore();
            var state = new MetaState { SaveVersion = 1, LastSeenUtcTicks = 12345 };
            store.Save(state, _testSavePath);

            Assert.IsTrue(store.ValidateIntegrity(_testSavePath));

            string json = File.ReadAllText(_testSavePath);
            string tampered = json.Replace("12345", "99999");
            File.WriteAllText(_testSavePath, tampered);

            Assert.IsFalse(store.ValidateIntegrity(_testSavePath));
        }

        [Test]
        public void Load_Throws_WhenIntegrityFails()
        {
            var store = new SaveStore();
            var state = new MetaState { SaveVersion = 1 };
            store.Save(state, _testSavePath);

            File.AppendAllText(_testSavePath, "corrupted");

            Assert.Throws<InvalidOperationException>(() => store.Load(_testSavePath));
        }

        [Test]
        public void Load_AppliesMigrations_WhenVersionIsOlder()
        {
            var migrationV0ToV1 = new TestMigration(0, 1, json => json.Replace("\"SaveVersion\": 0", "\"SaveVersion\": 1"));
            var store = new SaveStore(new[] { migrationV0ToV1 });

            var storeV0 = new SaveStore(currentVersion: 0);
            var oldState = new MetaState { SaveVersion = 0, LastSeenUtcTicks = 55555 };
            storeV0.Save(oldState, _testSavePath);

            MetaState loaded = store.Load(_testSavePath);
            Assert.AreEqual(1, loaded.SaveVersion);
            Assert.AreEqual(55555, loaded.LastSeenUtcTicks);
        }

        [Test]
        public void Load_Throws_WhenRequiredMigrationIsMissing()
        {
            var store = new SaveStore();
            var storeV0 = new SaveStore(currentVersion: 0);
            var oldState = new MetaState { SaveVersion = 0, LastSeenUtcTicks = 55555 };
            storeV0.Save(oldState, _testSavePath);

            Assert.Throws<InvalidOperationException>(() => store.Load(_testSavePath));
        }

        private sealed class TestMigration : ISaveMigration
        {
            private readonly Func<string, string> _migrationFunc;
            public int SourceVersion { get; }
            public int TargetVersion { get; }

            public TestMigration(int source, int target, Func<string, string> func)
            {
                SourceVersion = source;
                TargetVersion = target;
                _migrationFunc = func;
            }

            public string MigrateJson(string sourceJson) => _migrationFunc(sourceJson);
        }
    }
}
