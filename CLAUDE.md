# Ori Ascendant — Claude Code Config

## What We're Building
An idle cultivation mobile game for iOS built on West African cosmology, featuring a generational bloodline prestige system. Reference docs/PRD.md for full product context and docs/TECH_DESIGN.md for architecture.

## Stack
- Engine: Unity 6 (6000.4.10f1 — tech release, deliberately not LTS)
- Language: C#
- Platform: iOS 15+ (iPhone 12 minimum)
- iOS Export: Unity Cloud Build
- Persistence: JSON local + Game Center iCloud

## Commands
- Build: Via Unity Cloud Build (no local iOS compile — developer is on Linux)
- Local test: Unity Editor Play mode (Android/Standalone target for editor testing)
- Unit tests: Unity Test Runner (Window > General > Test Runner)

## Architecture
Service-oriented with ServiceLocator as central registry. ScriptableObjects for config (read-only, never modified at runtime). SaveData is the single source of truth for all player state. UI systems listen to C# events from game systems — they never write to game state directly. See docs/TECH_DESIGN.md §2 for full diagram.

## Coding Conventions
- All Àṣẹ values use BigNumber struct — never raw float or double
- Unix UTC timestamps only (DateTimeOffset.UtcNow.ToUnixTimeSeconds()) — never DateTime.Now
- Systems register with ServiceLocator in Awake(); never use FindObjectOfType
- ScriptableObjects are config only — never write to them at runtime
- Event names: On + PastTense (e.g. OnStageAdvanced, OnTribulationComplete)
- File naming: PascalCase for all C# files, matching class name exactly
- No magic numbers — all thresholds and rates defined in ScriptableObject assets

## Business Rules (Must Enforce)
- Tribulation cannot trigger unless currentStage == tier peak AND aseAmount >= threshold
- Offline progress capped at 28800 seconds (8 hours) — never exceed this cap
- A fallen cultivator still produces an ancestor (bonusMultiplier = 0.4) — never a dead end
- Cloud save auth failure must always fall through to local save silently — never block gameplay
- Council max is 5 for MVP; retiring ancestor bakes bonus into lineage.permanentAseBonus before removal

## Off-Limits
- `SaveData.cs` — never rename fields or change types without a migration version bump
- `BigNumber.cs` arithmetic — only modify with accompanying unit tests
- `OfflineProgressCalculator.cs` — pure math only, never Time.timeScale or coroutines
- `CloudSaveManager.cs` auth — async only, must always have a failure fallback path
- `/Resources/StageConfigs/` — threshold changes require playtesting, not just code edits

## If Unsure
- Propose 2 options and ask before implementing
- Never rename public APIs or event signatures without asking
- Reference docs/PRD.md §6 for business rules before implementing any progression logic
- Reference docs/TECH_DESIGN.md §4 for which system owns which responsibility
