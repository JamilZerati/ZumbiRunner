using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public class UiSpriteImportRules : AssetPostprocessor
    {
        public const string UiArtFolder = "Assets/_Game/Art/UI/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(UiArtFolder))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
