using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Import rules for generated crew characters (Assets/_Project/Art/Characters):
    /// skeleton kept as plain transforms for procedural animation, no cameras/lights/blend shapes,
    /// and textures capped for integrated graphics. Hair, eyebrow and eyelash textures keep their alpha
    /// (they are textured cards, not solid geometry).
    /// </summary>
    public class CharacterAssetPostprocessor : AssetPostprocessor
    {
        const string Folder = CharacterModelImporter.CharactersFolder;

        static readonly string[] AlphaCardNames = { "hair", "short0", "bob0", "ponytail", "braid", "long0", "afro", "eyebrow", "eyelash" };

        static bool InCharacters(string path) => path.Replace('\\', '/').StartsWith(Folder);

        void OnPreprocessModel()
        {
            if (!InCharacters(assetPath)) return;
            var m = (ModelImporter)assetImporter;
            m.animationType = ModelImporterAnimationType.Generic;
            m.importAnimation = false;
            m.importBlendShapes = false;
            m.importCameras = false;
            m.importLights = false;
            m.importVisibility = false;
            m.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            m.useFileScale = true;
            m.optimizeGameObjects = false; // bones must stay as transforms for lip sync and idle motion
        }

        void OnPreprocessTexture()
        {
            if (!InCharacters(assetPath)) return;
            var t = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            bool skin = name.Contains("skin") || name.Contains("_diffuse") && (name.Contains("male") || name.Contains("female"));
            t.maxTextureSize = skin ? 2048 : 1024;
            t.textureCompression = TextureImporterCompression.Compressed;
            t.mipmapEnabled = true;
            if (IsAlphaCard(name)) t.alphaIsTransparency = true;
        }

        // Materials are built from the export manifest by CharacterMaterialBuilder and remapped onto the model.

        static bool IsAlphaCard(string name)
        {
            foreach (var n in AlphaCardNames)
                if (name.Contains(n)) return true;
            return false;
        }
    }
}
