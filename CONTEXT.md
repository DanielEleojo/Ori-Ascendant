# Ori Ascendant

Glossary for *Ori Ascendant*, an idle-cultivation mobile game built on Igala/Yoruba/Igbo
cosmology. This file pins the working language of the project — especially the
production-and-appearance terms that disambiguate where the game *is* from where it's
*going*. For in-world cosmology vocabulary (Àṣẹ, Orí, the orisha paths, adire, the
Crossing), the canonical glossary is `docs/ART_BIBLE.md` §11; this file does not duplicate it.

## Appearance & production state

**Science-project look**:
The game's current un-art-directed placeholder appearance — flat colour-blocked panels on
Unity's default sprite, no painterly art, no background, broken Yoruba diacritics. The thing
this effort exists to replace.
_Avoid_: graybox, placeholder, programmer art (all fine, but "science-project look" is the team's word).

**Cultural vibe**:
The target feel — premium, warm, reverent, and specifically West-African-grounded (not
generic fantasy). Operationally it means obeying the "Painterly Cosmic Myth" direction in
`docs/ART_BIBLE.md`: indigo + Àṣẹ-gold palette, an adire textile fingerprint, and correct
Yoruba orthography.
_Avoid_: "the theme", "the style", "the aesthetic" (too vague — name it).

**Procedural skin**:
Reaching the cultural vibe through in-engine craft — gradients, shaders, particles,
typography, and UI skinning — rather than by importing painted art. The chosen path for the
current effort.
_Avoid_: "the art pass" (this is explicitly *not* an art-asset pass).

**Probe** (a.k.a. reference):
A generated image in `docs/art-probes/` used to validate the art direction; `KEEP`/`REJECT`
marks which ones did. Probes are reference-only and are never shipped as-is.
_Avoid_: calling a probe a "final" or "an asset".

**Final asset**:
A launch-quality visual generated through the Gemini / Nano Banana pipeline
(`docs/GEMINI_ASSET_PROMPTS.md`), red-line-checked (§9), and held for the §7.10
native-speaker review. None exist yet.
_Avoid_: conflating with a probe.

## Look & motif

**Silhouette of light**:
The procedural cultivator standing in the portrait / tap-to-channel zone — a humanoid
silhouette filled with gold light (rim-light + motes), whose proportions age across the six
stages (child → adult → elder) and whose aura takes on the active path's character. Keeps the
human ascent arc (ART_BIBLE §4) without painted portraits.
_Avoid_: "the avatar", "the character art".

**Ascent signature**:
The neutral, pre-path visual identity (stages 0–2) — a single Àṣẹ thread rising into a
constellation, the ancestor-stars, and the proverb. Carries the cultural vibe before any path
is chosen.
_Avoid_: "the default theme".

**Path re-theme** (a.k.a. motif-forward):
On choosing a path, the main screen atmospherically takes on that orisha's natural force —
accent palette, ambient behaviour (Sango storm, Osun river-current, Ane earth/embers), bar
fill, and the silhouette's aura all shift. This natural-force motif — not adire textile — is
the game's primary cultural fingerprint.
_Avoid_: "the path skin".
