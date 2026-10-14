using Game.Core.Gates;

namespace Game.Core.Weather
{
    public sealed class WeatherStageModifier : IStageModifier
    {
        public string SourceId { get; }
        public float EnemySpeedMultiplier { get; }
        public float EnemyContactDamageMultiplier { get; }
        public float SquadRangeMultiplier { get; }

        public WeatherStageModifier(WeatherType type, float enemySpeed = 1f, float enemyContact = 1f, float squadRange = 1f)
        {
            SourceId = $"clima:{type.ToString().ToLowerInvariant()}";
            EnemySpeedMultiplier = enemySpeed;
            EnemyContactDamageMultiplier = enemyContact;
            SquadRangeMultiplier = squadRange;
        }
    }
}
