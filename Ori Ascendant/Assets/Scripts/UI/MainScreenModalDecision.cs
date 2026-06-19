namespace OriAscendant.UI
{
    /// <summary>Which blocking modal — if any — the main screen should show.</summary>
    public enum MainScreenModal
    {
        None = 0,
        OriVow = 1,
        Crossroads = 2,
    }

    /// <summary>
    /// The main screen's modal-gate as a pure decision (issue #16 / PRD #13 ⑤a):
    /// given the two pieces of save state that the controller used to inspect in
    /// its Update loop — is an Ori vowed? is a crossroads pending? — return the
    /// modal that should be shown, or None.
    ///
    /// Host-free on purpose: no MonoBehaviour, no SaveData reference, no service
    /// lookup. The controller is responsible for reading the inputs from save
    /// state and routing the result back to the modal views. This lets every
    /// combination of the matrix be pinned by EditMode tests without a scene.
    ///
    /// Precedence is fixed: the birth vow (Àkùnlẹ̀yàn) blocks the climb beneath
    /// it, so an unvowed Ori always wins over a pending crossroads — the two
    /// modals never contend.
    /// </summary>
    public static class MainScreenModalDecision
    {
        public static MainScreenModal Decide(bool isOriVowed, bool hasCrossroadsPending)
        {
            if (!isOriVowed) return MainScreenModal.OriVow;
            if (hasCrossroadsPending) return MainScreenModal.Crossroads;
            return MainScreenModal.None;
        }
    }
}
