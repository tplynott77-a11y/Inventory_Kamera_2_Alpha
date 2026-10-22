# EXP-023 — EXP-022 talent regression: GOOD audit and controlled rollback

**Date:** 2026-10-22, GOOD timestamp 21:15. **Branch:** `feature/v4-hybrid`. **Status:** EXP-022 live character run **FAIL for all talents**; EXP-023 corrective code has passed an initial Windows build but **live retest pending**.

## Evidence received

- `genshinData_GOOD_2026_10_22_21_15.json` contains **71 characters**, the same number as EXP-021. Every character has all three talents zero, **except Tartaglia's auto is -1**. That is **212 zero fields and 1 negative field out of 213 talent fields**; none is a legitimate GOOD talent level. **Do not import this file**.
- User reports **no talent screenshots appeared in the Kamera UI**, and the latest Cryo Traveler was misread as `gryo`. Second custom-name mannequin ("Farty Pants") was rejected again; mannequin is not expected in playable GOOD characters.
- **Charlotte C4 and Kirara C5 are correctly reported** in EXP-022, vs C3/C4 in EXP-021 respectively. That supports keeping the new gold-mask constellation reader. Other constellation values should not automatically be assumed correct without screenshot audit.
- User provided `Experiment 22.zip`, **~420 MB**. The local file-processing container was unavailable during this audit, so its internal OCR debug lines and screenshot timestamps were **not independently read**. Therefore this report does not claim whether tiny-crop boundaries or individual Tesseract views are the primary source of rejection.

## Deterministic source-code cause

EXP-022's `ScanTalents` was a new method using small row crops, two differently preprocessed OCR reads per row, and requiring both reads to match a strictly parsed `Lv.` token for **all three rows in the same attempt**. If any one row failed, it discarded the entire attempt; after eight unsuccessful attempts, it returned zero/zero/zero. The UI preview calls were inside the success branch only, so it displayed **no talent screenshots when this happened**. The all-zero export and blank UI are direct consequences of this success gate, regardless of which exact OCR read failed.

A second independent bug was in `ApplyTartagliaFix`: it subtracted the Childe passive from an unverified zero, creating **-1**.

## EXP-023 corrective implementation

- **Restore the EXP-021 broad-region/first-three-`Lv.` talent reader** that provided 65 successful talent records. It is not perfect (EXP-021 incorrectly accepted Kachina auto Lv11), but recovers the previously functioning baseline without rolling back the new constellation improvements.
- Show the captured talent region in the UI on the **very first attempt**, with an "unverified" label, instead of waiting for OCR success. On exhausted retries, save both forced talent-region and full-window screenshots, emit error, and keep the zero review sentinel.
- Guard Tartaglia auto and Skirk skill passive corrections against zero or missing talents; no `-1` should arise from absent reads. `ApplyConstellationTalentScaling` already rejects invalid levels and impossible C3/C5 deductions.
- Add targeted sanity check for impossible low-ascension Normal Attack readings: an ascension-3-or-lower character can't display raw Normal Attack Lv11. Fail/retry instead of accepting EXP-021 Kachina's false 11; on failure mark the talent set for review. Avoid imposing this bound on higher ascension characters.
- Add exact observed `gryo` → `cryo` correction **only for independently recognised Traveler**, next to earlier `cryaq`; don't expand fuzzy matching to other characters.
- Add regression tests for the two observed Traveler misspellings and Kachina's impossible low-ascension normal attack.
- Do **not** modify material, weapon, artifact or verified EXP-020 archive code. This is still a characters-only change.

## Validation and next experiment

A successful automated Windows build/test is a code-health check, **not proof that the restored character scanner reads levels live**. The next useful experiment is a **Normal-delay, 4K, screenshot-enabled, Characters-only, short 10-character scan**, not another eight-minute full-roster scan. Audit at least one valid talent read in the on-screen UI and GOOD, before expanding to the whole roster. Then confirm Charlotte/Kirara C4/C5, Cryo Traveler, Kachina auto Lv1, six identity-verification misses from EXP-021. No live import from either EXP-021 or EXP-022.
