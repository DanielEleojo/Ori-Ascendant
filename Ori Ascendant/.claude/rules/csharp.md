---
paths:
  - "Assets/Scripts/**"
  - "Assets/Tests/**"
---
# C# conventions — Ori Ascendant

- Àṣẹ values: always the BigNumber struct — never raw float/double
- Time: Unix UTC only — DateTimeOffset.UtcNow.ToUnixTimeSeconds(); never DateTime.Now
- Systems self-register with ServiceLocator in Awake(); never FindObjectOfType
- ScriptableObjects are config only — never write to them at runtime
- Events: On + PastTense (e.g. OnStageAdvanced, OnTribulationComplete)
- File names: PascalCase, matching the class name exactly
- No magic numbers — thresholds/rates live in ScriptableObject assets
- UI listens to events; it never writes game state directly
