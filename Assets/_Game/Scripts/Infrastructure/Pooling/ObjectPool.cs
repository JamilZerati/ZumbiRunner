using System;
using System.Collections.Generic;

namespace Game.Infrastructure
{
    public class ObjectPool<T> : IObjectPool<T>
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _onRent;
        private readonly Action<T> _onReturn;
        private readonly Stack<T> _pool = new Stack<T>();
        private int _countActive;

        public int CountActive => _countActive;
        public int CountInactive => _pool.Count;

        public ObjectPool(Func<T> factory, Action<T> onRent = null, Action<T> onReturn = null, int initialCapacity = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onRent = onRent;
            _onReturn = onReturn;

            for (int i = 0; i < initialCapacity; i++)
            {
                _pool.Push(_factory());
            }
        }

        public T Rent()
        {
            T item = _pool.Count > 0 ? _pool.Pop() : _factory();
            _countActive++;
            _onRent?.Invoke(item);
            return item;
        }

        public void Return(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _onReturn?.Invoke(item);
            _pool.Push(item);
            _countActive = Math.Max(0, _countActive - 1);
        }
    }
}
