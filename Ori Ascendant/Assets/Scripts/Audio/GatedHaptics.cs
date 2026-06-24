namespace OriAscendant.Audio
{
    /// <summary>
    /// Wraps any IHapticFeedback and checks HapticPrefs before forwarding (issue #31).
    /// Injected by AudioManager so HapticRouter stays a pure routing table.
    /// Reads the pref at call time — toggle changes take effect immediately.
    /// </summary>
    public sealed class GatedHaptics : IHapticFeedback
    {
        private readonly IHapticFeedback _inner;

        public GatedHaptics(IHapticFeedback inner) => _inner = inner;

        public void Impact(ImpactStyle style)
        {
            if (HapticPrefs.HapticsEnabled) _inner.Impact(style);
        }

        public void Notify(NotificationStyle style)
        {
            if (HapticPrefs.HapticsEnabled) _inner.Notify(style);
        }

        public void Select()
        {
            if (HapticPrefs.HapticsEnabled) _inner.Select();
        }
    }
}
