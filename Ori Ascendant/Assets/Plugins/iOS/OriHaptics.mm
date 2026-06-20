// OriHaptics.mm — Taptic Engine bridge for Ori Ascendant (issue #21, ADR-0004)
// Exposes three C entry points that Unity's P/Invoke layer calls from iOSHaptics.cs.
// Each function creates a fresh generator, calls prepare() then the feedback method,
// and releases immediately — appropriate for infrequent, discrete game events.
//
// ImpactStyle int encoding matches the C# enum: 0=Light, 1=Medium, 2=Heavy.
// NotificationStyle int encoding:              0=Success, 1=Warning.

#import <UIKit/UIKit.h>

extern "C" void OriHaptics_Impact(int style)
{
    UIImpactFeedbackStyle feedbackStyle;
    switch (style) {
        case 1:  feedbackStyle = UIImpactFeedbackStyleMedium; break;
        case 2:  feedbackStyle = UIImpactFeedbackStyleHeavy;  break;
        default: feedbackStyle = UIImpactFeedbackStyleLight;  break;
    }
    UIImpactFeedbackGenerator *gen =
        [[UIImpactFeedbackGenerator alloc] initWithStyle:feedbackStyle];
    [gen prepare];
    [gen impactOccurred];
}

extern "C" void OriHaptics_Notify(int type)
{
    // ART_BIBLE 3.2: a Fall is routed to Impact(Light), so Warning is never
    // used for a fall outcome. This function exists for completeness only.
    UINotificationFeedbackType feedbackType = (type == 1)
        ? UINotificationFeedbackTypeWarning
        : UINotificationFeedbackTypeSuccess;
    UINotificationFeedbackGenerator *gen =
        [[UINotificationFeedbackGenerator alloc] init];
    [gen prepare];
    [gen notificationOccurred:feedbackType];
}

extern "C" void OriHaptics_Select()
{
    UISelectionFeedbackGenerator *gen =
        [[UISelectionFeedbackGenerator alloc] init];
    [gen prepare];
    [gen selectionChanged];
}
