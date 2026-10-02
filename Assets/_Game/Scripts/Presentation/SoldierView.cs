using Game.Gameplay;
using UnityEngine;

namespace Game.Presentation
{
    public class SoldierView : MonoBehaviour
    {
        public Vector3 TargetOffset { get; private set; }

        private void Awake()
        {
            EnsurePhysicsSetup();
        }

        public void EnsurePhysicsSetup()
        {
            gameObject.layer = CollisionLayers.SquadBodyLayer;
            if (!TryGetComponent<Collider>(out var col))
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }
        }

        public void SetTargetOffset(Vector3 offset)
        {
            TargetOffset = offset;
        }

        public void UpdatePosition(Vector3 leaderPosition, float deltaTime, float followSpeed = 15f)
        {
            Vector3 targetPos = leaderPosition + TargetOffset;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, followSpeed * deltaTime);
        }

        public void SnapToTarget(Vector3 leaderPosition)
        {
            transform.position = leaderPosition + TargetOffset;
        }
    }
}
