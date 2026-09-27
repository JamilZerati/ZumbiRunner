using UnityEngine;

namespace Game.Presentation
{
    public class SoldierView : MonoBehaviour
    {
        public Vector3 TargetOffset { get; private set; }

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
