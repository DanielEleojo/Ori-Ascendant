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

## Working Principles
*Adapted from the Karpathy coding-agent guidelines — bias toward caution over speed; use judgment on trivial edits. They are working if diffs carry fewer unrelated changes, fewer overcomplicated rewrites, and clarifying questions arrive before mistakes rather than after.*

**1. Think before coding.** Surface assumptions; if multiple readings exist, present them — never pick silently. Propose 2 options and ask before implementing anything non-trivial. Never rename a public API, an `On+PastTense` event signature, or a `SaveData` field without asking. Consult docs/PRD.md §6 (business rules) and docs/TECH_DESIGN.md §4 (system ownership) before touching progression logic.

**2. Simplicity first.** This is a ruthlessly-scoped 5-feature MVP (PRD §4): write the minimum that satisfies the request. No speculative features, no abstractions for single-use code, no configurability nobody asked for, no handling of impossible cases. If a senior engineer would call it overcomplicated, simplify. Post-MVP ideas go to the backlog, not the code.

**3. Surgical changes.** Every changed line must trace to the request. Don't refactor, reformat, or "improve" adjacent code — match the surrounding style and the conventions above, and respect Off-Limits. Regenerate the Main scene + config assets via `Assets/Editor/SceneBuilder.cs`; never hand-edit `Main.unity`. Remove only the orphans your change creates; flag unrelated dead code instead of deleting it.

**4. Goal-driven execution, verified.** Turn each task into a checkable success criterion and loop until it passes — don't stop at "make it work." Default verification is the headless test gate (docs/BUILD_PLAN.md): write/extend EditMode (and PlayMode for runtime) tests first, then make them pass — and trust the parsed results XML, never the batch exit code. For multi-step work, state a short plan with a verify step per item.

**5. Plan before non-trivial work.** Reach shared understanding before coding: grill the open design forks (`/grill-me` or `/grill-with-docs`), then express the plan of action as a braid Mermaid-flowchart-first (the `braid` skill) before implementing. This is faster, not slower — it front-loads the decisions that otherwise cause rework. Trivial edits skip it. Chains with principle 4: grill → braid plan → TDD → verify.

## Agent skills

### Issue tracker

Issues and PRDs are tracked as GitHub issues on `DanielEleojo/Ori-Ascendant` via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default triage vocabulary — label string equals role name. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
