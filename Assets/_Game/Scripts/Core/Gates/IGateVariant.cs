namespace Game.Core.Gates
{
    public interface IGateVariant
    {
        GateVariantType VariantType { get; }
        bool AcceptsProjectiles { get; }
        GateRuntimeState CreateInitialState(string defaultDisplayText);
        void RegisterHit(GateRuntimeState state, int hitCount = 1);
        int ApplyToSquad(ISquad squad, GateRuntimeState state, int defaultAppliedValue = 0);
        bool Validate(out string error);
    }
}
