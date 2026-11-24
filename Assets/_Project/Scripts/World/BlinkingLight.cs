using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Blinks or pulses an emissive renderer (server LEDs, beacons, standby lights) using a MaterialPropertyBlock,
    /// so hundreds of lights can share one material without extra draw-call cost.
    /// </summary>
    public class BlinkingLight : MonoBehaviour
    {
        public enum Mode { Blink, Pulse, Random }

        [SerializeField] Mode mode = Mode.Blink;
        [SerializeField] Color color = new Color(0.3f, 1f, 0.4f);
        [SerializeField] float intensity = 3f;
        [SerializeField] float period = 1.2f;
        [SerializeField, Range(0f, 1f)] float duty = 0.5f;
        [SerializeField] Light linkedLight;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Renderer target;
        MaterialPropertyBlock block;
        float phase;
        float nextRandom;
        bool randomOn;

        void Awake()
        {
            target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            phase = Random.value * period;
        }

        void Update()
        {
            float k;
            float t = Time.time + phase;
            switch (mode)
            {
                case Mode.Pulse:
                    k = 0.25f + 0.75f * (0.5f + 0.5f * Mathf.Sin(t / period * Mathf.PI * 2f));
                    break;
                case Mode.Random:
                    if (Time.time >= nextRandom) { randomOn = Random.value > 0.35f; nextRandom = Time.time + Random.Range(0.05f, period); }
                    k = randomOn ? 1f : 0.1f;
                    break;
                default:
                    k = (t % period) / period < duty ? 1f : 0.08f;
                    break;
            }

            if (target != null)
            {
                target.GetPropertyBlock(block);
                block.SetColor(EmissionId, color * intensity * k);
                target.SetPropertyBlock(block);
            }
            if (linkedLight != null) linkedLight.intensity = k * 1.5f;
        }
    }
}
