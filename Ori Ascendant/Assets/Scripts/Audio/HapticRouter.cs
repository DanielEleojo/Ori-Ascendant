namespace OriAscendant.Audio
{
    /// <summary>
    /// Pure static routing table: game event → haptic intent (issue #21).
    /// AudioManager delegates to these methods so the mapping is testable
    /// without a MonoBehaviour or a device — a SpyHaptics can be injected
    /// in place of iOSHaptics.
    ///
    /// ART_BIBLE 3.2: a Fall is not failure. RouteFall uses Impact(Light),
    /// never Notify(Warning).
    /// </summary>
    public static class HapticRouter
    {
        public static void RouteChanneled(IHapticFeedback h) => h.Select();

        public static void RouteStageAdvanced(IHapticFeedback h) => h.Impact(ImpactStyle.Medium);

        public static void RouteTribulationComplete(IHapticFeedback h, bool didAscend)
        {
            if (didAscend) RouteAscended(h);
            else RouteFall(h);
        }

        public static void RouteAscended(IHapticFeedback h) => h.Notify(NotificationStyle.Success);

        // ART_BIBLE 3.2: soft landing, not a harsh error
        public static void RouteFall(IHapticFeedback h) => h.Impact(ImpactStyle.Light);

        public static void RouteAncestorStarIgnite(IHapticFeedback h) => h.Impact(ImpactStyle.Light);

        /// <summary>Ọjà contest resolution (issue #38). A loss mirrors RouteFall —
        /// soft Impact(Light), never Notify(Warning): losing a contest is sandboxed
        /// from core progression and must land soft, not read as an error.</summary>
        public static void RouteContestResolved(IHapticFeedback h, bool didWin)
        {
            if (didWin) h.Notify(NotificationStyle.Success);
            else h.Impact(ImpactStyle.Light);
        }
    }
}
