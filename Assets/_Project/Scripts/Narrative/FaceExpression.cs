using System.Collections.Generic;
using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// A crew member's face: while they speak, the brows and mouth corners move to the line's expression
    /// (<see cref="ExpressionModel"/>), then ease back to rest. Bones move along the head's own axes, so it does not
    /// depend on how the exporter oriented them; it runs after the idle motion and lip sync. The dialogue manager
    /// sets the expression by character id.
    /// </summary>
    public class FaceExpression : MonoBehaviour
    {
        [SerializeField] string characterId;
        [SerializeField] Transform head;
        [SerializeField] Transform browInnerL, browInnerR, browOuterL, browOuterR, cornerL, cornerR;
        [Tooltip("Model units per millimetre (the MakeHuman export is in metres).")]
        [SerializeField] float unitsPerMm = 0.001f;
        [SerializeField] float holdSeconds = 2.5f;

        static readonly Dictionary<string, FaceExpression> Faces = new Dictionary<string, FaceExpression>();

        readonly Dictionary<Transform, Vector3> rest = new Dictionary<Transform, Vector3>();
        Vector4 current, target;
        float until;

        public Expression Current { get; private set; }

        /// <summary>Sets the face of whoever is speaking, if they are in the scene.</summary>
        public static void Express(string id, string line)
        {
            if (string.IsNullOrEmpty(id) || !Faces.TryGetValue(id.ToUpperInvariant(), out var f) || f == null) return;
            f.Show(ExpressionModel.For(line));
        }

        public void Show(Expression e)
        {
            Current = e;
            var t = ExpressionModel.Targets(e);
            target = new Vector4(t.innerBrow, t.outerBrow, t.cornerUp, t.cornerOut);
            until = Time.time + holdSeconds;
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(characterId))
            {
                var crew = GetComponentInParent<CrewMember>();
                if (crew != null) characterId = crew.CharacterId;
            }
            foreach (var b in new[] { browInnerL, browInnerR, browOuterL, browOuterR, cornerL, cornerR })
                if (b != null && !rest.ContainsKey(b)) rest[b] = b.localPosition;
        }

        void OnEnable() { if (!string.IsNullOrEmpty(characterId)) Faces[characterId.ToUpperInvariant()] = this; }

        void OnDisable()
        {
            if (!string.IsNullOrEmpty(characterId) && Faces.TryGetValue(characterId.ToUpperInvariant(), out var f) && f == this)
                Faces.Remove(characterId.ToUpperInvariant());
        }

        void LateUpdate()
        {
            if (head == null) return;
            if (Time.time > until && target != Vector4.zero) { target = Vector4.zero; Current = Expression.Neutral; }
            current = Vector4.MoveTowards(current, target, Time.deltaTime * 12f);
            // The model root faces the way the character does; bone axes are whatever the exporter left.
            Vector3 up = transform.up, right = transform.right;
            Move(browInnerL, up * current.x);
            Move(browInnerR, up * current.x);
            Move(browOuterL, up * current.y);
            Move(browOuterR, up * current.y);
            // MakeHuman ".L" is the character's own left (-right); "out" moves each corner away from the middle.
            Move(cornerL, up * current.z - right * current.w);
            Move(cornerR, up * current.z + right * current.w);
        }

        void Move(Transform bone, Vector3 offsetMm)
        {
            if (bone == null || !rest.TryGetValue(bone, out var r)) return;
            var parent = bone.parent;
            Vector3 local = parent != null ? parent.InverseTransformVector(offsetMm * unitsPerMm) : offsetMm * unitsPerMm;
            bone.localPosition = r + local;
        }
    }
}
