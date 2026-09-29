using Game.Core.Status;

namespace Game.Core.Stats
{
    public interface IWeaponLoadout
    {
        StatCollection Stats { get; }
        OnHitStatusSet OnHitStatuses { get; }
        string EquippedWeaponId { get; }
        bool TryEquip(string weaponId);
    }
}
