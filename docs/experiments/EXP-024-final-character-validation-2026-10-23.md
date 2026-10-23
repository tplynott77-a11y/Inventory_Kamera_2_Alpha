# EXP-024 — Final full-character scanner validation

**Status: PASS / BASELINE ELIGIBLE**  
**Date:** 2026-10-23  
**Repository branch tested:** `feature/v4-hybrid` (latest EXP024 code with named-scan recovery and DPI-safe menu)  
**Run:** 11:43:03 start; 11:51:16 character phase complete; 11:51:23 GOOD exported (approximately 8m20s end to end)  
**Scan settings:** Characters only; 3840×2160; DPI 192 (200%); game data 7.1; Normal delay setting 1; full roster, no named-selection override.

## Source evidence

Original user-provided, unmodified files (retained outside GitHub; **do not silently overwrite them**):

1. `genshinData_GOOD_2026_10_23_11_51.json` — SHA-256 `3afe4fba6255f2050464ff9ab46e121b821458509aae5cef82fa107e4ece83c0`
2. `InventoryKamera(4).log` — SHA-256 `554192d05b3e23b9355742b6832b402f2cc450648f144825c8ec7d6bfd8de5c2`
3. `InventoryKamera.debug(4).log` — SHA-256 `efc6b3d69620022af06ca6e6bc58eb2b6a7609b1da864125e95b6d49aa029217`

The original JSON contains 72 `characters` entries with **72 distinct GOOD keys**. Every entry has a level, ascension, constellation in [0,6], and `auto` / `skill` / `burst` all >= 1. Logs independently contain 72 `attributes`, 72 `Constellation`, 72 `Talents` and 72 `EXP024 TALENT_SUCCESS` records.

## Verification counts

| Check | Observed | Outcome |
|---|---:|---|
| Expected playable character records | 72/72 | PASS |
| Distinct GOOD keys | 72/72 | PASS |
| Attributes recorded | 72 | PASS |
| Constellations recorded | 72 | PASS |
| Combat talents recorded | 72 | PASS |
| Positive, bounded GOOD talent triplets | 72/72 | PASS |
| Explicit `EXP024 TALENT_SUCCESS` | 72 | PASS |
| `EXP024 TALENT_FAILURE` | 0 | PASS |
| `EXP024 HEADER_FAILURE` | 0 | PASS |
| `RESYNC` / `RESYNC_FAILED` | 0 | PASS for this full-roster run |
| Alternate header OCR acceptances | 11 | Successful recoveries |
| Fresh-frame header acceptances | 3 | Successful recoveries |
| First-attempt talent OCR | 59/72 | 13 recovered with retries |
| Non-mannequin error/warning entries | 0 | PASS |
| Intentional mannequin exclusion | 1 | Expected `cryo / farty_pants` |

## Previously blocking characters

| GOOD key | Constellation | Final base combat talents |
|---|---:|---|
| `TravelerCryo` | 4 | 5 / 5 / 6 |
| `Chongyun` | 5 | 1 / 1 / 1 |
| `Kinich` | 0 | 7 / 8 / 8 |
| `YunJin` | 3 | 1 / 1 / 1 |
| `Charlotte` | 4 | 1 / 1 / 1 |
| `Kirara` | 5 | 1 / 1 / 1 |
| `Mika` | 4 | 1 / 1 / 1 |
| `Chiori` | 0 | 1 / 1 / 1 |
| `Tighnari` | 0 | 1 / 1 / 1 |
| `Gorou` | 2 | 1 / 1 / 1 |
| `Mavuika` | 0 | 6 / 6 / 6 |
| `Yaoyao` | 2 | 1 / 2 / 1 |
| `Collei` | 0 | 1 / 1 / 1 |

Special handling confirmed: Cryo Traveler C3 adjusts raw burst 9 to GOOD 6; Chongyun C5 adjusts raw 1/4/4 to 1/1/1; Yun Jin C3 adjusts raw burst 4 to 1; Tartaglia's +1 Normal Attack passive is corrected on Tartaglia himself (log: "Applied Tartaglia auto attack fix to self only"); Skirk's team buff was not active under the scanned first-four party makeup.

## Regression comparison

Against earlier full export `genshinData_GOOD_2026_10_23_10_54.json`, the playable key set and all other character fields match **except**:

1. `Chongyun`: invalid review-zero `0/0/0` -> validated base `1/1/1`, which is the intended repair.
2. `Keqing`: previous `6/8/8` -> current `6/6/8`. The previous 10:54 log had a **10-attempt** accepted OCR of `6/8/8`; this scan's **2-attempt** accepted OCR reads `6/6/8`. Older GOOD snapshots on Oct 15, Oct 22 and Oct 23 10:11 also read `6/6/8`. Thus the single `8` is an **isolated historical outlier**, not evidence of a new regression. This does **not** independently prove Keqing's real in-game skill level: it is a residual spot-check option if a screenshot-level audit is requested.

Relative to `genshinData_GOOD_2026_10_23_10_11.json`, the only value differences are the two expected fixes: Chongyun `0/0/0` -> `1/1/1`, Cryo Traveler `5/5/9` -> `5/5/6`. All other values, including Keqing `6/6/8`, match.

## Decision and limits

**Decision: freeze the EXP024 character implementation as the validated full-roster baseline.** This is an in-game operational validation for the user's current roster at 3840×2160 and Normal speed, with automated CI and prior named-target scan corroboration. It is not a claim that every talent and constellation has separately been inspected against a screenshot or that arbitrary resolutions/input backends were tested. No extra full scan is required to close EXP024. Subsequent changes should be developed on the active branch and regression-checked against this snapshot, not silently changed in the archive.

The frozen **EXP020 Materials** branch (`archive/exp020-materials-validated-2026-10-22`, validated 558/558) remains independent and untouched.

**Next:** EXP025 full integration of Weapons, Artifacts, Characters, Materials; compare final combined GOOD to this character-only export and the EXP020 Materials snapshot. Keep experiments' actual GOOD/log evidence separately; do not upload users' full account inventory to the public repository by default.
