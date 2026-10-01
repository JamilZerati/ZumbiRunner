namespace Game.Core.Stats
{
    public interface IWeaponLoadout
    {
        StatCollection Stats { get; }
        string EquippedWeaponId { get; }
        bool TryEquip(string weaponId);
    }
}
