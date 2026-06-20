# Native bridge for iOS system features over a managed package

iOS system APIs that lack a Unity managed equivalent (haptics, accessibility settings) are reached through a thin native bridge — a C plug-in called via `DllImport("__Internal")` — rather than a third-party managed package.

## Why

- **Exact API surface** — the bridge exposes only what the game uses, with no deprecation risk from a package's opinion of the same API.
- **No App Store review risk** — the plug-in is a thin shim over documented Apple frameworks (UIImpactFeedbackGenerator, UIAccessibility); no private API use.
- **Compile-time safety on Linux** — the `DllImport` path is wrapped in `#if UNITY_IOS`, keeping EditMode tests headless-safe without conditional test logic.
- **No package maintenance** — updating a haptics package for a new Xcode / Unity version is recurring overhead; a 30-line plug-in is a one-time write.

## Scope

The pattern covers at minimum:
1. **Haptics** — `UIImpactFeedbackGenerator` (light / medium / heavy styles).
2. **Reduce Motion** — `UIAccessibility.isReduceMotionEnabled` written to `PlayerPrefs` on change so `MotionHelper` callers can read it without bridging (see ADR-0003).

## Considered and rejected

- **Lofelt / Apple Core Haptics package** — full package with an App Store entitlement review; more than we need for three taps.
- **UnityEngine.iOS.Haptic / Handheld.Vibrate** — only UINotificationFeedbackGenerator styles; no impact weights, no silence control.
- **Runtime reflection on Apple.Core** — fragile and invisible to the compiler.

## Consequences

- A `Plugins/iOS/NativeBridge.mm` source file must ship in the build; Unity Cloud Build compiles it automatically.
- The bridge is `#if UNITY_IOS`-gated so every call site on other platforms is a no-op.
- Native author must run `xcodebuild test` against a physical device before signing off on a new capability.
