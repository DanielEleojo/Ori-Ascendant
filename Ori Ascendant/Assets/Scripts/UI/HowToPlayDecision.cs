using OriAscendant.Save;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure derivation of the first-launch how-to-play overlay visibility from the
    /// persisted seenFlags bitmask (Unit 4 / PRD §5.3 discoverability).
    ///
    /// Host-free: no MonoBehaviour, no SaveData reference passed in — only the
    /// scalar int flag field. The controller reads the field, calls this, and
    /// routes the bool to the overlay — every state is pinnable by EditMode tests.
    ///
    /// Mirror of <see cref="ChannelHintDecision"/> in style (no lifetime window
    /// needed here — the overlay stays until tapped or already seen).
    /// </summary>
    public static class HowToPlayDecision
    {
        /// <summary>Returns true when the how-to-play overlay should be shown:
        /// the player has NOT yet tapped it away (<see cref="SeenFlags.HowToPlay"/>
        /// is not set in <paramref name="seenFlags"/>).</summary>
        public static bool ShouldShow(int seenFlags) =>
            (seenFlags & SeenFlags.HowToPlay) == 0;
    }
}
