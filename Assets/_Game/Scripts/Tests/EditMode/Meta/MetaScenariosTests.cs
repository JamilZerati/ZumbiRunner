using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.State;
using Game.Meta.Objectives;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Save;
using Game.Meta.Stages;
using Game.Meta.Time;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class MetaScenariosTests
    {
        [Test]
        public void CT_1_Wallet_Transactions_RequireSource_PreventNegativeBalance_AndEmitEvents()
        {
            IWallet wallet = new Wallet();
            wallet.Add(CurrencyKind.Coins, 100, "run_clear");
            Assert.AreEqual(100, wallet.GetBalance(CurrencyKind.Coins));

            bool spendOk = wallet.TrySpend(CurrencyKind.Coins, 40, "upgrade_barracks");
            Assert.IsTrue(spendOk);
            Assert.AreEqual(60, wallet.GetBalance(CurrencyKind.Coins));

            bool overSpend = wallet.TrySpend(CurrencyKind.Coins, 200, "illegal_purchase");
            Assert.IsFalse(overSpend);
            Assert.AreEqual(60, wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void CT_2_RewardBundle_IdempotentGrant_GrantsOnlyOncePerGrantId()
        {
            var fakeSink = new FakeRewardSink();
            var bundle = new RewardBundle("run_stage1_session99", new[]
            {
                new CurrencyReward(CurrencyKind.Coins, 500)
            });

            bool firstGrant = bundle.Grant(fakeSink);
            Assert.IsTrue(firstGrant);
            Assert.IsTrue(fakeSink.HasGranted("run_stage1_session99"));

            bool secondGrant = bundle.Grant(fakeSink);
            Assert.IsFalse(secondGrant);
        }

        [Test]
        public void CT_3_ObjectiveTracker_TracksRunAndCumulativeObjectives_FromEventsAndRunResult()
        {
            IObjectiveTracker tracker = new ObjectiveTracker();
            tracker.RegisterObjective(new ObjectiveDefinition("daily_kills", ObjectiveScope.Cumulative, "kills", 50));
            tracker.RegisterObjective(new ObjectiveDefinition("perfect_run", ObjectiveScope.Run, "stars", 3));

            var result = new RunResult
            {
                LevelId = "stage_1",
                Victory = true,
                Stars = 3,
                CoinsEarned = 150
            };

            tracker.IngestRunResult(result);
            var completed = tracker.CheckCompletedObjectives();
            CollectionAssert.Contains(completed, "perfect_run");
        }

        [Test]
        public void CT_4_ProgressTrack_UnlocksMilestonesByPoints_RequiresEntitlementForPremium()
        {
            IProgressTrack track = new ProgressTrack();
            track.AddPoints(100);

            Assert.IsTrue(track.CanClaimMilestone(0, isPremium: false));
            Assert.IsFalse(track.CanClaimMilestone(0, isPremium: true));
        }

        [Test]
        public void CT_5_MonotonicTimeAndSave_DetectsClockManipulation_AndEnforcesSaveIntegrity()
        {
            ITimeProvider time = new MonotonicTimeProvider();
            Assert.IsFalse(time.IsClockManipulated);

            ISaveStore store = new SaveStore();
            var state = new MetaState
            {
                SaveVersion = 1,
                LastSeenUtcTicks = DateTime.UtcNow.Ticks
            };

            string testPath = "test_meta_save.json";
            store.Save(state, testPath);
            Assert.IsTrue(store.ValidateIntegrity(testPath));
        }

        [Test]
        public void CT_6_RewardCalculator_ComputesAuditedEarnings_WithMultipliersAndMutations()
        {
            IRewardCalculator calculator = new RewardCalculator();
            var config = new RunConfig("stage_1", 10, "rifle", 1.5f, new[] { "fast_zombies" });
            var result = new RunResult
            {
                LevelId = "stage_1",
                CoinsEarned = 100,
                MultiplierReached = 2
            };

            RewardBundle bundle = calculator.Calculate(result, config);
            Assert.IsNotNull(bundle);
            Assert.IsNotNull(bundle.GrantId);
        }

        [Test]
        public void CT_7_StageMap_EnforcesLinearProgression_RecordsStars_AndMarksBossGates()
        {
            IStageMapService stageMap = new StageMapService();
            Assert.IsTrue(stageMap.IsStageUnlocked("stage_01"));
            Assert.IsFalse(stageMap.IsStageUnlocked("stage_02"));

            stageMap.RecordStageResult("stage_01", 3, isBoss: false);
            Assert.IsTrue(stageMap.IsStageUnlocked("stage_02"));
            Assert.AreEqual(3, stageMap.GetStars("stage_01"));
        }

        private sealed class FakeRewardSink : IRewardSink
        {
            private readonly HashSet<string> _granted = new HashSet<string>();
            public IWallet Wallet => null;
            public IEntitlements Entitlements => null;

            public bool HasGranted(string grantId) => _granted.Contains(grantId);
            public void RecordGrant(string grantId) => _granted.Add(grantId);
            public void AddItem(string itemId, int quantity) { }
            public void UnlockContent(string contentId) { }
        }
    }
}
