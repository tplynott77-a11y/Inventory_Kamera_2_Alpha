# Experiment 011 — Materials-only at Scanner Delay 1

**Date:** 22 Oct 2026 (Ireland). **Resolution:** 3840×2160. **Build:** V4 .NET 10 experimental preview. **Scan settings:** Materials only, delay **1**, screenshot logging enabled.

## Evidence

- Raw archive: user-uploaded `Experiment 11.zip`, 238 ZIP members including 199 individual material quantity PNG screenshots.
- GOOD: `genshinData_GOOD_2026_10_22_13_08.json`.
- At 13:07:50.569 material scan started.
- At 13:07:53.255 V4 **verified the current tab was Materials**, with no tab switch required.
- At 13:08:51.876 material scan stopped at a repeated `BitterPufferfish`, reporting **199 scanned, cancelled=False, stopped=False**.
- Export completed at 13:08:58.900, containing exactly **199 distinct material keys**.
- Material scanning phase took **61.307 seconds**. Full run including navigation/setup/export was longer.
- This is a dramatic improvement over Experiment 010's 7 exported materials at Scanner Delay 0, but a single data point does **not** prove the faster timing caused the premature stop.
- The last duplicate may represent an end-of-grid boundary or a controller misstep. The run does not definitively prove full inventory coverage. The 948/2600 indicator in the saved inventory screenshot is a shared total, **not** the 199-item category count.

## OCR audit: 199 quantity screenshots vs exported JSON

The 199 `logging/materials/quantity/*_Quantity.png` crops were visually reviewed against the matching GOOD-key values (one crop for each exported key). **184/199 match (92.46%), 15/199 mismatch (7.54%).** This measures only numerical accuracy *relative to captured quantities*, not material-name identification or completeness.

| Material key | Screenshot | Exported |
| --- | ---: | ---: |
| NorthlanderBowBillet | 2 | 4 |
| BorderlandClaymoreBillet | 2 | 4 |
| SandGreasePupa | 6 | 0 |
| RomaritimeFlower | 57 | 1 |
| LumidouceBell | 12 | 14 |
| QuenepaBerry | 27 | 14 |
| Salt | 101 | 1 |
| Rice | 6 | 0 |
| EelMeat | 2 | 4 |
| SpinelFruit | 17 | 11 |
| Butter | 6 | 0 |
| PineWood | 6 | 0 |
| Fabric | 2 | 4 |
| RedrotBait | 5 | 1 |
| FalseWormBait | 1109 | 11102 |

The scanner explicitly logged failures to parse SandGreasePupa, Rice, Butter, PineWood; the **other eleven incorrect quantities were silently accepted**. These screenshots remain the evidentiary baseline for an upcoming V3.2 OCR transplant. The cropped numbers generally appear visibly legible in the saved images. The original alpha fast-path for numeric OCR can accept a parsable yet wrong interpretation.

## UI observation

Current V4 `ScanViewModel.SetMaterial` overwrites a single `materialText` value (name/count) and two image snapshots. `MainForm.OnMaterialChanged` renders only the latest material; the UI does **not** accumulate a scan history. This explains why the user could not conveniently inspect ongoing OCR accuracy from the UI.

A useful V4-only quality-of-life improvement would be a scrollable scan audit with item number, matched name, exported numeric value, saved crop thumbnail/zoom and explicit low-confidence errors. Do not conflate UI changes with scan correctness. Maintain stable and official-alpha branches unchanged.

## Verdict

- **PASS (limited):** no immediate repeated-item termination; verified Materials tab; 199 entries traversed at delay 1.
- **FAIL:** 15 of 199 quantity values disagree with their own screenshots.
- **UNVERIFIED:** complete 199-item category coverage; correct names/identity for all 199; extrapolation to Character Development Items.
- **NOT APPROVED:** live farming tracker updates from this GOOD export.
- **Next:** Port V3.2 numeric OCR verifier; add bounded repeat/movement confirmation and explicit incomplete-scan status; improve UI feedback in separate work.
