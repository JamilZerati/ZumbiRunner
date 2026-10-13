using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "HoldoutDefinition", menuName = "Horde Runner/Data/Holdout Definition")]
    public class HoldoutDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private int maxHp = 200;
        [SerializeField] private int totalSurvivors = 5;
        [SerializeField] private float durationSeconds = 15f;
        [SerializeField] private float zombieContactDps = 10f;
        [SerializeField] private float waveIntervalSeconds = 3f;
        [SerializeField] private int zombiesPerWavePerLane = 2;

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxHp => maxHp;
        public int TotalSurvivors => totalSurvivors;
        public float DurationSeconds => durationSeconds;
        public float ZombieContactDps => zombieContactDps;
        public float WaveIntervalSeconds => waveIntervalSeconds;
        public int ZombiesPerWavePerLane => zombiesPerWavePerLane;

        public void SetData(
            string holdoutId,
            string name,
            int hp,
            int survivors,
            float duration,
            float contactDps,
            float waveInterval,
            int zombiesPerLane)
        {
            id = holdoutId;
            displayName = name;
            maxHp = hp;
            totalSurvivors = survivors;
            durationSeconds = duration;
            zombieContactDps = contactDps;
            waveIntervalSeconds = waveInterval;
            zombiesPerWavePerLane = zombiesPerLane;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name;
            if (maxHp < 1) maxHp = 1;
            if (totalSurvivors < 0) totalSurvivors = 0;
            if (durationSeconds < 1f) durationSeconds = 1f;
            if (zombieContactDps < 0f) zombieContactDps = 0f;
            if (waveIntervalSeconds < 0.1f) waveIntervalSeconds = 0.1f;
            if (zombiesPerWavePerLane < 1) zombiesPerWavePerLane = 1;
        }
#endif
    }
}
