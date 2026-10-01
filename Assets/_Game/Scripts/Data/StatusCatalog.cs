using System;
using System.Collections.Generic;
using Game.Core.Status;
using UnityEngine;

namespace Game.Data
{
    public class StatusCatalog : ScriptableObject, IStatusCatalog
    {
        [SerializeReference]
        private List<IStatusEffect> effects = new List<IStatusEffect>();

        public IReadOnlyList<IStatusEffect> Effects => effects;

        public bool TryGet(StatusKind kind, out IStatusEffect effect)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null && effects[i].Kind == kind)
                {
                    effect = effects[i];
                    return true;
                }
            }

            effect = null;
            return false;
        }

        public void SetEffects(IEnumerable<IStatusEffect> newEffects)
        {
            effects.Clear();
            if (newEffects != null)
            {
                effects.AddRange(newEffects);
            }
        }
    }
}
