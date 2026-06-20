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
The procedural cultivator at the centre of the main screen — a humanoid silhouette filled with
gold light (rim-light + motes), whose proportions age across the six stages (child → adult →
elder) and whose aura takes on the active path's character. Rendered **borderless**: no portrait
box, no frame, no hard margins — its edges dissolve into the indigo so it reads as *emerging
from* the dark, never as an image in a container. It stays modestly sized (not full-screen) —
the fix is the missing border, not scale. Its light **fills the figure like an empty vessel**: barely a flicker
at stage 0 (almost nonexistent), then a rising level of gold light that climbs through the body
as you ascend, brimming and fully radiant by tribulation — where the full vessel then *overflows upward* in a
rising column of light that serves as the tribulation gauge (GAMEPLAY §2.4). The vessel is the
*only* progress display — a minimalist choice removed the separate progress bar; the exact
number still lives in the Àṣẹ counter. Still tappable for channel via an
invisible hit-area. Keeps the human ascent arc (ART_BIBLE §4) without painted portraits.
_Avoid_: "the avatar", "the character art", "the portrait" (there is no portrait frame anymore).

**Ascent signature**:
The neutral, pre-path visual identity (stages 0–2) — a single Àṣẹ thread rising into the
constellation and the proverb. That constellation is no longer a one-off backdrop: its
ancestor-stars are the permanent, ever-growing bloodline sky (below). Carries the cultural vibe
before any path is chosen.
_Avoid_: "the default theme".

**Bloodline sky** (a.k.a. the ancestral constellation):
The night sky behind the main screen that holds every ancestor as a star and *grows each
generation* — the visual replacement for the flat five-chip council strip. The handful nearest
and brightest are the active Council (max 5) whose light reaches the living cultivator; when an
elder is bumped past five it does not vanish — its star **recedes into the deepening field**,
its light merged into the foundation of the house (its permanent bloodline blessing) — made
visible. Gen 1 opens on a near-empty sky; an Ascended joins as a bright star in its path's
colour, a fallen one as a low ember — never absent (no dead ends). The full, navigable sky is
the Chronicle.
_Avoid_: "the council chips", "the ancestor list" (it is a sky of light, not a roster).

**Path re-theme** (a.k.a. motif-forward):
On choosing a path, the main screen atmospherically takes on that orisha's natural force —
accent palette, ambient behaviour (Sango storm, Osun river-current, Ane earth/embers), bar
fill, and the silhouette's aura all shift. This natural-force motif — not adire textile — is
the game's primary cultural fingerprint.
_Avoid_: "the path skin".

## UI craft & feel

**Minimalist chrome, luminous hero**:
The UI discipline for the polish pass: apply Claude-style minimalism — flat clean surfaces,
hairline borders, generous negative space, restrained type, one clear action per surface — to
all *chrome* (counters, panels, menus, buttons), while the *only* thing allowed to glow or
read as painterly is the hero (the silhouette of light + motes + the Àṣẹ counter). The dark
indigo + Àṣẹ-gold palette stays; minimalism is a discipline layered on it, never a swap to
Claude's cream/light palette (gold light only reads on dark). Sharpens — does not replace —
the *cultural vibe* and the Art Bible's "generous negative space / restraint" §3.
_Avoid_: "Claude theme", "cream/light mode", "flat design" (it is selective restraint, not flat-everywhere).

**Cold open** (a.k.a. the kindling):
The brief, skippable launch beat before the main screen — the silhouette kindling out of
darkness, the proverb, and a single *tap to enter*. The whole story (emerge from the dark,
become light) told in one breath; it sets the "App Store game, not a project" tone in the first
three seconds. Honours iOS Reduce Motion (softens to a fade).
_Avoid_: "splash screen" (it is a story beat, not a logo card); "intro cutscene" (one breath, skippable, not a movie).
