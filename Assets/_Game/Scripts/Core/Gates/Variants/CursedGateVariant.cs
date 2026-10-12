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
            return new GateRuntimeState(VariantType, AcceptsProjectiles, 0, defaultDisplayText);
        }

        public void RegisterHit(GateRuntimeState state, int hitCount = 1)
        {
        }

        public int ApplyToSquad(ISquad squad, GateRuntimeState state, int defaultAppliedValue = 0)
        {
            return defaultAppliedValue;
        }

        public void ActivateModifier(StageModifierController controller, float currentDistance)
        {
            if (controller != null)
            {
                controller.Activate(stageModifier, currentDistance, durationDistance);
            }
        }

        public bool Validate(out string error)
        {
            if (durationDistance <= 0f)
            {
                error = "durationDistance must be greater than 0";
                return false;
            }

            error = null;
            return true;
        }
    }
}
