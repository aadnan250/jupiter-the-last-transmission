using System.Collections.Generic;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Audio
{
    /// <summary>
    /// No air, no sound. In vacuum, every positional sound in the world (creatures, machinery, the beam's hiss on
    /// the ice) comes to Sam only as a dull thud through his boots and suit: low-passed and quieter. His own
    /// breathing, footsteps and the radio are on the rig and stay clear. Back in air, the world sounds normal.
    /// </summary>
    public class VacuumAudio : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] float cutoffHz = 900f;
        [SerializeField] float volumeScale = 0.7f;
        [SerializeField] float rescanSeconds = 0.5f;

        readonly Dictionary<AudioSource, float> muffled = new Dictionary<AudioSource, float>();
        float nextScan;
        bool wasVacuum;

        public bool InVacuum => player != null && player.Environment != null && player.Environment.isVacuum;
        public int MuffledCount => muffled.Count;

        void Awake() { if (player == null) player = GetComponent<PlayerController>(); }

        void Update()
        {
            bool vacuum = InVacuum;
            if (vacuum != wasVacuum) { wasVacuum = vacuum; if (!vacuum) RestoreAll(); nextScan = 0f; }
            if (!vacuum || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + rescanSeconds;
            foreach (var src in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
            {
                if (src == null || muffled.ContainsKey(src) || src.spatialBlend < 0.5f) continue;
                if (src.transform.IsChildOf(transform)) continue;   // Sam's own sounds
                var lp = src.GetComponent<AudioLowPassFilter>();
                if (lp == null) lp = src.gameObject.AddComponent<AudioLowPassFilter>();
                lp.enabled = true;
                lp.cutoffFrequency = cutoffHz;
                muffled[src] = src.volume;
                src.volume *= volumeScale;
            }
        }

        void RestoreAll()
        {
            foreach (var pair in muffled)
            {
                if (pair.Key == null) continue;
                pair.Key.volume = pair.Value;
                var lp = pair.Key.GetComponent<AudioLowPassFilter>();
                if (lp != null) lp.enabled = false;
            }
            muffled.Clear();
        }

        void OnDisable() => RestoreAll();
    }
}
