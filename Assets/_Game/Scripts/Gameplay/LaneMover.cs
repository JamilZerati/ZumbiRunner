using Game.Core;
using Game.Core.Events;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay
{
    public class LaneMover : MonoBehaviour
    {
        [SerializeField] private LaneLayoutDefinition layoutDefinition;
        [SerializeField, Min(0.1f)] private float laneChangeSpeed = 12.0f;

        private LaneLayout layout = new LaneLayout(2);
        private ILaneInput input;
        private IEventBus eventBus;

        public int CurrentLane { get; private set; }
        public int TargetLane { get; private set; }
        public LaneLayout Layout => layout;
        public float LaneChangeSpeed => laneChangeSpeed;

        private void Awake()
        {
            if (layoutDefinition != null)
            {
                layout = layoutDefinition.ToLayout();
            }
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.MoveRequested += OnMoveRequested;
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.MoveRequested -= OnMoveRequested;
            }
        }

        private void Update()
        {
            UpdatePosition(Time.deltaTime);
        }

        public void Initialize(LaneLayout laneLayout, ILaneInput laneInput = null, IEventBus bus = null, int initialLane = 0)
        {
            if (input != null)
            {
                input.MoveRequested -= OnMoveRequested;
            }

            layout = laneLayout;
            input = laneInput;
            eventBus = bus;
            CurrentLane = Mathf.Clamp(initialLane, 0, layout.LaneCount - 1);
            TargetLane = CurrentLane;

            if (input != null && isActiveAndEnabled)
            {
                input.MoveRequested += OnMoveRequested;
            }

            Vector3 pos = transform.position;
            pos.x = layout.GetLaneCenterX(CurrentLane);
            transform.position = pos;
        }

        public bool RequestMove(int direction)
        {
            int candidate = Mathf.Clamp(TargetLane + direction, 0, layout.LaneCount - 1);
            if (candidate == TargetLane)
            {
                return false;
            }

            int previous = TargetLane;
            TargetLane = candidate;
            eventBus?.Publish(new LaneChangedEvent(previous, TargetLane));
            return true;
        }

        public void UpdatePosition(float deltaTime)
        {
            float targetX = layout.GetLaneCenterX(TargetLane);
            Vector3 currentPos = transform.position;
            float newX = Mathf.MoveTowards(currentPos.x, targetX, laneChangeSpeed * deltaTime);

            currentPos.x = newX;
            transform.position = currentPos;

            if (Mathf.Approximately(newX, targetX))
            {
                CurrentLane = TargetLane;
            }
        }

        private void OnMoveRequested(int direction)
        {
            RequestMove(direction);
        }
    }
}
