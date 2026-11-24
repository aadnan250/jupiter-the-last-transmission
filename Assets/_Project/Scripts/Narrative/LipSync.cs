using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Opens and closes a character's jaw from the loudness of their voice AudioSource.
    /// Amplitude-driven lip sync: cheap (one 256-sample read per frame), works with any voice clip,
    /// and runs after animation so it layers on top of idle motion. The opening axis is derived from the
    /// skeleton, so it works regardless of how the exporter oriented the jaw bone.
    /// </summary>
    public class LipSync : MonoBehaviour
    {
        [SerializeField] AudioSource voice;
        [SerializeField] Transform jaw;
        [SerializeField] float maxOpenDegrees = 13f;
        [SerializeField] float sensitivity = 9f;
        [SerializeField] float openSpeed = 28f;
        [SerializeField] float closeSpeed = 16f;

        readonly float[] samples = new float[256];
        Quaternion jawRest;
        Vector3 openAxis = Vector3.right;
        float open;

        public float Openness => open;

        void Start()
        {
            if (jaw == null) return;
            jawRest = jaw.localRotation;
            openAxis = BoneAxes.LateralAxisThatLowers(jaw, transform);
        }

        void LateUpdate()
        {
            if (jaw == null) return;

            float target = 0f;
            if (voice != null && voice.isPlaying)
            {
                voice.GetOutputData(samples, 0);
                float sum = 0f;
                for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
                target = Mathf.Clamp01(Mathf.Sqrt(sum / samples.Length) * sensitivity);
            }

            float speed = target > open ? openSpeed : closeSpeed;
            open = Mathf.MoveTowards(open, target, speed * Time.deltaTime);
            jaw.localRotation = jawRest * Quaternion.AngleAxis(open * maxOpenDegrees, openAxis);
        }
    }
}
