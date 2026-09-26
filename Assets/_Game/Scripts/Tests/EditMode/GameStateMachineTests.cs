using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class GameStateMachineTests
    {
        [Test]
        public void InitialState_IsBoot()
        {
            var bus = new EventBus();
            var fsm = new GameStateMachine(bus);

            Assert.AreEqual(GameState.Boot, fsm.CurrentState);
        }

        [Test]
        public void ValidTransitions_UpdateStateAndPublishEvent()
        {
            var bus = new EventBus();
            var fsm = new GameStateMachine(bus);
            GameStateChangedEvent lastEvent = default;
            bus.Subscribe<GameStateChangedEvent>(e => lastEvent = e);

            Assert.IsTrue(fsm.TryTransition(GameState.Run));
            Assert.AreEqual(GameState.Run, fsm.CurrentState);
            Assert.AreEqual(GameState.Boot, lastEvent.PreviousState);
            Assert.AreEqual(GameState.Run, lastEvent.NewState);

            Assert.IsTrue(fsm.TryTransition(GameState.Victory));
            Assert.AreEqual(GameState.Victory, fsm.CurrentState);
        }

        [Test]
        public void InvalidTransitions_AreRejected()
        {
            var bus = new EventBus();
            var fsm = new GameStateMachine(bus);

            Assert.IsFalse(fsm.TryTransition(GameState.Victory));
            Assert.AreEqual(GameState.Boot, fsm.CurrentState);

            Assert.IsFalse(fsm.TryTransition(GameState.Defeat));
            Assert.AreEqual(GameState.Boot, fsm.CurrentState);

            Assert.IsFalse(fsm.TryTransition(GameState.Boot));
            Assert.AreEqual(GameState.Boot, fsm.CurrentState);
        }
    }
}
