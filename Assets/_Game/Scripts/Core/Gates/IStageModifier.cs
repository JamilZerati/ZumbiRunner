namespace Game.Core.Gates
{
    public interface IStageModifier
    {
        string SourceId { get; }
        float EnemySpeedMultiplier { get; }
        float EnemyContactDamageMultiplier { get; }
    }
}
