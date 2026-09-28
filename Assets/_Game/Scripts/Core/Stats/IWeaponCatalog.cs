namespace Game.Core.Stats
{
    public interface IWeaponCatalog
    {
        bool TryGet(string weaponId, out WeaponProfile profile);
    }
}
