using System;
using System.Collections.Generic;
using Game.Core.Weather;
using UnityEngine;

namespace Game.Data.Weather
{
    [CreateAssetMenu(fileName = "WeatherCatalog", menuName = "Horde Runner/Data/Weather Catalog")]
    public class WeatherCatalog : ScriptableObject
    {
        [SerializeField] private List<WeatherDefinition> entries = new();

        public IReadOnlyList<WeatherDefinition> Entries => entries;

        public bool TryGet(string id, out WeatherDefinition definition)
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

        public bool TryGetByType(WeatherType type, out WeatherDefinition definition)
        {
            definition = null;
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.Type == type)
                {
                    definition = entry;
                    return true;
                }
            }

            return false;
        }

        public void SetEntries(IEnumerable<WeatherDefinition> newEntries)
        {
            if (entries == null)
            {
                entries = new List<WeatherDefinition>();
            }
            else
            {
                entries.Clear();
            }

            if (newEntries == null)
            {
                return;
            }

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
