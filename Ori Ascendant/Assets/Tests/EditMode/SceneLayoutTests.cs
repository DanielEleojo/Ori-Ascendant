using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Pure-math assertions on <see cref="MainScreenLayout"/> band constants.
    /// No scene build, no Canvas, no Unity host required — runs headlessly.
    ///
    /// Gates:
    ///   Counter sub-bands strictly ordered and non-overlapping.
    ///   Identity lanes non-overlapping (no two horizontal bands share x space).
    ///   Modal title / prompt / options / confirm ordered and non-overlapping.
    ///   Each option/card band ≥ the 56 px-equivalent minimum fraction.
    /// </summary>
    public class SceneLayoutTests
    {
        // ===== Counter =====

        [Test]
        public void Counter_EyebrowAboveNumber()
        {
            Assert.Greater(MainScreenLayout.CounterEyebrowBottom,
                           MainScreenLayout.CounterNumberTop,
                           "Eyebrow bottom must clear the top of the number band.");
        }

        [Test]
        public void Counter_NumberAboveHairline()
        {
            Assert.Greater(MainScreenLayout.CounterNumberBottom,
                           MainScreenLayout.CounterHairlineY,
                           "Number bottom must be above the hairline anchor Y.");
        }

        [Test]
        public void Counter_HairlineAboveRate()
        {
            Assert.Greater(MainScreenLayout.CounterHairlineY,
                           MainScreenLayout.CounterRateTop,
                           "Hairline Y must be above the rate band top.");
        }

        [Test]
        public void Counter_RateBottomValid()
        {
            Assert.GreaterOrEqual(MainScreenLayout.CounterRateBottom, 0f);
            Assert.Less(MainScreenLayout.CounterRateBottom,
                        MainScreenLayout.CounterRateTop,
                        "Rate band must have positive height.");
        }

        [Test]
        public void Counter_EyebrowBandNonZeroHeight()
        {
            Assert.Greater(MainScreenLayout.CounterEyebrowTop - MainScreenLayout.CounterEyebrowBottom, 0f);
        }

        [Test]
        public void Counter_NumberBandNonZeroHeight()
        {
            Assert.Greater(MainScreenLayout.CounterNumberTop - MainScreenLayout.CounterNumberBottom, 0f);
        }

        // ===== Identity lanes =====

        [Test]
        public void Identity_OriBadgeAndStageCentreDoNotOverlap()
        {
            // OriBadge ends at IdentityOriBadgeXMax; StageCentre starts at IdentityStageCentreXMin.
            Assert.LessOrEqual(MainScreenLayout.IdentityOriBadgeXMax,
                               MainScreenLayout.IdentityStageCentreXMin,
                               "OriBadge right edge must not enter the StageText centre lane.");
        }

        [Test]
        public void Identity_StageCentreAndPathBadgeDoNotOverlap()
        {
            // StageCentre ends at IdentityStageCentreXMax; PathBadge starts at IdentityPathBadgeXMin.
            Assert.LessOrEqual(MainScreenLayout.IdentityStageCentreXMax,
                               MainScreenLayout.IdentityPathBadgeXMin,
                               "StageText centre lane right edge must not enter the PathBadge right lane.");
        }

        [Test]
        public void Identity_SteadfastnessTopBelowMidpoint()
        {
            // SteadfastnessText lives in the centre-bottom, so its top stays below 0.5.
            Assert.Less(MainScreenLayout.IdentitySteadfastnessTop, 0.5f,
                        "SteadfastnessText sub-lane should occupy the lower portion of the zone.");
        }

        // ===== Modal anatomy (Path / Ori): title + 3 cards + confirm, no prompt body =====

        [Test]
        public void Modal_TitleAboveCard0()
        {
            Assert.Greater(MainScreenLayout.ModalTitleBottom,
                           MainScreenLayout.ModalCard0Top,
                           "Modal title bottom must clear the top card top.");
        }

        [Test]
        public void Modal_Card0AboveCard1()
        {
            Assert.GreaterOrEqual(MainScreenLayout.ModalCard0Bottom,
                                  MainScreenLayout.ModalCard1Top,
                                  "Card 0 bottom must not overlap Card 1 top.");
        }

        [Test]
        public void Modal_Card1AboveCard2()
        {
            Assert.GreaterOrEqual(MainScreenLayout.ModalCard1Bottom,
                                  MainScreenLayout.ModalCard2Top,
                                  "Card 1 bottom must not overlap Card 2 top.");
        }

        [Test]
        public void Modal_Card2AboveConfirm()
        {
            Assert.Greater(MainScreenLayout.ModalCard2Bottom,
                           MainScreenLayout.ModalConfirmTop,
                           "Card 2 bottom must clear the confirm band top.");
        }

        [Test]
        public void Modal_ConfirmBottomValid()
        {
            Assert.GreaterOrEqual(MainScreenLayout.ModalConfirmBottom, 0f);
            Assert.Less(MainScreenLayout.ModalConfirmBottom, MainScreenLayout.ModalConfirmTop);
        }

        [Test]
        public void Modal_EachCard_MeetsTapMinimum()
        {
            float min = MainScreenLayout.MinTapBandFraction;
            Assert.GreaterOrEqual(MainScreenLayout.ModalCard0Top - MainScreenLayout.ModalCard0Bottom, min,
                "Card 0 band must meet 56 px-equivalent minimum.");
            Assert.GreaterOrEqual(MainScreenLayout.ModalCard1Top - MainScreenLayout.ModalCard1Bottom, min,
                "Card 1 band must meet 56 px-equivalent minimum.");
            Assert.GreaterOrEqual(MainScreenLayout.ModalCard2Top - MainScreenLayout.ModalCard2Bottom, min,
                "Card 2 band must meet 56 px-equivalent minimum.");
        }

        [Test]
        public void Modal_ConfirmBand_MeetsTapMinimum()
        {
            float height = MainScreenLayout.ModalConfirmTop - MainScreenLayout.ModalConfirmBottom;
            Assert.GreaterOrEqual(height, MainScreenLayout.MinTapBandFraction,
                "Confirm band must meet the 56 px-equivalent minimum.");
        }

        // ===== Crossroads modal: title + prompt + 3 options + confirm =====

        [Test]
        public void Crossroads_TitleAbovePrompt()
        {
            Assert.Greater(MainScreenLayout.ModalTitleBottom,
                           MainScreenLayout.CrossroadsPromptTop,
                           "Crossroads title bottom must clear the prompt top.");
        }

        [Test]
        public void Crossroads_PromptAboveOption0()
        {
            Assert.Greater(MainScreenLayout.CrossroadsPromptBottom,
                           MainScreenLayout.CrossroadsOption0Top,
                           "Crossroads prompt bottom must clear option 0 top.");
        }

        [Test]
        public void Crossroads_Option0AboveOption1()
        {
            Assert.GreaterOrEqual(MainScreenLayout.CrossroadsOption0Bottom,
                                  MainScreenLayout.CrossroadsOption1Top,
                                  "Crossroads option 0 must not overlap option 1.");
        }

        [Test]
        public void Crossroads_Option1AboveOption2()
        {
            Assert.GreaterOrEqual(MainScreenLayout.CrossroadsOption1Bottom,
                                  MainScreenLayout.CrossroadsOption2Top,
                                  "Crossroads option 1 must not overlap option 2.");
        }

        [Test]
        public void Crossroads_Option2AboveConfirm()
        {
            Assert.Greater(MainScreenLayout.CrossroadsOption2Bottom,
                           MainScreenLayout.CrossroadsConfirmTop,
                           "Crossroads option 2 must clear the confirm band.");
        }

        [Test]
        public void Crossroads_EachOption_MeetsTapMinimum()
        {
            // Crossroads panel ≈ 741 px; 56/741 ≈ 0.076. Use shared minimum (0.083) — stricter.
            float min = MainScreenLayout.MinTapBandFraction;
            Assert.GreaterOrEqual(MainScreenLayout.CrossroadsOption0Top - MainScreenLayout.CrossroadsOption0Bottom,
                min, "Crossroads option 0 must meet tap minimum.");
            Assert.GreaterOrEqual(MainScreenLayout.CrossroadsOption1Top - MainScreenLayout.CrossroadsOption1Bottom,
                min, "Crossroads option 1 must meet tap minimum.");
            Assert.GreaterOrEqual(MainScreenLayout.CrossroadsOption2Top - MainScreenLayout.CrossroadsOption2Bottom,
                min, "Crossroads option 2 must meet tap minimum.");
        }

        [Test]
        public void Crossroads_ConfirmBand_MeetsTapMinimum()
        {
            float height = MainScreenLayout.CrossroadsConfirmTop - MainScreenLayout.CrossroadsConfirmBottom;
            Assert.GreaterOrEqual(height, MainScreenLayout.MinTapBandFraction,
                "Crossroads confirm band must meet the 56 px-equivalent minimum.");
        }
    }
}
