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
            int safeHits = Math.Max(0, totalHits);
            int safeHitsPerStep = hitsPerStep > 0 ? hitsPerStep : 1;
            int completedSteps = safeHits / safeHitsPerStep;
            int calculated = initialValue + completedSteps * step;
            return step >= 0 ? Math.Min(calculated, cap) : Math.Max(calculated, cap);
        }

        public GateRuntimeState CreateInitialState(string defaultDisplayText = null)
        {
            int value = EvaluateValue(0);
            string text = string.IsNullOrEmpty(defaultDisplayText)
                ? GateRuntimeState.FormatSignedValue(value)
                : defaultDisplayText;
            return new GateRuntimeState(VariantType, AcceptsProjectiles, value, text);
        }

        public void RegisterHit(GateRuntimeState state, int hitCount = 1)
        {
            if (state == null)
            {
                return;
            }

            int nextHits = state.CurrentHits + Math.Max(0, hitCount);
            int nextValue = EvaluateValue(nextHits);
            state.UpdateProgress(nextHits, nextValue, GateRuntimeState.FormatSignedValue(nextValue));
        }

        public int ApplyToSquad(ISquad squad, GateRuntimeState state, int defaultAppliedValue = 0)
        {
            if (squad == null)
            {
                return 0;
            }

            int value = state != null ? state.CurrentValue : defaultAppliedValue;
            if (value > 0)
            {
                squad.Add(value);
            }
            else if (value < 0)
            {
                int penalty = -value;
                int maxRemovable = Math.Max(0, squad.SquadCount - 1);
                int toRemove = Math.Min(penalty, maxRemovable);
                if (toRemove > 0)
                {
                    squad.Remove(toRemove);
                }
            }

            return value;
        }

        public bool Validate(out string error)
        {
            if (hitsPerStep <= 0)
            {
                error = "hitsPerStep must be greater than 0";
                return false;
            }
            if (step == 0)
            {
                error = "step cannot be 0";
                return false;
            }

            error = null;
            return true;
        }
    }
}
