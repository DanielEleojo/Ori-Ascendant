# Lens: balance

## Summary
Genre research: per-purchase cost growth in successful idles sits in a tight 1.07–1.15 band (Clicker Heroes 1.07, Cookie Clicker 1.15, AdVenture Capitalist 1.07–1.15), which for milestone-based games like Ori translates to "each stage should take ~1.5–3x the previous stage's wall-clock time." Mobile best practice puts first prestige within the first play day (Antimatter Dimensions' first Infinity: <2h; design guides recommend prestiging when progress drops to 10–20% of peak speed, with a concrete bonus preview). The genre norm for first-prestige feel is ~2x; Ori's fixed W=0.25 yields only 1.25x on an ascend, so the proposed curve compensates with a very fast gen-2 early game and relies on the full council (2.25x at 5 ascended ancestors) to hit genre-standard prestige feel. Proposed numbers: BASE_RATE 1.0/s; stage multipliers 1/5/20/80/320/1,250; cumulative advance thresholds 100/1.5k/8k/100k/750k; tribulation threshold 25,000,000 (placeholder 1M would be reached in ~9 minutes at stage-6 rate and would make the offline window meaningless). Result: stage 6 reached in ~47 min of active play, tribulation needs 3h36m more at stage-6 rate — exactly one overnight offline window (well under the 28,800s cap, which banks 54M) — or ~4.4h total for a single-evening hardcore clear. Gen-2 wall-clock speedup: 20% (ascend), 9% (fall), 16% expected at the 60/40 coin.

## Recommendations

### [high] BASE_RATE = 1.0 Àṣẹ/sec
Keep the placeholder. All tuning lives in StageConfig multipliers/thresholds (per CLAUDE.md no-magic-numbers rule), and 1.0 keeps BigNumber math and mental arithmetic clean. Stage-1 player sees +1/s ticking immediately.

### [high] Stage productionMultipliers: 1, 5, 20, 80, 320, 1250 (index 0-5)
~x4-5 production growth per stage. Gen-1 rates: 1/s, 5/s, 20/s, then with nominal path x1.5 in Tier 1: 120/s, 480/s, 1,875/s. Every stage-up multiplies the visible rate by ~4x — the dopamine spike the genre depends on.

### [high] Cumulative Àṣẹ thresholds to advance: 100, 1500, 8000, 100000, 750000 (stages 1-5), tribulation gate = stage 6
Threshold growth (x15, x5.3, x12.5, x7.5) deliberately outpaces production growth (x4-5), giving per-stage time growth of x1.5-2.8 — the genre-standard 'each milestone takes a bit longer' shape derived from the 1.07-1.15 cost-curve family. Assumes Àṣẹ is cumulative (never spent on advance), consistent with the locked 'ase >= threshold' tribulation eligibility. If you later make advancement SPEND Àṣẹ, times barely change (thresholds grow ~10x so delta ~= full threshold).

### [high] Tribulation threshold = 25,000,000 (replaces 1,000,000 placeholder)
At stage-6 rate 1,875/s the 1M placeholder is reached in 533s (~9 min) — no capstone tension and the 8h offline cap never matters. 25M = 3h36m at stage-6 rate: one normal night's sleep covers it (8h offline banks 54M, >2x what's needed, so even a 4h sleep qualifies), while a hardcore player can still grind it out in one long evening (~4.4h total active). This is the single designed wait of the generation.

### [high] Target wall-clock times: 47 min active to stage 6; full gen 1 = one evening + one overnight (~10-12h wall, ~50 min engaged)
Per-stage (gen 1): 1m40s / 4m40s / 5m25s / 12m47s / 22m34s, cumulative 47.1 min to enter stage 6. Path choice lands at minute ~6.3; Tier 1 (where the path multiplier bites) at minute ~11.7 — inside the PRD's 10-min path-differentiation session window. Matches mobile best practice of first prestige within the first play day, and beats Antimatter Dimensions' <2h-to-first-Infinity benchmark for engaged players (~4.4h active-only) while the intended path is evening + sleep.

### [high] Gen-2 speedup: 20% on ascend, 9% on fall, 16% expected — below genre norm, compensate in UI
Council factor with W=0.25 fixed: ascend = 1.25x rate = 20% less wall-clock (stage 6 in 37.7 min, full gen ~3.5h active-equivalent); fall = 1.10x = 9%. Genre norm for first prestige is ~2x (Firestone auto-prompts at 2x), so Ori's first ascension will feel modest unless the UI sells it: show '+25% Àṣẹ from your Ancestor' as a concrete preview on the tribulation screen and on the gen-2 HUD. Gen-2's early stages are now genuinely snappy (stages 1-2 in under 5 min combined). Full council of 5 ascended = 2.25x (gen ~6+), which is where the prestige loop reaches genre-standard feel; permanent retirement bonus then adds +0.25/gen unbounded — flag that content past ~gen 8-10 (factor 3x+) needs Tier 2 stages post-MVP.

### [medium] Path multipliers (assumption, not a locked deliverable): all three EV ~= x1.5 in Tier 1, differentiated by SHAPE
All timing math above assumes nominal pathMultiplier 1.5 from stage index 3. To make paths 'feel meaningfully different in 10 min' without breaking balance: Osun (river) flat x1.5 always; Sango (storm) x1.0 base with a x4 surge for 30s every 180s (EV = (150x1 + 30x4)/180 = exactly 1.5); Ane (earth/ancestors) x1.25 active but x2 effective offline (rewards the idle player). Flat-multiplier-only spreads like 1.35/1.5/1.65 read as ±10% and will NOT feel different in 10 minutes. Sensitivity: if paths ship at flat 1.0, Tier 1 times stretch x1.5 (stage-6 grind 5h23m) — still inside one overnight window, so the curve is robust either way.

### [high] 2-min session visibility: guaranteed everywhere except late stage 6 — fix with a one-decimal percent readout
Worst case is mid-stage-6: 2 min earns 225,000 Àṣẹ = +0.93% of the tribulation bar. The Àṣẹ counter visibly climbs, but render tribulation progress as '37.2% -> 38.1%' (one decimal) so every 2-min session shows the number move. All other stages: a 2-min session moves the stage bar >=8.9% (stage 5) up to a full stage-up (stage 1 completes inside the first 2-min session at 100s).

### [high] Offline forgiveness property: leaving early over-banks, never wastes
Because thresholds are cumulative and asePerSecond is cached at the stage the player left on, a player who sleeps at stage 5 (480/s x 8h = 13.8M) returns far past the 750k threshold and that surplus directly shortens the stage-6 grind to ~2h after advancing. No offline window is ever 'wasted' by being at the wrong stage — keep this; do not zero banked Àṣẹ on stage advance.

## Numbers
## Stage configuration (StageConfig ScriptableObjects)

BASE_RATE = **1.0 Àṣẹ/sec** | Tribulation threshold = **25,000,000** | W = 0.25 (fixed) | Nominal path EV = ×1.5, Tier 1 only

| Idx | Display | Tier | productionMultiplier | Gen-1 rate (path ×1.5 at idx≥3) | aseToAdvance (cumulative) | Threshold growth | Rate growth |
|---|---|---|---|---|---|---|---|
| 0 | Stage 1 | 0 | 1 | 1.0/s | 100 | — | — |
| 1 | Stage 2 | 0 | 5 | 5/s | 1,500 | ×15.0 | ×5.0 |
| 2 | Stage 3 | 0 | 20 | 20/s | 8,000 | ×5.3 | ×4.0 |
| 3 | Stage 4 | 1 | 80 | 120/s | 100,000 | ×12.5 | ×6.0 (incl. path) |
| 4 | Stage 5 | 1 | 320 | 480/s | 750,000 | ×7.5 | ×4.0 |
| 5 | Stage 6 | 1 | 1,250 | 1,875/s | 25,000,000 (tribulation) | ×33.3 (capstone) | ×3.9 |

## Time-per-stage arithmetic — Generation 1 (active play, council factor 1.0)

time = (threshold − previous threshold) ÷ rate

| Stage | Àṣẹ to earn | ÷ Rate | = Time | Cumulative | Time growth |
|---|---|---|---|---|---|
| 1→2 | 100 − 0 = 100 | 1.0/s | **100 s (1m40s)** | 1.7 min | — |
| 2→3 | 1,500 − 100 = 1,400 | 5/s | **280 s (4m40s)** | 6.3 min ← path choice | ×2.8 |
| 3→4 | 8,000 − 1,500 = 6,500 | 20/s | **325 s (5m25s)** | 11.7 min ← enters Tier 1 | ×1.16 |
| 4→5 | 100,000 − 8,000 = 92,000 | 120/s | **767 s (12m47s)** | 24.5 min | ×2.4 |
| 5→6 | 750,000 − 100,000 = 650,000 | 480/s | **1,354 s (22m34s)** | **47.1 min → stage 6** | ×1.8 |
| 6→Trib | 25,000,000 − 750,000 = 24,250,000 | 1,875/s | **12,933 s (3h35m)** | 4h 22m active-only | ×9.5 (designed wall) |

**Offline check:** needed at stage 6 = 12,933 s ≈ 3.6h < any normal night; 8h cap banks 1,875 × 28,800 = **54.0M** (2.2× the gate). Cap never blocks gen 1.

**Gen-1 wall-clock targets:** ~47 min engaged evening → stage 6 → sleep → tribulation ready next morning (**total ~10–12h wall, ~50 min engaged**). Hardcore single-sitting clear: **~4h22m**.

## Generation 2 (rates × council factor)

| Outcome | Council factor | To stage 6 | Stage-6 grind (or offline) | Wall-clock speedup |
|---|---|---|---|---|
| Ascend (60%) | 1 + 0.25×1.0 = **1.25** | 37.7 min | 2h52m | **−20.0%** |
| Fall (40%) | 1 + 0.25×0.4 = **1.10** | 42.8 min | 3h16m | **−9.1%** |
| Expected (60/40) | 1 + 0.25×0.76 = **1.19** | ~39.6 min | ~3h01m | **−16%** |

## Long-run council factor (ascends only, ×(1+0.25n))

| Gen | Active ancestors | Factor | Gen length vs gen 1 |
|---|---|---|---|
| 1 | 0 | 1.00 | 100% |
| 2 | 1 | 1.25 | 80% |
| 3 | 2 | 1.50 | 67% |
| 6 | 5 (full council) | 2.25 | 44% — genre-standard "2× prestige" feel |
| 10 | 5 + 4 retired (+1.00 permanent) | 3.25 | 31% — Tier 2 content needed post-MVP |

## Sensitivity: pathMultiplier = 1.0 (no/flat path)

Tier 1 times ×1.5: stage 4 = 19m12s, stage 5 = 33m51s, stage-6 grind = 5h23m → still inside one 8h overnight. Curve survives any path value in [1.0, 2.0].

## 2-min session visibility audit

| Player position | 2-min earnings | Visible movement |
|---|---|---|
| Stage 1 | 120 Àṣẹ | full stage-up + 20 banked |
| Stage 5 | 57,600 | +8.9% of stage bar |
| Stage 6 (worst) | 225,000 | +0.93% of tribulation bar → render % with one decimal |

## Sources
- The Math of Idle Games, Part I — Kongregate (Anthony Pecorella) https://blog.kongregate.com/the-math-of-idle-games-part-i/
- The Math of Idle Games, Part II — Game Developer https://www.gamedeveloper.com/game-platforms/the-math-of-idle-games-part-ii
- The Math of Idle Games, Part III — Game Developer https://www.gamedeveloper.com/design/the-math-of-idle-games-part-iii
- Numbers Getting Bigger: The Design and Math of Incremental Games — Envato Tuts+ https://code.tutsplus.com/numbers-getting-bigger-the-design-and-math-of-incremental-games--cms-24023a
- Idle Games Best Practices: Design and Strategy — GridInc https://gridinc.co.za/blog/idle-games-best-practices
- Idle Clicker Games: Best Practices for Design and Monetization — Mind Studios https://games.themindstudios.com/post/idle-clicker-game-design-and-monetization/
- STRATA — feedback on prestige timing and early curve (itch.io design thread) https://itch.io/t/6472187/strata-idle-digging-game-feedback-on-prestige-timing-and-early-curve
- Prestige — Egg, Inc. Wiki https://egg-inc.fandom.com/wiki/Prestige
- Prestige — Firestone Idle RPG (auto-prompt at 2x) https://fs.r2games.com/content/prestige_10258.html
- Antimatter Dimensions achievements (first-Infinity timing benchmarks) https://gamefaqs.gamespot.com/mac/382776-antimatter-dimensions/achievements

## VERIFICATION (needs-correction)
## Simulation walkthrough (rate = BASE_RATE × stageMult × pathMult(idx≥3) × councilFactor; time = Δthreshold ÷ rate)

### Test 1 — Per-stage times, gen 1 (path 1.5 from idx 3, council 1.0)
Designer's table reproduces EXACTLY:
- idx0: 100 ÷ 1/s = 100.0s (1m40s), cum 1.7 min
- idx1: 1,400 ÷ 5/s = 280.0s (4m40s), cum 6.3 min (path choice)
- idx2: 6,500 ÷ 20/s = 325.0s (5m25s), cum 11.75 min (enters Tier 1)
- idx3: 92,000 ÷ 120/s = 766.7s (12m47s), cum 24.5 min
- idx4: 650,000 ÷ 480/s = 1,354.2s (22m34s), cum 47.1 min (stage 6)
- idx5: 24,250,000 ÷ 1,875/s = 12,933.3s (3h35m33s)

### Test 2 — Gen-1 total: PASS
Active-only: 15,759s = 4h22m39s (between 1h floor and 12h ceiling). Casual path: 47.1 min engaged → one overnight (needs 3.6h of the 8h window; 8h banks 54.0M = 2.23× the 24.25M gate) → ~10–12h wall. Cap never blocks gen 1. Verified.
Arithmetic error in the designer's justification (conclusion unaffected, actually strengthened): with cumulative thresholds the player ENTERS stage 6 holding 750k, so the old 1M placeholder needs only 250,000 ÷ 1,875/s = 133s ≈ 2m13s — not "~9 min" (533s is 1M-from-zero). The 25M raise is even more necessary than claimed.

### Test 3 — Gen-2 speedup: PASS (marginal), designer's numbers confirmed
Ascend (60%): factor 1.25 → all times ×0.8 → stage 6 in 37.7 min, grind 2h52m, total 3h30m = exactly 20.0% faster. Meets the ≥~20% "visibly stronger" bar, with no margin — the +25% rate jump must be sold in UI (tribulation preview + gen-2 HUD badge) as the designer recommends. Fall (40%): factor 1.10 → 9.1% faster — below the visibility bar, but W=0.25 and bonusMultiplier=0.4 are locked design; fall is the penalty outcome. Nit: expected speedup is E[1/f] = 0.6×0.8 + 0.4×0.909 = 15.6%, not 1/E[f] = 16.0% (Jensen). Immaterial.

### Test 4 — 2-minute session: PASS
Stage 1: 120s × 1/s = 120 Àṣẹ vs 100 threshold → full stage-up inside the first 2-min session + 20 banked, counter ticks +1/s. Other stages: 2-min moves the bar 42.9% (idx1), 36.9% (idx2), 15.7% (idx3), 8.9% (idx4). Worst case idx5: 225,000 = 0.93% of the 24.25M gate — designer's one-decimal-percent readout fix is required and sufficient.

### Test 5 — 10-minute path test: FAIL as proposed
Paths are mechanically inert at idx 0–2 (locked: multiplier 1.0). With the proposed thresholds, Tier 1 — the first moment any path does anything — arrives at cumulative 705s = 11.75 min. The summary's claim that minute ~11.7 is "inside the PRD's 10-min path-differentiation session window" is false on its face (11.75 > 10). In the first 10 minutes of gen 1, all three paths produce identical output; divergence = zero. Additionally the player chooses a path at minute 6.3 and then waits 5m25s before it has any effect — a dead zone. (Gen 2+ enters Tier 1 at 9.4 min with one ascended ancestor, so only gen 1 misses.)
Fix (single number): stage-3 threshold 8,000 → 5,500. Stage 3 becomes 4,000 ÷ 20/s = 200s; Tier-1 entry = 100+280+200 = 580s = 9m40s < 10 min, and this holds for ANY path value since stages 0–2 are path-less. The post-choice dead zone shrinks to 3m20s. Side effects: stage-4 delta 94,500 ÷ 120/s = 787.5s (13m08s); to stage 6 = 45.4 min; gen-1 total = 15,655s = 4h21m — everything else within seconds of the original. Time-growth sequence becomes ×2.8, ×0.71, ×3.9, ×1.7, ×9.5 — the ×0.71 dip right after path choice is acceptable (a deliberate "launch ramp" that propels the chosen path into the tier where it matters; the designer's own curve already flattened to ×1.16 there for the same reason). With shape-differentiated paths (Sango ×4 surge every 180s, Osun flat ×1.5, Ane ×1.25/×2-offline), divergence is detectable within one 180s surge cycle of Tier-1 entry — i.e., well inside minute 10–13 of gen 1 and within any 10-min Tier-1 session. Flat spreads (1.35/1.5/1.65) would NOT pass; the shaped designs are load-bearing, keep them.

### Test 6 — Offline cap interaction: intended pacing, no hole
8h banks at cached stage rate: idx0 28.8k (skips to mid-stage-4 progress), idx1 144k, idx2 576k, idx3 3.46M (skips stage 5 entirely), idx4 13.8M (lands ~56% into the stage-6 gate, shortening the grind to ~1h38m–2h), idx5 54M (tribulation ready — the designed overnight). Early-stage skips are genre-normal and self-limiting: the cached-rate rule means sleeping early banks at 1/s instead of 1,875/s, so offline always undercounts vs. active play. Worst exploit: sleep at stage 5, then a second night at stage 6 → full gen in ~25 min active + 2 nights — still "first prestige within first play day" territory, consistent with intent. The forgiveness property (never zero banked Àṣẹ on advance) is correct; keep it. The 28,800s cap binds only in the sense intended: it bounds the stage-6 bank at 2.23× the gate, so longer absences confer nothing extra.

### Verdict rationale
Every load-bearing number in the proposal verifies to the second. Three defects: (1) Test 5 fails as proposed — paths exert zero effect in gen-1's first 10 minutes, and the summary misstates 11.75 min as inside the 10-min window; fixed by one threshold change (8,000 → 5,500). (2) The 1M-placeholder justification understates the problem (2m13s remaining, not ~9 min) — conclusion stands. (3) Expected-case speedup is 15.6%, not 16% — cosmetic. Hence needs-correction, with a minimal one-number fix.

## FINAL NUMBERS
## FINAL corrected numbers (one change from proposal: stage-3 threshold 8,000 → 5,500)

BASE_RATE = **1.0 Àṣẹ/sec** | Tribulation threshold = **25,000,000** | W = 0.25 (locked) | Path EV ×1.5, Tier 1 (idx≥3) only

### Stage configuration (StageConfig ScriptableObjects)

| Idx | Display | Tier | productionMultiplier | Gen-1 rate (×1.5 path at idx≥3) | aseToAdvance (cumulative) | Threshold growth |
|---|---|---|---|---|---|---|
| 0 | Stage 1 | 0 | 1 | 1.0/s | 100 | — |
| 1 | Stage 2 | 0 | 5 | 5/s | 1,500 | ×15.0 |
| 2 | Stage 3 | 0 | 20 | 20/s | **5,500** | ×3.7 |
| 3 | Stage 4 | 1 | 80 | 120/s | 100,000 | ×18.2 |
| 4 | Stage 5 | 1 | 320 | 480/s | 750,000 | ×7.5 |
| 5 | Stage 6 | 1 | 1,250 | 1,875/s | 25,000,000 (tribulation) | ×33.3 (capstone) |

### Expected per-stage times — Generation 1 (active, council 1.0)

| Stage | Àṣẹ to earn | ÷ Rate | = Time | Cumulative |
|---|---|---|---|---|
| 1→2 | 100 | 1.0/s | 100 s (1m40s) | 1.7 min |
| 2→3 | 1,400 | 5/s | 280 s (4m40s) | 6.3 min ← path choice |
| 3→4 | **4,000** | 20/s | **200 s (3m20s)** | **9.7 min ← Tier 1, paths active < 10 min** |
| 4→5 | **94,500** | 120/s | **787.5 s (13m08s)** | 22.8 min |
| 5→6 | 650,000 | 480/s | 1,354 s (22m34s) | **45.4 min → stage 6** |
| 6→Trib | 24,250,000 | 1,875/s | 12,933 s (3h35m) | **4h 21m active-only** |

**Gen-1 totals:** 45.4 min engaged to stage 6; tribulation via one overnight (needs 3.6h; 8h cap banks 54.0M = 2.23× the gate) → ~10–12h wall / ~46 min engaged; hardcore single-sitting clear 4h21m. Old 1M placeholder would leave only 2m13s of capstone (250k remaining at 1,875/s) — 25M is required.

### Generation 2 (one ancestor)

| Outcome | Council factor | To stage 6 | Stage-6 grind | Total active | Speedup |
|---|---|---|---|---|---|
| Ascend (60%) | 1.25 | 36.3 min | 2h52m | **3h29m** | **−20.0%** |
| Fall (40%) | 1.10 | 41.2 min | 3h16m | 3h57m | −9.1% |
| Expected (60/40) | — | ~38.3 min | ~3h02m | ~3h40m | **−15.6%** (E[1/f], not 1/E[f]) |

### Long-run council (ascends only)

| Gen | Active / retired | Factor | Gen length vs gen 1 |
|---|---|---|---|
| 1 | 0 / 0 | 1.00 | 100% |
| 2 | 1 / 0 | 1.25 | 80% |
| 6 | 5 / 0 (full council) | 2.25 | 44% — genre-standard 2× prestige feel |
| 10 | 5 / 4 (+1.00 permanent) | 3.25 | 31% — Tier 2 content needed post-MVP |

### Conditions attached (load-bearing, not optional)
1. Paths must ship SHAPE-differentiated: Osun flat ×1.5; Sango ×1.0 base + ×4 surge 30s/180s (EV 1.5); Ane ×1.25 active / ×2 effective offline. Flat spreads 1.35/1.5/1.65 fail the 10-min feel test.
2. Stage-6 tribulation progress renders with one decimal (e.g. 37.2% → 38.1%) so a 2-min session (225,000 Àṣẹ = +0.93%) visibly moves.
3. Tribulation screen previews "+25% Àṣẹ from your Ancestor" and gen-2 HUD shows the council bonus — the 20% ascend speedup needs UI amplification to read as genre-standard prestige.
4. Never zero banked Àṣẹ on stage advance (offline forgiveness property depends on cumulative thresholds).
5. Sensitivity holds: at flat path ×1.0, Tier-1 entry unchanged at 9m40s, stage-6 grind 5h23m — still inside one 8h overnight. Curve survives any path EV in [1.0, 2.0].
