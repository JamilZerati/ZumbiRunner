namespace Game.Core.Abilities
{
    public interface IAbilityEffect
    {
        string Description { get; }
        void Execute(AbilityExecutionContext context);
    }
}
