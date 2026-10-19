namespace Game.Meta.Objectives
{
    public class ObjectiveDefinition
    {
        public string Id { get; }
        public ObjectiveScope Scope { get; }
        public string Metric { get; }
        public int TargetValue { get; }
        public string Filter { get; }

        public ObjectiveDefinition(string id, ObjectiveScope scope, string metric, int targetValue, string filter = null)
        {
            Id = id;
            Scope = scope;
            Metric = metric;
            TargetValue = targetValue;
            Filter = filter;
        }
    }
}
