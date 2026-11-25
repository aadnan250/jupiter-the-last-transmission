using System.Collections.Generic;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// A fired flare: a small physics object with a red light that burns for <see cref="FlareModel.BurnSeconds"/>.
    /// Creatures read <see cref="Active"/> to be drawn to it (heat and light) and dazzled up close.
    /// A static registry, like Rime's: flares register for exactly their own burning lifetime.
    /// No shadows, to stay inside the Iris Xe light budget.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BurningFlare : MonoBehaviour
    {
        static readonly List<BurningFlare> active = new List<BurningFlare>();

        /// <summary>Every flare that is still burning.</summary>
        public static IReadOnlyList<BurningFlare> Active => active;

        [SerializeField] Light glow;
        [SerializeField] float lightIntensity = 4f;
        [SerializeField] float lingerSeconds = 20f;

        public FlareModel Model { get; private set; } = new FlareModel();
        public bool Burning => Model.Burning;
        public Vector3 Position => transform.position;

        float outTime = -1f;

        /// <summary>Builds a flare at runtime (the grey-box has no flare prefab) and launches it.</summary>
        public static BurningFlare Spawn(Vector3 position, Vector3 velocity, Material material, float burnSeconds, Collider ignore = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Flare";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.09f;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            var col = go.GetComponent<Collider>();
            if (ignore != null) Physics.IgnoreCollision(col, ignore, true);

            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.2f;
            body.linearDamping = 0.2f;
            body.angularDamping = 2f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = velocity;

            var lightGo = new GameObject("FlareLight");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.28f, 0.16f);
            light.range = 9f;
            light.shadows = LightShadows.None;

            var flare = go.AddComponent<BurningFlare>();
            flare.glow = light;
            flare.Model = new FlareModel(burnSeconds);
            light.intensity = flare.lightIntensity;
            return flare;
        }

        void OnEnable() { if (Model.Burning && !active.Contains(this)) active.Add(this); }
        void OnDisable() => active.Remove(this);

        void Update()
        {
            Model.Tick(Time.deltaTime);
            if (glow != null)
            {
                // A sputtering flame, not a steady bulb.
                float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 9f, transform.position.x);
                glow.intensity = lightIntensity * Model.Intensity * flicker;
            }

            if (Model.Burning) return;
            if (outTime < 0f)
            {
                outTime = Time.time;
                active.Remove(this);
                if (glow != null) glow.enabled = false;
            }
            else if (Time.time - outTime > lingerSeconds) Destroy(gameObject);
        }
    }
}
