using System;

namespace Game.Core.Gates
{
    [Serializable]
    public sealed class CursedGateVariant : IGateVariant
    {
        public string bonusPerkId = string.Empty;
        public StageModifierSpec stageModifier;
        public float durationDistance = 100f;

        public string BonusPerkId => bonusPerkId ?? string.Empty;
        public StageModifierSpec StageModifier => stageModifier;
        public float DurationDistance => durationDistance;

        public GateVariantType VariantType => GateVariantType.Cursed;
        public bool AcceptsProjectiles => false;

        public CursedGateVariant()
        {
        }

        public CursedGateVariant(string bonusPerkId, StageModifierSpec stageModifier, float durationDistance)
        {
            this.bonusPerkId = bonusPerkId ?? string.Empty;
            this.stageModifier = stageModifier;
            this.durationDistance = durationDistance;
        }

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

        public void ActivateModifier(StageModifierController controller, float currentDistance)
        {
            throw new NotImplementedException();
        }

        public bool Validate(out string error)
        {
            throw new NotImplementedException();
        }
    }
}
