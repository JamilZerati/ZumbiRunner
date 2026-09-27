using UnityEngine;

namespace Game.Presentation
{
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 7.0f, -9.0f);
        [SerializeField, Min(0.1f)] private float horizontalSmoothSpeed = 10.0f;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public Vector3 Offset
        {
            get => offset;
            set => offset = value;
        }

        public float HorizontalSmoothSpeed
        {
            get => horizontalSmoothSpeed;
            set => horizontalSmoothSpeed = Mathf.Max(0.1f, value);
        }

        private void LateUpdate()
        {
            UpdateCameraPosition(Time.deltaTime);
        }

        public void Snap()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + offset;
        }

        public void UpdateCameraPosition(float deltaTime)
        {
            if (target == null)
            {
                return;
            }

            Vector3 currentPos = transform.position;
            Vector3 desiredPos = target.position + offset;

            float newX = deltaTime <= 0f
                ? desiredPos.x
                : Mathf.Lerp(currentPos.x, desiredPos.x, Mathf.Clamp01(horizontalSmoothSpeed * deltaTime));

            transform.position = new Vector3(newX, desiredPos.y, desiredPos.z);
        }
    }
}
