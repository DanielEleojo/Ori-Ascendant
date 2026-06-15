# Ori Ascendant — Dynasty Redesign (design direction)

**Status:** Direction captured from the 2026-06-14 design session. The *spine* and
the *core mechanics* are locked; what remains (length numbers, the crossroads
data-model) is spec/tuning, noted under "Open." Supersedes parts of GAMEPLAY.md and
PRD §4 (see ADR-0002). All cultural content named here is subject to the §7.10
native-speaker review.

## Why
The locked MVP played thin: one verb (tap), nothing to stay for, and a payoff (the
Crossing) that didn't mean anything because nothing led up to it. We are rebuilding
the MVP around a dynasty story the player authors.

## The spine
- **Engine:** idle cultivation — the existing rate formula, six Stages, Paths,
  offline progress, and Council remain, as the machine underneath.
- **Point:** a per-player **Chronicle** — the bloodline's story, authored by the
  player's choices, accreting across generations.
- **Stories = authored deck (A) → personal chronicle (B).** Folklore-rooted,
  §7.10-reviewed beats, stitched per-player by choice and outcome. No live
  generation (ADR-0003).
- **The new verb: Crossroads.** Climb-tied choice-events that queue patiently for
  each check-in — the journey becomes a road with forks, not a progress bar.
- **Idle-first is law** (ADR-0005): patient crossroads, idle-rate density; "long"
  lives in the dynasty, never in grind.

## The mechanics (resolved)

**Per-life loop.**
- **Ori = a virtue held as a vow** ("the Path of Mercy"), chosen at birth
  (Àkùnlẹ̀yàn) from a small set (~4–6).
- **Crossroads** are dilemmas whose options each belong to a virtue; your own
  virtue's option is always on the table — the test is *temptation, not
  impossibility.* Climb-tied, patient, idle-rate (~1 per check-in stretch).
- **Steadfastness = the rate you held your virtue** — a legible "held N of M."
- **deity-Path (Ane/Sango/Osun)** is an orthogonal facet — *how* you climb,
  committed at Stage 3 — and never touches Crossing odds.
- **The Crossing is probabilistic, steadfastness-dominant, floor + ceiling**
  (~25% full-waver → ~90% full-faith; the old flat 60% near the midpoint), always
  shown. The river keeps the last word — even the steadfast can rarely fall
  (ADR-0004).

**Remembrance & dynasty.**
- **Ascend → a Title:** Stage-name as honorific + a personal name from a curated,
  §7.10-reviewed pool ("Aṣẹ́gun Adé").
- **Fall → a Nickname:** a deed-tied epithet selected by the single most defining
  deed / manner of the fall ("the one who turned back at the ford").
- **Names are in scope** as a curated, reviewed pool — never player-typed, never
  generated (amends GAMEPLAY §5.4).
- **Dynasty = light compounding:** the Chronicle mainly commemorates, plus a thin
  bounded thread where forebears reach forward (an unfinished vow becomes a
  descendant's crossroad; a sustained line-virtue earns a soft legacy). The Council
  rate-bonus is unchanged — this is the *story* face of the same ancestors.

## The loop, end to end
Choose your Ori → climb (offline + optional channel) → meet crossroads and choose,
holding or straying from your vow → at Stage 3 commit your deity-Path facet → reach
the Crossing → it weighs your steadfastness, transparently → ascend (Titled) or fall
(Nicknamed) → the Chronicle gains a chapter → the next descendant begins, the line
stronger and the story longer.

## Open — next
- **Length numbers.** Per-life pacing target (first Crossing within ~a day of idle),
  dynasty length, offline-cap interaction. *Needs simulation, like §2.4 — not a
  grill.*
- **Crossroads deck (data model).** Beat format, virtue-tagging, how a choice maps
  to virtue-alignment, contextual draw, the forebear-seeded thread. *A design task.*
- **Deity-Path as facet.** The fine print of how Ane/Sango/Osun sits inside an Ori.
- **Art close-out (parked).** Ascended reveal = bespoke transfiguration, exalted
  bearing (CONTEXT.md); still open: +3 per Appearance, and MVP vs post-MVP given the
  §7.10 critical-path cost.

## Anchors
- **Glossary:** CONTEXT.md — Ori, Steadfastness, Àkùnlẹ̀yàn, Crossroads, Chronicle,
  Deed, Title, Nickname, Appearance, Ascended reveal.
- **Decisions:** ADR-0002 (rescope), ADR-0003 (authored not generated), ADR-0004
  (steadfastness Crossing), ADR-0005 (idle-first).
- **Engine that survives:** GAMEPLAY §2.1 (rate + offline), §2.2 (stages), §2.3
  (paths), §3.6 (council) — re-baselined where the spine changes them.
