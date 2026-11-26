using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// Procedural animation for a suited astronaut built from parts (see AstronautBuilder): pivots named Hips,
    /// Torso, Helmet, Shoulder.L/R, Elbow.L/R, Thigh.L/R and Knee.L/R, each bending around its local X axis.
    /// Walking uses the low-gravity lope of the Apollo films: long strides, a slow rise and fall. Aiming raises
    /// the rifle to the shoulder; a hit makes the whole body flinch.
    /// </summary>
    public class SuitAnimator : MonoBehaviour
    {
        [SerializeField] Transform hips, torso, helmet;
        [SerializeField] Transform shoulderL, shoulderR, elbowL, elbowR;
        [SerializeField] Transform thighL, thighR, kneeL, kneeR;
        [SerializeField] float strideRate = 1.5f;     // cycles per second at walking speed
        [SerializeField] float bobHeight = 0.12f;

        Vector3 hipsRest;
        float phase, aimBlend, flinch;

        /// <summary>Ground speed in m/s, set by whoever moves the body.</summary>
        public float Speed { get; set; }
        public bool Aiming { get; set; }

        void Awake() { if (hips != null) hipsRest = hips.localPosition; }

        public void Flinch() => flinch = 1f;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            float move = Mathf.Clamp01(Speed / 2f);
            phase += dt * strideRate * Mathf.Lerp(0.3f, 1.4f, move) * Mathf.PI * 2f;
            aimBlend = Mathf.MoveTowards(aimBlend, Aiming ? 1f : 0f, dt * 4f);
            flinch = Mathf.MoveTowards(flinch, 0f, dt * 3f);

            float s = Mathf.Sin(phase);
            float stride = 32f * move;
            Rot(thighL, -stride * s);
            Rot(thighR, stride * s);
            Rot(kneeL, 10f + 30f * move * Mathf.Max(0f, -s));
            Rot(kneeR, 10f + 30f * move * Mathf.Max(0f, s));

            // Low-ready while walking, shouldered while aiming.
            float shoulder = Mathf.Lerp(-35f, -82f, aimBlend);
            float elbow = Mathf.Lerp(-55f, -12f, aimBlend);
            Rot(shoulderR, shoulder + 5f * move * s);
            Rot(shoulderL, shoulder - 8f - 5f * move * s);
            Rot(elbowR, elbow);
            Rot(elbowL, elbow - 15f);

            float breathe = Mathf.Sin(Time.time * 1.3f) * 1.5f;
            Rot(torso, 6f * move + breathe - 20f * flinch);
            Rot(helmet, -4f * aimBlend + 10f * flinch);
            if (hips != null)
                hips.localPosition = hipsRest + Vector3.up * (bobHeight * move * Mathf.Abs(Mathf.Sin(phase)));
        }

        static void Rot(Transform t, float x)
        {
            if (t != null) t.localRotation = Quaternion.Euler(x, 0f, 0f);
        }
    }
}
