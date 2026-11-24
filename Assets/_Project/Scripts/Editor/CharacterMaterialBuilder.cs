using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Builds URP materials for each generated character from the materials.json manifest written by
    /// Tools/CharacterPipeline/make_crew.py (MPFB's Blender node materials do not survive FBX export),
    /// then remaps the FBX's materials to them so every instance of the model uses them.
    /// Hair, eyebrows and eyelashes are alpha-tested and double-sided; skin and cloth are soft, not glossy.
    /// </summary>
    public static class CharacterMaterialBuilder
    {
        static readonly Regex Entry = new Regex("\"(?<mat>[^\"]+)\"\\s*:\\s*\\{[^}]*?\"texture\"\\s*:\\s*(\"(?<tex>[^\"]+)\"|null)", RegexOptions.Singleline);
        static readonly string[] AlphaCards = { "short0", "bob0", "ponytail", "braid", "long0", "afro", "eyebrow", "eyelash" };
        static readonly string[] HairCards = { "short0", "bob0", "ponytail", "braid", "long0", "afro", "eyebrow" };

        /// <summary>Hair colour per character, multiplied over the (light) hair textures. Eyebrows match the hair.</summary>
        static readonly System.Collections.Generic.Dictionary<string, Color> HairTint = new System.Collections.Generic.Dictionary<string, Color>
        {
            { "sam", new Color(0.16f, 0.14f, 0.13f) },     // black
            { "hana", new Color(0.11f, 0.10f, 0.10f) },    // black
            { "luca", new Color(0.32f, 0.22f, 0.16f) },    // dark brown
            { "ashford", new Color(0.78f, 0.76f, 0.74f) }, // greying
            { "mara", new Color(1.0f, 0.92f, 0.78f) },     // ash blonde
        };

        [MenuItem("Jupiter/Build Character Materials")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(CharacterModelImporter.CharactersFolder)) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            foreach (string dir in AssetDatabase.GetSubFolders(CharacterModelImporter.CharactersFolder))
            {
                string id = Path.GetFileName(dir);
                string fbx = $"{dir}/{id}.fbx";
                string manifest = Path.Combine(dir, "materials.json");
                if (!File.Exists(fbx) || !File.Exists(manifest)) continue;

                GreyboxKit.EnsureFolder($"{dir}/Materials");
                var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
                bool changed = false;

                foreach (Match m in Entry.Matches(File.ReadAllText(manifest)))
                {
                    string matName = m.Groups["mat"].Value;
                    string texName = m.Groups["tex"].Success ? m.Groups["tex"].Value : null;
                    string matPath = $"{dir}/Materials/{matName}.mat";

                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null)
                    {
                        mat = new Material(lit);
                        AssetDatabase.CreateAsset(mat, matPath);
                    }
                    Configure(mat, matName.ToLowerInvariant(),
                        texName != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/Textures/{texName}") : null);
                    if (HairTint.TryGetValue(id, out var tint) && IsAny(matName.ToLowerInvariant(), HairCards))
                        mat.SetColor("_BaseColor", tint);
                    EditorUtility.SetDirty(mat);

                    var source = new AssetImporter.SourceAssetIdentifier(typeof(Material), matName);
                    if (!importer.GetExternalObjectMap().TryGetValue(source, out var existing) || existing != mat)
                    {
                        importer.AddRemap(source, mat);
                        changed = true;
                    }
                }

                AssetDatabase.SaveAssets();
                if (changed) importer.SaveAndReimport();
            }
        }

        static void Configure(Material mat, string name, Texture2D texture)
        {
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0f);

            bool card = false;
            foreach (var a in AlphaCards) card |= name.Contains(a);

            if (card)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.4f);
                mat.SetKeyword(new LocalKeyword(mat.shader, "_ALPHATEST_ON"), true);
                mat.SetFloat("_Cull", (float)CullMode.Off);
                mat.renderQueue = (int)RenderQueue.AlphaTest;
                mat.SetFloat("_Smoothness", 0.3f);
                if (texture != null) SetAlphaFromTexture(texture);
            }
            else
            {
                mat.SetFloat("_AlphaClip", 0f);
                mat.SetKeyword(new LocalKeyword(mat.shader, "_ALPHATEST_ON"), false);
                mat.SetFloat("_Cull", (float)CullMode.Back);
                mat.renderQueue = -1;
                bool eye = name.EndsWith("low-poly") || name.Contains("eye.") || name.EndsWith("high-poly");
                bool wet = eye || name.Contains("teeth") || name.Contains("tongue");
                mat.SetFloat("_Smoothness", wet ? 0.75f : 0.28f);
            }
        }

        static bool IsAny(string name, string[] parts)
        {
            foreach (var p in parts) if (name.Contains(p)) return true;
            return false;
        }

        static void SetAlphaFromTexture(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && !ti.alphaIsTransparency)
            {
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
        }
    }
}
