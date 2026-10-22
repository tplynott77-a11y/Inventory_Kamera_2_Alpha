# Experiment 010 — V4 Materials starting on Artifacts

**Date:** 2026-10-22 (Ireland); **build:** .NET 10 V4 preview (code baseline `7882ec1c0d4e6113b8a604c9af1d5c31859247cc`); **branch:** `feature/v4-hybrid`.

## Inputs and setup

- Raw log/screenshots: user-uploaded `Experiment 10.zip`.
- GOOD export: `genshinData_GOOD_2026_10_22_12_34.json`.
- Genshin window reported as 3840×2160; game data 7.1; Materials **only**; delay 0; screenshots enabled.
- Starting inventory tab: Artifacts; intended destination: Materials.

## Timestamped evidence (local)

- 12:34:45.384 — `Scanning materials...`
- 12:34:46.612 — `V4 navigating Artifacts -> Materials: 3 right presses.`
- 12:34:48.691 — failed initial verification: observed `Food` (OCR `Food`), **not** Materials.
- 12:34:50.069 — `V4 recovered and verified 'Materials' in 1 additional tab steps.`
- 12:34:50.462 — detected quantity row 0 band: estimate 0.2150 -> top 0.2139 (45px).
- 12:34:51.588 — `Repeat material found (MidlanderClaymoreBillet) -- stopping controller Materials scan.`
- 12:34:51.588 — `Controller Materials scan finished: 7 scanned (cancelled=False, stopped=False)`.
- 12:34:56.632 — incomplete seven-key GOOD exported without cancellation flag.

The initial navigation was wrong but the new verification caught it and successfully recovered. The Materials scan itself was not complete. `FullInventory_3840x2160.png` visibly shows the Materials tab and inventory capacity 947/2600; that capacity figure must not be equated without qualification to the exact number of expected GOOD material keys.

## Quantity crop vs exported value (seven items)

| Key | Screenshot quantity | GOOD quantity | Match |
| --- | ---: | ---: | --- |
| NorthlanderBowBillet | 2 | 4 | **NO** |
| NorthlanderClaymoreBillet | 1 | 1 | YES |
| NorthlanderCatalystBillet | 2 | 2 | YES |
| NorthlanderPolearmBillet | 3 | 3 | YES |
| MidlanderSwordBillet | 6 | 4 | **NO** |
| MidlanderBowBillet | 1 | 1 | YES |
| MidlanderClaymoreBillet | 4 | 4 | YES |

These are direct visually readable `logging/materials/quantity/*_Quantity.png` crops from the same run, not guesses from an older inventory. **5/7 read correctly; 2/7 demonstrably misread.** The expected quantity-band crop placement for the first row appears appropriate.

## Code-level interpretation (not definitive cause)

- The current V4 `MaterialScraper.ScanMaterials` stops at the **first** repeated recognized material name (lines 473–477). A repeat was observed immediately after scanning seven items.
- It invokes `controller.MoveStep(Right, holdMs: ScaledControllerDelay(80), settleMs: ScaledControllerDelay(100))` per item. A missed/stale movement or an OCR alias could explain the repeated name; per-step screenshots are absent, so the cause is **not yet proven**.
- `ScanQuantityBitmap` still uses the inherited alpha OCR pipeline, including early return on the first parsable result, and does **not** contain V3.2's verified per-digit silhouette logic. This test independently proves quantity misreads, but not which internal OCR branch produced each misread.

## Verdict

- **PASS:** Fail-closed tab verification detected Food and recovered to the requested Materials tab before scan.
- **FAIL:** Controller Materials traversal terminated after seven items and nevertheless generated a seemingly normal GOOD export.
- **FAIL:** Two of seven numeric quantities conflict with their own saved screenshots.
- **OVERALL: PARTIAL / NOT SAFE FOR INVENTORY IMPORT.** Preserve V3.2 as source of truth; no live tracker update; retain current alpha lock logic.

## Recommended next experiment (011)

Instrument or harden controller selection advancement and repeat handling on V4 only: capture selected-card/name evidence before/after moving, allow bounded retry when an unexpected repeat appears, and avoid treating a single repeat as proof of end-of-list. Fail export completeness validation if an implausibly short uncancelled Materials run occurs. Then repeat Materials-only from Artifacts with screenshots and compare coverage. Quantity transplant from V3.2 remains a separate follow-on milestone.

**No code changes were made by this report.**
