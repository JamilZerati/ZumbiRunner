namespace Game.Core
{
    public readonly struct GameStateChangedEvent
    {
        public GameState PreviousState { get; }
        public GameState NewState { get; }

        public GameStateChangedEvent(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    public interface IGameStateMachine
    {
        GameState CurrentState { get; }
        bool TryTransition(GameState nextState);
    }
}
