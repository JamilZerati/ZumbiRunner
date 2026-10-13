using System;

namespace Game.Core.Gates
{
    [Serializable]
    public struct StageModifierSpec : IStageModifier
    {
        public string sourceId;
        public float enemySpeedMultiplier;
        public float enemyContactDamageMultiplier;

        public StageModifierSpec(string sourceId, float enemySpeedMultiplier, float enemyContactDamageMultiplier = 1f)
        {
            this.sourceId = sourceId ?? string.Empty;
            this.enemySpeedMultiplier = enemySpeedMultiplier;
            this.enemyContactDamageMultiplier = enemyContactDamageMultiplier;
        }

        public string SourceId => sourceId ?? string.Empty;
        public float EnemySpeedMultiplier => enemySpeedMultiplier > 0f ? enemySpeedMultiplier : 1f;
        public float EnemyContactDamageMultiplier => enemyContactDamageMultiplier > 0f ? enemyContactDamageMultiplier : 1f;
    }
}
