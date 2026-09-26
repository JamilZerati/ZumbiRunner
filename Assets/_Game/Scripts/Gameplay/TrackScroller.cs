using UnityEngine;

namespace Game.Gameplay
{
    public class TrackScroller : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float forwardSpeed = 8.0f;

        public float ForwardSpeed
        {
            get => forwardSpeed;
            set => forwardSpeed = Mathf.Max(0f, value);
        }

        public float DistanceTraveled { get; private set; }
        public bool IsPaused { get; set; }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        public void Step(float deltaTime)
        {
            if (IsPaused || deltaTime <= 0f)
            {
                return;
            }

            float delta = forwardSpeed * deltaTime;
            DistanceTraveled += delta;

            Vector3 pos = transform.position;
            pos.z += delta;
            transform.position = pos;
        }

        public void ResetDistance()
        {
            DistanceTraveled = 0f;
        }
    }
}
