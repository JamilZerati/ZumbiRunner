using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Analytics;
using UnityEngine;

namespace Game.Presentation.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private List<UILayer> _layers = new();
        [SerializeField] private List<UIWindow> _preloadedWindows = new();

        private readonly Dictionary<UILayerType, UILayer> _layersByType = new();
        private readonly Dictionary<Type, UIWindow> _windowsByType = new();
        private readonly List<UIWindow> _allWindows = new();

        private IGameStateMachine _stateMachine;
        private IAnalyticsService _analytics;
        private IEventBus _eventBus;
        private IDisposable _stateSubscription;

        public IGameStateMachine StateMachine => _stateMachine;
        public IReadOnlyList<UIWindow> Windows => _allWindows;

        private void Awake()
        {
            AutoRegisterComponents();
        }

        private void OnDestroy()
        {
            _stateSubscription?.Dispose();
            _stateSubscription = null;

            for (int i = 0; i < _allWindows.Count; i++)
            {
                var window = _allWindows[i];
                if (window != null)
                {
                    window.OnOpened -= HandleWindowOpened;
                    window.OnClosed -= HandleWindowClosed;
                }
            }

            _allWindows.Clear();
            _windowsByType.Clear();
            _layersByType.Clear();
        }

        public void Initialize(
            IGameStateMachine stateMachine = null,
            IAnalyticsService analytics = null,
            IEventBus eventBus = null)
        {
            _stateSubscription?.Dispose();
            _stateSubscription = null;

            AutoRegisterComponents();
            _stateMachine = stateMachine;
            _analytics = analytics;
            _eventBus = eventBus;

            if (_eventBus != null)
            {
                _stateSubscription = _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            }
        }

        public void RegisterLayer(UILayer layer)
        {
            if (layer == null)
            {
                return;
            }

            _layersByType[layer.LayerType] = layer;
            if (!_layers.Contains(layer))
            {
                _layers.Add(layer);
            }

            for (int i = 0; i < _allWindows.Count; i++)
            {
                var window = _allWindows[i];
                if (window != null && window.LayerType == layer.LayerType)
                {
                    layer.AddWindow(window);
                }
            }
        }

        public UILayer GetLayer(UILayerType layerType)
        {
            _layersByType.TryGetValue(layerType, out var layer);
            return layer;
        }

        public void SetLayerVisible(UILayerType layerType, bool visible)
        {
            var layer = GetLayer(layerType);
            layer?.SetVisible(visible);
        }

        public bool IsLayerVisible(UILayerType layerType)
        {
            var layer = GetLayer(layerType);
            return layer != null && layer.IsVisible;
        }

        public void RegisterWindow(UIWindow window)
        {
            if (window == null)
            {
                return;
            }

            var type = window.GetType();
            _windowsByType[type] = window;

            if (!_allWindows.Contains(window))
            {
                _allWindows.Add(window);
            }

            var layer = GetLayer(window.LayerType);
            layer?.AddWindow(window);

            window.OnOpened -= HandleWindowOpened;
            window.OnOpened += HandleWindowOpened;
            window.OnClosed -= HandleWindowClosed;
            window.OnClosed += HandleWindowClosed;
        }

        public void UnregisterWindow(UIWindow window)
        {
            if (window == null)
            {
                return;
            }

            _windowsByType.Remove(window.GetType());
            _allWindows.Remove(window);

            var layer = GetLayer(window.LayerType);
            layer?.RemoveWindow(window);

            window.OnOpened -= HandleWindowOpened;
            window.OnClosed -= HandleWindowClosed;

            if (window.LayerType == UILayerType.Modal)
            {
                UpdateModalInputBlocking();
            }
        }

        public T GetWindow<T>() where T : UIWindow
        {
            if (_windowsByType.TryGetValue(typeof(T), out var window) && window != null)
            {
                return (T)window;
            }

            for (int i = 0; i < _allWindows.Count; i++)
            {
                if (_allWindows[i] != null && _allWindows[i] is T typed)
                {
                    return typed;
                }
            }

            return null;
        }

        public T OpenWindow<T>() where T : UIWindow
        {
            var window = GetWindow<T>();
            if (window == null)
            {
                return null;
            }

            OpenWindowInstance(window);
            return window;
        }

        public UIWindow OpenWindow(UIWindow window)
        {
            if (window == null)
            {
                return null;
            }

            if (!_allWindows.Contains(window))
            {
                RegisterWindow(window);
            }

            OpenWindowInstance(window);
            return window;
        }

        public bool CloseWindow<T>() where T : UIWindow
        {
            var window = GetWindow<T>();
            if (window == null)
            {
                return false;
            }

            window.Close();
            return true;
        }

        public void CloseWindow(UIWindow window)
        {
            window?.Close();
        }

        public bool IsWindowOpen<T>() where T : UIWindow
        {
            var window = GetWindow<T>();
            return window != null && window.IsOpen;
        }

        public bool IsModalOpen()
        {
            for (int i = 0; i < _allWindows.Count; i++)
            {
                if (_allWindows[i] != null && _allWindows[i].LayerType == UILayerType.Modal && _allWindows[i].IsOpen)
                {
                    return true;
                }
            }

            return false;
        }

        public void CloseAllWindows()
        {
            for (int i = _allWindows.Count - 1; i >= 0; i--)
            {
                if (i < _allWindows.Count && _allWindows[i] != null)
                {
                    _allWindows[i].Close();
                }
            }
        }

        public void CloseWindowsInLayer(UILayerType layerType)
        {
            for (int i = _allWindows.Count - 1; i >= 0; i--)
            {
                if (i < _allWindows.Count && _allWindows[i] != null && _allWindows[i].LayerType == layerType)
                {
                    _allWindows[i].Close();
                }
            }
        }

        public bool TryTransition(GameState nextState)
        {
            return _stateMachine != null && _stateMachine.TryTransition(nextState);
        }

        private void OpenWindowInstance(UIWindow window)
        {
            if (window.IsOpen)
            {
                return;
            }

            SetLayerVisible(window.LayerType, true);
            window.Open();

            _analytics?.TrackEvent(AnalyticsEvents.ScreenView, new Dictionary<string, object>
            {
                ["screen_name"] = window.GetType().Name,
                ["layer"] = window.LayerType.ToString()
            });
        }

        private void HandleWindowOpened(UIWindow window)
        {
            if (window.LayerType == UILayerType.Modal)
            {
                UpdateModalInputBlocking();
            }
        }

        private void HandleWindowClosed(UIWindow window)
        {
            if (window.LayerType == UILayerType.Modal)
            {
                UpdateModalInputBlocking();
            }
        }

        private void UpdateModalInputBlocking()
        {
            bool hasModal = IsModalOpen();
            GetLayer(UILayerType.Modal)?.SetVisible(hasModal);
            GetLayer(UILayerType.Hub)?.SetInteractable(!hasModal);
            GetLayer(UILayerType.GameHud)?.SetInteractable(!hasModal);
            GetLayer(UILayerType.TutorialOverlay)?.SetInteractable(!hasModal);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case GameState.Hub:
                    SetLayerVisible(UILayerType.Hub, true);
                    SetLayerVisible(UILayerType.GameHud, false);
                    CloseWindowsInLayer(UILayerType.Modal);
                    SetLayerVisible(UILayerType.Modal, false);
                    break;
                case GameState.Run:
                    SetLayerVisible(UILayerType.Hub, false);
                    SetLayerVisible(UILayerType.GameHud, true);
                    CloseWindowsInLayer(UILayerType.Modal);
                    SetLayerVisible(UILayerType.Modal, false);
                    break;
                case GameState.Paused:
                case GameState.Result:
                    SetLayerVisible(UILayerType.Modal, true);
                    break;
            }
        }

        private void AutoRegisterComponents()
        {
            var layers = GetComponentsInChildren<UILayer>(true);
            for (int i = 0; i < layers.Length; i++)
            {
                RegisterLayer(layers[i]);
            }

            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] != null)
                {
                    RegisterLayer(_layers[i]);
                }
            }

            var windows = GetComponentsInChildren<UIWindow>(true);
            for (int i = 0; i < windows.Length; i++)
            {
                RegisterWindow(windows[i]);
            }

            for (int i = 0; i < _preloadedWindows.Count; i++)
            {
                if (_preloadedWindows[i] != null)
                {
                    RegisterWindow(_preloadedWindows[i]);
                }
            }
        }
    }
}
