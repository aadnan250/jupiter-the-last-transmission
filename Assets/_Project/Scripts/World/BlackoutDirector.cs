using System.Collections.Generic;
using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Chapters 3 and 4 on the Meridian, after the observation deck. Month 11: Sam is sent to the science lab to bring
    /// Tiamat's seismometers online (<see cref="jupiterFlag"/> unlocks the console; the Feature 7 pulse plays through the
    /// speakers). Soon after, the ship blacks out to emergency red (<see cref="blackoutFlag"/>), the radio plays on the
    /// move, and one of three relays at the comms junction has to be reset; <see cref="RelayPanel"/> calls
    /// <see cref="Restore"/> when the right one is. Then a packet from Earth arrives twice.
    /// </summary>
    public class BlackoutDirector : MonoBehaviour
    {
        [SerializeField] string afterConversation = "ch02_observation_radio";
        [SerializeField] string jupiterFlag = "ch3_jupiter";
        [SerializeField] string firstSoundConversation = "ch03_first_sound";
        [SerializeField] string blackoutFlag = "ship_blackout";
        [SerializeField] string restoredFlag = "comms_restored";
        [SerializeField] float blackoutDelay = 6f;
        [SerializeField] TextAsset blackoutRadio;
        [SerializeField] TextAsset restoredRadio;
        [SerializeField] Color emergency = new Color(1f, 0.12f, 0.08f);
        [SerializeField, Range(0f, 1f)] float emergencyLevel = 0.15f;
        [SerializeField] AudioSource alarm;
        [SerializeField] DialogueManager dialogue;

        readonly List<(Light light, float intensity, Color color)> saved = new List<(Light, float, Color)>();
        float waited;
        TextAsset pending;

        public bool BlackedOut { get; private set; }
        public bool Restored { get; private set; }

        void Start() { if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>(); }

        void Update()
        {
            if (dialogue == null) return;
            var story = dialogue.Story;
            if (pending != null && !dialogue.IsActive) { Play(pending); pending = null; }

            if (!story.HasFlag(jupiterFlag) && story.IsCompleted(afterConversation)) story.SetFlag(jupiterFlag);
            if (!BlackedOut && !Restored && story.IsCompleted(firstSoundConversation))
            {
                if ((waited += Time.deltaTime) >= blackoutDelay) Blackout();
            }
        }

        /// <summary>Every ship light drops to emergency red. Public for tests.</summary>
        public void Blackout()
        {
            if (BlackedOut) return;
            BlackedOut = true;
            var playerRoot = FindAnyObjectByType<PlayerController>();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) continue;
                if (playerRoot != null && l.transform.IsChildOf(playerRoot.transform)) continue;
                saved.Add((l, l.intensity, l.color));
                l.intensity *= emergencyLevel;
                l.color = emergency;
            }
            if (alarm != null) alarm.Play();
            dialogue.Story.SetFlag(blackoutFlag);
            Queue(blackoutRadio);
        }

        /// <summary>The right relay is reset: lights and comms come back. Called by <see cref="RelayPanel"/>.</summary>
        public void Restore()
        {
            if (!BlackedOut || Restored) return;
            Restored = true;
            foreach (var (light, intensity, color) in saved)
            {
                if (light == null) continue;
                light.intensity = intensity;
                light.color = color;
            }
            if (alarm != null) alarm.Stop();
            dialogue.Story.SetFlag(restoredFlag);
            Queue(restoredRadio);
        }

        void Queue(TextAsset radio)
        {
            if (radio == null) return;
            if (dialogue.IsActive) pending = radio;
            else Play(radio);
        }

        void Play(TextAsset radio)
        {
            if (dialogue.Story.IsCompleted(radio.name)) return;
            var conv = CrewMember.Load(radio);
            dialogue.PlayOnTheMove(conv, () => dialogue.Story.MarkCompleted(conv.Id));
        }
    }
}
