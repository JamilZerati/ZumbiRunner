using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Mutations
{
    [CreateAssetMenu(fileName = "MutationCatalog", menuName = "Horde Runner/Data/Mutation Catalog")]
    public class MutationCatalog : ScriptableObject, IMutationCatalog
    {
        [SerializeField] private List<MutationDefinition> entries = new();

        public IReadOnlyList<MutationDefinition> Entries => entries;

        public bool TryGet(string id, out MutationDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(id) || entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    definition = entry;
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyDictionary<string, float> GetRewardBonuses()
        {
            var dict = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            if (entries == null) return dict;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && !string.IsNullOrEmpty(entry.Id))
                {
                    dict[entry.Id] = entry.RewardBonus;
                }
            }

            return dict;
        }

        public void SetEntries(IEnumerable<MutationDefinition> newEntries)
        {
            if (entries == null)
            {
                entries = new List<MutationDefinition>();
            }
            else
            {
                entries.Clear();
            }

            if (newEntries == null) return;

            foreach (var item in newEntries)
            {
                if (item != null)
                {
                    entries.Add(item);
                }
            }
        }
    }
}
