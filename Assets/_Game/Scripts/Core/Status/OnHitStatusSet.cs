using System;
using System.Collections.Generic;

namespace Game.Core.Status
{
    public sealed class OnHitStatusSet
    {
        private readonly List<StatusApplication> items = new List<StatusApplication>();

        public event Action Changed;

        public IReadOnlyList<StatusApplication> Items => items;

        public void Add(StatusApplication application)
        {
            items.Add(application);
            Changed?.Invoke();
        }

        public int RemoveAllFromSource(object source)
        {
            int removed = items.RemoveAll(item => Equals(item.Source, source));
            if (removed > 0)
            {
                Changed?.Invoke();
            }
            return removed;
        }

        public StatusApplication[] Snapshot()
        {
            return items.Count == 0 ? Array.Empty<StatusApplication>() : items.ToArray();
        }
    }
}
