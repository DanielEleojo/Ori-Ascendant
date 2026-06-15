# Per-generation cosmetic Appearance, rolled at the Crossing

Each generation's Cultivator is rendered with one Appearance from a small fixed
pool (initially three), rolled once when a new generation begins at the Crossing,
persisted in the same crash-safe write as the rest of the generation reset, and
purely cosmetic — no effect on Stage, Path, Àṣẹ rates, or the Council.

We chose **per-generation** (a fresh look each Crossing) over **per-save** (one
look for the whole bloodline) because it fits the generational-bloodline fantasy
(each descendant is a distinct person), every player sees the whole — funding-
gated — art pool over a playthrough instead of only one-third of it, and it varies
representation across a run. Appearance is decoupled from Path so neither
constrains the other.

Implementation is deferred to Phase D: there is no portrait-display pipeline yet
and stage portraits are unfunded placeholders, so the SaveData field, the roll,
and the rendering land together rather than adding state nothing reads. Generation
1 is fixed to Appearance 0 (the only look with reference art); the roll takes
effect from generation 2.

## Consequences
- When implemented: one new per-generation index on SaveData. **Add-only changes
  need no schemaVersion bump** per the SaveData policy (Newtonsoft defaults missing
  fields) — confirmed by slice #2, which added `currentOri` with no bump and the
  locked-v1-defaults test left intact. The appearance roll lands in
  TribulationSystem.Resolve's atomic write.
- The "three starting cultivators" framing is retired for "appearance pool"
  (see CONTEXT.md: Appearance) to avoid colliding with Cultivator.
