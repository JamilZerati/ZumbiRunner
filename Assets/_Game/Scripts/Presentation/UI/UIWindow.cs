using System;
using UnityEngine;

namespace Game.Presentation.UI
{
    public class UIWindow : MonoBehaviour
    {
        [SerializeField] private UILayerType _layerType;
        private bool _isOpen;

        public virtual UILayerType LayerType => _layerType;
        public bool IsOpen => _isOpen;

        public event Action<UIWindow> OnOpened;
        public event Action<UIWindow> OnClosed;

        protected virtual void Awake()
        {
            _isOpen = gameObject.activeSelf;
        }

        public void SetLayerType(UILayerType layerType)
        {
            _layerType = layerType;
        }

        public virtual void Open()
        {
            if (_isOpen)
            {
                return;
            }

            gameObject.SetActive(true);
            _isOpen = true;
            OnOpen();
            OnOpened?.Invoke(this);
        }

        public virtual void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            gameObject.SetActive(false);
            _isOpen = false;
            OnClose();
            OnClosed?.Invoke(this);
        }

        protected virtual void OnOpen()
        {
        }

        protected virtual void OnClose()
        {
        }
    }
}
