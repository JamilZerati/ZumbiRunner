using System;
using Game.Core.Gates;

namespace Game.Core.Modifiers
{
    [Serializable]
    public class InitialSquadOverrideStageModifier : IStageModifier
    {
        public string sourceId;
        public int squadCount;

        public InitialSquadOverrideStageModifier() { }

        public InitialSquadOverrideStageModifier(string sourceId, int squadCount)
        {
            this.sourceId = sourceId;
            this.squadCount = squadCount;
        }

        public string SourceId => sourceId;
        public float EnemySpeedMultiplier => 1f;
        public float EnemyContactDamageMultiplier => 1f;
        public float SquadRangeMultiplier => 1f;
        public float EnemyHealthMultiplier => 1f;
        public int? InitialSquadOverride => squadCount;
        public bool RemapGateMultiplication => false;
        public bool RemapGateDivision => false;
    }
}
