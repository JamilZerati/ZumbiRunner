using System;

namespace Game.Core.Abilities
{
    public class AbilityChargeTracker
    {
        public int CurrentKills { get; private set; }
        public int KillsPerCharge { get; }
        public int CurrentCharges { get; private set; }

        public AbilityChargeTracker(int killsPerCharge, int initialCharges = 0)
        {
            if (killsPerCharge <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(killsPerCharge), "Kills per charge must be greater than zero.");
            }

            CurrentKills = 0;
            KillsPerCharge = killsPerCharge;
            CurrentCharges = initialCharges;
        }

        public void RegisterKill(bool byAbility = false)
        {
            // Abates provocados por habilidade nao geram cargas para impedir auto-recarga infinita.
            if (byAbility)
            {
                return;
            }

            CurrentKills++;
            if (CurrentKills >= KillsPerCharge)
            {
                CurrentCharges += CurrentKills / KillsPerCharge;
                CurrentKills %= KillsPerCharge;
            }
        }

        public bool TryConsumeCharge()
        {
            if (CurrentCharges > 0)
            {
                CurrentCharges--;
                return true;
            }

            return false;
        }
    }
}
