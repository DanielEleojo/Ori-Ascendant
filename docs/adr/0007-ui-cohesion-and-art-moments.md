# UI Cohesion and Art Moments

**Status:** Accepted

## Context

Seven screens currently set type sizes, spacing, card colours, and opacity values with
inline literals. The values are largely consistent (the same hardcoded hex appears in
three card views; the same scrim alpha appears in two modals) but not enforced.
The UI-cohesion pass (7 units) needs a shared vocabulary all units can reference so that
"consistent" becomes a compile-time guarantee rather than a convention.

Separately, the app lacks a first-launch tutorial and a proper splash-to-game transition.
These are art moments that need a design spec before any implementation lands.

## Decision

### (a) Cohesion contract — four scales + CardViewSpec

Five pure-`const` static classes under `OriAscendant.UI` form the design-token layer:

| Class | Owns |
|---|---|
| `TypographicScale` | TMP point sizes at 390×844 reference resolution |
| `SpacingScale` | 4-pixel base spacing steps (Xxs → Xxl) |
| `OpacitySpec` | Named alpha values for scrims, accents, field tints, pulse bounds (hero-glow + chrome-hairline live in `AseHeroSpec`) |
| `CornerRadiusSpec` | Named radii passed to `ProceduralSprites` (`Card=16`, `Chip=8`, `Pill=24`, `BorderStroke=3.5`) |
| `CardViewSpec` | Single source of truth for all selectable cards (Path/Ori/Crossroads) — idle/selected panel colours, ring, text colours, name/sub sizes, selection scale |

`Palette.StormTint` (`0x471F0A`, warm-amber) is added to `Palette.cs` in this pass to
centralise the literal previously in `TribulationAtmosphere`.

All five classes are host-free (no `MonoBehaviour`, no `ScriptableObject`) and therefore
assertable in headless EditMode — per the Spec side of ADR-0006.

### (b) Modal anatomy spec

Every modal in the game follows this layered structure:

1. **Scrim** — `OpacitySpec.Scrim` (0.62) full-screen dim image behind the panel.
2. **Panel** — corner radius `CornerRadiusSpec.Card` (16px), padding `SpacingScale.Lg`
   (24px) on all sides, hairline border at `AseHeroSpec.HairlineBorderAlpha` (0.10),
   faint hero glow behind the title using `AseHeroSpec.HeroGlowAlpha` (0.14).
3. **Title** — `TypographicScale.H1` (22pt), serif (`FontRoleSpec.DisplayFontResourcePath`),
   `Palette.AseGold`, `SpacingScale.Lg` (24px) below before the prompt.
4. **Prompt** — `TypographicScale.Body` (16pt), `Palette.TextPrimary`, comfortable line
   spacing (1.4× line-height minimum).
5. **Options** — gap `SpacingScale.Md` (16px) between cards, minimum tap height 56px,
   using `CardViewSpec` idle/selected state colours.
6. **Confirm** — pill button, radius `CornerRadiusSpec.Pill` (24px), `Palette.AseGold`
   fill, legible disabled state (opacity halved), hold-to-confirm fill animation where
   a destructive or irreversible action is present.

### (c) Splash + how-to-play spec

**Transition sequence:**
1. iOS LaunchScreen — static illustration (permitted by §d below), shown by the OS
   instantly before Unity initialises. No Unity involvement.
2. Cold-open beat — `ColdOpenSkin`: illustration backdrop + title + proverb.
   Respects `MotionPrefs.ReduceMotion` (instant reveal when enabled).
3. First tap → game. No loading screen between cold-open and gameplay.

**First-launch how-to-play:**
- Shown after the title tap, on first launch only.
- A runtime-built dismissible card (three short in-world lines):
  1. "Tap to channel the Àṣẹ within you."
  2. "The light fills as you dwell — it works while you rest."
  3. "Reach the peak of your stage and face the Crossing."
- Gated by an **additive** `SeenFlags.HowToPlay` bit flag on the existing `SeenFlags`
  enum — no `SaveData` version bump, no migration. The bit is set to 0 on existing saves
  (not seen), so the card appears exactly once for new and upgrading players alike.
- Marked seen on dismiss (any tap or swipe outside the card).

### (d) Scoped exception to ADR-0001 (procedural-only)

ADR-0001 mandates procedural art everywhere to avoid binary bloat and art-pipeline
dependencies. This ADR grants **one scoped exception**:

> ONE imported splash illustration (PNG) is permitted for the iOS LaunchScreen and the
> `ColdOpenSkin` backdrop. The illustration is a single static asset, not a sprite sheet
> or animation sequence.

The background texture (adire-grain pattern) **stays fully procedural** — generated in
code by the existing `ColdOpenSkin` / `TitleScreenSkin` infrastructure. The exception
covers only the thematic illustration that anchors the game's visual identity on first
impression.

## Why

- **Consistency as a constraint.** Three card views currently hardcode the same two hex
  values. One extra key-press on a future edit and they silently diverge. `CardViewSpec`
  makes divergence a compilation error.
- **Testability.** All five token classes are headlessly assertable (ADR-0006 §Spec). A
  failing `DesignSystemTests` in CI is a faster feedback loop than a visual QA pass.
- **Splash = retention.** App Store guidance and mobile-game analytics consistently show
  that a coherent, culturally grounded first impression reduces day-0 drop-off. The
  procedural-only rule was written for in-game screens, not the iOS LaunchScreen.
- **How-to-play without migration.** A bitfield addition to `SeenFlags` adds zero bytes
  to existing saves (the flag is absent = not seen). This satisfies the "never block
  gameplay" rule (CLAUDE.md §Business Rules) and the "cloud-save auth failure falls
  through silently" requirement.

## Considered and rejected

- **Inline constants per screen** — the status quo. Fast to write; fragile to maintain.
  Rejected because the cohesion pass exists precisely to eliminate this.
- **ScriptableObject for design tokens** — puts visual constants behind a Unity import
  and a designer-facing asset, defeating the Spec side of ADR-0006. Designers do not
  tune corner radii or spacing steps.
- **Separate how-to-play scene** — overhead in scene management, loading, and state.
  A runtime-built card inside the existing title flow is lighter and test-compatible.
- **Full illustrated background (multiple assets)** — rejected; single illustration only,
  background texture stays procedural.

## Consequences

- Units 2–7 of the cohesion pass consume these tokens directly. No further design-token
  PRs should be needed for the current scope.
- `DesignSystemTests.cs` provides a regression gate: if any token value drifts (e.g. a
  merge reorders `SpacingScale`) CI fails immediately.
- The `SeenFlags.HowToPlay` bit must never be reassigned to another flag once shipped,
  or existing seen-state will be misread.
- Unit 6 repoints `TribulationAtmosphere` to `Palette.StormTint`; until that lands, the
  literal and the named constant temporarily coexist (harmless).
