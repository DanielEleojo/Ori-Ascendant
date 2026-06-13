# Technical Design: Ori Ascendant
**Version:** 1.2
**Last Updated:** 2026-06-12

> **Companion docs:** gameplay detail + final balance numbers live in **docs/GAMEPLAY.md** (wins over prose here on game-design questions); the execution plan lives in **docs/BUILD_PLAN.md** (supersedes §9).

---

## 1. Stack Decision

| Layer | Choice | Why |
|-------|--------|-----|
| Engine | Unity 6 (6000.4.10f1, tech release) | Mobile monetization ecosystem (AdMob, Unity IAP), Unity Cloud Build for iOS from Linux, 70%+ of top mobile games, mature 2D pipeline |
| Language | C# | Unity native, strong typing catches idle game math bugs early, async/await for cloud save |
| Platform | iOS 15+ | Launch scope, Game Center is cleaner than Google Play Games Services, iPhone 12 (A14) as minimum covers mid-range target |
| Data persistence | JSON (local) + iCloud via Game Center | No backend server cost, Game Center handles auth + sync, graceful offline fallback |
| Numbers | Custom BigNumber struct | Standard double loses precision at large idle values; mantissa+exponent approach handles e300+ cleanly |
| JSON serialization | Newtonsoft.Json (Unity package) | Better than Unity's built-in JsonUtility — supports nested classes, dictionaries, proper null handling |
| iOS export | Unity Cloud Build | Developer is on Linux; Cloud Build compiles on Apple hardware remotely |

**Alternatives considered and rejected:**
- Godot 4: Daniel already knows it, but mobile monetization plugins (IAP, AdMob) are less mature than Unity's; would save 0 time on this project type
- Flutter: Great for UI-heavy apps, but not a game engine; fighting it for game loop timing would cost more than switching from familiar territory
- Firebase Cloud Firestore: Adds backend cost and complexity for solo dev; Game Center iCloud is free and sufficient for save sync
- Unity Gaming Services Cloud Save: Requires Unity account tiers, more setup than Game Center for iOS-only launch

---

## 2. Architecture Overview

Ori Ascendant uses a service-oriented architecture with a ServiceLocator as the central registry. All major systems (Àṣẹ generation, cultivation, ancestral council, save) register themselves at startup and communicate through the locator rather than direct references. Game config lives in ScriptableObjects (read-only, never modified at runtime). Player state lives in a serializable SaveData class that is the single source of truth for persistence. UI systems observe state changes through C# events — they never write to game state directly.

```
[App Start]
    ↓
ServiceLocator.RegisterAll()
    ↓
SaveManager.Load() → OfflineProgressCalculator.Apply()
    ↓
[Main Scene]
    ├── AseGenerationSystem (ticks every 1s)
    ├── CultivationSystem (event-driven)
    ├── AncestralCouncilSystem (event-driven)
    ├── TribulationSystem (player-triggered)
    ├── SaveManager (local JSON)
    └── CloudSaveManager (Game Center, optional)
         ↓ (all systems write to)
    SaveData (single source of truth)
         ↓ (UI reads from via events)
    UIManager → individual Screen controllers
```

---

## 3. Project Structure

```
/Assets
  ├── Scripts/
  │   ├── Core/
  │   │   ├── ServiceLocator.cs       — central service registry
  │   │   ├── GameManager.cs          — app lifecycle, scene management
  │   │   └── BigNumber.cs            — custom large number struct
  │   ├── Systems/
  │   │   ├── AseGenerationSystem.cs  — passive production ticks
  │   │   ├── CultivationSystem.cs    — stage/path/tier management
  │   │   ├── TribulationSystem.cs    — prestige event resolution
  │   │   ├── AncestralCouncilSystem.cs — council management + bonuses
  │   │   └── OfflineProgressCalculator.cs — on-load time delta math
  │   ├── Save/
  │   │   ├── SaveData.cs             — serializable save data model
  │   │   ├── SaveManager.cs          — local JSON read/write
  │   │   └── CloudSaveManager.cs     — Game Center iCloud wrapper
  │   ├── Data/
  │   │   ├── CultivationStageConfig.cs  — ScriptableObject
  │   │   ├── PathConfig.cs              — ScriptableObject
  │   │   └── AncestorTemplate.cs        — ScriptableObject
  │   ├── UI/
  │   │   ├── UIManager.cs            — screen routing
  │   │   └── Screens/
  │   │       ├── MainScreen.cs       — primary idle view
  │   │       ├── CouncilScreen.cs    — ancestral council view
  │   │       ├── PathScreen.cs       — path selection
  │   │       └── TribulationScreen.cs — tribulation event UI
  │   └── Audio/
  │       └── AudioManager.cs
  ├── Resources/
  │   ├── StageConfigs/               — 6x CultivationStageConfig assets
  │   ├── PathConfigs/                — 3x PathConfig assets
  │   └── AncestorTemplates/          — ancestor bonus templates
  ├── Scenes/
  │   ├── Loading.unity
  │   └── Main.unity
  ├── Art/
  │   ├── UI/
  │   ├── Portraits/                  — 1 portrait per stage (6 for MVP)
  │   └── Backgrounds/
  └── Audio/
      ├── Music/
      └── SFX/
```

> **Note:** `AncestorTemplate.cs` and `Resources/AncestorTemplates/` (shown in the tree above) are **POST-MVP** — cut from MVP scope (2026-06-09). MVP adds two small config ScriptableObjects instead: `TribulationConfig` (baseAscendChance) and `CouncilConfig` (ancestorBaseBonus / W).

---

## 4. Core Systems

**ServiceLocator**
Central registry. Systems call `ServiceLocator.Register<T>(instance)` on Awake, and `ServiceLocator.Get<T>()` to access dependencies. Avoids FindObjectOfType calls and reduces coupling. Does NOT own any game state.

**GameManager**
Owns app lifecycle: start, pause, resume, quit. On resume: triggers OfflineProgressCalculator. On quit: triggers SaveManager.Save(). Does NOT own game state — only orchestrates other systems.

**BigNumber** (struct, not class — stack allocated)
Stores `double mantissa` (1.0–999.999) and `int exponent`. Implements +, -, ×, /, comparison, and `ToString("K/M/B/T/e")` formatting. Unit-tested independently. This must exist before any other system — every Àṣẹ value in the codebase uses BigNumber, never raw double.

**AseGenerationSystem**
Owns: production rate calculation, tick processing (1s logical tick via an Update() frame-accumulator — `acc += Time.unscaledDeltaTime; while (acc >= 1f) { acc -= 1f; Tick(); }` — remainder carries so long-run drift is zero; supersedes the earlier InvokeRepeating decision, see §6), and the tap-to-channel grant (`aseAmount += asePerSecond × GameplayConfig.tapChannelSeconds`). Reads: current stage config, active path config, council bonus total from AncestralCouncilSystem. Writes: SaveData.aseAmount, and is the SOLE writer of the cached asePerSecond (asePerSecondMantissa/Exponent), recomputed via RecalculateRate() on stage advance, path chosen, Tribulation complete, and any council change. Fires: `OnAseChanged(BigNumber newAmount)` event after each tick. Does NOT own: UI updates, save triggers, path state.

**CultivationSystem**
Owns: current stage index, current path, tier state, Tribulation eligibility check. Reads: CultivationStageConfig assets, current Àṣẹ from SaveData. Writes: SaveData.currentStage, SaveData.currentPath. Fires: `OnStageAdvanced`, `OnPathChosen`, `OnTribulationAvailable`. Does NOT own: Àṣẹ amounts, the Tribulation resolution itself (that belongs to TribulationSystem).

**TribulationSystem**
Owns: Tribulation resolution logic (ascend vs fall determination), cultivator-to-ancestor conversion. Resolution is a flat weighted coin: BASE_ASCEND = 60% ascend (constant in TribulationConfig); speed-weighting is deferred post-MVP. There is one capstone Tribulation per generation, at the highest tier peak (Stage 6 / index 5). Writes: new AncestorData to SaveData.council, resets cultivator state to Stage 1 with lineage bonus applied. Fires: `OnTribulationComplete(bool didAscend, AncestorData newAncestor)`. Does NOT own: council bonus calculation (that's AncestralCouncilSystem's job after it receives the event).

**AncestralCouncilSystem**
Owns: ancestor list management, total council bonus calculation (ActiveCouncilSum = Σ W × bonusMultiplier, consumed by AseGenerationSystem), council-full retirement logic. Mutation happens via the synchronous `InductAncestor()` API called INSIDE TribulationSystem's atomic resolve (amended 2026-06-12: an event-driven mutation would run after the save was written, persisting an over-full council and breaking persist-first crash-safety — `OnTribulationComplete` is notification-only for UI). When a 6th ancestor would be added, retires the oldest (by completedTimestamp) FIRST — adds their contribution (W × bonusMultiplier) into `SaveData.lineage.permanentAseBonus` (an additive accumulator, default 0.0) before removing them. Retirement is Àṣẹ-neutral, verified under councilBonusModifier 1.0 and 2.0. Does NOT own: Tribulation resolution, production ticking.

**OfflineProgressCalculator**
Owns: offline Àṣẹ calculation on app load. Algorithm:
```csharp
long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
if (saveData.lastSaveTimestamp == 0) {          // fresh install — no prior session
    saveData.lastSaveTimestamp = now;            // no offline gain on first launch
} else {
    long elapsed = Math.Max(0, Math.Min(now - saveData.lastSaveTimestamp, 28800)); // clamp [0, 8h]
    BigNumber earned = saveData.asePerSecond * pathOfflineRateModifier * elapsed; // cached rate × path offline modifier (1.0 if no path); never recomputes
    saveData.aseAmount += earned;
    saveData.lastSaveTimestamp = now;
}
```
Never uses Time.timeScale. Never runs coroutines. Pure math. Fires: `OnOfflineProgressApplied(BigNumber earned, long seconds)` so UI can show a "Welcome back" collect screen. Runs on cold launch AND on resume-from-background (`OnApplicationPause(false)`) — iOS apps resume far more often than they cold-launch; elapsed ≥ 60s shows the collect modal, below that the gain is credited silently.

**SaveManager**
Owns: local JSON file read/write at `Application.persistentDataPath/save.json`. Serializes SaveData via Newtonsoft.Json. Primary trigger: `OnApplicationPause(true)` — `OnApplicationQuit` is documented-unreliable on iOS (apps suspend, and the OS can kill a suspended app with no callback). Belt-and-braces: autosave every 30s (GameplayConfig) + every progression event (stage advance, path choice, Tribulation resolve, retirement). Does NOT save on every tick — battery drain. Local JSON writes synchronously in the pause handler; cloud pushes never do (iOS suspension time budget).

**CloudSaveManager**
Owns: Game Center authentication, iCloud save upload/download via **Apple.GameKit's GKSavedGame** (Apple's official Unity plugins; prebuilt HalfbrickStudios .tgz committed to the repo since `build.py` requires a Mac — see BUILD_PLAN Phase A/D). Structured as an `ICloudSaveProvider` interface: GameKit-backed provider only under `#if UNITY_IOS && !UNITY_EDITOR`; `NullCloudSaveProvider` (always "auth failed" → local) in editor/standalone, which keeps Linux Play-mode fully functional and enforces the fallback rule structurally. Auth fires async at launch in parallel with local load — gameplay starts from local save immediately, never awaits auth. Conflict rule (cloud vs local, and ResolveConflictingSavedGames): **higher `lineage.generationCount` wins, then higher `aseAmount`** — deterministic and monotonic (replaces the older newest-timestamp rule, which clock skew could game). Gotchas: retry FetchSavedGames once on empty result (fresh-install quirk); `link.xml` must preserve Apple.GameKit (IL2CPP stripping removes the GKSavedGame ctor). Does NOT own: save data format — always reads/writes through SaveManager.

**UIManager**
Owns: which screen is active (single screen stack). Screens are GameObjects toggled active/inactive — not loaded/unloaded as separate scenes (too much overhead for an idle game). Listens to system events to trigger state updates. Does NOT write to game state.

**AudioManager**
Owns: BGM playback (looped), SFX one-shots. Reads: player's path to select the correct musical theme. Daniel's custom audio assets slot in here. Cross-fade between BGM tracks on path change or Tribulation event.

---

## 5. Data Model

```csharp
// SaveData.cs — the single serializable save structure
[Serializable]
public class SaveData
{
    public int schemaVersion = 1;          // migration anchor — bump on any schema change
    public double aseMantissa = 1.0;       // BigNumber mantissa
    public int aseExponent = 0;            // BigNumber exponent
    public double asePerSecondMantissa;    // cached derived rate — AseGenerationSystem is sole writer
    public int asePerSecondExponent;
    public int currentStage = 0;           // 0–5 (MVP: 6 stages); display = currentStage + 1
    public int currentPath = -1;           // -1 = not chosen (path-less stages 0–2); 0=Ane, 1=Sango, 2=Osun
    public long lastSaveTimestamp;         // Unix seconds UTC
    public long generationStartTimestamp;  // Unix seconds UTC — for the generation-summary screen
    public int seenFlags = 0;              // bitmask: 1=channel hint, 2=ascend ceremony seen, 4=fall ceremony seen
    public List<AncestorData> council = new();
    public LineageData lineage = new();
}

[Serializable]
public class AncestorData
{
    public int peakStage;
    public int path;
    public bool didAscend;                 // true = full power, false = lesser
    public double bonusMultiplier;         // 1.0 if ascended, 0.4 if fallen
    public long completedTimestamp;
}

[Serializable]
public class LineageData
{
    public double permanentAseBonus = 0.0; // ADDITIVE accumulator of retired ancestors' contributions (W × bonusMultiplier)
    public int generationCount = 0;
}

// ScriptableObjects — config only, never modified at runtime
[CreateAssetMenu] public class CultivationStageConfig : ScriptableObject
{
    public string stageName;               // e.g. "Ọmọ Ane"
    public string stageDescription;
    public double aseThresholdMantissa;    // Àṣẹ needed to reach this stage
    public int aseThresholdExponent;
    public double productionMultiplier;    // stage bonus to Àṣẹ/s
    public Sprite portrait;
    public int tier;                       // 0 or 1 for MVP
}

[CreateAssetMenu] public class PathConfig : ScriptableObject
{
    public string pathName;                // e.g. "Path of Earth (Ane)"
    public string pathDescription;
    public double aseGenerationModifier;   // online path multiplier (Sango 2.0; others 1.0)
    public double offlineRateModifier = 1.0;   // read ONLY by OfflineProgressCalculator (Ane 1.5; Sango 0.5 = net offline ×1.0)
    public double councilBonusModifier = 1.0;  // wraps the WHOLE lineage term (permanent + active) — Osun 2.0; keeps retirement Àṣẹ-neutral
    public TribulationType tribulationType;    // presentation-only (art/copy/SFX) — never touches the 60/40 odds
    public AudioClip musicTheme;
}

[CreateAssetMenu] public class GameplayConfig : ScriptableObject
{
    public double baseRate = 1.0;            // Àṣẹ/s at stage 1, no modifiers
    public double tapChannelSeconds = 5.0;   // tap-to-channel grant = asePerSecond × this
    public int welcomeBackMinSeconds = 60;   // below this, offline gain credited silently
    public int autosaveIntervalSeconds = 30;
}

[CreateAssetMenu] public class TribulationConfig : ScriptableObject
{
    public double baseAscendChance = 0.60; // BASE_ASCEND — flat ascend probability (MVP)
}

[CreateAssetMenu] public class CouncilConfig : ScriptableObject
{
    public double ancestorBaseBonus = 0.25; // W — per-ancestor additive weight (× bonusMultiplier)
}
```

---

## 6. Key Architectural Decisions

**Decision:** Use BigNumber struct instead of double for all Àṣẹ values.
**Reason:** Idle games regularly reach values where double precision causes silent rounding errors that corrupt saves. At 1e15+, standard arithmetic operations produce incorrect results that the player can detect (number stops changing, negative values appear). Fixing this after launch requires a save migration.
**Rejected alternatives:** Raw double (precision fails at scale), decimal (not JSON-serializable cleanly, slower math), third-party BigInteger (overkill for display purposes — we don't need arbitrary precision, just mantissa+exponent).

**Decision:** Single Main scene, screens toggled active/inactive — not separate Unity scenes.
**Reason:** An idle game has no concept of "loading a new level." Scene transitions create unnecessary overhead and complicate state management. All UI is always in memory (it's a mobile game, the UI asset budget is tiny).
**Rejected alternatives:** Separate scenes per screen (adds loading time, complicates ServiceLocator lifetime), Addressables (overkill for this project size).

**Decision:** Game Center cloud save is optional with graceful fallback — never gates gameplay.
**Reason:** Game Center auth can fail silently, the player may not have an Apple ID, or they may be in airplane mode. Blocking on auth means the player can't play. The save data is small (~5KB JSON), so the risk of local-only save is low.
**Rejected alternatives:** Force auth on launch (hostile UX, will cause negative reviews), no cloud save at all (unacceptable data loss risk on device swap).

**Decision (amended 2026-06-12):** 1-second logical tick via an Update() frame-accumulator; UI labels refresh only when the formatted value changes.
**Reason:** InvokeRepeating uses scaled game time, drifts with no remainder carry, and keeps firing on disabled objects; naive 1s coroutines overshoot by up to a frame per cycle (~1–2s/min drift at 30fps). The accumulator (`acc += unscaledDeltaTime; while (acc >= 1f) …`) carries the remainder, so long-run drift is zero — and correctness never depends on the timer anyway, because Unix timestamps + the resume path are the source of truth (nothing runs while iOS suspends the app; the tick is presentation cadence).
**Rejected alternatives:** InvokeRepeating (drift, scaled time — original choice, superseded), WaitForSeconds coroutine (frame overshoot accumulates), Awaitable.WaitForSecondsAsync (pooled-instance and async-void traps; buys nothing for one repeating tick).

**Decision:** Tribulation ascend/fall is weighted random, not pure threshold. MVP uses a flat 60% (BASE_ASCEND in TribulationConfig).
**Reason:** A pure threshold (if Àṣẹ >= X, you always ascend) removes tension and feels mechanical. A weighted coin (60% ascend) keeps every Tribulation meaningful while ensuring the majority succeed — players cannot be punished by bad luck alone. Speed-weighting the odds was considered but deferred post-MVP: wall-clock "completion speed" punishes idle players, which is hostile in an idle game.
**Rejected alternatives:** Pure threshold (too mechanical, no tension), player skill minigame (scope risk, not in MVP), speed-weighted odds (idle-hostile; post-MVP).

**Decision:** One capstone Tribulation per generation, at the highest tier peak (Stage 6 / index 5) — not one per tier.
**Reason:** The §8 success metric defines a generation as Stage 1→6 with a single Tribulation, and the "Tribulation = produce ancestor + reset to Stage 1" mechanic only works with one. Crossing the Tier 0→1 boundary mid-climb is an ordinary stage advance. Forward-compatible: post-MVP tiers raise the capstone to Stage 12. No `currentTier` field — tier is derived from `CultivationStageConfig.tier`.
**Rejected alternatives:** Two Tribulations (one per tier peak) — contradicts the success metric and makes Stage 6 unreachable (a Stage-3 reset sends you back to Stage 1).

**Decision:** Council bonus is a single additive pool with Àṣẹ-neutral retirement.
**Reason:** Rate uses `(1 + lineage.permanentAseBonus + Σ active( W × bonusMultiplier ))`, W = 0.25. Retirement moves the oldest member's `W × bonusMultiplier` from the active sum into `permanentAseBonus` (the same `(1 + …)` bucket), so the rate is identical before and after — a full council never costs Àṣẹ/s. `permanentAseBonus` is therefore additive (default 0.0), not multiplicative.
**Rejected alternatives:** Multiplicative bake (`permanentAseBonus *= (1 + c)`) — NOT neutral except with a single member, so it silently changes the rate on every retirement.

---

## 7. Off-Limits (Never Touch Without Explicit Instruction)

- **`SaveData.cs` schema** — any field rename, type change, or removal breaks existing saves. If schema must change, implement a migration version field first.
- **`BigNumber.cs` arithmetic operators** — the math here is subtle. Changes must be accompanied by unit tests. Never change the normalization logic without testing edge cases (addition of very small + very large numbers, subtraction, serialization round-trip).
- **`OfflineProgressCalculator.cs`** — never introduce Time.timeScale or coroutine-based simulation here. The calculation must remain pure math.
- **`CloudSaveManager.cs` auth flow** — never add blocking auth calls. The async auth must always have a failure path that falls through to local save.
- **`/Resources/StageConfigs/`** — stage threshold values affect game balance directly. Changes here require playtesting validation, not just code review.

---

## 8. Known Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Game Center auth fails on simulator | High | Medium | Test on physical device early; simulator Game Center is unreliable |
| BigNumber serialization round-trip loses precision | Medium | High | Unit test serialize→deserialize for values at e10, e50, e100, e200 |
| Offline progress calculation incorrect on timezone change | Medium | Medium | Always use UTC Unix timestamps, never local time |
| Unity Cloud Build iOS signing fails | Medium | High | Set up Apple Developer account and provisioning profiles in week 1, not day 7 |
| Àṣẹ/s balance too slow/fast for session targets | High | Medium | Playtest day 3; balance pass before submission |
| App Store review rejection (content, metadata) | Low | High | Read App Store Review Guidelines for games before submitting; fill all metadata fields |
| Clock manipulation (player sets clock forward for offline gains) | Low | Low | Cap offline gain at 8 hours; acceptable loss for MVP |
| Yoruba diacritics (subdot + tone marks) fail to render in TMP | Medium | Medium | Week-1 font test (Noto Sans fallback); degrade to dotted-only, never bare ASCII |
| IL2CPP stripping removes GKSavedGame ctor | Medium | High | link.xml preserving Apple.GameKit before first cloud test |
| Apple plugin frameworks can't be rebuilt on Linux | Medium | Medium | Prebuilt HalfbrickStudios .tgz committed; verify on an early TestFlight build |
| Xcode 26 / iOS 26 SDK mandate (since 2026-04-28) | High | High | UCB builder pinned to macOS Sequoia + Xcode 26 from day one |

---

## 9. Development Phases

> **SUPERSEDED (2026-06-12):** the live execution plan is **docs/BUILD_PLAN.md** (research-corrected: save hooks, cloud plugin strategy, tick mechanism, config-first sequencing, per-phase headless test gates). The original phases below are kept for historical context only.

**Phase 1 — Core Loop (Days 1–2)**
Goal: Àṣẹ generating on screen, saving and loading correctly.
- Unity 6 project (6000.4.10f1) created; folder structure from §3
- `BigNumber.cs` + unit tests (write tests first)
- `SaveData.cs`, `SaveManager.cs` (local JSON only)
- `AseGenerationSystem.cs` (1s tick, hardcoded base rate)
- `OfflineProgressCalculator.cs`
- Main scene: single screen with Àṣẹ counter UI
- Milestone: Open app → number ticks up → close app → reopen → offline gains shown

**Phase 2 — Cultivation Stages & Paths (Days 3–4)**
Goal: Player can progress through 6 stages and choose a path.
- 6x `CultivationStageConfig` assets (placeholder thresholds)
- 3x `PathConfig` assets (Ane, Sango, Osun)
- `CultivationSystem.cs` — stage advancement, Àṣẹ threshold check
- `PathScreen.cs` — path selection UI (triggered at Stage 3)
- Stage portrait display updates on advancement
- Path multiplier applies to Àṣẹ/s after selection
- Milestone: Progress from Stage 1 to Stage 6 with visibly different rates per path

**Phase 3 — Tribulation & Ancestral Council (Days 5–6)**
Goal: Full generational prestige loop working end-to-end.
- `TribulationSystem.cs` — weighted random resolution
- `TribulationScreen.cs` — event UI, outcome reveal
- `AncestorData`, `AncestralCouncilSystem.cs`
- `CouncilScreen.cs` — display ancestors + bonuses
- Second generation starts with council bonus applied
- Council-full retirement logic
- Milestone: Complete 2 full generations; second generation's Àṣẹ/s is visibly higher than first

**Phase 4 — Polish, Cloud Save & Submission Prep (Day 7)**
Goal: Submission-ready build.
- `CloudSaveManager.cs` — Game Center auth + iCloud sync
- `AudioManager.cs` — BGM loop, SFX, path music themes
- Offline progress "collect screen" UI on app open
- Balance pass on stage thresholds and path multipliers
- App icon, launch screen (required by App Store)
- App Store Connect: create app listing, screenshots, privacy policy URL, age rating
- Unity Cloud Build: iOS build pipeline configured, provisioning profile set
- TestFlight internal build for device testing
- Milestone: TestFlight build installs, runs, and saves correctly on physical iPhone
