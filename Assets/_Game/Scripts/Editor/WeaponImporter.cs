#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Data;

namespace Game.Editor
{
    public static class WeaponImporter
    {
        public const string DefaultSourcePath = "Content/Source/Weapons";
        public const string DefaultTargetPath = "Assets/_Game/Data/Weapons";
        public const string CatalogAssetName = "WeaponCatalog";

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            throw new NotImplementedException();
        }

        public static WeaponCatalog LoadCatalog(string targetFolder = null)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
