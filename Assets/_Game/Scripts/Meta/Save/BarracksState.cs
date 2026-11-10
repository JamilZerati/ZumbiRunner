using System.Collections.Generic;

namespace Game.Meta.Save
{
    public class BarracksState
    {
        public Dictionary<string, int> BuildingLevels { get; set; } = new Dictionary<string, int>
        {
            ["Recruitment"] = 1,
            ["Armory"] = 1,
            ["Canteen"] = 1,
            ["Infirmary"] = 1,
            ["Ammunition"] = 1
        };

        public string ActiveConstructionType { get; set; }
        public long ConstructionEndUtcTicks { get; set; }
        public int TargetLevel { get; set; }
        public int AdSpeedupsUsedToday { get; set; }
        public long LastAdUsageDateUtcTicks { get; set; }
        public long LastCanteenCollectionUtcTicks { get; set; }

        public int GetLevel(string buildingType)
        {
            return BuildingLevels != null && BuildingLevels.TryGetValue(buildingType, out int lv) ? lv : 1;
        }

        public void SetLevel(string buildingType, int level)
        {
            if (BuildingLevels == null)
            {
                BuildingLevels = new Dictionary<string, int>();
            }
            BuildingLevels[buildingType] = level;
        }
    }
}
