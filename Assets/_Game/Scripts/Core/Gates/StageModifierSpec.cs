using System;

namespace Game.Core.Gates
{
    [Serializable]
    public struct StageModifierSpec : IStageModifier
    {
        public string sourceId;
        public float enemySpeedMultiplier;
        public float enemyContactDamageMultiplier;
        public float squadRangeMultiplier;
        public float enemyHealthMultiplier;
        public int? initialSquadOverride;
        public bool remapGateMultiplication;
        public bool remapGateDivision;

        public StageModifierSpec(
            string sourceId,
            float enemySpeedMultiplier,
            float enemyContactDamageMultiplier = 1f,
            float squadRangeMultiplier = 1f,
            float enemyHealthMultiplier = 1f,
            int? initialSquadOverride = null,
            bool remapGateMultiplication = false,
            bool remapGateDivision = false)
        {
            this.sourceId = sourceId ?? string.Empty;
            this.enemySpeedMultiplier = enemySpeedMultiplier;
            this.enemyContactDamageMultiplier = enemyContactDamageMultiplier;
            this.squadRangeMultiplier = squadRangeMultiplier;
            this.enemyHealthMultiplier = enemyHealthMultiplier;
            this.initialSquadOverride = initialSquadOverride;
            this.remapGateMultiplication = remapGateMultiplication;
            this.remapGateDivision = remapGateDivision;
        }

        public string SourceId => sourceId ?? string.Empty;
        public float EnemySpeedMultiplier => enemySpeedMultiplier > 0f ? enemySpeedMultiplier : 1f;
        public float EnemyContactDamageMultiplier => enemyContactDamageMultiplier > 0f ? enemyContactDamageMultiplier : 1f;
        public float SquadRangeMultiplier => squadRangeMultiplier > 0f ? squadRangeMultiplier : 1f;
        public float EnemyHealthMultiplier => enemyHealthMultiplier > 0f ? enemyHealthMultiplier : 1f;
        public int? InitialSquadOverride => initialSquadOverride;
        public bool RemapGateMultiplication => remapGateMultiplication;
        public bool RemapGateDivision => remapGateDivision;
    }
}
