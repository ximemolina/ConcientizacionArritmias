using UnityEditor;
using UnityEngine;

public class SpriteSingleModeImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // Solo aplicar a texturas dentro de tu carpeta de sprites
        if (!assetPath.Contains("Assets/IMG_Tuto/")) // cambia esto a tu carpeta
            return;

        TextureImporter textureImporter = (TextureImporter)assetImporter;

        if (textureImporter.importSettingsMissing || textureImporter.textureType != TextureImporterType.Sprite)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.alphaIsTransparency = true;
        }
    }
}
