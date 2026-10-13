using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "CageCatalog", menuName = "Horde Runner/Data/Cage Catalog")]
    public class CageCatalog : ScriptableObject
    {
        [SerializeField] private List<CageDefinition> cages = new();

        public IReadOnlyList<CageDefinition> Cages => cages;

        public bool TryGet(string id, out CageDefinition definition)
        {
            if (!string.IsNullOrEmpty(id) && cages != null)
            {
                for (int i = 0; i < cages.Count; i++)
                {
                    var cage = cages[i];
                    if (cage != null && string.Equals(cage.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        definition = cage;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public void SetCages(IEnumerable<CageDefinition> newCages)
        {
            cages = new List<CageDefinition>();
            if (newCages == null)
            {
                return;
            }

            foreach (var cage in newCages)
            {
                if (cage != null)
                {
                    cages.Add(cage);
                }
            }
        }
    }
}
