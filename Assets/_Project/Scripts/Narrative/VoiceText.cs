using System.Text.RegularExpressions;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Turns a subtitle into text a voice should speak: stage directions such as "(laughing)" are dropped,
    /// "(beat)" becomes a pause, and a few written forms are spelled the way they are said.
    /// Returns an empty string for lines that are only a stage direction.
    /// </summary>
    public static class VoiceText
    {
        static readonly Regex Beat = new Regex(@"\((long\s+)?beat\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex Direction = new Regex(@"\([^)]*\)", RegexOptions.Compiled);
        static readonly Regex LetterDigit = new Regex(@"\b([A-Z])-(\d+)\b", RegexOptions.Compiled);
        static readonly Regex Spaces = new Regex(@"\s+", RegexOptions.Compiled);
        static readonly Regex LeadingPause = new Regex(@"^(\.\.\.\s*)+", RegexOptions.Compiled);

        public static string Clean(string subtitle)
        {
            if (string.IsNullOrWhiteSpace(subtitle)) return string.Empty;
            string s = Beat.Replace(subtitle, " ... ");
            s = Direction.Replace(s, " ");
            s = LetterDigit.Replace(s, "$1 $2");       // "C-4" -> "C 4"
            s = s.Replace("\"", "");
            s = Spaces.Replace(s, " ").Trim();
            s = LeadingPause.Replace(s, "").Trim();      // a pause before the first word is just silence
            return s.Trim('.', ' ').Length == 0 ? string.Empty : s;
        }
    }
}
