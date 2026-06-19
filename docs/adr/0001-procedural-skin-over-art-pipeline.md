# Procedural skin now; AI-art pipeline deferred

We render the game's "Painterly Cosmic Myth" look **procedurally in-engine** — gradients,
particle motes, vector silhouettes, typography, and UI skinning — rather than wiring the
approved art probes or running the 62-asset Gemini / Nano-Banana pipeline
(`docs/GEMINI_ASSET_PROMPTS.md`). This is the chosen path for making the game stop looking
like a science project "with what we have now."

## Why

- **Asset-independent** — no dependency on a generation run, downscale/cut pipeline, or
  imported binaries; the look ships from code.
- **Runtime control** — gradients/particles/tints animate and re-theme per path for free
  (see [0002](0002-cultural-identity-via-path-motifs.md)); a baked image can't.
- **No review gate** — abstract light/gradient/particle work carries far less cultural-safety
  risk than figurative art, so it does not block on the §7.10 native-speaker review.

## Considered and rejected (for now)

- **Wire the existing probes as a preview** — they are `KEEP` *references* (Higgsfield-sourced,
  wrong resolution, "references only" per the prompt sheet), not shippable finals.
- **Run the full Gemini pipeline** — large, and gated on the §7.10 review; out of scope for a
  fast in-engine pass.

## Consequences

- We invest in procedural systems (the bust silhouette of light, mote particles, the per-path
  tint) that **finals may later supersede**.
- The look is bounded by what we can draw in-engine — but finals can drop into the *same scene
  slots* later without rearchitecting, so this is reversible per-element, not a one-way door.
- The probes in `docs/art-probes/` stay as references for that future pass.
