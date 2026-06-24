#import <UIKit/UIKit.h>

// OriAccessibility — read the system Reduce Motion flag so Unity can mirror it
// into PlayerPrefs without writing it from native (Unity's PlayerPrefs key
// encoding is version-specific; C# owns all writes). ADR-0004 / issue #5.

extern "C"
{
    bool OriAccessibility_IsReduceMotionEnabled()
    {
        return UIAccessibilityIsReduceMotionEnabled();
    }
}
