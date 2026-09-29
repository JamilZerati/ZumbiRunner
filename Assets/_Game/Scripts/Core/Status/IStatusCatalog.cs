namespace Game.Core.Status
{
    public interface IStatusCatalog
    {
        bool TryGet(StatusKind kind, out IStatusEffect effect);
    }
}
