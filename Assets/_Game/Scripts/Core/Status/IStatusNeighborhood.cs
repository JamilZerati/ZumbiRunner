using System.Collections.Generic;

namespace Game.Core.Status
{
    public interface IStatusNeighborhood
    {
        IReadOnlyList<IStatusReceiver> FindNearby(IStatusReceiver origin, float radius, int maxCount);
    }
}
