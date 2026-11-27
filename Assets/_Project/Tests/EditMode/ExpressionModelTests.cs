using Jupiter.Narrative;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for reading a line's expression: directions first, then words, then punctuation.</summary>
    public class ExpressionModelTests
    {
        [TestCase("(smiling) You came back.", Expression.Smile)]
        [TestCase("(breathing hard) That's four. Or five.", Expression.Fear)]
        [TestCase("(quietly) I'm sorry, Sam.", Expression.Sad)]
        [TestCase("Eyes on the beacon. All of you.", Expression.Stern)]
        [TestCase("Move! Move!", Expression.Fear)]
        [TestCase("The hatch takes forty seconds to cycle.", Expression.Neutral)]
        [TestCase("", Expression.Neutral)]
        public void Reads_TheLine(string line, Expression expected) => Assert.AreEqual(expected, ExpressionModel.For(line));

        [Test]
        public void Direction_WinsOverTheWords()
        {
            Assert.AreEqual(Expression.Smile, ExpressionModel.For("(laughing) They're all dead, aren't they."));
        }

        [Test]
        public void Targets_MoveTheFaceTheRightWay()
        {
            Assert.Greater(ExpressionModel.Targets(Expression.Smile).cornerUp, 0f, "a smile lifts the corners");
            Assert.Less(ExpressionModel.Targets(Expression.Sad).cornerUp, 0f, "sadness drops them");
            Assert.Greater(ExpressionModel.Targets(Expression.Sad).innerBrow, 0f, "and lifts the inner brows");
            Assert.Less(ExpressionModel.Targets(Expression.Stern).innerBrow, 0f, "sternness lowers them");
            Assert.AreEqual((0f, 0f, 0f, 0f), ExpressionModel.Targets(Expression.Neutral));
        }
    }
}
