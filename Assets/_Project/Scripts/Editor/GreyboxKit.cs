using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Shared helpers for the editor tools that generate grey-box scenes:
    /// folders, materials, boxes, lights, audio sources and Inspector-style field assignment.
    /// </summary>
    public static class GreyboxKit
    {
        public const string MaterialFolder = "Assets/_Project/Art/Materials/Greybox";

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        /// <summary>Creates or updates a URP Lit material asset. Emission makes screens and LEDs glow.</summary>
        public static Material Mat(string name, Color color, Color? emission = null, float smoothness = 0.15f)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);

            // Use the LocalKeyword API: the string overload does not reliably persist on URP materials.
            var emissionKeyword = new UnityEngine.Rendering.LocalKeyword(m.shader, "_EMISSION");
            if (emission.HasValue)
            {
                var e = emission.Value;
                e.a = 1f;
                m.SetColor("_EmissionColor", e);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetKeyword(emissionKeyword, true);
            }
            else
            {
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                m.SetKeyword(emissionKeyword, false);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Axis-aligned static cube. Position is the centre; size is full extents.</summary>
        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
            return go;
        }

        /// <summary>Box defined by min/max corners, convenient for walls and slabs.</summary>
        public static GameObject Span(Transform parent, string name, Vector3 min, Vector3 max, Material mat, bool collider = true)
            => Box(parent, name, (min + max) * 0.5f, max - min, mat, collider);

        public static Light PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None; // budget: no additional-light shadows in grey-box
            return l;
        }

        /// <summary>Configures an AudioSource for 3D positional sound with a linear falloff.</summary>
        public static AudioSource Audio3D(GameObject go, AudioClip clip, float minDistance, float maxDistance)
        {
            // Explicit null check: Unity's overloaded == must be used, not ??.
            var src = go.GetComponent<AudioSource>();
            if (src == null) src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.dopplerLevel = 0f;
            return src;
        }

        /// <summary>Assigns a (possibly private) [SerializeField] through SerializedObject, exactly as the Inspector would.</summary>
        public static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[GreyboxKit] Field '{field}' not found on {target.GetType().Name}");
                return;
            }

            switch (value)
            {
                case null: prop.objectReferenceValue = null; break;
                case Object o: prop.objectReferenceValue = o; break;
                case Vector3 v: prop.vector3Value = v; break;
                case float f: prop.floatValue = f; break;
                case int i: prop.intValue = i; break;
                case bool b: prop.boolValue = b; break;
                case string s: prop.stringValue = s; break;
                case Color c: prop.colorValue = c; break;
                case IList list when prop.isArray:
                    prop.arraySize = list.Count;
                    for (int i = 0; i < list.Count; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = (Object)list[i];
                    break;
                default:
                    Debug.LogError($"[GreyboxKit] Unsupported value type {value.GetType().Name} for '{field}'");
                    break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
