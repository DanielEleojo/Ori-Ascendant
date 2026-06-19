namespace OriAscendant.UI
{
    /// <summary>Display state of the one-time channel hint derived from persisted save data.</summary>
    public enum ChannelHintState
    {
        /// <summary>Never shown — <see cref="OriAscendant.Save.SaveData.channelHintShownAt"/> is 0.</summary>
        Pending = 0,
        /// <summary>Shown and still within the display lifetime.</summary>
        Active = 1,
        /// <summary>Shown but the display lifetime has elapsed.</summary>
        Expired = 2,
    }

    /// <summary>
    /// Pure derivation of the one-time channel-hint display state from persisted
    /// save data (issue #18 / PRD #13 ⑤b). Given the Unix-seconds timestamp when
    /// the hint was first shown and the current time, returns whether the hint
    /// should be visible.
    ///
    /// Host-free on purpose: no MonoBehaviour, no SaveData reference. The
    /// controller reads the save field, passes it here, and routes the result to
    /// the hint view — every state is pinnable by EditMode tests without a scene.
    /// </summary>
    public static class ChannelHintDecision
    {
        /// <param name="shownAt">
        ///   <see cref="OriAscendant.Save.SaveData.channelHintShownAt"/> —
        ///   0 means the hint has never been shown.
        /// </param>
        /// <param name="nowUtc">Current Unix UTC seconds.</param>
        /// <param name="lifetimeSeconds">Display window after first appearance.</param>
        public static ChannelHintState Evaluate(long shownAt, long nowUtc, long lifetimeSeconds)
        {
            if (shownAt == 0) return ChannelHintState.Pending;
            return (nowUtc - shownAt) < lifetimeSeconds
                ? ChannelHintState.Active
                : ChannelHintState.Expired;
        }
    }
}
