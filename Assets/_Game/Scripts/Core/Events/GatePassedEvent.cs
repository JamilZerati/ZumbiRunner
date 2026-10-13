using Game.Core.Gates;

namespace Game.Core.Events
{
    public readonly struct GatePassedEvent
    {
        public string GateId { get; }
        public string PerkId { get; }
        public GateVariantType Variant { get; }
        public int LaneIndex { get; }
        public int AppliedValue { get; }

        public GatePassedEvent(string gateId, string perkId, GateVariantType variant, int laneIndex, int appliedValue)
        {
            GateId = gateId ?? string.Empty;
            PerkId = perkId ?? string.Empty;
            Variant = variant;
            LaneIndex = laneIndex;
            AppliedValue = appliedValue;
        }
    }
}
