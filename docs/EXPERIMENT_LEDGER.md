# Inventory Kamera V4 — Experiment Ledger

**Persistent source of truth:** this file tracks completed V4 trials. The original scan ZIP and GOOD JSON exports should be kept intact for reproducibility. Log text, screenshots and JSON provide evidence; GitHub summary does not replace the raw files.

**Baseline:** 3840×2160 Genshin window, V4 controller-first .NET 10 preview from commit `7882ec1c0d4e6113b8a604c9af1d5c31859247cc`, minimum artifact rarity 4★, minimum artifact level 0; 285 inventory entries.

| Experiment | Date/time (local) | Start tab | Requested tab | Navigation | Entry count | Export count | Phase time | Notes |
| --- | --- | --- | --- | --- | ---: | ---: | ---: | --- |
| 007 | 2026-10-22 (earlier) | — | Artifacts | V2 vs V3.2 benchmark | 285 each | 276 each | Alpha 33.083 s; V3.2 110.776 s | 276/276 exact non-lock stats; lock status unreliable in both |
| 008 / Test 1 | 2026-10-22 12:27 | Weapons | Artifacts | **PASS**: detected Weapons, navigated right, verified Artifacts | 285/285 | **277** (one duplicate) | **34.223 s** | Identical screenshot repeated as artifact ID 251/252; all 276 reference signatures present |
| 009 / Test 2 | 2026-10-22 12:30 | Artifacts | Artifacts | **PASS**: verified current tab, no switch | 285/285 | **276** | **34.173 s** | All 276 non-lock signatures match 008, duplicate absent; three differing lock flags |
| 010 / Test 3 | 2026-10-22 12:34 | Artifacts | Materials | **PASS after recovery**: overshot to Food, recovered to Materials | **7 (premature stop)** | **7** | **6.204 s** | Repeat MidlanderClaymoreBillet stopped scan; 2/7 quantities wrong vs screenshot; export incomplete |

| 011 / Materials delay 1 | 2026-10-22 13:07–13:09 | Materials | Materials | **PASS:** independently verified Materials, no switch | **199** | **199** | **61.307 s** | 184/199 quantity crops match JSON; 15 wrong (4 flagged, 11 silent); ended on repeated BitterPufferfish |

| 012 / Mixed categories | 2026-10-22 14:40 export | Unverified | Char Dev + Materials requested | **UNKNOWN**: matching scan log absent | Unknown (181 distinct in export) | **181 Dev / 0 ordinary Materials** | Unverified | Provided ZIP repeats all 223 EXP-011 images; `ChaosGear=0` unresolved OCR; mixed-scan storage and image-log segregation fixes committed |

| 013 / Repeated mixed category | 2026-10-22 15:05 export | Unverified | Char Dev + Materials requested | **FAIL / navigation-invalid**: user observes alternate skipping; phase log missing | Unknown (181 distinct) | **181 Dev / 0 ordinary Materials** | Unverified | Identical 181 keys/order to 012, 158/177 V3.2 reference adjacent transitions advance by +2; three exported 0 quantities. ZIP byte-identical to 012 (223 PNGs + logs). **Do not score OCR.** |

| 015 / 45ms stick pulse video | 2026-10-22 afternoon | Dev Items | Dev Items + Materials selected in UI | **FAIL**: alternating omission persists with Normal 45ms hold / 250ms neutral | Partial / canceled | No matching GOOD | Video ~14.87s | Video shows skipped item detail cards *are visibly displayed*, e.g. Foreign Synapse / Operative's Constancy / Old Operative's Pocket Watch. Could be two moves per pulse OR capture/recognition timing; do not assume one without synchronized pulse log. `ChaosGear` OCR error seen. |

| 016 / instrumented pulse trial | 2026-10-22 (user-supplied ZIP) | Unknown | Char Dev expected | **INCONCLUSIVE**: evidence from current run not included | Unknown | Not provided | Unknown | `Experiment 16.zip` matches EXP 012/013 byte-for-byte (238 entries, last timestamp 14:35); no `navigation_samples` or `V4 NAV EXP016` log lines. Need correct live process `./logging` directory, not another scan yet. |

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

- Experiment 010 logs: `Experiment 10.zip`; output: `genshinData_GOOD_2026_10_22_12_34.json`; details: [EXP-010 report](experiments/EXP-010-materials-starting-on-artifacts.md).
- 010 navigation: 12:34:46.612 `Artifacts -> Materials: 3 right presses`; 12:34:48.691 unexpected `Food`; 12:34:50.069 recovered and verified `Materials` in one additional step. 12:34:51.588 repeated `MidlanderClaymoreBillet` ended controller scan at 7 items; export contains just 7 materials.
- 010 screenshot crops prove quantity disagreements: `NorthlanderBowBillet` screenshot 2 vs GOOD 4, `MidlanderSwordBillet` screenshot 6 vs GOOD 4. Other five exported quantities match saved screenshots. No verified source of wrong quantities beyond inherited alpha OCR and no verified root cause of repeated material read yet.

- Experiment 011 logs: `Experiment 11.zip`; export: `genshinData_GOOD_2026_10_22_13_08.json`. Full results: [EXP-011 report](experiments/EXP-011-material-ocr-audit-delay-1.md).
- 011 used scanner delay 1 (010 used 0); reported 199 material entries in 61.307 seconds. No assertion of exactly 199 expected inventory items is established by the shared capacity indicator.
- **Manual review of all 199 saved material quantity PNG crops:** 184 agree with their corresponding GOOD values and **15 disagree (92.46% quantity agreement)**. Four mismatches generated parsing-error messages; the other eleven were silently exported with incorrect positive values.

- Experiment 012 files: `Experiment 12.zip` and `genshinData_GOOD_2026_10_22_14_40.json`. Raw archive has **no 14:40 scan log/images**; its **223/223 PNG files exactly repeat Experiment 011**. The 181-key GOOD export covers only Character Development Items and contains a 0 count for `ChaosGear`. Details: [EXP-012 inconclusive analysis](experiments/EXP-012-mixed-scan-incomplete-evidence.md). The actual settings, phase stop reason and OCR quantity accuracy remain unverified.

- Experiment 013: `Experiment 13.zip`, `genshinData_GOOD_2026_10_22_15_05.json`; full report [EXP-013 navigation-invalid](experiments/EXP-013-navigation-invalid-mixed-scan.md). **Same 181 exported item names/order as 012**, eight quantities changed; three 0 counts. The ZIP has the **same 229 file contents as the Experiment 012 ZIP** (all 223 PNGs old), and no 15:05 scan log. Independently checking against historical V3.2 ordering, 158 of 177 comparable adjacent index jumps skip exactly one item. User again reports Materials category not scanned. OCR voting remains untested against fresh saved crops.

- Experiment 015 video: `IMG_2671.mp4` (14.87s phone recording), [video evidence report](experiments/EXP-015-video-evidence-material-navigation.md). Genshin visibly highlights items that EXP 012/013 omitted, showing a *scanner capture-versus-selection mismatch*, not proof that the cursor never visited these cells. Both Dev Items and Materials checked in UI; `ChaosGear` quantity parsing error visible. Distinguish **multiple physical grid advances per command** from **missing/stale OCR sampling** using synchronized per-pulse logging before additional timing adjustments.

- EXP 016 upload: `Experiment 16.zip`. **Exact same ZIP bytes** as EXP 012 and 013. Includes only 223 old Materials quantity PNGs and old NLog files, last modified 2026-10-22 14:35; **no** `Materials/CharacterDevelopmentItems/navigation_samples` images and no instrumented run. Cannot infer pulse movement from this archive. `Program.cs` NLog file targets and `SaveInventoryBitmap` use *relative* `./logging`; the active process's working directory may differ from the folder manually zipped. User should use the running Kamera **error log** link (which opens `logging` relative to the active process) and verify recent screenshot timestamps before zipping. Current-run navigational behavior still unverified.

## Interpretation

The **initial controller tab verification feature** has passed two artifact-only test cases and now has a third *navigation-recovery* success in Materials-only scanning. Experiment 010 exposed an early repeat-triggered stop (7 exported), while Experiment 011 (delay 1) navigated 199 materials but produced 15 screenshot-confirmed quantity errors. A repeat now appeared at the very end, which is consistent with reaching the last item but does not independently prove full coverage. These results do *not* validate complete material inventory coverage, mixed scans, quantity OCR, character roster handling, or absence of occasional controller-selection timing errors. V4 is still experimental. The fully validated V3.2 inventory/farming tracker should not be overwritten with test scans.

## Next experiment

**016 evidence recovery — no new scan yet.** Locate the `./logging` directory for the actual run (open **error log** directly from that same running Kamera app), check for recent `Materials/CharacterDevelopmentItems/navigation_samples/step_01_BEFORE.png` and matching `step_01_AFTER.png`, and ZIP those plus `InventoryKamera.debug.log`. If the captures do not exist, confirm executable/branch/build and screenshot checkbox before retrying the 5–12-step diagnostic. Do not infer navigation result from the stale ZIP.

## Updating this ledger

For each subsequent experiment, record:
1. starting UI tab and requested category;
2. branch/commit, Genshin resolution, configuration;
3. filenames of ZIP log and GOOD JSON;
4. exact log timestamps, scanned/filtered/exported counts;
5. field-level comparisons, errors, and user-observed behaviour;
6. PASS / PARTIAL / FAIL, and next action.

**Unknown user-reported major issue:** User has indicated discovering a new major issue while submitting Experiment 009 but has not yet described its symptoms. Do not speculate or mark as confirmed until details/evidence are supplied.
