# §7.10 Content Review Record — Crossroads Deck & Name/Epithet Pool

**Issue:** #10 — Dynasty: author + §7.10-review the crossroads deck & name/epithet pool (slice 7)
**Date authored:** 2026-06-19
**Review status:** Self-reviewed against ART_BIBLE §7 red lines (see below).
**⚠ Pending:** Human native-speaker / community review before launch (ART_BIBLE §7.10 — the pipeline never self-certifies).

---

## Content authored

### OriConfig — Ori virtue names (virtueIndex 0/1/2)

| Index | Yoruba name | Meaning | Source |
|---|---|---|---|
| 0 | Sùúrù | Patience / endurance | Attested common Yoruba word; proverb "Sùúrù ni baba ìmọ̀" (patience is the father of wisdom) |
| 1 | Ìgboyà | Courage / boldness | Standard Yoruba word for bravery |
| 2 | Àánú | Mercy / compassion | Standard Yoruba word for compassion/pity |

No initiatory titles; no supreme-deity compounds; full diacritics.

### CrossroadsConfig — production deck (6 cards)

| id | Scenario | Virtue options (0=Sùúrù, 1=Ìgboyà, 2=Àánú) |
|---|---|---|
| road_stranger | Stranger blocks the narrow road | Wait beside / Speak firmly / Step around |
| elder_call | Ill elder calls while urgent task waits | Turn back / Send word & press on / Go to elder |
| market_debt | Market-woman claims an unremembered debt | Hear the account / Refuse clearly / Offer to settle |
| market_accusation | Man of standing accuses you publicly of theft | Stand quietly / Speak in contest / Settle in private |
| hungry_child | Unknown hungry child in the road; provisions only for yourself | Wait for someone who knows them / Speak into market / Share |
| brothers_land | Two brothers ask you to judge their father's land dispute | Hear fully first / Name what's right / Find middle road |

### CrossroadsDeckConfig — fallen epithets (1:1 with deck by index)

| Index / card | Epithet |
|---|---|
| 0 / road_stranger | Who Passed the Stranger By |
| 1 / elder_call | Who Did Not Turn Back |
| 2 / market_debt | Who Gave Without Remembering |
| 3 / market_accusation | Who Let the Word Stand |
| 4 / hungry_child | Who Kept Their Provision |
| 5 / brothers_land | Who Did Not Finish the Judgment |

### RemembranceConfig — personal names pool (ascended Title)

| Name | Meaning |
|---|---|
| Àyọ̀ | Joy |
| Ẹniọlá | Person of honour |
| Abíọ́dún | Born at the festival |
| Ọládélé | Honour comes home |
| Adéọlá | The crown's honour |
| Ìdòwú | Born after twins — perseverance |
| Bólájí | Find honour in this |
| Fẹ́mi | One who is loved |
| Ọmọ́tọ́lá | A child worthy of wealth |
| Dúpẹ́ | Give thanks |

All are attested common Yoruba given names. No initiatory titles. No compound with Olódùmarè/Ọlọ́run. Full diacritics throughout.

### RemembranceConfig — faithfulFallLine

`"Who Faced the River Faithful"` — acknowledges the Crossing (river imagery), honours a life that held its vow, dignified and warm (not punitive).

---

## §7 self-review checklist

> **Reviewer default posture: adversarial and conservative — when in doubt, FAIL.**
> A false-reject costs one revision; a false-accept ships disrespect.

- [x] **§7.1 No supreme deity depicted or named.** None of the scenarios, names, or epithets reference Olódùmarè/Ọlọ́run/Igala Ọjọ́.
- [x] **§7.2 No initiatory offices as ranks.** Virtue names (Sùúrù/Ìgboyà/Àánú) are common vocabulary words, not conferred religious titles. Personal names are common given names.
- [x] **§7.3 No egungun/masks/sacred regalia.** Text content only; none of the scenarios, epithets, or names invoke masquerade, regalia, or ancestor-as-figure imagery.
- [x] **§7.4 No reproduced liturgy or divination imagery.** No Ifá verses, no cowrie/ikin/opón Ifá references, no Odù marks in any text.
- [x] **§7.5 Orisha as natural forces only; no personified deity.** The scenarios are ordinary life dilemmas (the road, the market, family disputes). No orisha named or depicted as a character.
- [x] **§7.6 No voodoo/witchcraft framing.** All scenarios are warm, ordinary, human. The faithfulFallLine and epithets are dignified — no grey/death/horror cues. Epithets describe choices, not punishments.
- [x] **§7.7 No homogenization of traditions; full diacritics.** Yoruba names carry full diacritics (subdot + tone marks). Ane is not invoked in content (Igala distinction preserved). No pan-African pastiche in any text.
- [x] **§7.9 Orthography.** All Yoruba words use full diacritics verified character-by-character.
- [ ] **§7.10 Human native-speaker / community sign-off.** ⚠ **NOT YET COMPLETE.** This review record documents the AI-authored self-review pass. A human native speaker must review all content above before launch. The pipeline does not self-certify.

---

## Notes for human reviewer

1. **Virtue names (Sùúrù, Ìgboyà, Àánú):** Please confirm these are natural, respectful choices — especially that `Àánú` (mercy/compassion) is appropriate for this context and does not carry any unintended connotations.
2. **Personal names:** Please verify each name's diacritics are correct and that no name is associated with an initiatory office or carries unintended meaning in context.
3. **Crossroads prompts:** Please check that the scenarios feel grounded in West African life experience without invoking sacred/liturgical/proprietary cultural elements.
4. **Epithets and faithfulFallLine:** Please confirm these feel dignified and honoring — the ART_BIBLE requires that "Would a grieving family find this honoring?" be the test.
5. **"Who crossed the river"** phrasing in `faithfulFallLine`: "Who Faced the River Faithful" uses the Crossing (Ìrékọjá) river imagery — please confirm this is appropriate as a fallen-but-faithful epithet without invoking sacred passage rites too literally.
