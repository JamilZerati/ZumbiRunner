namespace Game.Core.Events
{
    public class BossTelegraphEvent
    {
        public int TargetLane { get; }
        public float Duration { get; }

        public BossTelegraphEvent(int targetLane, float duration)
        {
            TargetLane = targetLane;
            Duration = duration;
        }
    }
}
