using System;
using System.Collections.Generic;

namespace Game.Core.Stats
{
    public sealed class StatCollection
    {
        private readonly Dictionary<StatId, float> bases = new Dictionary<StatId, float>();
        private readonly Dictionary<StatId, List<StatModifier>> modifiers = new Dictionary<StatId, List<StatModifier>>();

        public event Action<StatId> Changed;

        public float GetBase(StatId stat)
        {
            return bases.TryGetValue(stat, out float value) ? value : 0f;
        }

        public void SetBase(StatId stat, float value)
        {
            bases[stat] = value;
            Changed?.Invoke(stat);
        }

        public float GetValue(StatId stat)
        {
            float flat = 0f;
            float percentAdd = 0f;
            float percentMultiply = 1f;

            if (modifiers.TryGetValue(stat, out List<StatModifier> list))
            {
                foreach (StatModifier modifier in list)
                {
                    switch (modifier.Kind)
                    {
                        case ModifierKind.Flat:
                            flat += modifier.Value;
                            break;
                        case ModifierKind.PercentAdd:
                            percentAdd += modifier.Value;
                            break;
                        case ModifierKind.PercentMultiply:
                            percentMultiply *= 1f + modifier.Value;
                            break;
                    }
                }
            }

            return (GetBase(stat) + flat) * (1f + percentAdd) * percentMultiply;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier.Source == null)
            {
                throw new ArgumentException("Modificador sem fonte (default(StatModifier)).", nameof(modifier));
            }

            if (!modifiers.TryGetValue(modifier.Stat, out List<StatModifier> list))
            {
                list = new List<StatModifier>();
                modifiers[modifier.Stat] = list;
            }

            list.Add(modifier);
            Changed?.Invoke(modifier.Stat);
        }

        public bool RemoveModifier(StatModifier modifier)
        {
            if (!modifiers.TryGetValue(modifier.Stat, out List<StatModifier> list) || !list.Remove(modifier))
            {
                return false;
            }

            Changed?.Invoke(modifier.Stat);
            return true;
        }

        public int RemoveAllFromSource(object source)
        {
            int removed = 0;
            var affected = new List<StatId>();

            foreach (KeyValuePair<StatId, List<StatModifier>> entry in modifiers)
            {
                int count = entry.Value.RemoveAll(modifier => Equals(modifier.Source, source));
                if (count > 0)
                {
                    removed += count;
                    affected.Add(entry.Key);
                }
            }

            // Notifica só depois de todas as remoções para o ouvinte ler a coleção já consistente.
            foreach (StatId stat in affected)
            {
                Changed?.Invoke(stat);
            }

            return removed;
        }

        public IReadOnlyList<StatModifier> GetModifiers(StatId stat)
        {
            return modifiers.TryGetValue(stat, out List<StatModifier> list)
                ? list.ToArray()
                : Array.Empty<StatModifier>();
        }
    }
}
