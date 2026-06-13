# Build Plan: Ori Ascendant MVP
**Version:** 1.0
**Last Updated:** 2026-06-12
**Supersedes:** TECH_DESIGN.md §9 (Development Phases)
**Design truth:** docs/GAMEPLAY.md · **Architecture truth:** docs/TECH_DESIGN.md

## Status
- ✅ Done: project scaffold (URP 2D, 6000.4.10f1), Newtonsoft.Json in manifest, `BigNumber.cs` + EditMode tests, asmdefs (`OriAscendant`, `OriAscendant.Tests.EditMode`).
- ✅ **Play-mode + balance verification done (2026-06-12): EditMode 173/173 + PlayMode 2/2.** Added PacingContractTests (the GAMEPLAY §2.4 timeline as executable assertions over the real config assets) and RuntimeLoopPlayTests (headless PlayMode: tick accrues over real wall-clock; the built Main scene boots with every system wired through ServiceLocator and Àṣẹ ticking). **This caught a latent-since-Phase-A bug:** GameplayConfig was loaded but never SetDirty'd in SceneBuilder, so SaveScene unloaded it and every `_config`/`_gameplayConfig` reference serialized as fake-null — the game NRE'd on boot. Fixed via BuildGameplayConfig() + SetDirty (pins it through save, matching the other configs); the scene-boot PlayMode test is now the permanent guard. EditMode-only testing could never have found this (it injects configs and never loads the scene).
- ✅ **Phase D code side complete (2026-06-12, Gate D: 167/167 EditMode tests green).** Cloud architecture: ICloudSaveProvider seam + NullCloudSaveProvider + SaveConflictResolver (higher generationCount → higher aseAmount → ties keep local, truth-tabled) + CloudSyncCoordinator (auth/load/reconcile/push, never throws, always falls to local — 9 tests incl. auth-throws-swallowed) + thin CloudSaveManager (async, fire-and-forget reconcile, opportunistic push; OFF-LIMITS rules honored); GameKitCloudSaveProvider fully behind `#if UNITY_IOS && !UNITY_EDITOR` (compiles to nothing on Linux, no asmdef Apple ref yet) + link.xml. Cloud-sync-on-tribulation hook wired + integration-tested. AudioManager (crossfade + track-selector pure cores, null-safe clip slots, haptics behind IHapticFeedback) + AudioPrefs (PlayerPrefs, not SaveData). Settings + About/Glossary (heritage statement + 15-term diacritic glossary, §7 cultural requirement, content-tested) on the header gear. Welcome-Back count-up + tap-to-skip. BuildConfigurator (product/bundle/iOS 15/portrait — validated). **SaveData schema UNCHANGED. Codebase is MVP feature-complete; all remaining work is user/device-gated (checklist below).**
- ✅ **Phase C complete (2026-06-12, Gate C: 133/133 EditMode tests green).** Full generational loop end-to-end in tests: TribulationSystem (roll-once-persist-first via injectable RNG; complete GAMEPLAY §4.4 reset asserted field-by-field; locked OnTribulationComplete signature, notification-only — council mutation moved to a synchronous InductAncestor API inside the atomic resolve; TECH_DESIGN §4 amended); AncestralCouncilSystem (ActiveCouncilSum live in the rate; oldest-by-timestamp retirement, Àṣẹ-neutral under councilBonusModifier 1.0 AND 2.0); Ìrékọjá ceremony state machine (computed confirm sheet + disclosed 60/40 panel + 0.8s hold-to-begin; transition → 3 storm waves → silence → reveal, identical until reveal; fall = "THE LINE ENDURES" + real lineage delta; ancestor card with retirement beat; gen summary → N+1 rate preview; first-time-per-outcome skippability via seenFlags). Council strip live (path-motif tints, fallen dimmed) + Lineage Shrine screen; CTA arms on eligibility; ambient vignette stub; all beat timings in TribulationConfig (validated). 2-generation integration test passes incl. re-announcement, Osun mid-walk factor, and fall-is-never-a-dead-end. Cloud-sync-on-tribulation hook noted in Resolve() for Phase D.
- ✅ **Phase B complete (2026-06-12, Gate B: 119/119 EditMode tests green).** All 11 config assets built with the locked GAMEPLAY §2 values (6 stages incl. diacritics, 3 paths, TribulationConfig 60%/25M, CouncilConfig W=0.25/max 5) and guarded by ConfigAssetValidationTests; CultivationSystem (manual advance, path gate at the Tier 0 peak where choosing IS the advance, once-per-generation eligibility); AseGen rate recompute reads stage/path live (Sango ×2 on selection, Osun joint wrap — both asserted); offline modifier wired to the live path; UI: progress bar (one-decimal tribulation % at stage 6), Advance CTA (tribulation morph placeholder), PathScreen modal with runtime-bound cards, tap-to-channel + floating "+N" + one-time hint (seenFlags), Welcome-Back Ane itemized line. BigNumber.ToDouble() added WITH 14 accompanying tests (off-limits rule honored). **Bonus: Yoruba glyph coverage automated** — builder audits game copy against the default font (was MISSING 9: all subdots + tone marks), downloads resolved via committed Assets/Fonts/NotoSans-Regular.ttf → TMP fallback asset generated + registered headlessly; audit now PASS. Phase D portrait art still pending; play-mode milestone walkthrough pending next Editor session.
- ✅ Phase A complete (2026-06-12, Gate A: 69/69 EditMode tests green). Code: ServiceLocator, TickAccumulator, SaveData v1 (locked shape), SaveSerializer, SaveManager (atomic writes, pause/autosave triggers), OfflineProgressCalculator (pure, clamped, fresh-install guard), RateCalculator (full §2.1 formula incl. Osun wrap), AseGenerationSystem (accumulator tick, sole rate writer, channel-tap API), GameManager (orchestrated load → Begin → offline → recalc), MainScreenView/WelcomeBackModal/TitleScreen. Scene: `Main.unity` generated by `Assets/Editor/SceneBuilder.cs` (replayable; menu "Ori Ascendant/Build Main Scene" or `-executeMethod OriAscendant.EditorTools.SceneBuilder.BuildAll`); `Assets/Configs/GameplayConfig.asset`; TMP Essentials extracted; build settings → Main.unity. Phase A user-action items (Apple/UCB/font test) tracked below — still open.
- Headless test gate (run after every phase chunk; exit 0 = pass):
  `/home/baba/Unity/Hub/Editor/6000.4.10f1/Editor/Unity -runTests -batchmode -nographics -projectPath "/home/baba/ASE/Ori Ascendant" -testPlatform EditMode -testResults Logs/editmode-results.xml -logFile Logs/unity-editmode.log`

## Efficiency principles (what changed vs the old §9 plan)
1. **Config-first, final values:** all ScriptableObject *types and assets* are authored in one batch at the start of Phase B with the final researched numbers (GAMEPLAY §2) — no placeholder churn, no re-balancing pass mid-build. The Phase-4 "balance pass" becomes a verification playtest, not a tuning session.
2. **UI shell once:** the full MainScreen layout (GAMEPLAY §3.2, all 7 zones) is built once in Phase A with later-phase elements present but inactive. Later phases only activate and wire — no re-layout.
3. **Logic/view split for testability:** every system's decision logic is a plain-C# testable core (no MonoBehaviour) with a thin MonoBehaviour host. EditMode tests cover the cores headlessly on this Linux box; PlayMode/device covers integration.
4. **Risk front-loading:** the three external-dependency risks (Apple Developer/UCB setup, Apple Unity plugin tarballs, Yoruba diacritics font) start in Phase A as a parallel track — they have multi-day external latency and zero code dependencies.
5. **Crash-safety as design:** tribulation = roll once → persist full next-gen state → then ceremony. The ceremony is replayable theater; tests assert state, not animation.

---

## Phase A — Core loop closure (≈ old Phase 1 remainder)
Goal: number ticks up, saves/loads, offline gains shown. **Milestone unchanged:** open app → ticks → close → reopen → offline gains shown.

Code (build order):
1. `Core/ServiceLocator.cs` — register in Awake, no state.
2. `Data/GameplayConfig.cs` (SO: baseRate 1.0, tapChannelSeconds 5.0, welcomeBackMinSeconds 60, autosaveIntervalSeconds 30) + asset.
3. `Save/SaveData.cs` — **final v1 shape from GAMEPLAY §6** (schemaVersion, cached rate, generationStartTimestamp, seenFlags, council, lineage). Off-limits rules apply from this commit on.
4. `Save/SaveManager.cs` — local JSON at persistentDataPath; triggers: OnApplicationPause(true), 30s autosave, progression events. Never in-tick.
5. `Systems/OfflineProgressCalculator.cs` — pure math core: `(lastSave, now, cachedRate, offlineRateModifier) → earned`; clamp [0, 28800]; fresh-install guard; fires OnOfflineProgressApplied.
6. `Systems/AseGenerationSystem.cs` — Update() accumulator tick (GAMEPLAY §5.1); `RecalculateRate()` sole writer of cached rate (full formula incl. councilBonusModifier wrap — path/council read as neutral until those systems exist); channel-tap grant API.
7. `Core/GameManager.cs` — lifecycle: load → offline calc → play; pause(true) save; pause(false) offline calc (≥60s → modal, else silent).
8. Main scene: full UI shell (§3.2 zones, inactive extras), counter + rate + Welcome Back modal v1 (time/amount/collect — Ane line added in Phase B), title screen.

EditMode tests (gate A): SaveData JSON round-trip (incl. BigNumber fields at e10/e50/e100/e200) · offline calc (zero elapsed, negative/future timestamp, exact cap, fresh install, modifier 1.5/0.5) · RecalculateRate composition · accumulator math.

Parallel track (start day 1, no code deps):
- Apple Developer Program enrollment; App ID with **Game Center + iCloud (Documents) + container**; distribution cert + provisioning profile (regenerate AFTER enabling iCloud); upload .p12 + profile to UCB.
- UCB build target: pin editor 6000.4.10f1, Builder OS **macOS Sequoia**, Xcode 26 pinned (iOS-26-SDK mandate in force since 2026-04-28).
- Download HalfbrickStudios prebuilt `com.apple.unityplugin` .tgz (Apple.Core 3.1.8 + Apple.GameKit 3.0.2); commit; **don't reference in manifest until Phase D**.
- TMP font test: render "Akẹ́kọ̀ọ́ / Aláàṣẹ / Ọ̀run" — Noto Sans fallback; decide full-diacritics vs dotted-only now (GAMEPLAY §7.9).

## Phase B — Progression & paths (≈ old Phase 2)
Goal: 6 stages + path choice, paths feel different. **Milestone:** stage 1→6 with visibly different behavior per path; path gate < 10 min.

1. `Data/CultivationStageConfig.cs` + **6 assets, final values** (GAMEPLAY §2.2 — names, diacritics, descriptions, multipliers, thresholds, tier, portrait slots); `Data/PathConfig.cs` (+offlineRateModifier, +councilBonusModifier) + **3 assets** (§2.3); `Data/TribulationConfig.cs` + `Data/CouncilConfig.cs` + assets (§2.5) — authored now, consumed in Phase C.
2. `Systems/CultivationSystem.cs` — advance check (cumulative, never spends), manual Advance, multi-advance per §4.3, eligibility (`stage==5 && ase>=trib.threshold`), OnStageAdvanced/OnPathChosen/OnTribulationAvailable.
3. `UI/Screens/PathScreen.cs` — advance-gated modal flow (§3.3); path badge + identity line on HUD; portrait swap on advance.
4. Tap-to-channel + one-time hint (seenFlags bit 0) + floating "+N".
5. Welcome Back: add Ane itemized line; offlineRateModifier wired.

EditMode tests (gate B): threshold table drives advancement exactly · multi-advance chaining · rate recompute on advance/path-choice (Sango 2×, Osun wrap) · channel grant = rate × 5 · path-less neutrality (currentPath −1 ⇒ all modifiers 1.0).

## Phase C — Tribulation & council (≈ old Phase 3)
Goal: full generational loop. **Milestone:** two full generations; gen 2 visibly stronger; kill the app mid-ceremony and the outcome survives.

1. `Systems/TribulationSystem.cs` — roll-once-persist-first (§3.5); full generation reset scope (§4.4) written atomically.
2. `Systems/AncestralCouncilSystem.cs` — W from CouncilConfig; retirement bake (Àṣẹ-neutral, incl. under Osun wrap).
3. `UI/Screens/TribulationScreen.cs` — buildup tiers, confirm sheet (computed copy + odds panel), ceremony beats per §3.5 timing table, skippability via seenFlags, gen summary → N+1 preview.
4. `UI/Screens/CouncilScreen.cs` + council strip wiring; ancestor card derivation (§5.4).

EditMode tests (gate C): resolution persistence (state complete before ceremony flag) · 60/40 via injected RNG · ancestor data correctness (1.0/0.4) · retirement neutrality: rate identical before/after at council=5, **for councilBonusModifier 1.0 AND 2.0** · generation reset scope (every field of §4.4) · Osun bonus = 2× displayed and applied.

## Phase D — Cloud, audio, polish, submission (≈ old Phase 4)
Goal: TestFlight build on a physical iPhone.

1. Reference Apple .tgz in manifest; `ICloudSaveProvider` + `NullCloudSaveProvider` (editor/standalone) + GameKit provider under `#if UNITY_IOS && !UNITY_EDITOR`; `link.xml` preserving Apple.GameKit (GKSavedGame stripping crash); auth async at launch, never awaited before gameplay; conflict rule: higher generationCount → higher aseAmount; FetchSavedGames retry-once-on-empty.
2. `Audio/AudioManager.cs` — BGM loop, path themes crossfade, ceremony stingers, SFX/haptics per GAMEPLAY tables.
3. Polish: particles (motes, collect burst), haptics, app icon, launch screen, About/Glossary content.
4. Verification playtest against GAMEPLAY §2.4 expected times (not a tuning pass); App Store Connect listing; TestFlight.

Device test checklist: Game Center auth on hardware (simulator GC unreliable) · cloud round-trip + conflict · force-close mid-ceremony · overnight offline on device · diacritics rendering on retina.

## Risk register deltas (vs TECH_DESIGN §8)
| New/changed risk | Mitigation |
|---|---|
| Apple plugin .tgz can't be rebuilt on Linux | Prebuilt Halfbrick tarballs committed; verify with one early TestFlight build |
| GKSavedGame stripped by IL2CPP | link.xml from day one of Phase D |
| Xcode 26 / Sequoia builder mandate | Pinned in UCB at Phase A |
| Yoruba diacritics render failure | Phase A font test; dotted-only fallback, never ASCII |
| OnApplicationQuit unreliable on iOS | pause(true) primary + autosave web (already in design) |
