# Experiment 013 — navigation-invalid mixed inventory trial

**Date:** 2026-10-22 15:05 local (GOOD export timestamp).
**Status:** INVALID FOR OCR ACCURACY / FAILED COVERAGE. Retain the raw artifacts for diagnosis.

## Supplied evidence

- `genshinData_GOOD_2026_10_22_15_05.json`: **181** distinct exported keys in `materials`, with **zero ordinary Materials** and the same **181 keys in exactly the same order** as the 14:40 EXP-012 GOOD. Eight quantity values changed across the two exports: `HerosWit` 1→99, `FrostnightsGlow` 91→40, `DenialAndJudgment` 3→9, `ErodedSunfire` 3→9, `MaskOfTheVirtuousDoctor` 3→9, `TeachingsOfJustice` 30→0, `PhilosophiesOfKindling` 3→9, `TheFrostEmperorsCeremony` 1→0. Cannot assess which count is correct without matching screenshots.
- Zero counts in this export: `ChaosGear`, `TeachingsOfJustice`, `TheFrostEmperorsCeremony`. Zero represents unresolved quantity OCR in V4, **not independently validated zero stock**.
- `Experiment 13.zip` contains exactly the **same 229 files**, contents and filenames as `Experiment 12.zip` (including **223/223 identical PNG screenshots**). Logs stop at 14:35 and include the older 13:07–13:09 Experiment 011 scan; there is **no 15:05 log or corresponding current quantity captures**.
- Comparing V4's 181 ordered item names with the 15 October V3.2 stable GOOD: 178 matched stable keys (3 not in the older source). Of 177 adjacent matched index transitions, **158 are a +2 index jump**. This agrees with user-observed approximately one-in-two item skipping, without proving whether selection moved twice or capture/name recognition missed an intermediate step.
- User reported that the Materials page did not scan at all. The correct 15:05 debug log is unavailable; root cause of the missing second category remains unconfirmed.

## Decision

Do not use EXP-013 for weighted OCR scoring; the selection population is systematically incomplete and its screenshots are stale. Retain the 15:05 GOOD and supplied ZIP unchanged for reproducibility. **No export into live tracker.**

## Experiment 014 plan — navigation gate

1. Run only one Inventory Kamera instance. Confirm checked tabs in UI and `Scan settings` in the current log; use Normal delay=1 and 3840×2160.
2. Start on Character Development Items. Observe first 10–12 highlighted selections or manually measure each selection after a *single* controller input. Expect 1 grid slot per tap. If items alternate/skip again, stop immediately; do not continue a full scan.
3. Resolve whether a single 80ms left-stick hold + 100ms settle advances by two positions or another code/UI task is making a second move. Do not adjust Fast timings or the weighted OCR based on this incomplete data.
4. After correcting and demonstrating one-to-one movement, test the Development Items → Materials tab handoff as a separate gate and verify that a new `Materials` phase actually begins.
5. Recover the real scan directory (runtime relative `./logging` may differ from the folder users zip). Capture newly timestamped PNGs and scan logs before any OCR benchmark.

**Repository:** Only `feature/v4-hybrid`; original alpha and stable branches remain untouched.