using System.Collections.Generic;
using UnityEngine;

namespace Game.Presentation.UI
{
    public class UILayer : MonoBehaviour
    {
        [SerializeField] private UILayerType _layerType;
        [SerializeField] private CanvasGroup _canvasGroup;
        private readonly List<UIWindow> _windows = new();

        public UILayerType LayerType => _layerType;
        public CanvasGroup CanvasGroup => _canvasGroup;
        public IReadOnlyList<UIWindow> Windows => _windows;
        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public void Initialize(UILayerType layerType, CanvasGroup canvasGroup = null)
        {
            _layerType = layerType;
            if (canvasGroup != null)
            {
                _canvasGroup = canvasGroup;
            }
            else if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public void AddWindow(UIWindow window)
        {
            if (window != null && !_windows.Contains(window))
            {
                _windows.Add(window);
            }
        }

        public void RemoveWindow(UIWindow window)
        {
            _windows.Remove(window);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1.0f : 0.0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = interactable;
                _canvasGroup.blocksRaycasts = interactable;
            }
        }
    }
}
