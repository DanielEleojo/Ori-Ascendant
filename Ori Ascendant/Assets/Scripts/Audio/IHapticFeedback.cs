#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace OriAscendant.Audio
{
    /// <summary>Impact intensity — maps 1:1 to UIImpactFeedbackStyle on device.</summary>
    public enum ImpactStyle { Light, Medium, Heavy }

    /// <summary>Notification kind — maps to UINotificationFeedbackType on device.
    /// Warning is included for completeness; Fall never uses it (ART_BIBLE 3.2).</summary>
    public enum NotificationStyle { Success, Warning }

    /// <summary>
    /// Haptic seam (issue #21). Carries iOS Taptic Engine semantics so callers
    /// express INTENT (impact / notification / selection) rather than raw weight.
    /// NullHaptics is the editor/Linux no-op; iOSHaptics bridges UIFeedbackGenerator
    /// via P/Invoke to the native OriHaptics.mm plugin.
    /// </summary>
    public interface IHapticFeedback
    {
        void Impact(ImpactStyle style);
        void Notify(NotificationStyle style);
        void Select();
    }

    public sealed class NullHaptics : IHapticFeedback
    {
        public void Impact(ImpactStyle style) { }
        public void Notify(NotificationStyle style) { }
        public void Select() { }
    }

#if UNITY_IOS && !UNITY_EDITOR
    /// <summary>Bridges UIFeedbackGenerator via the OriHaptics.mm native plugin.
    /// ImpactStyle/NotificationStyle int values match the ObjC switch cases.</summary>
    public sealed class iOSHaptics : IHapticFeedback
    {
        [DllImport("__Internal")] static extern void OriHaptics_Impact(int style);
        [DllImport("__Internal")] static extern void OriHaptics_Notify(int type);
        [DllImport("__Internal")] static extern void OriHaptics_Select();

        public void Impact(ImpactStyle style) => OriHaptics_Impact((int)style);
        public void Notify(NotificationStyle style) => OriHaptics_Notify((int)style);
        public void Select() => OriHaptics_Select();
    }
#endif
}
