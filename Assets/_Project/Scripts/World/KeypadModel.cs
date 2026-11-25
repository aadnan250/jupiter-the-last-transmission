using System;

namespace Jupiter.World
{
    /// <summary>
    /// A numeric code lock. Digits are entered one at a time; when the entry reaches the code's length it is
    /// checked automatically (no Enter key, so it works the same on keyboard and controller). Controllers pick
    /// a digit by cycling <see cref="Selected"/> and confirming it. Pure C#.
    /// </summary>
    public class KeypadModel
    {
        public string Code { get; }
        public string Entry { get; private set; } = string.Empty;
        public bool Unlocked { get; private set; }
        public int FailedAttempts { get; private set; }

        /// <summary>The digit a controller would enter next (0-9).</summary>
        public int Selected { get; private set; }

        public KeypadModel(string code)
        {
            if (string.IsNullOrEmpty(code)) throw new ArgumentException("A keypad needs a code.", nameof(code));
            foreach (char c in code)
                if (c < '0' || c > '9') throw new ArgumentException($"Keypad codes are digits only: '{code}'.", nameof(code));
            Code = code;
        }

        /// <summary>Adds a digit. Returns Accepted or Rejected when the entry is complete, otherwise Pending.</summary>
        public KeypadResult Press(int digit)
        {
            if (Unlocked) return KeypadResult.Accepted;
            if (digit < 0 || digit > 9) return KeypadResult.Pending;
            Entry += (char)('0' + digit);
            if (Entry.Length < Code.Length) return KeypadResult.Pending;

            bool ok = Entry == Code;
            Entry = string.Empty;
            if (ok) { Unlocked = true; return KeypadResult.Accepted; }
            FailedAttempts++;
            return KeypadResult.Rejected;
        }

        /// <summary>Removes the last digit. Returns false if there was nothing to remove.</summary>
        public bool Backspace()
        {
            if (Entry.Length == 0) return false;
            Entry = Entry.Substring(0, Entry.Length - 1);
            return true;
        }

        public void Clear() => Entry = string.Empty;

        /// <summary>Moves the controller's selected digit, wrapping 9 → 0 and 0 → 9.</summary>
        public void Cycle(int delta) => Selected = ((Selected + delta) % 10 + 10) % 10;

        public KeypadResult ConfirmSelected() => Press(Selected);

        /// <summary>Restores an already-opened lock (from a save).</summary>
        public void ForceUnlock() { Unlocked = true; Entry = string.Empty; }
    }
}
