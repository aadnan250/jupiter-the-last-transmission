using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Procedural "alive" motion for a rigged character without animation clips:
    /// lowers the arms from the export A-pose to a relaxed stance, breathes, shifts weight, blinks,
    /// and turns the head toward the player when they are close. Rotation axes are derived from the
    /// skeleton (<see cref="BoneAxes"/>), and any missing bone is simply skipped.
    /// </summary>
    public class CharacterIdleMotion : MonoBehaviour
    {
        [Header("Bones")]
        [SerializeField] Transform spine;
        [SerializeField] Transform neck;
        [SerializeField] Transform head;
        [SerializeField] Transform upperArmL, upperArmR, lowerArmL, lowerArmR;
        [SerializeField] Transform eyelidL, eyelidR;

        [Header("Stance")]
        [Tooltip("How far the arms hang away from vertical, in degrees.")]
        [SerializeField] float armAngleFromVertical = 9f;

        [Header("Life")]
        [SerializeField] float breathRate = 0.22f;      // breaths per second
        [SerializeField] float breathDegrees = 1.4f;
        [SerializeField] float swayDegrees = 0.8f;
        [SerializeField] float blinkDegrees = 22f;

        [Header("Look at player")]
        [SerializeField] float lookRange = 4f;
        [SerializeField] float maxHeadYaw = 55f;
        [SerializeField] float maxHeadPitch = 20f;
        [SerializeField] float lookSpeed = 4f;

        Quaternion spineRest, neckRest, headRest, lidLRest, lidRRest;
        Vector3 lidLAxis, lidRAxis, spineSideAxis, spineFwdAxis;
        Transform player;
        float seed, nextBlink, blinkT = -1f;
        Quaternion headLook = Quaternion.identity;

        void Start()
        {
            // Relax the arms once; nothing else animates them, so the pose persists.
            BoneAxes.LowerArm(upperArmL, lowerArmL, transform, armAngleFromVertical);
            BoneAxes.LowerArm(upperArmR, lowerArmR, transform, armAngleFromVertical);

            seed = Random.value * 100f;
            nextBlink = Time.time + Random.Range(1f, 4f);
            if (spine != null)
            {
                spineRest = spine.localRotation;
                spineSideAxis = Quaternion.Inverse(spine.rotation) * transform.right;
                spineFwdAxis = Quaternion.Inverse(spine.rotation) * transform.forward;
            }
            if (neck != null) neckRest = neck.localRotation;
            if (head != null) headRest = head.localRotation;
            if (eyelidL != null) { lidLRest = eyelidL.localRotation; lidLAxis = BoneAxes.LateralAxisThatLowers(eyelidL, transform); }
            if (eyelidR != null) { lidRRest = eyelidR.localRotation; lidRAxis = BoneAxes.LateralAxisThatLowers(eyelidR, transform); }

            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        void LateUpdate()
        {
            float t = Time.time + seed;

            // Breathing (chest pitches gently) and a slow weight shift (side to side).
            if (spine != null)
            {
                float breath = Mathf.Sin(t * breathRate * Mathf.PI * 2f) * breathDegrees;
                float sway = Mathf.Sin(t * 0.13f) * swayDegrees;
                spine.localRotation = spineRest * Quaternion.AngleAxis(breath, spineSideAxis) * Quaternion.AngleAxis(sway, spineFwdAxis);
            }

            // Head turns toward the player when they are close and roughly in front.
            if (head != null)
            {
                Quaternion want = Quaternion.identity;
                if (player != null)
                {
                    Vector3 to = player.position + Vector3.up * 1.6f - head.position;
                    if (to.magnitude < lookRange)
                    {
                        Vector3 local = transform.InverseTransformDirection(to.normalized);
                        float yawRaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                        if (Mathf.Abs(yawRaw) < 110f)
                        {
                            float yaw = Mathf.Clamp(yawRaw, -maxHeadYaw, maxHeadYaw);
                            float pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg, -maxHeadPitch, maxHeadPitch);
                            want = Quaternion.Euler(pitch, yaw, 0f);
                        }
                    }
                }
                headLook = Quaternion.Slerp(headLook, want, 1f - Mathf.Exp(-lookSpeed * Time.deltaTime));

                // Split the turn between neck and head for a natural motion.
                if (neck != null) ApplyCharacterSpace(neck, neckRest, Quaternion.Slerp(Quaternion.identity, headLook, 0.4f));
                ApplyCharacterSpace(head, headRest, Quaternion.Slerp(Quaternion.identity, headLook, 0.6f));
            }

            // Blinking every 2-5.5 seconds.
            if (blinkT < 0f && Time.time >= nextBlink) blinkT = 0f;
            float lid = 0f;
            if (blinkT >= 0f)
            {
                blinkT += Time.deltaTime / 0.15f;
                lid = Mathf.Sin(Mathf.Clamp01(blinkT) * Mathf.PI);
                if (blinkT >= 1f) { blinkT = -1f; nextBlink = Time.time + Random.Range(2f, 5.5f); }
            }
            if (eyelidL != null) eyelidL.localRotation = lidLRest * Quaternion.AngleAxis(lid * blinkDegrees, lidLAxis);
            if (eyelidR != null) eyelidR.localRotation = lidRRest * Quaternion.AngleAxis(lid * blinkDegrees, lidRAxis);
        }

        /// <summary>Applies a rotation expressed in the character's space on top of a bone's rest rotation.</summary>
        void ApplyCharacterSpace(Transform bone, Quaternion rest, Quaternion characterSpaceOffset)
        {
            bone.localRotation = rest;
            Quaternion world = transform.rotation * characterSpaceOffset * Quaternion.Inverse(transform.rotation);
            bone.rotation = world * bone.rotation;
        }
    }
}
