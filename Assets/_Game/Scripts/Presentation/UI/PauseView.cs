using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class PauseView : UIWindow
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;

        private PauseController _controller;

        public override UILayerType LayerType => UILayerType.Modal;

        public Button ResumeButton => resumeButton;
        public Button RestartButton => restartButton;
        public Button OptionsButton => optionsButton;
        public Button QuitButton => quitButton;
        public PauseController Controller => _controller;

        public event Action OnResumeClicked;
        public event Action OnRestartClicked;
        public event Action OnOptionsClicked;
        public event Action OnQuitClicked;

        protected override void Awake()
        {
            base.Awake();
            WireListeners();
        }

        protected override void OnDestroy()
        {
            // Invariante de seguranca: nunca permitir que timeScale permaneca congelado se a janela for destruida
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1.0f;
            }

            UnwireListeners();
            base.OnDestroy();
        }

        public void Initialize(PauseController controller)
        {
            _controller = controller;
        }

        public void SetupControls(
            Button resume = null,
            Button restart = null,
            Button options = null,
            Button quit = null)
        {
            UnwireListeners();

            if (resume != null) resumeButton = resume;
            if (restart != null) restartButton = restart;
            if (options != null) optionsButton = options;
            if (quit != null) quitButton = quit;

            WireListeners();
        }

        private void WireListeners()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(HandleResumeClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (optionsButton != null)
            {
                optionsButton.onClick.AddListener(HandleOptionsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(HandleQuitClicked);
            }
        }

        private void UnwireListeners()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (optionsButton != null)
            {
                optionsButton.onClick.RemoveListener(HandleOptionsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(HandleQuitClicked);
            }
        }

        private void HandleResumeClicked()
        {
            OnResumeClicked?.Invoke();
            _controller?.ResumeGame();
        }

        private void HandleRestartClicked()
        {
            OnRestartClicked?.Invoke();
            _controller?.RestartRun();
        }

        private void HandleOptionsClicked()
        {
            OnOptionsClicked?.Invoke();
            _controller?.OpenOptions();
        }

        private void HandleQuitClicked()
        {
            OnQuitClicked?.Invoke();
            _controller?.QuitToHub();
        }
    }
}
