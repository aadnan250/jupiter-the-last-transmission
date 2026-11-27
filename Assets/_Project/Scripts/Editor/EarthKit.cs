using UnityEditor;
using UnityEngine;
using Kit = Jupiter.EditorTools.GreyboxKit;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Shared pieces for the two scenes on Earth (Chapter 1 at dawn, Chapter 10 at golden hour): a physically based
    /// sky with a low sun, a sea to the horizon, a light coastal haze, and the sound of sea and wind.
    /// Earth is the warm, open counterpoint to everything in between.
    /// </summary>
    public static class EarthKit
    {
        public static Light SkyAndSun(string name, float sunElevation, float sunAzimuth, Color sunColor, float intensity, Color skyTint, Color haze)
        {
            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunSize", 0.05f);
            sky.SetFloat("_SunSizeConvergence", 6f);
            sky.SetFloat("_AtmosphereThickness", 1.35f);
            sky.SetColor("_SkyTint", skyTint);
            sky.SetColor("_GroundColor", new Color(0.25f, 0.3f, 0.36f));
            sky.SetFloat("_Exposure", 1.2f);
            string path = $"{Kit.MaterialFolder}/{name}_Sky.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(sky, path);
            RenderSettings.skybox = sky;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(sunElevation, sunAzimuth, 0f);
            sun.color = sunColor;
            sun.intensity = intensity;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = haze;
            RenderSettings.fogDensity = 0.0035f;
            return sun;
        }

        /// <summary>A sea to the horizon: a huge, very smooth plane that mirrors the sky.</summary>
        public static GameObject Ocean(Transform parent, float height)
        {
            var m = Kit.Mat("EARTH_Sea", new Color(0.06f, 0.2f, 0.26f), smoothness: 0.93f);
            m.SetFloat("_Metallic", 0.15f);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(EuropaTextureGenerator.IceNormal);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetTextureScale("_BumpMap", new Vector2(60f, 60f));
                m.SetFloat("_BumpScale", 0.35f);
                m.EnableKeyword("_NORMALMAP");
            }
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Sea";
            Object.DestroyImmediate(sea.GetComponent<Collider>());
            sea.transform.SetParent(parent, false);
            sea.transform.position = new Vector3(0f, height, 0f);
            sea.transform.localScale = new Vector3(400f, 1f, 400f);
            sea.GetComponent<Renderer>().sharedMaterial = m;
            return sea;
        }

        /// <summary>The loop of sea and wind, all around the listener.</summary>
        public static void SeaAndWind(Transform parent, float volume)
        {
            var go = new GameObject("SeaAndWind");
            go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = CombatAudioGenerator.Load(CombatAudioGenerator.OceanWind);
            src.loop = true;
            src.playOnAwake = true;
            src.spatialBlend = 0f;
            src.volume = volume;
        }
    }
}
