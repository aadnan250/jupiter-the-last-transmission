using UnityEngine;

namespace Jupiter.Audio
{
    /// <summary>
    /// A looping ambience emitter that gives a room its identity by ear (vents, equipment hum, monitors).
    /// Starts at a random point in the loop so identical emitters never phase against each other,
    /// and fades in to avoid a pop when the scene loads.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class RoomTone : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float volume = 0.5f;
        [SerializeField] float fadeInSeconds = 1.5f;

        AudioSource source;
        float fade;

        public AudioSource Source => source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        void Start()
        {
            if (source.clip == null) return;
            source.time = Random.Range(0f, source.clip.length);
            source.Play();
        }

        void Update()
        {
            if (fade >= 1f) return;
            fade = fadeInSeconds <= 0f ? 1f : Mathf.Min(1f, fade + Time.deltaTime / fadeInSeconds);
            source.volume = volume * fade;
        }
    }
}
