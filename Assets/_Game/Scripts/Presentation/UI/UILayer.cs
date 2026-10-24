using System.Collections.Generic;
using UnityEngine;

namespace Game.Presentation.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UILayer : MonoBehaviour
    {
        [SerializeField] private UILayerType _layerType;
        [SerializeField] private CanvasGroup _canvasGroup;
        private readonly List<UIWindow> _windows = new();

        public UILayerType LayerType => _layerType;
        public CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                {
                    _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
                }
                return _canvasGroup;
            }
        }
        public IReadOnlyList<UIWindow> Windows => _windows;
        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            _canvasGroup ??= GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        public void Initialize(UILayerType layerType, CanvasGroup canvasGroup = null)
        {
            _layerType = layerType;
            if (canvasGroup != null)
            {
                _canvasGroup = canvasGroup;
            }
            else
            {
                _canvasGroup ??= GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
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
            var cg = CanvasGroup;
            if (cg != null)
            {
                cg.alpha = visible ? 1.0f : 0.0f;
                cg.interactable = visible;
                cg.blocksRaycasts = visible;
            }
        }

        public void SetInteractable(bool interactable)
        {
            var cg = CanvasGroup;
            if (cg != null)
            {
                cg.interactable = interactable;
                cg.blocksRaycasts = interactable;
            }
        }
    }
}
