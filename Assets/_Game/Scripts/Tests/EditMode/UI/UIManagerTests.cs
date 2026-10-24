using System.Collections.Generic;
using Game.Core;
using Game.Core.Analytics;
using Game.Presentation.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.UI
{
    public class UIManagerTests
    {
        private class DummyHubWindow : UIWindow
        {
            public override UILayerType LayerType => UILayerType.Hub;
        }

        private class DummyHudWindow : UIWindow
        {
            public override UILayerType LayerType => UILayerType.GameHud;
        }

        private class DummyModalWindow : UIWindow
        {
            public override UILayerType LayerType => UILayerType.Modal;
        }

        private readonly List<GameObject> _createdObjects = new();
        private UIManager _uiManager;
        private EventBus _eventBus;
        private GameStateMachine _stateMachine;
        private MockAnalyticsService _analytics;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _stateMachine = new GameStateMachine(_eventBus, GameState.Boot);
            _analytics = new MockAnalyticsService();

            _uiManager = CreateComponent<UIManager>("UIManager");
            _uiManager.Initialize(_stateMachine, _analytics, _eventBus);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();
        }

        private T CreateComponent<T>(string name = null) where T : Component
        {
            var go = new GameObject(name ?? typeof(T).Name);
            _createdObjects.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void RegisterAndRetrieveWindow_ByType_ReturnsRegisteredInstance()
        {
            var hubWindow = CreateComponent<DummyHubWindow>();
            _uiManager.RegisterWindow(hubWindow);

            var retrieved = _uiManager.GetWindow<DummyHubWindow>();
            Assert.AreSame(hubWindow, retrieved);
        }

        [Test]
        public void OpenWindow_ActivatesGameObject_SetsIsOpenTrue_AndMakesLayerVisible()
        {
            var layer = CreateComponent<UILayer>();
            layer.Initialize(UILayerType.Hub);
            layer.SetVisible(false);
            _uiManager.RegisterLayer(layer);

            var hubWindow = CreateComponent<DummyHubWindow>();
            hubWindow.gameObject.SetActive(false);
            _uiManager.RegisterWindow(hubWindow);

            var opened = _uiManager.OpenWindow<DummyHubWindow>();

            Assert.AreSame(hubWindow, opened);
            Assert.IsTrue(hubWindow.IsOpen);
            Assert.IsTrue(hubWindow.gameObject.activeSelf);
            Assert.IsTrue(layer.IsVisible);
        }

        [Test]
        public void OpenWindow_EmitsScreenViewAnalyticsEvent()
        {
            var modalWindow = CreateComponent<DummyModalWindow>();
            _uiManager.RegisterWindow(modalWindow);

            _uiManager.OpenWindow<DummyModalWindow>();

            Assert.AreEqual(1, _analytics.Events.Count);
            var evt = _analytics.Events[0];
            Assert.AreEqual(AnalyticsEvents.ScreenView, evt.EventName);
            Assert.AreEqual(nameof(DummyModalWindow), evt.GetParameter<string>("screen_name"));
            Assert.AreEqual(UILayerType.Modal.ToString(), evt.GetParameter<string>("layer"));
        }

        [Test]
        public void CloseWindow_DeactivatesGameObject_AndSetsIsOpenFalse()
        {
            var hudWindow = CreateComponent<DummyHudWindow>();
            _uiManager.RegisterWindow(hudWindow);
            _uiManager.OpenWindow<DummyHudWindow>();

            Assert.IsTrue(hudWindow.IsOpen);

            bool closed = _uiManager.CloseWindow<DummyHudWindow>();
            Assert.IsTrue(closed);
            Assert.IsFalse(hudWindow.IsOpen);
            Assert.IsFalse(hudWindow.gameObject.activeSelf);
        }

        [Test]
        public void IsModalOpen_ReturnsTrue_WhenModalWindowIsOpen()
        {
            var modalWindow = CreateComponent<DummyModalWindow>();
            _uiManager.RegisterWindow(modalWindow);

            Assert.IsFalse(_uiManager.IsModalOpen());

            _uiManager.OpenWindow<DummyModalWindow>();
            Assert.IsTrue(_uiManager.IsModalOpen());

            _uiManager.CloseWindow<DummyModalWindow>();
            Assert.IsFalse(_uiManager.IsModalOpen());
        }

        [Test]
        public void ModalInputBlocking_DisablesInteractionOnLowerLayers_WhenModalOpens_AndRestoresOnClose()
        {
            var hubLayer = CreateComponent<UILayer>("HubLayer");
            var hubCanvasGroup = hubLayer.gameObject.AddComponent<CanvasGroup>();
            hubLayer.Initialize(UILayerType.Hub, hubCanvasGroup);
            _uiManager.RegisterLayer(hubLayer);

            var modalWindow = CreateComponent<DummyModalWindow>();
            _uiManager.RegisterWindow(modalWindow);

            Assert.IsTrue(hubCanvasGroup.interactable);

            _uiManager.OpenWindow<DummyModalWindow>();
            Assert.IsFalse(hubCanvasGroup.interactable);
            Assert.IsFalse(hubCanvasGroup.blocksRaycasts);

            _uiManager.CloseWindow<DummyModalWindow>();
            Assert.IsTrue(hubCanvasGroup.interactable);
            Assert.IsTrue(hubCanvasGroup.blocksRaycasts);
        }

        [Test]
        public void GameStateChanged_HubState_ShowsHubLayer_HidesHudLayer_AndClosesModals()
        {
            var hubLayer = CreateComponent<UILayer>("HubLayer");
            hubLayer.Initialize(UILayerType.Hub);
            _uiManager.RegisterLayer(hubLayer);

            var hudLayer = CreateComponent<UILayer>("HudLayer");
            hudLayer.Initialize(UILayerType.GameHud);
            _uiManager.RegisterLayer(hudLayer);

            var modalWindow = CreateComponent<DummyModalWindow>();
            _uiManager.RegisterWindow(modalWindow);
            _uiManager.OpenWindow<DummyModalWindow>();

            _stateMachine.TryTransition(GameState.Hub);

            Assert.IsTrue(hubLayer.IsVisible);
            Assert.IsFalse(hudLayer.IsVisible);
            Assert.IsFalse(modalWindow.IsOpen);
        }

        [Test]
        public void GameStateChanged_RunState_ShowsHudLayer_HidesHubLayer_AndClosesModals()
        {
            var hubLayer = CreateComponent<UILayer>("HubLayer");
            hubLayer.Initialize(UILayerType.Hub);
            _uiManager.RegisterLayer(hubLayer);

            var hudLayer = CreateComponent<UILayer>("HudLayer");
            hudLayer.Initialize(UILayerType.GameHud);
            _uiManager.RegisterLayer(hudLayer);

            _stateMachine.TryTransition(GameState.Run);

            Assert.IsFalse(hubLayer.IsVisible);
            Assert.IsTrue(hudLayer.IsVisible);
        }

        [Test]
        public void TryTransition_DelegatesToGameStateMachine()
        {
            Assert.IsTrue(_uiManager.TryTransition(GameState.Hub));
            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);

            Assert.IsFalse(_uiManager.TryTransition(GameState.Paused));
            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);
        }

        [Test]
        public void UnregisterWindow_RemovesWindowFromManagerAndLayer()
        {
            var layer = CreateComponent<UILayer>();
            layer.Initialize(UILayerType.Hub);
            _uiManager.RegisterLayer(layer);

            var window = CreateComponent<DummyHubWindow>();
            _uiManager.RegisterWindow(window);

            Assert.AreSame(window, _uiManager.GetWindow<DummyHubWindow>());
            Assert.Contains(window, (System.Collections.ICollection)layer.Windows);

            _uiManager.UnregisterWindow(window);

            Assert.IsNull(_uiManager.GetWindow<DummyHubWindow>());
            Assert.IsFalse(((System.Collections.IList)layer.Windows).Contains(window));
        }
    }
}
