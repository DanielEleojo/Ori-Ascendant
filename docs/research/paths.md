# Lens: paths

## Summary
Research across Realm Grinder (each faction = one mechanical identity: active play vs offline/idle vs long-term scaling), Egg Inc (offline-only multipliers up to 3x are genre-standard, surfaced on the collect screen), and Sid Meier's "double it or halve it" principle (players do not perceive small percentage deltas; differentiation must be 1.5x-2x to register) converges on a minimal-surface design: each path gets exactly ONE numeric hook on an orthogonal axis of the locked rate formula. Ane (earth, endurance) boosts offline earnings rate 1.5x; Sango (thunder, fury) doubles the online path multiplier (2.0 aseGenerationModifier) with offline normalized back to baseline; Osun (river, lineage continuity) doubles the entire council/lineage additive term (each ascended ancestor gives +50% instead of +25%). This requires adding exactly two double fields to PathConfig (offlineRateModifier, councilBonusModifier), touches no tribulation odds, respects the 8-hour offline TIME cap (only the rate is modified, never the cap), keeps OfflineProgressCalculator pure math (one extra multiply on the cached rate), and preserves Ase-neutral council retirement because Osun's modifier wraps permanentAseBonus and the active-council sum jointly. Each path is legible in a 10-minute session through a different UI moment: Sango's Ase/s readout visibly doubles at the instant of selection, Ane's bonus is itemized as a separate line on every Welcome Back collect screen (triggered by any phone-lock/background), and Osun's doubled contribution is printed on the ancestor card at the first tribulation. The known risk is that Ane dominates daily EV for offline-heavy players while Osun is invisible until the first ancestor exists; both are acceptable for the MVP differentiation metric and have one-knob tuning levers.

## Recommendations

### [high] Design pattern: one hook per path, on three orthogonal axes of the locked formula
asePerSecond = BASE_RATE x stageMult x pathMult x (1 + permanentAseBonus + Sum(0.25 x bonusMultiplier)) has exactly three path-safe levers without touching tribulation odds: (A) pathMult itself (online rate), (B) a multiplier applied only inside OfflineProgressCalculator (offline rate, cap untouched), (C) a multiplier on the lineage additive term (council scaling). Assign one lever per path: Sango=A, Ane=B, Osun=C. This is the Realm Grinder faction pattern (Elves=active, Undead=offline, Demons=scaling) compressed to the smallest possible mechanical surface. Per Sid Meier's doubling rule, every hook is sized 1.5x-2.0x -- never 1.1x-1.2x, which players cannot feel in 10 minutes.

### [high] Ane -- Path of Earth: 'The Mountain Endures' (offline 1.5x)
Identity: patience, rootedness, the land that grows while you sleep (Igala earth deity Ane / Igbo Ala). Hook: offlineRateModifier = 1.5; aseGenerationModifier = 1.0; councilBonusModifier = 1.0. Offline earned = cachedAsePerSecond x 1.5 x clamp(elapsed, 0, 28800). The 8h cap is NEVER extended (locked business rule) -- only the rate during capped time is boosted. Net effect: every Welcome Back screen pays 50% more. Detectable in-session because iOS players background/lock constantly and OfflineProgressCalculator runs on every resume; even a 2-minute phone lock shows the boosted collect. Genre anchor: Egg Inc's Internal Hatchery Calm is a 3x offline-only multiplier, so 1.5x is conservative and tunable upward.

### [high] Sango -- Path of Thunder: 'The Storm Strikes Now' (online 2.0x)
Identity: sudden overwhelming force while you are present (Yoruba thunder orisha). Hook: aseGenerationModifier = 2.0 (this IS the locked formula's pathMultiplier -- no new field needed); offlineRateModifier = 0.5 so net offline = 2.0 x 0.5 = 1.0x baseline ('the storm sleeps when you are away' -- Sango's bonus applies only while cultivating). The instant the player confirms Sango on PathScreen, RecalculateRate() fires (OnPathChosen is already a recompute trigger) and the Ase/s HUD readout visibly doubles -- the single most legible path moment in the game, zero new code paths. Sango's real value is velocity, not daily EV: in-session stage thresholds fall twice as fast, so Sango players cycle tribulations/generations fastest, which feeds the council. Document in the PathConfig asset that net offline = aseGenerationModifier x offlineRateModifier so the 0.5 is read as normalization, not a penalty.

### [high] Osun -- Path of the River: 'The River Remembers' (council/lineage 2.0x)
Identity: flow, continuity, mother of generations (Yoruba river orisha -- thematically perfect for the bloodline fantasy). Hook: councilBonusModifier = 2.0; aseGenerationModifier = 1.0; offlineRateModifier = 1.0. Formula application: bonus term becomes (1 + councilBonusModifier x (permanentAseBonus + Sum(0.25 x bonusMultiplier))). CRITICAL correctness detail: the modifier MUST wrap permanentAseBonus AND the active-council sum together, otherwise retiring the oldest ancestor (moving 0.25 x bonus from the sum into permanentAseBonus) would change the rate for Osun players, violating the locked 'retirement is Ase-neutral' business rule. Effect: each ascended ancestor = +50% (vs +25%), fallen = +20% (vs +10%); full ascended council = +250% vs +125% (Osun end-state is ~1.56x the other paths' lineage term). Since path is re-chosen each generation, an emergent arc appears with zero extra code: race early generations on Sango, harvest a full council on Osun.

### [high] PathConfig SO: add exactly 2 fields, keep tribulationType cosmetic-only
Add to PathConfig.cs: 'public double offlineRateModifier = 1.0;' (read ONLY by OfflineProgressCalculator: earned = cachedAsePerSecond x offlineRateModifier x clampedElapsed -- stays pure math, one extra multiply, no Time.timeScale, no new SaveData fields, cap logic untouched; treat currentPath == -1 as 1.0) and 'public double councilBonusModifier = 1.0;' (read ONLY by AseGenerationSystem.RecalculateRate(); existing recompute triggers -- path chosen, council change -- already cover it). The existing TribulationType enum stays as a pure presentation discriminator (per-path tribulation art/copy/SFX) and must never touch TribulationConfig.baseAscendChance = 0.60 (locked flat 60/40). No other fields needed; defaults of 1.0 mean a misconfigured asset degrades to neutral, never broken.

### [high] UI legibility: numbers in copy, a persistent badge, and an itemized collect screen
Differentiation only counts if surfaced. (1) PathScreen: each card carries one concrete stat line, not adjectives -- Ane: 'Ase gathers while you rest: x1.5 offline'; Sango: 'x2 Ase while you cultivate'; Osun: 'Ancestors' blessings flow twice as strong: council bonuses x2'. (2) HUD: small path glyph + modifier badge pinned next to the Ase/s readout ('OFFLINE x1.5' / 'ACTIVE x2' / 'COUNCIL x2') so the hook is always on screen. (3) Welcome Back screen MUST itemize: 'While you were away: 12,400 Ase' + separate highlighted line 'Earth's Patience x1.5: +6,200' -- this is the only way Ane is legible (Egg Inc does exactly this on its offline cap screen). (4) Sango: animate the Ase/s counter rolling up to 2x at selection. (5) Ancestor card + post-tribulation toast: 'Adaeze joins the Council: +50% Ase (River's Memory x2)' vs '+25%' for other paths.

### [medium] 10-minute detectability script (the PRD metric, made testable)
Sango: choose at stage index 2, watch Ase/s double on the spot, clear stages 3-5 at 2x speed -- detectable in under 60 seconds. Ane: choose, lock the phone for 2 minutes, resume, see the itemized x1.5 line on the collect screen -- detectable in under 5 minutes. Osun: with MVP placeholder thresholds the gen-1 tribulation is reachable in a test session; the first ancestor lands at +50% instead of +25% and the post-tribulation rate jump is visibly double-sized -- detectable at first tribulation, plus the PathScreen preview states it up front. Osun is structurally the least gen-1-detectable (zero ancestors at first choice); accept this for MVP since the success metric also includes completing the full generational loop, and the path is re-chosen every generation.

### [medium] Balance risks and single-knob tuning levers
Known EV skew: offline time dominates credited rate-seconds for typical idle play (a 4-sessions/day player credits 50k-80k offline seconds vs under 1k online), so Ane wins raw daily Ase for low-engagement players (~+45-50% daily) while Sango's +100% online is worth only a few percent of daily EV but doubles in-session push speed and generation cycling. This is acceptable for the MVP metric ('meaningfully different', not 'perfectly balanced'), and each path has exactly one tuning knob: Ane 1.5 -> 1.25 if dominant in playtests; Sango 2.0 -> 2.5/3.0 if active play feels unrewarded (Sid Meier: adjust by big steps); Osun 2.0 -> 1.5 if late-game snowballs. All three knobs live in ScriptableObject assets per the no-magic-numbers convention, so balance passes (Phase 4, TECH_DESIGN day 7) need no code changes.

## Numbers
## PathConfig asset values (proposed)

| Path | Deity / Theme | aseGenerationModifier (online pathMult, locked formula) | offlineRateModifier (NEW) | Net offline rate vs base | councilBonusModifier (NEW) | Per ascended ancestor | Per fallen ancestor | Full council (5 ascended) |
|---|---|---|---|---|---|---|---|---|
| Ane | Earth — endurance | 1.0 | 1.5 | **1.5x** | 1.0 | +25% | +10% | +125% |
| Sango | Thunder — active fury | **2.0** | 0.5 | 1.0x (normalized) | 1.0 | +25% | +10% | +125% |
| Osun | River — lineage flow | 1.0 | 1.0 | 1.0x | **2.0** | **+50%** | **+20%** | **+250%** |

Net offline rate = aseGenerationModifier x offlineRateModifier (offlineRateModifier multiplies the cached asePerSecond, which already contains the path multiplier).

## Formula integration

- Online (per 1s tick, locked): `asePerSecond = BASE_RATE x stageMult x aseGenerationModifier x (1 + councilBonusModifier x (permanentAseBonus + Sum(0.25 x bonusMultiplier)))`
- Offline (cap untouched): `earned = cachedAsePerSecond x offlineRateModifier x max(0, min(elapsed, 28800))`
- Tribulation: flat 0.60 ascend, all paths, unchanged. TribulationType = art/copy/SFX only.
- currentPath == -1 (stages 0-2): all three modifiers treated as 1.0.

## 10-minute detectability check (post-choice, gen 1, no council)

| Path | Signal | Time to detect | Magnitude |
|---|---|---|---|
| Sango | Ase/s readout doubles at selection; stages 3-5 fill 2x faster | < 1 min | 2.0x (well above perception threshold) |
| Ane | Itemized x1.5 line on every Welcome Back collect (any lock/background triggers it) | < 5 min | 1.5x on collect amount |
| Osun | First ancestor card shows +50% vs +25%; rate jump after tribulation is double-sized | At first tribulation | 2.0x on lineage term |

## Sources
- Realm Grinder Factions wiki — one mechanical identity per faction (Elves=active/clicking, Undead=offline, Demons=scaling) https://shapes.inc/fandom/realm-grinder/factions
- Realm Grinder — Offline Production wiki page https://realm-grinder.fandom.com/wiki/Offline_Production
- Egg Inc Wiki — Internal Hatcheries / Internal Hatchery Calm (3x offline-only multiplier) https://egg-inc.fandom.com/wiki/Internal_Hatcheries
- PocketGamer.biz — Why Egg, Inc is the gold standard for idle games (offline cap surfaced on collect screen) https://www.pocketgamer.biz/egg-inc-idle-games-gold-standard/
- NotesByLex — Doubling or Halving as a Game Balancing Technique (Sid Meier) https://notesbylex.com/doubling-or-halving-as-a-game-balancing-technique
- Game Developer — Sid Meier's Key Design Lessons (players don't notice small changes) https://www.gamedeveloper.com/game-platforms/analysis-sid-meier-s-key-design-lessons
- Eric Guan — Idle Game Design Principles (multiplier compounding, idle/active split) https://ericguan.substack.com/p/idle-game-design-principles
- Task Bar Hero class tier list — idle-class vs active-class numeric tradeoffs in shipped idle games https://www.xmodhub.com/info/blog/task-bar-hero-best-class/
- Ori Ascendant PRD (rate formula, council math, success metrics) file:///home/baba/ASE/docs/PRD.md
- Ori Ascendant Tech Design (PathConfig SO, OfflineProgressCalculator, recompute triggers) file:///home/baba/ASE/docs/TECH_DESIGN.md
