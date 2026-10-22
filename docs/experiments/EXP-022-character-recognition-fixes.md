# EXP-022 — Character OCR and diagnostic fixes

**Date:** 2026-10-22. **Branch:** `feature/v4-hybrid` only. **Status:** Implementation and automated regression tests; **fresh live character scan required**. The preserved `archive/exp020-materials-validated-2026-10-22` reference has not been changed. No material scanner or navigation code changed.

## Motivation and independent EXP-021 evidence

The 71-character export in `genshinData_GOOD_2026_10_22_20_27.json` and screenshot/log archive `Experiment 21.zip` revealed:
- Cryo Traveler omitted after OCR parsed `cryaq / t_2the_homas77`.
- Charlotte C4 and Kirara C5 *visibly* showed `Activated` in logged constellation node screenshots yet the gamma-only OCR returned false, causing C3 and C4 exports.
- Kachina's screenshot displayed Normal Attack `Lv. 1`, but wide-region OCR exported `11`.
- Six character talent passes never called talent OCR because `VerifyOnExpectedCharacter` could not read the selected header. Their all-zero GOOD levels are invalid rather than true talent levels.
- EXP-021's tall talent crop covered unnecessary passive rows. It parsed the first three apparent `Lv.` matches, not three independently identified rows.

## Changes in EXP-022

1. **Constrained Cryo Traveler repair**: `CharacterScanRecognition.RepairTravelerElement` maps the exact observed element token `cryaq` to `cryo` **only** after the name has independently resolved to Traveler. Existing `CharacterMatchesElement` still validates the pair. No general edit-distance element fuzzy match was added.
2. **Gold colour mask for constellation OCR**: `IsolateGoldConstellationText` creates a black-on-white crop by selecting pixels with `R>160`, `G>90`, `R-G>30`, `G-B>40`, then enlarges 3× and reads via Tesseract SingleLine. The previous gamma/invert/grayscale recognition remains as a secondary view, up to three screenshot attempts. Only an isolated whole word `Activated` counts; `Activate` must not count.
3. **Three bounded combat-talent crops**: the existing character talent region is captured once per attempt and split into dedicated Normal Attack, Skill and Burst level bands, with passive rows excluded and left-side talent names cropped away. Each band gets two differently preprocessed OCR reads. Both must independently parse exactly one legal `Lv. 1`–`Lv. 15` token **and agree** for that band to be accepted; inconsistent results trigger a retry, never a guessed digit. Up to eight attempts. Explicit zero sentinel + forced screenshot + error on failure. This intentionally prioritizes correctness over coverage; a fresh live run must measure any increase in unresolved levels.
4. **Character-header forensic captures**: failed Constellation/Talent identity verification force-saves both OCR-header crop and full 4K game screenshot under `characters/<name>/diagnostics/` and surfaces an explicit UI error; the missing six EXP-021 talents can now be diagnosed instead of remaining unexplained zeros.
5. **Constellation talent correction guard**: when raw talent OCR is missing, skip the C3/C5 bonus subtraction. When an alleged C3/C5 result would require subtracting 3 from a displayed talent below level 4, flag the contradiction, export a review-required zero sentinel, and avoid impossible negative levels.

## Offline checks

Against the **actual EXP-021** PNG crops (256×98):
- An offline pixel-for-pixel implementation of the gold-mask threshold produced crisp `Activated` lettering on **Charlotte C4** and **Kirara C5**.
- System Tesseract (English, SingleLine) read **`Activated`** from both enlarged masks. In contrast, comparison **Kachina C6 (locked)** and **Zhongli C1 (locked)** masks returned no text.
- These offline checks use system English Tesseract, *not* the packaged Genshin traineddata; fresh live scanner verification is therefore still required.
- Six sample full talent screenshots were inspected visually; Kachina's original scan crop clearly shows `Lv. 1` for the normal attack. The original ROI is 174×959; the new separated bands include the three combat level labels and exclude the passive talent rows. Generic system English OCR on these small crops is weak, so **do not claim validated talent accuracy** until the in-app trained OCR can be observed on a fresh scan.
- Automated Windows tests cover the observed Cryo typo restriction; gold foreground/background RGB discrimination; recognition of `Activated` versus `Activate`; bounds for the three talent bands; invalid, out-of-range and ambiguous `Lv.` text; and forced rejection of a `Lv. 11` versus `Lv. 1` two-view conflict. Check CI status for the current branch HEAD.

## Unmodified behavior / limitations

- EXP-020 Material and Development Items readout, navigation, and quantity-shape readers are untouched; archived proven baseline remains a recovery point.
- The experimental Constellation opt-out/cache toggle discussed with the user is **deferred**. GOOD requires a complete constellation value and C3/C5 affects base talents, so simple disabling without trusted saved values would corrupt exports.
- Weapons and Artifacts were not retested in EXP-022.
- **The modified Character scanner is NOT yet independently verified live and its GOOD must not be imported without a screenshot audit.** A scan may still contain zero-value review markers; those signal incompleteness, not owned Lv.0.

## Operator test — EXP-022 fresh live scan

Git Pull `feature/v4-hybrid`, close old Kamera processes, rebuild Visual Studio Release, use the same 3840×2160 game settings and Normal delay, **Characters only** (not Materials/Dev/Weapons/Artifacts), select All, enable screenshots and start in free-roam. Let it finish and provide (1) new GOOD export and (2) ZIP of the active Error Log folder. Review Cryo Traveler count, Charlotte/Kirara constellations, Kachina Normal Attack, all `EXP022` talent disagreement logs, all forced diagnostics, any zeros and all C3/C5 corrections. Compare all GOOD fields against new screenshots and the known previous GOOD before import.

