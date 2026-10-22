# EXP-019 — full materials scan: no silent wrong counts observed, 24 flagged quantities

**Date:** 2026-10-22 17:57–18:03 local. **Status:** Full material-category navigation PASS; quantity export PARTIAL due to flagged-zero results. **Do not import this GOOD into the live tracker.** Characters deliberately not tested.

## Ground truth / input

- User-provided ZIP: `Experiment 19.zip`, including **558 individual 195×44 material quantity screenshots**, current `logging/InventoryKamera.log`, and 5,580 material OCR readings in `logging/InventoryKamera.debug.log`.
- GOOD export `genshinData_GOOD_2026_10_22_18_02.json`: exactly **558 material keys**.
- Every screenshot was visually checked against the corresponding GOOD material quantity, grouped into 14 contact sheets (42 entries per sheet, final 12). A full, 558-row CSV with original screenshot path, exported quantity, visually read quantity, and review status was saved as `Experiment_019_Full_Quantity_Audit.csv` in the ChatGPT session. The source ZIP and CSV contain user inventory; do not publish screenshots or private inventory CSV to the public repo without permission.

## Navigation / phase outcome

- 17:57:36 started Character Development Items; 17:57:39 verified target tab using OCR.
- 18:00:56 Char Dev **359 accepted names**, ended normally: `cancelled=False, stopped=False`.
- 18:00:56 requested ordinary Materials; 18:01:02 tab OCR verified `Materials`.
- 18:02:51 ordinary Materials **199 accepted names**, ended normally: `cancelled=False, stopped=False`.
- 18:02:58 converted/exported GOOD. No character scan requested.

## All-558 screenshot-to-export audit

| Category | Correct nonzero exported | Failed-safe zeros | Wrong nonzero accepted | Exact export accuracy |
| --- | ---: | ---: | ---: | ---: |
| Ordinary Materials | **188 / 199** | 11 | **0** | **94.47%** |
| Character Development Items | **346 / 359** | 13 | **0** | **96.38%** |
| **Overall** | **534 / 558** | **24** | **0** | **95.70%** |

- **All 534 nonzero exported values visibly matched their screenshots** in this single complete scan. Zero silent mistakes *observed* here does not prove future zero-error performance.
- The **24 zero exports were all visibly positive quantities**, so zero means unresolved scan, *not* owned item count of zero.
- The log has exactly **24 `Material quantity REVIEW REQUIRED` warnings**, **24 `Failed to parse quantity` messages**, and **17 `shape corrected weighted OCR` events**. Each of the 17 corrected outputs is screenshot-consistent.
- EXP-017 before verifier: 431/558 correct (77.24%), 122 silent mistakes, 5 uncertain zeros. EXP-019 after verifier: 534/558 correct, **0 observed silent mistakes**, 24 uncertain zeros. Different capture times; this comparison measures observed behaviour, not identical item snapshots.

### Actual values visible in 24 rejected screenshots

| Ordinary Materials (11) | True screenshot quantity |
| --- | ---: |
| CrystalChunk | 35 |
| RainbowdropCrystal | 37 |
| NilotpalaLotus | 5 |
| SpringOfTheFirstDewdrop | 32 |
| SkysplitGembloom | 33 |
| MistFlowerCorolla | 33 |
| Radish | 32 |
| Almond | 35 |
| AdhigamaWood | 37 |
| CypressWood | 35 |
| RedrotBait | 5 |

| Development Items (13) | True screenshot quantity |
| --- | ---: |
| FirmArrowhead | 677 |
| TrimmedRedSilk | 38 |
| FadedRedSatin | 318 |
| MechanicalSpurGear | 35 |
| AshenHeart | 10 |
| SilkenFeather | 3 |
| DenialAndJudgment | 3 |
| TheCornerstoneOfStarsAndFlames | 7 |
| ErodedSunfire | 3 |
| GuideToEquity | 5 |
| GuideToJustice | 33 |
| PhilosophiesOfKindling | 3 |
| PhilosophiesOfElysium | 3 |

## Root cause / follow-up

The shape reader is actually identifying **23 of the 24 unresolved screenshot counts correctly**, but current `ConfirmByShape` refuses to use them because Tesseract provided fewer than two agreeing numerical reads. This is a deliberate fail-closed tradeoff, not a movement/name regression.

**Critical safety exception:** `FirmArrowhead` screenshot shows **677**, but shape reader reported **65** because of wrong glyph segmentation; the OCR filtered branch produced **677** across all five scales and the weighted winner was **677**. Simply trusting shape-only answers would introduce a silent wrong value here, so **do not disable OCR corroboration wholesale**.

**EXP-020 development target:** Find safe additional validation for (a) isolated highly legible digit shapes when Tesseract repeatedly misreads 3/5/7, and (b) cases where digit segmentation merges adjacent digits (e.g., 677). Evaluate any new fallback **offline against both EXP-017 and fully independently held-out EXP-019 screenshot datasets** before a live scan. Preserve all successful navigation fixes and OCR whitelist reset; no character roster work until material outputs are correct. Avoid claiming 100% before a fresh test.
