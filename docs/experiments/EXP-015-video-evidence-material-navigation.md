# EXP 015 — Analogue-timing trial: video evidence and navigation mismatch

**Date:** 2026-10-22, user-submitted phone video `IMG_2671.mp4` (14.87 seconds, 910 × 512 pixels, 30 fps).

**Status:** FAILED navigation validation; STOP. Do not score OCR or import a partial export.

## Configuration

- User retried V4 Character Development Item navigation using the Normal-speed timing change introduced immediately after cancelled EXP 014.
- Source at commit `7c413581` includes **45 ms analogue-stick hold / 250 ms neutral** for Normal material-grid steps; Fast/Slow unaffected. The build was CI-green, but the video alone does not prove which build executable was launched.
- Video's app UI visibly shows both **Dev Items** and **Materials** checked.
- Screen captures in the video also show `Failed to parse quantity for ChaosGear`.

## Frame-by-frame observation

The game **visibly highlights and displays item cards for names that are absent in earlier EXP 012 and 013 GOOD exports**:

| Approx. video time | Genshin detail-card heading | Present in 012/013 exports? |
| --- | --- | --- |
| 0.0 s | Drop of Tainted Water | not compared |
| 0.4 s | Alien Life Core | yes |
| 1.0 s | Foreign Synapse | no |
| 1.8 s | Rift Core | yes |
| 2.2 s | Operative's Constancy | no |
| 2.6 s | Operative's Standard Pocket Watch | yes |
| 3.2 s | Old Operative's Pocket Watch | no |
| 3.6 s | Chasmlight Fin | yes |
| 4.2 s | Lunar Fin | no |
| 4.6 s | Feathery Fin | yes |
| 5.0 s | Still-Smoldering Hilt | no |
| 5.4 s | Splintered Hilt | yes |
| 5.8 s | Ruined Hilt | no |
| 6.4 s | Sigil of a Striding Will | yes |
| 7.0 s | Locus of a Clear Will | no |
| 7.4 s | Shard of a Shattered Will | yes |

These observed title transitions establish that the cursor **visibly passes through** alternate items. It is incorrect to conclude that Genshin never highlights them; the failure is that the scanner is missing them at the point it records/exports recognized items. Two hypotheses remain:

1. **One analogue pulse produces two UI selection advances**, with the scanner capturing only the later card.
2. **Two separate input/capture cycles occur but one item's reading is skipped/rejected/delayed**, due to OCR, scheduling, or selection-to-image synchronization.

A video of screen output **without synchronized per-pulse input log timestamps cannot distinguish these mechanisms**. The 45/250 ms timing adjustment did not visibly resolve the alternating omission in this experiment.

## Other observations

- The visible UI checkbox states rule out “user forgot to select Materials” **for the moment shown**. They do not prove the later Materials phase executed; the run was stopped during Development Items.
- `ChaosGear` quantity OCR failure recurred. It is a separate symptom and not yet a quantity-accuracy score.
- Video alone does not supply the true `./logging` directory or correspondingly timestamped screenshots; old archived ZIPs for EXP 012 and 013 were stale.

## Recommended next diagnostic

**Do not repeat a full scan or blindly retune pulse duration.** Run an instrumented 5–10-transition diagnostic logging the *exact command timestamp* for each individual right pulse, the nameplate immediately before and after, and the screenshot of the selected grid cell. Compare one commanded pulse against observed cursor transitions, including an intermediate post-release read if needed. If one pulse advances twice, fix the controller movement generation. If exactly one pulse advances one item, fix the name/OCR/capture acceptance path. Retain original stable and alpha branches and leave material OCR voter unchanged until navigation evidence is conclusive.
