using System.Collections.Generic;
using UnityEngine;

namespace Jupiter.Creatures
{
    public enum StalkerPose { Buried, Emerging, Idle, Run, Attack, Dead }

    /// <summary>
    /// Procedural animation for the Stalker model (Tools/CreaturePipeline/make_stalker.py names every bone). It
    /// bends the upright rest pose into a hunched crouch, a low loping run on hands and feet, a two-armed slash,
    /// an emergence (clawing up out of the ice) and a collapse. Each bone bends around the creature's own
    /// right/forward axes, worked out once from the rest pose, so it does not depend on how bones were exported.
    /// </summary>
    public class StalkerRig : MonoBehaviour
    {
        [SerializeField] Transform model;           // the skinned model's root, lowered while crouching
        [SerializeField] float crouchDrop = 0.36f;

        class Bone
        {
            public Transform T;
            public Quaternion Rest;
            public Vector3 Pitch, Roll;              // the creature's right and forward axes, in this bone's local space
            public float P, R;                       // current smoothed angles
        }

        readonly Dictionary<string, Bone> bones = new Dictionary<string, Bone>();
        Vector3 modelRest;
        float phase, attackT = 1f, twitchTimer, twitch, flinch;

        public StalkerPose Pose { get; set; } = StalkerPose.Idle;
        /// <summary>0 (standing) to 1 (full run).</summary>
        public float Speed { get; set; }
        public bool HasBones => bones.Count > 0;

        void Awake()
        {
            if (model == null && transform.childCount > 0) model = transform.GetChild(0);
            if (model != null) modelRest = model.localPosition;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t == transform || bones.ContainsKey(t.name)) continue;
                bones[t.name] = new Bone
                {
                    T = t,
                    Rest = t.localRotation,
                    Pitch = Quaternion.Inverse(t.rotation) * transform.right,
                    Roll = Quaternion.Inverse(t.rotation) * transform.forward,
                };
            }
        }

        /// <summary>Starts a two-armed slash (0.45 s).</summary>
        public void Attack() => attackT = 0f;

        /// <summary>A recoil when hit.</summary>
        public void Flinch() => flinch = 1f;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            phase += dt * Mathf.Lerp(1.2f, 9f, Speed);
            attackT = Mathf.Min(1f, attackT + dt / 0.45f);
            flinch = Mathf.MoveTowards(flinch, 0f, dt * 3f);
            if ((twitchTimer -= dt) <= 0f) { twitchTimer = Random.Range(0.6f, 2.4f); twitch = Random.Range(-25f, 25f); }

            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            float drop = crouchDrop;

            switch (Pose)
            {
                case StalkerPose.Dead:
                    Set("hips", 70); Set("spine", 30); Set("chest", 20); Set("neck", -10); Set("head", 10); Set("jaw", 30);
                    Limbs(-95, 150, -60, -10, 40, 30);
                    drop = 0.9f;
                    break;

                case StalkerPose.Emerging:
                    // Clawing up out of the ice: arms thrown up and hauling, head back, jaw wide.
                    Set("hips", 10); Set("spine", 5 + 10 * s); Set("chest", -10); Set("neck", -40); Set("head", -25); Set("jaw", 40);
                    Limbs(-60, 100, -30, -150 + 40 * s, -30 - 20 * c, 25);
                    break;

                case StalkerPose.Run:
                    // Low and fast, nearly on all fours: back legs and long arms reaching in turn.
                    Set("hips", 45 + 6 * c); Set("spine", 25); Set("chest", 20 + 5 * s); Set("neck", -50); Set("head", -25 + twitch * 0.2f);
                    Set("jaw", 25 + 10 * Mathf.Abs(s));
                    Set("thigh.L", -70 + 40 * s); Set("shin.L", 100 - 35 * c); Set("foot.L", -35);
                    Set("thigh.R", -70 - 40 * s); Set("shin.R", 100 + 35 * c); Set("foot.R", -35);
                    Set("upperarm.L", -75 - 45 * s, 8); Set("forearm.L", -20 + 15 * c);
                    Set("upperarm.R", -75 + 45 * s, -8); Set("forearm.R", -20 - 15 * c);
                    drop = crouchDrop + 0.12f + 0.06f * Mathf.Abs(s);
                    break;

                default: // Idle and Attack share the hunched crouch
                    float breathe = Mathf.Sin(Time.time * 1.7f) * 3f;
                    Set("hips", 25); Set("spine", 20 + breathe); Set("chest", 15 + breathe);
                    Set("neck", -35); Set("head", -20, twitch);   // sudden, wrong head tilts
                    Set("jaw", 15 + 5 * Mathf.Sin(Time.time * 7f));
                    Limbs(-70, 110, -40, -30, -25, 10);
                    break;
            }

            if (attackT < 1f)
            {
                // Both arms rake down from overhead.
                float k = Mathf.Sin(attackT * Mathf.PI);
                float sweep = Mathf.Lerp(-150f, 10f, attackT);
                Set("upperarm.L", sweep, 15); Set("upperarm.R", sweep, -15);
                Set("forearm.L", -40 * k); Set("forearm.R", -40 * k);
                Set("jaw", 45);
                Set("chest", 15 + 25 * k);
            }
            if (flinch > 0f) { Set("chest", -25 * flinch); Set("head", -35 * flinch); }

            Apply(dt);
            if (model != null) model.localPosition = Vector3.Lerp(model.localPosition, modelRest + Vector3.down * drop, 1f - Mathf.Exp(-10f * dt));
        }

        void Limbs(float thigh, float shin, float foot, float upperarm, float forearm, float spread)
        {
            Set("thigh.L", thigh); Set("thigh.R", thigh);
            Set("shin.L", shin); Set("shin.R", shin);
            Set("foot.L", foot); Set("foot.R", foot);
            Set("upperarm.L", upperarm, spread); Set("upperarm.R", upperarm, -spread);
            Set("forearm.L", forearm); Set("forearm.R", forearm);
        }

        readonly Dictionary<string, Vector2> targets = new Dictionary<string, Vector2>();

        void Set(string bone, float pitch, float roll = 0f) => targets[bone] = new Vector2(pitch, roll);

        void Apply(float dt)
        {
            float k = 1f - Mathf.Exp(-14f * dt);
            foreach (var kv in bones)
            {
                var b = kv.Value;
                Vector2 target = targets.TryGetValue(kv.Key, out var v) ? v : Vector2.zero;
                b.P = Mathf.Lerp(b.P, target.x, k);
                b.R = Mathf.Lerp(b.R, target.y, k);
                if (Mathf.Abs(b.P) < 0.01f && Mathf.Abs(b.R) < 0.01f) { b.T.localRotation = b.Rest; continue; }
                b.T.localRotation = b.Rest * Quaternion.AngleAxis(b.P, b.Pitch) * Quaternion.AngleAxis(b.R, b.Roll);
            }
            targets.Clear();
        }
    }
}
