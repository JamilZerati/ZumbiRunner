using System;
using System.Collections.Generic;
using System.IO;
using Game.Core;
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
        private string _testSavePath;

        [SetUp]
        public void SetUp()
        {
            _testSavePath = Path.Combine(Path.GetTempPath(), $"meta_test_save_{Guid.NewGuid():N}.json");
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
        public void CT_1_Wallet_Transactions_RequireSource_PreventNegativeBalance_AndEmitEvents()
        {
            var eventBus = new EventBus();
            var emittedEvents = new List<WalletChangedEvent>();
            eventBus.Subscribe<WalletChangedEvent>(e => emittedEvents.Add(e));

            IWallet wallet = new Wallet(eventBus);
            wallet.Add(CurrencyKind.Coins, 100, "run_clear");
            Assert.AreEqual(100, wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(1, emittedEvents.Count);
            Assert.AreEqual(100, emittedEvents[0].Delta);
            Assert.AreEqual("run_clear", emittedEvents[0].Source);

            bool spendOk = wallet.TrySpend(CurrencyKind.Coins, 40, "upgrade_barracks");
            Assert.IsTrue(spendOk);
            Assert.AreEqual(60, wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(2, emittedEvents.Count);
            Assert.AreEqual(-40, emittedEvents[1].Delta);

            bool overSpend = wallet.TrySpend(CurrencyKind.Coins, 200, "illegal_purchase");
            Assert.IsFalse(overSpend);
            Assert.AreEqual(60, wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(2, emittedEvents.Count);
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

            tracker.IngestEvent(new EnemyKilledEvent("zombie_walker", 0));
            Assert.AreEqual(1, tracker.GetProgress("daily_kills").CurrentValue);

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
            var freeReward = new RewardBundle("track_free_0", new[] { new CurrencyReward(CurrencyKind.Coins, 50) });
            var premiumReward = new RewardBundle("track_prem_0", new[] { new CurrencyReward(CurrencyKind.Medals, 5) });
            var milestones = new[]
            {
                new MilestoneDefinition(0, 100, freeReward, premiumReward)
            };
            var def = new ProgressTrackDefinition("season_pass_1", milestones, "entitlement_battlepass_s1");
            IEntitlements entitlements = new Entitlements();
            IProgressTrack track = new ProgressTrack(def, entitlements);

            track.AddPoints(100);

            Assert.IsTrue(track.CanClaimMilestone(0, isPremium: false));
            Assert.IsFalse(track.CanClaimMilestone(0, isPremium: true));

            entitlements.GrantEntitlement("entitlement_battlepass_s1");
            Assert.IsTrue(track.CanClaimMilestone(0, isPremium: true));
        }

        [Test]
        public void CT_5_MonotonicTimeAndSave_DetectsClockManipulation_AndEnforcesSaveIntegrity()
        {
            ITimeProvider time = new MonotonicTimeProvider();
            Assert.IsFalse(time.IsClockManipulated);
            time.RecordCurrentTime();

            ISaveStore store = new SaveStore();
            var state = new MetaState
            {
                SaveVersion = 1,
                LastSeenUtcTicks = DateTime.UtcNow.Ticks
            };

            store.Save(state, _testSavePath);
            Assert.IsTrue(store.ValidateIntegrity(_testSavePath));

            File.AppendAllText(_testSavePath, "tampered_bytes");
            Assert.IsFalse(store.ValidateIntegrity(_testSavePath));
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
            Assert.AreEqual(1, bundle.Rewards.Count);
            var currencyReward = bundle.Rewards[0] as CurrencyReward;
            Assert.IsNotNull(currencyReward);
            Assert.AreEqual(CurrencyKind.Coins, currencyReward.Currency);
            Assert.AreEqual(300, currencyReward.Amount);
        }

        [Test]
        public void CT_7_StageMap_EnforcesLinearProgression_RecordsStars_AndMarksBossGates()
        {
            IStageMapService stageMap = new StageMapService();
            Assert.IsTrue(stageMap.IsStageUnlocked("stage_01"));
            Assert.IsFalse(stageMap.IsStageUnlocked("stage_02"));

            stageMap.RecordStageResult("stage_01", 3, isBoss: true);
            Assert.IsTrue(stageMap.IsStageUnlocked("stage_02"));
            Assert.AreEqual(3, stageMap.GetStars("stage_01"));
            CollectionAssert.Contains(stageMap.GetAllBossStages(), "stage_01");
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
