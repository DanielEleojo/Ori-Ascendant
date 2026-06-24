---
paths:
  - "Ori Ascendant/Assets/Scripts/**"
  - "Ori Ascendant/Assets/Tests/**"
---
## Architecture
ServiceLocator is the central registry (self-register in Awake — never FindObjectOfType). ScriptableObjects = read-only config. SaveData = single source of truth. UI listens to C# events; never writes game state.

## Off-Limits
- SaveData.cs — no field rename/type change without migration version bump
- BigNumber.cs arithmetic — only with accompanying unit tests
- OfflineProgressCalculator.cs — pure math, never Time.timeScale or coroutines
- CloudSaveManager.cs auth — async only, always a failure-fallback path
- /Resources/StageConfigs/ — threshold changes need playtesting, not just edits
