using UnityEngine;

namespace Jupiter.Creatures
{
    /// <summary>
    /// Poses the Listener placeholder from its AI state: kneeling with palms on the floor and crest flared when
    /// listening, upright and crest raised when hunting, reared back from a flare when blinded, collapsed in torpor
    /// (nodes dimming), still when dead.
    /// Ruptured nodes go dark. On the Blender model the states drive dedicated poses: kneeling to listen, a slow upright stalk, the
    /// loping run, a recoil with the arms across the face, and a slumped torpor distinct from death.
    /// </summary>
    public class ListenerVisual : MonoBehaviour
    {
        [SerializeField] Transform body;      // everything above the legs
        [SerializeField] Transform crest;
        [SerializeField] Renderer[] nodes;
        [SerializeField] CreatureHealth health;
        [Tooltip("When set (the Blender model), states drive this procedural rig instead of the primitive parts.")]
        [SerializeField] StalkerRig rig;
        UnityEngine.AI.NavMeshAgent agent;
        [SerializeField] Color nodeColor = new Color(0.2f, 0.9f, 1f) * 2.5f;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock block;
        Vector3 bodyRestPos, crestRestScale;
        Quaternion bodyRestRot, rootRestRot;
        Vector3 targetOffset, targetCrest = Vector3.one;
        float targetPitch, nodeGlow = 1f, targetGlow = 1f;
        bool[] dead;

        public CreatureState ShownState { get; private set; }

        void Awake()
        {
            if (body != null) { bodyRestPos = body.localPosition; bodyRestRot = body.localRotation; }
            if (crest != null) crestRestScale = crest.localScale;
            dead = new bool[nodes != null ? nodes.Length : 0];
            if (health == null) health = GetComponentInParent<CreatureHealth>();
            agent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
        }

        void OnEnable() { if (health != null) health.NodeRuptured += OnNode; }
        void OnDisable() { if (health != null) health.NodeRuptured -= OnNode; }

        void OnNode(int i) { if (i >= 0 && i < dead.Length) dead[i] = true; }

        public void SetState(CreatureState s)
        {
            ShownState = s;
            if (rig != null) PoseRig(s);
            switch (s)
            {
                case CreatureState.Listening:
                    targetOffset = new Vector3(0f, -0.7f, 0.15f); targetPitch = 28f; targetCrest = new Vector3(1.8f, 1.15f, 1f); targetGlow = 1.3f; break;
                case CreatureState.Hunting:
                case CreatureState.Attacking:
                    targetOffset = new Vector3(0f, -0.15f, 0f); targetPitch = 12f; targetCrest = new Vector3(1.5f, 1.3f, 1f); targetGlow = 1.6f; break;
                case CreatureState.Torpor:
                    targetOffset = new Vector3(0f, -1.6f, 0.6f); targetPitch = 78f; targetCrest = new Vector3(0.6f, 0.5f, 1f); targetGlow = 0.25f; break;
                case CreatureState.Dead:
                    targetOffset = new Vector3(0f, -1.7f, 0.7f); targetPitch = 85f; targetCrest = new Vector3(0.4f, 0.3f, 1f); targetGlow = 0f; break;
                case CreatureState.Blinded:
                    // Recoils from the light: reared back, crest folded flat, nodes dim.
                    targetOffset = new Vector3(0f, -0.25f, -0.35f); targetPitch = -22f; targetCrest = new Vector3(0.55f, 0.5f, 1f); targetGlow = 0.5f; break;
                case CreatureState.Recovering:
                    targetOffset = new Vector3(0f, -0.5f, 0.1f); targetPitch = 20f; targetCrest = Vector3.one; targetGlow = 0.8f; break;
                default:
                    targetOffset = Vector3.zero; targetPitch = 0f; targetCrest = Vector3.one; targetGlow = 1f; break;
            }
        }

        /// <summary>The Listener's states on the creature rig: crouched and still while it listens, loping when it moves.</summary>
        void PoseRig(CreatureState s)
        {
            switch (s)
            {
                case CreatureState.Listening: rig.Pose = StalkerPose.Listen; break;
                case CreatureState.Investigating: rig.Pose = StalkerPose.Stalk; break;
                case CreatureState.Hunting: rig.Pose = StalkerPose.Run; break;
                case CreatureState.Attacking: rig.Pose = StalkerPose.Idle; rig.Attack(); break;
                case CreatureState.Torpor: rig.Pose = StalkerPose.Slump; break;
                case CreatureState.Dead: rig.Pose = StalkerPose.Dead; break;
                case CreatureState.Recovering: rig.Pose = StalkerPose.Emerging; break;
                case CreatureState.Blinded: rig.Pose = StalkerPose.Recoil; rig.Flinch(); break;
                default: rig.Pose = StalkerPose.Idle; break;
            }
        }

        void Update()
        {
            float k = 1f - Mathf.Exp(-6f * Time.deltaTime);
            if (rig != null && agent != null && agent.enabled) rig.Speed = Mathf.Clamp01(agent.velocity.magnitude / 4f);
            if (body != null)
            {
                body.localPosition = Vector3.Lerp(body.localPosition, bodyRestPos + targetOffset, k);
                body.localRotation = Quaternion.Slerp(body.localRotation, bodyRestRot * Quaternion.Euler(targetPitch, 0f, 0f), k);
            }
            if (crest != null) crest.localScale = Vector3.Lerp(crest.localScale, Vector3.Scale(crestRestScale, targetCrest), k);

            // Nodes pulse in the Feature 7 rhythm (four beats, a gap) at roughly 0.4 Hz.
            nodeGlow = Mathf.Lerp(nodeGlow, targetGlow, k);
            float phase = Mathf.Repeat(Time.time * 0.4f, 1f);
            float pulse = phase < 0.5f ? 0.7f + 0.3f * Mathf.Abs(Mathf.Sin(phase * 8f * Mathf.PI)) : 0.6f;
            if (nodes == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] == null) continue;
                nodes[i].GetPropertyBlock(block);
                block.SetColor(EmissionId, dead[i] ? Color.black : nodeColor * nodeGlow * pulse);
                nodes[i].SetPropertyBlock(block);
            }
        }
    }
}
