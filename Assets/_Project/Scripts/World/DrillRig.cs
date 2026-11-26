using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// A portable sampling drill: the bit spins and the rig shudders while it runs, and its work light comes on.
    /// It also thumps through the ice: anything listening below hears it.
    /// </summary>
    public class DrillRig : MonoBehaviour
    {
        [SerializeField] Transform bit;
        [SerializeField] Light workLight;
        [SerializeField] AudioSource hum;
        [SerializeField] float spinDegreesPerSecond = 540f;

        Vector3 restPos;
        float noise;

        public bool Running { get; private set; }

        void Awake()
        {
            if (bit != null) restPos = bit.localPosition;
            Set(false);
        }

        public void Set(bool running)
        {
            Running = running;
            if (workLight != null) workLight.enabled = running;
            if (hum != null) { if (running && !hum.isPlaying) hum.Play(); else if (!running) hum.Stop(); }
        }

        void Update()
        {
            if (!Running || bit == null) return;
            bit.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
            bit.localPosition = restPos + Random.insideUnitSphere * 0.01f;
            if ((noise -= Time.deltaTime) <= 0f)
            {
                noise = 1f;
                Creatures.VibrationSystem.Emit(new Creatures.VibrationEvent(transform.position, 30f, Creatures.VibrationMedium.Ground, true, gameObject));
            }
        }
    }
}
