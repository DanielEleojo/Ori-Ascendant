# Lens: prestige-ux

## Summary
Research across prestige design, gambling psychology, and behavioral economics converges on a clear shape for the Tribulation. Suspense theory (Ely/Frankel/Kamenica) says to concentrate belief-variance into one held beat just before a fast, honest reveal — not to drip-feed pseudo-information, which is exactly the near-miss manipulation regulators and the slot-machine literature condemn. Because our 40% fall pays a genuine permanent reward (0.4x ancestor, rate strictly increases), we are the ethical inverse of a "loss disguised as a win" — and we should prove it on screen by showing the before/after lineage rate delta on the fall screen, not just play warm music. Hades demonstrates that failure framed as narrative progress (failure triggers MORE story, not less) eliminates the punishment feel, and Yoruba/Igala ancestral veneration makes this diegetic for free: becoming an ancestor is itself an honored state, so a fall is an early arrival at honor, not a denial of it. On odds: XCOM research shows resentment spikes when high displayed percentages (85-95%) miss, but 60% is near coin-flip and creates no entitlement; gacha transparency research (Genshin, China/Korea disclosure law, Apple's paid-lootbox rules) shows disclosure builds trust while hiding breeds superstition — so disclose the exact 60/40 one tap away in an info panel, keep the ceremony itself qualitative and mythic.

## Recommendations

### [high] Pre-tribulation buildup: ambient escalation keyed to threshold fractions, player-triggered climax
Once at stage index 5, show a 'Tribulation Approaches' progress bar (ase/threshold). Layer ambience at 50%/80%/100% of threshold (fractions in a TribulationConfig ScriptableObject, per no-magic-numbers rule): 50% = sky tint + low drum layer; 80% = storm vignette + distant thunder; 100% = pulsing 'Face the Tribulation' button with lightning particles (Sango storm imagery). The Tribulation must be PLAYER-TRIGGERED, never automatic — voluntary commitment reframes outcome attribution (players accept outcomes of chosen risks; prestige research shows players need agency plus a concrete preview before pressing reset). The button opens a confirm sheet showing BOTH outcomes as a two-row table: 'Ascend — Ancestor of Radiance, +25% lineage Àṣẹ' / 'Fall — Ancestor of Embers, +10% lineage Àṣẹ', plus the line 'Either way, your lineage grows stronger.' Confirm via 0.8s hold-to-begin ring (prevents misfires, adds ritual commitment).

### [high] Roll once, persist before animating — never stage partial RNG
Resolve the 60/40 weighted roll at the instant of confirmation, write outcome + new generation state to SaveData BEFORE the ceremony plays (crash-safety: PRD requires the generational loop crash-free; if the app dies mid-animation the outcome must persist on reload, with the reveal replayed from save). The three storm-wave beats in the ceremony are pure theater and must be VISUALLY IDENTICAL regardless of outcome. Never show '2 of 3 trials passed' or a wheel landing one notch from Ascend — manufactured near-misses are the single most manipulative pattern in the gambling literature (attentional-bias studies; UK Gambling Commission guidance calls for removing them). With both outcomes paying real rewards, we don't need fake almost-wins, and using them would poison the trust the fall-framing depends on.

### [high] Reveal sequence: ~9 seconds, three thunder beats, one silent hold, fast honest resolve
Suspense is maximized by concentrating uncertainty into a single held beat before resolution (Ely/Frankel/Kamenica: suspense = variance of next-period beliefs). Sequence: (1) 0-2s transition — cultivator silhouette, music drops to heartbeat drum; (2) 2-5s three lightning strikes at 1s intervals, haptics medium/medium/heavy (UIImpactFeedbackGenerator), screen flash each strike; (3) 5-6.5s whiteout + total silence — the held-breath beat, no UI, no sound; (4) 6.5s+ branch reveal. Pack-opening research confirms anticipation pacing (signal → tension → reveal) drives the dopamine arc, and the silence beat is the cheapest, most powerful tool a solo dev has. Skippability: full ceremony plays untouched the first time each outcome is seen; afterwards any tap during waves jumps to the hold beat (respects the 2-minute-session PRD target without gutting the first-time peak).

### [high] Fall is a bittersweet honor, structurally identical to Ascend — difference of grade, not kind
Ascend: gold palette, figure rises, major chord, success haptic, title 'ASCENDED'. Fall: NEVER grey/red, no buzzer, no 'FAILURE'. Warm ember palette; the figure kneels and dissolves into rising embers; soft kora/choral lament; gentle haptic; title 'THE LINE ENDURES' with subtitle 'A fallen cultivator still watches over their blood' (the core pillar, verbatim, as diegetic text). This is cosmologically free: in Yoruba/Igala ancestral veneration, joining the honored dead IS an exalted state — frame the fall as arriving at honor early, not being denied it. Critically, both branches then play the SAME ancestor-card ceremony (card forges, joins Council shelf, council count ticks) — Hades shows failure must trigger equal-or-more content than success to stop feeling like punishment. The only differences: frame art (radiance vs embers) and the bonus number. And prove the gain is real, not celebration-noise: on the FALL screen show the lineage rate delta counting up, e.g. 'Lineage Àṣẹ: 1.00x → 1.10x'. This is the exact inverse of 'losses disguised as wins' — slot LDWs celebrate net losses; we have a net gain and must display the arithmetic so the player can verify it.

### [high] Odds display: qualitative in the ceremony, exact 60/40 one tap away — never hidden, never foregrounded
Disclose the true numbers, but not on the ceremony screens. Confirm sheet carries mythic copy ('Most who are ready ascend; those who fall are honored among the ancestors') plus a '?' info panel stating plainly: 'Ascend: 60%. Fall: 40%. Every Tribulation, every generation, same odds. Both outcomes grant an Ancestor.' Justification: (a) hiding odds breeds superstition and wiki-mistrust — players will invent theories that path or timing changes the roll, then feel deceived when datamined; gacha research shows disclosed rates (Genshin set the norm; China/Korea mandate it; Apple requires it for paid loot boxes) measurably increase trust and reduce burnout; (b) foregrounding a big '60%' on the trigger button reads as slot-machine UI and invites probability-resentment; (c) XCOM research shows the resentment problem lives at 85-95% displayed odds where misses feel like robbery — at 60% the number is honest about being near a coin flip, so disclosure is psychologically cheap. Also state 'same odds every time' explicitly to kill gambler's-fallacy grinding.

### [high] Transition to generation N+1: end on the number going up (peak-end rule)
After council induction (and the oldest-ancestor retirement beat when council is full: 'Ancestor X passes into the lineage' with the permanentAseBonus delta shown — dignified, ~2s, never framed as deletion), show a Generation Summary: 'Generation N complete' with time-in-generation, peak Àṣẹ, path taken; then 'Generation N+1' preview with old stage-1 rate vs new stage-1 rate side by side. Kahneman's peak-end rule: the sequence is remembered by its peak (the reveal) and its end — so the END must be concrete acceleration, not the reset. Final beat: 2s 'A child of the lineage takes up the path' with the stage-1 portrait, then drop into play. Balance requirement this implies: generation 2 must visibly clear stage 1 dramatically faster than gen 1 did (target under ~30s) — the AdVenture Capitalist lesson is that prestige only feels like reward when the post-reset speed is immediately, viscerally observable.

### [medium] Post-MVP flag (do not build now): fall-streak comfort, not odds manipulation
MVP's flat 60/40 is locked and fine — falls pay out, so there is no dead end. But note the math: 6.4% of players will open with three consecutive falls and will feel cursed despite the ember ancestors. Post-MVP options ranked: (1) cosmetic streak acknowledgment — third consecutive fall gets unique copy ('Three embers burn in the shrine; the heavens take notice') costing nothing and converting the streak into lore; (2) a disclosed pity rule (guaranteed ascend after 2 falls), which gacha research shows players tolerate bad luck far better when the worst case is bounded — but ONLY if disclosed, since a hidden pity would be discovered and erode the 'honest 60%' trust. Do not ship hidden dynamic odds under any circumstance.

## Numbers
## Tribulation Ceremony Timing (first-time, unskipped)

| Beat | Time | Visual | Audio | Haptic |
|---|---|---|---|---|
| Ambient tier 1 | at 50% of threshold | sky tint | low drum layer fades in | — |
| Ambient tier 2 | at 80% | storm vignette | distant thunder | — |
| Eligible | at 100% | pulsing "Face the Tribulation" button, lightning particles | drum intensifies | — |
| Confirm sheet | user-paced | two-outcome table + "?" odds panel | — | light (sheet open) |
| Hold-to-begin | 0.8s hold | ring fill around button | rising tone | light |
| Transition | 0–2s | silhouette, screen darkens | music drops to heartbeat | — |
| Storm wave 1 | 2.0–3.0s | lightning strike + flash | thunder crack | medium |
| Storm wave 2 | 3.0–4.0s | lightning strike + flash | thunder crack | medium |
| Storm wave 3 | 4.0–5.0s | biggest strike | loudest crack | heavy |
| Hold beat | 5.0–6.5s | whiteout, no UI | total silence | none |
| Reveal (Ascend) | 6.5–9.0s | gold light, figure rises, "ASCENDED" | major chord | success notification |
| Reveal (Fall) | 6.5–9.5s | embers, figure kneels/dissolves, "THE LINE ENDURES" | soft kora lament | gentle single tap |
| Ancestor card | +2.5s | card forges (radiance 1.0x / ember 0.4x frame), bonus shown (+25% / +10%) | forge shimmer | light |
| Rate delta (Fall only, also fine on Ascend) | +1.5s | "Lineage Àṣẹ: 1.00x → 1.10x" counts up | tick-up SFX | — |
| Council induction | ~3s | card flies to Council shelf, count ticks; retirement beat if council full | — | light |
| Gen summary | user-paced | Gen N stats; old vs new stage-1 rate | — | — |
| Gen N+1 start | 2s | "A child of the lineage takes up the path" + stage-1 portrait | theme returns, brighter key | — |

Total forced duration: ~9–12s to reveal, ~17–20s to gameplay. Repeat-view skip: tap during waves jumps to hold beat (sequence floor ~4s).

## Key constants (put in TribulationConfig ScriptableObject)

| Constant | Value |
|---|---|
| ascendChance | 0.60 |
| ambientTier fractions | 0.50, 0.80, 1.00 |
| holdToConfirmSeconds | 0.8 |
| stormWaveCount / interval | 3 / 1.0s |
| silenceHoldSeconds | 1.5 |
| ascend / fall bonusMultiplier | 1.0 / 0.4 (locked) |
| per-ancestor rate term | 0.25 × bonusMultiplier (locked) |
| Gen-2 stage-1 clear target | < 30s (balance goal) |
| P(3 consecutive falls) | 6.4% (post-MVP comfort flag) |

## Sources
- Ely, Frankel & Kamenica — Suspense and Surprise (Journal of Political Economy) https://www.journals.uchicago.edu/doi/abs/10.1086/677350
- Jake Solomon explains the careful use of randomness in XCOM 2 (Game Developer) https://www.gamedeveloper.com/design/jake-solomon-explains-the-careful-use-of-randomness-in-i-xcom-2-i-
- Probability Problems in Game Design (Game Developer) https://www.gamedeveloper.com/design/probability-problems-in-game-design
- The Near-Miss Effect in Slot Machines: A Review and Experimental Analysis (J. Gambling Studies) https://link.springer.com/article/10.1007/s10899-019-09891-8
- Slot Machine Psychology: How the Near Miss Effect Drives Player Behavior (Casino Center — incl. UK Gambling Commission guidance) https://www.casinocenter.com/slot-machine-psychology-how-the-near-miss-effect-drives-player-behavior-in-online-gaming/
- Losses disguised as wins: Slot machines and deception (Oxford Practical Ethics) https://blog.uehiro.ox.ac.uk/2013/07/losses-disguised-as-wins-slot-machines-and-deception/
- The Effect of Losses Disguised as Wins and Near Misses in EGMs: Systematic Review (PMC) https://www.ncbi.nlm.nih.gov/pmc/articles/PMC5663799/
- Achievement Relocked: Loss Aversion and Game Design (MIT Press, Engelstein) https://direct.mit.edu/books/book/4611/Achievement-RelockedLoss-Aversion-and-Game-Design
- Failure is Death, and Death is Progress: repetition and narrative progression in Hades (Medium) https://natalia-nazeem.medium.com/failure-is-death-and-death-is-progress-the-use-of-repetition-replayability-and-narrative-673cfa4e2e8
- How Hades Succeeds by Making Failure the Point (Collider) https://collider.com/hades-gameplay-mechanics/
- The Genshin Impact Standard: How Pity Systems Redefine Gacha Economics (COGconnected) https://cogconnected.com/2025/10/the-genshin-impact-standard-how-pity-systems-and-soft-currency-caps-redefine-gacha-game-economics/
- Pity System — Gacha Guaranteed-Drop Mechanic Explained (MWM glossary) https://mwm.ai/glossary/pity-system
- Case-Opening Mechanics in Game Design: Reward Loops & Dopamine (Gaming-Fans) https://gaming-fans.com/2025/08/case-opening-mechanics-in-game-design-reward-loops-dopamine/
- Peak-End Rule: Kahneman's Memory Heuristic (Yu-kai Chou) https://yukaichou.com/behavioral-analysis/peak-end-rule-kahneman-experience-design/
- Idle Game Design: Systems, Mechanics, and Progression (Missions Zanx) https://missionszanx.com/guides/idle-game-design-systems-mechanics-and-progression
- Angel Investors / Hard Reset (AdVenture Capitalist Wiki) https://adventure-capitalist.fandom.com/wiki/Angel/Hard_Reset
