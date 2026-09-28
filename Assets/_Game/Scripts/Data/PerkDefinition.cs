using System.Collections.Generic;
using Game.Core.Perks;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "PerkDefinition", menuName = "Horde Runner/Data/Perk Definition")]
    public class PerkDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeReference] private List<IPerkEffect> effects = new();

        public string Id => id;
        public string DisplayName => displayName;
        public IReadOnlyList<IPerkEffect> Effects => effects;

        public void SetData(string perkId, string name, List<IPerkEffect> perkEffects)
        {
            id = perkId;
            displayName = name;
            effects = perkEffects != null ? new List<IPerkEffect>(perkEffects) : new List<IPerkEffect>();
        }

        public void Apply(PerkContext context)
        {
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                effects[i]?.Apply(context);
            }
        }
    }
}
