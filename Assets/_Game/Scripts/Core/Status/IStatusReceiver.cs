using System.Collections.Generic;

namespace Game.Core.Status
{
    public interface IStatusReceiver
    {
        bool IsAlive { get; }
        int ReceiveHit(DamageInfo hit, IReadOnlyList<StatusApplication> onHit);
    }
}
