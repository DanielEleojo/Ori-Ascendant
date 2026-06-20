# The main screen is a luminous hero, not framed chrome

The MVP main screen showed the cultivator as a `null`-sprite Image inside a square portrait box,
with a separate fill-bar gauge and a five-chip council strip. We are replacing all of it with a
single luminous-hero composition: a **borderless silhouette of light** that **fills like an empty
vessel** as Àṣẹ accrues (the *only* progress display — the separate progress bar is removed),
**overflows upward into a column of light at tribulation**, and stands beneath a **growing
constellation of ancestor-stars** (the *bloodline sky*) that replaces the council chips. All
other chrome (counter, menus, Settings, buttons) follows Claude-style minimalist discipline —
flat surfaces, hairline borders, negative space — so the *only* thing that glows is the hero.
See `CONTEXT.md` ("Minimalist chrome, luminous hero", "Silhouette of light", "Bloodline sky").

## Why

- The framed portrait *is* the literal "science-project look": a `null` sprite on a UGUI Image
  renders as a hard-edged rectangle. Removing the box (the Image draws nothing; only procedural
  light remains) fixes it at the root, not cosmetically.
- A hero that *fills with light* turns abstract numeric progress into something felt and seen —
  and is the cosmology made literal (the cultivator is a vessel for Àṣẹ).
- Folding progress into the vessel and the council into a star-field removes two pieces of
  chrome — which *is* the minimalism the polish pass exists to deliver.
- The Council-max-5 cap and the `permanentAseBonus` "foundation" stop reading as mechanics: the
  five nearest stars are the Council; retired elders recede into the deepening sky (the
  foundation), made visible. The cap becomes "which stars are near," not a wall.

## Considered and rejected

- **Keep the box, give it a real sprite later** — still a bounded frame; defers the look to the
  §7.10-gated art pipeline (see [0001](0001-procedural-skin-over-art-pipeline.md)) instead of
  fixing it now.
- **Full-bleed / screen-filling silhouette** — rejected by the design owner; the fix is the
  *missing border*, not scale. The hero stays modestly sized.
- **Keep the explicit progress bar alongside the vessel** — redundant once the vessel is the
  gauge, and it fights the minimalist cut.

## Consequences

- The continuous fill wants a small interior **shader** (`_FillLevel` 0→1) so the level animates
  smoothly without re-baking the silhouette texture each frame — a deliberate step beyond the
  pure-CPU procedural skin, still asset-free per [0001](0001-procedural-skin-over-art-pipeline.md).
- Precise per-stage "how close am I" is no longer shown on a bar; the felt vessel + the arming
  CTA + the exact Àṣẹ counter carry it. The one place that needed visible movement (tribulation,
  GAMEPLAY §2.4) is served by the overflow column.
- [0002](0002-cultural-identity-via-path-motifs.md) is preserved, not overturned: path identity
  now lives in star/aura **colour + motion character** — the minimalist substitute for full
  per-path storms.
- **No SaveData change** — the sky reads existing council/chronicle/lineage data, so no migration.
