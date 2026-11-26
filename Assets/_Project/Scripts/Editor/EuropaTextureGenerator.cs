using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Procedural textures for the Europa surface (deterministic, original):
    /// a panoramic night sky (thousands of stars and a dusty Milky Way band with dark rifts) and Europa's ice
    /// (blue-white, cracked, crossed by the reddish-brown "lineae" seen in Galileo images), with a normal map.
    /// </summary>
    public static class EuropaTextureGenerator
    {
        public const string Folder = "Assets/_Project/Art/Textures/Europa";
        public const string Sky = Folder + "/europa_sky.png";
        public const string IceAlbedo = Folder + "/europa_ice_albedo.png";
        public const string IceNormal = Folder + "/europa_ice_normal.png";

        public static void Generate()
        {
            GreyboxKit.EnsureFolder(Folder);
            WriteSky();
            WriteIce();
            AssetDatabase.Refresh();
            Configure(Sky, false, 4096, TextureWrapMode.Clamp);
            Configure(IceAlbedo, false, 1024, TextureWrapMode.Repeat);
            Configure(IceNormal, true, 1024, TextureWrapMode.Repeat);
        }

        static void WriteSky()
        {
            const int w = 4096, h = 2048;
            var px = new Color[w * h];
            var rng = new System.Random(2183);
            // Milky Way: a great circle tilted against the horizon.
            Vector3 bandNormal = Quaternion.Euler(62f, 0f, 28f) * Vector3.up;

            for (int y = 0; y < h; y++)
            {
                float lat = (y / (float)(h - 1) - 0.5f) * Mathf.PI;
                for (int x = 0; x < w; x++)
                {
                    float lon = x / (float)w * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                    float band = Mathf.Abs(Vector3.Dot(dir, bandNormal));
                    float glow = Mathf.Exp(-band * band * 38f);
                    float dust = Mathf.PerlinNoise(x * 0.004f, y * 0.008f) * 0.7f + Mathf.PerlinNoise(x * 0.015f, y * 0.03f) * 0.3f;
                    float rift = Mathf.Clamp01(Mathf.PerlinNoise(x * 0.006f + 31f, y * 0.012f) * 1.6f - 0.55f);
                    float m = glow * Mathf.Lerp(0.35f, 1f, dust) * (1f - 0.75f * rift);
                    // Core brighter and warmer on one side of the band.
                    float core = Mathf.Exp(-Mathf.Pow((lon - 4.1f) * 1.3f, 2f)) * glow;
                    Color sky = new Color(0.004f, 0.005f, 0.012f)
                                + new Color(0.16f, 0.18f, 0.24f) * m
                                + new Color(0.22f, 0.16f, 0.1f) * core * 0.8f;
                    px[y * w + x] = sky;
                }
            }

            // Stars: many faint, few bright; denser along the band.
            for (int i = 0; i < 26000; i++)
            {
                int x = rng.Next(w), y = rng.Next(h);
                float lat = (y / (float)(h - 1) - 0.5f) * Mathf.PI, lon = x / (float)w * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                float band = Mathf.Abs(Vector3.Dot(dir, bandNormal));
                if (rng.NextDouble() > 0.35 + 0.65 * Mathf.Exp(-band * band * 20f)) continue;
                float b = Mathf.Pow((float)rng.NextDouble(), 6f) * 2.2f + 0.08f;
                double tint = rng.NextDouble();
                Color c = tint < 0.15 ? new Color(1f, 0.8f, 0.62f) : tint < 0.3 ? new Color(0.7f, 0.82f, 1f) : Color.white;
                Plot(px, w, h, x, y, c * b);
                if (b > 1.2f) { Plot(px, w, h, x + 1, y, c * b * 0.35f); Plot(px, w, h, x - 1, y, c * b * 0.35f); Plot(px, w, h, x, y + 1, c * b * 0.35f); Plot(px, w, h, x, y - 1, c * b * 0.35f); }
            }
            Save(Sky, px, w, h);
        }

        static void Plot(Color[] px, int w, int h, int x, int y, Color c)
        {
            if (y < 0 || y >= h) return;
            x = (x % w + w) % w;
            px[y * w + x] += c;
        }

        static void WriteIce()
        {
            const int n = 1024;
            var albedo = new Color[n * n];
            var height = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float broad = Tile(u, v, 3f, 0f);
                    float fine = Tile(u, v, 24f, 7f);
                    // Lineae: long, slightly wavy bands crossing the tile (tileable sines).
                    float l1 = Mathf.Abs(Mathf.Sin((u * 2f + v * 1f + 0.08f * Mathf.Sin(v * Mathf.PI * 6f)) * Mathf.PI * 2f));
                    float l2 = Mathf.Abs(Mathf.Sin((u * -1f + v * 3f + 0.05f * Mathf.Sin(u * Mathf.PI * 8f)) * Mathf.PI * 2f));
                    float lineae = Mathf.Clamp01(1f - Mathf.Min(l1, l2) * 14f) * Mathf.Lerp(0.4f, 1f, broad);
                    // Cracks: narrow dark lines from a second, sharper pattern.
                    float crack = Mathf.Clamp01(1f - Mathf.Abs(Tile(u, v, 9f, 3f) - 0.5f) * 70f);
                    Color ice = Color.Lerp(new Color(0.62f, 0.7f, 0.8f), new Color(0.9f, 0.94f, 0.98f), broad * 0.7f + fine * 0.3f);
                    ice = Color.Lerp(ice, new Color(0.45f, 0.27f, 0.17f), lineae * 0.55f);
                    ice = Color.Lerp(ice, new Color(0.3f, 0.35f, 0.43f), crack * 0.35f);
                    albedo[y * n + x] = ice;
                    height[y * n + x] = broad * 0.5f + fine * 0.35f - crack * 0.4f - lineae * 0.15f;
                }
            }
            Save(IceAlbedo, albedo, n, n);

            var normal = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = height[y * n + (x + 1) % n] - height[y * n + (x - 1 + n) % n];
                    float dy = height[((y + 1) % n) * n + x] - height[((y - 1 + n) % n) * n + x];
                    var nrm = new Vector3(-dx * 3f, -dy * 3f, 1f).normalized;
                    normal[y * n + x] = new Color(nrm.x * 0.5f + 0.5f, nrm.y * 0.5f + 0.5f, nrm.z * 0.5f + 0.5f);
                }
            Save(IceNormal, normal, n, n);
        }

        /// <summary>Tileable fractal noise in 0..1 (Perlin sampled on a torus).</summary>
        static float Tile(float u, float v, float scale, float seed)
        {
            float a = u * Mathf.PI * 2f, b = v * Mathf.PI * 2f, r = scale / (Mathf.PI * 2f);
            float sum = 0f, amp = 1f, total = 0f, freq = 1f;
            for (int o = 0; o < 4; o++)
            {
                float x = Mathf.Cos(a) * r * freq + seed * 13f, y = Mathf.Sin(a) * r * freq;
                float z = Mathf.Cos(b) * r * freq + seed * 7f, w = Mathf.Sin(b) * r * freq;
                sum += amp * (Mathf.PerlinNoise(x + z, y + w) * 0.5f + Mathf.PerlinNoise(x - w + 50f, y + z) * 0.5f);
                total += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return sum / total;
        }

        static void Save(string path, Color[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void Configure(string path, bool normal, int max, TextureWrapMode wrap)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var t = (TextureImporter)AssetImporter.GetAtPath(path);
            t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            t.maxTextureSize = max;
            t.wrapMode = wrap;
            t.mipmapEnabled = true;
            t.textureCompression = TextureImporterCompression.Compressed;
            t.SaveAndReimport();
        }
    }
}
