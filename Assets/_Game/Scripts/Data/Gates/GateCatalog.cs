using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "GateCatalog", menuName = "Horde Runner/Data/Gate Catalog")]
    public class GateCatalog : ScriptableObject
    {
        [SerializeField] private List<GateDefinition> gates = new();

        public IReadOnlyList<GateDefinition> Gates => gates;

        public bool TryGet(string id, out GateDefinition definition)
        {
            if (!string.IsNullOrEmpty(id) && gates != null)
            {
                for (int i = 0; i < gates.Count; i++)
                {
                    var gate = gates[i];
                    if (gate != null && string.Equals(gate.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        definition = gate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public void SetGates(IEnumerable<GateDefinition> newGates)
        {
            gates = new List<GateDefinition>();
            if (newGates == null)
            {
                return;
            }

            foreach (var gate in newGates)
            {
                if (gate != null)
                {
                    gates.Add(gate);
                }
            }
        }
    }
}
