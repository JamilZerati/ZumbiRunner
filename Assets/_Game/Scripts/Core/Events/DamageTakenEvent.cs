namespace Game.Core.Events
{
    public readonly struct DamageTakenEvent
    {
        public IDamageable Target { get; }
        public DamageInfo Damage { get; }
        public int RemainingHealth { get; }

        public DamageTakenEvent(IDamageable target, DamageInfo damage, int remainingHealth)
        {
            Target = target;
            Damage = damage;
            RemainingHealth = remainingHealth;
        }
    }
}
