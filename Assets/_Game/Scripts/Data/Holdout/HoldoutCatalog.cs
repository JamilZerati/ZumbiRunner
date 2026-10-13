using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "HoldoutCatalog", menuName = "Horde Runner/Data/Holdout Catalog")]
    public class HoldoutCatalog : ScriptableObject
    {
        [SerializeField] private List<HoldoutDefinition> holdouts = new();

        public IReadOnlyList<HoldoutDefinition> Holdouts => holdouts;

        public bool TryGet(string id, out HoldoutDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(id) || holdouts == null)
            {
                return false;
            }

            for (int i = 0; i < holdouts.Count; i++)
            {
                var h = holdouts[i];
                if (h != null && string.Equals(h.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    definition = h;
                    return true;
                }
            }

            return false;
        }

        public void SetHoldouts(IEnumerable<HoldoutDefinition> newHoldouts)
        {
            if (holdouts == null)
            {
                holdouts = new List<HoldoutDefinition>();
            }
            else
            {
                holdouts.Clear();
            }

            if (newHoldouts == null)
            {
                return;
            }

            foreach (var h in newHoldouts)
            {
                if (h != null)
                {
                    holdouts.Add(h);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (holdouts == null) return;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < holdouts.Count; i++)
            {
                var h = holdouts[i];
                if (h != null && !string.IsNullOrEmpty(h.Id) && !seen.Add(h.Id))
                {
                    Debug.LogError($"[HoldoutCatalog] Duplicate holdout ID '{h.Id}' in catalog '{name}'.", this);
                }
            }
        }
#endif
    }
}
