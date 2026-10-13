using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "CageDefinition", menuName = "Horde Runner/Data/Cage Definition")]
    public class CageDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private int maxHp = 60;
        [SerializeField] private int survivorCount = 3;
        [SerializeField] private string lostEnemyArchetypeId = "walker";

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxHp => maxHp;
        public int SurvivorCount => survivorCount;
        public string LostEnemyArchetypeId => lostEnemyArchetypeId;

        public void SetData(string cageId, string name, int hp, int survivors, string archetypeId = "walker")
        {
            id = cageId;
            displayName = name;
            maxHp = hp;
            survivorCount = survivors;
            lostEnemyArchetypeId = archetypeId;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name;
            if (maxHp < 1) maxHp = 1;
            if (survivorCount < 1) survivorCount = 1;
            if (string.IsNullOrWhiteSpace(lostEnemyArchetypeId)) lostEnemyArchetypeId = "walker";
        }
#endif
    }
}
