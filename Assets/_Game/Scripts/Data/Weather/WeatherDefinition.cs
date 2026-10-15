using Game.Core.Weather;
using UnityEngine;

namespace Game.Data.Weather
{
    [CreateAssetMenu(fileName = "WeatherDefinition", menuName = "Horde Runner/Data/Weather Definition")]
    public class WeatherDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private WeatherType type;
        [SerializeField] private string displayName;
        [SerializeField] private float enemySpeedMultiplier = 1f;
        [SerializeField] private float enemyContactDamageMultiplier = 1f;
        [SerializeField] private float squadRangeMultiplier = 1f;
        [SerializeField] private string appliedStatus;

        public string Id { get => id; set => id = value; }
        public WeatherType Type { get => type; set => type = value; }
        public string DisplayName { get => displayName; set => displayName = value; }
        public float EnemySpeedMultiplier { get => enemySpeedMultiplier; set => enemySpeedMultiplier = value; }
        public float EnemyContactDamageMultiplier { get => enemyContactDamageMultiplier; set => enemyContactDamageMultiplier = value; }
        public float SquadRangeMultiplier { get => squadRangeMultiplier; set => squadRangeMultiplier = value; }
        public string AppliedStatus { get => appliedStatus; set => appliedStatus = value; }

        public void SetData(string id, WeatherType type, string displayName, float enemySpeedMultiplier,
                            float enemyContactDamageMultiplier, float squadRangeMultiplier, string appliedStatus)
        {
            this.id = id;
            this.type = type;
            this.displayName = displayName;
            this.enemySpeedMultiplier = enemySpeedMultiplier;
            this.enemyContactDamageMultiplier = enemyContactDamageMultiplier;
            this.squadRangeMultiplier = squadRangeMultiplier;
            this.appliedStatus = appliedStatus;
        }
    }
}
