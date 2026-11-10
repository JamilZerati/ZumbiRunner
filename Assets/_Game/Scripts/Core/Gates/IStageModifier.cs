namespace Game.Core.Gates
{
    public interface IStageModifier
    {
        string SourceId { get; }
        float EnemySpeedMultiplier { get; }
        float EnemyContactDamageMultiplier { get; }
        float SquadRangeMultiplier { get; }
        float EnemyHealthMultiplier { get; }
        int? InitialSquadOverride { get; }
        bool RemapGateMultiplication { get; }
        bool RemapGateDivision { get; }
    }
}
