using System;

namespace Game.Core.Abilities
{
    [Serializable]
    public class AbilityTargetingConfig
    {
        public AbilityTargetingType Type = AbilityTargetingType.Instant;
        public float MaxRange = 0f;
        public float Radius = 4f;

        public AbilityPosition ClampTarget(AbilityPosition origin, AbilityPosition desired)
        {
            if (Type == AbilityTargetingType.Instant)
            {
                return origin;
            }

            float deltaZ = desired.Z - origin.Z;
            if (deltaZ < 0f) deltaZ = 0f;
            if (deltaZ > MaxRange) deltaZ = MaxRange;

            return new AbilityPosition(desired.X, origin.Y, origin.Z + deltaZ);
        }
    }
}
