using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public class SpriteImportRules : AssetPostprocessor
    {
        public static readonly string[] SpriteFolders =
        {
            "Assets/_Game/Art/UI/",
            "Assets/_Game/Art/VFX/",
        };

        private void OnPreprocessTexture()
        {
            if (!SpriteFolders.Any(folder => assetPath.StartsWith(folder)))
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
