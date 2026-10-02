namespace Game.Core.Abilities
{
    public class AbilityExecutionContext
    {
        public AbilityPosition OriginPosition { get; set; }
        public int TargetLane { get; set; }
        public IAbilityDamageSink DamageSink { get; set; }
        public IEventBus EventBus { get; set; }
    }
}
