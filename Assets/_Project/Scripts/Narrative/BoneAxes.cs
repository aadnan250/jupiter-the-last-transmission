using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Works out bone rotation axes from the skeleton itself, so procedural animation does not depend on
    /// how a particular exporter oriented the bones. Bones point along their local Y axis (Blender convention,
    /// preserved by the FBX importer).
    /// </summary>
    public static class BoneAxes
    {
        /// <summary>
        /// The local axis to rotate <paramref name="bone"/> around (positive angles) so that its tip moves down,
        /// pivoting side to side relative to <paramref name="character"/>: a jaw opening, an eyelid closing.
        /// </summary>
        public static Vector3 LateralAxisThatLowers(Transform bone, Transform character)
        {
            Vector3 axisLocal = (Quaternion.Inverse(bone.rotation) * character.right).normalized;
            Vector3 tip = bone.up;
            Vector3 rotated = Quaternion.AngleAxis(10f, bone.rotation * axisLocal) * tip;
            return rotated.y < tip.y ? axisLocal : -axisLocal;
        }

        /// <summary>
        /// Rotates an upper arm so the direction from <paramref name="upper"/> to <paramref name="lower"/> ends up
        /// <paramref name="degreesFromVertical"/> away from the character's down axis, keeping it on the same side.
        /// </summary>
        public static void LowerArm(Transform upper, Transform lower, Transform character, float degreesFromVertical)
        {
            if (upper == null || lower == null) return;
            Vector3 down = -character.up;
            Vector3 current = (lower.position - upper.position).normalized;
            Vector3 side = Vector3.ProjectOnPlane(current, down).normalized;
            if (side.sqrMagnitude < 1e-6f) return;
            float rad = degreesFromVertical * Mathf.Deg2Rad;
            Vector3 target = (down * Mathf.Cos(rad) + side * Mathf.Sin(rad)).normalized;
            upper.rotation = Quaternion.FromToRotation(current, target) * upper.rotation;
        }
    }
}
