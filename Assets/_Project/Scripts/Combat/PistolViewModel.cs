using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// Sam's gloved hands and the pistol in first person, shown while the sidearm is equipped. It sways against the
    /// look, bobs with walking, snaps up and back on each shot, and tilts down for a reload.
    /// </summary>
    public class PistolViewModel : MonoBehaviour
    {
        [SerializeField] Sidearm sidearm;
        [SerializeField] InputReader input;
        [SerializeField] PlayerController player;
        [SerializeField] GameObject visual;

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
            if (sidearm == null) sidearm = GetComponentInParent<Sidearm>();
        }

        void OnEnable() { if (sidearm != null) sidearm.Fired += Kick; }
        void OnDisable() { if (sidearm != null) sidearm.Fired -= Kick; }

        void Kick() => kick = 1f;

        void LateUpdate()
        {
            bool show = sidearm != null && sidearm.Equipped;
            if (visual != null && visual.activeSelf != show) visual.SetActive(show);
            if (!show) return;

            float dt = Time.deltaTime;
            Vector2 look = input != null ? input.LookPointer * 0.02f + input.LookStick * 0.6f : Vector2.zero;
            sway = Vector2.Lerp(sway, -Vector2.ClampMagnitude(look, 1f) * 0.01f, 1f - Mathf.Exp(-10f * dt));
            float speed = player != null ? player.HorizontalVelocity.magnitude : 0f;
            if (player != null && player.IsGrounded) bobPhase += speed * dt * 5f;
            Vector3 bob = new Vector3(Mathf.Cos(bobPhase * 0.5f) * 0.007f, Mathf.Sin(bobPhase) * 0.005f, 0f) * Mathf.Clamp01(speed / 2f);
            kick = Mathf.MoveTowards(kick, 0f, dt * 9f);
            reloadDip = Mathf.MoveTowards(reloadDip, sidearm.Model.State == WeaponState.Reloading ? 1f : 0f, dt * 5f);

            transform.localPosition = restPos + new Vector3(sway.x, sway.y, 0f) + bob + new Vector3(0f, 0.01f * kick - 0.08f * reloadDip, -0.05f * kick);
            transform.localRotation = restRot * Quaternion.Euler(-14f * kick + 30f * reloadDip, 0f, -15f * reloadDip);
        }
    }
}
