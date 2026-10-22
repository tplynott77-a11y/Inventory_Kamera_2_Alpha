# EXP-017 — both material tabs complete; quantity OCR audit

**Date:** 2026-10-22 16:49–16:55 (local). **Status:** Category traversal and name-recognition recovery PASS; quantity OCR accuracy FAIL. This is a test export, not approved live-tracker data.

## Evidence
- Actual scan ZIP: `Experiment 17.zip`; GOOD: `genshinData_GOOD_2026_10_22_16_55.json`.
- Actual log: `logging/InventoryKamera.debug.log`.
- Requested: Character Development Items and Materials, screenshot logging on, resolution 3840×2160.
- 16:49:55.913 verified **Character Development Items** tab; 16:53:12.265 scan stopped after **359 accepted names** on repeated `TheFrostEmperorsCeremony` (cancelled=False).
- 16:53:17.564 verified **Materials** tab; 16:55:07.922 scan stopped after **199 accepted names** on repeated `BitterPufferfish` (cancelled=False).
- 16:55:14.953 GOOD exported with **558 distinct material keys**. Both scanned categories have 1 uniquely named quantity screenshot per export entry (359 + 199). There was no character scan (not requested).
- Reference V3.2 GOOD of 2026-10-15 had 565 keys including Mora; 550 names overlap, 15 older reference names absent (including Mora) and 8 new names present. This **does not prove** older missing items were skipped; inventory can change between dates. Last-name repeat is a reasonable termination signal but not a definitive complete-item inventory count.

## Full manual screenshot-vs-export quantity audit

Every one of **558** named 195×44 quantity screenshots was visually checked against the GOOD value. The full per-item spreadsheet-friendly CSV was produced in this ChatGPT session as `Experiment_017_Full_Quantity_Audit.csv`. No historical inventory count was used as ground truth.

| Category | Crops match GOOD | Crops differ | Match rate |
| --- | ---: | ---: | ---: |
| Materials | 157 / 199 | 42 | **78.89%** |
| Character Development Items | 274 / 359 | 85 | **76.32%** |
| **Combined** | **431 / 558** | **127** | **77.24%** |

- Exactly **five** quantities were flagged as uncertain and exported as **0** even though screenshots visibly contain digits: `Almond` **35**, `ChaosStorage` **83**, `ShardOfAShatteredWill` **39**, `FrostnightsGlimmer` **137**, `TeachingsOfJustice` **30**. The remaining **122** quantity errors are silent, plausible-looking nonzero wrong values.
- Examples: `ChaosGear` GOOD **3906** versus screenshot **356**; `RawMeat` GOOD **104** versus screenshot **704**; `RainbowdropCrystal` GOOD **91** versus screenshot **37**; `GuideToJustice` GOOD **93** versus screenshot **33**; `Fowl` GOOD **400** versus screenshot **455**.
- Important positive control: `FalseWormBait` GOOD **1109** and screenshot **1109** (this was a problematic crop in prior versions).
- Visually dominant OCR confusions include screenshot **5** becoming GOOD **9**, **7** becoming **1**, and **3** becoming **9**. Repeated wrong readings from the ten-pass grayscale-weighted voter are apparently gaining consensus; having a weighted consensus is not sufficient to establish correctness.
- EXP-011 old reader scored 184/199 **92.46%** on a different run of ordinary Materials, so the new 78.89% is a concerning regression, though not the same item snapshots.

## Interpretation and next gates
1. **EXP-016 root cause fix works**: resetting Tesseract's shared engine digit-only whitelist restored name recognition and allowed scanning both tabs. This test does not establish the scanner is ready to import into the live tracker.
2. **EXP-018 should repair numeric OCR in V4's 195×44 crops**: inspect the two vote branches and voting confidence calibration; use these saved crops and screenshot truth as an offline regression corpus. Do not simply lower the threshold or blindly speed up.
3. Once numeric counts are reliable, turn off EXP-016's before/after full-screen forensic screenshots (or gate them behind a separate navigation-diagnostics option). The existing Log Screenshots setting can remain optional.
4. Separately validate **Characters** scanning and then benchmark/optimize throughput while maintaining accuracy.
5. **Do not overwrite the live farming tracker** with this GOOD. Original alpha and stable branches remain untouched; all V4 source fixes remain on `feature/v4-hybrid`.
