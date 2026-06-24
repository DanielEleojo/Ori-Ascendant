# Ori Ascendant
Idle cultivation mobile game (iOS 15+, iPhone 12 min) on West African cosmology. Product: docs/PRD.md · Architecture: docs/TECH_DESIGN.md.

## Stack
Unity 6 (6000.4.10f1 — tech release, NOT LTS) · C# · iOS via Unity Cloud Build (Linux dev, no local iOS compile) · JSON local + Game Center iCloud.

## Commands
- Build: Unity Cloud Build only
- Test: Unity Test Runner — EditMode/PlayMode; editor Play mode targets Android/Standalone

<!-- C# arch + off-limits guards auto-load from .claude/rules/csharp.md when editing Assets/Scripts or Assets/Tests -->

## Business Rules (docs/PRD.md §6)
- Tribulation triggers only if currentStage == tier peak AND aseAmount >= threshold
- Offline progress capped at 28800s (8h) — never exceed
- A fallen cultivator still produces an ancestor (bonusMultiplier = 0.4) — never a dead end
- Cloud-save auth failure falls through to local save silently — never block gameplay
- Council max 5 (MVP); retiring an ancestor bakes its bonus into lineage.permanentAseBonus before removal

## If Unsure
- Propose 2 options and ask; never rename public APIs or event signatures without asking
