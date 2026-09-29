using System;
using System.Collections.Generic;
using Game.Core.Status;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public struct EffectInteractionEntry
    {
        public string id;
        public StatusKind requires;
        public InteractionTrigger trigger;
        public int minHitDamage;
        public float damageMultiplier;

        public EffectInteraction ToInteraction() =>
            new EffectInteraction(id, requires, trigger, minHitDamage, damageMultiplier);
    }

    public class EffectInteractionTable : ScriptableObject, IEffectInteractionTable
    {
        [SerializeField]
        private List<EffectInteractionEntry> entries = new List<EffectInteractionEntry>();

        [NonSerialized]
        private List<EffectInteraction> cachedInteractions;

        public IReadOnlyList<EffectInteraction> Interactions
        {
            get
            {
                if (cachedInteractions == null)
                {
                    cachedInteractions = new List<EffectInteraction>(entries.Count);
                    for (int i = 0; i < entries.Count; i++)
                    {
                        cachedInteractions.Add(entries[i].ToInteraction());
                    }
                }
                return cachedInteractions;
            }
        }

        public void SetInteractions(IEnumerable<EffectInteraction> interactions)
        {
            entries.Clear();
            cachedInteractions = null;
            if (interactions != null)
            {
                foreach (var interaction in interactions)
                {
                    entries.Add(new EffectInteractionEntry
                    {
                        id = interaction.Id,
                        requires = interaction.RequiredStatus,
                        trigger = interaction.Trigger,
                        minHitDamage = interaction.MinHitDamage,
                        damageMultiplier = interaction.DamageMultiplier
                    });
                }
            }
        }
    }
}
