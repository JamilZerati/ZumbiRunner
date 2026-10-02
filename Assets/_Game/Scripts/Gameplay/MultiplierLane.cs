using UnityEngine;
using Game.Core;
using Game.Core.Events;

namespace Game.Gameplay
{
    public class MultiplierLane : MonoBehaviour
    {
        public int[] CostsPerStep = new int[] { 5, 10, 15, 20, 25 };
        public int DistanceBetweenMilestones = 15;
        
        public int CurrentMultiplier { get; private set; } = 0;
        
        private IEventBus _eventBus;

        public void Initialize(IEventBus eventBus)
        {
            _eventBus = eventBus;
            CurrentMultiplier = 0;
        }

        public bool TryAdvance(ISquad squad)
        {
            if (squad == null || CurrentMultiplier >= CostsPerStep.Length)
            {
                return false;
            }

            int cost = CostsPerStep[CurrentMultiplier];
            if (squad.SquadCount >= cost)
            {
                if (squad.Remove(cost))
                {
                    CurrentMultiplier++;
                    _eventBus?.Publish(new MultiplierReachedEvent(CurrentMultiplier, cost));
                    return true;
                }
            }

            return false;
        }
    }
}
