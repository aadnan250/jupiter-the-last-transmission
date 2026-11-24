using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Generates a placeholder equirectangular Jupiter texture: latitude cloud bands with turbulence
    /// and a Great Red Spot in the southern hemisphere (~22° S). Replaced by public-domain mission imagery
    /// in the art pass. Deterministic output.
    /// </summary>
    public static class JupiterTextureGenerator
    {
        public const string TexturePath = "Assets/_Project/Art/Textures/Generated/jupiter_placeholder.png";
        const int Width = 1024, Height = 512;

        public static Texture2D Generate()
        {
            GreyboxKit.EnsureFolder("Assets/_Project/Art/Textures/Generated");

            Color[] palette =
            {
                new Color(0.95f, 0.91f, 0.82f), // pale zone
                new Color(0.76f, 0.58f, 0.40f), // tan belt
                new Color(0.52f, 0.34f, 0.22f), // brown belt
                new Color(0.93f, 0.85f, 0.70f), // cream
                new Color(0.66f, 0.48f, 0.34f),
            };

            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var px = new Color32[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                float lat = (y / (float)(Height - 1)) * 180f - 90f; // -90 (south) .. 90 (north)
                for (int x = 0; x < Width; x++)
                {
                    float lon = x / (float)Width * 360f;
                    float u = x / (float)Width;

                    // Turbulence shifts the band lookup so edges swirl instead of being straight lines.
                    float turb = (Mathf.PerlinNoise(u * 24f, y * 0.06f) - 0.5f) * 5f
                               + (Mathf.PerlinNoise(u * 70f + 3f, y * 0.2f) - 0.5f) * 1.5f;
                    float b = (lat + turb) / 9f;
                    int i0 = Mod(Mathf.FloorToInt(b), palette.Length);
                    int i1 = (i0 + 1) % palette.Length;
                    Color c = Color.Lerp(palette[i0], palette[i1], Mathf.SmoothStep(0f, 1f, b - Mathf.Floor(b)));

                    // Darker poles.
                    c *= Mathf.Lerp(1f, 0.72f, Mathf.Pow(Mathf.Abs(lat) / 90f, 3f));

                    // Great Red Spot: an ellipse ~22° S.
                    float dx = Mathf.DeltaAngle(lon, 200f) / 13f;
                    float dy = (lat + 22f) / 6.5f;
                    float d = dx * dx + dy * dy;
                    if (d < 1f)
                    {
                        float k = Mathf.SmoothStep(1f, 0f, d);
                        Color spot = Color.Lerp(new Color(0.78f, 0.42f, 0.30f), new Color(0.66f, 0.30f, 0.22f), Mathf.PerlinNoise(dx * 3f, dy * 3f));
                        c = Color.Lerp(c, spot, k);
                    }

                    px[y * Width + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            string full = Path.GetFullPath(TexturePath);
            if (!File.Exists(full) || !File.ReadAllBytes(full).AsSpan().SequenceEqual(png))
                File.WriteAllBytes(full, png);
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(TexturePath) is TextureImporter importer)
            {
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
