using Game.Gameplay.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class TutorialGuideView : UIWindow
    {
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private GameObject _bannerRoot;
        [SerializeField] private Image _stepIcon;

        private TutorialGuideController _controller;
        private string _currentStepId;
        private string _currentMessage;

        public override UILayerType LayerType => UILayerType.TutorialOverlay;

        public TMP_Text InstructionText => _instructionText;
        public GameObject BannerRoot => _bannerRoot;
        public Image StepIcon => _stepIcon;
        public string CurrentStepId => _currentStepId;
        public string CurrentMessage => _currentMessage;
        public TutorialGuideController Controller => _controller;

        protected override void Awake()
        {
            SetLayerType(UILayerType.TutorialOverlay);
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnbindController();
        }

        public void BindComponents(TMP_Text instructionText = null, GameObject bannerRoot = null, Image stepIcon = null)
        {
            if (instructionText != null) _instructionText = instructionText;
            if (bannerRoot != null) _bannerRoot = bannerRoot;
            if (stepIcon != null) _stepIcon = stepIcon;
        }

        public void Initialize(TutorialGuideController controller)
        {
            UnbindController();
            _controller = controller;

            if (_controller != null)
            {
                _controller.OnShowStep += ShowStep;
                _controller.OnHideStep += HideStep;

                if (_controller.HasActiveStep)
                {
                    ShowStep(_controller.ActiveStepId, TutorialGuideController.GetDefaultInstructionForStep(_controller.ActiveStepId));
                }
            }
        }

        public void UnbindController()
        {
            if (_controller != null)
            {
                _controller.OnShowStep -= ShowStep;
                _controller.OnHideStep -= HideStep;
                _controller = null;
            }
        }

        public void ShowStep(string stepId, string message)
        {
            _currentStepId = stepId;
            _currentMessage = message;

            if (_instructionText != null)
            {
                _instructionText.text = message ?? string.Empty;
            }

            if (_bannerRoot != null)
            {
                _bannerRoot.SetActive(true);
            }

            Open();
        }

        public void HideStep()
        {
            _currentStepId = null;
            _currentMessage = null;

            if (_bannerRoot != null)
            {
                _bannerRoot.SetActive(false);
            }

            Close();
        }
    }
}
