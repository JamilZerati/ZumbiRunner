using System.Collections.Generic;
using Game.Core.State;
using Game.Meta.Progression;
using Game.Meta.Rewards;
using Game.Meta.Save;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class ProgressionServiceTests
    {
        [Test]
        public void ProcessRunResult_Victory_GrantsUnlockReward_AndRecordsGrant()
        {
            var sink = new RewardSink();
            var service = new ProgressionService();

            var runResult = new RunResult
            {
                LevelId = "level_05_boss",
                Victory = true,
                Stars = 3
            };

            bool granted = service.ProcessRunResult(runResult, sink, out var reward);

            Assert.IsTrue(granted);
            Assert.IsNotNull(reward);
            var unlockReward = reward as UnlockReward;
            Assert.IsNotNull(unlockReward);
            Assert.AreEqual("weapon:shotgun", unlockReward.ContentId);
            Assert.IsTrue(sink.IsContentUnlocked("weapon:shotgun"));
            Assert.IsTrue(sink.HasGranted("unlock_boss_level_05_boss_weapon:shotgun"));
            Assert.IsTrue(service.IsBossDefeated("level_05_boss"));
            Assert.IsTrue(service.IsContentUnlocked("weapon:shotgun"));
        }

        [Test]
        public void ProcessRunResult_Idempotency_SecondVictoryDoesNotGrantDuplicate()
        {
            var sink = new RewardSink();
            var service = new ProgressionService();

            var runResult = new RunResult
            {
                LevelId = "level_05_boss",
                Victory = true,
                Stars = 3
            };

            bool first = service.ProcessRunResult(runResult, sink, out var reward1);
            Assert.IsTrue(first);
            Assert.IsNotNull(reward1);

            bool second = service.ProcessRunResult(runResult, sink, out var reward2);
            Assert.IsFalse(second);
            Assert.IsNull(reward2);
        }

        [Test]
        public void ProcessRunResult_Defeat_DoesNotGrantUnlockReward()
        {
            var sink = new RewardSink();
            var service = new ProgressionService();

            var runResult = new RunResult
            {
                LevelId = "level_05_boss",
                Victory = false,
                Stars = 0
            };

            bool granted = service.ProcessRunResult(runResult, sink, out var reward);

            Assert.IsFalse(granted);
            Assert.IsNull(reward);
            Assert.IsFalse(sink.IsContentUnlocked("weapon:shotgun"));
            Assert.IsFalse(sink.HasGranted("unlock_boss_level_05_boss_weapon:shotgun"));
            Assert.IsFalse(service.IsBossDefeated("level_05_boss"));
            Assert.IsFalse(service.IsContentUnlocked("weapon:shotgun"));
        }

        [Test]
        public void ProcessRunResult_StageWithoutUnlock_DoesNotGrant()
        {
            var sink = new RewardSink();
            var service = new ProgressionService();

            var runResult = new RunResult
            {
                LevelId = "level_01",
                Victory = true,
                Stars = 3
            };

            bool granted = service.ProcessRunResult(runResult, sink, out var reward);

            Assert.IsFalse(granted);
            Assert.IsNull(reward);
        }

        [Test]
        public void ProcessRunResult_InvalidInputs_ReturnsFalseSafely()
        {
            var sink = new RewardSink();
            var service = new ProgressionService();

            Assert.IsFalse(service.ProcessRunResult(null, sink, out var reward1));
            Assert.IsNull(reward1);

            var validResult = new RunResult { LevelId = "level_05_boss", Victory = true };
            Assert.IsFalse(service.ProcessRunResult(validResult, null, out var reward2));
            Assert.IsNull(reward2);

            var emptyLevelResult = new RunResult { LevelId = "", Victory = true };
            Assert.IsFalse(service.ProcessRunResult(emptyLevelResult, sink, out var reward3));
            Assert.IsNull(reward3);
        }

        [Test]
        public void ProgressionService_WithExistingMetaState_LoadsExistingGrantsAndEntitlements()
        {
            var metaState = new MetaState
            {
                ClaimedGrantIds = new HashSet<string> { "unlock_boss_level_05_boss_weapon:shotgun" },
                Entitlements = new HashSet<string> { "weapon:shotgun" }
            };

            var service = new ProgressionService(metaState: metaState);
            var sink = new RewardSink();

            Assert.IsTrue(service.IsBossDefeated("level_05_boss"));
            Assert.IsTrue(service.IsContentUnlocked("weapon:shotgun"));

            var runResult = new RunResult
            {
                LevelId = "level_05_boss",
                Victory = true,
                Stars = 3
            };

            bool granted = service.ProcessRunResult(runResult, sink, out var reward);
            Assert.IsFalse(granted);
            Assert.IsNull(reward);
        }

        [Test]
        public void ProgressionService_UpdatesMetaState_WhenGranted()
        {
            var metaState = new MetaState();
            var service = new ProgressionService(metaState: metaState);
            var sink = new RewardSink();

            var runResult = new RunResult
            {
                LevelId = "level_10_boss",
                Victory = true,
                Stars = 3
            };

            bool granted = service.ProcessRunResult(runResult, sink, out var reward);

            Assert.IsTrue(granted);
            Assert.IsNotNull(reward);
            Assert.IsTrue(metaState.ClaimedGrantIds.Contains("unlock_boss_level_10_boss_hero:pyromancer"));
            Assert.IsTrue(metaState.Entitlements.Contains("hero:pyromancer"));
            Assert.IsTrue(service.IsBossDefeated("level_10_boss"));
            Assert.IsTrue(service.IsContentUnlocked("hero:pyromancer"));
        }

        [Test]
        public void ProgressionCatalog_CustomRegistration_AndRetrieval()
        {
            var catalog = new ProgressionCatalog();
            catalog.RegisterUnlock("level_99_boss", "weapon:laser_cannon", ContentUnlockType.Weapon);

            Assert.IsTrue(catalog.TryGetUnlock("level_99_boss", out var def));
            Assert.AreEqual("level_99_boss", def.LevelId);
            Assert.AreEqual("weapon:laser_cannon", def.ContentId);
            Assert.AreEqual(ContentUnlockType.Weapon, def.UnlockType);

            Assert.IsFalse(catalog.TryGetUnlock("level_non_existent", out _));
            Assert.IsFalse(catalog.TryGetUnlock(null, out _));

            int count = 0;
            foreach (var item in catalog.AllUnlocks)
            {
                count++;
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ProgressionService_MultipleBossStages_GrantsExpectedContent()
        {
            var sink = new RewardSink();
            var service = new ProgressionService();

            var r5 = new RunResult { LevelId = "level_05_boss", Victory = true };
            var r10 = new RunResult { LevelId = "level_10_boss", Victory = true };
            var r15 = new RunResult { LevelId = "level_15_boss", Victory = true };

            Assert.IsTrue(service.ProcessRunResult(r5, sink, out var rew5));
            Assert.AreEqual("weapon:shotgun", ((UnlockReward)rew5).ContentId);

            Assert.IsTrue(service.ProcessRunResult(r10, sink, out var rew10));
            Assert.AreEqual("hero:pyromancer", ((UnlockReward)rew10).ContentId);

            Assert.IsTrue(service.ProcessRunResult(r15, sink, out var rew15));
            Assert.AreEqual("world:world_02", ((UnlockReward)rew15).ContentId);

            Assert.IsTrue(service.IsBossDefeated("level_05_boss"));
            Assert.IsTrue(service.IsBossDefeated("level_10_boss"));
            Assert.IsTrue(service.IsBossDefeated("level_15_boss"));

            Assert.IsTrue(service.IsContentUnlocked("weapon:shotgun"));
            Assert.IsTrue(service.IsContentUnlocked("hero:pyromancer"));
            Assert.IsTrue(service.IsContentUnlocked("world:world_02"));
        }
    }
}
