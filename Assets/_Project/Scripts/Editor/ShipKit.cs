using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Modular kit for building spaceship interiors from code: boxes whose UVs tile in world metres
    /// (so panel textures never stretch), textured ship materials, emissive light panels and LEDs,
    /// pipes, structural ribs, wall signs and window frames.
    /// </summary>
    public static class ShipKit
    {
        const string MeshFolder = "Assets/_Project/Art/Meshes/Generated";
        const float TileMetres = 2f; // one texture repeat per 2 m (wall texture = 2x2 panels of 1 m)

        static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

        // ------------------------------------------------------------------ materials

        public static Material Wall => Textured("SHIP_Wall", "wall", Color.white, 0.3f, 0.35f);
        public static Material Ceiling => Textured("SHIP_Ceiling", "ceiling", Color.white, 0.3f, 0.3f);
        public static Material Floor => Textured("SHIP_Floor", "floor", Color.white, 0.55f, 0.45f);
        public static Material Hazard => Textured("SHIP_Hazard", "hazard", Color.white, 0.1f, 0.35f);
        public static Material DoorPanel => Textured("SHIP_Door", "door", Color.white, 0.4f, 0.45f);
        public static Material DarkMetal => Plain("SHIP_DarkMetal", new Color(0.12f, 0.13f, 0.15f), 0.6f, 0.45f);
        public static Material Metal => Plain("SHIP_Metal", new Color(0.5f, 0.52f, 0.55f), 0.7f, 0.5f);
        public static Material Frame => Plain("SHIP_Frame", new Color(0.2f, 0.21f, 0.23f), 0.5f, 0.4f);
        public static Material White => Plain("SHIP_WhitePlastic", new Color(0.86f, 0.87f, 0.86f), 0f, 0.55f);
        public static Material Fabric => Plain("SHIP_Fabric", new Color(0.24f, 0.3f, 0.38f), 0f, 0.1f);
        public static Material Wood => Plain("SHIP_Composite", new Color(0.42f, 0.33f, 0.24f), 0f, 0.3f);
        public static Material Mirror => Plain("SHIP_Mirror", new Color(0.8f, 0.82f, 0.85f), 1f, 0.95f);
        public static Material Glass => Plain("SHIP_DarkGlass", new Color(0.05f, 0.07f, 0.09f), 0.2f, 0.95f);
        public static Material Pipe => Plain("SHIP_Pipe", new Color(0.55f, 0.57f, 0.6f), 0.8f, 0.55f);
        public static Material PipeYellow => Plain("SHIP_PipeYellow", new Color(0.85f, 0.65f, 0.12f), 0.3f, 0.45f);
        public static Material Orange => Plain("SHIP_Orange", new Color(0.9f, 0.42f, 0.1f), 0.1f, 0.4f);
        public static Material RedCross => Emissive("SHIP_RedCross", new Color(0.9f, 0.1f, 0.1f), 1.5f);
        public static Material LightPanel => Emissive("SHIP_LightPanel", new Color(0.92f, 0.96f, 1f), 3.2f);
        public static Material WarmPanel => Emissive("SHIP_WarmPanel", new Color(1f, 0.88f, 0.72f), 2.6f);
        public static Material ScreenCyan => Display("SHIP_Screen", new Color(0.25f, 0.8f, 1f), 1.5f);
        public static Material ScreenAmber => Display("SHIP_ScreenAmber", new Color(1f, 0.62f, 0.18f), 1.3f);
        public static Material ScreenGreen => Display("SHIP_ScreenGreen", new Color(0.35f, 1f, 0.5f), 1.2f);
        public static Material LedCyan => Emissive("SHIP_LedCyan", new Color(0.2f, 0.9f, 1f), 3f);
        public static Material LedGreen => Emissive("SHIP_LedGreen", new Color(0.3f, 1f, 0.4f), 3f);
        public static Material LedRed => Emissive("SHIP_LedRed", new Color(1f, 0.15f, 0.1f), 3f);
        public static Material LedAmber => Emissive("SHIP_LedAmber", new Color(1f, 0.6f, 0.1f), 3f);
        public static Material Hob => Emissive("SHIP_HobRing", new Color(1f, 0.25f, 0.05f), 1.2f, new Color(0.03f, 0.03f, 0.03f));

        static Material Textured(string name, string tex, Color tint, float metallic, float smooth)
        {
            if (Mats.TryGetValue(name, out var cached) && cached != null) return cached;
            var m = GetOrCreate(name);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(SciFiTextureGenerator.Albedo(tex)));
            m.SetColor("_BaseColor", tint);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(SciFiTextureGenerator.Normal(tex));
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1f);
            m.SetKeyword(new LocalKeyword(m.shader, "_NORMALMAP"), normal != null);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            NoEmission(m);
            EditorUtility.SetDirty(m);
            return Mats[name] = m;
        }

        static Material Plain(string name, Color color, float metallic, float smooth)
        {
            if (Mats.TryGetValue(name, out var cached) && cached != null) return cached;
            var m = GetOrCreate(name);
            m.SetTexture("_BaseMap", null);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            NoEmission(m);
            EditorUtility.SetDirty(m);
            return Mats[name] = m;
        }

        static Material Emissive(string name, Color glow, float intensity, Color? baseColor = null)
        {
            if (Mats.TryGetValue(name, out var cached) && cached != null) return cached;
            var m = GetOrCreate(name);
            m.SetTexture("_BaseMap", null);
            m.SetColor("_BaseColor", baseColor ?? glow * 0.6f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.7f);
            var e = glow * intensity; e.a = 1f;
            m.SetColor("_EmissionColor", e);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetKeyword(new LocalKeyword(m.shader, "_EMISSION"), true);
            EditorUtility.SetDirty(m);
            return Mats[name] = m;
        }

/// <summary>A computer display: dark glass whose emission comes from the generated interface texture.</summary>
        static Material Display(string name, Color glow, float intensity)
        {
            if (Mats.TryGetValue(name, out var cached) && cached != null) return cached;
            var m = GetOrCreate(name);
            var ui = AssetDatabase.LoadAssetAtPath<Texture2D>(SciFiTextureGenerator.Albedo("screen_ui"));
            m.SetTexture("_BaseMap", ui);
            m.SetColor("_BaseColor", glow * 0.08f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.85f);
            m.SetTexture("_EmissionMap", ui);
            var e = glow * intensity; e.a = 1f;
            m.SetColor("_EmissionColor", e);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetKeyword(new LocalKeyword(m.shader, "_EMISSION"), true);
            EditorUtility.SetDirty(m);
            return Mats[name] = m;
        }

        /// <summary>Depth-tested sign text in one colour (Unity's default 3D text shows through walls).</summary>
        static Material TextMaterial(Font font, Color color)
        {
            string name = $"SHIP_Text_{ColorUtility.ToHtmlStringRGB(color)}";
            if (Mats.TryGetValue(name, out var cached) && cached != null) return cached;
            GreyboxKit.EnsureFolder(GreyboxKit.MaterialFolder);
            string path = $"{GreyboxKit.MaterialFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Jupiter/WorldText"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_MainTex", font.material.mainTexture);
            m.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(m);
            return Mats[name] = m;
        }

        static void NoEmission(Material m)
        {
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            m.SetKeyword(new LocalKeyword(m.shader, "_EMISSION"), false);
        }

        static Material GetOrCreate(string name)
        {
            GreyboxKit.EnsureFolder(GreyboxKit.MaterialFolder);
            string path = $"{GreyboxKit.MaterialFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        // ------------------------------------------------------------------ geometry

        /// <summary>Axis-aligned box from min/max corners with world-scaled UVs and an optional box collider.</summary>
        public static GameObject Block(Transform parent, string name, Vector3 min, Vector3 max, Material mat, bool collider = true)
        {
            Vector3 size = max - min;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            go.isStatic = true;
            return go;
        }

        /// <summary>Box by centre and size, optionally rotated (for braces and angled consoles).</summary>
        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, Quaternion? rotation = null, bool collider = true)
        {
            var go = Block(parent, name, center - size * 0.5f, center + size * 0.5f, mat, collider);
            if (rotation.HasValue) go.transform.rotation = rotation.Value;
            return go;
        }

        public static GameObject Cylinder(Transform parent, string name, Vector3 center, float radius, float height, Material mat, Vector3? axisEuler = null, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            if (axisEuler.HasValue) go.transform.rotation = Quaternion.Euler(axisEuler.Value);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 center, float radius, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = Vector3.one * radius * 2f;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        /// <summary>Straight pipe between two points along one axis, with brackets every ~2 m.</summary>
        public static void PipeRun(Transform parent, string name, Vector3 from, Vector3 to, float radius, Material mat)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            var axis = Quaternion.FromToRotation(Vector3.up, d.normalized).eulerAngles;
            Cylinder(parent, name, (from + to) * 0.5f, radius, len, mat, axis);
            int brackets = Mathf.Max(1, Mathf.FloorToInt(len / 2f));
            for (int i = 0; i <= brackets; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)brackets);
                Box(parent, $"{name}_Bracket{i}", p, new Vector3(radius * 3f, radius * 3f, radius * 3f), Frame, collider: false);
            }
        }

        /// <summary>A wall sign: dark plate with legible block lettering, readable from <paramref name="facing"/>.</summary>
        public static GameObject Sign(Transform parent, string text, Vector3 position, Vector3 facing, float width = 1.2f, Color? color = null)
        {
            var root = new GameObject($"Sign_{text}");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-facing));
            Box(root.transform, "Plate", position, new Vector3(width, 0.22f, 0.03f), DarkMetal,
                Quaternion.LookRotation(-facing), collider: false);
            var label = new GameObject("Label");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            var tm = label.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.022f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color ?? new Color(0.95f, 0.8f, 0.3f);
            label.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial(font, tm.color);
            return root;
        }

        public static Light Point(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
            => GreyboxKit.PointLight(parent, name, position, color, intensity, range);

        // ------------------------------------------------------------------ meshes

        /// <summary>A unit-cube mesh scaled to <paramref name="size"/> with UVs in metres / <see cref="TileMetres"/>.</summary>
        public static Mesh BoxMesh(Vector3 size)
        {
            string key = $"box_{size.x:0.00}x{size.y:0.00}x{size.z:0.00}".Replace(',', '.');
            if (MeshCache.TryGetValue(key, out var cached) && cached != null) return cached;

            GreyboxKit.EnsureFolder(MeshFolder);
            string path = $"{MeshFolder}/{key}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = BuildBox(size);
                mesh.name = key;
                AssetDatabase.CreateAsset(mesh, path);
            }
            return MeshCache[key] = mesh;
        }

        static Mesh BuildBox(Vector3 s)
        {
            Vector3 h = s * 0.5f;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();

            void Face(Vector3 normal, Vector3 u, Vector3 w, float uLen, float wLen)
            {
                int i = v.Count;
                Vector3 c = Vector3.Scale(normal, h);
                Vector3 hu = u * (uLen * 0.5f), hw = w * (wLen * 0.5f);
                v.Add(c - hu - hw); v.Add(c + hu - hw); v.Add(c + hu + hw); v.Add(c - hu + hw);
                for (int k = 0; k < 4; k++) n.Add(normal);
                float U = uLen / TileMetres, W = wLen / TileMetres;
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(U, 0)); uv.Add(new Vector2(U, W)); uv.Add(new Vector2(0, W));
                // Unity treats clockwise triangles (as seen by the viewer) as front faces. Seen from outside,
                // +u runs to the viewer's left (left-handed axes), so this order is clockwise on screen.
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            // Each face: outward normal, then two in-plane axes with u x w == normal.
            Face(Vector3.right, Vector3.back, Vector3.up, s.z, s.y);
            Face(Vector3.left, Vector3.forward, Vector3.up, s.z, s.y);
            Face(Vector3.up, Vector3.right, Vector3.back, s.x, s.z);
            Face(Vector3.down, Vector3.right, Vector3.forward, s.x, s.z);
            Face(Vector3.forward, Vector3.right, Vector3.up, s.x, s.y);
            Face(Vector3.back, Vector3.left, Vector3.up, s.x, s.y);

            var mesh = new Mesh();
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
