using ICSharpCode.TextEditor.Document;
using NUnit.Framework;

namespace ICSharpCode.TextEditor.Tests
{
    [TestFixture]
    public class HighlightingManagerTests
    {
        [TestCase("test.xml", "XML")]
        [TestCase("test.vcxproj.filters", "XML")] //Extension with a '.' inside
        [TestCase("test.cs", "C#")] //lowercase
        [TestCase("test.CS", "C#")] //Upper case
        [TestCase("test.htm", "HTML")]
        public void FindHighlighterForFile_Should_find_Highlight_strategy(string filename, string expectedStrategy)
        {
            IHighlightingStrategy highlightingStrategy = HighlightingManager.Manager.FindHighlighterForFile(filename);
            Assert.AreEqual(expectedStrategy, highlightingStrategy.Name);
        }

        [Test]
        public void FindHighlighterForFile_Should_not_find_Highlight_strategy()
        {
            IHighlightingStrategy highlightingStrategy = HighlightingManager.Manager.FindHighlighterForFile("test.unkown");
            Assert.AreEqual("Default", highlightingStrategy.Name);
        }
    }
}