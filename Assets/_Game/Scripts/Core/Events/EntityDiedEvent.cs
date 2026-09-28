namespace Game.Core.Events
{
    public readonly struct EntityDiedEvent
    {
        public IDamageable Target { get; }
        public object Source { get; }

        public EntityDiedEvent(IDamageable target, object source = null)
        {
            Target = target;
            Source = source;
        }
    }
}
