namespace Game.Core.Perks
{
    public interface IPerkEffect
    {
        string Description { get; }
        void Apply(PerkContext context);
    }
}
