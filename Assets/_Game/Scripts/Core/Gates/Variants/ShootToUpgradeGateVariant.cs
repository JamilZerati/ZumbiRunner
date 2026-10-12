using System;

namespace Game.Core.Gates
{
    [Serializable]
    public sealed class ShootToUpgradeGateVariant : IGateVariant
    {
        public int initialValue = -5;
        public int step = 1;
        public int hitsPerStep = 1;
        public int cap = 15;

        public int InitialValue => initialValue;
        public int Step => step;
        public int HitsPerStep => hitsPerStep;
        public int Cap => cap;

        public GateVariantType VariantType => GateVariantType.ShootToUpgrade;
        public bool AcceptsProjectiles => true;

        public ShootToUpgradeGateVariant()
        {
        }

        public ShootToUpgradeGateVariant(int initialValue, int step, int hitsPerStep, int cap)
        {
            this.initialValue = initialValue;
            this.step = step;
            this.hitsPerStep = hitsPerStep;
            this.cap = cap;
        }

        public int EvaluateValue(int totalHits)
        {
            throw new NotImplementedException();
        }

        public GateRuntimeState CreateInitialState(string defaultDisplayText = null)
        {
            throw new NotImplementedException();
        }

        public void RegisterHit(GateRuntimeState state, int hitCount = 1)
        {
            throw new NotImplementedException();
        }

        public int ApplyToSquad(ISquad squad, GateRuntimeState state, int defaultAppliedValue = 0)
        {
            throw new NotImplementedException();
        }

        public bool Validate(out string error)
        {
            throw new NotImplementedException();
        }
    }
}
