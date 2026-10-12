using System;

namespace Game.Core.Gates
{
    [Serializable]
    public sealed class GuardedGateVariant : IGateVariant
    {
        public string enemyWaveId = string.Empty;
        public string guardArchetypeId = string.Empty;
        public int guardCount = 5;
        public float guardDistanceOffset = 8f;

        public string EnemyWaveId => enemyWaveId ?? string.Empty;
        public string GuardArchetypeId => guardArchetypeId ?? string.Empty;
        public int GuardCount => guardCount;
        public float GuardDistanceOffset => guardDistanceOffset;

        public GateVariantType VariantType => GateVariantType.Guarded;
        public bool AcceptsProjectiles => false;

        public GuardedGateVariant()
        {
        }

        public GuardedGateVariant(string enemyWaveId, string guardArchetypeId, int guardCount, float guardDistanceOffset)
        {
            this.enemyWaveId = enemyWaveId ?? string.Empty;
            this.guardArchetypeId = guardArchetypeId ?? string.Empty;
            this.guardCount = guardCount;
            this.guardDistanceOffset = guardDistanceOffset;
        }

        public GateRuntimeState CreateInitialState(string defaultDisplayText)
        {
            throw new NotImplementedException();
        }

        public void RegisterHit(GateRuntimeState state, int hitCount = 1)
        {
        }

        public int ApplyToSquad(ISquad squad, GateRuntimeState state, int defaultAppliedValue = 0)
        {
            throw new NotImplementedException();
        }

        public bool Validate(out string error)
        {
            throw new NotImplementedException();
        }
    }
}
