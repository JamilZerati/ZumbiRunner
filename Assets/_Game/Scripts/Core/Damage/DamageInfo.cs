namespace Game.Core
{
    public readonly struct DamageInfo
    {
        public int Amount { get; }
        public DamageType Type { get; }
        public object Source { get; }

        public DamageInfo(int amount, DamageType type = DamageType.Physical, object source = null)
        {
            Amount = amount;
            Type = type;
            Source = source;
        }
    }
}
