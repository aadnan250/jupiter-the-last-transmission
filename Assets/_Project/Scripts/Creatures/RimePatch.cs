using UnityEngine;

namespace Jupiter.Creatures
{
    /// <summary>
    /// Rime: a filament mat that looks exactly like hoarfrost but is the colony's nervous tissue (story bible §8.4).
    /// Any vibration that starts within <see cref="radius"/> is amplified (radius ×2) and the patch flashes cyan,
    /// the player's visible warning that they have been "heard".
    /// </summary>
    public class RimePatch : MonoBehaviour
    {
        [SerializeField] float radius = 3f;
        [SerializeField] Renderer glow;
        [SerializeField] Color flashColor = new Color(0.25f, 0.95f, 1f) * 3f;
        [SerializeField] float flashSeconds = 1.2f;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock block;
        float flash;

        public float Radius => radius;
        public int FlashCount { get; private set; }

        public bool Contains(Vector3 point)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            return d.magnitude <= radius;
        }

        public void Flash()
        {
            flash = 1f;
            FlashCount++;
        }

        void OnEnable() => VibrationSystem.RegisterRime(this);
        void OnDisable() => VibrationSystem.UnregisterRime(this);

        void Update()
        {
            if (glow == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            flash = Mathf.Max(0f, flash - Time.deltaTime / flashSeconds);
            // A faint idle pulse so attentive players can tell Rime from real frost.
            float idle = 0.06f + 0.04f * Mathf.Sin(Time.time * 0.8f + transform.position.x);
            glow.GetPropertyBlock(block);
            block.SetColor(EmissionId, flashColor * Mathf.Max(idle, flash * flash));
            glow.SetPropertyBlock(block);
        }
    }
}
