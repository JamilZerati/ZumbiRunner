using System.Collections.Generic;

namespace Game.Gameplay.Tutorial
{
    public class FtueStepProgress
    {
        public const string SwipeStep = "swipe";
        public const string GateStep = "gate";
        public const string CageStep = "cage";

        private readonly HashSet<string> _completedSteps = new();

        public IReadOnlyCollection<string> CompletedSteps => _completedSteps;

        public FtueStepProgress(IEnumerable<string> initialCompletedSteps = null)
        {
            if (initialCompletedSteps != null)
            {
                foreach (var step in initialCompletedSteps)
                {
                    if (!string.IsNullOrEmpty(step))
                    {
                        _completedSteps.Add(step);
                    }
                }
            }
        }

        public bool IsStepCompleted(string stepName)
        {
            if (string.IsNullOrEmpty(stepName))
            {
                return false;
            }

            return _completedSteps.Contains(stepName);
        }

        public bool CompleteStep(string stepName)
        {
            if (string.IsNullOrEmpty(stepName))
            {
                return false;
            }

            return _completedSteps.Add(stepName);
        }

        public string GetTutorialStepForStage(int stageIndex)
        {
            string step = stageIndex switch
            {
                1 => SwipeStep,
                2 => GateStep,
                3 => CageStep,
                _ => null
            };

            if (step != null && _completedSteps.Contains(step))
            {
                return null;
            }

            return step;
        }
    }
}
