namespace Game.Infrastructure
{
    public interface IObjectPool<T>
    {
        T Rent();
        void Return(T item);
        int CountActive { get; }
        int CountInactive { get; }
    }
}
