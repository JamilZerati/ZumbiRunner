using System;

namespace Game.Core
{
    public static class SwipeDetector
    {
        public static bool TryDetectSwipe(float startX, float startY, float endX, float endY, float minDistance, out int direction)
        {
            direction = 0;
            float deltaX = endX - startX;
            float deltaY = endY - startY;

            if (Math.Abs(deltaX) < minDistance || Math.Abs(deltaX) <= Math.Abs(deltaY))
            {
                return false;
            }

            direction = deltaX > 0f ? 1 : -1;
            return true;
        }
    }
}
