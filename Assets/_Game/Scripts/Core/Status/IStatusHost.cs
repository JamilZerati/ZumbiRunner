namespace Game.Core.Status
{
    public interface IStatusHost
    {
        IStatusReceiver Owner { get; }
        IStatusNeighborhood Neighborhood { get; }
        IStatusCatalog Catalog { get; }
        bool Has(StatusKind kind);
        int GetStacks(StatusKind kind);
        void Apply(StatusApplication application);
        bool Remove(StatusKind kind);
        void DealDamage(int amount, DamageType type, object source);
    }
}
