using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Data
{
    [Serializable]
    public class BossSummonWaveBehavior : IEnemyBehavior
    {
        public float[] Thresholds = new float[] { 0.75f, 0.50f, 0.25f };
        public string[] MinionArchetypes = new string[] { "walker", "runner", "brute" };

        private readonly HashSet<float> _triggeredThresholds = new();

        public event Action<float, string[]> OnWaveSummoned;

        public bool HasTriggered(float threshold) => _triggeredThresholds.Contains(threshold);
        public int TriggeredCount => _triggeredThresholds.Count;
        public IReadOnlyCollection<float> TriggeredThresholds => _triggeredThresholds;

        public void Reset()
        {
            _triggeredThresholds.Clear();
        }

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime)
        {
            if (context == null || context.MaxHealth <= 0)
            {
                return;
            }

            CheckThresholds(context);
        }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context)
        {
            if (context == null || context.MaxHealth <= 0)
            {
                return;
            }

            CheckThresholds(context);
        }

        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }

        private void CheckThresholds(EnemyBehaviorContext context)
        {
            if (Thresholds == null || Thresholds.Length == 0)
            {
                return;
            }

            float currentPct = (float)context.CurrentHealth / context.MaxHealth;

            for (int i = 0; i < Thresholds.Length; i++)
            {
                float threshold = Thresholds[i];
                if (currentPct <= threshold + 1e-5f && !_triggeredThresholds.Contains(threshold))
                {
                    _triggeredThresholds.Add(threshold);
                    context.BossSummonTriggered = true;
                    if (MinionArchetypes != null && MinionArchetypes.Length > 0)
                    {
                        context.BossSummonArchetypes.AddRange(MinionArchetypes);
                    }
                    OnWaveSummoned?.Invoke(threshold, MinionArchetypes);
                }
            }
        }
    }
}
