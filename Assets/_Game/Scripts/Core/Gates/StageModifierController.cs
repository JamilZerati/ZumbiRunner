using System;
using System.Collections.Generic;

namespace Game.Core.Gates
{
    public sealed class StageModifierController
    {
        private struct ActiveModifierEntry
        {
            public IStageModifier Modifier;
            public float ExpiryDistance;
        }

        private readonly List<ActiveModifierEntry> activeModifiers = new();

        public int ActiveCount => activeModifiers.Count;
        public float CurrentDistance { get; private set; }
        public float CurrentEnemySpeedMultiplier { get; private set; } = 1f;
        public float CurrentEnemyContactDamageMultiplier { get; private set; } = 1f;
        public float CurrentSquadRangeMultiplier { get; private set; } = 1f;
        public float CurrentEnemyHealthMultiplier { get; private set; } = 1f;
        public int? CurrentInitialSquadOverride { get; private set; }
        public bool RemapGateMultiplication { get; private set; }
        public bool RemapGateDivision { get; private set; }

        public void Activate(IStageModifier modifier, float startDistance, float durationDistance)
        {
            if (modifier == null || durationDistance <= 0f)
            {
                return;
            }

            activeModifiers.Add(new ActiveModifierEntry
            {
                Modifier = modifier,
                ExpiryDistance = startDistance + durationDistance
            });

            RecalculateMultipliers();
        }

        public void TickDistance(float currentDistance)
        {
            CurrentDistance = currentDistance;

            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                if (currentDistance >= activeModifiers[i].ExpiryDistance)
                {
                    activeModifiers.RemoveAt(i);
                }
            }

            RecalculateMultipliers();
        }

        public bool RemoveBySourceId(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
            {
                return false;
            }

            bool removed = false;
            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                if (string.Equals(activeModifiers[i].Modifier.SourceId, sourceId, StringComparison.OrdinalIgnoreCase))
                {
                    activeModifiers.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                RecalculateMultipliers();
            }

            return removed;
        }

        public void Clear()
        {
            activeModifiers.Clear();
            RecalculateMultipliers();
        }

        private void RecalculateMultipliers()
        {
            float speed = 1f;
            float contact = 1f;
            float range = 1f;
            float health = 1f;
            int? initialSquadOverride = null;
            bool remapMult = false;
            bool remapDiv = false;

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var mod = activeModifiers[i].Modifier;
                speed *= mod.EnemySpeedMultiplier;
                contact *= mod.EnemyContactDamageMultiplier;
                range *= mod.SquadRangeMultiplier;
                health *= mod.EnemyHealthMultiplier;
                if (mod.InitialSquadOverride.HasValue && mod.InitialSquadOverride.Value > 0)
                {
                    initialSquadOverride = mod.InitialSquadOverride.Value;
                }
                if (mod.RemapGateMultiplication)
                {
                    remapMult = true;
                }
                if (mod.RemapGateDivision)
                {
                    remapDiv = true;
                }
            }

            CurrentEnemySpeedMultiplier = speed;
            CurrentEnemyContactDamageMultiplier = contact;
            CurrentSquadRangeMultiplier = range;
            CurrentEnemyHealthMultiplier = health;
            CurrentInitialSquadOverride = initialSquadOverride;
            RemapGateMultiplication = remapMult;
            RemapGateDivision = remapDiv;
        }
    }
}
