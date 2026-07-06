using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Crossing-ceremony animation state (issue #34, PRD W3). Owns the ceremony clock and
    /// the outcome stashed at OnTribulationComplete, and outputs the star-ignition and
    /// column-exit values to apply each frame. Pure — no MonoBehaviour, headlessly testable
    /// like OverlayTransition.
    ///
    /// Two-phase trigger (#4): OnTribulationComplete fires while the overlay is still opaque,
    /// so <see cref="Stash"/> records the outcome; ignition only begins at <see cref="Start"/>
    /// (called on OnCeremonyClosed, overlay fully down) so the flash is actually visible. The
    /// owning skin keeps the star/column Images and applies the returned values.
    /// </summary>
    public struct CeremonyDriver
    {
        private float _elapsed;     // seconds since ignition
        private bool _didAscend;
        private int _path;
        private bool _pending;      // outcome stashed, awaiting overlay close
        private bool _ignited;      // Start() has fired — guards the default(struct) state

        /// <summary>Stash the Crossing outcome (overlay still opaque). Ignition waits for Start().</summary>
        public void Stash(bool didAscend, int path)
        {
            _didAscend = didAscend;
            _path = path;
            _pending = true;
        }

        /// <summary>Begin the star ignition once the overlay has fully closed. No-op with no stash.</summary>
        public void Start()
        {
            if (!_pending) return;
            _pending = false;
            _elapsed = 0f;
            _ignited = true;
        }

        /// <summary>True while the ignition flash is still playing — the column hands its glow
        /// to the ceremony during this window.</summary>
        public bool IsActive => _ignited && CrossingCeremonySpec.IsActive(_elapsed);

        /// <summary>Advances the ceremony clock and outputs the new-star alpha, its base colour
        /// (ascended = path colour, fallen = ember), and the column's exit alpha. Returns false
        /// when no ceremony is playing (the skin should clear the star). Under Reduce Motion the
        /// kindling overshoot is skipped: the star holds its settled alpha and the column snaps
        /// out — alpha-only, matching the MotionHelper reduce-motion contract.</summary>
        public bool Tick(float dt, out float starAlpha, out Color starBase, out float columnExitAlpha,
            bool reduceMotion = false) // ponytail: defaulted — pre-RM call sites (headless tests) stay source-compatible
        {
            starAlpha = 0f;
            starBase = default;
            columnExitAlpha = 0f;
            if (!_ignited) return false;
            _elapsed += dt;
            if (!CrossingCeremonySpec.IsActive(_elapsed)) return false;
            starBase = _didAscend && _path >= 0
                ? PathMotif.ColorOf(_path)
                : PathMotif.Ember;
            if (reduceMotion)
            {
                // No flash pulse: the star simply appears settled; the column is already out.
                starAlpha = CrossingCeremonySpec.NewStarAlpha(_didAscend);
                return true;
            }
            starAlpha = CrossingCeremonySpec.StarIgnitionAlpha(
                _elapsed, CrossingCeremonySpec.StarIgnitionSeconds, _didAscend);
            columnExitAlpha = CrossingCeremonySpec.ColumnExitAlpha(
                _elapsed, CrossingCeremonySpec.ColumnFadeSeconds);
            return true;
        }
    }
}
