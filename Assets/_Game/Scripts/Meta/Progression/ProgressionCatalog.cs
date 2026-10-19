using System;
using System.Collections.Generic;

namespace Game.Meta.Progression
{
    public class ProgressionCatalog
    {
        private readonly Dictionary<string, ContentUnlockDefinition> _unlocksByLevel = new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<ContentUnlockDefinition> AllUnlocks => _unlocksByLevel.Values;

        public void RegisterUnlock(string levelId, string contentId, ContentUnlockType unlockType)
        {
            var def = new ContentUnlockDefinition(levelId, contentId, unlockType);
            _unlocksByLevel[levelId] = def;
        }

        public bool TryGetUnlock(string levelId, out ContentUnlockDefinition definition)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                definition = null;
                return false;
            }
            return _unlocksByLevel.TryGetValue(levelId, out definition);
        }

        public static ProgressionCatalog CreateDefaultCatalog()
        {
            var catalog = new ProgressionCatalog();
            catalog.RegisterUnlock("level_05_boss", "weapon:shotgun", ContentUnlockType.Weapon);
            catalog.RegisterUnlock("level_10_boss", "hero:pyromancer", ContentUnlockType.Hero);
            catalog.RegisterUnlock("level_15_boss", "world:world_02", ContentUnlockType.World);
            return catalog;
        }
    }
}
