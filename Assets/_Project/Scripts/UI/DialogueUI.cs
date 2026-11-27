using System.Collections.Generic;
using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Jupiter.UI
{
    /// <summary>
    /// Subtitle panel with a typewriter effect and up to four reply rows.
    /// Each reply shows its button: 1-4 on keyboard, or Triangle / Circle / Cross / Square on PlayStation.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text speakerText;
        [SerializeField] Text lineText;
        [SerializeField] Text continueHint;
        [SerializeField] Text[] choiceTexts = new Text[4];
        [SerializeField] float charactersPerSecond = 45f;

        string fullText = string.Empty;
        float visibleChars;
        IReadOnlyList<DialogueChoice> shownChoices;

        public bool IsTyping => root != null && root.activeSelf && lineText != null && lineText.enabled && visibleChars < fullText.Length;

        int[] baseSizes;

        void Awake() => Hide();

        Text[] ScaledTexts()
        {
            var list = new List<Text> { speakerText, lineText, continueHint };
            if (choiceTexts != null) list.AddRange(choiceTexts);
            return list.ToArray();
        }

        /// <summary>Scales the subtitle, speaker and reply text (the accessibility subtitle size option).</summary>
        public void SetTextScale(float scale)
        {
            var texts = ScaledTexts();
            if (baseSizes == null)
            {
                baseSizes = new int[texts.Length];
                for (int i = 0; i < texts.Length; i++) baseSizes[i] = texts[i] != null ? texts[i].fontSize : 0;
            }
            for (int i = 0; i < texts.Length; i++)
                if (texts[i] != null) texts[i].fontSize = Mathf.RoundToInt(baseSizes[i] * scale);
        }

        void OnEnable() => InputDeviceWatcher.Changed += RefreshGlyphs;
        void OnDisable() => InputDeviceWatcher.Changed -= RefreshGlyphs;

        public void ShowLine(string speaker, Color color, string text, bool radio)
        {
            if (root != null) root.SetActive(true);
            HideChoiceRows();
            shownChoices = null;

            if (speakerText != null)
            {
                speakerText.enabled = true;
                speakerText.text = radio ? $"{speaker}  <size=13><color=#9AA4AE>[RADIO]</color></size>" : speaker;
                speakerText.color = color;
            }
            fullText = text ?? string.Empty;
            visibleChars = 0f;
            if (lineText != null)
            {
                lineText.enabled = true;
                lineText.fontStyle = radio ? FontStyle.Italic : FontStyle.Normal;
                lineText.text = string.Empty;
            }
            RefreshGlyphs();
        }

        public void ShowChoices(IReadOnlyList<DialogueChoice> choices)
        {
            if (root != null) root.SetActive(true);
            shownChoices = choices;
            if (speakerText != null) speakerText.enabled = false;
            if (lineText != null) lineText.enabled = false;
            if (continueHint != null) continueHint.enabled = false;
            fullText = string.Empty;
            HideChoiceRows();
            RefreshGlyphs();
        }

        public void CompleteTyping()
        {
            visibleChars = fullText.Length;
            if (lineText != null) lineText.text = fullText;
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            shownChoices = null;
        }

        void Update()
        {
            if (!IsTyping || Time.timeScale == 0f) return;   // paused
            visibleChars = Mathf.Min(fullText.Length, visibleChars + charactersPerSecond * Time.unscaledDeltaTime);
            lineText.text = fullText.Substring(0, Mathf.FloorToInt(visibleChars));
        }

        void RefreshGlyphs()
        {
            if (continueHint != null)
            {
                continueHint.enabled = shownChoices == null;
                continueHint.text = $"[{ButtonGlyphs.Advance}]  continue";
            }
            if (shownChoices == null) return;
            for (int i = 0; i < choiceTexts.Length; i++)
            {
                if (choiceTexts[i] == null) continue;
                bool used = i < shownChoices.Count;
                choiceTexts[i].enabled = used;
                if (used) choiceTexts[i].text = $"{ButtonGlyphs.Choice(i)}   {shownChoices[i].Text}";
            }
        }

        /// <summary>Hides every reply row. Rows in use are re-enabled by <see cref="RefreshGlyphs"/>.</summary>
        void HideChoiceRows()
        {
            foreach (var t in choiceTexts)
                if (t != null) t.enabled = false;
        }
    }
}
