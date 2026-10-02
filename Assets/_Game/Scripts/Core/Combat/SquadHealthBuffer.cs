using System;

namespace Game.Core
{
    public class SquadHealthBuffer
    {
        public float SoldierMaxHealth { get; }
        public float CurrentSoldierHealth { get; private set; }
        public float GeneralMaxHealth { get; }
        public float CurrentGeneralHealth { get; private set; }
        public bool IsGeneralAlive => CurrentGeneralHealth > 0f;

        public SquadHealthBuffer(float soldierMaxHealth = 10f, float generalMaxHealth = 10f)
        {
            SoldierMaxHealth = Math.Max(1f, soldierMaxHealth);
            CurrentSoldierHealth = SoldierMaxHealth;
            GeneralMaxHealth = Math.Max(1f, generalMaxHealth);
            CurrentGeneralHealth = GeneralMaxHealth;
        }

        public int ApplyDamage(float damage, int currentSquadCount, out bool generalDied)
        {
            generalDied = false;
            if (damage <= 0f)
            {
                return 0;
            }

            int soldiersLost = 0;
            if (currentSquadCount > 0)
            {
                CurrentSoldierHealth -= damage;
                while (CurrentSoldierHealth <= 0f && (currentSquadCount - soldiersLost) > 0)
                {
                    soldiersLost++;
                    if ((currentSquadCount - soldiersLost) > 0)
                    {
                        CurrentSoldierHealth += SoldierMaxHealth;
                    }
                    else
                    {
                        float overflow = -CurrentSoldierHealth;
                        CurrentSoldierHealth = 0f;
                        if (overflow > 0f)
                        {
                            CurrentGeneralHealth -= overflow;
                            if (CurrentGeneralHealth <= 0f)
                            {
                                CurrentGeneralHealth = 0f;
                                generalDied = true;
                            }
                        }
                        return soldiersLost;
                    }
                }
                return soldiersLost;
            }

            CurrentGeneralHealth -= damage;
            if (CurrentGeneralHealth <= 0f)
            {
                CurrentGeneralHealth = 0f;
                generalDied = true;
            }
            return 0;
        }

        public void ResetSoldierHealth()
        {
            CurrentSoldierHealth = SoldierMaxHealth;
        }

        public void ResetGeneralHealth()
        {
            CurrentGeneralHealth = GeneralMaxHealth;
        }
    }
}
