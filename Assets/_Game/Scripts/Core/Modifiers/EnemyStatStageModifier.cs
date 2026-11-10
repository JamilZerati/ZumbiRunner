using System;
using Game.Core.Gates;

namespace Game.Core.Modifiers
{
    [Serializable]
    public class EnemyStatStageModifier : IStageModifier
    {
        public string sourceId;
        public StatTarget statTarget;
        public float multiplier = 1f;

        public EnemyStatStageModifier() { }

        public EnemyStatStageModifier(string sourceId, StatTarget statTarget, float multiplier)
        {
            this.sourceId = sourceId;
            this.statTarget = statTarget;
            this.multiplier = multiplier;
        }

        public string SourceId => sourceId;
        public float EnemySpeedMultiplier => statTarget == StatTarget.MoveSpeed ? multiplier : 1f;
        public float EnemyContactDamageMultiplier => statTarget == StatTarget.ContactDamage ? multiplier : 1f;
        public float SquadRangeMultiplier => 1f;
        public float EnemyHealthMultiplier => statTarget == StatTarget.MaxHealth ? multiplier : 1f;
        public int? InitialSquadOverride => null;
        public bool RemapGateMultiplication => false;
        public bool RemapGateDivision => false;
    }
}
