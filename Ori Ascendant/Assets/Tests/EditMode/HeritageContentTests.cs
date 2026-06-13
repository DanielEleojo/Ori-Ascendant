using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate D: the heritage statement + glossary satisfy the §7 cultural
    /// requirements — homage framing present, all three traditions named and
    /// labelled distinctly, each Path covered.
    /// </summary>
    public class HeritageContentTests
    {
        [Test]
        public void Heritage_IsHomageAndNamesAllThreeTraditions()
        {
            string h = HeritageContent.Heritage;
            Assert.IsNotEmpty(h);
            StringAssert.Contains("homage", h.ToLowerInvariant());
            StringAssert.Contains("Igala", h);
            StringAssert.Contains("Yoruba", h);
            StringAssert.Contains("Igbo", h);
        }

        [Test]
        public void Glossary_IsPopulated_WithPronunciationAndMeaning()
        {
            Assert.IsNotEmpty(HeritageContent.Glossary);
            foreach (var term in HeritageContent.Glossary)
            {
                Assert.IsNotEmpty(term.Word, "glossary term needs a word");
                Assert.IsNotEmpty(term.Pronunciation, $"{term.Word} needs a pronunciation");
                Assert.IsNotEmpty(term.Meaning, $"{term.Word} needs a meaning");
                Assert.IsNotEmpty(term.Tradition, $"{term.Word} needs a labelled tradition (red line §7.7)");
            }
        }

        [Test]
        public void Glossary_CoversCoreConceptsAndEachPath()
        {
            string all = "";
            foreach (var t in HeritageContent.Glossary) all += t.Word + "|";

            StringAssert.Contains("Àṣẹ", all);
            StringAssert.Contains("Orí", all);
            StringAssert.Contains("Ìrékọjá", all);
            StringAssert.Contains("Ane", all);
            StringAssert.Contains("Ṣàngó", all);
            StringAssert.Contains("Ọ̀ṣun", all);
        }

        [Test]
        public void Glossary_LabelsIgalaForAne()
        {
            foreach (var t in HeritageContent.Glossary)
            {
                if (t.Word.StartsWith("Ane"))
                {
                    Assert.AreEqual("Igala", t.Tradition, "Ane is Igala — must not be homogenized (§7.7)");
                    return;
                }
            }
            Assert.Fail("Ane entry missing from glossary");
        }
    }
}
