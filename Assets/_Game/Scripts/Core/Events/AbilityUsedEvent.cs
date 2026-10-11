using Game.Core.Abilities;

namespace Game.Core.Events
{
    public readonly struct AbilityUsedEvent
    {
        public string AbilityId { get; }
        public int RemainingCharges { get; }
        public AbilityPosition OriginPosition { get; }
        public AbilityPosition TargetPosition { get; }
        public bool HasSpatialTarget { get; }

        public AbilityUsedEvent(
            string abilityId,
            int remainingCharges,
            AbilityPosition originPosition = default,
            AbilityPosition targetPosition = default,
            bool hasSpatialTarget = false)
        {
            AbilityId = abilityId;
            RemainingCharges = remainingCharges;
            OriginPosition = originPosition;
            TargetPosition = targetPosition;
            HasSpatialTarget = hasSpatialTarget;
        }
    }
}
