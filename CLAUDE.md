# Ori Ascendant
Idle cultivation mobile game (iOS 15+, iPhone 12 min) on West African cosmology, with a generational bloodline prestige system. Product: docs/PRD.md · Architecture: docs/TECH_DESIGN.md.

## Stack
Unity 6 (6000.4.10f1 — tech release, deliberately NOT LTS) · C# · iOS export via Unity Cloud Build (no local iOS compile — dev is on Linux) · Persistence: JSON local + Game Center iCloud.

## Commands
- Build: Unity Cloud Build only
- Test: Unity Test Runner — EditMode/PlayMode; editor Play mode targets Android/Standalone
<!-- C# coding conventions auto-load from .claude/rules/csharp.md when editing Assets/Scripts or Assets/Tests -->

## Architecture
ServiceLocator is the central registry (systems self-register in Awake — never FindObjectOfType). ScriptableObjects = read-only config. SaveData = single source of truth for player state. UI listens to C# events; it never writes game state. Detail: docs/TECH_DESIGN.md §2/§4.

## Business Rules (must enforce — docs/PRD.md §6)
- Tribulation triggers only if currentStage == tier peak AND aseAmount >= threshold
- Offline progress capped at 28800s (8h) — never exceed
- A fallen cultivator still produces an ancestor (bonusMultiplier = 0.4) — never a dead end
- Cloud-save auth failure falls through to local save silently — never block gameplay
- Council max 5 (MVP); retiring an ancestor bakes its bonus into lineage.permanentAseBonus before removal

## Off-Limits (no change without the noted guard)
- SaveData.cs — no field rename/type change without a migration version bump
- BigNumber.cs arithmetic — only with accompanying unit tests
- OfflineProgressCalculator.cs — pure math, never Time.timeScale or coroutines
- CloudSaveManager.cs auth — async only, always a failure-fallback path
- /Resources/StageConfigs/ — threshold changes need playtesting, not just edits

## If Unsure
- Propose 2 options and ask; never rename public APIs or event signatures without asking
