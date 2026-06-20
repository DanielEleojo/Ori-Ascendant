# Haptics via a native UIFeedbackGenerator bridge, not a package

The haptic seam (`IHapticFeedback.Light/Medium/Heavy`) currently maps all three to
`Handheld.Vibrate()`, which on iOS is the coarse legacy vibration — not the Taptic Engine — so
the intensities feel identical and cheap. We will reach the real Taptic Engine through a **small
in-repo Objective-C bridge** over Apple's `UIFeedbackGenerator` (impact light/medium/heavy +
notification success/warning + selection tick), rather than adding a third-party haptics package
(e.g. Nice Vibrations / Lofelt).

## Why

- A `UIFeedbackGenerator` wrapper is ~40 lines and zero-dependency — it fits the
  build-it-ourselves, deliberately-lean ethos (cf. [0001](0001-procedural-skin-over-art-pipeline.md)).
- It covers ~90% of need (crisp differentiated impacts + the premium "success" notification for
  an Ascension) without pulling a package into the project.
- The richer semantic set (selection/notification, not just impact) is what makes an iPhone game
  feel native — and it is free from Apple's own API.

## Considered and rejected

- **Nice Vibrations (free, Unity-owned)** — richer (author custom haptic *clips*, cross-platform
  for a future Android port), but a real third-party dependency for capability we don't yet need.
  Revisit if we want a bespoke custom-curve tribulation haptic, or ship on Android.
- **Keep `Handheld.Vibrate()`** — it is the problem; no.

## Consequences

- The bridge is iOS-native (`.mm` under `Assets/Plugins/iOS/`), so haptics can only be *felt* on
  a device via Cloud Build → TestFlight; the editor/Linux path stays on `NullHaptics`. The C#
  seam remains the test boundary.
- `IHapticFeedback` grows beyond Light/Medium/Heavy to carry selection + notification semantics;
  the cultural mapping (a Fall uses a soft pattern, **never** the harsh error haptic — ART_BIBLE
  §3.2) lives behind that seam.
- A bespoke escalating tribulation pattern, if wanted later, extends the same native bridge with
  CoreHaptics — still no package.
