# Lens: unity-impl

## Summary
Research confirms the plan's core stack is viable but needs four concrete adjustments. (1) Apple's official Unity plugins (Apple.Core + Apple.GameKit with GKSavedGame) are the right 2026 path for Game Center iCloud saves — there is no official CloudKit plugin and none is needed — but Apple ships source only and `build.py` requires macOS/Xcode/npm, which a Linux dev cannot run; the fix is committing prebuilt .tgz packages from the HalfbrickStudios fork (Apple.Core 3.1.8 + Apple.GameKit 3.0.2, built Mar 2025) and letting the plugin's Xcode post-processors run on Unity Cloud Build's Mac builders. (2) OnApplicationQuit is officially documented as unreliable on iOS — OnApplicationPause(true) must be the primary save hook, and offline progress must also be computed on resume-from-background, not only on cold launch, since iOS apps rarely cold-launch. (3) Unity Build Automation fully supports Mac-less iOS signing (.p12 + provisioning profile upload), but after April 28, 2026 App Store Connect requires iOS 26 SDK / Xcode 26, which on UCB is only available by setting the builder OS to macOS Sequoia. (4) For the 1s tick, the correct idle-game pattern is a frame-accumulator/Unix-timestamp source of truth (drift-proof, same code path as offline calc) rather than InvokeRepeating or a raw 1s coroutine; and UGUI + TextMeshPro is the lower-risk UI choice for a 7-day single-screen build versus UI Toolkit.

## Recommendations

### [high] Use Apple.GameKit's GKSavedGame for iCloud save — no CloudKit plugin exists or is needed
Apple's official unityplugins repo contains Apple.Core, Apple.GameKit, CoreHaptics, GameController, PHASE, Accessibility — no CloudKit plugin. Apple.GameKit 3.x includes GKSavedGame (GKLocalPlayer.FetchSavedGames / SaveGameData / ResolveConflictingSavedGames), which stores save blobs in the player's iCloud via Game Center — exactly matching the 'Game Center iCloud save' design. CloudSaveManager.cs should: authenticate GKLocalPlayer async on launch; if auth fails or iCloud Drive is disabled (common), silently fall to local JSON per the locked business rule. Save the same JSON blob you write locally (it's tiny — well under any practical limit). Known 2025 community gotcha: after a fresh install, FetchSavedGames may need to be called twice to return all saves — retry once on empty result before concluding 'no cloud save'.

### [medium] Linux dev cannot run Apple's build.py — commit prebuilt .tgz from the HalfbrickStudios fork
Apple distributes source only; build.py requires macOS with Xcode, python3, npm, and Unity (it runs xcodebuild to compile the native .frameworks). Unity Cloud Build will NOT run build.py for you — it builds your Unity project, so the plugin tarballs with prebuilt native libraries must already be in the repo. Workaround verified: HalfbrickStudios/apple-unityplugins Releases ships prebuilt com.apple.unityplugin .tgz files — latest release (Mar 27, 2025) bundles Apple.Core 3.1.8 + Apple.GameKit 3.0.2 built with Xcode 16.3 on macOS Sequoia. Commit the two .tgz files (Core + GameKit only, skip PHASE etc.) and reference via file: paths in Packages/manifest.json. The plugins' [PostProcessBuild] scripts inject frameworks + entitlements into the generated Xcode project on UCB's Mac builder. Risk to monitor: if Apple's iOS 26 SDK mandate ever requires re-linking the plugin frameworks, you'd need someone with a Mac (or a CI Mac runner) to rebuild — frameworks built with Xcode 16.3 linked into an Xcode 26 app build are expected to be fine, but verify with one early TestFlight build.

### [high] Guard all GameKit code behind #if UNITY_IOS && !UNITY_EDITOR with an editor mock
The native libraries are macOS/iOS only — they cannot load in the Linux editor. Define an ICloudSaveProvider interface; register a NullCloudSaveProvider (always 'auth failed', falls to local) in editor/standalone, and the GameKit-backed one only under UNITY_IOS && !UNITY_EDITOR. This also satisfies the locked rule that cloud failure silently falls through to local, and keeps Play-mode testing on Linux fully functional.

### [high] Entitlements/provisioning: App ID needs Game Center + iCloud (iCloud Documents) with a container; regenerate the profile after
GKSavedGame requires BOTH the Game Center capability and the iCloud capability with iCloud Documents enabled and a container (iCloud.<bundle-id>) on the App ID in the Apple Developer portal. Provisioning profiles do not auto-update: after enabling iCloud on the App ID, regenerate and re-upload the .mobileprovision to Unity Cloud Build, or you get opaque signing/entitlement mismatch failures. UCB iOS signing flow (no Mac needed, officially supported): create distribution certificate + profile in the developer portal, export .p12 with password, upload both to the UCB build target credentials. The Apple plugin's post-processor injects the com.apple.developer.game-center and iCloud entitlements into the Xcode project automatically; your profile must already permit them.

### [medium] Set Managed Stripping Level to Minimal (or add link.xml preserving Apple.GameKit)
Documented failure mode on Apple Developer forums with GameKit 3.0.x: IL2CPP managed stripping at Low+ removes the default constructor of Apple.GameKit.GKSavedGame, crashing at runtime when fetching saves. Either set Managed Stripping Level to Minimal for iOS, or (better for binary size) add a link.xml with <assembly fullname="Apple.GameKit" preserve="all"/>. Add this to the project before the first cloud-save test, not after the first mystery crash.

### [high] Save on OnApplicationPause(true), never rely on OnApplicationQuit — plan hook correction
Unity's official docs: iOS apps suspend rather than quit, so OnApplicationQuit is not called, and the OS can kill a suspended app with no callback at all. Primary save trigger must be OnApplicationPause(pauseStatus == true) (fires on home button, app switcher, lock screen, incoming call). Belt-and-braces for an idle game: also autosave every 30s from the tick system and on every meaningful event (stage advance, path choice, tribulation resolve, ancestor retire). Write lastSaveUnix (DateTimeOffset.UtcNow.ToUnixTimeSeconds) inside every save — it is the offline-calc anchor. Do NOT attempt cloud upload synchronously in OnApplicationPause (iOS gives you very little time); write local JSON synchronously there, and push to GKSavedGame opportunistically while the app is foregrounded.

### [high] Compute offline progress on resume-from-background too, not only 'on load' — plan assumption correction
The plan says offline progress is 'computed on load'. On iOS, users rarely cold-launch — the app resumes from suspension via OnApplicationPause(false). If you only compute on Awake/load, a player who backgrounds the app for 6 hours and resumes gets nothing (or worse, the tick system silently jumps). Run the same OfflineProgressCalculator path in OnApplicationPause(false): elapsed = nowUnix - lastSaveUnix; if elapsed >= 60s, show the Welcome Back collect screen; if smaller, just grant it silently through the normal tick math. Keeps OfflineProgressCalculator.cs pure (it just takes two timestamps + rate) per the off-limits rules, and makes the 8-hour clamp apply uniformly.

### [high] Unity Cloud Build: select macOS Sequoia builder + Xcode 26 before the April 28, 2026 App Store deadline
Apple requires all App Store Connect uploads to be built with iOS 26 SDK (Xcode 26+) starting April 28, 2026 — already in force as of today (June 2026). On Unity Build Automation, Xcode 26 only appears after setting the build target's Builder OS to macOS Sequoia (community-confirmed on Unity Discussions; no formal Unity announcement). Configure this from day one so TestFlight builds match the submission toolchain. Also: UCB retains Xcode binaries ~15 months, so pin the Xcode version explicitly rather than 'latest'. Pin the editor version to exactly 6000.4.10f1 in the build target to avoid silent auto-upgrades on the non-LTS tech stream.

### [medium] Unity 6000.4.x iOS via UCB: no known showstoppers, but treat the tech stream with patch discipline
6000.4.0f1 went stable March 18, 2026; patch releases through at least 6000.4.9f1/.10f1 exist with routine iOS fixes (on-screen keyboard regression UUM-132968, an iOS asset-cache crash UUM-120877 fixed in beta). No widespread Unity-6000.4-specific iOS or Cloud Build blocking issues surfaced in 2026 forum/issue-tracker searches. Residual risk of the deliberately-non-LTS choice: regressions land in tech-stream patches more often than LTS — read patch notes before bumping, and don't bump mid-phase during the 7-day MVP build.

### [high] 1s tick: Update() accumulator with Unix-timestamp source of truth — not InvokeRepeating, not a 1s coroutine
For an idle game, drift correctness should not depend on the timer at all: keep aseAmount derivable from (lastProcessedUnix, asePerSecond). In the tick system's Update(): accumulator += Time.unscaledDeltaTime; while (accumulator >= 1f) { accumulator -= 1f; GrantTick(1s); } — remainder carries, so long-run drift is zero. Avoid InvokeRepeating (string-based, scaled game time, keeps firing on disabled objects, drifts with no remainder carry). Avoid WaitForSeconds(1f) coroutine loops (each cycle completes on the first frame AFTER 1s elapses, so every iteration overshoots by up to one frame — drift accumulates ~1-2s/min at 30fps if naively re-waited). Awaitable.WaitForSecondsAsync is fine in Unity 6 but buys nothing for a single repeating tick and adds traps: Awaitable instances are pooled (never await twice), async-void exceptions vanish silently, and you must wire destroyCancellationToken. Critical mobile note shared by ALL approaches: nothing runs while the app is suspended — the resume path (recommendation 7) is what guarantees correctness, the per-frame tick is just presentation cadence. Update the on-screen number only when the displayed (formatted BigNumber) value changes, once per tick, not per frame.

### [high] UI: UGUI + TextMeshPro, single Canvas — lower-risk than UI Toolkit for a 7-day solo single-screen build
UI Toolkit runtime in Unity 6 is production-viable and performs well with many elements, but for this MVP its advantages are irrelevant (one portrait screen, ~a dozen widgets, labels updating 1×/s) while its gaps bite a 7-day schedule: animation limited to USS transitions (no Animator/Timeline — the tribulation flash/celebration moments are easier with UGUI tweens), thinner Asset Store/ecosystem, and a real learning curve for UXML/USS if unfamiliar. The 2025 Angry Shark guide's heuristic — casual mobile shipping within 6 months → UGUI — directly matches this project. Concrete setup: one Screen Space-Overlay Canvas, CanvasScaler 'Scale With Screen Size' reference 390×844 (iPhone 12), match=0.5; separate child Canvas for the once-per-second Àṣẹ counter so its rebuild doesn't dirty static elements; TextMeshPro for all text. Per existing architecture rules, UI components subscribe to On*-events and only read state.

### [medium] Game Center auth UX: authenticate once at launch, async, with a 'never blocks' timeout
GKLocalPlayer authentication on iOS may present a system sign-in sheet if the player isn't signed in — for an idle game this is acceptable only at cold launch, never on resume. Pattern: fire Authenticate() async at launch in parallel with loading local save; gameplay starts from local save immediately; when/if auth completes, reconcile cloud vs local (keep whichever has higher generation, then higher aseAmount, as the conflict rule — also use this for ResolveConflictingSavedGames). Never await auth before showing the game: this enforces the locked 'cloud failure must never block gameplay' rule structurally rather than by exception handling.

## Numbers
| Parameter | Recommended value | Why |
|---|---|---|
| Apple.Core package | 3.1.8 (prebuilt .tgz, Halfbrick release 2025-03-27) | Latest prebuilt; no Mac needed |
| Apple.GameKit package | 3.0.2 (same release) | Includes GKSavedGame |
| Managed Stripping Level (iOS) | Minimal, or link.xml preserve `Apple.GameKit` | Prevents GKSavedGame ctor strip crash |
| UCB Builder OS / Xcode | macOS Sequoia / Xcode 26 (pinned) | iOS 26 SDK mandatory for ASC uploads since 2026-04-28 |
| UCB Editor version | Pin exactly 6000.4.10f1 | Non-LTS tech stream; avoid silent bumps |
| Save triggers | OnApplicationPause(true) + every 30 s autosave + every progression event | OnApplicationQuit unreliable on iOS |
| Cloud push cadence | Opportunistic while foregrounded (after local write); never in pause handler | iOS suspension time budget |
| Offline-calc trigger | Cold launch AND OnApplicationPause(false); elapsed = now − lastSaveUnix | iOS resumes ≫ cold launches |
| Welcome Back screen threshold | Show only if elapsed ≥ 60 s; else grant silently | Avoids popup spam on quick app switches |
| Offline cap | clamp(elapsed, 0, 28800) — unchanged | Locked rule, confirmed sound |
| Tick implementation | Update() accumulator: `acc += Time.unscaledDeltaTime; while (acc >= 1f) { acc -= 1f; Tick(); }` | Zero long-run drift, remainder carried |
| UI label refresh | On tick (1/s) and only when formatted value changes | Minimizes UGUI canvas rebuilds |
| CanvasScaler | Scale With Screen Size, 390×844 reference, match 0.5 | iPhone 12 baseline, portrait |
| Cloud-vs-local conflict rule | Higher generation wins, then higher aseAmount | Deterministic, monotonic progress |

## Sources
- apple/unityplugins (official repo, GameKit/Core, 27-beta branch) https://github.com/apple/unityplugins
- Apple unityplugins Quickstart — build.py requires Xcode, python3, npm, Unity https://github.com/apple/unityplugins/blob/main/Documentation/Quickstart.md
- HalfbrickStudios apple-unityplugins — prebuilt .tgz releases (Core 3.1.8, GameKit 3.0.2) https://github.com/HalfbrickStudios/apple-unityplugins/releases
- playdigious apple-unityplugins fork (GameKit cloud saves) https://github.com/playdigious/apple-unityplugins
- Apple Developer Forums — GameKit 3.0.0 crash, stripping removes GKSavedGame ctor https://developer.apple.com/forums/thread/768441
- Apple Developer Forums — Game Center save game data to iCloud (iCloud Documents entitlement) https://developer.apple.com/forums/thread/775306
- Apple Docs — Game Center entitlement https://developer.apple.com/documentation/bundleresources/entitlements/com.apple.developer.game-center
- Apple Docs — resolveConflictingSavedGames https://developer.apple.com/documentation/gamekit/gklocalplayer/1521116-resolveconflictingsavedgames
- Unity Scripting API — MonoBehaviour.OnApplicationQuit (iOS suspends; use OnApplicationPause/Focus) https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnApplicationQuit.html
- Unity Discussions — OnApplicationPause and resuming an iOS game https://discussions.unity.com/t/onapplicationpause-and-resuming-an-ios-game/803958
- Unity Support — Release iOS game with Build Automation without a Mac or Xcode https://support.unity.com/hc/en-us/articles/32998687849108-Can-I-release-my-iOS-game-with-Unity-Build-Automation-without-directly-using-a-Mac-or-Xcode
- Unity Docs — Build Automation: Available Xcode versions https://docs.unity.com/en-us/build-automation/reference/available-xcode-versions
- Unity Docs — Build Automation: Sign an iOS application https://docs.unity.com/ugs/en-us/manual/devops/manual/build-automation/sign-build-artifacts/sign-an-ios-application
- Unity Discussions — Xcode 26 support for Unity Cloud Build (Sequoia builder required) https://discussions.unity.com/t/xcode-26-support-for-unity-cloud-build/1716431
- Unity 6000.4.0f1 release notes https://unity.com/releases/editor/whats-new/6000.4.0f1
- Unity 6000.4.9f1 release notes https://unity.com/releases/editor/whats-new/6000.4.9f1
- Unity Manual — Introduction to Awaitable (pooling, never await twice) https://docs.unity3d.com/6000.3/Documentation/Manual/async-awaitable-introduction.html
- Unity Scripting API — Awaitable.WaitForSecondsAsync https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Awaitable.WaitForSecondsAsync.html
- Unity Discussions — Accuracy of WaitForSeconds https://discussions.unity.com/t/accuracy-of-waitforseconds/194398
- Tantzy Games — Most Accurate Timer in Unity https://www.tantzygames.com/blog/most-accurate-timer-in-unity/
- Angry Shark Studio — Unity UI Toolkit vs UGUI: 2025 Developer Guide https://www.angry-shark-studio.com/blog/unity-ui-toolkit-vs-ugui-2025-guide/
- Unity Manual — Comparison of UI systems in Unity https://docs.unity3d.com/Manual/UI-system-compare.html
- Unity Discussions — UI Toolkit development status and next milestones (Nov 2025) https://discussions.unity.com/t/ui-toolkit-development-status-and-next-milestones-november-2025/1698009/67
