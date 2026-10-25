using System;
using Game.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Presentation.UI
{
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private UIManager uiManager;
        [SerializeField] private PauseView pauseView;
        [SerializeField] private OptionsView optionsView;

        private IGameStateMachine _stateMachine;
        private UIManager _uiManager;
        private PauseView _pauseView;
        private OptionsView _optionsView;

        public IGameStateMachine StateMachine => _stateMachine;
        public UIManager UiManager => _uiManager;
        public PauseView PauseView => _pauseView;
        public bool IsPaused => _stateMachine != null ? _stateMachine.CurrentState == GameState.Paused : Time.timeScale == 0f;

        public event Action OnRestartRequested;
        public event Action OnQuitToHubRequested;

        private void Awake()
        {
            if (uiManager == null)
            {
                uiManager = GetComponentInParent<UIManager>() ?? FindFirstObjectByType<UIManager>();
            }

            if (pauseView == null)
            {
                pauseView = GetComponentInChildren<PauseView>(true);
            }

            if (pauseView != null && _pauseView == null)
            {
                _pauseView = pauseView;
                _pauseView.Initialize(this);
            }
        }

        private void OnDestroy()
        {
            // Invariante de seguranca: restaurar simulacao caso o controlador seja destruido com o jogo pausado
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1.0f;
            }
        }

        public void Initialize(
            IGameStateMachine stateMachine,
            UIManager manager = null,
            PauseView view = null,
            OptionsView options = null)
        {
            _stateMachine = stateMachine;
            _uiManager = manager != null ? manager : uiManager;
            _pauseView = view != null ? view : pauseView;
            _optionsView = options != null ? options : optionsView;

            if (_pauseView == null && _uiManager != null)
            {
                _pauseView = _uiManager.GetWindow<PauseView>();
            }

            if (_optionsView == null && _uiManager != null)
            {
                _optionsView = _uiManager.GetWindow<OptionsView>();
            }

            if (_pauseView != null)
            {
                _pauseView.Initialize(this);
            }
        }

        public void PauseGame()
        {
            if (_stateMachine != null)
            {
                if (_stateMachine.CurrentState == GameState.Paused)
                {
                    return;
                }

                if (!_stateMachine.TryTransition(GameState.Paused))
                {
                    return;
                }
            }

            Time.timeScale = 0f;

            if (_uiManager != null)
            {
                if (_pauseView != null)
                {
                    _uiManager.OpenWindow(_pauseView);
                }
                else
                {
                    _uiManager.OpenWindow<PauseView>();
                }
            }
            else if (_pauseView != null)
            {
                _pauseView.Open();
            }
        }

        public void ResumeGame()
        {
            ClosePauseView();
            Time.timeScale = 1.0f;
            _stateMachine?.TryTransition(GameState.Run);
        }

        public void RestartRun()
        {
            ClosePauseView();
            Time.timeScale = 1.0f;
            _stateMachine?.TryTransition(GameState.Run);

            if (OnRestartRequested != null)
            {
                OnRestartRequested.Invoke();
            }
            else if (Application.isPlaying)
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.isLoaded && !string.IsNullOrEmpty(activeScene.name))
                {
                    SceneManager.LoadScene(activeScene.name);
                }
            }
        }

        public void OpenOptions()
        {
            if (_uiManager != null)
            {
                _uiManager.OpenWindow<OptionsView>();
            }
            else if (_optionsView != null)
            {
                _optionsView.Open();
            }
        }

        public void QuitToHub()
        {
            try
            {
                ClosePauseView();
                _stateMachine?.TryTransition(GameState.Hub);
                OnQuitToHubRequested?.Invoke();
            }
            finally
            {
                Time.timeScale = 1.0f;
            }
        }

        private void ClosePauseView()
        {
            if (_uiManager != null)
            {
                if (_pauseView != null)
                {
                    _uiManager.CloseWindow(_pauseView);
                }
                else
                {
                    _uiManager.CloseWindow<PauseView>();
                }
            }
            else if (_pauseView != null)
            {
                _pauseView.Close();
            }
        }
    }
}
