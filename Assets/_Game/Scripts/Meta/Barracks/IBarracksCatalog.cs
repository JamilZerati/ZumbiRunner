using System.Collections.Generic;

namespace Game.Meta.Barracks
{
    public interface IBarracksCatalog
    {
        BarracksBuildingDefinition Get(BarracksBuildingType type);
        IReadOnlyCollection<BarracksBuildingDefinition> GetAll();
    }
}
