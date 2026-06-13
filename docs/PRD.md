# PRD: Ori Ascendant
**Version:** 1.1
**Last Updated:** 2026-06-09
**Status:** Draft

---

## 1. Problem Statement

The mobile idle/cultivation genre is saturated with identical Chinese xianxia clones — same realm names, same tap-to-cultivate loop, same gacha banners, zero cultural differentiation. Players looking for idle progression games with genuine identity, narrative weight, or cultural authenticity have nothing to choose from. Ori Ascendant fills this gap with a cultivation idle game built on West African cosmology (Igala, Yoruba, Igbo traditions), featuring a generational bloodline prestige mechanic that replaces generic number-go-up resets with emotionally resonant legacy-building across cultivator lifetimes.

---

## 2. Solution Overview

Ori Ascendant is a mobile idle game where the player cultivates Àṣẹ — divine force — to advance a bloodline across generations. The player is not one cultivator grinding to infinity; they are a royal lineage trying to produce an ancestor powerful enough to break an ancestral curse. Each cultivator lives, cultivates, faces their Tribulation, and either ascends as a full-power ancestor or falls as a lesser one. Both outcomes feed a growing Ancestral Council that permanently strengthens every generation that follows. The game is mechanically an idle incremental but emotionally a legacy-builder — you are constructing something generational, not chasing a number.

---

## 3. Target User

**Primary:** Mobile gamers aged 18–35 who are fans of idle/incremental games (AFK Arena, Archero, Cookie Clicker) and want more narrative and cultural identity than the genre currently offers. Likely already familiar with cultivation fiction through manga, manhwa, or games. iOS users on mid-range or better hardware (iPhone 12+).

**Secondary:** Players from the African diaspora or those interested in African mythology and cosmology who are looking for games that represent their cultural heritage authentically. This audience has serious spending potential and is chronically underserved.

**Context of use:** Short sessions (3–10 minutes) checking in on progress, making cultivation decisions, collecting Àṣẹ. Also opened passively during commutes or breaks. Game must feel rewarding at both 2-minute and 20-minute session lengths.

---

## 4. MVP Feature Set

*Ruthlessly scoped to 5 features. Everything else is Post-MVP.*

1. **As a player, I want Àṣẹ to accumulate passively while I'm away** so that returning to the game always feels rewarding and my progress continues even when I'm not playing.

2. **As a player, I want to advance my cultivator through 6 cultivation stages across 2 tiers** so that I feel long-term progression and each stage feels like a meaningful power milestone.

3. **As a player, I want to choose one of 3 Paths (Ane, Sango, Osun) for my cultivator** so that my playstyle has an identity and my cultivation style feels personal.

4. **As a player, I want to face a Tribulation event at the peak of my cultivation (the highest tier)** so that each cultivator's arc has a narrative climax and the prestige loop feels earned rather than automatic.

5. **As a player, I want completed cultivators (ascended or fallen) to join my Ancestral Council up to 5 members** so that each generation is visibly stronger than the last and failure never feels like a dead end.

---

## 5. Non-Goals (What We Are NOT Building)

- Android build (iOS-only at launch)
- Sacred Trials active events (post-MVP)
- Stages 7–12 / Tiers 3–4 (post-MVP content)
- Paths 4 and 5 (Ogun, Ifa) and the hidden 6th Path
- Rewarded ads integration
- IAP cosmetics store
- PvP or any multiplayer feature
- Social sharing or leaderboards
- Lineage synergy combos (require 6+ ancestors, out of MVP scope)
- Custom cultivator naming
- Animated character art (static portrait per stage is sufficient for MVP)
- Tutorial / onboarding flow (bare minimum tap-to-start)
- Push notifications
- Localization (English only)

---

## 6. Business Rules

- A cultivator faces exactly one capstone Tribulation per generation. It cannot trigger unless they have reached the peak stage of the highest tier (Stage 6 / index 5 in MVP) AND accumulated the Tribulation Àṣẹ threshold. Crossing the Tier 0→1 boundary mid-climb is an ordinary stage advance, not a Tribulation.
- Àṣẹ generation rate = base rate × stage production multiplier × path multiplier × (1 + path.councilBonusModifier × (lineage.permanentAseBonus + Σ active council contributions)). Each active ancestor contributes W × bonusMultiplier (W = ANCESTOR_BASE_BONUS, default 0.25); all path modifiers are 1.0 until a Path is chosen; lineage.permanentAseBonus is an additive accumulator (default 0.0). The councilBonusModifier (Osun ×2) wraps the permanent and active terms together so retirement stays Àṣẹ-neutral on every path. Full numbers: docs/GAMEPLAY.md §2.
- Offline progress is calculated mathematically on app load AND on resume-from-background: `aseEarned = asePerSecond × path.offlineRateModifier × max(0, min(elapsedSeconds, 28800))`. Cap is 8 hours and applies to TIME — paths may modify the offline rate (Ane ×1.5) but never the cap; the max(0,…) clamp plus a first-launch guard (lastSaveTimestamp == 0 ⇒ no gain) prevent negative gains and a first-launch windfall. Never simulated via time scaling.
- A failed Tribulation still produces an ancestor — the player always moves forward. The difference is `bonusMultiplier`: full ascension produces 1.0x, a fall produces 0.4x.
- The Ancestral Council holds a maximum of 5 ancestors in MVP. When a 6th would join, the oldest (by completedTimestamp) is retired: their contribution (W × bonusMultiplier) is added into lineage.permanentAseBonus and they leave the visible council. Retirement is Àṣẹ-neutral — the generation rate is identical before and after.
- Cloud save syncs on every Tribulation completion and every app close/suspend. Not on every tick.
- A player cannot regress in cultivation stage. Once a stage is reached, it persists even if Àṣẹ is spent. Within a generation, Àṣẹ is cumulative and never spent on advancement; banked Àṣẹ is never zeroed on stage advance (offline forgiveness property).
- Stage advancement is player-triggered (a manual Advance tap when the threshold is met), one stage per tap.
- The Path is chosen at the Tier 1 gate (advancing out of Stage 3) and is re-chosen every generation — Tribulation resets it. Path hooks modify generation behavior only (online rate / offline rate / council scaling); Tribulation odds are identical on all paths.
- Àṣẹ values are stored as BigNumber (mantissa + exponent) — never as raw float or double, to prevent precision loss at large values.

---

## 7. Technical Constraints

- **Platform:** iOS 15.0 minimum (covers iPhone 12+, A14 Bionic chip)
- **Engine:** Unity 6 (6000.4.10f1, tech release)
- **Language:** C#
- **iOS Export:** Unity Cloud Build (developer is on CachyOS Linux, no Mac available)
- **Must work offline:** Yes — offline progress calculation is a core feature
- **External services:**
  - Game Center (Apple) — cloud save via iCloud, authentication
  - Unity Cloud Build — iOS compilation pipeline
- **Paid services required:**
  - Apple Developer Program ($99/year) — required for App Store submission and Game Center
- **No backend server** — all game logic is client-side. Save data lives locally + Game Center iCloud only.
- **No Unity Pro required** — Unity Personal is sufficient (under $200K revenue threshold)

---

## 8. Success Metrics

- **Output metric:** A player can complete one full generational loop — cultivate from Stage 1 to Stage 6, face a Tribulation, produce an ancestor, and begin a second generation with a visible bonus — without encountering a crash, save corruption, or progression blocker.
- **Input metric 1:** Offline progress is correctly calculated and displayed on every app open (no silent failures or zero-gain returns after 4+ hours away).
- **Input metric 2:** All 3 Paths produce meaningfully different Àṣẹ generation behavior detectable within a 10-minute session.
- **Input metric 3:** Save data survives app force-close and restore without loss, both local and via Game Center sync.

---

## 9. Post-MVP Backlog

*Confirmed for later. Not in scope now.*

- Tiers 3–4 (Stages 7–12, Ancestor and Divine stages)
- Paths 4 and 5 (Ogun, Ifa) + hidden Path 6 (The Complete Way)
- Sacred Trials — Masquerade Trial, Ancestor Request, Spirit Market, The Great Test
- Lineage synergy combos (3 Ane ancestors = Roots of the Earth, etc.)
- Rewarded ads (double offline Àṣẹ, accelerate Tribulation prep)
- Ancestral Covenant monthly pass
- Cosmetic IAP (alternate cultivator portraits, shrine decorations)
- Android port
- Cultivator naming
- Push notifications (offline progress ready, Tribulation available)
- Dynamic audio system (Path-specific music themes)
- Social sharing (Ancestral Council screenshot)

---

## 10. Open Questions

- ~~What is the Àṣẹ threshold for each Tribulation?~~ **RESOLVED (2026-06-12, research + simulation):** one capstone Tribulation per generation at Stage 6, threshold **25,000,000 Àṣẹ** (the 1M placeholder left ~2 minutes of capstone at stage-6 rates). Full verified stage table in docs/GAMEPLAY.md §2.2.
- ~~What is the base Àṣẹ per second at Stage 1?~~ **RESOLVED (2026-06-12):** 1.0/s confirmed by simulation against session targets (first Advance inside the first 2-minute session; path gate at 9m40s; gen 1 = one evening + one overnight). docs/GAMEPLAY.md §2.4.
- ~~What triggers a "fall" vs an "ascend" in Tribulation?~~ **RESOLVED (2026-06-09):** flat 60% ascend (a weighted coin), BASE_ASCEND in TribulationConfig. Still "weighted random, not pure threshold." Speed-weighting deferred post-MVP.
- Does the Game Center authentication gate gameplay, or is it optional with a fallback to local-only save? (Recommendation: optional — never gate gameplay behind auth)
- What happens if the player has no Apple ID / Game Center disabled? (Answer: graceful fallback to local save only, no blocking prompt)
