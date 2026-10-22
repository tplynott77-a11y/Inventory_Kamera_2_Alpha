# Experiment 009 — V4 Artifacts starting on Artifacts

**Date:** 2026-10-22; **build:** V4 .NET 10 hybrid preview; **state:** independently read active Artifacts tab.

## Inputs
- `logging artifacts starting on artifacts.zip` (original screenshots and log archive);
- `genshinData_GOOD_2026_10_22_12_31.json` (GOOD output);
- prior Experiment 008 `logging artifacts stating on weapons.zip` and `genshinData_GOOD_2026_10_22_12_28.json`.

## Findings
- 12:30:40.085 — artifact phase began.
- 12:30:42.307 — `V4 verified target inventory tab: 'Artifacts' (OCR='Artifacts').` **No switch was requested**.
- 12:31:14.258 — `Controller artifact scan finished: 285 of 285 scanned (cancelled=False, stopped=False)`.
- **34.173 seconds** for artifact phase including initial verification. Output written 12:31:19.264.
- **276 exported**, 266 five-star and 10 four-star; 201 equipped.
- All 276 artifacts match Experiment 008 on **all non-lock fields** when treated as a multiset, despite index shift from the previous duplicate.
- 008 had a byte-identical double capture of Gladiator's Finale flower at export IDs 251/252 and exported 277. 009 exported one corresponding item and produced **276 without the extra duplicate**.
- 009 lock counts: 111 true, 165 false; 008: 109 true, 168 false (includes extra repeated locked flower). Of corresponding 276 signatures, only 3 flags differ (false→true in 009), at common ID 2, 17, 84.
- No navigation-abort or cancellation logged.

## Verdict

**PASS for starting-tab recognition and artifact completeness relative to the existing reference.** Not a guarantee of perfect lock accuracy or controller reliability in other inventory screens. No code changes made; original alpha lock logic remains.
