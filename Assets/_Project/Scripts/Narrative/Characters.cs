using System.Collections.Generic;
using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>Display names and subtitle colours for every speaker id used in dialogue scripts.</summary>
    public static class Characters
    {
        static readonly Dictionary<string, (string name, Color color)> Roster = new Dictionary<string, (string, Color)>
        {
            { "SAM", ("Sam", new Color(0.85f, 0.9f, 1f)) },
            { "MARA", ("Mara", new Color(0.55f, 0.85f, 0.95f)) },
            { "LUCA", ("Luca", new Color(1f, 0.78f, 0.4f)) },
            { "HANA", ("Hana", new Color(0.75f, 0.95f, 0.7f)) },
            { "ASHFORD", ("Ashford", new Color(0.95f, 0.9f, 0.7f)) },
            { "GRACE", ("Grace (CAPCOM)", new Color(0.8f, 0.8f, 0.8f)) },
            { "ADA", ("Ada", new Color(0.95f, 0.75f, 0.85f)) },
            { "ZARA", ("Zara", new Color(1f, 0.85f, 0.95f)) },
            { "TOMAS", ("Tomas", new Color(0.8f, 0.8f, 0.95f)) },
        };

        public static string DisplayName(string id) =>
            id != null && Roster.TryGetValue(id.ToUpperInvariant(), out var c) ? c.name : id;

        public static Color SubtitleColor(string id) =>
            id != null && Roster.TryGetValue(id.ToUpperInvariant(), out var c) ? c.color : Color.white;

        public static bool IsKnown(string id) => id != null && Roster.ContainsKey(id.ToUpperInvariant());
    }
}
