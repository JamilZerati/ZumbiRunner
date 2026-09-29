using Game.Core.Status;

namespace Game.Core.Events
{
    public readonly struct SynergyTriggeredEvent
    {
        public string InteractionId { get; }
        public IStatusReceiver Target { get; }
        public int HitDamage { get; }
        public int FinalDamage { get; }

        public SynergyTriggeredEvent(string interactionId, IStatusReceiver target, int hitDamage, int finalDamage)
        {
            InteractionId = interactionId;
            Target = target;
            HitDamage = hitDamage;
            FinalDamage = finalDamage;
        }
    }
}
