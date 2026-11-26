using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Sets up URP post-processing (a global Volume with bloom, tonemapping, colour grading, vignette, film grain
    /// and a touch of chromatic aberration) and turns it on for a camera. The editor assembly does not reference
    /// the URP runtime assembly, so the URP types are found by name: if a future URP renames them, this logs a
    /// warning and the level simply renders without the film look.
    /// </summary>
    public static class PostFxKit
    {
        static Type Find(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName)).FirstOrDefault(t => t != null);

        public static void CinematicVolume(string profilePath, float exposure, float saturation, float contrast, Color tint)
        {
            var volumeType = Find("UnityEngine.Rendering.Volume");
            var profileType = Find("UnityEngine.Rendering.VolumeProfile");
            if (volumeType == null || profileType == null) { Debug.LogWarning("[PostFxKit] URP Volume types not found; skipping."); return; }

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(profilePath) != null) AssetDatabase.DeleteAsset(profilePath);
            var profile = ScriptableObject.CreateInstance(profileType);
            AssetDatabase.CreateAsset(profile, profilePath);

            Add(profile, "UnityEngine.Rendering.Universal.Bloom", ("threshold", 0.95f), ("intensity", 1.1f), ("scatter", 0.72f));
            Add(profile, "UnityEngine.Rendering.Universal.Tonemapping", ("mode", "ACES"));
            Add(profile, "UnityEngine.Rendering.Universal.ColorAdjustments", ("postExposure", exposure), ("contrast", contrast),
                ("saturation", saturation), ("colorFilter", tint));
            Add(profile, "UnityEngine.Rendering.Universal.Vignette", ("intensity", 0.38f), ("smoothness", 0.45f));
            Add(profile, "UnityEngine.Rendering.Universal.FilmGrain", ("type", "Medium1"), ("intensity", 0.32f));
            Add(profile, "UnityEngine.Rendering.Universal.ChromaticAberration", ("intensity", 0.12f));
            AssetDatabase.SaveAssets();

            var go = new GameObject("PostProcessing");
            var volume = go.AddComponent(volumeType);
            volumeType.GetProperty("isGlobal")?.SetValue(volume, true);
            volumeType.GetField("sharedProfile")?.SetValue(volume, profile);
        }

        /// <summary>Turns post-processing on for the camera (URP keeps the switch on its own camera data component).</summary>
        public static void EnableOn(Camera cam)
        {
            var dataType = Find("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (dataType == null || cam == null) return;
            var data = cam.GetComponent(dataType) ?? cam.gameObject.AddComponent(dataType);
            dataType.GetProperty("renderPostProcessing")?.SetValue(data, true);
            EditorUtility.SetDirty(data);
        }

        static void Add(ScriptableObject profile, string typeName, params (string field, object value)[] settings)
        {
            var type = Find(typeName);
            if (type == null) { Debug.LogWarning($"[PostFxKit] {typeName} not found"); return; }
            var add = profile.GetType().GetMethod("Add", new[] { typeof(Type), typeof(bool) });
            var component = (ScriptableObject)add.Invoke(profile, new object[] { type, true });
            foreach (var (field, value) in settings)
            {
                var param = type.GetField(field)?.GetValue(component);
                if (param == null) { Debug.LogWarning($"[PostFxKit] {typeName}.{field} not found"); continue; }
                var valueProp = param.GetType().GetProperty("value");
                object v = value;
                if (value is string s && valueProp.PropertyType.IsEnum) v = Enum.Parse(valueProp.PropertyType, s);
                valueProp.SetValue(param, v);
                param.GetType().GetProperty("overrideState")?.SetValue(param, true);
            }
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
        }
    }
}
