using Game.Core.Gates;

namespace Game.Core.Weather
{
    public sealed class WeatherStageModifier : IStageModifier
    {
        public string SourceId { get; }
        public float EnemySpeedMultiplier { get; }
        public float EnemyContactDamageMultiplier { get; }
        public float SquadRangeMultiplier { get; }

        public WeatherStageModifier(WeatherType type)
        {
            SourceId = $"clima:{type.ToString().ToLowerInvariant()}";
            switch (type)
            {
                case WeatherType.Blizzard:
                    EnemySpeedMultiplier = 0.8f;
                    EnemyContactDamageMultiplier = 1f;
                    SquadRangeMultiplier = 1f;
                    break;
                case WeatherType.Heatwave:
                    EnemySpeedMultiplier = 1f;
                    EnemyContactDamageMultiplier = 1.2f;
                    SquadRangeMultiplier = 1f;
                    break;
                case WeatherType.Fog:
                    EnemySpeedMultiplier = 1f;
                    EnemyContactDamageMultiplier = 1f;
                    SquadRangeMultiplier = 0.75f;
                    break;
                default:
                    EnemySpeedMultiplier = 1f;
                    EnemyContactDamageMultiplier = 1f;
                    SquadRangeMultiplier = 1f;
                    break;
            }
        }

        public WeatherStageModifier(WeatherType type, float enemySpeed, float enemyContact = 1f, float squadRange = 1f)
        {
            SourceId = $"clima:{type.ToString().ToLowerInvariant()}";
            EnemySpeedMultiplier = enemySpeed;
            EnemyContactDamageMultiplier = enemyContact;
            SquadRangeMultiplier = squadRange;
        }
    }
}
