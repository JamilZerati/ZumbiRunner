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
            definition = null;
            if (string.IsNullOrEmpty(id) || cages == null)
            {
                return false;
            }

            for (int i = 0; i < cages.Count; i++)
            {
                var cage = cages[i];
                if (cage != null && string.Equals(cage.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    definition = cage;
                    return true;
                }
            }

            return false;
        }

        public void SetCages(IEnumerable<CageDefinition> newCages)
        {
            if (cages == null)
            {
                cages = new List<CageDefinition>();
            }
            else
            {
                cages.Clear();
            }

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cages == null) return;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < cages.Count; i++)
            {
                var cage = cages[i];
                if (cage != null && !string.IsNullOrEmpty(cage.Id) && !seen.Add(cage.Id))
                {
                    Debug.LogError($"[CageCatalog] Duplicate cage ID '{cage.Id}' in catalog '{name}'.", this);
                }
            }
        }
#endif
    }
}
