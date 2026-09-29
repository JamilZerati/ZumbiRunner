using System;

namespace Game.Core.Status
{
    public readonly struct StatusApplication
    {
        public StatusKind Kind { get; }
        public int Stacks { get; }
        public object Source { get; }

        public StatusApplication(StatusKind kind, int stacks, object source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (stacks < 1) throw new ArgumentOutOfRangeException(nameof(stacks));
            Kind = kind;
            Stacks = stacks;
            Source = source;
        }
    }
}
