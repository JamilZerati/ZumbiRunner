namespace Game.Core
{
    public interface ISquad
    {
        int SquadCount { get; }
        bool Add(int amount);
        bool Remove(int amount);
        bool Multiply(int factor);
        bool Divide(int divisor);
        bool SetCount(int newCount);
    }
}
