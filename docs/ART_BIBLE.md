# ART_BIBLE.md — Ori Ascendant

**Art Direction:** "Painterly Cosmic Myth" (locked)
**Version:** 1.0 · **Status:** Canonical — governs every visual + audio asset
**Grounded in:** `/home/baba/ASE/docs/research/cosmology.md`, `/home/baba/ASE/docs/GAMEPLAY.md` (esp. §2.2, §2.3, §3, §5.4, §7)
**Companion:** `ASSET_MANIFEST.md` (the production table)

---

## 0. How to use this document (and the one rule above all)

This is the binding art direction for *Ori Ascendant* — a reverent idle-cultivation game built on **Igala, Yoruba, and Igbo** cosmology, a heritage homage by a descendant. Read §1–§3 once for the global system, then jump to the per-screen section (§5) for the asset you are making. §7 is the cultural-safety firewall — **it overrides aesthetics, schedule, and prompt convenience every single time.**

**The rule above all rules.** AI image and audio models, when fed "West African / orisha / ancestral / Yoruba ritual," reliably produce **exactly the imagery the red lines forbid** — egungun masks, a supreme-deity figure, skulls, divination boards, "voodoo" pastiche. Their training distribution is biased toward tourist-festival photography and occult stock art. **We therefore never feed those triggers.** Our prompts describe natural forces and light, never religion, never the proper nouns of the faith. Safety is engineered into every prompt (§7, §8), checked on every returned asset (§9), and signed off by a human before launch (§7.10).

**Heritage posture (informs mood, not just legal):** the feeling target is *warm, premium, mythic, dignified, reverent* — a contemplative art game (Gris, Sky: Children of the Light) crossed with the saturated, joyful authority of Yoruba textile (adire indigo, aṣọ-òkè gold). Joy and dignity, never grimdark; awe, never horror; intimacy, never spectacle.

**Source of truth for content:** stage names/meanings come from GAMEPLAY §2.2, path themes from §2.3, screen layouts from §3. This guide governs only *how it looks and sounds* — never the numbers, names, or layout.

---

## 1. Global palette

The look maps onto **two documented real-world color systems**, which is what keeps it from reading as generic fantasy: **adire indigo** (the night-sky/UI base; historically a marker of *wealth and prestige* — quietly premium) and **Àṣẹ gold** (the single sacred light, doubling as Osun's signature gold/amber).

**Base (every screen) — deep indigo night.** The constant ground of the game; the visible texture of ọ̀run-adjacent space and of adire cloth.

| Role | Hex | Use |
|---|---|---|
| Indigo Night | `#0E1330` | darkest; vignette edges; true-black substitute |
| Indigo Base | `#1B2150` | primary background field |
| Indigo Lift | `#2C3470` | mid sky; atmospheric distance |
| Dusk Violet | `#3E3A6E` | where indigo warms toward horizon |
| Warm Ecru | `#EDE3CE` | undyed-adire ground; cloth; text-on-dark surfaces |

**Àṣẹ Gold — the single sacred light of the whole game.** Àṣẹ is luminous power; it is *always* the brightest thing on screen and *always* warm. It is the only color allowed to glow at full intensity over indigo. Reserve it: counter, motes, CTA glow, ascended radiance, ancestor stars.

| Role | Hex | Use |
|---|---|---|
| Àṣẹ Core | `#FFE6A8` | hottest highlight; near-white gold |
| Àṣẹ Gold | `#F4C14E` | the signature — counters, primary glow |
| Àṣẹ Deep | `#C98A2B` | gold in shadow; brass edges |

**Per-path accents** — used ONLY once a path is chosen, and only on path-relevant UI; never bleed one path's color into another's screen. Three deliberately distinct hue families (earth vs fire vs brass/teal) so a glance separates the traditions — palette-level support for red line §7.6.

| Path (tradition) | Accents | Mood |
|---|---|---|
| **Ane** — Igala earth, endurance | Earth-Green `#5C7A4A` · Ochre `#C28A3A` · clay shadow `#6E4B2E` (+ black `#1A150F` & maize `#E8C24A` for Igala color symbolism) | grounded, mineral, patient; lowest saturation |
| **Sango** — Yoruba thunder, sudden force | Storm-Amber `#F2A33C` · Thunder-Red `#C8412B` · hot white `#FFF3D6` | high-contrast, electric, fast; highest saturation; reds appear ONLY for Sango |
| **Osun** — Yoruba river, lineage/flow | River-Teal `#2E8C8C` · Brass `#C9A24B` · pale river-light `#BFE6DD` | cool, flowing, generous, reflective |

**Neutrals & text:**
- Text primary `#F3EEDD` (warm bone-white — never pure `#FFFFFF`)
- Text secondary `#B9B7C8` (cool muted lilac-grey)
- **Ember (fallen ancestors)** `#D2742F` warming to `#7A2E1E` — a dignified dimmed glow, **NEVER grey, NEVER red-as-failure.**

**Palette discipline:** Indigo + Àṣẹ gold is the spine of EVERY screen. Path accents are seasoning, never the base. No screen may go cool-grey or desaturated to signal "bad" — dignity is rendered as *warm and dim*, not *cold and dead*.

---

## 2. Rendering technique

**Medium:** rich digital painting — visible, confident brushwork in skies and natural forces; smoother rendering on faces. Gouache-meets-oil with soft edges. **NOT** cel-shaded, NOT vector-flat, NOT photoreal, NOT 3D render. Touchstones: the atmospheric painterliness of Gris and Sky: Children of the Light, the layered light of Ghibli sky paintings, warmed toward Yoruba textile saturation.

**Edge language:** soft, lost-and-found edges; forms emerge from and dissolve into atmosphere. Hard edges are reserved and *meaningful* — a lightning bolt (Sango), the rim-light on the ascending cultivator. Everything else breathes.

**Light as the subject:** light is painted as a physical, volumetric thing — god-rays through dust, glow bleeding past its source (soft bloom baked in), motes catching light. Àṣẹ is warm light suspended in cool dark. This single contrast (warm light / cool dark) is the engine of the whole look.

**Atmospheric depth:** always 3+ planes — near (subject; warm, saturated, detailed), mid (transitional), far (cool indigo; desaturated, hazy, low-detail). Distance reads cooler and softer. This makes static art deep enough to layer engine particles over.

**Texture:** subtle grain/paper tooth over everything to unify generations of assets and kill banding in indigo gradients (critical on OLED iPhone). A faint **adire-cloth / woven texture** may ghost into UI panels and the title — never loud, a cultural fingerprint in the substrate.

**Resolution & export:** author at 3× for iPhone 12 baseline (390×844 pt). Bake soft bloom INTO the art (the engine adds motion/particles, not the core glow). Keep a 60px safe margin from screen edges for key focal content (notch/home-indicator). Exact per-asset dims/format live in `ASSET_MANIFEST.md`.

---

## 3. Lighting, composition & mood rules

**Lighting:**
1. **One dominant warm source per image** — almost always Àṣẹ gold, motivated (rising from the cultivator, pooling on the far bank, flowing from a path motif). Everything else is reflected cool light.
2. **Warm light, cool shadow** — never the reverse. Shadows tend indigo/violet, never neutral grey or black holes.
3. **Rim-light the hero** — the cultivator and ascended ancestors always carry a thin gold rim separating them from the indigo field.
4. **Glow has falloff** — light blooms and fades into atmosphere; no flat halos, no hard-edged auras.

**Composition:**
1. **Reverent verticality** — a portrait-orientation game about *ascent*. Figures rise, motes flow up, the road/river leads the eye up toward a far light. Horizon kept low (sky dominant), except the Crossing (river dominant).
2. **Generous negative space** — let the indigo breathe; premium-contemplative feeling comes from restraint. Negative space also leaves room for live UI and particles.
3. **Centered, iconic subjects** — idle game = long looking; centered subjects rest the eye and survive UI overlap.
4. **Thumb-zone awareness** — keep critical focal art in the upper 2/3; the bottom third hosts interactive UI (GAMEPLAY §3.2). Backgrounds must read with a panel/strip over the lower third.
5. **Lead the eye upward to light** — every composition resolves toward the brightest point, always Àṣẹ gold, usually near the top.

**Mood, one line:** *reverent, warm, mythic, patient, premium.* The player should feel they are tending something sacred and alive, not grinding a meter.

---

## 4. The 6-portrait ascent arc — character consistency is the hardest job

**The brief:** ONE cultivator, ONE lineage member, ascending across six stages. **Character consistency is mandatory** — it must read as the same person aging and accruing àṣẹ, not six different characters. Names/meanings locked (GAMEPLAY §2.2). Attire is **ordinary, regalia-FREE — no sacred regalia, no egungun, no priestly office costume** (§7.2, §7.3).

**Two axes escalate; identity stays fixed.**
- **AGE** climbs steadily: child → youth → young adult → adult → elder → radiant elder at the river.
- **AURA (Àṣẹ glow)** climbs from a faint inner spark to a body wreathed in gold light + abstract constellation hints.
- **FIXED across all six:** facial structure, skin tone, identity, painterly technique, indigo ground, framing (centered, chest-up to 3/4, slow camera).

| Idx | Name (meaning) | Age | Attire (ordinary) | Àṣẹ aura | Setting hint | What changes from prior |
|---|---|---|---|---|---|---|
| 0 | **Ọmọ Ayé** — Child of the World | child (~7) | simple everyday tunic, undyed/earth-toned, barefoot feel | single faint gold spark at the chest, almost shy | warm marketplace dusk, soft bustle far behind | (baseline) wide curious eyes, first stir of light |
| 1 | **Akẹ́kọ̀ọ́** — The Learner | older child/early teen | same palette, slightly fuller everyday dress, a learner's wrap | spark grown to a small steady chest-glow, a few motes orbiting | quiet study corner, lamplight | older face, posture of attention, light now *moves* |
| 2 | **Awo** — The Initiate (path unlocks) | youth (~16–18) | plain initiate's wrap, still NO regalia, path-neutral | glow spreads to the hands; motes form a loose halo | threshold/doorway into deeper indigo | first sense of *power held in the hands*; on the cusp of choosing |
| 3 | **Aláàṣẹ** — Wielder of Àṣẹ | young adult | confident everyday dress; **subtle path accent enters here** (Ane ochre / Sango amber / Osun teal) in cloth trim only | àṣẹ answers the gesture — light gathers where they point; strong chest-glow + rim-light | Ọ̀run-facing: indigo deepens, first stars | speaks and light obeys; the path's color quietly enters |
| 4 | **Àgbà** — The Elder | elder, grey at temples, lined dignified face | dignified elder's everyday wrap; path accent stronger but still cloth, never regalia | body softly wreathed in gold; motes constant; faint constellation behind (abstract) | high indigo, the road home far below | gravitas; the lineage "steadies around them"; light now ambient |
| 5 | **Aṣẹ́gun** — The Victor | venerable radiant elder | same humble wrap, now luminous; still NO regalia | fully wreathed in Àṣẹ gold, near-translucent at the edges, constellations bright behind | the river's edge — night river + far-bank lights hinted | peak radiance; poised at the Crossing; the figure half-made-of-light |

**Red-line checks for portraits:** ordinary clothing only; aura is *light*, not masks/spirits; constellations behind are abstract points of light (never figures/faces); the figure is a human cultivator, never a deity. An ordinary plain support staff is acceptable on the Elder ONLY — never a carved/beaded ritual staff. The escalation is **age + light**, full stop.

---

## 5. Per-screen art direction

### 5.1 Title / launch — "Ori Ascendant"
Black-to-indigo wash; a low marketplace horizon dissolving into a star-rich ọ̀run sky; a single thread of Àṣẹ gold rising from the bottom (the lineage's first spark) up toward a faint constellation. Title in warm bone-white with a gold under-glow; proverb **"Ayé l'ọjà, ọ̀run nilé"** + translation beneath in secondary text — **full Yoruba diacritics, no exceptions (§7.9).** Faint adire texture ghosted in the substrate. "Touch to begin" low, quiet. Mood: a held breath before a long, warm journey. Launch-screen art = the same composition, static, instant-loading. **All rendered text is composited in-engine (TMP), never baked into AI art.**

### 5.2 Main idle background
The constant home. A vast indigo ọ̀run sky with slow painterly cloud-banding and a scatter of stars (the watching ancestors, abstract). A warm gold horizon-glow low and centered, from which àṣẹ motes drift upward — where engine particles continue the painted motes. Leave the centered portrait zone (24–58% height) clean and the lower third calm enough for the counter/CTA/council strip. 3 planes; quiet enough to look at for hours. Author neutral-warm so a storm vignette can layer over it during tribulation buildup (§5.5).

### 5.3 Path screen + 3 path motifs/badges
Three cards side by side over the indigo idle field, each owning its accent palette. The motifs are **natural-force emblems, abstract, never deities, never figures** (§7.4–§7.5). Keep the three visually distinct enough that color alone identifies the path (they are reused on MainScreen, ancestor cards, badges). Identity line + the one stat line are computed copy, never baked into art.
- **Ane (Igala earth, endurance):** layered earth/stone strata and a steadfast mountain silhouette; black + maize-gold over ochre + earth-green, with gold seams of àṣẹ running through rock like ore veins. Woven-band / concentric earth-ring / granary-lattice geometry from Igala basketry & Igala-Achi cloth. Mineral, patient, weighty. *One-line brief: "A black-and-maize woven earth-disc — concentric strata and a single gold seed of àṣẹ rooting downward; quiet, heavy, enduring."*
- **Sango (Yoruba thunder, sudden force):** a forked lightning bolt over a storm-amber sky; the only hard-edged, high-contrast motif; thunder-red + hot-white strike on dark cloud. **NO axe.** Where you want "two-directional swift justice," render a forked bolt splitting two ways — never the double-axe (oṣẹ́ Ṣàngó), never an axe-head, never a wielded weapon, never a figure. *One-line brief: "A vermilion-and-white storm-burst — a forked bolt of gold àṣẹ splitting the indigo, no staff, no axe, only the strike."*
- **Osun (Yoruba river, lineage/flow):** a curving river of teal and brass light, flowing then pooling, with gold reflections; concentric ripple-rings; a single abstract peacock-eye glint as iridescent motif (never a bird). Adire "Olokun" waves-and-fish water-geometry as border. Soft, generous, cool. *One-line brief: "A brass-gold river-disc — concentric ripples and a single peacock-eye glint of àṣẹ flowing; sweet, luminous, unbroken."*

Selected card brightens and saturates; unselected dim toward indigo.

### 5.4 The 4 backgrounds
1. **Main idle** — see §5.2.
2. **Tier 0 "Ayé / the Marketplace":** a warm West African dusk marketplace dissolving into painterly haze — stalls, cloth, lamplight, the *bustle of life* as warm glints, NOT detailed crowds, no modern signage. **This is the one populated scene — the prompt must explicitly steer: no discernible human faces or figures; the crowd is implied as warm light/glints only** (so stray rendered figures are prevented at the prompt, not just caught at review). Gold-warm, lowest indigo of the four (Ayé is the lit world). Adire **Ibadandun** (praise-of-place) geometry as a subtle ground texture. Lower third calm for UI.
3. **Tier 1 "Ọ̀run / the Road Home":** the palette cools and rises — a road winding upward through deepening indigo toward a distant warm light and a field of stars. Quieter, more reverent, more vertical than Ayé. The cultivator is "ọ̀run-facing." Stars (ancestors, abstract) begin to populate.
4. **The Crossing / Ìrékọjá** — its own frame, see §5.5.

### 5.5 The Crossing — Ìrékọjá (tribulation)
The emotional summit. **A night river** under a storm-charged indigo sky; on the **far bank, the lights of the lineage** — warm gold points and soft drum-glows, a gathered welcome (death-as-homecoming, NOT judgment). Near bank dark and intimate; river wide, reflective, cool teal-indigo with broken gold reflections. **Absolutely NO deity, NO judge, NO gatekeeper figure anywhere (§7.1).** The far bank is light and implied welcome only — a scatter of *roughly-equal* lights, never one dominant figure or central brightest light that reads as "the one in charge." The "judge" is the river itself / the cultivator's own orí holding true.
- **Buildup states** (ambientFractions 50/80/100): 50% — sky tint warms/darkens, far-bank lights faintly appear; 80% — storm vignette closes in, distant lightning over the river; 100% — full storm, lightning particles, river charged with reflected light.
- **Ceremony art needs:** a silhouette transition frame; storm/lightning strike frames; a **whiteout** frame (a near-total warm-white wash, no UI) for the held-breath beat; then the branch.
- **Ascend reveal:** gold palette floods in; the figure rises and dissolves into rising light toward the far bank; constellations brighten. Radiant, major, warm.
- **Fall reveal:** **never grey, never red, never a buzzer.** Warm embers; the figure kneels and dissolves into rising sparks that still drift toward the far bank — *received, just dimmer*. Ember-orange warming to deep clay, gold lowered not absent. "THE LINE ENDURES." Dignity = warmth-at-lower-intensity (§1 ember palette).

### 5.6 Council shrine (CouncilScreen)
A **lineage shrine, explicitly NOT egungun (§7.3).** Translate "shrine" into safe secular object-vocabulary: an unlit row of **calabash/gourd vessels** (household objects), **plain carved wooden posts** (undecorated/architectural, NOT ritual staffs of office), and a **central flame** of àṣẹ. Intimate indigo interior lit by a single warm flame. Five **council slot frames** (use **Eléko** adire geometry — royalty/power — to feel reverent and premium) arranged for the ancestor cards; a "foundation" band beneath (retired ancestors baked into the lineage — "settled into the foundation of the house," rendered as a flowing river of light, distinct from the discrete star-cards above). Empty slots = quiet outlined frames awaiting an ancestor. Àtúnwá framing: the lineage returning stronger each generation. The feeling is kneeling in a warm room of memory, not a trophy case.

### 5.7 Ancestor card art
Abstract only — **light / river / flame / stars, NEVER a figure, NEVER a mask, NEVER a face** (§7.3, §5.4). Card = a frame + a composited path motif (B1–B3) at runtime, so frames are path-agnostic. Four abstraction registers map to affordances: **stars/constellations** = a named individual ancestor (the 5 council cards); **light (gold bloom)** = ascended radiance; **flame/ember** = fallen; **river of light** = the merged/collective lineage on the foundation line.
- **Radiance frame (ascended):** bright gold luminous border, constellation/star sparkle, warm halo at full warmth — a star coming into being. A good crossing.
- **Ember frame (fallen):** the SAME composition with a dimmed ember-glow border (orange→clay), motif lowered but warm — never grey, never broken, gently rising sparks. Dignity = lower intensity, not different temperature.
- **Per-path motif inside the frame (combine register + element, §5.4):** Ane = constellation rising from / embedded in earth & strata (earth + stars); Sango = constellation struck through by a lightning/flame bolt of gold; Osun = constellation carried on a river of light (flow/continuity). Reuse §5.3 motif language at card scale.
Card carries computed title ("Gen N — Aṣẹ́gun of Thunder") + contribution line — text computed, not painted. Frames and motif fields are the art deliverable.

### 5.8 UI skin
Warm, premium, restrained. **Buttons:** softly rounded, indigo-deep fill with a thin Àṣẹ-gold rim-light; primary CTA carries a gentle baked gold inner-glow + an engine pulse when active. **Panels/modals:** dark indigo glass with subtle adire-ghost texture and a 1px gold edge; soft drop into the scene, never opaque-flat. **Progress-bar fill:** track is indigo-deep; the **fill is liquid Àṣẹ gold** with a brighter leading edge (the bar literally fills with light) — at stage 6 it becomes the storm-charged Tribulation bar (one-decimal percent, GAMEPLAY §2.4). **Council slot frames (5):** one reused carved/woven gold-edged frame (Eléko adire geometry) echoing the shrine (§5.6), with an empty/outlined state. Path badges on MainScreen carry the path accent + the §5.3 glyph. Typography: a warm humanist face with **full Yoruba diacritic support (Noto Sans fallback for subdot+tone stacking, §7.9)** — bone-white text, gold for the sacred number. No baked-in lettering in any AI-generated UI art; text composited in-engine.

### 5.9 App icon (1024) + launch screen
**Icon:** maximal legibility at small size + brand soul — a single thread/spark of Àṣẹ gold rising into a stylized constellation against deep indigo (the lineage's spark ascending). High-contrast, warm-light-on-cool-dark, painterly but bold-shaped, instantly readable at 60px. **No text, no figure, no forbidden imagery; opaque, no alpha, no pre-rounded corners** (Apple applies the mask and rejects transparency). It should feel like a contemplative premium game. **Launch screen:** the §5.1 title composition, static, instant-loading.

---

## 6. Audio direction (4 BGM, 5 SFX, 1 stinger)

Warm, organic, reverent; live/acoustic timbres over synthetic. **Format:** 48 kHz WAV masters; BGM exported OGG Streaming, SFX OGG Mono; all loops seamless; 10 null-safe assets. Keep traditions' timbres distinct (Igala vs Yoruba) — never homogenize into generic "African drums" (§7.6/§7.7).

**BGM** — instrumental, seamless loop, no bàtá pattern, wordless vowels only:
- **Menu (Ayé):** ~48s, 76 BPM — kora-harp, balafon, shekere, frame drum, gold pad. The "held breath" of the title; spacious, contemplative; distinct from path themes.
- **Ane (Igala earth, endurance — ×1.5 offline):** ~56s, 66 BPM — frame drum + udu ostinato, slow balafon, low drone, kalimba. Grounded, patient, mineral weight; Igala-rooted.
- **Sango (Yoruba thunder — ×2 active):** ~40s, 112 BPM — dùndún talking-drum *melodic* (explicitly NOT a ritual/liturgical cadence), frame drums + shekere, balafon, storm swell. Driving, energetic, the storm that strikes now.
- **Osun (Yoruba river — council ×2):** ~52s, 88 BPM, 6/8 — kora-harp arpeggio, balafon, shekere, vowel humming, river ambience. Flowing, sweet, the most lyrical theme; lineage-and-river feeling.

**SFX:**
- **Advance:** ~0.8s — balafon/kalimba + a gold bell; a warm rising bloom.
- **Channel-tap:** ~0.25s — muffled "tup," repeatable ~2×/sec, no reverb, no fatigue.
- **Collect:** ~1.3s — gold cascade resolving to a bell (motes gathering); NOT slot-machine.
- **Ascend:** ~2.5s — harp swell to a major chord + vowel shimmer; the good crossing.
- **Fall:** ~2.5s — soft kora, low down-then-up + embers; warm, dignified, **NEVER a buzzer/error tone, NEVER grim.** "The line endures."
- **Tribulation stinger (Ìrékọjá):** ~4.0s — heartbeat + riser + THREE thunder cracks, then a held note cut to total silence; no tell, no "judge," NOT bàtá. Tension as awe, not dread.

**Audio red lines (mirror §7):** NO sampled real liturgy; NO bàtá rhythms presented as "spells" or gameplay cues (§7.4); NO chant presented as real invocation. Drums evoke celebration/homecoming, never ritual reproduction. **When prompting an audio generator, avoid the words "voodoo / ritual / sacrifice / chant / spell" entirely**; prompt for "warm West African kora / talking drum / ngoni / balafon, contemplative cinematic reverent, organic acoustic, [Igala-grounded earthy | Yoruba bright driving | flowing watery] per theme." Reviewer line (**HARD**): "Is any cue a recognizable sacred liturgical rhythm or a real invocation chant? If yes, reject."

---

## 7. The §7 Red-Line Visual Catalog (the cultural-safety firewall)

This is the guardrail for the entire image/audio pipeline. AI models, fed the literal subject matter, produce *exactly* the forbidden imagery. The defense is **prompt hygiene** + **adversarial review**. Every asset passes three gates:
1. **Prompt gate** — every prompt appends the relevant POSITIVE fragment AND the **Universal Negative Block** (below) + any per-red-line negatives that apply.
2. **Review gate** — every returned image is checked against the per-asset checklist (§9). **A single HARD fail = reject + regenerate. No "close enough."**
3. **Sign-off gate** — before launch, a native-speaker / community reviewer (§7.10) does one pass over the final approved set. The pipeline cannot self-certify cultural safety.

**Reviewer default posture is adversarial and conservative: when in doubt, FAIL.** A false-reject costs one regeneration; a false-accept ships disrespect in a heritage homage. The cost is asymmetric — bias hard toward rejection.

**Severity tiers:** **HARD (red-line)** — violates §7; any single occurrence = automatic reject, no discretion, logged. **SOFT (quality/consistency)** — off-brief but not a cultural violation (wrong palette, broken consistency, wrong age); reject if it harms the asset.

### 7.0 The Universal Negative Block (paste into EVERY image prompt)
```
NEGATIVE (universal — all assets):
egungun mask, tribal mask, ceremonial mask, masquerade costume, face mask,
wooden mask, deity figure, god figure, divine being, enthroned god, glowing
celestial judge, anthropomorphic deity, idol, religious idol, carved idol,
statue of a god, orisha as a character, personified river goddess,
personified thunder god, robed priest, witch doctor, shaman, tribal chief,
feather headdress, raffia costume, beaded ceremonial regalia, cowrie-shell
divination, divining chain, palm nuts, sacred altar with offerings,
blood sacrifice, animal sacrifice, skull, skulls, human skull, animal skull,
bones, bone pile, voodoo, voodoo doll, hoodoo, witchcraft, occult ritual,
dark ritual, demonic, sinister, eerie, pentagram, candles-and-skulls altar,
spooky, horror, grotesque, scarification ritual, "tribal" generic pan-African
pastiche, kente-everything stereotype, savanna-and-acacia tourist cliche,
text, watermark, signature, gibberish lettering, corrupted glyphs, captions
```

### 7.1 Never depict the supreme deity (Olódùmarè / Ọlọ́run / Igala Ọjọ́)
**AI failure modes:** the Crossing/tribulation → a glowing enthroned figure, a giant face in the clouds, a radiant humanoid judging; "ancestors on the far bank" → one dominant luminous figure presiding; "cosmic/divine light" → a god-shaped silhouette inside the light; app icon → a deity hand reaching down / eye-of-providence.
**Steer toward:** the Crossing = an impersonal night river with drums and the lights of the lineage on the far bank; no presiding entity; far bank = a scatter of *equal* warm lights. Divine power = abstract gold light + àṣẹ motes, never a source-figure. Light has no owner on screen.
```
POSITIVE: impersonal natural forces only; gold àṣẹ light as ambient glow with no
source figure; the Crossing is a calm night river under indigo sky, scattered equal
warm lanterns/star-lights on the far bank representing the lineage, no presiding
being, no enthroned figure, no face in the sky; reverent, empty, awe from landscape
not from any character.
NEGATIVE (stack on universal): enthroned figure, god in the sky, giant glowing face,
face in the clouds, presiding deity, judge figure, radiant humanoid overseer, hand
of god, eye of providence, single dominant luminous being, halo'd sovereign,
celestial throne, godrays forming a silhouette.
```
**Checklist (Crossing bg, Tribulation FX, app icon, main idle bg):**
- [ ] **HARD:** No humanoid/figure in the sky, clouds, or light source. (Cultivator on the ground/near bank is fine; nothing presiding above or across.)
- [ ] **HARD:** Far bank is a scatter of roughly-equal lights — NOT one larger/central/brighter figure or light that reads as "the one in charge."
- [ ] **HARD:** No face, eye, hand, or throne formed by clouds, light, godrays, or negative space. (Squint-test for pareidolia.)
- [ ] **HARD:** Gold light has no rendered owner/emitter shaped like a being.
- [ ] **SOFT:** Mood reads as homecoming/awe, not trial-before-a-judge.

### 7.2 No initiatory offices as ranks/insignia
**AI failure modes:** elder-stage portraits (Aláàṣẹ/Àgbà/Aṣẹ́gun) → the cultivator dressed as a babaláwo (white robe + beaded ìlẹ̀kẹ̀ + ìrùkẹ̀rẹ̀ whisk + ọpẹlẹ chain); "elder/wielder of power" → priestly staff (ọ̀pá òṣun), idè bracelets; path badges → divining-board (opón Ifá) iconography.
**Steer toward:** ordinary, dignified, age-appropriate clothing in the Yoruba textile palette; NO sacred regalia, NO office insignia. Authority shown by **age + àṣẹ aura intensity**, never costume rank-markers. An ordinary plain elder's support staff is acceptable; a beaded/carved ritual staff is not.
```
POSITIVE: ordinary dignified everyday clothing for the period, simple woven tunic or
wrapper, warm earthy Yoruba textile colors (indigo, ochre, deep red, gold thread
accent); authority conveyed only by age and by intensity of the surrounding gold
àṣẹ aura; plain, humble, lived-in attire.
NEGATIVE (stack on universal): white priest robe, beaded necklaces of office, ilekes,
divining chain, opele, horsetail whisk, fly-whisk, irukere, beaded ritual staff,
opa osun, divination board, opon Ifa, priestly insignia, rank regalia, ceremonial
beadwork, ide bracelets, conferred-office costume.
```
**Checklist (6 stage portraits, path badges):**
- [ ] **HARD:** No beaded religious necklaces (ìlẹ̀kẹ̀), divining chain (ọpẹlẹ), horsetail/fly-whisk (ìrùkẹ̀rẹ̀), or divining board.
- [ ] **HARD:** No all-white "priest" robe-of-office; attire reads as ordinary clothing, not vestments.
- [ ] **SOFT:** Any staff on the Elder is a plain support staff, not carved/beaded ritual regalia.
- [ ] **SOFT:** Rising authority across stages comes from age + aura, not added ceremonial accessories.

### 7.3 No egungun masks/regalia; ancestors are ABSTRACT only — the single highest-risk red line
**AI failure modes:** ancestor card → a figure in layered egungun cloth + mask (catastrophic — egungun cloth rights are hereditary and sacred); "council of ancestors" / shrine → a row of masked masquerade dancers or a wall of carved masks; "spirit returning" → a ghostly masked figure; Crossing far bank → masked figures waiting. Igala-specific trap: the **Oloja masquerade** (crocodile headdress) is exactly what a model produces for "Igala ancestor."
**Steer toward:** pure abstraction — a constellation of stars, a ribbon of river-light, a coil of warm flame, a column of gold radiance — in a radiance (ascended) or ember (fallen) frame with an abstract path motif. NEVER a figure, face, or mask. Shrine = calabash, plain carved support posts, a central flame, scattered lights — NOT a mask wall.
```
POSITIVE (ancestor card): fully abstract representation of an honored ancestor as
light only — a constellation of warm stars / a ribbon of luminous river water /
a coil of gentle gold flame / a vertical column of radiance; no figure, no face,
no body, no mask; ornate [radiance | ember] border frame; small abstract path
motif (stylized earth-and-stars | stylized lightning | stylized river current).
POSITIVE (shrine bg): warm reverent shrine interior, calabash gourds, plain unadorned
wooden support posts, small flames, scattered gold lights, no masks.
NEGATIVE (stack on universal): egungun, masquerade, masked dancer, masked figure,
masked spirit, ghost with a mask, person in layered cloth costume, raffia, carved
wooden ancestor mask, mask on a wall, mask shelf, face on the card, humanoid
ancestor, spirit person, robed ghost, crocodile headdress, Oloja.
```
**Checklist (ancestor cards [both frames], CouncilScreen shrine bg, Crossing far bank, council slot frames):**
- [ ] **HARD:** Ancestor card contains NO figure, NO face, NO mask — only light/river/flame/stars. (Check both radiance and ember variants.)
- [ ] **HARD:** No egungun/masquerade costume, no raffia, no carved mask anywhere in shrine or card art.
- [ ] **HARD:** Path motif on the card is abstract (no figure, no orisha, no mask).
- [ ] **HARD:** Ember (fallen) frame reads as *dimmer warm light*, NOT death/skull/grey/horror (cross-check §7.6).
- [ ] **SOFT:** Radiance vs ember frames clearly distinguishable but both *honored/warm* — fallen is never punitive.

### 7.4 No reproduced liturgy or divination imagery
**AI failure modes:** Awo ("the Initiate") portrait / path-choice screen → opón Ifá board with iyẹ̀rọsùn powder, ọpẹlẹ chain, ikin palm nuts; path badges → cowrie patterns, Odù binary marks; "sacred/mystical" bg → an altar with divination tools.
**Steer toward:** the "mysteries" the Awo enters = abstract àṣẹ light intensifying / a threshold of glow, the moment a path *opens* — not a divination scene. Path choice = three elemental motifs (earth/lightning/river), never tools of divination.
```
POSITIVE: the 'mysteries' shown only as a threshold of intensifying gold light and
àṣẹ motes; abstraction, not ritual objects; path choice rendered as three natural
elemental forces (earth/mountain, lightning/storm, river/water) in painterly light.
NEGATIVE (stack on universal): divining board, opon Ifa, Ifa tray, iyerosun powder,
opele divining chain, ikin palm nuts, 16 cowrie shells, cowrie divination, Odu
marks, binary Ifa figures, sacred altar, offering bowl, ritual paraphernalia,
divination tools, closed-grove interior, igbodu.
```
**Checklist (Awo portrait, PathScreen bg, the 3 path badges, any "mystical" bg):**
- [ ] **HARD:** No divining board, cowries, palm nuts, divining chain, or powder tray.
- [ ] **HARD:** No Odù Ifá binary marks used as decoration/pattern.
- [ ] **HARD:** No altar laid with offerings / ritual objects.
- [ ] **SOFT:** "Initiation/mysteries" reads as light & threshold, not a depicted ceremony.

### 7.5 Orisha are patrons who bless — never collectibles/monsters/personified beings
**AI failure modes:** "Sango thunder path" → a muscular warrior-king wielding the double-axe; "Osun river path" → a beautiful woman/goddess rising from the river; "Ane Igala earth" → an earth-mother figure; any of these rendered card-game-collectible style.
**Steer toward — documented attributes as ABSTRACT motifs only:** Sango = lightning/storm/sudden fire, red + white, **the double-axe REPLACED entirely with raw forked lightning** (no axe, even as a held object); Osun = flowing fresh water, gold/brass/honey sheen, ripple-current, abstract peacock-eye glint; Ane = layered earth/stone, mountain, deep roots, steady stars over land, black + maize-gold (Igala, distinct from Igbo Ala / Yoruba Ilẹ̀). No king, no woman, no earth-mother, no boss, no pet.
```
POSITIVE (Sango badge): abstract emblem of thunder and sudden force only — a raw
forked lightning bolt, storm cloud, spark, vermilion red and bright white over
indigo; NO axe, NO person, NO king, NO warrior.
POSITIVE (Osun badge): abstract emblem of a flowing river — golden rippling water,
brass sheen, warm honey light, concentric ripples, a single abstract peacock-eye
glint; NO woman, NO goddess, NO bird, NO figure.
POSITIVE (Ane badge): abstract emblem of the Igala earth — layered strata, steadfast
mountain, deep roots, calm stars over land, black and maize-gold over ochre, woven
earth-ring geometry; grounded, enduring; NO figure, NO earth-mother.
NEGATIVE (stack on universal): orisha character, personified deity, warrior king,
thunder god figure, man with double axe, double axe, oshe Sango, river goddess,
woman in the river, mermaid, brass-fan goddess, peacock bird, earth mother figure,
collectible character card, monster, boss, creature, mascot, anthropomorphic god.
```
**Checklist (3 path motifs/badges, PathScreen cards, ancestor card motifs):**
- [ ] **HARD:** No human/figure/creature representing Sango, Osun, or Ane — abstract motif only.
- [ ] **HARD:** No double-axe anywhere for Sango — only raw lightning. (Replace the axe, do not stylize it.)
- [ ] **HARD:** Osun motif is water/light, NOT a woman/mermaid/goddess/bird.
- [ ] **HARD:** Nothing reads as a "collectible character," pet, or enemy.
- [ ] **SOFT:** The three motifs are visually distinct (earth vs storm vs river) — no generic-blur homogenization (cross-check §7.6/§7.7).
- [ ] **SOFT:** Each uses only its documented attributes (no marriage-drama / unrelated tropes).

### 7.6 Zero "voodoo"/witchcraft framing
**AI failure modes:** "spiritual power / àṣẹ / ritual" → candles + skulls + bones altar, voodoo dolls, sickly-green occult palette; "fallen cultivator" → death imagery (skull, grave, grim reaper, grey corpse) — **the most dangerous failure**, because the design explicitly forbids framing the fall as punishment; "tribulation storm" → demonic/sinister tone; night scenes → spooky-horror grading.
**Steer toward:** the fall = warm dimmed embers, soft rising sparks, dignity ("THE LINE ENDURES") — never grey, never red-buzzer, never skulls; honor at a lower intensity. Power/ritual mood = warm gold + reverent indigo, awe and beauty (Gris/Sky sensibility), never occult-green or horror. Storm = majestic natural awe, not menace.
```
POSITIVE: warm, reverent, premium, beautiful; deep indigo sacred night and gold light;
awe and serenity; the 'fall' outcome shown as dignified dimmed embers and soft
rising sparks, honored and gentle, never grim.
NEGATIVE (stack on universal): voodoo, voodoo doll, hoodoo, witchcraft, witch, Iyami,
aje, dark magic, occult, demonic, sinister, evil, eerie, haunted, horror, creepy,
spooky, skull, skeleton, bones, grave, tombstone, grim reaper, death imagery,
dripping candles and skulls, blood, sacrifice altar, sickly green glow, ominous
red, cursed.
```
**Checklist (ALL assets; extra scrutiny on the Fall reveal, ember frame, Tribulation FX, all night/dark backgrounds):**
- [ ] **HARD:** No skulls, bones, graves, voodoo dolls, or sacrifice/altar imagery anywhere.
- [ ] **HARD:** Fall/ember art is WARM and dignified — no grey, no red-buzzer, no death/horror cues. ("Would a grieving family find this honoring?" — if not, FAIL.)
- [ ] **HARD:** No occult/demonic/horror grading; no sickly-green or ominous-red occult palette.
- [ ] **SOFT:** Night scenes read as sacred-calm indigo, not haunted/spooky.
- [ ] **SOFT:** Overall mood is reverent & premium, not "dark mystical."

### 7.7 Don't homogenize traditions; orthography is respect
**AI failure modes:** any "African" prompt → generic pan-African pastiche (kente-on-everything, savanna+acacia, "tribal" mud-cloth) collapsing three distinct traditions into one stereotype; the three path motifs come back visually interchangeable; baked text → fake "African-looking" glyphs, or Yoruba names with wrong/missing diacritics (tone marks change meaning: àṣẹ "power" vs aṣọ "cloth").
**Steer toward:** each path its own visual language — Ane = Igala earth (umber/ochre/black/maize, stone, mountain, grounded); Sango = Yoruba storm (vermilion red, white, lightning over indigo); Osun = Yoruba river (gold/brass/teal, flowing water). Distinct silhouettes, distinct color families. **All rendered text composited in-engine** via TextMeshPro with the Noto Sans fallback (§7.9); AI art generated **text-free**.
```
POSITIVE: each tradition visually distinct, not generic pan-African; Ane = Igala earth
(umber, ochre, black, maize-gold, stone, mountain, grounded); Sango = Yoruba storm
(vermilion red, white, lightning over indigo); Osun = Yoruba river (gold, brass,
teal, flowing water); specific and rooted, not 'tribal'. Generate completely WITHOUT
any text or lettering.
NEGATIVE (stack on universal): generic tribal pattern, pan-African pastiche, kente-on-
everything, mud cloth texture, savanna acacia tourist backdrop, mixed-up culture
soup, fake african glyphs, invented script, any text, any lettering, words,
mislabeled tradition.
```
**Checklist (3 path badges as a SET, all assets bearing names, anything with cultural pattern work):**
- [ ] **HARD:** No baked-in text/lettering in AI art at all. If text appears, FAIL — regenerate text-free.
- [ ] **HARD:** The three path motifs are clearly distinguishable by palette + element (earth / storm / river) — not interchangeable.
- [ ] **SOFT:** No generic "tribal"/kente-everything/savanna-cliche pastiche.
- [ ] **SOFT (in-engine text layer):** Any displayed Yoruba name carries full diacritics (Àṣẹ, Aláàṣẹ, Akẹ́kọ̀ọ́) — verified against GAMEPLAY §2.2, never bare ASCII.
- [ ] **SOFT:** Ane is presented/labeled as Igala, distinct from the Yoruba pair (check About/glossary + any motif captions in-engine).

### 7.9 Orthography
Any rendered text uses **full Yoruba diacritics** (Àṣẹ, Aláàṣẹ, Akẹ́kọ̀ọ́, Aṣẹ́gun, Ọ̀run, Ìrékọjá, "Ayé l'ọjà, ọ̀run nilé"). Font must handle subdot + tone stacking (Noto Sans fallback); degrade to dotted-only (Akẹkọọ) before ever stripping to ASCII. Names come from GAMEPLAY §2.2. Per the asset pipeline, **never trust an image model to spell Yoruba** — generate art text-free, composite real diacritic names in-engine via TMP.

### 7.10 Sign-off gate (non-negotiable)
None of the above substitutes for the scheduled **native-speaker / community review pass before launch** (GAMEPLAY §7.7). Treat this catalog as the thing that makes that review *short*, not the thing that replaces it. Anything bearing names or culturally specific form is additionally flagged for that human pass. The pipeline never auto-approves a cultural-safety judgment call; ambiguous cases are held, not shipped.

---

## 8. Reusable prompt scaffolding (apply to every image generation)

**Positive style block (prepend to every image prompt):**
> "Rich digital painting, painterly cosmic myth style, soft volumetric warm light over deep indigo night, atmospheric depth with cool hazy distance, visible confident brushwork, baked soft bloom, subtle paper-grain texture, contemplative premium mobile game art (sensibility of Gris and Sky: Children of the Light crossed with warm Yoruba textile color), reverent and warm mood, gold Àṣẹ light as the only bright sacred glow. Palette: indigo #1B2150 base, Àṣẹ gold #F4C14E light, [path accent if applicable]."

**Universal negative block** — see §7.0 (always appended; per-red-line negatives stack on top).

**Per asset:** add ONLY the controlled variables from the relevant §5 entry (subject, age/aura band for portraits, path accent, setting hint). Lock seed + Soul per §10.

**Audio prompts:** positive = "warm West African kora / talking drum / ngoni / balafon, contemplative cinematic reverent, organic acoustic, [Igala-grounded earthy | Yoruba bright driving | flowing watery] per theme"; negative-by-omission = never use "voodoo, ritual, chant, spell, sacrifice, sampled liturgy, bàtá."

**Generation order (dependency-correct):** (1) Stage-1 portrait anchor → approve → train the Soul → (2) Stages 2–6 batch through the Soul → (3) the 6 backgrounds as one seed family → (4) 3 path motifs as one set → (5) ancestor frames + UI skin → (6) icon + launch → (7) particle textures → (8) audio. Every step ends with the §9 checklist.

---

## 9. The pre-delivery red-line checklist (run on every asset)

Reject and regenerate if ANY box fails:
- [ ] No supreme-deity figure or implied judge/being anywhere (incl. the Crossing far bank).
- [ ] No mask, egungun, or sacred regalia; cultivator attire is ordinary.
- [ ] Any ancestor representation is ABSTRACT (light/river/flame/stars) — no figures, no faces.
- [ ] No divination/liturgy imagery (cowries, ikin, Ifá board, igbodu, Odù marks).
- [ ] Orisha shown ONLY as natural force; no deity figure, monster, pet, or boss; documented attributes only; **no double-axe for Sango.**
- [ ] No skulls, occult, blood, dark-sacrifice, witchcraft, or horror framing; mood reads warm/reverent.
- [ ] Traditions kept distinct (Ane = Igala vs Sango/Osun = Yoruba); no generic "tribal" mush.
- [ ] Palette obeys §1: indigo base + Àṣẹ gold sacred light; path accents only where path-relevant; Fall art warm-dim, never grey/red.
- [ ] Technique obeys §2: painterly, soft light, atmospheric depth (not flat/cel/photoreal/3D).
- [ ] Composition obeys §3: reverent verticality, negative space, eye leads to gold light, lower-third UI clearance.
- [ ] (Portraits) reads as the SAME aging person; age ↑ and aura ↑ monotonic; ordinary attire throughout.
- [ ] (Any rendered text) full Yoruba diacritics, correct names from GAMEPLAY §2.2 — but AI art itself generated text-free.
- [ ] (Batch) seed/palette/Soul locked so the set reads as one world/person.

**Reviewer operating procedure, in order:** (1) **glance/pareidolia test** — squint; does any light/cloud/shadow resolve into a face/figure/mask/hand/throne? (2) **object scan** — any Universal-Negative item present? (3) **per-asset HARD lines** (§7 + §9). (4) **text scan** — any baked lettering → reject, regenerate text-free. (5) **mood/tone** — warm/reverent/premium; fall warm-dignified; storm awe not menace. (6) **consistency** (portraits) — same aging person, ordinary attire. (7) **distinctness** (path set) — earth vs storm vs river. **Log every reject** (asset + red-line(s) + offending element) — the log tunes prompt fragments and is the evidence pack for the §7.10 sign-off.

---

## 10. Consistency strategy — one cultivator across six portraits (the Soul method)

The single biggest production risk is the 6-portrait ascent reading as six different people. Strategy, in order:

**Step 1 — Approve Stage 1 (Ọmọ Ayé) first, as the anchor.** Generate and lock the Stage-0 portrait to spec (§4): ordinary child, faint chest-spark, indigo ground, the exact painterly technique and palette of this guide. Iterate ONLY this one until it is the definitive face/skin/identity. Everything downstream inherits from it.

**Step 2 — Train a reusable Soul from the approved Stage-1 portrait.** Use the locked Stage-1 image as the reference set for a **Higgsfield Soul** (custom character identity). This Soul is the identity carrier for all six portraits — generate Stages 2–6 *through the Soul* so facial structure persists while age + aura vary via prompt. (Render multiple approved angles/expressions of Stage 1 first if needed for a robust Soul.)

**Step 3 — Lock seed + palette per batch.** Generate the six as one batch with a **fixed seed family** and the **exact §1 palette hexes** pinned in every prompt, changing ONLY the controlled variables (age band, aura intensity, path accent at stages 3+, setting hint). Re-roll within the locked seed family rather than free-rolling.

**Step 4 — The consistency contract (hold constant in EVERY portrait prompt):** same Soul/identity; the §2 technique block ("rich digital painting, soft painterly light, warm gold over deep indigo, atmospheric depth"); the §1 palette hexes; centered chest-up framing; ordinary regalia-free attire; àṣẹ as warm gold light. **Vary ONLY:** age, aura intensity, (stage 3+) path accent, background hint.

**Step 5 — Side-by-side QA.** Lay all six in a strip: is it obviously ONE aging person? Does age climb monotonically? Does aura climb monotonically? Is attire ordinary throughout? If any portrait breaks identity, re-roll it through the Soul at the locked seed — never accept a near-miss face.

**Step 6 — Extend the discipline to non-portrait batches.** Backgrounds share one seed/palette family so the six read as one world; the three path motifs share a seed family (varying only force + accent) so they read as a set; ancestor frames (radiance/ember) are one batch. Consistency is a per-batch lock everywhere, not just for portraits.

---

## 11. Respectful culture vocabulary (quick reference)

The operative principle: **render the natural force the orisha governs, never the orisha's cult apparatus.** Thunder, not the thunder-priest's axe. River-water and brass-light, not the river-priestess's altar. Earth and growth, not the earth-shrine. Orisha are patrons who *bless* — forces of nature in the art, never figures, collectibles, or judges. And **do not homogenize:** Ane is **Igala** (Niger–Benue confluence, distinct from Igbo Ala and Yoruba Ilẹ̀); Sango and Osun are **Yoruba** (Oyo / Osogbo).

| Term | What it is | EVOKE (safe, abstract) | SACRED (never render; negative-prompt) |
|---|---|---|---|
| **Àṣẹ** | Yoruba "power to make things happen" | warm gold light, motes, glow | — (it is light, render freely as light) |
| **Orí** | the inner head / chosen destiny | the cultivator holding true at the river (text-framed) | Ajalá's workshop / pre-birth choosing scene |
| **Ìrékọjá** | "The Crossing" (death-as-homecoming) | night river, far-bank lights, drums (homecoming) | any deity/judge/gatekeeper figure |
| **Ane** (Igala earth) | Igala earth deity; harvest/endurance | soil, strata, mountain, roots, black + maize-gold, woven earth-rings | egwu/Oloja masquerade, scarification marks, earth shrine, a personified Ane |
| **Sango** (Yoruba thunder) | deified Alaafin of Oyo; thunder/justice | forked lightning, storm, fire, red + white | oṣẹ́ Ṣàngó double-axe, bàtá drums, edùn àrá thunderstones, mortar, ram, any king/figure |
| **Osun** (Yoruba river) | orisha of sweet waters; lineage/flow | flowing water, gold/brass/honey, ripples, abstract peacock-eye | a goddess/woman figure, the Osogbo grove/shrine, ẹbọ offering bowls, love-goddess framing |
| **Adire** | secular Yoruba resist-dyed indigo cloth | indigo + ecru geometry: oníko (rings/sunbursts), eléko (grids), alábéré (fine lines), bàtàní (tiling); motifs Olokun (water), Eléko (royalty), Ibadandun (place) | personifying Olokun as a deity/character |
| **Ancestors** | recent named → individual; old → merged lineage | stars/constellations (named), gold bloom (ascended), ember (fallen), river of light (merged) | egungun masks/regalia, any face/figure |
| **Council shrine** | the lineage's place of memory | calabash, plain carved posts, central àṣẹ flame, lights | egungun mask wall, ritual staffs of office |

**Final reminder:** orthography is respect (§7.9), and this brief reduces risk but does **not** replace the §7.10 native-speaker review before launch.
