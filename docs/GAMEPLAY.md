# Gameplay Design: Ori Ascendant
**Version:** 1.0
**Last Updated:** 2026-06-14
**Status:** Partially superseded — the MVP has been rescoped to a dynasty-narrative idle game (ADR-0002, `docs/DYNASTY_REDESIGN.md`). The *engine* described here (rate formula, Stages, Paths, offline, Council) largely survives; the *prestige experience* — flat-60/40 Crossing (§2.3/§3.5), "no names" (§5.4), fixed-odds honesty (§8) — is amended by ADR-0004 and the redesign. Where they conflict, the redesign brief is the north star.

This document is the detail layer between PRD.md (what/why) and TECH_DESIGN.md (how). It specifies how the game actually plays, screen by screen, with final numbers. Where this doc and older prose in TECH_DESIGN conflict, this doc wins; the locked decisions log lives in §10.

---

## 1. The Fantasy

You are not a cultivator. You are a **lineage**. Each generation, one child of the bloodline walks the road from Ọmọ Ayé (a soul newly arrived in the world's marketplace) to Aṣẹ́gun (victor at the river's edge), then faces **Ìrékọjá — The Crossing**. Whether they cross in radiance or stumble at the river, they join the Ancestral Council and the bloodline grows stronger. The player's real progress bar is the council shrine, not the Àṣẹ counter.

Framing proverb (title screen + loading): **"Ayé l'ọjà, ọ̀run nilé"** — *The world is a marketplace; ọ̀run is home.*

---

## 2. Final Numbers (verified by simulation)

### 2.1 Rate formula (amended — one change from TECH_DESIGN v1.1)

```
asePerSecond = baseRate
             × stage.productionMultiplier
             × path.aseGenerationModifier                 // 1.0 until path chosen
             × (1 + path.councilBonusModifier             // 1.0 for all but Osun
                    × (lineage.permanentAseBonus + Σ_active(W × ancestor.bonusMultiplier)))
```

- `W = 0.25` (CouncilConfig.ancestorBaseBonus). `currentPath == -1` ⇒ all three path modifiers read as 1.0.
- **Amendment:** `path.councilBonusModifier` wraps the *entire* lineage term (permanent + active together). This preserves Àṣẹ-neutral retirement for every path: retiring moves `W × bonus` between the two wrapped terms, sum unchanged.

Offline: `earned = cachedAsePerSecond × path.offlineRateModifier × clamp(now − lastSaveTimestamp, 0, 28800)`, with the fresh-install guard (`lastSaveTimestamp == 0` ⇒ no gain). Cap is on **time**, never modified by paths.

### 2.2 Stage table (StageConfig assets — final values)

`baseRate = 1.0 Àṣẹ/s` (GameplayConfig). Thresholds are **cumulative** Àṣẹ required to advance *out of* the stage; Àṣẹ is never spent.

| Idx | Display | Tier | Name | Meaning | productionMultiplier | aseToAdvance | One-liner (StageConfig description) |
|---|---|---|---|---|---|---|---|
| 0 | Stage 1 | 0 — Ayé | Ọmọ Ayé | Child of the World | 1 | 100 | "A newborn soul in the world's great marketplace, feeling the first stir of àṣẹ." |
| 1 | Stage 2 | 0 — Ayé | Akẹ́kọ̀ọ́ | The Learner | 5 | 1,500 | "A devoted learner, training breath and tongue to carry àṣẹ." |
| 2 | Stage 3 | 0 — Ayé | Awo | The Initiate | 20 | 5,500 | "Admitted to the mysteries at last — the initiate must now choose a path." |
| 3 | Stage 4 | 1 — Ọ̀run | Aláàṣẹ | Wielder of Àṣẹ | 80 | 100,000 | "One whose word makes things happen; àṣẹ answers when they speak." |
| 4 | Stage 5 | 1 — Ọ̀run | Àgbà | The Elder | 320 | 750,000 | "An elder of weight; the whole lineage steadies itself around their presence." |
| 5 | Stage 6 | 1 — Ọ̀run | Aṣẹ́gun | The Victor | 1,250 | — (Tribulation gate) | "Victor of the mortal road, standing at the river's edge where ayé ends." |

Tier names: **Ayé** ("The Marketplace", stages 1–3) and **Ọ̀run** ("The Road Home", stages 4–6). Tribulation gate: **25,000,000 Àṣẹ** (TribulationConfig — replaces the 1M placeholder, which would leave only ~2m13s of capstone).

### 2.3 Path table (PathConfig assets — final values)

One hook per path, on orthogonal axes (Sid Meier doubling rule: every hook ≥1.5×, never ±10%). All paths: tribulation odds identical (locked 60/40); `tribulationType` is presentation-only.

| Path | Deity / Theme | aseGenerationModifier (online) | offlineRateModifier | councilBonusModifier | Identity line (PathScreen card) |
|---|---|---|---|---|---|
| **Ane** | Igala earth — endurance | 1.0 | **1.5** | 1.0 | "The Mountain Endures — Àṣẹ gathers while you rest: ×1.5 offline" |
| **Sango** | Yoruba thunder — fury | **2.0** | 0.5 (net offline ×1.0) | 1.0 | "The Storm Strikes Now — ×2 Àṣẹ while you cultivate" |
| **Osun** | Yoruba river — lineage | 1.0 | 1.0 | **2.0** | "The River Remembers — ancestors' blessings flow twice as strong: council bonuses ×2" |

- Net offline = `aseGenerationModifier × offlineRateModifier` (cached rate already contains the online multiplier): Ane ×1.5, Sango ×1.0 ("the storm sleeps when you are away"), Osun ×1.0.
- Osun ancestors: +50% ascended / +20% fallen (vs +25%/+10%). **All council copy is computed from the active PathConfig, never hardcoded.**
- Per-path bonus per ancestor must render on: PathScreen card, tribulation confirm sheet, ancestor card, gen-N+1 preview.
- Path is **re-chosen every generation** (tribulation resets `currentPath = -1`). Emergent arc: race early gens on Sango, harvest a full council on Osun.
- Known EV skew (accepted for MVP, single-knob tunable in assets): Ane wins raw daily Àṣẹ for low-engagement players; Sango wins generation cycle speed; Osun wins late-game.

### 2.4 Expected pacing (gen 1, active play)

Tier 0 (path-less, identical for all): Stage 1 → 100s, Stage 2 → 280s, Stage 3 → 200s. **Path gate at 9m40s** — inside the PRD's 10-minute window.

| From → To | Sango (×2) | Ane / Osun (×1) |
|---|---|---|
| Stage 4 | 9m51s | 19m41s |
| Stage 5 | 16m56s | 33m51s |
| **Reach Stage 6** | **~36 min** | **~63 min** |
| Stage-6 grind (24.25M) | 2h42m | 5h23m |
| **Gen-1 active-only total** | **~3h18m** | **~6h26m** |

**Intended casual path (all paths):** one engaged evening to Stage 6 (~36–63 min) → sleep → tribulation ready in the morning. Overnight bank at stage-6 cached rate: Ane 54M, Sango/Osun 36M — all ≥ the 24.25M needed. The 8h cap never blocks gen 1; it only makes >8h absences confer nothing extra.

**Gen-2 council factor:** Ane/Sango — 1.25 (ascend) / 1.10 (fall); Osun — 1.50 / 1.20. Gen-2 clears Tier 0 ~20–33% faster passively, far faster with channeling (§5.3). Full ascended council: ×2.25 (Ane/Sango) or ×3.5 (Osun) — Osun reaches the post-MVP content wall ~3 generations earlier; acceptable, flagged for Tier 2 planning.

**2-minute visibility:** every stage moves its progress bar ≥8.9% in 2 minutes except the stage-6 gate (+0.6–1.2%) — therefore the tribulation bar **must render percent with one decimal** ("37.2% → 38.1%").

### 2.5 Config constants (ScriptableObjects — no magic numbers)

| Asset | Field | Value |
|---|---|---|
| GameplayConfig | baseRate | 1.0 |
| | tapChannelSeconds | 5.0 |
| | welcomeBackMinSeconds | 60 |
| | autosaveIntervalSeconds | 30 |
| TribulationConfig | baseAscendChance | 0.60 |
| | aseThreshold | 25,000,000 |
| | ambientFractions | 0.50 / 0.80 / 1.00 |
| | holdToConfirmSeconds | 0.8 |
| CouncilConfig | ancestorBaseBonus (W) | 0.25 |
| | maxCouncil | 5 |

---

## 3. Screens

Single Main scene; screens are toggled GameObjects. UGUI + TextMeshPro, one Screen Space-Overlay Canvas, CanvasScaler "Scale With Screen Size" 390×844 (iPhone 12 baseline), match 0.5. Separate child Canvas for the 1Hz Àṣẹ counter (isolates rebuilds). All interactive elements live in the bottom-third thumb zone.

### 3.1 Title / first launch
Black screen → title "Ori Ascendant", proverb + translation, "Touch to begin." One touch → MainScreen, counter at 0, +1.0/s. That is the entire onboarding (tutorial is a non-goal). The channel hint (§5.3) fires ~10s in.

### 3.2 MainScreen (top → bottom)
1. **Header (~6–10%):** Generation badge left ("Gen 3"), settings gear right.
2. **Àṣẹ counter (~10–20%):** the big number (~52pt), suffix-abbreviated (1.24M, 3 sig figs, never scientific in MVP — `BigNumber.ToDisplayString()`); "+12.4 Àṣẹ/s" rate line beneath (~17pt). Counter visibly ticks every 1s; label updates only when the formatted value changes.
3. **Identity line (~20–24%):** "Stage 4 — Aláàṣẹ" + path glyph & hook badge once chosen ("ACTIVE ×2" / "OFFLINE ×1.5" / "COUNCIL ×2").
4. **Cultivator portrait (~24–58%):** static per-stage art + slow ambient drift of àṣẹ motes flowing in. This is the **tap-to-channel** target.
5. **Stage progress bar (~58–64%):** fill = aseAmount/aseToAdvance, clamped at 100%; label "Next: Àgbà — 45.2K / 100K". At stage 6 this becomes the **Tribulation bar** with one-decimal percent.
6. **Primary CTA (~66–76%, full-width, ≥50pt):** "Advance" — disabled-but-visible until affordable (goal always on screen); at stage 6 + 25M it morphs into pulsing **"Face the Tribulation"**.
7. **Ancestral Council strip (~78–88%):** five 56pt slots; ancestor icons (fallen rendered dimmed/ember), empty slots outlined. Tapping opens CouncilScreen. Nothing interactive below (home indicator).

### 3.3 PathScreen (choice gate)
Trigger: player taps **Advance at stage index 2** with the threshold met. Modal, mandatory, no dismiss — choosing IS the advance into Tier 1 (diegetic: Awo, the initiate, enters the mysteries by choosing).
- Three cards, each: deity name + tradition of origin ("Ane — Igala earth deity"), identity line, **one concrete stat line** (from §2.3 — numbers, not adjectives).
- Tap card → expands with flavor + the stat restated; "Walk this Path" confirm button.
- On confirm: stage → index 3, `OnPathChosen` fires, rate recomputes, **the rate line visibly jumps** (20/s → 160/s Sango, → 80/s others) — the path's first legibility moment.
- Choice is locked for the generation; re-chosen next generation.

### 3.4 Welcome Back (offline collect)
Trigger: cold start **and** resume-from-background when `elapsed ≥ welcomeBackMinSeconds (60)`; below that, credit silently. (60s — not 180s — so Ane's hook is legible from a 2-minute phone lock.)
Modal over dimmed MainScreen: (1) "Your Orí kept watch" header; (2) time away — show "8h (cap)" honestly when capped; (3) earned Àṣẹ as 1.0–1.5s count-up, tap-to-skip; (4) rate context "at 12.4 Àṣẹ/s"; (4b) **Ane only:** itemized highlighted line "Earth's Patience ×1.5: +6,200"; (5) single full-width **Collect**. One tap, ≤3s total, no multiplier choice (no ads in MVP), no close-X.
**Sequencing rule:** Collect modal always resolves first; only after dismiss does the tribulation escalation state evaluate (§4.4). Ambient stingers never replay in sequence on return — the screen state jumps silently to the current fraction; only the 100% "eligible" morph plays its fanfare, once per generation.

### 3.5 TribulationScreen — Ìrékọjá, "The Crossing"
Visual frame: a night river; drums and lights of the lineage on the far bank (death-as-homecoming, not divine judgment; no deity rendered as judge — red line §7).

**Buildup (on MainScreen, keyed to TribulationConfig.ambientFractions):** 50% — sky tint + low drum layer; 80% — storm vignette + distant thunder; 100% — CTA morphs to pulsing "Face the Tribulation" + lightning particles. **Player-triggered, never automatic.**

**Confirm sheet:** two-outcome table computed from active path — "Ascend — radiant Ancestor, +25% lineage Àṣẹ" / "Fall — ember Ancestor, +10% lineage Àṣẹ" (+50%/+20% for Osun); line: *"Either way, your lineage grows stronger."* Mythic odds copy ("Most who are ready ascend; those who fall are honored among the ancestors") + a "?" panel disclosing exactly: **"Ascend: 60%. Fall: 40%. Every Tribulation, every generation, same odds. Both outcomes grant an Ancestor."** (Disclosed-but-not-foregrounded: hiding odds breeds superstition; foregrounding reads as slot UI.) Confirm = 0.8s hold-to-begin ring.

**Resolution rule (crash-safety):** roll once at confirmation; write outcome + full new-generation state to SaveData **before** the ceremony plays. If the app dies mid-animation, the reveal replays from save. The ceremony beats are pure theater and **visually identical for both outcomes until the reveal** — no staged partial RNG, no near-miss theatrics, ever.

**Ceremony (~9s to reveal, ~17–20s to gameplay):**
| Beat | Time | Content |
|---|---|---|
| Transition | 0–2s | silhouette, music drops to heartbeat drum |
| Storm waves | 2–5s | 3 lightning strikes @1s; haptics med/med/heavy; flash each |
| Held breath | 5–6.5s | whiteout, **total silence**, no UI |
| Reveal | 6.5s+ | branch |

**Ascend:** gold palette, figure rises, major chord, "ASCENDED — a good crossing."
**Fall:** never grey/red, no buzzer. Warm embers; figure kneels and dissolves into rising sparks; soft kora lament; **"THE LINE ENDURES"** — *"A fallen cultivator still watches over their blood."* A hard crossing — arrival at honor early, not denial of it.
Both branches then play the **same** ancestor-card ceremony: card forges (radiance vs ember frame + path motif), bonus shown from path config, card flies to the council strip, count ticks. **On the fall screen, prove the gain:** "Lineage Àṣẹ: 1.00× → 1.10×" counting up (the honest inverse of losses-disguised-as-wins).
If council is full: retirement beat first — "**[Ancestor] settles into the foundation of the house**" with the permanentAseBonus delta shown (~2s, dignified, never framed as deletion).
**Skippability:** full ceremony plays untouched the first time each outcome is seen (seenFlags bits); afterwards any tap during the waves jumps to the held-breath beat (floor ~4s).

**Generation summary → N+1 (peak-end rule — end on the number going up):** "Generation N complete" (time-in-generation, peak Àṣẹ, path taken) → "Generation N+1" preview: old stage-1 rate vs new stage-1 rate side by side + council strip → final beat (2s): *"A child of the lineage takes up the path"* + stage-1 portrait → play.

### 3.6 CouncilScreen (lineage shrine)
Framing: carved staffs, calabash, flame motifs — explicitly NOT egungun imagery (§7). Shows: 5 ancestor cards (path motif + radiance/ember frame, "Gen N — Aṣẹ́gun of Thunder", contribution line computed from path config), a **lineage foundation** line (permanentAseBonus from retired ancestors), and the total council factor as one number ("Lineage blessing: ×1.50"). Àtúnwá flavor: each generation is the lineage returning stronger. Display-only in MVP.

### 3.7 Settings (gear)
BGM / SFX toggles · **About & Glossary** (heritage statement: "a fantasy homage by a descendant, drawing on Igala, Yoruba, and Igbo traditions" + term glossary with full diacritics + pronunciation) · cloud status line ("Game Center: connected / local save only") · version. Nothing else.

---

## 4. Flows

### 4.1 The 2-minute check-in (≤10 taps, ≥1 state change)
Welcome Back collect (1 tap, 3s) → glance at counter + bar → **Advance** if affordable (1 tap; rate line jumps ×4) → optional 20–30s channel burst to crest a near threshold → leave with the next goal visible.

### 4.2 The 20-minute session (an arc with a capstone)
Either the Tier-1 path push (choice at ~9m40s + first doubled-rate stages) or the stage-6 tribulation push. Channeling at ~11× idle makes 20 active minutes ≈ 3–4 idle hours; the 60/40 ceremony + ancestor reveal is the payoff.

### 4.3 Multi-advance on return
Banked overnight Àṣẹ often affords 2–3 advances. Advance stays **one stage per tap** (each tap = its own rate-jump moment); the bar clamps at 100% and the CTA stays lit until the player has caught up. The cached offline rate is always the stage they left at (forgiveness property — never zero banked Àṣẹ on advance).

### 4.4 Generation reset scope (on tribulation resolution, written atomically before ceremony)
- `aseAmount → 0` · `currentStage → 0` · `currentPath → -1` · `generationStartTimestamp → now` · `lineage.generationCount++`
- New AncestorData appended (peakStage 5, path, didAscend, bonusMultiplier 1.0/0.4, completedTimestamp); retirement first if council full.
- `asePerSecond` recomputed (sole writer: AseGenerationSystem) — gen N+1 starts at `baseRate × councilFactor`.
- Stage-6 escalation state is derived from `ase/threshold`, so it resets implicitly. seenFlags persists (ceremony skippability), audio/settings persist.

### 4.5 First session script (cold, no tutorial)
0:00 title → touch · 0:10 channel hint pulses ("Touch your cultivator to channel àṣẹ") · ~1:40 first Advance affordable (inside the first 2-min session) · ~6:20 Stage 3 (Awo) · ~9:40 PathScreen — first real decision, rate visibly multiplies on confirm · ~36–63 min Stage 6, storm building · evening ends; overnight banks the gate · next morning: Welcome Back (capped, honest) → Collect → CTA morphs → first Crossing.

---

## 5. Mechanics detail

### 5.1 Tick
1s game tick via Update() frame-accumulator (`acc += unscaledDeltaTime; while (acc >= 1f) …`) — zero long-run drift; Unix timestamps remain the source of truth. Not InvokeRepeating, not coroutines (supersedes TECH_DESIGN §6's InvokeRepeating decision; see §10).

### 5.2 Saves
Primary hook `OnApplicationPause(true)` (iOS never reliably fires Quit) + autosave every 30s + on every progression event (advance, path, tribulation, retirement). Local JSON synchronous in the pause handler; cloud push opportunistic while foregrounded, never in the pause handler. Offline calc runs on cold load **and** `OnApplicationPause(false)`.

### 5.3 Tap-to-channel (the one active mechanic)
Tap the portrait: `aseAmount += asePerSecond × tapChannelSeconds (5.0)`. Floating "+N" text, portrait pulse, light haptic. ~2 taps/s ≈ 11× idle — active play clearly wins, bounded by thumb fatigue, no cooldown state, no cap code, strictly optional. Because it reuses the full rate formula, channeling as Sango (×2) feels different from Ane — paths differ even while tapping. Discoverability: one-time hint at ~10s of play (seenFlags bit 0) + portrait idle pulse every ~10s untouched. **Safest cut if scope demands — nothing depends on it** (but gen-2's visceral acceleration partly rides on it; passive gen-2 stage 1 is 80s vs prestige-UX's <30s ideal — channeling closes that gap).

### 5.4 Ancestor identity (no names in MVP)
> **Amended by the dynasty redesign (ADR-0002; `docs/DYNASTY_REDESIGN.md`):** names are now *in scope* — an ascended ancestor bears a Title (Stage-name + a personal name) and a fallen one a deed-tied Nickname, both drawn from a **curated, §7.10-reviewed pool** (still never player-typed, never generated). The custom-naming-is-a-non-goal and abstract-visuals stances below still stand; the *no-names* stance does not.

No name pools (custom naming is a non-goal; names are a cultural-sensitivity asset requiring native-speaker review — post-MVP with AncestorTemplate). Cards derive entirely from AncestorData: title "Gen N — Aṣẹ́gun of [Thunder/Earth/the River]"; frame = radiance (ascend) / ember (fall); motif = path (Ane earth/stars, Sango lightning/flame, Osun river/light). Abstract visuals only — never masks/regalia.

### 5.5 Number formatting
Suffix abbreviation everywhere (1.24K/M/B/T, 3 sig figs) via `BigNumber.ToDisplayString()`. Rate always paired with the counter. Tribulation bar: one-decimal percent.

### 5.6 Cosmetic Appearance (per generation) — Phase D
The active Cultivator is rendered with one **Appearance** from a small fixed pool (initially three — see CONTEXT.md). The Appearance is rolled once when a new generation begins at the Crossing, persisted in the same atomic write as the rest of the generation reset (§4.4), and is **purely cosmetic** — never affecting Stage, Path, Àṣẹ rates, or the Council; it only selects which set of per-stage portraits (§3.2 item 4) the generation shows. Generation 1 is fixed to the first appearance (the lean humble man, the only one with reference art); the roll takes effect from generation 2. Deferred to Phase D alongside the portrait pipeline and art — full rationale and the SaveData/schemaVersion consequence in `docs/adr/0001-per-generation-appearance.md`.

---

## 6. SaveData v1 (final shape — supersedes TECH_DESIGN §5 listing)

Adds two fields to the locked v1 shape (greenfield, pre-code, so no migration):

```csharp
public int schemaVersion = 1;
public double aseMantissa; public int aseExponent;
public double asePerSecondMantissa; public int asePerSecondExponent;  // cached; AseGenerationSystem sole writer
public int currentStage = 0;          // 0–5; display = index + 1
public int currentPath = -1;          // -1 none / 0 Ane / 1 Sango / 2 Osun; reset to -1 each generation
public long lastSaveTimestamp;        // Unix UTC seconds
public long generationStartTimestamp; // NEW — gen summary "time in generation"
public int seenFlags = 0;             // NEW bitmask: 1=channel hint, 2=ascend ceremony, 4=fall ceremony
public List<AncestorData> council = new();
public LineageData lineage = new();   // permanentAseBonus (additive, 0.0), generationCount
```

PathConfig gains: `offlineRateModifier = 1.0` (read only by OfflineProgressCalculator), `councilBonusModifier = 1.0` (read only by RecalculateRate). Defaults of 1.0 ⇒ a misconfigured asset degrades to neutral, never broken.

---

## 7. Cultural red lines (binding for all content)

1. Never render, name, or stat-ify the supreme God (Olódùmarè/Ọlọ́run, Igala Ọjọ́) — including as tribulation judge.
2. No real initiatory offices as ranks/cosmetics (Babaláwo, Ìyánífá, Olúwo, Ẹlẹ́gùn, Alagbaa). Stage names use generic standing words only (§2.2).
3. No egungun masks/regalia anywhere; ancestors are abstract (light/river/flame/stars).
4. No reproduced liturgy: no Odù Ifá verses, no divination mechanics, no bàtá rhythms as "spells", no igbodu depiction.
5. Orisha are patrons who bless — never collectibles, pets, or boss fights; use documented attributes only.
6. Zero "voodoo"/witchcraft framing; no Ìyàmi/àjẹ́; never gamify ẹbọ.
7. Don't homogenize: label each path's tradition of origin (Ane is Igala — distinct from Igbo Ala / Yoruba Ilẹ̀). Ship the About/heritage statement; schedule one native-speaker/community review pass before launch.
8. Xianxia loanwords ("cultivator", "tribulation") are fine — they signal genre fantasy.
9. Orthography is respect: full diacritics in display names (Akẹ́kọ̀ọ́, Aláàṣẹ). **Week-1 task: TMP font test for subdot+tone stacking; Noto Sans fallback; degrade to dotted-only (Akẹkọọ), never bare ASCII.**

---

## 8. Honest-design commitments (from the gambling-psychology research)

- Odds always disclosed exactly, one tap away; never foregrounded as slot UI; "same odds every time" stated to kill gambler's-fallacy grinding.
- No near-misses, no staged partial RNG, no losses-disguised-as-wins — the fall screen shows the real rate delta.
- No hidden dynamic odds, ever. (Post-MVP comfort for the 6.4% who open with three falls: cosmetic streak copy first; a *disclosed* pity rule at most.)
- Offline cap shown honestly ("8h (cap)").

---

## 9. PRD success-metric traceability

| PRD metric | Mechanism |
|---|---|
| Full loop crash-free | Roll-once-persist-first tribulation; save-before-ceremony; autosave web |
| Offline correct & displayed every open | Resume-path offline calc (pause(false) + cold load); 60s modal floor; honest cap line |
| 3 paths meaningfully different in 10 min | Path gate at 9m40s; ≥1.5× hooks on orthogonal axes; Sango legible at confirm, Ane at first ≥60s lock, Osun at card/preview + first tribulation |
| Save survives force-close | Pause-hook saves + 30s autosave + event saves; conflict rule: higher generationCount, then higher aseAmount |

---

## 10. Adjudication log (conflicts resolved 2026-06-12)

1. **Path design:** orthogonal axes (paths lens) over surge-shapes (balance lens) — no timers, 2 config fields; balance sensitivity verified the curve for path EV ∈ [1.0, 2.0]. *User-confirmed.*
2. **Path effect timing:** choice moved to the Advance tap at index 2 (gateway into Tier 1, effect instant) — kills the choose-then-wait dead zone and reconciles "Sango doubles at selection" with path-less stages 0–2.
3. **Welcome Back floor:** 60s (unity-impl) over 180s (session-ux) — required for Ane legibility; still suppresses app-switch spam.
4. **Gen-2 "<30s stage 1":** unreachable with locked W; closed via channeling (~7s tapped), rate-delta preview, and council strip. Target relaxed to "visibly faster + delta shown."
5. **Stale session-ux numbers** (1M gate, 17-min Tier 0): re-derived against §2.2/§2.4 throughout this doc.
6. **Council copy:** computed from active PathConfig (Osun +50%/+20%), never static strings.
7. **Osun vs locked W:** W stays 0.25; Osun's ×2 wraps the whole lineage term (formula amendment §2.1) — neutrality preserved; long-run table fork accepted and flagged.
8. **Tribulation threshold:** 25,000,000 (verified); stage-3 threshold 5,500 (verifier's one-number fix to bring the path gate under 10 min).
9. **Tick mechanism:** Update() accumulator supersedes TECH_DESIGN §6 InvokeRepeating decision (drift; iOS suspension reality).
10. **Tap-to-channel, manual Advance, path re-choice per generation:** all in. *User-confirmed.*
