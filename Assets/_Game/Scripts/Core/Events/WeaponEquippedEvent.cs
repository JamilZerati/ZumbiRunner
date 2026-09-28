using System;

namespace Game.Core.Events
{
    public readonly struct WeaponEquippedEvent
    {
        public string WeaponId { get; }
        public string PreviousWeaponId { get; }

        public WeaponEquippedEvent(string weaponId, string previousWeaponId)
        {
            throw new NotImplementedException();
        }
    }
}
