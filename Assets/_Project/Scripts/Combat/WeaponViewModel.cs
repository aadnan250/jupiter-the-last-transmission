using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// Sam's suited arms and the beam rifle in first person. Shown only while the rifle is equipped. It sways
    /// against the look direction, bobs with walking, kicks and shudders while the beam fires, and dips while a
    /// cell is swapped. Pure presentation: aiming and damage come from the camera (<see cref="BeamRifle"/>).
    /// </summary>
    public class WeaponViewModel : MonoBehaviour
    {
        [SerializeField] BeamRifle rifle;
        [SerializeField] InputReader input;
        [SerializeField] PlayerController player;
        [SerializeField] GameObject visual;
        [SerializeField] Renderer[] glowParts;
        [SerializeField] Color glowColor = new Color(0.3f, 0.85f, 1f);
        [SerializeField] float swayAmount = 0.012f;
        [SerializeField] float swaySmoothing = 10f;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock block;
        Vector3 restPos;
        Quaternion restRot;
        Vector2 sway;
        float bobPhase, kick, reloadDip;

        void Awake()
        {
            restPos = transform.localPosition;
            restRot = transform.localRotation;
            if (input == null) input = GetComponentInParent<InputReader>();
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (rifle == null) rifle = GetComponentInParent<BeamRifle>();
        }

        void LateUpdate()
        {
            bool show = rifle != null && rifle.Equipped;
            if (visual != null && visual.activeSelf != show) visual.SetActive(show);
            if (!show) return;

            float dt = Time.deltaTime;
            Vector2 look = input != null ? input.LookPointer * 0.02f + input.LookStick * 0.6f : Vector2.zero;
            sway = Vector2.Lerp(sway, -Vector2.ClampMagnitude(look, 1f) * swayAmount, 1f - Mathf.Exp(-swaySmoothing * dt));

            float speed = player != null ? player.HorizontalVelocity.magnitude : 0f;
            if (player != null && player.IsGrounded) bobPhase += speed * dt * 5f;
            Vector3 bob = new Vector3(Mathf.Cos(bobPhase * 0.5f) * 0.008f, Mathf.Sin(bobPhase) * 0.006f, 0f) * Mathf.Clamp01(speed / 2f);

            bool firing = rifle.Model.State == BeamState.Firing;
            kick = Mathf.MoveTowards(kick, firing ? 1f : 0f, dt * (firing ? 12f : 4f));
            reloadDip = Mathf.MoveTowards(reloadDip, rifle.Model.State == BeamState.Reloading ? 1f : 0f, dt * 4f);
            Vector3 shudder = firing ? Random.insideUnitSphere * 0.0025f : Vector3.zero;

            transform.localPosition = restPos + new Vector3(sway.x, sway.y, 0f) + bob + shudder
                                      + new Vector3(0f, -0.09f * reloadDip, -0.03f * kick);
            transform.localRotation = restRot * Quaternion.Euler(-2.5f * kick + 25f * reloadDip, 0f, -18f * reloadDip);

            if (glowParts == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            float charge = rifle.Model.ChargeFraction;
            float pulse = firing ? 5f + 2f * Mathf.Sin(Time.time * 40f) : 1.2f + 0.3f * Mathf.Sin(Time.time * 2f);
            foreach (var r in glowParts)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(EmissionId, glowColor * pulse * Mathf.Lerp(0.2f, 1f, charge));
                r.SetPropertyBlock(block);
            }
        }
    }
}
