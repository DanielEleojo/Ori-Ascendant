# In-house motion helper + hero idle breathing over third-party tween

The main screen's hero silhouette breathes with a subtle idle animation — a slow scale and brightness sine (~0.24 Hz, ±1.2% scale, ±7% brightness) — driven by an in-house `MotionHelper` static class rather than a third-party tween library. iOS Reduce Motion silences all motion (sine returns 0) but does not remove the silhouette.

## Why

- **No package dependency** — a tween library adds compile-time and runtime overhead for a single animation that is five lines of math.
- **Headlessly testable** — pure static functions take `reduceMotion: bool` as a parameter and run on Linux without a scene or MonoBehaviour.
- **Reduce Motion compliance** — the pure-function seam makes gating trivial and verifiable by EditMode tests.
- **Ownership** — the helper lives alongside the skin that uses it; there is no version-upgrade surface.

## Considered and rejected

- **DOTween / LeanTween** — both are capable, but add a package with a licence review step, update maintenance, and a runtime allocator for a single two-channel animation. Overkill.
- **Unity Animator** — no artist animator on the project; driving a clip from code is more wiring than writing the three-line sine directly.
- **Coroutine tween** — blocking and hard to unit-test; the Update-driven approach matches the existing CTA glow and bar leading-edge patterns in `MainScreenSkin`.

## Breathing constants (tunable)

| Constant | Value | Rationale |
|---|---|---|
| `BreathPeriodSeconds` | 4.2 s | ~0.24 Hz — below the 0.5 Hz distraction threshold |
| `BreathScaleAmp` | 0.012 | ±1.2% — subliminal at arm's length; perceptible up close |
| `BreathBrightAmp` | 0.07 | ±7% — gentle luminance heartbeat on the gold bust |

## Reduce Motion: PlayerPrefs bridge pattern

`IsReduceMotion()` reads `PlayerPrefs.GetInt("ReduceMotion", 0)`. On iOS, a native bridge (ADR-0004 pattern) writes this key when `UIAccessibility.isReduceMotionEnabled` changes; on all other platforms it defaults to false. The pure math functions never query device state directly.

## Consequences

- `MotionHelper.EaseOut`, `Tween`, and `BreathingSine` are the canonical motion primitives; future animations must use them, not inline math.
- The breathing amplitude constants sit in `MainScreenSkin`; a designer can tune them without touching `MotionHelper`.
- A native iOS Reduce Motion bridge is required before launch (see ADR-0004); until then the feature always animates on device.
