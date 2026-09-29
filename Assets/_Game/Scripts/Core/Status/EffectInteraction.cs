using System;

namespace Game.Core.Status
{
    public enum InteractionTrigger
    {
        HeavyHit
    }

    public readonly struct EffectInteraction
    {
        public string Id { get; }
        public StatusKind RequiredStatus { get; }
        public InteractionTrigger Trigger { get; }
        public int MinHitDamage { get; }
        public float DamageMultiplier { get; }

        public EffectInteraction(string id, StatusKind requiredStatus, InteractionTrigger trigger,
                                 int minHitDamage, float damageMultiplier)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Id cannot be null or empty", nameof(id));
            if (minHitDamage < 1) throw new ArgumentOutOfRangeException(nameof(minHitDamage));
            if (damageMultiplier <= 1f || float.IsNaN(damageMultiplier) || float.IsInfinity(damageMultiplier))
                throw new ArgumentException("DamageMultiplier must be finite and > 1", nameof(damageMultiplier));

            Id = id;
            RequiredStatus = requiredStatus;
            Trigger = trigger;
            MinHitDamage = minHitDamage;
            DamageMultiplier = damageMultiplier;
        }
    }
}
