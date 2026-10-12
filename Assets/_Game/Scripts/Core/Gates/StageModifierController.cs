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

        public void Clear()
        {
            activeModifiers.Clear();
            RecalculateMultipliers();
        }

        private void RecalculateMultipliers()
        {
            float speed = 1f;
            float contact = 1f;

            for (int i = 0; i < activeModifiers.Count; i++)
            {
                speed *= activeModifiers[i].Modifier.EnemySpeedMultiplier;
                contact *= activeModifiers[i].Modifier.EnemyContactDamageMultiplier;
            }

            CurrentEnemySpeedMultiplier = speed;
            CurrentEnemyContactDamageMultiplier = contact;
        }
    }
}
