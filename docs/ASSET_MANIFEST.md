# ASSET_MANIFEST.md — Ori Ascendant

**Companion to:** `ART_BIBLE.md` (art direction & §7 red lines)
**Version:** 1.0 · **Status:** Production-ready
**Scope:** 37 static visual assets (8 categories) + 10 audio assets = **47 total**
**Grounded in:** `/home/baba/ASE/docs/GAMEPLAY.md` (§3 dims, §3.5 Crossing, §3.6 council, §5.4 ancestors, §7 red lines), `/home/baba/ASE/docs/research/cosmology.md`

> Every prompt-bearing asset carries the §7 red lines (ART_BIBLE §7): no supreme deity; no egungun masks/regalia; ancestors abstract only; orisha as patrons (motif/light only, never figures, **no double-axe for Sango**); zero voodoo tropes; traditions labeled distinctly (Ane = Igala, Sango/Osun = Yoruba); full Yoruba diacritics in any in-engine text (AI art generated text-free).

---

## Dimensioning logic (read first)

All pixel sizes reason from the **iPhone 12 baseline: 390×844 pt @3× = 1170×2532 px**. Unity CanvasScaler "Scale With Screen Size" 390×844, match 0.5 (GAMEPLAY §3).

| Class | Reasoning | Generate at | Notes |
|---|---|---|---|
| **Backgrounds** | Must full-bleed cover taller devices (iPhone 14 Pro Max ≈ 430×932pt); add ~13% margin both axes for safe-area + parallax. | **1320×2868** opaque | No alpha. Import as Sprite, full-rect, no 9-slice. Safe-zone focal content inside central 1170×2532. |
| **Portraits** | Shown in the portrait zone (~24–58% height ≈ 720px @3×); generating that small is too low for a premium look. | **1024×1024** transparent | One cultivator, character-consistent across all 6, regalia-FREE. |
| **Path motifs/badges** | Rendered inline ~24–40pt across MainScreen, PathScreen, ancestor cards. | **512×512** transparent | Abstract motif only — earth/stars, lightning/flame, river/light. Never a figure. |
| **Ancestor frames** | Council slots 56pt ≈ 168px @3×; CouncilScreen cards larger; must stay crisp small. | **768×768** transparent | Two variants: radiance (ascend) / ember (fall). |
| **App icon** | App Store / springboard master is 1024×1024 (Apple); all sizes auto-derived. | **1024×1024** opaque | No alpha (Apple rejects transparent icons); no pre-rounded corners. |
| **UI 9-slice** | 9-slice scales the center; only the safe border needs resolution. | **512×512** (panels) / per-aspect | Set 9-slice borders in Sprite Editor; keep corners inside the border. |
| **Particle textures** | Single-particle sprites multiplied by the system; small, soft, premultiplied-friendly. | **256×256** → **512×512** | Soft alpha falloff, no hard edges except lightning. |

**Finalization:** run **upscale_image** on the 1024 portraits + icon before downscale/import (max edge fidelity); run **remove_background** on any motif/cutout/FX that returns a non-transparent field.

---

## Category A — Stage Portraits (6) — `soul_2` + trained Soul

**Character consistency is the hard requirement.** One single cultivator, the same person, aging across all six: child → youth → adult → mature → elder → victor-elder. Train a **Soul** on the stage-1 face first (ART_BIBLE §10), then generate stages 2–6 against that Soul. Attire **ordinary / regalia-FREE**; aura/àṣẹ-gold escalates each stage; deep-indigo painterly ground.

| ID | Filename | Stage — Name (meaning) | Visual brief (escalating age + aura, regalia-free) | Unity slot | Generate | Format | Model |
|---|---|---|---|---|---|---|---|
| A1 | `portrait_stage1_omo_aye.png` | 0 — Ọmọ Ayé (Child of the World) | Young child (~7), plain undyed wrap; single shy gold chest-spark; wide-eyed in marketplace dusk | StageConfig[0].portraitSprite | 1024×1024 | PNG, alpha | soul_2 + Soul |
| A2 | `portrait_stage2_akekoo.png` | 1 — Akẹ́kọ̀ọ́ (The Learner) | Older child/early teen, fuller everyday dress + learner's wrap; small steady chest-glow, a few orbiting motes; lamplit study corner | StageConfig[1].portraitSprite | 1024×1024 | PNG, alpha | soul_2 + Soul |
| A3 | `portrait_stage3_awo.png` | 2 — Awo (The Initiate) | Youth (~16–18), plain initiate wrap, path-neutral; glow spreads to the hands, loose mote-halo; threshold into deeper indigo | StageConfig[2].portraitSprite | 1024×1024 | PNG, alpha | soul_2 + Soul |
| A4 | `portrait_stage4_alaase.png` | 3 — Aláàṣẹ (Wielder of Àṣẹ) | Young adult, finer everyday cloth with **subtle path-accent trim** (Ane ochre / Sango amber / Osun teal); àṣẹ gathers where they gesture; strong glow + rim-light; Ọ̀run-facing, first stars | StageConfig[3].portraitSprite | 1024×1024 | PNG, alpha | soul_2 + Soul |
| A5 | `portrait_stage5_agba.png` | 4 — Àgbà (The Elder) | Greying elder, lined dignified face, elder's everyday wrap (accent stronger, still cloth); body softly wreathed in gold, constant motes, faint constellation behind | StageConfig[4].portraitSprite | 1024×1024 | PNG, alpha | soul_2 + Soul |
| A6 | `portrait_stage6_asegun.png` | 5 — Aṣẹ́gun (The Victor) | Venerable radiant elder at the river's edge, ọ̀run-facing; fully wreathed in Àṣẹ gold, near-translucent edges, bright constellations; serene, complete | StageConfig[5].portraitSprite | 1024×1024 | PNG, alpha | soul_2 + Soul |

**Red-line guards (all 6):** no egungun mask/regalia, no priestly office vestments, no Ifá/divination objects, no deity figure behind them; any elder staff is a plain support staff only. Just a person and gold light. **Finalize each: upscale_image → remove_background** (clean alpha for the mote-drift particle layer).

---

## Category B — Path Motifs / Badges (3) — `nano_banana_pro`

Abstract motif badges, one per path, traditions kept **distinct** (§7.7). Used on MainScreen identity line, PathScreen cards, and composited into ancestor cards. Documented attributes only; **never a figure of the orisha** (§7.5).

| ID | Filename | Path / Tradition | Visual brief (documented attributes, abstract) | Unity slot | Generate | Format | Model |
|---|---|---|---|---|---|---|---|
| B1 | `motif_ane_earth.png` | **Ane — Igala earth, endurance** | Layered strata + steadfast mountain + deep roots + steady stars; black + maize-gold over ochre/earth-green; gold àṣẹ ore-seams; woven earth-ring geometry (Igala, NOT Igbo Ala / Yoruba Ilẹ̀) | PathConfig[0].motifSprite | 512×512 | PNG, alpha | nano_banana_pro |
| B2 | `motif_sango_thunder.png` | **Sango — Yoruba thunder, sudden force** | A raw forked lightning bolt splitting two ways + storm cloud + spark; vermilion red + bright white over indigo; sudden force/justice. **NO axe** | PathConfig[1].motifSprite | 512×512 | PNG, alpha | nano_banana_pro |
| B3 | `motif_osun_river.png` | **Osun — Yoruba river, lineage/flow** | Flowing river lines + concentric ripples + sweet honey-gold light + brass sheen + a single abstract peacock-eye glint; flow/fertility/lineage | PathConfig[2].motifSprite | 512×512 | PNG, alpha | nano_banana_pro |

**Guards:** Sango = lightning only (the double-axe oṣẹ́ Ṣàngó is forbidden, even as a flat glyph); Osun = water/light, never a woman/mermaid/bird; no marriage-drama or pet/boss framing; three must be visually distinct. **Finalize: remove_background.**

---

## Category C — Ancestor Card Art (4) — `nano_banana_pro`

Ancestors are **ABSTRACT ONLY** — light/river/flame/stars, **never figures, never masks** (§7.3, §5.4). Card = a path-agnostic frame + a composited path motif (B1–B3) at runtime.

| ID | Filename | Purpose | Visual brief | Unity slot | Generate | Format | Model |
|---|---|---|---|---|---|---|---|
| C1 | `ancestor_frame_radiance.png` | Ascended ancestor frame (×1.0) | Gold radiant border, constellation/star sparkle, warm halo — a good crossing | AncestorCardView.radianceFrame | 768×768 | PNG, alpha | nano_banana_pro |
| C2 | `ancestor_frame_ember.png` | Fallen ancestor frame (×0.4) | Warm ember/glowing-coal border, dimmer but **never grey/red**, rising sparks — honored, not denied | AncestorCardView.emberFrame | 768×768 | PNG, alpha | nano_banana_pro |
| C3 | `ancestor_slot_empty.png` | Empty council slot outline | Faint carved-post outline, unlit; "a seat awaiting". **Per-asset steer (ART_BIBLE §5.6):** calabash = HOUSEHOLD vessel, NOT an offering bowl or altar; flame is secular hearth-light, NOT a ritual/ẹbọ flame | CouncilStrip.emptySlotSprite | 512×512 | PNG, alpha | nano_banana_pro |
| C4 | `ancestor_foundation.png` | Retired-ancestor "foundation of the house" art | Abstract bedrock/earth-strata + a flowing river of light the lineage stands on (permanentAseBonus visual). **Per-asset steer (ART_BIBLE §5.6):** calabash = HOUSEHOLD vessel, NOT an offering bowl or altar; flame is secular hearth-light, NOT a ritual/ẹbọ flame | CouncilScreen.foundationSprite | 768×512 | PNG, alpha | nano_banana_pro |

**Guards:** zero faces, zero egungun cloth, zero skulls; pure light/flame/star/river abstraction; ember reads warm-dim, never death/horror. **Finalize: remove_background.**

---

## Category D — Backgrounds (6) — `soul_2` / `nano_banana_pro`

Full-bleed opaque painterly backgrounds, deep-indigo cosmic-myth palette. Generate at **1320×2868** to cover tall devices; focal content safe-zoned inside the central 1170×2532.

| ID | Filename | Purpose | Visual brief | Unity slot | Generate | Format | Model |
|---|---|---|---|---|---|---|---|
| D1 | `bg_main_idle.png` | MainScreen ambient backdrop | Deep indigo night, slow cloud-banding, scatter of stars; warm gold horizon-glow with upward àṣẹ motes; calm, premium; portrait zone (24–58%) clean, lower third UI-clear | MainScreen.backgroundImage | 1320×2868 | PNG, opaque | soul_2 |
| D2 | `bg_tier0_aye_marketplace.png` | Tier 0 — "Ayé / the Marketplace" | Warm West African dusk marketplace dissolving into haze; stalls/cloth/lamplight as warm glints (not detailed crowds, no modern signage); lowest indigo; Ibadandun adire ground texture. **D2-specific negative (the one populated scene):** no discernible human faces or figures; crowd implied as warm light/glints only | TierBackground[0] | 1320×2868 | PNG, opaque | nano_banana_pro |
| D3 | `bg_tier1_orun_road_home.png` | Tier 1 — "Ọ̀run / the Road Home" | A road winding upward through deepening indigo toward a distant warm light; cooler, more vertical, stars emerging; the crossing implied ahead | TierBackground[1] | 1320×2868 | PNG, opaque | soul_2 |
| D4 | `bg_crossing_river.png` | The Crossing (Ìrékọjá) base | A night river; far bank a scatter of **equal** warm lineage-lights + drum-glows; near bank dark/intimate; death-as-homecoming, NOT judgment, NO figure | TribulationScreen.riverBackground | 1320×2868 | PNG, opaque | soul_2 |
| D5 | `bg_crossing_storm.png` | Crossing — storm/buildup state | Same river under storm vignette, distant lightning, charged sky (80–100% escalation); majestic awe, not menace | TribulationScreen.stormBackground | 1320×2868 | PNG, opaque | soul_2 |
| D6 | `bg_title.png` | Title / first-launch | Near-black indigo, low marketplace horizon → star-rich sky, a single rising gold àṣẹ thread to a faint constellation; room for title + proverb (text in-engine) | TitleScreen.backgroundImage | 1320×2868 | PNG, opaque | soul_2 |

**Guards (esp. D4/D5):** far bank shows **lights and drums, never a deity figure as judge** (§7.1); no rendered Olódùmarè/Ọlọ́run/Ọjọ́; far-bank lights are equal abstract points, none dominant. Pareidolia-check the sky/light for any face/figure/throne.

---

## Category E — UI Skin (9-slice) (8) — `nano_banana_pro`

Crisp UI chrome, painterly-but-clean, gold-on-indigo with adire textile accent. All **9-slice** with documented safe borders (corners inside the border, center stretchable).

| ID | Filename | Purpose | Unity slot | Generate | 9-slice border | Format | Model |
|---|---|---|---|---|---|---|---|
| E1 | `ui_panel.png` | Generic modal/panel bg (Welcome Back, confirm sheets, About) | UISkin.panelSprite | 512×512 | 48px | PNG, alpha | nano_banana_pro |
| E2 | `ui_button_primary.png` | Primary CTA ("Advance" / "Face the Tribulation" / "Collect") | UISkin.primaryButtonSprite | 512×256 | L/R 48, T/B 40 | PNG, alpha | nano_banana_pro |
| E3 | `ui_button_primary_disabled.png` | Disabled-but-visible CTA state | UISkin.primaryButtonDisabledSprite | 512×256 | L/R 48, T/B 40 | PNG, alpha | nano_banana_pro |
| E4 | `ui_button_secondary.png` | Card / "Walk this Path" / settings buttons | UISkin.secondaryButtonSprite | 512×256 | L/R 48, T/B 40 | PNG, alpha | nano_banana_pro |
| E5 | `ui_progressbar_bg.png` | Stage/Tribulation bar track (indigo-deep) | UISkin.progressBarBg | 512×96 | L/R 24, T/B 24 | PNG, alpha | nano_banana_pro |
| E6 | `ui_progressbar_fill.png` | Bar fill — liquid Àṣẹ gold, brighter leading edge (Image type Filled-Horizontal, left-anchored) | UISkin.progressBarFill | 512×96 | L/R 24, T/B 24 | PNG, alpha | nano_banana_pro |
| E7 | `ui_council_slot_frame.png` | Council slot frame — Eléko adire geometry, carved-post/calabash/flame motif, **NOT egungun**; one sprite instanced 5×. **Per-asset steer (ART_BIBLE §5.6):** calabash = HOUSEHOLD vessel, NOT an offering bowl or altar; flame is secular hearth-light, NOT a ritual/ẹbọ flame | CouncilStrip.slotFrameSprite | 256×256 | L/R 32, T/B 32 | PNG, alpha | nano_banana_pro |
| E8 | `ui_pathcard.png` | PathScreen choice-card frame (×3 reused, motif composited in) | PathScreen.cardFrameSprite | 512×768 | L/R 48, T/B 64 | PNG, alpha | nano_banana_pro |

**Note:** the 5 council slot frames are **one reused sprite** (E7), instanced 5× — not 5 files. **Guard (E7/CouncilScreen):** carved posts / calabash / flame only, explicitly NOT egungun masks (§3.6, §7.3); no Odù marks used as ornament; keep adire purely geometric.

---

## Category F — App Icon & Launch (2) — `nano_banana_pro`

| ID | Filename | Purpose | Visual brief | Unity slot | Generate | Format | Model |
|---|---|---|---|---|---|---|---|
| F1 | `app_icon_1024.png` | App Store / springboard master | A single ascending àṣẹ-gold thread/spark into a stylized constellation over deep indigo; legible at 60px; no text, no figure | PlayerSettings iOS icon (1024) | 1024×1024 | PNG, **opaque** | nano_banana_pro |
| F2 | `launch_screen.png` | iOS launch screen | Static near-black indigo with the rising gold thread; matches D6; no spinner, no localizable text | LaunchScreen.storyboard image | 1320×2868 | PNG, opaque | nano_banana_pro |

**Guards:** icon must be **opaque, no alpha, no pre-rounded corners** (Apple applies the mask and rejects transparency); no deity, no hand-from-sky, no mask, no text. **Finalize F1: upscale_image.**

---

## Category G — Particle / FX Textures (8) — `nano_banana_pro` + remove_background

Single-particle sprites multiplied by Unity particle systems / tweens. Soft alpha, additive-blend-friendly.

| ID | Filename | Purpose | Visual brief | Unity slot | Generate | Format | Model |
|---|---|---|---|---|---|---|---|
| G1 | `fx_ase_mote.png` | Ambient àṣẹ motes (portrait drift + main idle) | Soft round gold glow, falloff to transparent | FX/AseMotes material | 256×256 | PNG, alpha | nano_banana_pro |
| G2 | `fx_collect_burst.png` | Collect / Welcome-Back burst | Radial gold spark burst, single frame; generous, not arcade | FX/CollectBurst material | 512×512 | PNG, alpha | nano_banana_pro |
| G3 | `fx_channel_spark.png` | Tap-to-channel pulse (+N) | Small bright spark / star | FX/ChannelSpark material | 256×256 | PNG, alpha | nano_banana_pro |
| G4 | `fx_lightning.png` | Crossing storm strikes (3× @1s) | Sharp white-gold forked bolt on transparent (the only hard-edged FX) | FX/Lightning material | 512×512 | PNG, alpha | nano_banana_pro |
| G5 | `fx_storm_vignette.png` | 80% escalation storm overlay | Dark edge vignette, charged indigo/blue-grey, center clear; awe not menace | FX/StormVignette overlay | 1320×2868 | PNG, alpha | nano_banana_pro |
| G6 | `fx_whiteout.png` | "Held breath" whiteout (5–6.5s beat) | Full soft warm-white gradient field for the silent fade | FX/Whiteout overlay | 512×512 | PNG, alpha | nano_banana_pro |
| G7 | `fx_ascend_rays.png` | Ascend reveal — gold god-rays | Radiant upward gold light rays | FX/AscendRays material | 512×768 | PNG, alpha | nano_banana_pro |
| G8 | `fx_fall_embers.png` | Fall reveal — rising warm embers | Soft glowing ember/spark cluster rising, **warm (never grey/red)** | FX/FallEmbers material | 256×512 | PNG, alpha | nano_banana_pro |

**Guards (G7/G8):** ascend = gold radiance; fall = warm embers, never grey/red, no buzzer-visual (§3.5); no skulls/dark imagery anywhere; pareidolia-check G2/G7 light clusters for accidental faces/figures (§7.1). **Finalize: remove_background.**

**In-engine composites (no separate deliverable — do NOT generate as new assets):**
- **0–2s ceremony silhouette transition frame** (ART_BIBLE §5.5, GAMEPLAY §3.5): **composited in-engine** from the Stage-6 portrait **A6 (`portrait_stage6_asegun.png`)** — silhouetted/backlit via shader against the Crossing backgrounds (D4/D5). No new visual asset is required for this slot.
- **Ascend reveal** (ART_BIBLE §5.5 — figure rises and dissolves into light): **composited in-engine** from the Stage-6 portrait **A6** + **G7 (`fx_ascend_rays.png`)** over the **D4/D5** Crossing backgrounds. No separate composition deliverable.
- **Fall reveal** (ART_BIBLE §5.5 — figure kneels and dissolves into sparks): **composited in-engine** from the Stage-6 portrait **A6** + **G8 (`fx_fall_embers.png`)** over the **D4/D5** Crossing backgrounds. No separate composition deliverable.

---

## Visual totals & model summary

### Total visual asset count: **37**

| Category | Count |
|---|---|
| A — Stage Portraits | 6 |
| B — Path Motifs/Badges | 3 |
| C — Ancestor Card Art | 4 |
| D — Backgrounds | 6 |
| E — UI Skin (9-slice) | 8 |
| F — Icon & Launch | 2 |
| G — Particle/FX Textures | 8 |
| **Total visual** | **37** |

### Model assignment summary

| Model | Used for | Asset count |
|---|---|---|
| **soul_2 + trained Soul** | 6 stage portraits (character consistency) | 6 |
| **soul_2** | painterly backgrounds (D1, D3, D4, D5, D6) | 5 |
| **nano_banana_pro** | icon, all UI, motifs, ancestor frames, FX, marketplace bg (B×3, C×4, D2, E×8, F×2, G×8) | 26 |
| **remove_background** (post-pass) | portraits, motifs, ancestor cutouts, FX (clean alpha) | all transparent assets |
| **upscale_image** (post-pass) | finalize the 1024 portraits + icon (A1–A6, F1) | 7 |

---

## Audio assets (10) — `sonilo_music` (BGM) / `mirelo` (SFX & stinger)

Out of the 37 visual count; tracked here for completeness. **Format:** 48 kHz WAV masters; BGM → OGG Streaming; SFX → OGG Mono; all loops seamless; 10 null-safe assets. Red-line note: NO sampled bàtá liturgy as cues; fall = soft kora, NOT a buzzer (§7.4).

| ID | Filename | Type | Slot purpose | Spec | Model |
|---|---|---|---|---|---|
| H1 | `bgm_menu_aye.ogg` | BGM (loop) | Menu / title | ~48s, 76 BPM — kora-harp, balafon, shekere, frame drum, gold pad; contemplative; no bàtá; wordless vowels only. **Loop acceptance:** loop on a whole number of bars at 76 BPM (4/4) — target **16 bars** (≈50.5s); zero-crossing trim; no audible seam | sonilo_music |
| H2 | `bgm_path_ane.ogg` | BGM (loop) | Ane theme (Igala earth, ×1.5 offline) | ~56s, 66 BPM — frame drum + udu ostinato, slow balafon, low drone, kalimba; grounded/patient. **Loop acceptance:** loop on a whole number of bars at 66 BPM (4/4) — target **16 bars** (≈58.2s); zero-crossing trim; no audible seam | sonilo_music |
| H3 | `bgm_path_sango.ogg` | BGM (loop) | Sango theme (Yoruba thunder, ×2 active) | ~40s, 112 BPM — dùndún talking-drum *melodic* (NOT ritual cadence), frame drums + shekere, balafon, storm swell. **Loop acceptance:** loop on a whole number of bars at 112 BPM (4/4) — target **19 bars** (≈40.7s); zero-crossing trim; no audible seam | sonilo_music |
| H4 | `bgm_path_osun.ogg` | BGM (loop) | Osun theme (Yoruba river, council ×2) | ~52s, 88 BPM, 6/8 — kora-harp arpeggio, balafon, shekere, vowel humming, river ambience; lyrical. **Loop acceptance:** loop on a whole number of bars at 88 BPM (6/8, dotted-quarter pulse) — target **38 bars** (≈51.8s); zero-crossing trim; no audible seam | sonilo_music |
| H5 | `sfx_advance.ogg` | SFX (mono) | Stage advance | ~0.8s — balafon/kalimba + gold bell; warm rising bloom | mirelo |
| H6 | `sfx_channel_tap.ogg` | SFX (mono) | Tap-to-channel | ~0.25s — muffled "tup," repeatable ~2×/sec, no reverb, no fatigue | mirelo |
| H7 | `sfx_collect.ogg` | SFX (mono) | Collect / Welcome Back | ~1.3s — gold cascade to a bell; NOT slot-machine | mirelo |
| H8 | `sfx_ascend.ogg` | SFX (mono) | Ascend reveal | ~2.5s — harp swell to major chord + vowel shimmer; the good crossing | mirelo |
| H9 | `sfx_fall.ogg` | SFX (mono) | Fall reveal | ~2.5s — soft kora, low down-then-up + embers; warm/dignified, **NEVER a buzzer** | mirelo |
| H10 | `stinger_irekoja.ogg` | Stinger | Tribulation (Ìrékọjá) | ~4.0s — heartbeat + riser + THREE thunder cracks, held note cut to total silence; no tell, no "judge," NOT bàtá | mirelo |

### Audio totals

| Type | Count |
|---|---|
| BGM | 4 |
| SFX | 5 |
| Stinger | 1 |
| **Total audio** | **10** |

---

## Grand total: **47 assets** (37 visual + 10 audio)

### Global red-line checklist (apply to EVERY prompt — ART_BIBLE §7)
1. No supreme deity rendered/named/stat-ified (Olódùmarè/Ọlọ́run/Ọjọ́) — incl. as Crossing judge.
2. No egungun masks/regalia anywhere; ancestors abstract (light/river/flame/stars).
3. No initiatory offices, Odù Ifá, 16-cowrie/ikin, igbodu, bàtá-as-spell.
4. Orisha = patrons via **documented attributes/motifs only**, never figures/collectibles/bosses; **no double-axe for Sango.**
5. Zero voodoo/witchcraft: no skulls, dark sacrifice, Ìyàmi/àjẹ́; fall art warm-dim never grey/red.
6. Label traditions distinctly: **Ane = Igala**; **Sango / Osun = Yoruba.** No pan-African "tribal" mush.
7. Any rendered text = full Yoruba diacritics (Akẹ́kọ̀ọ́, Aláàṣẹ, Aṣẹ́gun); generate AI art text-free, composite names in-engine (TMP, Noto Sans fallback).
8. **All cultural-form assets flagged for the pre-launch native-speaker / community review (§7.10) — the pipeline never self-certifies.**

### Source files this manifest is grounded in
- `/home/baba/ASE/docs/GAMEPLAY.md` — §2.2 stage table, §2.3 paths, §3 screens & dimensions, §3.5 Crossing, §3.6 council, §5.4 ancestor identity, §7 red lines.
- `/home/baba/ASE/docs/research/cosmology.md` — stage/tier naming, Ìrékọjá framing, ancestor-veneration structure, red-line rationale, diacritics requirement.
