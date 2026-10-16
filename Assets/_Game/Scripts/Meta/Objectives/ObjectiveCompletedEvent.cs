namespace Game.Meta.Objectives
{
    public readonly struct ObjectiveCompletedEvent
    {
        public string ObjectiveId { get; }

        public ObjectiveCompletedEvent(string objectiveId)
        {
            ObjectiveId = objectiveId;
        }
    }
}
