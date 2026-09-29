using System;
using System.Collections.Generic;
using Game.Data;

namespace Game.Editor
{
    public static class StatusContentImporter
    {
        public const string StatusSourcePath = "Content/Source/Statuses";
        public const string InteractionSourcePath = "Content/Source/Interactions";
        public const string DefaultTargetPath = "Assets/_Game/Data/Statuses";
        public const string CatalogAssetName = "StatusCatalog";
        public const string InteractionTableAssetName = "EffectInteractionTable";

        public static int ImportStatuses(string sourceFolder = null, string targetFolder = null, List<string> errors = null) => throw new NotImplementedException();
        public static int ImportInteractions(string sourceFolder = null, string targetFolder = null, List<string> errors = null) => throw new NotImplementedException();
        public static StatusCatalog LoadStatusCatalog(string targetFolder = null) => throw new NotImplementedException();
        public static EffectInteractionTable LoadInteractionTable(string targetFolder = null) => throw new NotImplementedException();
    }
}
