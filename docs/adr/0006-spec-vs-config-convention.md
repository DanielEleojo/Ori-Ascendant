# Spec vs Config — where tunable constants live

Every tunable number lives in one of two homes, chosen by **who tunes it** and **whether the
change needs a recompile**:

- **`*Spec` — a pure C# static class** (no `ScriptableObject`, no `MonoBehaviour`). Intrinsic
  geometry, ratios, and animation-curve constants a *programmer* changes for visual or
  mathematical correctness. Compile-time `const`, headlessly testable on Linux. Examples:
  `CrossingColumnSpec` (column px height/width), `VesselFillRatio`, `CrossingCeremonySpec`,
  `AseHeroSpec`.
- **`*Config` — a `ScriptableObject` in `OriAscendant.Data`.** Gameplay timings, thresholds,
  rates, and economy values a *designer* tunes for balance, often gated on playtesting.
  Examples: `GameplayConfig.baseRate`, `TribulationConfig` ascend curve and threshold,
  `CouncilConfig.ancestorBaseBonus`, the per-stage `CultivationStageConfig` thresholds.

## Why

- **The two audiences differ.** A designer tuning ascend odds or stage thresholds must not
  recompile; a programmer fixing a column's pixel height or an easing curve must not push a
  ScriptableObject edit through a playtest gate.
- **Testability.** Spec constants are pure and assert-able in EditMode. The procedural-skin
  decomposition (ADR-0001) leans on this: each animation driver reads a Spec, never an inline
  literal, so its output is verifiable headlessly — the way `ColdOpenBeat` already is.
- **It names what already exists.** The repo split this way by instinct; this ADR records the
  rule so it stays consistent as the skin hub is broken into drivers.

## The litmus

> If a **designer** would change it to tune feel or balance → **Config**.
> If a **programmer** would change it to fix layout or curve correctness → **Spec**.

## Considered and rejected

- **One home for everything (all ScriptableObjects).** Forces a designer-facing asset and a
  Unity import for a pixel offset or an easing constant, and drags pure layout math out of
  headless EditMode reach.
- **One home for everything (all consts).** Bakes balance values into the binary, defeating
  the `/Resources/StageConfigs/` playtesting gate and the "designer tunes without a recompile"
  goal.

## Consequences

- The "no magic numbers" rule (`csharp.md`) is satisfied by *both* homes — a literal inlined in
  a MonoBehaviour `Update()` belongs in one or the other.
- The animation drivers extracted from `MainScreenSkin` consume `*Spec` constants and must not
  reintroduce inline magic numbers.
- Moving a gameplay-balance value out of a `*Config` into a `*Spec` to dodge the playtesting
  gate is a smell, not a shortcut.
