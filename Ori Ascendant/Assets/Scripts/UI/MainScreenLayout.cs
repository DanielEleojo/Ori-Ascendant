namespace OriAscendant.UI
{
    /// <summary>
    /// Static band-edge constants for the Main screen's fixed zones.
    /// All values are normalised fractions (0–1) inside the named zone's
    /// local <c>RectTransform</c> space, matching the anchors set by
    /// <c>SceneBuilder.BuildUi</c> so SceneBuilder and tests share one source.
    ///
    /// CounterZone occupies 0.10–0.20 of the 844px canvas height ≈ 84px.
    /// IdentityZone occupies 0.20–0.24 ≈ 34px.
    /// Modal panels are computed from the panel's own local space (0–1).
    ///
    /// Host-free: pure constants, verifiable headlessly.
    /// </summary>
    public static class MainScreenLayout
    {
        // ---- Counter sub-bands (local fracs inside CounterZone, bottom = 0) ----
        // Clean non-overlapping vertical stack: eyebrow / number / hairline / rate.
        // At 84px zone height: eyebrow≈14px · number≈36px · gap+rule≈8px · rate≈18px.

        /// <summary>"ÀṢẸ" eyebrow label — Caption (11pt), spaced caps, gold.</summary>
        public const float CounterEyebrowBottom = 0.84f;
        public const float CounterEyebrowTop    = 1.00f;

        /// <summary>Hero Àṣẹ number — Display (30pt), centered mid-band.</summary>
        public const float CounterNumberBottom  = 0.36f;
        public const float CounterNumberTop     = 0.82f;

        /// <summary>Hairline rule anchor (centre Y) — thin gold separator, no height band needed.</summary>
        public const float CounterHairlineY     = 0.31f;

        /// <summary>Rate text — BodySm (14pt), àṣẹ/s at bottom.</summary>
        public const float CounterRateBottom    = 0.04f;
        public const float CounterRateTop       = 0.29f;

        // ---- Identity lanes (local fracs inside IdentityZone) ----
        // Three horizontal lanes prevent OriBadge / StageText / PathBadge collision.

        /// <summary>OriBadge occupies the left lane; right edge here.</summary>
        public const float IdentityOriBadgeXMax    = 0.28f;

        /// <summary>StageText centre band — left edge.</summary>
        public const float IdentityStageCentreXMin = 0.30f;

        /// <summary>StageText centre band — right edge.</summary>
        public const float IdentityStageCentreXMax = 0.70f;

        /// <summary>PathBadge right lane — left edge.</summary>
        public const float IdentityPathBadgeXMin   = 0.72f;

        /// <summary>SteadfastnessText sub-lane — top fraction (centre-bottom of the zone).</summary>
        public const float IdentitySteadfastnessTop = 0.40f;

        // ---- Modal anatomy bands (local fracs inside the panel RectTransform) ----
        // Shared by Path, Ori, and Crossroads modals.
        // Reference height: Path/Ori panel = 0.80 × 844 ≈ 675 px.
        //   Each option card spans ~0.23 → 155 px ≥ 56 px minimum.

        /// <summary>Modal title band (H1 = 22pt) — top of panel, gold serif.</summary>
        public const float ModalTitleBottom   = 0.91f;
        public const float ModalTitleTop      = 0.99f;

        // Path/Ori modals have no prompt body — the title sits directly above the
        // top card. (Only the Crossroads modal has a prompt; see Crossroads* below.)

        /// <summary>
        /// Option/card slot 0 (top card) band.
        /// Three slots: 0→(0.66–0.89), 1→(0.41–0.64), 2→(0.16–0.39).
        /// Each is 0.23 fraction (≈155 px) — well above the 56 px minimum.
        /// </summary>
        public const float ModalCard0Bottom   = 0.66f;
        public const float ModalCard0Top      = 0.89f;
        public const float ModalCard1Bottom   = 0.41f;
        public const float ModalCard1Top      = 0.64f;
        public const float ModalCard2Bottom   = 0.16f;
        public const float ModalCard2Top      = 0.39f;

        /// <summary>Confirm pill button — bottom of panel (≈67 px ≥ 56 px min).</summary>
        public const float ModalConfirmBottom = 0.03f;
        public const float ModalConfirmTop    = 0.13f;

        // ---- Crossroads modal bands (panel spans y 0.06–0.94 → 741 px) ----
        // Options each 0.17 fraction → ≈126 px — above 56 px minimum.

        public const float CrossroadsPromptBottom  = 0.75f;
        public const float CrossroadsPromptTop     = 0.89f; // clears the title (bottom 0.91)

        public const float CrossroadsOption0Bottom = 0.56f;
        public const float CrossroadsOption0Top    = 0.73f;
        public const float CrossroadsOption1Bottom = 0.37f;
        public const float CrossroadsOption1Top    = 0.54f;
        public const float CrossroadsOption2Bottom = 0.18f;
        public const float CrossroadsOption2Top    = 0.35f;

        public const float CrossroadsConfirmBottom = 0.03f;
        public const float CrossroadsConfirmTop    = 0.13f;

        // ---- Minimum tap-band fraction equivalent of 56 px ----
        // Path/Ori panel height ≈ 675 px; 56/675 ≈ 0.083.
        // Crossroads panel height ≈ 741 px; 56/741 ≈ 0.076.
        // Use the stricter fraction (Path/Ori) as the shared minimum.
        /// <summary>Minimum fraction band height that satisfies the 56 px tap target.</summary>
        public const float MinTapBandFraction = 0.083f;
    }
}
