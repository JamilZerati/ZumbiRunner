using System.Collections.Generic;
using Game.Core;
using Game.Presentation.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.UI
{
    public class PauseViewTests
    {
        private readonly List<GameObject> _createdObjects = new();

        private EventBus _eventBus;
        private GameStateMachine _stateMachine;
        private UIManager _uiManager;
        private PauseView _pauseView;
        private PauseController _pauseController;
        private OptionsView _optionsView;

        private Button _resumeButton;
        private Button _restartButton;
        private Button _optionsButton;
        private Button _quitButton;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1.0f;

            _eventBus = new EventBus();
            _stateMachine = new GameStateMachine(_eventBus, GameState.Boot);

            var uiManagerGo = CreateGameObject("UIManager");
            _uiManager = uiManagerGo.AddComponent<UIManager>();
            _uiManager.Initialize(_stateMachine, eventBus: _eventBus);

            var modalLayerGo = CreateGameObject("ModalLayer");
            modalLayerGo.transform.SetParent(uiManagerGo.transform);
            var modalLayer = modalLayerGo.AddComponent<UILayer>();
            modalLayer.Initialize(UILayerType.Modal);
            _uiManager.RegisterLayer(modalLayer);

            var pauseGo = CreateGameObject("PauseView");
            pauseGo.transform.SetParent(modalLayerGo.transform);
            _pauseView = pauseGo.AddComponent<PauseView>();

            _resumeButton = CreateComponent<Button>("ResumeButton");
            _restartButton = CreateComponent<Button>("RestartButton");
            _optionsButton = CreateComponent<Button>("OptionsButton");
            _quitButton = CreateComponent<Button>("QuitButton");

            _pauseView.SetupControls(
                resume: _resumeButton,
                restart: _restartButton,
                options: _optionsButton,
                quit: _quitButton);

            var optionsGo = CreateGameObject("OptionsView");
            optionsGo.transform.SetParent(modalLayerGo.transform);
            _optionsView = optionsGo.AddComponent<OptionsView>();

            _uiManager.RegisterWindow(_pauseView);
            _uiManager.RegisterWindow(_optionsView);

            var controllerGo = CreateGameObject("PauseController");
            _pauseController = controllerGo.AddComponent<PauseController>();
            _pauseController.Initialize(_stateMachine, _uiManager, _pauseView, _optionsView);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1.0f;

            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        private T CreateComponent<T>(string name = null) where T : Component
        {
            var go = CreateGameObject(name ?? typeof(T).Name);
            return go.AddComponent<T>();
        }

        [Test]
        public void LayerType_IsModal()
        {
            Assert.AreEqual(UILayerType.Modal, _pauseView.LayerType);
        }

        [Test]
        public void PauseGame_WhenInRunState_TransitionsToPaused_FreezesSimulationTime_AndOpensPauseView()
        {
            _stateMachine.TryTransition(GameState.Run);
            Assert.AreEqual(GameState.Run, _stateMachine.CurrentState);

            _pauseController.PauseGame();

            Assert.AreEqual(GameState.Paused, _stateMachine.CurrentState);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(_pauseView.IsOpen);
            Assert.IsTrue(_pauseView.gameObject.activeSelf);
        }

        [Test]
        public void PauseGame_WhenNotInRunState_DoesNotTransitionOrFreezeSimulationTime()
        {
            _stateMachine.TryTransition(GameState.Hub);
            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);

            _pauseController.PauseGame();

            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale);
            Assert.IsFalse(_pauseView.IsOpen);
        }

        [Test]
        public void PauseGame_WhenAlreadyPaused_RemainsPausedSafely()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();
            Assert.AreEqual(GameState.Paused, _stateMachine.CurrentState);
            Assert.AreEqual(0f, Time.timeScale);

            _pauseController.PauseGame();

            Assert.AreEqual(GameState.Paused, _stateMachine.CurrentState);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(_pauseView.IsOpen);
        }

        [Test]
        public void ResumeGame_ClosesPauseView_RestoresTimeScaleToOne_AndTransitionsToRun()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();
            Assert.AreEqual(0f, Time.timeScale);

            _pauseController.ResumeGame();

            Assert.AreEqual(GameState.Run, _stateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale);
            Assert.IsFalse(_pauseView.IsOpen);
        }

        [Test]
        public void ResumeButton_Click_InvokesResumeGame()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();

            _resumeButton.onClick.Invoke();

            Assert.AreEqual(GameState.Run, _stateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale);
            Assert.IsFalse(_pauseView.IsOpen);
        }

        [Test]
        public void RestartRun_ClosesPauseView_RestoresTimeScale_TransitionsToRun_AndInvokesCallback()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();

            var restartInvoked = false;
            _pauseController.OnRestartRequested += () => restartInvoked = true;

            _pauseController.RestartRun();

            Assert.AreEqual(GameState.Run, _stateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale);
            Assert.IsFalse(_pauseView.IsOpen);
            Assert.IsTrue(restartInvoked);
        }

        [Test]
        public void RestartButton_Click_InvokesRestartRun()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();

            var restartInvoked = false;
            _pauseController.OnRestartRequested += () => restartInvoked = true;

            _restartButton.onClick.Invoke();

            Assert.AreEqual(GameState.Run, _stateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale);
            Assert.IsFalse(_pauseView.IsOpen);
            Assert.IsTrue(restartInvoked);
        }

        [Test]
        public void OpenOptions_OpensOptionsViewModalViaUiManager()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();

            _pauseController.OpenOptions();

            Assert.IsTrue(_uiManager.IsWindowOpen<OptionsView>());
        }

        [Test]
        public void OptionsButton_Click_OpensOptionsViewModal()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();

            _optionsButton.onClick.Invoke();

            Assert.IsTrue(_uiManager.IsWindowOpen<OptionsView>());
        }

        [Test]
        public void QuitToHub_ClosesPauseView_RestoresTimeScaleInconditionally_AndTransitionsToHub()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();

            var quitInvoked = false;
            _pauseController.OnQuitToHubRequested += () => quitInvoked = true;

            _pauseController.QuitToHub();

            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale);
            Assert.IsFalse(_pauseView.IsOpen);
            Assert.IsTrue(quitInvoked);
        }

        [Test]
        public void QuitButton_Click_InvokesQuitToHub()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();

            _quitButton.onClick.Invoke();

            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale);
            Assert.IsFalse(_pauseView.IsOpen);
        }

        [Test]
        public void OnDestroy_PauseView_WhenDestroyedWhilePaused_InconditionallyRestoresTimeScale()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();
            Assert.AreEqual(0f, Time.timeScale);

            typeof(PauseView)
                .GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                ?.Invoke(_pauseView, null);
            Object.DestroyImmediate(_pauseView.gameObject);

            Assert.AreEqual(1.0f, Time.timeScale);
        }

        [Test]
        public void OnDestroy_PauseController_WhenDestroyedWhilePaused_InconditionallyRestoresTimeScale()
        {
            _stateMachine.TryTransition(GameState.Run);
            _pauseController.PauseGame();
            Assert.AreEqual(0f, Time.timeScale);

            typeof(PauseController)
                .GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                ?.Invoke(_pauseController, null);
            Object.DestroyImmediate(_pauseController.gameObject);

            Assert.AreEqual(1.0f, Time.timeScale);
        }
    }
}
