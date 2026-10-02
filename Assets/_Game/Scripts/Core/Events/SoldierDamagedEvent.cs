namespace Game.Core.Events
{
    public readonly struct SoldierDamagedEvent
    {
        public readonly float CurrentHealth;
        public readonly float MaxHealth;

        public SoldierDamagedEvent(float currentHealth, float maxHealth)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }
}
