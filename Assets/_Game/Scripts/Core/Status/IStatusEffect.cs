namespace Game.Core.Status
{
    public interface IStatusEffect
    {
        StatusKind Kind { get; }
        bool IsPersistent { get; }
        void OnApply(IStatusHost host, StatusState state, int stacks);
        void OnTick(IStatusHost host, StatusState state, float deltaTime);
        void OnHostDied(IStatusHost host, StatusState state);
        float MoveSpeedMultiplier(StatusState state);
    }
}
