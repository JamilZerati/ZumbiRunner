using System;

namespace Game.Core.Gates
{
    public sealed class StageModifierController
    {
        public int ActiveCount => 0;
        public float CurrentDistance { get; private set; }
        public float CurrentEnemySpeedMultiplier => 1f;
        public float CurrentEnemyContactDamageMultiplier => 1f;

        public void Activate(IStageModifier modifier, float startDistance, float durationDistance)
        {
            throw new NotImplementedException();
        }

        public void TickDistance(float currentDistance)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }
    }
}
