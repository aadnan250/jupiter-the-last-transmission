using Jupiter.Core;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the ending: both variants, the names in order, the credits, and THE END that stays.</summary>
    public class EndingScriptTests
    {
        static bool Shows(EndingScript s, string text)
        {
            for (float t = 0f; t < s.Duration; t += 0.25f) if (s.At(t).Text.Contains(text)) return true;
            return false;
        }

        [Test]
        public void Vented_ShowsTheBeach_NotTheQuarantine()
        {
            var s = new EndingScript(kept: false);
            Assert.IsTrue(Shows(s, "empty beach"));
            Assert.IsFalse(Shows(s, "QUARANTINE"));
        }

        [Test]
        public void Kept_ShowsTheQuarantineMonitor()
        {
            var s = new EndingScript(kept: true);
            Assert.IsTrue(Shows(s, "QUARANTINE"));
            Assert.IsTrue(Shows(s, "0.40 Hz"));
        }

        [Test]
        public void EveryNameAndTheVoiceCreditAppear_InOrder()
        {
            var s = new EndingScript(false);
            float last = -1f;
            foreach (var name in EndingScript.Names)
            {
                float at = -1f;
                for (float t = 0f; t < s.Duration; t += 0.1f) if (s.At(t).Text == name) { at = t; break; }
                Assert.Greater(at, last, name);
                last = at;
            }
            Assert.IsTrue(Shows(s, "CC BY 4.0"), "the voice licence requires credit");
        }

        [Test]
        public void TheEnd_StaysUpAfterTheCredits()
        {
            var s = new EndingScript(true);
            var f = s.At(s.Duration + 100f);
            Assert.AreEqual("THE END", f.Text);
            Assert.AreEqual(1f, f.Black);
            Assert.AreEqual(1f, f.TextAlpha);
        }
    }
}
