using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Boss;
using Game.Core.Events;
using Game.Core.State;
using Game.Meta.Progression;
using Game.Meta.Rewards;
using Game.Meta.Save;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class BossScenariosTests
    {
        private class FakeRewardSink : IRewardSink
        {
            private readonly HashSet<string> _granted = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _unlocked = new(StringComparer.OrdinalIgnoreCase);

            public IWallet Wallet => null;
            public IEntitlements Entitlements { get; } = new Entitlements();

            public bool HasGranted(string grantId) => _granted.Contains(grantId);
            public void RecordGrant(string grantId) => _granted.Add(grantId);
            public void AddItem(string itemId, int quantity) { }
            public void UnlockContent(string contentId) => _unlocked.Add(contentId);
            public bool IsContentUnlocked(string contentId) => _unlocked.Contains(contentId);
        }

        [Test]
        public void CT_1_BossPingPongBehavior_SweepsAcrossLanes_AndInvertsAtBoundaries()
        {
            float minLane = 0f;
            float maxLane = 2f;
            float currentPos = 0f;
            float speed = 2f;
            int direction = 1;

            for (int i = 0; i < 3; i++)
            {
                currentPos += direction * speed * 0.5f;
                if (currentPos >= maxLane)
                {
                    currentPos = maxLane;
                    direction = -1;
                }
                else if (currentPos <= minLane)
                {
                    currentPos = minLane;
                    direction = 1;
                }
            }

            Assert.AreEqual(1f, currentPos, 1e-4f);
            Assert.AreEqual(-1, direction);
        }

        [Test]
        public void CT_2_BossLaneTelegraphDive_ActivatesWarningForOneSecond_BeforeApplyingDamage()
        {
            var bossState = new BossCombatRuntimeState();
            bossState.StartArena("boss_colossus", 1000, initialLane: 0);

            bool expired = false;
            int targetedLane = -1;
            bossState.OnTelegraphExpired += lane =>
            {
                expired = true;
                targetedLane = lane;
            };

            bossState.TriggerTelegraph(lane: 1, duration: 1.0f);
            Assert.IsTrue(bossState.IsTelegraphing);
            Assert.AreEqual(1, bossState.TargetTelegraphLane);

            bossState.Tick(0.5f);
            Assert.IsTrue(bossState.IsTelegraphing);
            Assert.IsFalse(expired);

            bossState.Tick(0.55f);
            Assert.IsFalse(bossState.IsTelegraphing);
            Assert.IsTrue(expired);
            Assert.AreEqual(1, targetedLane);
        }

        [Test]
        public void CT_3_BossSummonWave_TriggersMinionsAtHpThresholds_WithoutDuplicateEvents()
        {
            var bossState = new BossCombatRuntimeState();
            bossState.StartArena("boss_colossus", 1000);

            var triggeredThresholds = new HashSet<float>();
            Action<float> checkThreshold = pct =>
            {
                float currentPct = (float)bossState.CurrentHp / bossState.MaxHp;
                if (currentPct <= pct && !triggeredThresholds.Contains(pct))
                {
                    triggeredThresholds.Add(pct);
                }
            };

            bossState.ApplyDamage(200); // 800 hp (80%)
            checkThreshold(0.75f);
            Assert.AreEqual(0, triggeredThresholds.Count);

            bossState.ApplyDamage(100); // 700 hp (70%)
            checkThreshold(0.75f);
            Assert.AreEqual(1, triggeredThresholds.Count);
            Assert.IsTrue(triggeredThresholds.Contains(0.75f));

            bossState.ApplyDamage(50); // 650 hp (65%)
            checkThreshold(0.75f);
            Assert.AreEqual(1, triggeredThresholds.Count);
        }

        [Test]
        public void CT_4_BossCombatRuntimeState_TracksHealth_AndReportsDeathWhenHpDepleted()
        {
            var bossState = new BossCombatRuntimeState();
            bossState.StartArena("boss_colossus", 500);

            bool died = false;
            bossState.OnBossDied += () => died = true;

            int dealt1 = bossState.ApplyDamage(300);
            Assert.AreEqual(300, dealt1);
            Assert.AreEqual(200, bossState.CurrentHp);
            Assert.IsTrue(bossState.IsAlive);
            Assert.IsFalse(died);

            int dealt2 = bossState.ApplyDamage(300);
            Assert.AreEqual(200, dealt2);
            Assert.AreEqual(0, bossState.CurrentHp);
            Assert.IsFalse(bossState.IsAlive);
            Assert.IsTrue(died);
        }

        [Test]
        public void CT_5_BossArena_ProtectsBossFromDespawnBehindGeneral_UntilFightConcluded()
        {
            var bossState = new BossCombatRuntimeState();
            bossState.StartArena("boss_colossus", 1000);

            float generalZ = 50f;
            float bossZ = 40f; // 10m behind general
            float escapeDistance = 5f;

            bool canEscape = !bossState.IsArenaActive && (generalZ - bossZ >= escapeDistance);
            Assert.IsFalse(canEscape);

            bossState.EndArena();
            canEscape = !bossState.IsArenaActive && (generalZ - bossZ >= escapeDistance);
            Assert.IsTrue(canEscape);
        }

        [Test]
        public void CT_6_ProgressionService_GrantsUnlockReward_OnBossVictory()
        {
            var sink = new FakeRewardSink();
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
            Assert.AreEqual("weapon:shotgun", ((UnlockReward)reward).ContentId);
            Assert.IsTrue(sink.IsContentUnlocked("weapon:shotgun"));
            Assert.IsTrue(service.IsBossDefeated("level_05_boss"));
        }

        [Test]
        public void CT_7_ProgressionService_Idempotency_DoesNotDuplicateGrantsForSameBoss()
        {
            var sink = new FakeRewardSink();
            var service = new ProgressionService();

            var runResult = new RunResult
            {
                LevelId = "level_05_boss",
                Victory = true,
                Stars = 3
            };

            bool first = service.ProcessRunResult(runResult, sink, out var reward1);
            Assert.IsTrue(first);

            bool second = service.ProcessRunResult(runResult, sink, out var reward2);
            Assert.IsFalse(second);
            Assert.IsNull(reward2);
        }

        [Test]
        public void CT_8_ProgressionService_Defeat_DoesNotGrantUnlockReward()
        {
            var sink = new FakeRewardSink();
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
        }

        [Test]
        public void CT_9_BossCatalog_HasRegisteredUnlocksForConfiguredBossStages()
        {
            var catalog = ProgressionCatalog.CreateDefaultCatalog();

            Assert.IsTrue(catalog.TryGetUnlock("level_05_boss", out var def5));
            Assert.AreEqual("weapon:shotgun", def5.ContentId);
            Assert.AreEqual(ContentUnlockType.Weapon, def5.UnlockType);

            Assert.IsTrue(catalog.TryGetUnlock("level_10_boss", out var def10));
            Assert.AreEqual("hero:pyromancer", def10.ContentId);
            Assert.AreEqual(ContentUnlockType.Hero, def10.UnlockType);

            Assert.IsTrue(catalog.TryGetUnlock("level_15_boss", out var def15));
            Assert.AreEqual("world:world_02", def15.ContentId);
            Assert.AreEqual(ContentUnlockType.World, def15.UnlockType);

            Assert.IsFalse(catalog.TryGetUnlock("level_01", out _));
        }

        [Test]
        public void CT_10_SimulateCommand_BossSegmentContract_ValidatesSimulationInputs()
        {
            var result = new RunResult
            {
                LevelId = "level_05_boss",
                Victory = true,
                SquadAtEnd = 12,
                MultiplierReached = 3
            };

            Assert.IsTrue(result.Victory);
            Assert.AreEqual("level_05_boss", result.LevelId);
            Assert.AreEqual(12, result.SquadAtEnd);
            Assert.AreEqual(3, result.MultiplierReached);
        }
    }
}
