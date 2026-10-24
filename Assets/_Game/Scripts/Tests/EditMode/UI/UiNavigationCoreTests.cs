using System.Collections.Generic;
using Game.Core;
using Game.Core.State;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode.UI
{
    public class UiNavigationCoreTests
    {
        private sealed class FakeRewardSink : IRewardSink
        {
            private readonly HashSet<string> _granted = new();
            public IWallet Wallet => null;
            public IEntitlements Entitlements => null;

            public bool HasGranted(string grantId) => _granted.Contains(grantId);
            public void RecordGrant(string grantId) => _granted.Add(grantId);
            public void AddItem(string itemId, int quantity) { }
            public void UnlockContent(string contentId) { }
        }

        [Test]
        public void CT_1_GameStateMachine_SupportsHubAndNavigationFlow_AndRejectsInvalidTransitions()
        {
            var bus = new EventBus();
            var fsm = new GameStateMachine(bus, GameState.Boot);

            GameStateChangedEvent lastEvent = default;
            bus.Subscribe<GameStateChangedEvent>(e => lastEvent = e);

            Assert.IsTrue(fsm.TryTransition(GameState.Hub));
            Assert.AreEqual(GameState.Hub, fsm.CurrentState);
            Assert.AreEqual(GameState.Boot, lastEvent.PreviousState);
            Assert.AreEqual(GameState.Hub, lastEvent.NewState);

            Assert.IsFalse(fsm.TryTransition(GameState.Paused));
            Assert.AreEqual(GameState.Hub, fsm.CurrentState);

            Assert.IsTrue(fsm.TryTransition(GameState.Run));
            Assert.AreEqual(GameState.Run, fsm.CurrentState);

            Assert.IsTrue(fsm.TryTransition(GameState.Paused));
            Assert.AreEqual(GameState.Paused, fsm.CurrentState);

            Assert.IsTrue(fsm.TryTransition(GameState.Run));
            Assert.AreEqual(GameState.Run, fsm.CurrentState);

            Assert.IsTrue(fsm.TryTransition(GameState.Result));
            Assert.AreEqual(GameState.Result, fsm.CurrentState);

            Assert.IsTrue(fsm.TryTransition(GameState.Hub));
            Assert.AreEqual(GameState.Hub, fsm.CurrentState);

            Assert.IsFalse(fsm.TryTransition(GameState.Result));
            Assert.IsFalse(fsm.TryTransition(GameState.Victory));

            Assert.IsTrue(fsm.TryTransition(GameState.Run));
            Assert.IsTrue(fsm.TryTransition(GameState.Victory));
            Assert.AreEqual(GameState.Victory, fsm.CurrentState);

            Assert.IsFalse(fsm.TryTransition(GameState.Paused));
            Assert.IsTrue(fsm.TryTransition(GameState.Result));
            Assert.AreEqual(GameState.Result, fsm.CurrentState);

            Assert.IsTrue(fsm.TryTransition(GameState.Hub));
            Assert.IsTrue(fsm.TryTransition(GameState.Run));
            Assert.IsTrue(fsm.TryTransition(GameState.Defeat));
            Assert.AreEqual(GameState.Defeat, fsm.CurrentState);

            Assert.IsFalse(fsm.TryTransition(GameState.Paused));
            Assert.IsTrue(fsm.TryTransition(GameState.Result));
            Assert.AreEqual(GameState.Result, fsm.CurrentState);

            Assert.IsFalse(fsm.TryTransition(GameState.Paused));
            Assert.IsFalse(fsm.TryTransition(GameState.Victory));
        }

        [Test]
        public void CT_2_ResultRewardPresenter_CalculatesCoinsAndDoubling_WithIdempotentGrant()
        {
            var calculator = new RewardCalculator();
            var config = new RunConfig
            {
                LevelId = "stage_01",
                InitialSquad = 10,
                WeaponId = "smg",
                RewardMultiplier = 1.0f
            };
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };

            var normalBundle = calculator.Calculate(result, config, doubleReward: false);
            Assert.AreEqual(200, ((CurrencyReward)normalBundle.Rewards[0]).Amount);

            var doubledBundle = calculator.Calculate(result, config, doubleReward: true);
            Assert.AreEqual(400, ((CurrencyReward)doubledBundle.Rewards[0]).Amount);

            var sink = new FakeRewardSink();
            Assert.IsTrue(doubledBundle.Grant(sink));
            Assert.IsFalse(doubledBundle.Grant(sink));

            var zeroResult = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 0,
                MultiplierReached = 2,
                Stars = 1
            };
            var zeroBundle = calculator.Calculate(zeroResult, config, doubleReward: false);
            Assert.AreEqual(0, ((CurrencyReward)zeroBundle.Rewards[0]).Amount);
        }

        [Test]
        public void CT_3_PauseLifecycle_PausesDuringRun_AndAllowsQuitToHub()
        {
            var bus = new EventBus();
            var fsm = new GameStateMachine(bus, GameState.Boot);

            fsm.TryTransition(GameState.Run);
            Assert.AreEqual(GameState.Run, fsm.CurrentState);

            Assert.IsTrue(fsm.TryTransition(GameState.Paused));
            Assert.AreEqual(GameState.Paused, fsm.CurrentState);

            Assert.IsTrue(fsm.TryTransition(GameState.Hub));
            Assert.AreEqual(GameState.Hub, fsm.CurrentState);
        }
    }
}
