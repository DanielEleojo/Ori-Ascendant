# §7.10 Pre-Review Record — Crowned Ascended Reveal (Appearance-0)

**Issue:** #11 — Dynasty: funded hero art + the bespoke crowned Ascended reveal (slice 8)
**Status:** Engineering pipeline wired. Art production pending funding + §7.10 native-speaker sign-off.
**⚠ Blocked:** Human native-speaker / community review is mandatory before art ships (ART_BIBLE §7.10).

---

## What this asset is

The **Crowned Ascended Reveal** (`crownedAscendedRevealPortrait`) is a single portrait (appearance-0) shown during the `TribulationScreen` ceremony when the cultivator **successfully crosses the river** (ascended outcome only). It replaces the humble Stage-6 Victor portrait for the reveal beat, showing the same person now crowned and exalted.

**Design rule (GEMINI_ASSET_PROMPTS.md §A, row ✦):** "Ascension = royal — the crown goes on **after the Crossing succeeds**, not at Stage 5. Stages 0–5 stay HUMBLE. The ✦Ascended reveal is the royal exception."

**Exalted bearing, not physique (CONTEXT.md):** the cultivator's build does not change. What changes is attire and bearing — they are the same person, elevated.

**Fallback:** until this art ships, `TribulationConfig.RevealSprite()` falls back to the Stage-6 humble Victor portrait + gold-radiance FX overlay (the slice 6 committed fallback, already in prod).

---

## Asset specification (appearance-0)

| Field | Value |
|---|---|
| Unity slot | `TribulationConfig.crownedAscendedRevealPortrait` (Sprite) |
| Filename | `portrait_ascended_appearance0.png` |
| Size | 1024×1024, PNG, transparent background |
| Model | soul_2 + trained Soul (character-consistency with stages 0–5) |
| Post-pass | upscale_image → remove_background |

### Generation prompt (Gemini / Nano Banana Pro)

**Style lead** (prepend):
> Painterly cosmic-myth illustration, soft gouache brushwork, Gris / Sky: Children of the Light feel, NOT photoreal, NOT glossy, NOT 3D; warm gold light on deep indigo (#1B2150 + Àṣẹ gold #F4C14E), soft bloom, paper-grain, text-free.

**Body** (use the approved Stage-3 anchor + Soul):
> Centered chest-up portrait of [IDENTITY], at the moment of SUCCESSFUL ascension after the Crossing — no longer humble but exalted and ROYAL: rich aṣọ-òkè handwoven prestige cloth with gold thread, a beaded royal crown (adé), a royal coral collar, gold adornment (secular royalty of an exalted Oba, celebratory). Fully wreathed in radiant àṣẹ gold, near-translucent glowing edges, bright abstract gold constellations behind (no figures); triumphant, ascending into light. EXALTED BEARING, same person — do not alter build or face, only attire and bearing.

**Negative tail** (drop the humble negative tail for this row; use this instead):
> NOT priestly — no orisha-devotee ìlẹ̀kẹ̀, no divining chain/tools, no egungun, no priestly vestments; a human ascending in royal splendour, not a deity, no halo-throne, no second face in the light. No text, no watermark, no gibberish lettering.

---

## §7 self-review checklist (to be completed at review time)

> **Reviewer default posture: adversarial and conservative — when in doubt, FAIL.**

### Hard fails (any single fail = reject + regenerate)

- [ ] **§7.1:** No supreme deity rendered or implied — no enthroned god in the sky, no glowing judge, no face in the clouds. Gold light has no shaped source-figure.
- [ ] **§7.2:** The royal adé crown is SECULAR ROYALTY (an exalted Oba), not a sacred priestly office. No ìlẹ̀kẹ̀ (orisha-devotee beaded necklaces), no divining chain (ọpẹlẹ), no fly-whisk (ìrùkẹ̀rẹ̀), no priestly robes of office.
- [ ] **§7.3:** Ancestors on the far bank (if visible) are abstract equal lights — NOT masked figures, NOT egungun regalia.
- [ ] **§7.4:** No divination imagery: no Ifá board, no cowries/ikin, no Odù marks, no altar with offerings.
- [ ] **§7.5:** No orisha personified as a figure, boss, or collectible in the art.
- [ ] **§7.6:** No skulls, bones, occult/voodoo framing, sickly-green palette, dark sacrifice imagery. Mood reads warm, radiant, joyful.
- [ ] **§7.7:** No baked-in text or lettering in the AI art.
- [ ] Pareidolia test (squint): no face/figure/mask hidden in the gold light, clouds, or constellations.

### Soft flags (worth a regeneration if possible)

- [ ] The figure reads as the **same person** as their Stage-3–5 portraits (same face, skin tone, build) — identity held across.
- [ ] Exalted BEARING, not altered physique — the crown and cloth transform, not the body.
- [ ] Mood is radiant, warm, triumphant — not menacing, horror-adjacent, or deity-like.
- [ ] The adé crown reads as secular royal adornment, not liturgical regalia.
- [ ] Palette stays indigo + Àṣẹ gold (ART_BIBLE §1); no sickly green, no cold grey.

---

## Notes for human reviewer

1. **The adé crown:** the beaded royal crown (`adé`) worn by Yoruba monarchs is secular royal regalia, not sacred priestly regalia. Please confirm the design clearly reads as a monarch's crown, not as orisha-devotee ìlẹ̀kẹ̀ (orisha beadwork).
2. **Coral collar:** royal coral is associated with Yoruba royalty (Oba) as a secular status marker. Please confirm no connotation of priestly office.
3. **"Exalted bearing, not physique":** the ART_BIBLE and CONTEXT.md rule is that the crown transforms bearing and attire, not build or face. The generated asset must read as the same person.
4. **Pareidolia check on the gold light:** the gold constellations and radiance behind the figure should resolve as abstract points of light, not as a deity face, halo-throne, or presiding figure.
5. This asset is **appearance-0 only** — per-appearance crowns ride with the Appearance pool (ADR-0001) and are deferred.

---

## Pipeline integration checklist (before assigning to Unity slot)

- [ ] Generated at 1024×1024 (or higher, then downscaled).
- [ ] Post-processed: upscale_image → remove_background (clean alpha for the mote-drift particle layer).
- [ ] Run through the full ART_BIBLE §9 pre-delivery checklist.
- [ ] §7.10 human native-speaker / community sign-off obtained.
- [ ] Sprite imported in Unity as PNG with transparency, uncompressed or high-quality compression.
- [ ] Assigned to `TribulationConfig.crownedAscendedRevealPortrait` in the Inspector.
- [ ] Verified in Play Mode: crowned portrait appears on successful Crossing; Stage-6 portrait + FX shows on fall and when slot is null.
