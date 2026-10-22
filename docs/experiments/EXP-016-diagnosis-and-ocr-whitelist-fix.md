# EXP 016 — Root cause found: digits-only Tesseract whitelist leaked into item-name OCR

**Date:** 2026-10-22; actual evidence upload `Experiment 16 actual.zip`.

## Evidence and verdict

- **Navigation PASS:** A pair of full-window images (`step_01_BEFORE/AFTER.png` through `step_12_BEFORE/AFTER.png`) confirms that each of **12/12 controller commands selected exactly the next grid item**. No skipped highlighted cells, including the first row wrap (Chaos Circuit → Chaos Device).
- **Item-name OCR FAIL:** Every screenshot of the 12 selected nameplates (`slot_01_r0_c0.png` through `slot_12_r1_c1.png`) shows readable text. The diagnostic log nevertheless recorded five empty matched names (slots 3, 5, 7, 9 and 11). Thus items were lost **in name OCR or matching before quantity OCR**, not through controller double-steps.
- The latest diagnostic log is `logging/archives/InventoryKamera.20261022162721.debug.log`; actual scan starts at 16:26:32, verified Char Dev tab at 16:26:48.620, and ends cancelled at 16:27:14.497 after 23 accepted material names. The 45 ms / 250 ms Normal controller setting worked for the instrumented 12 steps. Scanning 23 accepted names is **not** 23 visited selections.
- Full game resolution was 3840×2160 (recorded by navigation log). The archived PNG captured screenshots are 1920×1080 stored (capture pipeline scales output), yet nameplate 550×61 crops are crystal clear.
- The UI settings log explicitly confirms **Dev Items=True and Materials=True**; because user cancelled the run, absence of ordinary Materials is not evidence that the phase fails to start on a complete scan.
- Archive also contains an earlier canceled 15:45 run using the uninstrumented V4 navigation logging. The definitive full-window diagnostic run is 16:26.

## Visual sample sequence and logged recognition

| Selected slot | Screenshot detail card | Recorded name |
| --- | --- | --- |
| 1 | Hero's Wit | `HerosWit` |
| 2 | Adventurer's Experience | `AdventurersExperience` |
| 3 | Black Crystal Horn | **empty** |
| 4 | Black Bronze Horn | `BlackBronzeHorn` |
| 5 | Heavy Horn | **empty** |
| 6 | Ley Line Sprout | `LeyLineSprout` |
| 7 | Dead Ley Line Leaves | **empty** |
| 8 | Dead Ley Line Branch | `DeadLeyLineBranch` |
| 9 | Chaos Core | **empty** |
| 10 | Chaos Circuit | `ChaosCircuit` |
| 11 | Chaos Device | **empty** |
| 12 | Mist Grass Wick | `MistGrassWick` |

## Source-level root cause

`InventoryKamera/scraping/OcrService.cs` uses a shared pool of **12 TesseractEngine instances**, reused via `BlockingCollection<TesseractEngine>`. Before the fix, `AnalyzeTextWithConfidence` did the following:

```csharp
if (numbersOnly) e.SetVariable("tessedit_char_whitelist", "0123456789");
```

It **never cleared** the digit-only whitelist when the same pooled engine was borrowed for a subsequent `numbersOnly: false` item-name call. A `TesseractEngine` preserves `SetVariable` state across calls. The V3.2 quantity voter introduced **ten** digit-only OCR calls for every recognised item, rapidly contaminating most of the pool. Consequently ordinary item names become empty on reused whitelisted engines, producing the apparently alternating missing items.

The screenshot, debug log and source behaviour independently support this mechanism. The specific underlying pattern of engine checkout under any concurrent OCR traffic may vary; do not assume that all alternation patterns imply the same engine index.

## Fix committed

- Changed `OcrService.AnalyzeTextWithConfidence` to **set the whitelist on every checkout**: digits only if `numbersOnly`, otherwise **empty/unrestricted**.
- Ensured engine is always returned to pool via `finally`, even if OCR fails.
- Added integration regression test with **one forced pooled engine**: read numeric quantity first, then read a material-like alphabetic name, and confirm it still recognizes letters.
- **Scoped fix:** shared OCR state reset only; material controller still uses 45 ms hold/250 ms neutral at Normal, and the weighted quantity voting logic remains untouched.
- This root cause diagnosis supersedes the previously presumed double-step explanation in EXP 012–015; whether other navigation issues exist outside the first 12 steps or in other tabs remains to be tested.

## Next experiment

**EXP 017 — Validate whitelist reset, not controller retune.** Pull/rebuild V4 after CI passes, close extra Kamera processes, enable fresh screenshots, and scan Development Items first. Verify first 12: Black Crystal Horn, Heavy Horn, Dead Ley Line Leaves, Chaos Core, Chaos Device must now be recognized. If first 12 pass, continue enough to test a row wrap and the transition to ordinary Materials. Keep the incomplete GOOD export out of the live tracker.
