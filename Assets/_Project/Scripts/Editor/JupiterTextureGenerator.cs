using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Generates Jupiter's equirectangular texture (2048 x 1024), laid out on the real planet's bands: the pale
    /// Equatorial Zone, the dark North and South Equatorial Belts with festoons trailing off the NEB's edge, the
    /// temperate belts and zones, white ovals in the South Temperate Belt, the Great Red Spot at 22° S with its
    /// hollow in the SEB, and the bluish-grey polar haze. Cloud texture is domain-warped noise sheared along the
    /// bands, as the jets shear the real thing. Deterministic output.
    /// </summary>
    public static class JupiterTextureGenerator
    {
        public const string TexturePath = "Assets/_Project/Art/Textures/Generated/jupiter_placeholder.png";
        const int Width = 2048, Height = 1024;

        // Band edges by planetographic latitude (north positive), with each band's colour.
        static readonly (float from, float to, Color color)[] Bands =
        {
            (-90f, -60f, new Color(0.55f, 0.56f, 0.58f)),  // south polar region
            (-60f, -45f, new Color(0.70f, 0.66f, 0.60f)),
            (-45f, -36f, new Color(0.82f, 0.77f, 0.68f)),  // south south temperate zone
            (-36f, -29f, new Color(0.62f, 0.52f, 0.42f)),  // south temperate belt
            (-29f, -20f, new Color(0.93f, 0.89f, 0.80f)),  // south tropical zone
            (-20f, -7f, new Color(0.62f, 0.44f, 0.30f)),   // south equatorial belt
            (-7f, 7f, new Color(0.95f, 0.91f, 0.82f)),     // equatorial zone
            (7f, 18f, new Color(0.55f, 0.37f, 0.25f)),     // north equatorial belt
            (18f, 24f, new Color(0.92f, 0.87f, 0.77f)),    // north tropical zone
            (24f, 31f, new Color(0.66f, 0.53f, 0.42f)),    // north temperate belt
            (31f, 40f, new Color(0.86f, 0.80f, 0.70f)),    // north temperate zone
            (40f, 50f, new Color(0.70f, 0.64f, 0.56f)),
            (50f, 90f, new Color(0.56f, 0.57f, 0.60f)),    // north polar region
        };

        static Color BandColor(float lat)
        {
            for (int i = 0; i < Bands.Length; i++)
            {
                var b = Bands[i];
                if (lat < b.from || lat > b.to) continue;
                // Blend into the neighbour over the last 1.5° so edges are soft but still read as edges.
                float edge = 1.5f;
                if (i + 1 < Bands.Length && lat > b.to - edge) return Color.Lerp(b.color, Bands[i + 1].color, (lat - (b.to - edge)) / (2f * edge));
                if (i > 0 && lat < b.from + edge) return Color.Lerp(Bands[i - 1].color, b.color, 0.5f + (lat - b.from) / (2f * edge));
                return b.color;
            }
            return Bands[Bands.Length - 1].color;
        }

        static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f, amp = 0.5f, f = 1f;
            for (int i = 0; i < octaves; i++) { sum += amp * (Mathf.PerlinNoise(x * f + i * 17.3f, y * f - i * 9.1f) - 0.5f); f *= 2.03f; amp *= 0.5f; }
            return sum;
        }

        public static Texture2D Generate()
        {
            GreyboxKit.EnsureFolder("Assets/_Project/Art/Textures/Generated");
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, true);
            var px = new Color32[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                float lat = (y / (float)(Height - 1)) * 180f - 90f;
                float shear = Mathf.Sin(lat * 0.35f) * 0.6f;     // alternating jets
                for (int x = 0; x < Width; x++)
                {
                    float lon = x / (float)Width * 360f;
                    // Noise on a cylinder (two copies blended across the seam so the texture wraps).
                    float u = x / (float)Width, v = y / (float)Height;
                    float wrap = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, u));
                    float Sample(float uu)
                    {
                        float nx = uu * 14f + shear * v * 30f, ny = v * 60f;
                        float wx = Fbm(nx * 0.5f, ny * 0.5f, 3) * 3f, wy = Fbm(nx * 0.5f + 5f, ny * 0.5f + 3f, 3) * 0.8f;
                        return Fbm(nx + wx, ny + wy, 5);
                    }
                    float n = wrap > 0f ? Mathf.Lerp(Sample(u), Sample(u - 1f), wrap) : Sample(u);

                    // The band lookup wanders with the turbulence, so the edges curl.
                    Color c = BandColor(lat + n * 4f);
                    c *= 1f + n * 0.35f;

                    // Festoons: dark blue-grey plumes trailing south-west off the NEB's southern edge.
                    float fest = Mathf.Repeat(lon / 360f * 11f + lat * 0.04f, 1f);
                    if (lat > 4f && lat < 9f && fest < 0.12f)
                        c = Color.Lerp(c, new Color(0.38f, 0.40f, 0.46f), (1f - fest / 0.12f) * Mathf.InverseLerp(4f, 8f, lat) * 0.7f);

                    // White ovals strung along the South Temperate Belt.
                    for (int o = 0; o < 4; o++)
                    {
                        float ox = Mathf.DeltaAngle(lon, 40f + o * 85f) / 4.5f, oy = (lat + 33f) / 2.4f;
                        float od = ox * ox + oy * oy;
                        if (od < 1f) c = Color.Lerp(c, new Color(0.96f, 0.95f, 0.92f), Mathf.SmoothStep(1f, 0f, od) * 0.9f);
                    }

                    // The Great Red Spot, with spiral banding and the pale hollow it sits in.
                    float gx = Mathf.DeltaAngle(lon, 200f) / 12f, gy = (lat + 22f) / 6f;
                    float gd = gx * gx + gy * gy;
                    if (gd < 2.2f) c = Color.Lerp(c, new Color(0.93f, 0.88f, 0.80f), Mathf.SmoothStep(2.2f, 1f, gd) * 0.8f);
                    if (gd < 1f)
                    {
                        float ang = Mathf.Atan2(gy, gx) + gd * 4f;
                        float swirl = 0.5f + 0.5f * Mathf.Sin(ang * 3f + n * 6f);
                        Color spot = Color.Lerp(new Color(0.80f, 0.42f, 0.28f), new Color(0.62f, 0.27f, 0.18f), swirl * (1f - gd));
                        c = Color.Lerp(c, spot, Mathf.SmoothStep(1f, 0.15f, gd));
                    }

                    // Polar haze: cooler, darker, softer.
                    float pole = Mathf.Pow(Mathf.InverseLerp(45f, 90f, Mathf.Abs(lat)), 1.5f);
                    c = Color.Lerp(c, new Color(0.45f, 0.48f, 0.52f), pole * 0.6f) * Mathf.Lerp(1f, 0.75f, pole);
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
                importer.maxTextureSize = 2048;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }
    }
}
