namespace OriAscendant.Audio
{
    /// <summary>Haptic seam (GAMEPLAY §5.2 feedback): light on channel tap, medium
    /// on advance, heavy on Tribulation resolution. No-op off-device so it
    /// compiles and runs on Linux/standalone.</summary>
    public interface IHapticFeedback
    {
        void Light();
        void Medium();
        void Heavy();
    }

    public sealed class NullHaptics : IHapticFeedback
    {
        public void Light() { }
        public void Medium() { }
        public void Heavy() { }
    }

#if UNITY_IOS && !UNITY_EDITOR
    /// <summary>Minimal device haptics. Apple.CoreHaptics (committed with the
    /// Apple plugins) can replace this with sharp/medium/heavy patterns later;
    /// Handheld.Vibrate is the dependency-free MVP stand-in.</summary>
    public sealed class DeviceHaptics : IHapticFeedback
    {
        public void Light() => UnityEngine.Handheld.Vibrate();
        public void Medium() => UnityEngine.Handheld.Vibrate();
        public void Heavy() => UnityEngine.Handheld.Vibrate();
    }
#endif
}
