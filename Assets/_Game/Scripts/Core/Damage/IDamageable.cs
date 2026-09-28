namespace Game.Core
{
    public interface IDamageable
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damage);
    }
}
