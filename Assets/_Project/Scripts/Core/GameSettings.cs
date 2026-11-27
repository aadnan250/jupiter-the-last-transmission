using UnityEngine;

namespace Jupiter.Core
{
    /// <summary>
    /// The player's options: look sensitivity, invert Y, head bob, subtitle size and volume. Plain data with
    /// clamping, so the rules are unit tested; <see cref="Load"/> and <see cref="Save"/> keep it in PlayerPrefs,
    /// so the choice carries across levels and sessions.
    /// </summary>
    public class GameSettings
    {
        public const float MinSensitivity = 0.25f, MaxSensitivity = 3f;
        public const float MinSubtitleScale = 0.8f, MaxSubtitleScale = 1.8f;

        float sensitivity = 1f, subtitleScale = 1f, volume = 1f;

        /// <summary>Multiplier on mouse and stick look speed.</summary>
        public float Sensitivity { get => sensitivity; set => sensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity); }
        public bool InvertY { get; set; }
        public bool HeadBob { get; set; } = true;
        /// <summary>Multiplier on the subtitle and reply text size.</summary>
        public float SubtitleScale { get => subtitleScale; set => subtitleScale = Mathf.Clamp(value, MinSubtitleScale, MaxSubtitleScale); }
        /// <summary>Master volume, 0-1.</summary>
        public float Volume { get => volume; set => volume = Mathf.Clamp01(value); }
        int quality = GraphicsQuality.Medium;
        /// <summary>Graphics preset (<see cref="GraphicsQuality"/>): Medium by default, for integrated graphics.</summary>
        public int Quality { get => quality; set => quality = GraphicsQuality.Clamp(value); }

        const string Prefix = "jupiter.settings.";

        public static GameSettings Load()
        {
            return new GameSettings
            {
                Sensitivity = PlayerPrefs.GetFloat(Prefix + "sensitivity", 1f),
                InvertY = PlayerPrefs.GetInt(Prefix + "invertY", 0) == 1,
                HeadBob = PlayerPrefs.GetInt(Prefix + "headBob", 1) == 1,
                SubtitleScale = PlayerPrefs.GetFloat(Prefix + "subtitleScale", 1f),
                Volume = PlayerPrefs.GetFloat(Prefix + "volume", 1f),
                Quality = PlayerPrefs.GetInt(Prefix + "quality", GraphicsQuality.Medium),
            };
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(Prefix + "sensitivity", Sensitivity);
            PlayerPrefs.SetInt(Prefix + "invertY", InvertY ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "headBob", HeadBob ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "subtitleScale", SubtitleScale);
            PlayerPrefs.SetFloat(Prefix + "volume", Volume);
            PlayerPrefs.SetInt(Prefix + "quality", Quality);
            PlayerPrefs.Save();
        }
    }
}
