using System;
using System.Collections.Generic;

namespace Game.Core
{
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        public void Publish<T>(T eventData)
        {
            var eventType = typeof(T);
            if (!_subscribers.TryGetValue(eventType, out var delegates))
            {
                return;
            }

            var snapshot = delegates.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i] is Action<T> action)
                {
                    action.Invoke(eventData);
                }
            }
        }

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(T);
            if (!_subscribers.TryGetValue(eventType, out var delegates))
            {
                delegates = new List<Delegate>();
                _subscribers[eventType] = delegates;
            }

            delegates.Add(handler);
            return new SubscriptionToken(() => Unsubscribe(eventType, handler));
        }

        private void Unsubscribe(Type eventType, Delegate handler)
        {
            if (_subscribers.TryGetValue(eventType, out var delegates))
            {
                delegates.Remove(handler);
                if (delegates.Count == 0)
                {
                    _subscribers.Remove(eventType);
                }
            }
        }

        private sealed class SubscriptionToken : IDisposable
        {
            private Action _unsubscribeAction;

            public SubscriptionToken(Action unsubscribeAction)
            {
                _unsubscribeAction = unsubscribeAction;
            }

            public void Dispose()
            {
                var action = System.Threading.Interlocked.Exchange(ref _unsubscribeAction, null);
                action?.Invoke();
            }
        }
    }
}
