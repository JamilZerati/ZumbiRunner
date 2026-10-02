using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "EnemyCatalog", menuName = "Horde Runner/Data/Enemy Catalog")]
    public class EnemyCatalog : ScriptableObject
    {
        [SerializeField] private List<EnemyDefinition> enemies = new();

        public IReadOnlyList<EnemyDefinition> Enemies => enemies;

        public bool TryGet(string id, out EnemyDefinition definition)
        {
            if (!string.IsNullOrEmpty(id) && enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    if (enemy != null && string.Equals(enemy.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        definition = enemy;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public void SetEnemies(IEnumerable<EnemyDefinition> newEnemies)
        {
            enemies = new List<EnemyDefinition>();
            if (newEnemies == null)
            {
                return;
            }

            foreach (var enemy in newEnemies)
            {
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
            }
        }
    }
}
