using System;
using Game.Core;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay
{
    public class SquadController : MonoBehaviour
    {
        [SerializeField] private int initialCount = 1;

        private IEventBus eventBus;
        private bool isInitialized;

        public event Action<SquadSizeChangedEvent> SquadSizeChanged;

        public int SquadCount { get; private set; }
        public bool IsAlive => SquadCount > 0;

        private void Awake()
        {
            if (!isInitialized)
            {
                SquadCount = Mathf.Max(0, initialCount);
                isInitialized = true;
            }
        }

        public void Initialize(int count, IEventBus bus = null)
        {
            SquadCount = Mathf.Max(0, count);
            eventBus = bus;
            isInitialized = true;
        }

        public bool Add(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            return ApplyCountChange(SquadCount + amount);
        }

        public bool Remove(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            return ApplyCountChange(SquadCount - amount);
        }

        public bool Multiply(int factor)
        {
            if (factor < 0)
            {
                return false;
            }

            return ApplyCountChange(SquadCount * factor);
        }

        public bool Divide(int divisor)
        {
            if (divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(divisor), "Divisor must be greater than zero.");
            }

            return ApplyCountChange(SquadCount / divisor);
        }

        public bool SetCount(int newCount)
        {
            return ApplyCountChange(newCount);
        }

        private bool ApplyCountChange(int next)
        {
            next = Mathf.Max(0, next);
            if (next == SquadCount)
            {
                return false;
            }

            int previous = SquadCount;
            SquadCount = next;
            var evt = new SquadSizeChangedEvent(previous, SquadCount);
            eventBus?.Publish(evt);
            SquadSizeChanged?.Invoke(evt);
            return true;
        }
    }
}
