# Changelog

All notable changes to Ori Ascendant. Format follows [Keep a Changelog](https://keepachangelog.com);
this project uses [Semantic Versioning](https://semver.org).

## [Unreleased]

### Added
- iOS capability wiring: `IosBuildPostProcessor` adds Game Center + iCloud (Documents container)
  to the generated Xcode project at build time.
- Submission docs: privacy policy (`docs/PRIVACY.md`), App Store listing draft
  (`docs/store/app-store-listing.md`), and the release runbook (`docs/RELEASE_CHECKLIST.md`).
- ADR-0006 (spec-vs-config convention); per-layer assembly definitions; `RateInputs` value type;
  animation drivers (`BreathingDriver`, `AseFlashDriver`, `CeremonyDriver`) and shared
  `ProceduralSprites` / `UiBuilder` skin modules.

### Changed
- `BuildConfigurator` now sets the iOS scripting backend to IL2CPP explicitly.
- Internal architecture pass (no gameplay change): `MainScreenSkin` decomposed 1234 → 920 lines;
  `TribulationSystem.Resolve` expressed as named phases; runtime split into per-layer assemblies.

## [0.1.0] — MVP feature-complete (pre-release)

The first complete generational loop, code-complete and gate-green (EditMode 584 passing).
Not yet submitted — see `docs/RELEASE_CHECKLIST.md` for the remaining external + content gates.

### Added
- Core idle loop: 1-second Àṣẹ production, tap-to-channel, offline progress (8-hour cap).
- Cultivation: 6 stages across 2 tiers; 3 paths (Ane, Sango, Osun) with orthogonal modifiers.
- Tribulation (the Crossing): roll-once / persist-first resolution; ascend or fall, both produce
  an ancestor (fallen at 0.4× — never a dead end).
- Ancestral Council (max 5) with Àṣẹ-neutral retirement into a permanent lineage bonus.
- Crossroads, Chronicle, Ori vows, and the Remembrance system.
- Save: local JSON (atomic write) + optional Game Center / iCloud cloud sync with silent
  local-first fallback and conflict resolution.
- Procedural cultural skin (ADR-0001): the luminous silhouette-of-light hero, bloodline sky,
  per-path re-theme, cold-open, hero breathing, and the Crossing ceremony — all rendered in-engine.
- iOS native bridges (ADR-0004): haptics + Reduce-Motion, behind `#if UNITY_IOS` seams.

### Known gaps (tracked for release, not bugs)
- Final visual art (37 assets) and audio (10 assets) — procedural/placeholder until produced.
- §7.10 native-speaker cultural review — **pending** (hard gate before publication).
- On-device validation (Game Center, haptics, Reduce Motion, diacritics) — TestFlight-gated.

### Post-MVP backlog (not in this release)
- Tiers 3–4 and paths 4–6; Sacred Trials; lineage-synergy combos; Android port; localization.
