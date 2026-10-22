## Fresh EXP-020 live validation — PASS (2026-10-22, 19:54–20:00)

**Source artifacts:** user-uploaded `Experiment 20.zip` (actual live logging directory and screenshots) and `genshinData_GOOD_2026_10_22_20_00.json`. No source artifact or private inventory data was added to GitHub.

**Navigation and GOOD completeness:**
- 19:54:48 verified Character Development Items; finished **359** names at 19:58:07 with `cancelled=False, stopped=False`.
- 19:58:13 verified ordinary Materials; finished **199** names at 20:00:02 with `cancelled=False, stopped=False`.
- GOOD export completed 20:00:09 and contains **558 distinct quantities** with **zero 0 values**. Exactly one fresh 195×44 quantity screenshot exists for every key, in the correct category.
- The current debug log has **23 `EXP020 high-confidence shape fallback`** events and **16 `shape corrected weighted OCR`** events; **zero** `REVIEW REQUIRED`, **zero** failed parse warnings, and **zero** error/warn level log lines.

**Measured screenshot consistency: 558/558 (100%)**:
- Compare every current GOOD quantity to the previously independently transcribed `Screenshot_quantity` in the full 558-row `Experiment_019_Full_Quantity_Audit.csv`: **558 match, 0 differ**.
- This reuse of previous visual truth is justified by direct screenshot comparison. On all 558 corresponding original quantity crops, the glyph-color binary masks of **542** EXP019/EXP020 pairs are pixel-for-pixel identical. The remaining **16** are visually checked side-by-side and have the same numeric glyphs; most differences are small background/star animation changes or only 1–2 mask pixels.
- The **24** items that were exported as zero in EXP019 are recovered with the screenshot-confirmed correct quantity; `FirmArrowhead` is **677** and `ChaosGear` remains **356**. There are no observed wrong accepted quantities in this complete live run.
- The files from EXP019 and EXP020 are not byte-identical: all 558 PNGs differ at least slightly, indicating independent capture. The matching inventory quantities and shapes allow previous visual labels to validate the current run.

**Accuracy interpretation:** This is the first live full Materials + Development Items scan with **558/558 accepted, screenshot-consistent quantities**. **100% is an observed single-run score, not a demonstrated guarantee of future perfect scans.** Both runs used the same 3840×2160 game UI, a highly similar stable inventory, and a digit-shape model originally calibrated using EXP017; testing across resolutions, UI changes, and changing counts is still worthwhile before general release. Quantity coverage does not validate the separate *Characters* roster scanner.

**Next engineering priorities:** preserve the validated quantity and name OCR algorithms; reduce the experimental before/after navigation forensic captures or gate under a diagnostics-only option; benchmark OCR processing and safely optimise speed; then separately test and repair Characters. Retain a regression image corpus and never change the release baseline without confirming accuracy.

---

# EXP-020 — Quantity fallback + touching-digit recovery

**Date:** 2026-10-22. **Branch:** `feature/v4-hybrid` only. **Status:** Implemented and replayed offline; **new live EXP-020 scan still required**. Do not import an experimental GOOD before checking its screenshot audit.

## Why EXP-019 exported 24 zeros

The full 558-item EXP-019 scan accepted 534 correct nonzero quantities and flagged 24 uncertain values (zero in GOOD); **zero incorrect nonzero quantities were observed in that scan**. Examining the 24 screenshots and raw OCR vote streams shows:

- **23/24** were correctly decoded by the independent glyph-shape reader, but each had fewer than two exact OCR vote matches.
- The remaining one, `FirmArrowhead` (actual **677**), had correctly repeated filtered OCR **677** but the glyph reader segmented adjacent 7s as one 36-column blob and returned **65**.
- In many failures, grayscale misreads 3→9, 5→9, 7→1, or drops the final 0 from 10. These patterns are visible repeatedly in the actual logs.

## EXP-020 changes

1. `MaterialDigitShapeReader.Analyze` now provides per-digit confidence metadata: **minimum margin** between the best and second best silhouette and **maximum distance** to the winning silhouette; it also checks expected 4K glyph geometry. 195×44 size remains required.
2. The reader splits abnormally wide **32–42-pixel** column runs into two approximately equal glyphs before classification. This recovers `FirmArrowhead` 677 (previously 65). Sub-parts with unexpected geometry or unclear templates still fail the fallback.
3. `MaterialQuantityVotingReader.ConfirmWithFallback` retains the existing safe rule first: **two exact OCR readings matching the independent shape answer**.
4. If that rule fails, the candidate is accepted **only** when its shape has normal digit geometry (width 7–23, height 19–31), minimum silhouette confidence margin **≥400**, maximum winning distance **≤600**, and **at least two** numeric OCR readings compatible with the independently decoded number via the observed confusions 3→9/1, 5→9, 7→1, or one trailing zero dropped. Unrelated disagreements, unfamiliar sizes and weak glyphs still return **0 requiring review**.
5. The new path is explicitly logged as `EXP020 high-confidence shape fallback`. Weighted filtering and ten OCR reads are unchanged; EXP-016 name-engine whitelist and all navigation code untouched.

## Full offline replay against two captured runs

Replayed the saved item-labelled crop masks and raw ten-vote streams using the new segmentation and decision conditions; did **not** adjust templates or train on EXP-019.

| Historical dataset | Previous actual export | EXP-020 predicted accepted | Predicted wrong accepted | Predicted unresolved |
| --- | ---: | ---: | ---: | ---: |
| EXP-017 (558 items) | 431/558 correct, 122 silent mistakes, 5 zero | **558/558** | **0** | **0** |
| EXP-019 (558 items) | 534/558 correct, 0 silent mistakes, 24 zero | **558/558** | **0** | **0** |

EXP-019 replay: 535 items accepted with exact OCR corroboration after splitting 677; 23 by conservative shape fallback. EXP-017: 536 exact; 22 fallback.

**Critical caveats:** This is **not** an independent 1,116-image generalization test. Digit templates were derived from EXP-017, both runs used the same user/UI/resolution, and some item screenshots may be pixel-identical. The confidence thresholds and allowed confusion patterns were selected after examining failures on these datasets. Therefore perfect replay is **not** evidence that new inventories or resolutions will be perfect. A blind live scan, manual verification and review of fallback-accepted values are essential. The verifier can still falsely accept novel bad cases. Maintain fail-closed results outside supported geometry.

## Build and tests

Added tests for individual observed confusion cases; recovery of 35 from OCR 99/39 and 10 from repeated OCR 1; retention of two exact OCR confirmations for 677; rejection of unknown mismatches and weak/abnormal shape confidence. Windows CI is required before live testing.

## Next operator test

**Experiment 020:** Git Pull `feature/v4-hybrid`, close all Kamera processes before rebuilding, select both **Character Development Items + ordinary Materials**, Normal controller delay, enable normal screenshot logging. Run to natural end, export GOOD and ZIP the active `Error Log` directory. Audit every item, particularly `FirmArrowhead`, all 24 previously unresolved names, every `EXP020 high-confidence shape fallback` log entry, all zeros and any shape-vs-weighted disagreements. Never silently merge current experimental GOOD into the live tracker. Characters as a roster scan remain out of scope.

