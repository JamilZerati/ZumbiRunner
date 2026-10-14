using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Allies;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay
{
    public class SquadController : MonoBehaviour, ISquad, IAllyRoster
    {
        [SerializeField] private int initialCount = 1;

        private IEventBus eventBus;
        private bool isInitialized;
        private IAllyRoster _roster;

        public event Action<SquadSizeChangedEvent> SquadSizeChanged;

        public IAllyRoster Roster
        {
            get => _roster ??= new AllyRoster(bus: eventBus);
            set => _roster = value;
        }

        public IReadOnlyList<IAllyInstance> Allies => Roster.Allies;
        public int HeroCount => Roster.HeroCount;
        public int PetCount => Roster.PetCount;

        public bool TryAddAlly(IAllyInstance ally, out IAllyInstance registeredAlly) => Roster.TryAddAlly(ally, out registeredAlly);
        public bool TryRecruit(string allyId, out IAllyInstance registeredAlly) => Roster.TryRecruit(allyId, out registeredAlly);
        public bool TryGetAlly(string id, out IAllyInstance ally) => Roster.TryGetAlly(id, out ally);
        public bool ContainsAlly(string id) => Roster.ContainsAlly(id);
        public void Clear() => Roster.Clear();

        public static Vector3 GetAllyFlankOffset(int allyIndex, float lateralSpacing = 1.8f)
        {
            float side = (allyIndex % 2 == 0) ? -1f : 1f;
            int rank = allyIndex / 2;
            float x = side * (lateralSpacing + (rank * 0.4f));
            float z = -0.5f - (rank * 0.7f);
            return new Vector3(x, 0f, z);
        }

        public void PositionAlly(Transform allyTransform, int allyIndex)
        {
            if (allyTransform != null)
            {
                allyTransform.position = transform.position + GetAllyFlankOffset(allyIndex);
            }
        }

        private int _squadCount;
        public int SquadCount
        {
            get
            {
                if (!isInitialized)
                {
                    _squadCount = Mathf.Max(0, initialCount);
                    isInitialized = true;
                }
                return _squadCount;
            }
            private set => _squadCount = value;
        }

        public bool IsAlive => SquadCount > 0;

        private void Awake()
        {
            if (!isInitialized)
            {
                SquadCount = Mathf.Max(0, initialCount);
                isInitialized = true;
            }
        }

        public void Initialize(int count, IEventBus bus = null, IAllyRoster roster = null)
        {
            SquadCount = Mathf.Max(0, count);
            eventBus = bus;
            if (roster != null) _roster = roster;
            else if (_roster == null && bus != null) _roster = new AllyRoster(bus: bus);
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
