namespace Game.Meta.Barracks.Events
{
    public readonly struct BuildingUpgradeCompletedEvent
    {
        public BarracksBuildingType BuildingType { get; }
        public int NewLevel { get; }

        public BuildingUpgradeCompletedEvent(BarracksBuildingType type, int newLevel)
        {
            BuildingType = type;
            NewLevel = newLevel;
        }
    }
}
