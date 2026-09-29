using System;
using System.Collections.Generic;

namespace Game.Core.Status
{
    public sealed class OnHitStatusSet
    {
        public event Action Changed;
        public IReadOnlyList<StatusApplication> Items => throw new NotImplementedException();
        public void Add(StatusApplication application) => throw new NotImplementedException();
        public int RemoveAllFromSource(object source) => throw new NotImplementedException();
        public StatusApplication[] Snapshot() => throw new NotImplementedException();
    }
}
