using System;

namespace Game.Core.Gates
{
    [Serializable]
    public sealed class StaticGateVariant : IGateVariant
    {
        public GateVariantType VariantType => GateVariantType.Static;
        public bool AcceptsProjectiles => false;

        public GateRuntimeState CreateInitialState(string defaultDisplayText)
        {
            throw new NotImplementedException();
        }

        public void RegisterHit(GateRuntimeState state, int hitCount = 1)
        {
        }

        public int ApplyToSquad(ISquad squad, GateRuntimeState state, int defaultAppliedValue = 0)
        {
            throw new NotImplementedException();
        }

        public bool Validate(out string error)
        {
            error = null;
            return true;
        }
    }
}
