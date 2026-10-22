# Experiment 012 — Mixed Materials + Character Development Items: incomplete evidence

**Date:** 2026-10-22, GOOD export timestamp 14:40 (Europe/Dublin). **Intended test:** V4 weighted two-view OCR, Materials + Character Development Items, normal controller delay. Actual scan settings/phase navigation **cannot be verified** because the supplied ZIP does not contain the 14:40 scan log.

## User-provided files

- `genshinData_GOOD_2026_10_22_14_40.json`: 181 keys in the GOOD `materials` object, all recognizable as Character Development Item keys; no ordinary Materials entries (e.g. the previously scanned fishing bait, wood or produce) in this export.
- `Experiment 12.zip`: 238 ZIP entries. **All 223 PNG screenshots are byte-for-byte identical to the PNG files in `Experiment 11.zip`**. The only changed entries consist of two active startup-only logs and updated logs in the archive directory.
- ZIP archives include the 13:07–13:09 Experiment 011 material scan, startup logs beginning 14:34:53 and 14:35:42, but **no 14:40 scan phase** and **no fresh character-development quantity PNG crops**.
- The JSON and ZIP therefore cannot be treated as evidence of the same scan. Search for the `logging` directory used by the latest Visual Studio instance, distinguished by log entries around 14:40 and PNG timestamps around that time. The configured GOOD output path may differ from the process working directory.

## Data observations (not an accuracy benchmark)

- `ChaosGear: 0` in the GOOD JSON. The V4 voting reader returns 0 on no independent weighted OCR consensus, so this is an **unknown/failed count**, not a validated quantity of zero. The scraper logs a parsing error but currently still exports that zero.
- On comparison with the *historical, seven-days-earlier* `genshinData_GOOD_2026_10_15_22_33.json`, 178 material keys overlap with the current 181: 82 have exactly matching quantities and 96 differ. These differences must **not** be scored as OCR errors: user inventory may have changed and there are no matching new screenshots. In particular, `ChaosGear` was previously 356; the current zero is a processing failure or missing consensus, but its actual present quantity is unverified.
- The 199-item Experiment 011 Materials screenshot agreement rate (184/199, 92.46%) **cannot** be compared against Experiment 012 until the fresh captures are located.
- Missing ordinary Materials in GOOD may mean the category was not selected, a phase failed, or an intentional early termination. Without the scan-phase log, the cause is unknown.

## Independent source-level issue confirmed and patched

The original controller `MaterialScraper.ScanMaterials` unconditionally called `inventory.Materials.Contains/Add` for **both** inventory pages, leaving `Inventory.DevMaterials` unused. This is a clear bookkeeping bug but does **not alone** explain the missing ordinary Materials in this export. The V4-only patch now routes each page into its respective collection and tests that separation. GOOD still merges both via `Inventory.AllMaterials`.

Logging: `InventoryScraper.SaveInventoryBitmap` previously put both material-category screenshots in the same `./logging/Materials/` directory; the new V4 branch places Character Development Item captures under `./logging/Materials/CharacterDevelopmentItems/` and leaves normal Materials captures under `./logging/Materials/`. Logging directory cleanup still removes the shared Materials parent.

A separate old UI formatting regression had an existing test expecting LF after the Windows output switched to CRLF. This was corrected; unrelated to quantity recognition.

**No change** to controller input delays, the weighted OCR/voting algorithm, the alpha artifact lock classifier, original V2 alpha branch, V3.2 stable branch, or the live farming tracker.

## Verdict and next data need

**INCONCLUSIVE:** a 181-key Development Item GOOD export exists, but there is no matching run log or screenshot set. Do not treat this as a successful mixed scan or as an OCR-accuracy test. Preserve the incomplete GOOD exclusively for debugging. Reinspect the actual 14:40 logs and screenshots before further recognition changes; explicitly confirm both checkbox states from `Scan settings` in those logs.
