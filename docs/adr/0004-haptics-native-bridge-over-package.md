# Native bridge for iOS system features over a managed package

iOS system APIs that lack a Unity managed equivalent (haptics, accessibility settings) are reached through a thin native bridge — a C plug-in called via `DllImport("__Internal")` — rather than a third-party managed package.

## Why

- **Exact API surface** — the bridge exposes only what the game uses, with no deprecation risk from a package's opinion of the same API.
- **No App Store review risk** — the plug-in is a thin shim over documented Apple frameworks (UIImpactFeedbackGenerator, UIAccessibility); no private API use.
- **Compile-time safety on Linux** — the `DllImport` path is wrapped in `#if UNITY_IOS`, keeping EditMode tests headless-safe without conditional test logic.
- **No package maintenance** — updating a haptics package for a new Xcode / Unity version is recurring overhead; a 30-line plug-in is a one-time write.

## Scope

The pattern covers at minimum:
1. **Haptics** — `UIImpactFeedbackGenerator` (light / medium / heavy styles) in `OriHaptics.mm`.
2. **Reduce Motion** — `UIAccessibility.isReduceMotionEnabled` read via `OriAccessibility.mm`.
   The native side only *reads* the OS flag; Unity C# owns all `PlayerPrefs` writes to avoid
   version-specific key-encoding issues. `MotionPrefs.SyncOsFlag()` writes to `"ReduceMotionOS"`,
   and `MotionPrefs.ReduceMotionEnabled` returns `InAppToggle OR OsFlag` so neither clobbers the
   other (issue #5, ADR-0005).

## Considered and rejected

- **Lofelt / Apple Core Haptics package** — full package with an App Store entitlement review; more than we need for three taps.
- **UnityEngine.iOS.Haptic / Handheld.Vibrate** — only UINotificationFeedbackGenerator styles; no impact weights, no silence control.
- **Runtime reflection on Apple.Core** — fragile and invisible to the compiler.

## Consequences

- `Plugins/iOS/OriHaptics.mm` and `Plugins/iOS/OriAccessibility.mm` must ship in the build; Unity Cloud Build compiles them automatically.
- The bridge is `#if UNITY_IOS`-gated so every call site on other platforms is a no-op.
- The Reduce Motion bridge is **device-only** — validate via Cloud Build → TestFlight at iOS export.
- Native author must run `xcodebuild test` against a physical device before signing off on a new capability.
