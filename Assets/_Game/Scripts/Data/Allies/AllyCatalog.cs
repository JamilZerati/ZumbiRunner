using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Allies
{
    [CreateAssetMenu(fileName = "AllyCatalog", menuName = "Horde Runner/Data/Ally Catalog")]
    public class AllyCatalog : ScriptableObject
    {
        [SerializeField] private List<AllyDefinition> allies = new();

        public IReadOnlyList<AllyDefinition> Allies => allies;

        public bool TryGet(string id, out AllyDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(id) || allies == null)
            {
                return false;
            }

            for (int i = 0; i < allies.Count; i++)
            {
                var ally = allies[i];
                if (ally != null && string.Equals(ally.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    definition = ally;
                    return true;
                }
            }

            return false;
        }

        public void SetAllies(IEnumerable<AllyDefinition> newAllies)
        {
            if (allies == null)
            {
                allies = new List<AllyDefinition>();
            }
            else
            {
                allies.Clear();
            }

            if (newAllies == null)
            {
                return;
            }

            foreach (var ally in newAllies)
            {
                if (ally != null)
                {
                    allies.Add(ally);
                }
            }
        }
    }
}
