using System;

namespace Game.Core
{
    public interface IEventBus
    {
        void Publish<T>(T eventData);
        IDisposable Subscribe<T>(Action<T> handler);
    }
}
