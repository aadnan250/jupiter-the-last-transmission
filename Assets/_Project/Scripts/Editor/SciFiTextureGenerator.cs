using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Generates the Meridian's surface textures procedurally, with matching normal maps derived from a height field:
    /// panelled walls (seams, bevels, rivets, vents, stencil marks, scuffs), diamond-plate floor, ceiling panels,
    /// hazard stripes, and an equirectangular starfield for the windows. Deterministic, original, and cheap to
    /// render on integrated graphics (one albedo + one normal map per surface type).
    /// </summary>
    public static class SciFiTextureGenerator
    {
        public const string Folder = "Assets/_Project/Art/Textures/Ship";

        public static string Albedo(string name) => $"{Folder}/{name}_albedo.png";
        public static string Normal(string name) => $"{Folder}/{name}_normal.png";
        public const string Stars = Folder + "/starfield.png";

        [MenuItem("Jupiter/Generate Ship Textures")]
        public static void GenerateAll()
        {
            GreyboxKit.EnsureFolder(Folder);
            WallPanel("wall", 512, new Color(0.58f, 0.6f, 0.63f), seed: 11, withLabels: true);
            WallPanel("ceiling", 512, new Color(0.36f, 0.38f, 0.41f), seed: 23, withLabels: false);
            DoorPanel("door", 512, seed: 31);
            DiamondPlate("floor", 512, seed: 41);
            Hazard("hazard", 256, seed: 53);
            ScreenUI("screen_ui", 512, seed: 71);
            Starfield(2048, 1024, seed: 61);
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ surfaces

        /// <summary>2x2 panels per tile with seams, bevels, rivets, a vent, stencil labels and scuffs.</summary>
        static void WallPanel(string name, int size, Color baseColor, int seed, bool withLabels)
        {
            var rng = new Random(seed);
            var albedo = new Color[size * size];
            var height = new float[size * size];
            int half = size / 2;
            int seam = Math.Max(3, size / 128);
            int bevel = size / 40;

            // Per-panel variation
            var tint = new float[4];
            for (int i = 0; i < 4; i++) tint[i] = 0.94f + (float)rng.NextDouble() * 0.1f;
            int ventPanel = rng.Next(4);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int px = x % half, py = y % half;
                int panel = (x / half) + 2 * (y / half);
                float h = 0.5f;
                Color c = baseColor * tint[panel];

                // Seams between panels
                int edge = Math.Min(Math.Min(px, half - 1 - px), Math.Min(py, half - 1 - py));
                if (edge < seam) { h = 0.0f; c = baseColor * 0.35f; }
                else if (edge < seam + bevel)
                {
                    float k = (edge - seam) / (float)bevel;
                    h = 0.2f + 0.3f * k;
                    // light from top-left: top/left bevels brighter
                    bool topLeft = (px < half / 2 && px == edge) || (py >= half / 2 && half - 1 - py == edge);
                    c *= topLeft ? 1.08f : 0.86f;
                }

                // Low-frequency grime and fine noise
                float grime = Mathf.PerlinNoise(x * 0.012f + seed, y * 0.012f);
                float fine = Mathf.PerlinNoise(x * 0.3f, y * 0.3f + seed);
                c *= 0.9f + 0.12f * grime + 0.04f * (fine - 0.5f);

                albedo[y * size + x] = c;
                height[y * size + x] = h;
            }

            // Rivets in panel corners
            for (int p = 0; p < 4; p++)
            {
                int ox = (p % 2) * half, oy = (p / 2) * half;
                int inset = seam + bevel + size / 40;
                foreach (var (cx, cy) in new[] { (inset, inset), (half - inset, inset), (inset, half - inset), (half - inset, half - inset) })
                    Disc(albedo, height, size, ox + cx, oy + cy, size / 110f + 1.5f, baseColor * 0.75f, 0.9f);
            }

            // Vent slots on one panel
            {
                int ox = (ventPanel % 2) * half, oy = (ventPanel / 2) * half;
                int slots = 6, slotH = half / 28, gap = half / 14;
                int x0 = ox + half / 4, x1 = ox + half * 3 / 4;
                int y0 = oy + half / 2 - (slots * gap) / 2;
                for (int s = 0; s < slots; s++)
                    Rect(albedo, height, size, x0, y0 + s * gap, x1, y0 + s * gap + slotH, baseColor * 0.18f, 0.05f);
            }

            // Stencil labels: small coloured bars and dark text-like blocks
            if (withLabels)
            {
                for (int i = 0; i < 3; i++)
                {
                    int p = rng.Next(4);
                    if (p == ventPanel) continue;
                    int ox = (p % 2) * half, oy = (p / 2) * half;
                    int lx = ox + half / 6 + rng.Next(half / 3), ly = oy + half / 6 + rng.Next(half / 2);
                    Color stripe = rng.Next(2) == 0 ? new Color(0.92f, 0.72f, 0.12f) : new Color(0.85f, 0.85f, 0.82f);
                    Rect(albedo, null, size, lx, ly, lx + half / 3, ly + half / 40, stripe, 0);
                    for (int k = 0; k < 5; k++) // "text" glyph blocks
                    {
                        int gx = lx + k * (half / 18);
                        Rect(albedo, null, size, gx, ly + half / 24, gx + half / 30, ly + half / 24 + half / 28, baseColor * 0.3f, 0);
                    }
                }
            }

            // Scuffs and scratches
            for (int i = 0; i < 40; i++)
            {
                float sx = rng.Next(size), sy = rng.Next(size), ang = (float)(rng.NextDouble() * Math.PI);
                int len = 10 + rng.Next(size / 10);
                for (int t = 0; t < len; t++)
                {
                    int xx = (int)(sx + Math.Cos(ang) * t) & (size - 1), yy = (int)(sy + Math.Sin(ang) * t) & (size - 1);
                    albedo[yy * size + xx] *= 1.12f;
                }
            }

            Save(name, size, albedo, height, normalStrength: 6f);
        }

        /// <summary>Door panel: horizontal ribs, a central window band and a hazard chevron strip at the bottom.</summary>
        static void DoorPanel(string name, int size, int seed)
        {
            var albedo = new Color[size * size];
            var height = new float[size * size];
            var baseColor = new Color(0.42f, 0.45f, 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = y / (float)size;
                Color c = baseColor;
                float h = 0.5f;
                // ribs
                if (((y / (size / 16)) % 2) == 0 && v > 0.15f && v < 0.85f) { c *= 0.92f; h = 0.6f; }
                // hazard band near bottom
                if (v < 0.12f)
                {
                    bool stripe = ((x + y) / (size / 10)) % 2 == 0;
                    c = stripe ? new Color(0.93f, 0.74f, 0.12f) : new Color(0.08f, 0.08f, 0.09f);
                }
                // frame edge
                int edge = Math.Min(Math.Min(x, size - 1 - x), Math.Min(y, size - 1 - y));
                if (edge < size / 40) { c = baseColor * 0.4f; h = 0.2f; }
                c *= 0.95f + 0.08f * Mathf.PerlinNoise(x * 0.02f + seed, y * 0.02f);
                albedo[y * size + x] = c;
                height[y * size + x] = h;
            }
            Save(name, size, albedo, height, normalStrength: 4f);
        }

        /// <summary>Dark diamond-plate deck: raised lozenges in a staggered grid, with worn bright tops.</summary>
        static void DiamondPlate(string name, int size, int seed)
        {
            var albedo = new Color[size * size];
            var height = new float[size * size];
            var baseColor = new Color(0.24f, 0.25f, 0.27f);
            int cell = size / 16;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int row = y / cell;
                float cx = ((x + (row % 2) * cell / 2) % cell) - cell / 2f;
                float cy = (y % cell) - cell / 2f;
                // rotate 45 degrees alternately for the classic tread pattern
                float a = (row + x / cell) % 2 == 0 ? 0.785f : -0.785f;
                float rx = cx * Mathf.Cos(a) - cy * Mathf.Sin(a), ry = cx * Mathf.Sin(a) + cy * Mathf.Cos(a);
                float d = (rx * rx) / (cell * 0.32f * cell * 0.32f) + (ry * ry) / (cell * 0.08f * cell * 0.08f);
                float h = d < 1f ? 0.5f + 0.5f * (1f - d) : 0.3f;
                Color c = baseColor * (d < 1f ? 1.25f : 1f);
                c *= 0.88f + 0.2f * Mathf.PerlinNoise(x * 0.01f + seed, y * 0.01f);
                albedo[y * size + x] = c;
                height[y * size + x] = h;
            }
            Save(name, size, albedo, height, normalStrength: 5f);
        }

        static void Hazard(string name, int size, int seed)
        {
            var albedo = new Color[size * size];
            var height = new float[size * size];
            var rng = new Random(seed);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool yellow = ((x + y) / (size / 4)) % 2 == 0;
                Color c = yellow ? new Color(0.93f, 0.73f, 0.1f) : new Color(0.07f, 0.07f, 0.08f);
                float wear = Mathf.PerlinNoise(x * 0.05f + seed, y * 0.05f);
                if (wear > 0.72f) c = Color.Lerp(c, new Color(0.35f, 0.35f, 0.36f), (wear - 0.72f) * 3f);
                albedo[y * size + x] = c;
                height[y * size + x] = 0.5f;
            }
            _ = rng;
            Save(name, size, albedo, height, normalStrength: 1f);
        }

        /// <summary>
        /// Monochrome interface mask used as both base and emission map for displays: a faint grid, framed panes,
        /// a waveform, bar graphs, rows of text-like glyph blocks and a radar sweep. Any crop of it reads as a UI.
        /// </summary>
        static void ScreenUI(string name, int size, int seed)
        {
            var rng = new Random(seed);
            var v = new float[size * size];
            void Px(int x, int y, float a) { if (x >= 0 && y >= 0 && x < size && y < size) v[y * size + x] = Math.Max(v[y * size + x], a); }
            void Line(int x0, int y0, int x1, int y1, float a)
            {
                int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
                for (int i = 0; i <= n; i++) Px(x0 + (x1 - x0) * i / Math.Max(1, n), y0 + (y1 - y0) * i / Math.Max(1, n), a);
            }
            void RectOutline(int x0, int y0, int x1, int y1, float a) { Line(x0, y0, x1, y0, a); Line(x0, y1, x1, y1, a); Line(x0, y0, x0, y1, a); Line(x1, y0, x1, y1, a); }
            void Fill(int x0, int y0, int x1, int y1, float a) { for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) Px(x, y, a); }

            // Faint grid
            for (int i = 0; i < size; i += 16) { Line(i, 0, i, size - 1, 0.08f); Line(0, i, size - 1, i, 0.08f); }

            int h = size / 2, pad = 10;
            // Pane borders (2x2)
            for (int py = 0; py < 2; py++) for (int px = 0; px < 2; px++)
                RectOutline(px * h + pad, py * h + pad, px * h + h - pad, py * h + h - pad, 0.55f);

            // Pane 1 (bottom-left): waveform
            for (int x = pad + 6; x < h - pad - 6; x++)
            {
                float t = x / (float)h;
                int y = h / 2 + (int)(Math.Sin(t * 18) * 30 + Math.Sin(t * 53) * 10 + (rng.NextDouble() - 0.5) * 8);
                Px(x, y, 1f); Px(x, y + 1, 0.9f);
            }
            // Pane 2 (bottom-right): bar graph
            for (int b = 0; b < 12; b++)
            {
                int x0 = h + pad + 14 + b * 18, bh = 20 + rng.Next(h - 70);
                Fill(x0, pad + 12, x0 + 11, pad + 12 + bh, b % 4 == 0 ? 1f : 0.7f);
            }
            // Pane 3 (top-left): rows of text-like glyph blocks
            for (int row = 0; row < 14; row++)
            {
                int y = h + pad + 14 + row * 16, x = pad + 14;
                while (x < h - pad - 30)
                {
                    int w = 4 + rng.Next(4) * 3;
                    Fill(x, y, x + w, y + 7, row == 0 ? 1f : 0.75f);
                    x += w + (rng.Next(5) == 0 ? 12 : 3);
                }
            }
            // Pane 4 (top-right): radar rings, sweep and contacts
            int cx = h + h / 2, cy = h + h / 2;
            for (int r = 24; r < h / 2 - pad; r += 24)
                for (int a = 0; a < 360; a++)
                    Px(cx + (int)(Math.Cos(a * Math.PI / 180) * r), cy + (int)(Math.Sin(a * Math.PI / 180) * r), 0.45f);
            for (int k = 0; k < 40; k++) // sweep wedge fading
            {
                double ang = 0.9 - k * 0.012;
                Line(cx, cy, cx + (int)(Math.Cos(ang) * (h / 2 - pad - 4)), cy + (int)(Math.Sin(ang) * (h / 2 - pad - 4)), 1f - k / 45f);
            }
            for (int d = 0; d < 6; d++) Fill(cx - 60 + rng.Next(120), cy - 60 + rng.Next(120), 0, 0, 0f);
            for (int d = 0; d < 6; d++) { int dx = cx - 70 + rng.Next(140), dy = cy - 70 + rng.Next(140); Fill(dx, dy, dx + 4, dy + 4, 1f); }

            var albedo = new Color[size * size];
            for (int i = 0; i < v.Length; i++) albedo[i] = new Color(v[i], v[i], v[i]);
            var t2 = new Texture2D(size, size, TextureFormat.RGB24, true);
            t2.SetPixels(albedo);
            t2.Apply();
            WritePng(Albedo(name), t2.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t2);
            Configure(Albedo(name), normal: false, maxSize: size, wrapClampV: false);
        }

        /// <summary>Equirectangular starfield: thousands of stars with a power-law brightness and a faint galactic band.</summary>
        static void Starfield(int w, int h, int seed)
        {
            var rng = new Random(seed);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Faint band across the sky, like the Milky Way seen edge-on.
                float lat = (y / (float)h - 0.5f) * Mathf.PI;
                float lon = x / (float)w * Mathf.PI * 2f;
                float band = Mathf.Exp(-Mathf.Pow((lat - 0.35f * Mathf.Sin(lon)) * 4f, 2f));
                float dust = Mathf.PerlinNoise(x * 0.01f, y * 0.02f) * band * 0.05f;
                byte b = (byte)(Mathf.Clamp01(0.006f + dust) * 255);
                px[y * w + x] = new Color32(b, b, (byte)Mathf.Min(255, b + 2), 255);
            }
            int stars = 9000;
            for (int i = 0; i < stars; i++)
            {
                int x = rng.Next(w), y = rng.Next(h);
                float bright = Mathf.Pow((float)rng.NextDouble(), 6f);           // most are faint
                float temp = (float)rng.NextDouble();
                Color c = temp < 0.2f ? new Color(1f, 0.82f, 0.7f) : temp > 0.85f ? new Color(0.75f, 0.85f, 1f) : Color.white;
                c *= 0.25f + bright * 0.75f;
                px[y * w + x] = Color.Lerp(px[y * w + x], c, 1f);
                if (bright > 0.6f) // a few bright stars get a 1-pixel glow
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int xx = (x + dx + w) % w, yy = Mathf.Clamp(y + dy, 0, h - 1);
                        px[yy * w + xx] = Color.Lerp(px[yy * w + xx], c, 0.35f);
                    }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.SetPixels32(px);
            tex.Apply();
            WritePng(Stars, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Configure(Stars, normal: false, maxSize: 2048, wrapClampV: true);
        }

        // ------------------------------------------------------------------ helpers

        static void Disc(Color[] albedo, float[] height, int size, int cx, int cy, float r, Color c, float h)
        {
            int ir = Mathf.CeilToInt(r);
            for (int y = -ir; y <= ir; y++)
            for (int x = -ir; x <= ir; x++)
            {
                float d = Mathf.Sqrt(x * x + y * y);
                if (d > r) continue;
                int i = ((cy + y) & (size - 1)) * size + ((cx + x) & (size - 1));
                albedo[i] = c * (1.1f - 0.3f * d / r);
                height[i] = h * Mathf.Sqrt(1f - (d / r) * (d / r));
            }
        }

        static void Rect(Color[] albedo, float[] height, int size, int x0, int y0, int x1, int y1, Color c, float h)
        {
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                int i = (y & (size - 1)) * size + (x & (size - 1));
                albedo[i] = c;
                if (height != null) height[i] = h;
            }
        }

        static void Save(string name, int size, Color[] albedo, float[] height, float normalStrength)
        {
            var a = new Texture2D(size, size, TextureFormat.RGB24, true);
            a.SetPixels(albedo);
            a.Apply();
            WritePng(Albedo(name), a.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(a);

            // Normal map from the height field (central differences, tileable).
            var n = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float hl = height[y * size + ((x - 1) & (size - 1))], hr = height[y * size + ((x + 1) & (size - 1))];
                float hd = height[((y - 1) & (size - 1)) * size + x], hu = height[((y + 1) & (size - 1)) * size + x];
                var v = new Vector3((hl - hr) * normalStrength, (hd - hu) * normalStrength, 1f).normalized;
                n[y * size + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f);
            }
            var nt = new Texture2D(size, size, TextureFormat.RGB24, true);
            nt.SetPixels(n);
            nt.Apply();
            WritePng(Normal(name), nt.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(nt);

            Configure(Albedo(name), normal: false, maxSize: size, wrapClampV: false);
            Configure(Normal(name), normal: true, maxSize: size, wrapClampV: false);
        }

        static void WritePng(string assetPath, byte[] png)
        {
            string full = Path.GetFullPath(assetPath);
            if (!File.Exists(full) || !File.ReadAllBytes(full).AsSpan().SequenceEqual(png)) File.WriteAllBytes(full, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        static void Configure(string path, bool normal, int maxSize, bool wrapClampV)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.wrapModeU = TextureWrapMode.Repeat;
            ti.wrapModeV = wrapClampV ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.maxTextureSize = maxSize;
            ti.mipmapEnabled = true;
            ti.anisoLevel = normal ? 1 : 4;
            ti.SaveAndReimport();
        }
    }
}
