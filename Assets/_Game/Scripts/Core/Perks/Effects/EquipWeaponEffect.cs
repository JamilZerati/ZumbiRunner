using System;

namespace Game.Core.Perks.Effects
{
    [Serializable]
    public class EquipWeaponEffect : IPerkEffect
    {
        public string WeaponId;

        public EquipWeaponEffect()
        {
        }

        public EquipWeaponEffect(string weaponId)
        {
            WeaponId = weaponId;
        }

        public string Description => throw new NotImplementedException();

        public void Apply(PerkContext context) => throw new NotImplementedException();
    }
}
