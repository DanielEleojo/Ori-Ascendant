# Release runbook — Ori Ascendant → App Store

The path from the current code-complete MVP to a responsible App Store release. Items are tagged
**[repo]** (done in this repository), **[you]** (external — Apple account, art, review, etc.), or
**[cloud]** (validated on Unity Cloud Build / TestFlight, not on the Linux dev box).

> **Reality check.** The code is feature-complete and gate-green (EditMode 584 passing). The
> remaining work is mostly *not* code: Apple account + signing, the Apple plugin binary, final
> art/audio, on-device validation, and the cultural review. Plan for **weeks**, not hours.

---

## 0. Hard gate — cultural review (do not skip)

- [ ] **[you] §7.10 native-speaker / community review.** The project's own docs are explicit:
  *"the pipeline cannot self-certify cultural safety."* All in-world text (Crossroads cards,
  names, epithets) and any figurative art must be signed off by a native Yoruba / Igala / Igbo
  reviewer against the ART_BIBLE §7 red lines **before** submission. This is an ethical line, not
  a technical one — it blocks publication regardless of how ready the build is.

## 1. In-repo engineering — DONE

- [x] **[repo]** Bundle id `com.vallicade.oriascendant`, product/version, iOS 15 min, portrait lock
  (`Assets/Editor/BuildConfigurator.cs`).
- [x] **[repo]** Explicit IL2CPP for iOS (deterministic Cloud Build).
- [x] **[repo]** `IosBuildPostProcessor` adds Game Center + iCloud (Documents container
  `iCloud.com.vallicade.oriascendant`) to the Xcode project. ⚠️ **[cloud] must be validated on the
  first Cloud Build** — it cannot compile/run on Linux (no iOS Build Support locally). If the
  `ProjectCapabilityManager.AddiCloud` overload differs in this Unity version, adjust the call.
- [x] **[repo]** Privacy policy (`docs/PRIVACY.md`), store listing (`docs/store/`), changelog.
- [ ] **[you]** Verify the two `Assets/Plugins/iOS/*.mm.meta` files import as **iOS-only** plugins
  the first time the project is opened in the Editor / on Cloud Build (folder convention should
  handle it; the metas are currently minimal).

## 2. Apple Developer setup — [you]

- [ ] Enrol in the Apple Developer Program (if not already).
- [ ] Create the **App ID** `com.vallicade.oriascendant` with **Game Center** and **iCloud
  (Documents) + container** capabilities enabled. The container id must match
  `iCloud.com.vallicade.oriascendant` (or update the constant in `IosBuildPostProcessor`).
- [ ] Create the app record in **App Store Connect** (name, bundle id, primary language).
- [ ] Generate a **distribution certificate** and an **App Store provisioning profile** for the
  App ID — regenerate the profile *after* enabling iCloud + Game Center so it carries the
  entitlements.

## 3. Apple GameKit plugin — [you]

- [ ] Commit the prebuilt **Apple.Core + Apple.GameKit** `.tgz` (the Halfbrick packages) into the
  repo — they cannot be rebuilt on Linux. Add their `file:` entries to `Packages/manifest.json`
  and `"Apple.Core"` / `"Apple.GameKit"` to the `OriAscendant.Systems` (or `.Save`) asmdef
  references. `Assets/link.xml` already preserves them for IL2CPP stripping.
  **Cloud save will not link without this.**

## 4. Unity Cloud Build — [you] / [cloud]

- [ ] Configure the iOS Cloud Build target: Apple Team ID, the distribution cert + provisioning
  profile, and let Cloud Build own the **build number** (auto-increment per upload — it is
  deliberately *not* committed in `BuildConfigurator`).
- [ ] Pin the macOS + Xcode image Cloud Build uses.
- [ ] **[cloud]** Run the first build. Confirm: it compiles with the GameKit plugin, the
  `IosBuildPostProcessor` adds the capabilities cleanly (watch for a conflict with
  `iOSAutomaticallyDetectAndAddCapabilities` — if Game Center is double-added, turn that Player
  Settings toggle off), and the IPA signs.

## 5. Art & audio — [you] (blocks store screenshots & launch quality)

- [ ] App icon 1024×1024 (opaque) + the in-app icon set assigned in Player Settings.
- [ ] The 37 visual assets and 10 audio assets per `ASSET_MANIFEST.md` (procedural placeholders
  ship a playable but unfinished look). Each art asset runs the ART_BIBLE §9 red-line checklist
  **and** the §7.10 review before it ships.
- [ ] A branded launch screen (currently a solid black background — acceptable but plain).

## 6. On-device validation — [cloud] (TestFlight)

- [ ] Game Center auth on real hardware (the simulator is unreliable for GC).
- [ ] Cloud save round-trip + conflict resolution across two devices/installs.
- [ ] Force-close mid-Crossing, reopen — confirm the persist-first outcome survived.
- [ ] Overnight offline — confirm the 8-hour cap is honoured.
- [ ] Haptics + Reduce-Motion (ADR-0004/0005) behave on device.
- [ ] Yoruba diacritics render correctly on a retina screen.

## 7. App Store Connect submission — [you]

- [ ] Fill the listing from `docs/store/app-store-listing.md` (description, subtitle, keywords,
  category, support URL).
- [ ] Host `docs/PRIVACY.md` at a stable URL and set the **Privacy Policy URL**.
- [ ] Complete the **App Privacy** questions → *Data Not Collected*.
- [ ] Answer **export compliance** → exempt (standard OS encryption only — confirm yourself).
- [ ] Complete the **age-rating** questionnaire (expected 4+/9+).
- [ ] Upload screenshots (needs final art).
- [ ] Submit for review.
