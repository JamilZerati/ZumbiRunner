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
            throw new NotImplementedException();
        }

        public void RegisterKill(bool byAbility = false)
        {
            throw new NotImplementedException();
        }

        public bool TryConsumeCharge()
        {
            throw new NotImplementedException();
        }
    }
}
