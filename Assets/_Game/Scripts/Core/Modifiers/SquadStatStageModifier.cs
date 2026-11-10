using System;
using Game.Core.Gates;

namespace Game.Core.Modifiers
{
    [Serializable]
    public class SquadStatStageModifier : IStageModifier
    {
        public string sourceId;
        public StatTarget statTarget;
        public float multiplier = 1f;

        public SquadStatStageModifier() { }

        public SquadStatStageModifier(string sourceId, StatTarget statTarget, float multiplier)
        {
            this.sourceId = sourceId;
            this.statTarget = statTarget;
            this.multiplier = multiplier;
        }

        public string SourceId => sourceId;
        public float EnemySpeedMultiplier => 1f;
        public float EnemyContactDamageMultiplier => 1f;
        public float SquadRangeMultiplier => statTarget == StatTarget.Range ? multiplier : 1f;
        public float EnemyHealthMultiplier => 1f;
        public int? InitialSquadOverride => null;
        public bool RemapGateMultiplication => false;
        public bool RemapGateDivision => false;
    }
}
