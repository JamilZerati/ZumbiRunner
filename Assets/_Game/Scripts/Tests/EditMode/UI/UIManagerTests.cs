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

        private class DummyModalWindow2 : UIWindow
        {
            public override UILayerType LayerType => UILayerType.Modal;
        }

        private class DummyTutorialWindow : UIWindow
        {
            public override UILayerType LayerType => UILayerType.TutorialOverlay;
        }

        private class AutoUnregisterWindow : UIWindow
        {
            private UIManager _manager;

            public void SetManager(UIManager manager)
            {
                _manager = manager;
            }

            protected override void OnClose()
            {
                _manager?.UnregisterWindow(this);
            }
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
            hubLayer.Initialize(UILayerType.Hub);
            var hubCanvasGroup = hubLayer.CanvasGroup;
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

        [Test]
        public void ModalStacking_ClosingOneModal_KeepsLowerLayersBlocked_UntilAllModalsClosed()
        {
            var hubLayer = CreateComponent<UILayer>("HubLayer");
            hubLayer.Initialize(UILayerType.Hub);
            _uiManager.RegisterLayer(hubLayer);

            var tutorialLayer = CreateComponent<UILayer>("TutorialLayer");
            tutorialLayer.Initialize(UILayerType.TutorialOverlay);
            _uiManager.RegisterLayer(tutorialLayer);

            var modalLayer = CreateComponent<UILayer>("ModalLayer");
            modalLayer.Initialize(UILayerType.Modal);
            _uiManager.RegisterLayer(modalLayer);

            var modal1 = CreateComponent<DummyModalWindow>();
            var modal2 = CreateComponent<DummyModalWindow2>();
            _uiManager.RegisterWindow(modal1);
            _uiManager.RegisterWindow(modal2);

            _uiManager.OpenWindow<DummyModalWindow>();

            Assert.IsTrue(modalLayer.IsVisible);
            Assert.IsFalse(hubLayer.CanvasGroup.interactable);
            Assert.IsFalse(hubLayer.CanvasGroup.blocksRaycasts);
            Assert.IsFalse(tutorialLayer.CanvasGroup.interactable);
            Assert.IsFalse(tutorialLayer.CanvasGroup.blocksRaycasts);

            _uiManager.OpenWindow<DummyModalWindow2>();

            Assert.IsFalse(hubLayer.CanvasGroup.interactable);
            Assert.IsFalse(tutorialLayer.CanvasGroup.interactable);

            _uiManager.CloseWindow<DummyModalWindow>();

            Assert.IsTrue(_uiManager.IsModalOpen());
            Assert.IsTrue(modalLayer.IsVisible);
            Assert.IsFalse(hubLayer.CanvasGroup.interactable);
            Assert.IsFalse(tutorialLayer.CanvasGroup.interactable);

            _uiManager.CloseWindow<DummyModalWindow2>();

            Assert.IsFalse(_uiManager.IsModalOpen());
            Assert.IsFalse(modalLayer.IsVisible);
            Assert.IsTrue(hubLayer.CanvasGroup.interactable);
            Assert.IsTrue(hubLayer.CanvasGroup.blocksRaycasts);
            Assert.IsTrue(tutorialLayer.CanvasGroup.interactable);
            Assert.IsTrue(tutorialLayer.CanvasGroup.blocksRaycasts);
        }

        [Test]
        public void UIWindow_OpenAndClose_AreIdempotent_AndDoNotDuplicateEvents()
        {
            var window = CreateComponent<DummyHubWindow>();
            int openEvents = 0;
            int closeEvents = 0;

            window.OnOpened += _ => openEvents++;
            window.OnClosed += _ => closeEvents++;

            window.Open();
            window.Open();

            Assert.AreEqual(1, openEvents);
            Assert.IsTrue(window.IsOpen);

            window.Close();
            window.Close();

            Assert.AreEqual(1, closeEvents);
            Assert.IsFalse(window.IsOpen);
        }

        [Test]
        public void UIManager_OpenWindow_WhenAlreadyOpen_DoesNotDuplicateAnalytics()
        {
            var window = CreateComponent<DummyHubWindow>();
            _uiManager.RegisterWindow(window);

            _uiManager.OpenWindow<DummyHubWindow>();
            _uiManager.OpenWindow<DummyHubWindow>();

            Assert.AreEqual(1, _analytics.Events.Count);
        }

        [Test]
        public void GameStateChanged_PausedAndResult_MakeModalLayerVisible_AndHubRunHideModalLayer()
        {
            var modalLayer = CreateComponent<UILayer>("ModalLayer");
            modalLayer.Initialize(UILayerType.Modal);
            modalLayer.SetVisible(false);
            _uiManager.RegisterLayer(modalLayer);

            _stateMachine.TryTransition(GameState.Hub);
            Assert.IsFalse(modalLayer.IsVisible);

            _stateMachine.TryTransition(GameState.Run);
            Assert.IsFalse(modalLayer.IsVisible);

            _stateMachine.TryTransition(GameState.Paused);
            Assert.IsTrue(modalLayer.IsVisible);

            _stateMachine.TryTransition(GameState.Run);
            Assert.IsFalse(modalLayer.IsVisible);

            _stateMachine.TryTransition(GameState.Result);
            Assert.IsTrue(modalLayer.IsVisible);
        }

        [Test]
        public void UnregisterWindow_ActiveModal_RestoresInputBlocking()
        {
            var hubLayer = CreateComponent<UILayer>("HubLayer");
            hubLayer.Initialize(UILayerType.Hub);
            _uiManager.RegisterLayer(hubLayer);

            var modalLayer = CreateComponent<UILayer>("ModalLayer");
            modalLayer.Initialize(UILayerType.Modal);
            _uiManager.RegisterLayer(modalLayer);

            var modal = CreateComponent<DummyModalWindow>();
            _uiManager.RegisterWindow(modal);
            _uiManager.OpenWindow<DummyModalWindow>();

            Assert.IsFalse(hubLayer.CanvasGroup.interactable);
            Assert.IsTrue(modalLayer.IsVisible);

            _uiManager.UnregisterWindow(modal);

            Assert.IsTrue(hubLayer.CanvasGroup.interactable);
            Assert.IsTrue(hubLayer.CanvasGroup.blocksRaycasts);
            Assert.IsFalse(modalLayer.IsVisible);
        }

        [Test]
        public void CloseAllWindows_SupportsAutoUnregisteringWindows_InReverseOrder()
        {
            var win1 = CreateComponent<AutoUnregisterWindow>("AutoWin1");
            win1.SetManager(_uiManager);
            var win2 = CreateComponent<AutoUnregisterWindow>("AutoWin2");
            win2.SetManager(_uiManager);

            _uiManager.RegisterWindow(win1);
            _uiManager.RegisterWindow(win2);

            _uiManager.OpenWindow(win1);
            _uiManager.OpenWindow(win2);

            Assert.DoesNotThrow(() => _uiManager.CloseAllWindows());
            Assert.AreEqual(0, _uiManager.Windows.Count);
        }

        [Test]
        public void OnDestroy_UnsubscribesFromWindowEvents_AndClearsCollections()
        {
            var window = CreateComponent<DummyModalWindow>();
            _uiManager.RegisterWindow(window);

            Object.DestroyImmediate(_uiManager.gameObject);

            Assert.DoesNotThrow(() => window.Open());
        }
    }
}
