using System;
using Game.Core.Gates;

namespace Game.Core.Modifiers
{
    [Serializable]
    public class GateOperationRemapStageModifier : IStageModifier
    {
        public string sourceId;
        public bool remapMultiplyToAdd;
        public bool remapDivideToSubtract;

        public GateOperationRemapStageModifier() { }

        public GateOperationRemapStageModifier(string sourceId, bool remapMultiplyToAdd, bool remapDivideToSubtract)
        {
            this.sourceId = sourceId;
            this.remapMultiplyToAdd = remapMultiplyToAdd;
            this.remapDivideToSubtract = remapDivideToSubtract;
        }

        public string SourceId => sourceId;
        public float EnemySpeedMultiplier => 1f;
        public float EnemyContactDamageMultiplier => 1f;
        public float SquadRangeMultiplier => 1f;
        public float EnemyHealthMultiplier => 1f;
        public int? InitialSquadOverride => null;
        public bool RemapGateMultiplication => remapMultiplyToAdd;
        public bool RemapGateDivision => remapDivideToSubtract;
    }
}
