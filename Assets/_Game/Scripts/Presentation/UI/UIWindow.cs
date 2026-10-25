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
            if (!_isOpen)
            {
                gameObject.SetActive(false);
            }
        }

        protected virtual void OnDestroy()
        {
            OnOpened = null;
            OnClosed = null;
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

            _isOpen = true;
            gameObject.SetActive(true);
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
