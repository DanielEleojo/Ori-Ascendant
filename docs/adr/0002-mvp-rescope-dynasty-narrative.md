# MVP rescoped: from minimal idle-prestige to a dynasty-narrative idle game

The original MVP (PRD §4; GAMEPLAY "Locked for MVP") was a ruthlessly-scoped idle
prestige loop: tap to gather Àṣẹ, climb six Stages, pick a Path, cross the
Tribulation, grow an Ancestral Council. In play it proved too thin — one verb
(tap), nothing to stay for, and a payoff (the Crossing) that meant little because
nothing led up to it. We are deliberately rescoping the MVP around a **dynasty
story**: the idle cultivation becomes the *engine*, and the *point* becomes the
per-player Chronicle the player authors as their bloodline climbs.

This is a genuine product pivot, chosen over shipping the thinner loop sooner,
because the thin loop did not deliver an experience worth retaining. "Locked for
MVP" in GAMEPLAY.md and the 5-feature list in PRD §4 are now provisional and will
be re-baselined as the new spine is specced.

## Consequences
- GAMEPLAY.md and PRD §4 are partially superseded; see `docs/DYNASTY_REDESIGN.md`
  for the new spine. Much of the existing engine (rate formula, Stages, Paths,
  offline calc, Council) survives, as the machine beneath the story.
- The load-bearing decisions of the pivot follow in ADR-0003 (authored story, not
  generated), ADR-0004 (the Crossing as a steadfastness test), and ADR-0005
  (idle-first).
