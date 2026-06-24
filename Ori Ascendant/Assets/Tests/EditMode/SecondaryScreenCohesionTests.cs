using NUnit.Framework;
using OriAscendant.UI;
using OriAscendant.UI.Screens;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Unit 7 cohesion gate: verify that the layout constants in
    /// ChronicleScreenView.Layout are wired to the correct shared tokens and are
    /// geometrically self-consistent. Pure/host-free — no MonoBehaviour, no Canvas.
    /// </summary>
    public class SecondaryScreenCohesionTests
    {
        // ---- font-size tokens ----

        [Test]
        public void Chronicle_GenLabelFontSize_MatchesTypographicScale_BodySm()
        {
            Assert.AreEqual(
                TypographicScale.BodySm,
                ChronicleScreenView.Layout.GenLabelFontSize,
                $"GenLabelFontSize must equal TypographicScale.BodySm ({TypographicScale.BodySm}pt)");
        }

        [Test]
        public void Chronicle_RemembranceFontSize_MatchesTypographicScale_Label()
        {
            Assert.AreEqual(
                TypographicScale.Label,
                ChronicleScreenView.Layout.RemembranceFontSize,
                $"RemembranceFontSize must equal TypographicScale.Label ({TypographicScale.Label}pt)");
        }

        [Test]
        public void Chronicle_OriNameFontSize_IsPositive_AndBelowBodySm()
        {
            // Ori name uses a bespoke 12f (between Caption=11 and Label=13) — not a token.
            // Gate: must be positive and smaller than the label beside it.
            Assert.Greater(ChronicleScreenView.Layout.OriNameFontSize, 0f,
                "Ori name font size must be positive");
            Assert.Less(ChronicleScreenView.Layout.OriNameFontSize,
                        ChronicleScreenView.Layout.GenLabelFontSize,
                "Ori name must be smaller than the primary label beside it (secondary hierarchy)");
        }

        // ---- spacing token ----

        [Test]
        public void Chronicle_TextLeftMargin_MatchesSpacingScale_Xxl()
        {
            Assert.AreEqual(
                SpacingScale.Xxl,
                ChronicleScreenView.Layout.TextLeftMargin,
                $"TextLeftMargin must equal SpacingScale.Xxl ({SpacingScale.Xxl}px)");
        }

        // ---- geometric self-consistency ----

        [Test]
        public void Chronicle_NodeRowHeight_IsPositive()
        {
            Assert.Greater(ChronicleScreenView.Layout.NodeRowHeight, 0f,
                "node row height must be positive — a zero height collapses the thread");
        }

        [Test]
        public void Chronicle_DotSize_SmallerThanTextLeftMargin()
        {
            // The dot (centred on ThreadX) must sit entirely left of the text column.
            Assert.Less(ChronicleScreenView.Layout.DotSize, ChronicleScreenView.Layout.TextLeftMargin,
                "dot diameter must be less than TextLeftMargin so it never overlaps the label");
        }

        [Test]
        public void Chronicle_ThreadX_SmallerThanTextLeftMargin()
        {
            Assert.Less(ChronicleScreenView.Layout.ThreadX, ChronicleScreenView.Layout.TextLeftMargin,
                "thread x-centre must be left of the text column");
        }

        [Test]
        public void Chronicle_ThreadWidth_SmallerThanDotSize()
        {
            // The thread is a hairline behind the dot; it must be narrower.
            Assert.Less(ChronicleScreenView.Layout.ThreadWidth, ChronicleScreenView.Layout.DotSize,
                "thread line must be narrower than the dot it passes through");
        }

        // ---- token sanity (scale constants themselves are stable) ----

        [Test]
        public void SpacingScale_Xxl_Is48()
        {
            Assert.AreEqual(48f, SpacingScale.Xxl,
                "SpacingScale.Xxl must be 48px per the 4px base scale definition");
        }

        [Test]
        public void TypographicScale_BodySm_Is14()
        {
            Assert.AreEqual(14f, TypographicScale.BodySm,
                "TypographicScale.BodySm must be 14pt");
        }

        [Test]
        public void TypographicScale_Label_Is13()
        {
            Assert.AreEqual(13f, TypographicScale.Label,
                "TypographicScale.Label must be 13pt");
        }
    }
}
