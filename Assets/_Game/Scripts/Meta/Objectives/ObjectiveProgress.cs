namespace Game.Meta.Objectives
{
    public class ObjectiveProgress
    {
        public string ObjectiveId { get; }
        public int CurrentValue { get; set; }
        public int TargetValue { get; }
        public bool IsCompleted => CurrentValue >= TargetValue;

        public ObjectiveProgress(string objectiveId, int targetValue, int currentValue = 0)
        {
            ObjectiveId = objectiveId;
            TargetValue = targetValue;
            CurrentValue = currentValue;
        }
    }
}
