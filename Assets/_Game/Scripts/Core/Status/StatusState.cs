namespace Game.Core.Status
{
    public sealed class StatusState
    {
        public StatusKind Kind { get; }
        public int Stacks { get; set; }
        public float Remaining { get; set; }
        public float TickTimer { get; set; }
        public float Potency { get; set; }

        public StatusState(StatusKind kind)
        {
            Kind = kind;
        }
    }
}
