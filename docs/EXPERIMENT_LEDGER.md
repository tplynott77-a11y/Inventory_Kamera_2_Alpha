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

| 016 / actual paired screenshots | 2026-10-22 16:26–16:27 | Char Dev Items | Dev Items + Materials UI enabled | **PASS navigation**: 12/12 commands exactly one visible grid step; **FAIL name OCR**: 5 of first 12 recognized as empty despite legible crops | 12 instrumented selections; user canceled after 23 accepted names | N/A (canceled) | 16:26:48–16:27:14 | ROOT CAUSE found: pooled Tesseract digit-only whitelist persists into later item-name reads; repaired in `OcrService.cs` with regression test. See [EXP-016 report](experiments/EXP-016-diagnosis-and-ocr-whitelist-fix.md). |

| 017 / material category & crop audit | 2026-10-22 16:49–16:55 | Char Dev verified | Char Dev + Materials | **PASS both categories:** 359 Dev; then verified Materials and scanned 199; both stop on end-repeat (not cancelled) | **558 accepted** | **558 GOOD keys** | Dev ~196 s; Materials ~110 s | **FAIL quantity OCR:** full 558-crop manual audit: 431/558 correct (**77.24%**); 127 mismatches (5 uncertain zeros and 122 silent). [EXP-017 report](experiments/EXP-017-material-crop-audit.md). Do not import to live tracker. |

| 018 / filtered-primary vote replay | 2026-10-22 (offline) | No new game run | Dev + Materials vote logs | **OFFLINE RULE VALIDATION ONLY**: 3:1 filtered/gray vs prior 1:2 | Replay 558 crops, 5,580 OCR reads | No new GOOD | No new phase | 3:1 weight replay: **519/558 (93.01%)** correct vs 431/558 (77.24%) old; still 37 silent wrong and 2 zero; unfit live tracker. [EXP-018 report](experiments/EXP-018-filtered-primary-quantity-voting.md) |

| 018 / short live validation | 2026-10-22 17:49–17:49 | Materials → Char Dev verified | Dev + Materials checked; cancelled in Dev | **PASS short Dev sample:** 31 sequential names, 31 quantities consistent with captures/votes, 0 review warnings; not a full GOOD test | 31 Dev | N/A (cancelled) | ~21s actual Dev scanning | 310 OCR readings audited; replay old 1:2 policy would have scored 22 correct, 8 wrong, 1 uncertain vs new 3:1 31/31. Shape guard did not trigger a mismatch on this sample; ordinary Materials untested this run. |

| 019 / full mixed Materials with shape verifier | 2026-10-22 17:57–18:03 | Char Dev, tab OCR verified | Dev + ordinary Materials | **PASS BOTH**: 359 Dev then 199 Materials, cancelled=False | **558 distinct** | **558 GOOD keys** | Dev ~198 s; Materials ~110 s | **534/558 correct (95.70%)** across all visually checked crops; **0 wrong nonzero accepted**; 24 REVIEW_REQUIRED exported 0 despite positive screenshots (11 Materials, 13 Dev); 17 shape-based corrected values. [EXP-019 report](experiments/EXP-019-full-material-quantity-audit.md) |

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

- **EXP 016 actual evidence recovered**: `Experiment 16 actual.zip` contains 12 BEFORE/AFTER full-window pairs, 12 original OCR name crops, and the 16:26 diagnostic log. **12/12 movements selected exactly one adjacent item**, including row wrap. OCR name results empty for **Black Crystal Horn, Heavy Horn, Dead Ley Line Leaves, Chaos Core, Chaos Device** (slots 3, 5, 7, 9, 11), while nameplate images are legible. Source inspection identifies **persistent `tessedit_char_whitelist=0123456789`** set by quantity reads in `OcrService.AnalyzeTextWithConfidence` on reusable pooled Tesseract engines; non-digit reads never cleared it. Reset per OCR call; add one-engine regression test. [Detailed root-cause report](experiments/EXP-016-diagnosis-and-ocr-whitelist-fix.md). Original EXP-016 ZIP from prior ledger entry was stale but superseded by actual ZIP.

- **EXP 017 audited:** `Experiment 17.zip` and `genshinData_GOOD_2026_10_22_16_55.json` contain current logs, **359 Char Dev** and **199 ordinary Materials** OCR/name scans, both without cancellation; GOOD holds **558 keys**. 558 individual cropped quantity screenshots all reviewed against GOOD: **157/199** ordinary Material counts and **274/359** Char Dev counts agree; **431/558 (77.24%)** overall. 127 wrong (122 silent, 5 zero from uncertainty). Major error examples: `ChaosGear` 3906 instead of crop 356, `RawMeat` 104 instead of 704, and `RainbowdropCrystal` 91 instead of 37. See [EXP-017 detailed audit](experiments/EXP-017-material-crop-audit.md). Historical V3.2 565-material baseline overlaps on 550 names and has 15 older-only (including Mora) and 8 current-only names; not definitive coverage ground truth.

- **EXP 018 offline policy replay**: all 5,580 raw OCR predictions from 558 EXP-017 screenshot crops parsed and recomputed with the original voting policy. All filtered/inverted scales outperform clean grayscale individually; 3:1 filtered/clean weight increases screenshot-matching reconstructed exports from **431 to 519 / 558**, leaving **37 silent errors and two uncertainty zeros**. Committed isolated voter calibration + real-stream regression tests to `feature/v4-hybrid`. See [EXP-018 report](experiments/EXP-018-filtered-primary-quantity-voting.md). This is **not live measured improvement**; OCR remains below release standards. A shape-based independent verifier achieved 520 correct / 0 wrong / 38 uncertain in offline cross-validated combination; not integrated in runtime yet.

- **EXP-018 shape verifier implementation:** `MaterialDigitShapeReader.cs` + embedded average digit silhouette templates; production quantity reader now requires **shape/OCR agreement with at least two numeric OCR reads**, otherwise exports zero and logs *REVIEW REQUIRED*. Five-fold held-out-item evaluation on EXP-017 crops: raw shape reading **557/558** correct; combined guard accepts **535 correct / 0 wrong / 23 uncertain** (offline only). It is intentionally limited to 195×44 controller crops and must pass a new live test before trusted use. See [EXP-018 report](experiments/EXP-018-filtered-primary-quantity-voting.md).

- **EXP-018 short live scan**: `Experiment 18.zip` is fresh (17:49 logs and 31 Character Development Item quantity PNGs). The user selected both Dev Items and Materials but cancelled at 17:49:49 before category handoff. Logged 31 selected/accepted material names and 310 OCR reads, including all previously missing item names. **31/31 crop figures agree with filtered OCR and 3:1 reconstructed vote**, no `REVIEW REQUIRED`, `shape corrected`, or failed-parse messages. Old 1:2 rule would have given 22 correct, 8 wrong, 1 uncertain on the same OCR readings; new 3:1 rule gives 31 correct, 0 wrong/uncertain. Because the verifier did not encounter an actual disagreement, this does **not** prove zero future false accepts; no GOOD JSON was exported due to cancellation.

- **EXP-019 full live forensic audit:** User's `Experiment 19.zip` and 18:02 GOOD include all 558 matched screenshots and logs. **534/558 export quantities match screenshot** and **all 534 accepted nonzero numbers match**; the other **24 are flagged and exported 0**, not genuine zero ownership. 17 shape correction events; 24 REVIEW warnings. Shape predictions of 23 rejected items were actually correct but rejected because fewer than two OCR reads agreed; one rejected `FirmArrowhead` shows **677** with shape wrongly predicting 65 while OCR saw 677. Do not simply trust shape alone. [EXP-019 report](experiments/EXP-019-full-material-quantity-audit.md).

## Interpretation

The **initial controller tab verification feature** has passed two artifact-only test cases and now has a third *navigation-recovery* success in Materials-only scanning. Experiment 010 exposed an early repeat-triggered stop (7 exported), while Experiment 011 (delay 1) navigated 199 materials but produced 15 screenshot-confirmed quantity errors. A repeat now appeared at the very end, which is consistent with reaching the last item but does not independently prove full coverage. These results do *not* validate complete material inventory coverage, mixed scans, quantity OCR, character roster handling, or absence of occasional controller-selection timing errors. V4 is still experimental. The fully validated V3.2 inventory/farming tracker should not be overwritten with test scans.

## Next experiment

**020 — Safe recovery of 24 flagged quantities.** EXP-019 all-558 audit confirms 534 accepted correct, no accepted silent errors in this scan, but 24 uncertain zeros. Improve fallback to discriminate confidently segmented digits from OCR disagreements and safeguard against `FirmArrowhead` 677→shape 65 under-segmentation. Validate offline against both EXP-017 and held-out EXP-019 screenshot corpora before asking user for another full run. Continue deferring characters and performance changes. Do not import EXP-019 experimental GOOD into live tracker.

## Updating this ledger

For each subsequent experiment, record:
1. starting UI tab and requested category;
2. branch/commit, Genshin resolution, configuration;
3. filenames of ZIP log and GOOD JSON;
4. exact log timestamps, scanned/filtered/exported counts;
5. field-level comparisons, errors, and user-observed behaviour;
6. PASS / PARTIAL / FAIL, and next action.

**Unknown user-reported major issue:** User has indicated discovering a new major issue while submitting Experiment 009 but has not yet described its symptoms. Do not speculate or mark as confirmed until details/evidence are supplied.
