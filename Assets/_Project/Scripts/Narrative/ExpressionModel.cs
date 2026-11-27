using System;

namespace Jupiter.Narrative
{
    public enum Expression { Neutral, Smile, Sad, Fear, Stern }

    /// <summary>
    /// Reads a line of dialogue for the face to wear while saying it: the stage direction first ("(smiling)",
    /// "(breathing hard)", "(quietly)"), then the words and punctuation. Also gives each expression's targets for
    /// the face bones: brow lift (inner, outer), mouth corners (up, out). Pure logic, unit tested.
    /// </summary>
    public static class ExpressionModel
    {
        static readonly string[] SmileWords = { "smil", "laugh", "grin", "warm", "teas", "chuckl", "love you", "good morning", "ha." };
        static readonly string[] SadWords = { "quiet", "soft", "sorry", "tear", "cry", "dead", "gone", "miss", "lost", "goodbye", "alone", "beat" };
        static readonly string[] FearWords = { "breathing hard", "panic", "scream", "shout", "run!", "move!", "they're coming", "here they come", "oh god", "what is that" };
        static readonly string[] SternWords = { "flat", "hard", "cold", "sharp", "order", "commander", "that's enough", "keep walking", "eyes on" };

        public static Expression For(string line)
        {
            if (string.IsNullOrEmpty(line)) return Expression.Neutral;
            string s = line.ToLowerInvariant();
            // A stage direction decides it when there is one.
            int open = s.IndexOf('('), close = open >= 0 ? s.IndexOf(')', open) : -1;
            if (open >= 0 && close > open)
            {
                var e = Match(s.Substring(open + 1, close - open - 1));
                if (e != Expression.Neutral) return e;
            }
            var w = Match(s);
            if (w != Expression.Neutral) return w;
            int bangs = 0;
            foreach (char ch in s) if (ch == '!') bangs++;
            return bangs >= 2 ? Expression.Fear : Expression.Neutral;
        }

        static Expression Match(string s)
        {
            if (Any(s, FearWords)) return Expression.Fear;
            if (Any(s, SmileWords)) return Expression.Smile;
            if (Any(s, SadWords)) return Expression.Sad;
            if (Any(s, SternWords)) return Expression.Stern;
            return Expression.Neutral;
        }

        static bool Any(string s, string[] words)
        {
            foreach (var w in words) if (s.IndexOf(w, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>
        /// Face targets in millimetres along the head's axes: (inner brow up, outer brow up, mouth corner up,
        /// mouth corner out). Sadness lifts the inner brows and drops the corners; fear lifts the whole brow and
        /// pulls the corners out; a smile lifts and widens the corners; sternness lowers the brows.
        /// </summary>
        public static (float innerBrow, float outerBrow, float cornerUp, float cornerOut) Targets(Expression e)
        {
            switch (e)
            {
                case Expression.Smile: return (1f, 1.5f, 3.5f, 2.5f);
                case Expression.Sad: return (3f, -1.5f, -2.5f, 0f);
                case Expression.Fear: return (4f, 3.5f, -1f, 3f);
                case Expression.Stern: return (-2.5f, -1f, -0.5f, -0.5f);
                default: return (0f, 0f, 0f, 0f);
            }
        }
    }
}
