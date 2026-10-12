using System;

namespace Game.Core.Gates
{
    public sealed class GateRuntimeState
    {
        public GateVariantType VariantType { get; }
        public bool AcceptsProjectiles { get; }
        public int CurrentValue { get; private set; }
        public int CurrentHits { get; private set; }
        public string DisplayText { get; private set; }

        public bool IsNegative => VariantType == GateVariantType.ShootToUpgrade && CurrentValue < 0;
        public bool IsCursed => VariantType == GateVariantType.Cursed;

        public GateRuntimeState(GateVariantType variantType, bool acceptsProjectiles, int initialValue, string displayText)
        {
            VariantType = variantType;
            AcceptsProjectiles = acceptsProjectiles;
            CurrentValue = initialValue;
            CurrentHits = 0;
            DisplayText = displayText ?? string.Empty;
        }

        public void UpdateProgress(int currentHits, int currentValue, string displayText)
        {
            CurrentHits = Math.Max(0, currentHits);
            CurrentValue = currentValue;
            DisplayText = displayText ?? string.Empty;
        }

        public static string FormatSignedValue(int value)
        {
            return value >= 0 ? $"+{value}" : value.ToString();
        }
    }
}
