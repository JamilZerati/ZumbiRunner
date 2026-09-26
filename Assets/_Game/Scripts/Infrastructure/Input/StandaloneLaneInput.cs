using System;
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Infrastructure.Input
{
    public class StandaloneLaneInput : MonoBehaviour, ILaneInput
    {
        [SerializeField, Min(10f)] private float minSwipeDistancePixels = 40.0f;

        public event Action<int> MoveRequested;

        private Vector2 touchStartPosition;
        private bool isPointerPressed;

        public float MinSwipeDistancePixels
        {
            get => minSwipeDistancePixels;
            set => minSwipeDistancePixels = Mathf.Max(10f, value);
        }

        private void Update()
        {
            PollKeyboard();
            PollPointer();
        }

        public void TriggerMove(int direction)
        {
            if (direction != 0)
            {
                MoveRequested?.Invoke(direction);
            }
        }

        private void PollKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                TriggerMove(-1);
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                TriggerMove(1);
            }
        }

        private void PollPointer()
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                touchStartPosition = pointer.position.ReadValue();
                isPointerPressed = true;
            }
            else if (isPointerPressed && pointer.press.wasReleasedThisFrame)
            {
                isPointerPressed = false;
                Vector2 touchEndPosition = pointer.position.ReadValue();

                if (SwipeDetector.TryDetectSwipe(
                    touchStartPosition.x,
                    touchStartPosition.y,
                    touchEndPosition.x,
                    touchEndPosition.y,
                    minSwipeDistancePixels,
                    out int direction))
                {
                    TriggerMove(direction);
                }
            }
        }
    }
}
