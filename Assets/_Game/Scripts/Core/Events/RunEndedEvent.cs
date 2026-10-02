using Game.Core.State;

namespace Game.Core.Events
{
    public readonly struct RunEndedEvent
    {
        public RunResult Result { get; }

        public RunEndedEvent(RunResult result)
        {
            Result = result;
        }
    }
}
