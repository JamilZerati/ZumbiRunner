using System;

namespace Game.Core
{
    public class GameStateMachine : IGameStateMachine
    {
        private readonly IEventBus _eventBus;
        private GameState _currentState;

        public GameState CurrentState => _currentState;

        public GameStateMachine(IEventBus eventBus, GameState initialState = GameState.Boot)
        {
            _eventBus = eventBus;
            _currentState = initialState;
        }

        public bool TryTransition(GameState nextState)
        {
            if (!IsValidTransition(_currentState, nextState))
            {
                return false;
            }

            var previous = _currentState;
            _currentState = nextState;
            _eventBus?.Publish(new GameStateChangedEvent(previous, nextState));
            return true;
        }

        private static bool IsValidTransition(GameState from, GameState to)
        {
            if (from == to)
            {
                return false;
            }

            switch (from)
            {
                case GameState.Boot:
                    return to == GameState.Hub || to == GameState.Run;
                case GameState.Hub:
                    return to == GameState.Run;
                case GameState.Run:
                    return to == GameState.Paused || to == GameState.Victory || to == GameState.Defeat || to == GameState.Result || to == GameState.Hub;
                case GameState.Paused:
                    return to == GameState.Run || to == GameState.Hub;
                case GameState.Victory:
                case GameState.Defeat:
                    return to == GameState.Result || to == GameState.Boot || to == GameState.Run || to == GameState.Hub;
                case GameState.Result:
                    return to == GameState.Hub || to == GameState.Run;
                default:
                    return false;
            }
        }
    }
}
