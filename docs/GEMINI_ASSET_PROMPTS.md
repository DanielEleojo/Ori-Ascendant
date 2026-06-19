# Ori Ascendant — Remaining Asset Prompts (for Gemini / Nano Banana)

**Date:** 2026-06-14 (rev 2 — 3-character roster) · **Companion to:** `ASSET_MANIFEST.md`, `ART_BIBLE.md`, `PROBE_FINDINGS_AND_PLAN.md`
**Purpose:** generate the final art in **Gemini** (Nano Banana). **Higgsfield outputs are REFERENCES only, not final assets.**

## Why Gemini works here
Gemini's image model is **Nano Banana** — the same engine (`nano_banana_pro`) that produced every reference. Validated prompts port over directly.
- **Nano Banana Pro** (Gemini 3 Pro Image, paid/"redo", up to 4K) → heroes: portraits, backgrounds, icon.
- **Nano Banana 2** (app default) → motifs, UI, frames, FX.
- **ChatGPT Images 2.0 (`gpt-image-2`)** is a backup; it holds a character across up to 8 images in one prompt (handy for a 6-stage set).

## Scope: 3 starting cultivators (our first three characters; roster is expandable)
Every character runs the **same six cultivation stages** (humble), plus a **royal Ascended reveal** after a successful Crossing. **21 portraits total (3 × 7).**

## How to use this file
1. **Every prompt is paste-ready** = `[Style lead] + [body] + [Negative tail]` (portraits) or the single line given (other assets).
2. **Consistency:** attach reference image(s) before prompting. Char 1 → attach the Higgsfield keepers. Char 2 & 3 → generate their **Stage-3 anchor first**, approve it, then attach it for their other 5 stages.
3. **Always text-free** — Yoruba names composited in Unity (TMP).
4. **Transparency:** Nano Banana can't reliably output alpha — generate motifs / frames / FX on **pure black**, then key/cut out (additive/screen blend for glows).
5. **Sizes:** generate 2K–4K, downscale to manifest px. Portraits/backgrounds tall; motifs/frames/icon 1:1; buttons/bars wide.
6. **Red lines still bind** (`ART_BIBLE` §7) and the **pre-launch native-speaker review is still mandatory.**

---

## A — Stage Portraits · 3 characters × (6 humble stages + 1 royal Ascended) = **21**

**Style lead** (start every portrait prompt with this):
> Painterly cosmic-myth illustration, soft gouache brushwork, Gris / Sky: Children of the Light feel, NOT photoreal, NOT glossy, NOT 3D; warm gold light on deep indigo (#1B2150 + Àṣẹ gold #F4C14E), soft bloom, paper-grain, text-free.

**Negative tail** (end every portrait prompt with this):
> Bare neck — no necklace, no beads, no jewellery, no regalia, no mask, no headdress, no crown, no ritual staff; a quiet humble human, not a deity, no halo, no throne, no second face in the light.

**Identity clauses (one per character):**

| # | Character | `[IDENTITY]` clause | Reference to attach |
|---|---|---|---|
| 1 | The lean man (exists) | a humble West African man, calm gentle face, short cropped hair, ordinary slim build | Attach Higgsfield keepers: `cultivator__nano_banana_pro__KEEP` + `stage0_child` + `stage5_victor` |
| 2 | The woman | a humble West African woman, calm dignified face, short natural coils or a simple head-wrap, ordinary build | None yet → make her **Stage 3** first, approve, then attach to her other 5 stages |
| 3 | The big man | a humble West African man, large, heavy-set, powerfully-built, broad and tall, with a calm **gentle** face — strength in build only, never aggressive, never a warrior | None yet → make his **Stage 3** first, approve, then attach to his other 5 stages |

**Stage bodies (slot your `[IDENTITY]` in; wrap with the style lead + negative tail):**

| Stage | Name (meaning) | Body |
|---|---|---|
| 0 | Ọmọ Ayé (Child of the World) | Centered chest-up portrait of `[IDENTITY]`, rendered as a young child about 7, same identity in child form; plain undyed earth-toned child's tunic, humble; a single faint shy gold spark at the chest, almost no glow; warm marketplace-dusk haze far behind; deep indigo ground, faint abstract stars. |
| 1 | Akẹ́kọ̀ọ́ (The Learner) | Centered chest-up portrait of `[IDENTITY]`, rendered as an older child / early teenager; everyday earth-toned tunic with a learner's shoulder wrap; a small steady warm-gold chest-glow with a few gold motes orbiting; quiet study corner, soft warm lamplight. |
| 2 | Awo (The Initiate) | Centered chest-up portrait of `[IDENTITY]`, as a youth about 16–18, calm and earnest; plain ordinary initiate's wrap, path-neutral; warm gold glow spreading from chest to the open hands, a loose halo of gold motes around the hands; standing at a threshold into deeper indigo. |
| 3 | Aláàṣẹ (Wielder of Àṣẹ) | Centered chest-up portrait of `[IDENTITY]`, as a young adult, calm and dignified; finer everyday cloth; àṣẹ gathers where they gesture, a strong warm chest-glow + a thin gold rim-light; Ọ̀run-facing, first stars emerging. **← ANCHOR stage — generate this first for Chars 2 & 3.** |
| 4 | Àgbà (The Elder) | Centered chest-up portrait of `[IDENTITY]`, as a dignified elder, grey at the temples, lined wise serene face; elder's ordinary earth-toned wrap; body softly wreathed in warm gold, constant motes, a faint field of abstract gold star-points behind (points of light only, no figures); high indigo, road home far below. |
| 5 | Aṣẹ́gun (The Victor) | Centered chest-up portrait of `[IDENTITY]`, as a venerable radiant elder, grey-haired, serene and complete — still HUMBLE, no crown yet; humble wrap now luminous; fully wreathed in radiant àṣẹ gold, near-translucent edges, bright abstract gold constellations behind (no figures); the river's edge, far-bank warm equal lights. |
| ✦ | **Ascended** (post-Crossing, ROYAL — drops the humble tail) | Centered chest-up portrait of `[IDENTITY]`, at the moment of SUCCESSFUL ascension after the Crossing — no longer humble but exalted and ROYAL: rich aṣọ-òkè handwoven prestige cloth with gold thread, a beaded royal crown (adé), a royal coral collar, gold adornment (secular royalty of an exalted Oba/queen, celebratory). Fully wreathed in radiant àṣẹ gold, near-translucent glowing edges, bright abstract gold constellations behind (no figures); triumphant, ascending into light. **Instead of the humble tail:** NOT priestly — no orisha-devotee ìlẹ̀kẹ̀, no divining chain/tools, no egungun, no priestly vestments; a human ascending in royal splendour, not a deity, no halo-throne, no second face in the light. |

**21-portrait checklist:** each character × {0, 1, 2, 3, 4, 5, ✦Ascended}. Make each char's Stage 3 first (the anchor), then the other stages against it. Stages 0–5 humble (negative tail); ✦Ascended is the royal exception (drops the tail).

> **Ascension = royal — RESOLVED (design rule, `PROBE_FINDINGS_AND_PLAN.md` §11):** the crown goes on **after the Crossing succeeds**, not at Stage 5. So **stages 0–5 (incl. Aṣẹ́gun the Victor) stay HUMBLE** (use the negative tail); the separate **✦Ascended reveal** (post-Crossing, one per character) is the royal exception — aṣọ-òkè gold-thread cloth, a beaded royal crown (adé), royal coral collar, gold (secular royalty). For that row, **drop the humble negative tail** and steer *away from sacred/priestly items* (no orisha-devotee ìlẹ̀kẹ̀, no divining tools, no egungun, no priestly vestments). The reveal shows the crowned cultivator dissolving into light; the **ancestors on the council cards stay abstract** (§7.3). The **fall** outcome stays humble-dimmed. Flag this regalia for the §7.10 native-speaker review.

---

## D — Backgrounds (6) · 1320×2868 opaque, full-bleed, tall (9:16+)

| ID | Purpose | Gemini prompt |
|---|---|---|
| **D1** | Main idle (ref exists) | Painterly cosmic-myth, soft gouache, Gris/Sky feel, NOT photoreal, warm gold on deep indigo (#1B2150 + #F4C14E), soft bloom, paper-grain, text-free, vertical full-bleed. A vast deep-indigo night sky with slow painterly cloud-banding and a soft scatter of stars; a warm gold horizon-glow low and centered, gold motes drifting upward; three depth planes. Centre vertical band and lower third kept calm for UI. *(Higgsfield `bg_main_idle…KEEP` is the reference.)* |
| **D2** | Ayé / Marketplace | [same lead] A warm West African dusk marketplace dissolving into painterly haze — stalls, cloth, lamplight as warm golden glints, the bustle implied as light only. IMPORTANT: no discernible human faces or figures — crowd as warm light/glints only. Lowest-indigo, warmest scene; subtle adire cloth ground texture. No modern signage. Lower third calm for UI. |
| **D3** | Ọ̀run / Road Home | [same lead] A road winding upward through deepening indigo toward a single distant warm-gold light, stars emerging; cooler, more vertical, reverent. The far light is a calm glow, never a figure. Lower third calm for UI. |
| **D4** | Crossing — river | [same lead, + cool teal river reflections] A calm night river under deep indigo; far bank a scatter of small, roughly-equal warm-gold lights and soft drum-glows (death-as-homecoming). Near bank dark/intimate; reflective river, broken gold reflections. CRITICAL: no deity/judge/gatekeeper/presiding figure; far-bank lights equal, none dominant; no face/figure/throne in sky or water. |
| **D5** | Crossing — storm | [same lead] The same river under a storm-charged indigo sky — closing storm vignette, distant lightning, electric sky; majestic awe, NOT menace/horror. Far-bank lights still faintly visible, equal. No deity, no figure, no face in the clouds. |
| **D6** | Title / launch | [same lead, near-black #0E1330→#1B2150] Near-black indigo, low marketplace horizon into a star-rich sky; a single gold àṣẹ thread rising to a faint constellation. Generous calm space for a title + one text line in-engine — render no text. |

---

## B — Path Motifs (3) · 512×512, on black, cut out

| ID | Path | Gemini prompt |
|---|---|---|
| **B1** | Ane (Igala earth) | Painterly cosmic-myth emblem, soft gouache, Gris feel, NOT photoreal, text-free, on plain black. Abstract emblem of the Igala earth: layered earth/stone strata + steadfast mountain silhouette with deep roots, gold àṣẹ ore-seams through rock, calm stars over land; woven concentric earth-ring geometry. Black + maize-gold over ochre + earth-green (#1A150F, #E8C24A, #C28A3A, #5C7A4A, gold #F4C14E). Mineral, patient, heavy. No person, no earth-mother, no face, no figure, no animal. |
| **B2** | Sango (Yoruba thunder) | [same emblem lead] Abstract emblem of thunder and sudden force: a raw forked bolt of lightning splitting two ways through a dark storm cloud, sharp and bright with sparks, the only hard-edged shape, a thread of gold àṣẹ along the bolt. Vermilion + hot white over indigo (#C8412B, #FFF3D6, #F2A33C, #1B2150, #F4C14E). NO axe, NO double-axe, no weapon, no person, no king, no warrior, no figure, no face, no deity — only the strike. |
| **B3** | Osun (Yoruba river, ref exists) | [same emblem lead] Abstract emblem of a sweet river: a gently curving river of luminous water flowing then pooling, concentric ripple rings, a single small iridescent peacock-eye glint. **Push WARMER than the reference — brass and honey-gold dominant** (#C9A24B brass, #F4C14E gold) with cool teal accents (#2E8C8C, #BFE6DD) over indigo. Soft, generous, flowing. No woman, no goddess, no person, no figure, no face, no mermaid, no bird, no animal. *(Higgsfield `motif_osun…KEEP` is the reference; it ran too cool.)* |

---

## C — Ancestor Card Art (4) · on black, cut out

| ID | Purpose | Gemini prompt |
|---|---|---|
| **C1** | Radiance frame | Painterly cosmic-myth, soft gouache, text-free, on plain black. An ornate abstract card-border frame with an empty open centre, bright luminous gold — radiant border, constellation/star sparkle, warm halo (#F4C14E + #FFE6A8 over indigo). Light and stars only — no figure, no face, no mask; centre empty. |
| **C2** | Ember frame | [same lead] The same border frame with empty centre, dimmed to warm ember — coal orange to deep clay (#D2742F→#7A2E1E), gentle rising sparks; honoured and warm, NEVER grey, NEVER red-as-failure, never death/skull/horror. No figure/face/mask. |
| **C3** | Empty council slot | [same lead] A faint unlit abstract slot outline for an empty council seat, secular household forms only — a calabash gourd, a plain carved post, a small secular hearth-flame (NOT an offering bowl, NOT a ritual staff, NOT an egungun mask). Dim indigo, thin gold edge. No figure, no mask, no offering. |
| **C4** | Foundation | [same lead] An abstract horizontal band of bedrock/earth-strata with a flowing river of warm gold light along it — "the foundation of the house," the merged lineage. Earth and a river of light only — no figures, no faces, no masks. |

---

## E — UI Skin (8) · 9-slice, on black, crisp corners / plain centre

| ID | Purpose | Gemini prompt |
|---|---|---|
| **E1** | Panel | Clean painterly UI, text-free, on black. Rounded-rectangle panel: dark indigo glass fill, faint adire woven-geometry ghost texture, thin 1px warm-gold edge; plain stretchable centre, crisp corners. |
| **E2** | Primary button | [same UI lead] Softly-rounded primary button: deep indigo fill, thin Àṣẹ-gold rim-light, gentle baked gold inner-glow; plain centre. |
| **E3** | Primary disabled | [same lead] Same primary button, disabled-yet-visible: dimmer indigo, faint gold rim, muted inner-glow — still warm, not grey-dead. |
| **E4** | Secondary button | [same lead] Softly-rounded secondary button: indigo-deep fill, thin muted-gold edge, no inner glow, calmer than primary. |
| **E5** | Progress-bar track | [same lead] Horizontal progress-bar track: deep-indigo recessed channel, thin gold edge, empty, for horizontal 9-slice. |
| **E6** | Progress-bar fill | [same lead] Horizontal fill of liquid Àṣẹ gold with a brighter leading edge (#F4C14E→#FFE6A8), for a left-anchored horizontal fill. |
| **E7** | Council slot frame | [same lead] Small ornate gold-edged slot frame, adire (Eléko) woven-grid geometry with calabash / carved-post / hearth-flame motif — explicitly secular: NOT an egungun mask, NOT a ritual staff, NOT an offering bowl. Empty outlined centre. |
| **E8** | Path choice card | [same lead] Tall choice-card frame: indigo glass, faint adire ghost texture, thin gold edge, empty centre to composite a motif into later. |

---

## F — Icon & Launch (2)

| ID | Purpose | Gemini prompt |
|---|---|---|
| **F1** | App icon · 1024² OPAQUE | Painterly cosmic-myth, bold/legible, text-free, fully opaque (no transparency), no pre-rounded corners, fills the square. A single thread/spark of Àṣẹ gold (#F4C14E) rising into a stylised constellation against deep indigo (#1B2150); high-contrast, readable small. No figure, no deity, no mask, no text. |
| **F2** | Launch screen · 1320×2868 opaque | [style lead, vertical opaque] The title composition: near-black indigo, low horizon, a single rising gold àṣẹ thread to a faint constellation; calm, static, room for a title in-engine (render no text). Matches D6. |

---

## G — Particle / FX Textures (8) · on PURE BLACK, additive/screen or cut out

| ID | Purpose | Gemini prompt |
|---|---|---|
| **G1** | Àṣẹ mote | A single soft round gold glow mote (#F4C14E), smooth falloff to black, no hard edge, centred. On pure black. |
| **G2** | Collect burst | A single radial burst of warm gold sparks from a centre point, generous (not arcade), one frame. On pure black. |
| **G3** | Channel spark | A small bright gold spark / four-point star, crisp, centred. On pure black. |
| **G4** | Lightning | A single sharp white-gold forked lightning bolt, hard-edged, centred — the only hard-edged FX. No axe, no figure. On pure black. |
| **G5** | Storm vignette · full overlay | A dark edge vignette: charged indigo/blue-grey closing from the edges, centre clear; awe not menace. Vertical full frame. |
| **G6** | Whiteout | A full soft warm-white gradient field, gently glowing, for a held-breath fade. On pure black. |
| **G7** | Ascend rays | Upward radiant gold god-rays fanning from a low centre, warm and luminous. On pure black. |
| **G8** | Fall embers | A soft cluster of warm glowing embers/sparks rising gently, warm orange-gold — NEVER grey, NEVER red-as-failure, no skulls. On pure black. |

---

## H — Audio (10) · NOT a Gemini job
Use an audio generator (Suno/Udio for the 4 BGM loops; ElevenLabs or Higgsfield `mirelo` for the 5 SFX + 1 stinger). Briefs in `ASSET_MANIFEST.md` §H / `ART_BIBLE` §6.

---

## Tally (rev 2)
**Visual: 52** — Portraits **21** (3 × 6 humble stages + 1 royal ✦Ascended) · Backgrounds 6 · Motifs 3 · Ancestor frames 4 · UI 8 · Icon/Launch 2 · FX 8.
**Audio: 10** (separate tool). **Grand total 62.**
Supersedes the manifest's 37 visual (portraits 6 → 21 for the 3-character roster + the royal Ascended reveal). Generate text-free, run each past `ART_BIBLE` §9, downscale to manifest sizes, hold for the pre-launch native-speaker review.
