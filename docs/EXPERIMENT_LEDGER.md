# Inventory Kamera V4 — Experiment Ledger

**Persistent source of truth:** this file tracks completed V4 trials. The original scan ZIP and GOOD JSON exports should be kept intact for reproducibility. Log text, screenshots and JSON provide evidence; GitHub summary does not replace the raw files.

**Baseline:** 3840×2160 Genshin window, V4 controller-first .NET 10 preview from commit `7882ec1c0d4e6113b8a604c9af1d5c31859247cc`, minimum artifact rarity 4★, minimum artifact level 0; 285 inventory entries.

| Experiment | Date/time (local) | Start tab | Requested tab | Navigation | Entry count | Export count | Artifact phase | Notes |
| --- | --- | --- | --- | --- | ---: | ---: | ---: | --- |
| 007 | 2026-10-22 (earlier) | — | Artifacts | V2 vs V3.2 benchmark | 285 each | 276 each | Alpha 33.083 s; V3.2 110.776 s | 276/276 exact non-lock stats; lock status unreliable in both |
| 008 / Test 1 | 2026-10-22 12:27 | Weapons | Artifacts | **PASS**: detected Weapons, navigated right, verified Artifacts | 285/285 | **277** (one duplicate) | **34.223 s** | Identical screenshot repeated as artifact ID 251/252; all 276 reference signatures present |
| 009 / Test 2 | 2026-10-22 12:30 | Artifacts | Artifacts | **PASS**: verified current tab, no switch | 285/285 | **276** | **34.173 s** | All 276 non-lock signatures match 008, duplicate absent; three differing lock flags |
| 010 / Test 3 | Pending | Artifacts | Materials | Not yet tested | — | — | — | Verify category switch before examining material OCR |

## Sources and reproducibility

- Experiment 008 logs: `logging artifacts stating on weapons.zip`; output: `genshinData_GOOD_2026_10_22_12_28.json`.
- Experiment 009 logs: `logging artifacts starting on artifacts.zip`; output: `genshinData_GOOD_2026_10_22_12_31.json`.
- 008 log at 12:27:26.293: `V4 navigating Weapons -> Artifacts: 1 right presses.`
- 008 log at 12:27:27.664: `V4 verified target inventory tab: 'Artifacts' (OCR='Artifacts').`
- 009 log at 12:30:42.307: `V4 verified target inventory tab: 'Artifacts' (OCR='Artifacts').`
- 008 phase: 12:27:25.210–12:27:59.433; 009 phase: 12:30:40.085–12:31:14.258. These phase durations include navigation verification and artifact capture but exclude initial game/menu setup and later export.
- Both logs: `Controller artifact scan finished: 285 of 285 scanned (cancelled=False, stopped=False)`.
- 008 exported ID 251 and 252 as a duplicate Gladiator's Finale flower with **identical card screenshot SHA-256** (not a database-key deduplication issue). The corresponding 009 screenshots are different; IDs from 009 shift after index 251.
- After excluding lock and sequential export ID, the multiset of 009's 276 signatures equals the 276 baseline signatures present in 008. 008 includes one additional duplicated signature.
- Lock changes on aligned signatures: 008→009 false→true for the selected `HeartOfTheFurnace` sands (ID 2), `ScrollOfTheHeroOfCinderCity` flower (ID 17), and `CrimsonWitchOfFlames` flower (ID 84). 008 reported 109 locked across 277 exported entries; 009 reported 111 locked across 276. Current decision: preserve alpha/V4 lock detector as-is, defer changes.

## Interpretation

The **initial controller tab verification feature** has passed two live artifact-only test cases. These results do *not* validate material-only navigation, mixed scans, quantity OCR, character roster handling, or absence of occasional controller-selection timing errors. V4 is still experimental. The fully validated V3.2 inventory/farming tracker should not be overwritten with test scans.

## Next experiment

**010 — Materials only, starting from Artifacts.** Confirm screenshot/tab OCR identifies Materials after switching. Short test suffices: material quantity accuracy is intentionally out of scope until the V3.2 precision OCR transplant.

## Updating this ledger

For each subsequent experiment, record:
1. starting UI tab and requested category;
2. branch/commit, Genshin resolution, configuration;
3. filenames of ZIP log and GOOD JSON;
4. exact log timestamps, scanned/filtered/exported counts;
5. field-level comparisons, errors, and user-observed behaviour;
6. PASS / PARTIAL / FAIL, and next action.

**Unknown user-reported major issue:** User has indicated discovering a new major issue while submitting Experiment 009 but has not yet described its symptoms. Do not speculate or mark as confirmed until details/evidence are supplied.
