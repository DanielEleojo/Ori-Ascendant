# Ori Ascendant — Art Probe Findings & Corrected Production Plan

**Status:** Probe phase complete · 2026-06-14 · Direction approved by Baba · Production run gated on credits.
**Companion to:** `ART_BIBLE.md` (art direction + §7 red lines) and `ASSET_MANIFEST.md` (asset table).
**Precedence:** where this doc and `ASSET_MANIFEST.md` disagree on **which model** to use, *this doc wins* — its model picks are evidence-based from live probes, the manifest's were a first guess.

---

## 1. What was validated
- The locked **"Painterly Cosmic Myth"** direction is approved: indigo-night + Àṣẹ-gold palette, Gris-style painterly technique (NOT photoreal/cel/3D), Yoruba **adire** (tie-and-dye) geometry as the motif + UI substrate.
- Touchstones confirmed as on-target: Gris (Nomada Studio), Sky: Children of the Light (thatgamecompany), Yoruba adire (Ibadandun / Eléko).

## 2. Probe results (5 images, ~5 credits)
| Probe | Model | Verdict |
|---|---|---|
| Main idle background | `soul_2` | PASS — painterly, palette + red-lines clean; but lower-res and the gold pooled at the very bottom (fights UI) |
| Main idle background | `nano_banana_pro` | **PASS — WINNER.** Better composition, calm UI-clear lower third, higher res |
| Cultivator, base | `soul_2` | **REJECT** — photoreal/glossy; added a beaded necklace (§7.2 violation); bare-chested idealized physique |
| Cultivator, ascended (ref-carry) | `soul_2` | **REJECT** — worse. An auto prompt-enhancer rewrote the brief to "hyper-realistic photograph," kept the beads, produced a bodybuilder with a power-blast flare (§7.5 collectible-character risk) |
| Cultivator | `nano_banana_pro` | **PASS — WINNER.** Painterly, humble clothed tunic, bare neck (no beads), soft àṣẹ glow, palette dead-on |

Probe files saved in `docs/art-probes/` (named `…__KEEP.png` / `…__REJECT.png`).

## 3. Corrected model assignments (supersede the manifest's model column)
- **Backgrounds (D1–D6) → `nano_banana_pro`**
- **Stage portraits (A1–A6) → `nano_banana_pro` + Element consistency** (see §4). **`soul_2` RETIRED.**
- Path motifs (B), ancestor frames (C), UI skin (E), icon/launch (F), FX (G) → `nano_banana_pro` (as manifest). **Budget option (validated 2026-06-14):** abstract emblems / motifs / FX can use `seedream_v5_lite` at **1 credit** (vs nano's 2) — the Osun river motif probe came back clean, on-palette, and red-line-safe. Caveat: seedream lite's NSFW filter is trigger-happy on violent-sounding words (the Sango "lightning / strike / bolt / thunder" prompt was false-flagged `nsfw`, no charge) — use gentle wording, or fall back to `nano_banana_pro` for the storm motif. Hero assets (portraits, backgrounds) stay on `nano_banana_pro`.
- Post-passes → `remove_background`, `upscale_image` (as manifest)
- Audio (H1–H10) → `sonilo_music` / `mirelo` (UNTESTED — validate with a probe before the batch)

**Why `soul_2` is retired:** it renders humans hyper-photoreal and re-injects red-line gear (beaded necklaces, bare chest) via an auto prompt-enhancer that rewrites prompts toward "photograph." It was only ever in the plan for its trainable Soul — which the Element system replaces.

## 4. Character consistency method (replaces the soul_2 "Soul")
- The cultivator is saved as a Higgsfield **Element**: name `ori-cultivator`, id **`ddcfd9ce-031c-44dd-80ef-55bf4265cd58`**.
- Reuse across all six stages by embedding `<<<ddcfd9ce-031c-44dd-80ef-55bf4265cd58>>>` in the `nano_banana_pro` prompt; vary **only** age + aura intensity (+ path accent at stages 3+).
- Free to reuse, nano-compatible, and avoids soul_2's photoreal lock.
- **Caveat:** the anchor is a young-adult male (placeholder gender — confirm or change before the batch). For Stage-0 (Ọmọ Ayé, a child) the Element is prompted younger; re-roll if identity drifts.

## 5. Process rules (carried from ART_BIBLE)
- Every prompt = §8 positive style block + the asset's §5 controlled variables + §7 negative exclusions folded into the prompt text (nano obeys them; soul_2 did not).
- Run the §9 checklist on every returned asset; squint/pareidolia test for any face/figure/throne in sky or light; reject-and-regenerate on any HARD fail.
- All AI art generated **text-free**; Yoruba diacritic names composited in-engine (TextMeshPro).
- The pre-launch **native-speaker / community review (§7.10) is still mandatory** — the pipeline never self-certifies cultural safety.

## 6. Generation order (when funded)
1. Cultivator Element — **DONE** (`ddcfd9ce…`)
2. 6 stage portraits via the Element (age + aura escalation; path accent at A4+) → `remove_background` → `upscale_image`
3. 6 backgrounds (one seed/palette family)
4. 3 path motifs (one set; distinct earth / storm / river)
5. Ancestor frames + UI skin
6. App icon + launch screen
7. Particle / FX textures
8. Audio (4 BGM, 5 SFX, 1 stinger) — validate the audio model first
Each step ends with the §9 checklist.

## 7. Credit budget (the reality)
- Observed cost ≈ **1 credit per 2K image**.
- 37 visual assets × ~2–3 re-rolls per keeper ≈ 75–110 credits; plus `remove_background` / `upscale_image` passes; plus 10 audio assets. **Realistic total ≈ 150–250+ credits.**
- Savings vs the original plan: no soul_2 Soul-training cost (Elements is free), and fewer wasted photoreal re-rolls.
- The free tier (10 credits) is a *probe* budget, not a *production* budget. A top-up or paid plan is required to execute.

## 8. Status
Anchor locked, pipeline corrected, plan written. **Ready to fire the full run on funding** — resume at §6 step 2.

## 9. Ascended-form style note (Baba, 2026-06-14)
Baba likes the dramatic, heightened-radiance look of the rejected `cultivator__soul2_ascended` image as a possible **ascended form** (renamed it `…__KEEP`). The heightened intensity is a legitimate transformation-beat choice — BUT that specific image carries HARD §7 violations and CANNOT ship as-is: the **beaded necklaces (ìlẹ̀kẹ̀) are a §7.2 red-line** (initiatory office insignia), and the bare-chested power-physique + chest power-blast drift into §7.5 (orisha-as-power-character). Path forward: keep the drama, regenerate it **clothed, bead-free, radiance-through-light-not-physique**, and reserve the louder style for the **Ascend-reveal beat (§5.5)** where a style shift reads as intentional — the calm painterly victor stays the Stage-5 portrait. Heightened-ascended Gemini prompt drafted 2026-06-14 (in chat; add to `GEMINI_ASSET_PROMPTS.md` on request). Original file kept as a mood reference only.

## 10. Scope change — appearance pool (3 looks) + Gemini finals (Baba, 2026-06-14)
The cultivator art expanded to an **appearance pool of three** looks (first three; expandable): Appearance 1 the existing lean humble man (Higgsfield refs exist), Appearance 2 a woman, Appearance 3 a large heavy-set but **gentle/humble** man (strength in build only — NOT a warrior; §7.5). These are **cosmetic Appearances**, not separate playable characters: the game rolls one per generation for the single active Cultivator (gen 1 fixed to Appearance 1), with no gameplay effect — see `CONTEXT.md` (Appearance) and `docs/adr/0001-per-generation-appearance.md`. Each needs the full 6-stage arc → **18 portraits** (was 6). **Higgsfield generations are now REFERENCES, not final assets — all finals are produced in Gemini / Nano Banana.** Total visual 37 → 49 (+12 portraits). Per-appearance workflow: Appearance 1 attach the existing Higgsfield keepers; Appearances 2 & 3 generate their Stage-3 young-adult anchor first, approve, then attach it across their other 5 stages. Full prompt structure in `GEMINI_ASSET_PROMPTS.md` §A (rev 2).

## 11. Ascension regalia — design rule (Baba, 2026-06-14)
**Lore rule:** the cultivator is HUMBLE through stages 0–4 and stays humble-dimmed if they FALL at the Crossing — but on **successful ascension they shed the humble look and take up regalia / a royal, exalted vibe** (the visual reward of victory). This refines §9: "bead-free / humble" applies to stages 0–4 and the fall; the **success/ascended state is deliberately royal.**
**Cultural steer (important):** aim the regalia at **secular ROYALTY**, not sacred priesthood — aṣọ-òkè gold-thread prestige cloth, a beaded royal crown (adé), royal coral collar/regalia, gold adornment (the vocabulary of an exalted Oba/queen, celebratory). **Avoid the items §7.2/§7.3 actually forbid:** orisha-devotee beaded necklaces (ìlẹ̀kẹ̀), divining chain/tools, priestly vestments, egungun. A generic AI "beaded necklace" renders ambiguously — prompt specifically for crown-and-coral royalty and explicitly away from priestly/divination items.
**Sign-off:** the royal-celebration-vs-sacred-office line is squarely the §7.10 native-speaker review's call; as a heritage homage, that reviewer green-lights the specific regalia.
**Resolved (Baba, 2026-06-14):** the crown goes on **after the Crossing succeeds** — stages 0–5 (incl. Aṣẹ́gun the Victor) stay humble; royal regalia appears only at the **post-Crossing ✦Ascended reveal**, a distinct portrait one per character (+3 → portraits 18→21, visual 49→52). The reveal shows the crowned cultivator dissolving into light; council-card ancestors stay abstract (§7.3). The fall stays humble-dimmed.
