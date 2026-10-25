using System;
using Game.Core.Analytics;

namespace Game.Gameplay.Tutorial
{
    public class TutorialGuideController
    {
        private readonly FtueStepProgress _progress;
        private readonly IAnalyticsService _analytics;
        private string _activeStepId;
        private int _activeStageIndex;

        public FtueStepProgress Progress => _progress;
        public string ActiveStepId => _activeStepId;
        public int ActiveStageIndex => _activeStageIndex;
        public bool HasActiveStep => !string.IsNullOrEmpty(_activeStepId);

        public event Action<string, string> OnShowStep;
        public event Action OnHideStep;
        public event Action<string> OnStepStarted;
        public event Action<string> OnStepCompleted;

        public TutorialGuideController(FtueStepProgress progress = null, IAnalyticsService analytics = null)
        {
            _progress = progress ?? new FtueStepProgress();
            _analytics = analytics;
        }

        public void CheckTutorialStep(int stageIndex)
        {
            string step = _progress.GetTutorialStepForStage(stageIndex);
            if (string.IsNullOrEmpty(step))
            {
                if (HasActiveStep)
                {
                    DismissActiveStep();
                }

                return;
            }

            if (_activeStepId == step)
            {
                return;
            }

            _activeStepId = step;
            _activeStageIndex = stageIndex;
            _analytics?.TrackFtueStep(step, stageIndex, "started");
            OnStepStarted?.Invoke(step);

            string instruction = GetDefaultInstructionForStep(step);
            OnShowStep?.Invoke(step, instruction);
        }

        public void CompleteTutorialStep(string stepId, int stageIndex)
        {
            if (string.IsNullOrEmpty(stepId))
            {
                return;
            }

            if (_progress.IsStepCompleted(stepId))
            {
                return;
            }

            _progress.CompleteStep(stepId);
            _analytics?.TrackFtueStep(stepId, stageIndex, "completed");
            OnStepCompleted?.Invoke(stepId);

            if (_activeStepId == stepId)
            {
                DismissActiveStep();
            }
        }

        public void DismissActiveStep()
        {
            _activeStepId = null;
            _activeStageIndex = 0;
            OnHideStep?.Invoke();
        }

        public static string GetDefaultInstructionForStep(string stepId)
        {
            return stepId switch
            {
                FtueStepProgress.SwipeStep => "Deslize para mover a tropa",
                FtueStepProgress.GateStep => "Passe pelos portões azuis para multiplicar a tropa",
                FtueStepProgress.CageStep => "Destrua a jaula para resgatar aliados",
                _ => string.Empty
            };
        }
    }
}
