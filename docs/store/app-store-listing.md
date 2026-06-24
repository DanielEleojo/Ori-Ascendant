# App Store Connect listing — Ori Ascendant (draft)

Draft metadata for the App Store Connect submission. Copy is editable; the **bold** fields are
the App Store Connect inputs. Anything in `[brackets]` is a decision you must make. Screenshots
and the 1024×1024 marketing icon are **blocked on final art + the §7.10 review** (see
`docs/RELEASE_CHECKLIST.md`).

---

## Identity

- **App name:** Ori Ascendant
- **Subtitle** (≤ 30 chars) — pick one:
  - `Cultivate Àṣẹ across lifetimes` (30)
  - `An idle dynasty of light` (24)
  - `Idle cultivation & lineage` (26)
- **Bundle ID:** `com.oriascendant.game`
- **Primary category:** Games → Simulation
- **Secondary category:** Games → Role Playing
- **Price:** Free (no ads, no in-app purchases in this release)

## Promotional text (≤ 170 chars, updatable without review)

> Tend a bloodline across generations. Each cultivator rises, faces the Crossing, and becomes an
> ancestor whose light makes the next life stronger. No ads. No timers you must buy past.

## Description (≤ 4000 chars)

> **Ori Ascendant** is a calm idle game about lineage, patience, and the long arc of becoming.
>
> You cultivate Àṣẹ — a quiet inner force — life after life. A single cultivator climbs through
> six stages, chooses a path, and at their peak faces the Crossing: a tribulation that either
> lifts them into the spirit realm or returns them to the earth. Either way, they do not vanish.
> They become an ancestor, a star in your bloodline sky, lending their strength to the cultivator
> who comes after. A fall is never a dead end — it is a foundation.
>
> Progress continues while you are away (up to eight hours banked), so the game waits for you
> instead of demanding you. Come back to a brighter sky and a stronger heir.
>
> **Three paths, three temperaments:**
> • **Ane** — earth and endurance; steady, and strongest while you rest.
> • **Sango** — thunder and sudden force; fast, fierce, present.
> • **Osun** — the river and the line; flow that deepens with every ancestor.
>
> **What you'll find:**
> • A full generational loop — cultivate, cross, leave an ancestor, begin again stronger.
> • An Ancestral Council whose members' light compounds your production.
> • Crossroads — small moments of choice that shape how a life is remembered.
> • A Chronicle of every cultivator who came before.
> • Honest idle pacing: no ads, no energy meters, no purchases. Just the long climb.
>
> Ori Ascendant draws its world from West African (Yoruba, Igala, and Igbo) cosmology, rendered
> with care and reviewed for cultural respect. It is a homage, told in light and abstraction.
>
> Cultivate well. The line remembers.

## Keywords (≤ 100 chars, comma-separated, no spaces after commas)

`idle,incremental,cultivation,idle rpg,prestige,lineage,ancestor,mythology,African,clicker,calm,offline`

_(That string is 99 chars — trim/retune as desired. Do not repeat the app name or category.)_

## URLs

- **Support URL:** `[https://… — a page or email-backed contact]`
- **Marketing URL** (optional): `[https://…]`
- **Privacy Policy URL:** `[host docs/PRIVACY.md at a stable URL, e.g. GitHub Pages or your site]`

## Age rating (IARC questionnaire — suggested answers)

All content categories → **None**: no violence (the "tribulation" is an abstract pass/fail event,
no gore), no profanity, no sexual content, no nudity, no realistic gambling (no real-money or
simulated-gambling mechanics), no alcohol/drugs, no horror/fear, no mature/suggestive themes.
Expected result: **4+**. (If the reviewer treats the spiritual/mythological framing as "mild
mature/suggestive themes", it lands at 9+ — both are fine.)

## App Privacy ("nutrition label")

Answer **"Data Not Collected."** The app collects no data: no analytics, no ads, no trackers;
local save + Apple-managed Game Center/iCloud only (Apple handles that auth, you receive nothing).
See `docs/PRIVACY.md`.

## Export compliance

Uses only Apple's standard HTTPS/iCloud encryption; no proprietary cryptography. In App Store
Connect, answer the encryption question as **exempt** (standard OS encryption only) — confirm with
your own compliance review.

## Assets still required (blocked on art + §7.10)

- App icon 1024×1024 (opaque, no alpha, no rounded corners).
- 6.7" + 6.5" + 5.5" iPhone screenshots (2–10 each).
- Optional 30s app preview video.
