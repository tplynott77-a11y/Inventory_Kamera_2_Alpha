# EXP-018 — Filtered-primary quantity OCR calibration (offline replay)

**Status:** Experimental code and unit tests committed, awaiting live revalidation. **Do not upload GOOD to live tracker.**

## Material-first scope

Characters are deliberately deferred. Preserve EXP-016 pooled-engine alphabet reset, the validated EXP-017 material category navigation, and current analogue controller timing. This work only adjusts the OCR quantity decision policy.

## Ground truth

- Experiment 017: 558 individually saved, 195×44 material quantity crops (359 Character Development Item, 199 ordinary Materials).
- Screenshot quantities manually transcribed from all 558 crops into `Experiment_017_Full_Quantity_Audit.csv` (shared in original investigation; do not publish users' entire screenshot corpus to GitHub).
- Parsed precisely **5,580** `Material OCR ...` debug lines (five filtered/inverted and five grayscale readings per item). The logs contain exactly 558 groups of ten votes in scan order.
- Original double-weighted clean-grayscale voting scored **431/558 (77.24%)** exact. Five uncertain items exported zero; **122 silent nonzero errors**.

## Why the old weighting is wrong for V4 crops

Individual OCR view accuracy against the 558 screenshot truths:

| OCR view | Correct out of 558 |
| --- | ---: |
| Filtered/inverted, 1x | 445 (79.75%) |
| Filtered/inverted, 1.5x | 465 (83.33%) |
| Filtered/inverted, 2x | 460 (82.44%) |
| Filtered/inverted, 2.5x | 464 (83.15%) |
| Filtered/inverted, 3x | 468 (83.87%) |
| Clean grayscale, 1x | 356 (63.80%) |
| Clean grayscale, 1.5x | 416 (74.55%) |
| Clean grayscale, 2x | 390 (69.89%) |
| Clean grayscale, 2.5x | 411 (73.66%) |
| Clean grayscale, 3x | 390 (69.89%) |

Across 139 screenshots where both views produced conflicting majority predictions, the filtered branch matched the screenshot on 115, while the grayscale branch matched on only 9. Example: Crystal Chunk screenshot 25; all five filtered reads 25, all five grayscale reads 29; old rule exported 29. Raw Meat screenshot 704; filtered reads 704 all five, grayscale 104 all five; old rule exported 104.

## Offline policy replay

Replay the exact original vote stream using the same unique-top-winner + two-distinct-read policy and change only the per-view weight:

| Filtered / Grayscale weight | Correct/558 | Silent wrong | Zero / abstain |
| --- | ---: | ---: | ---: |
| 1 / 2 (old live EXP-017) | 431 (77.24%) | 122 | 5 |
| 2 / 1 | 513 (91.94%) | 44 | 1 |
| **3 / 1 (committed EXP-018 candidate)** | **519 (93.01%)** | **37** | **2** |

This is **not a new in-game result**, and **93% is still not reliable enough for the live farming tracker**. Changes are deliberate and isolated so a subsequent short test can establish behaviour.

## Regression tests

The reader still performs ten OCR calls. New `MaterialQuantityVotingTests` reproduce real log contrasts:
- `CrystalChunk`: 25 vs grayscale 29.
- `RawMeat`: 704 vs grayscale 104.
- `ChaosGear`: filtered recovers 356 at three scales; grayscale invents 3906/396.

## Next improvement under investigation

Independent shape confirmation: a lightweight digit glyph reader applied to the saved PNGs gave 549/558 complete quantities correctly in 5-fold held-out-item cross-validation. Requiring its decoded quantity to agree with either existing OCR branch identified **520 correct / 0 silent wrong / 38 uncertain** in that *offline dataset*. This is a **promising prototype, not a committed runtime feature**. It needs careful cross-resolution checks, implementation tests and live verification before deployment. Avoid overfitting one user's 4K UI screenshots.

**Next step:** review the CI for committed 3:1 weighting and tests; only then decide between another controlled in-game test and integrating a separately tested digit-shape verifier. Keep experiment 016 forensic screenshots as optional debug cleanup for later.

## EXP-018 independent glyph confirmation — integrated candidate

**Implementation:** `MaterialDigitTemplates.cs`, `MaterialDigitShapeReader.cs`, `MaterialQuantityVotingReader.cs` on `feature/v4-hybrid`. No other scanner types, pooled OCR settings, or analogue timings were changed.

The shape reader uses a digital-color mask and column segmentation of the original **195×44** screenshot; each digit is normalized to an 8×14 glyph and classified against ten averaged digit silhouettes. Only numeric glyph patterns are embedded; no inventory names or individual screenshot images were committed to GitHub.

**True 5-fold held-out-item validation:** the digit-shape component decoded **557/558** complete quantities (FirmArrowhead 677 under-segmented two adjacent 7s). When additionally requiring **at least two** of ten original OCR predictions to agree with the shape output, **535/558** were accepted and all 535 were correct; **23** were abstentions; **zero silent errors** in this specific offline replay. The 535/0/23 result is more conservative than accepting a single matching OCR reading (540/0/18).

The implementation returns an unreadable/uncertain **zero requiring user review** instead of accepting a nonzero Tesseract count without independent confirmation. It emits diagnostic logs for mismatches. For unit tests evaluating the older weighted voter in isolation, the reader can be constructed with `verifyByShape: false`; the **production default is true**.

**Important limitations:**
- This shape model has been tested only against one user's 4K, 195×44 controller crops. Other screenshot sizes currently reject rather than guess (needs wider-resolution validation).
- The averaged templates were fitted from the labelled EXP-017 crop collection, and cross-validation estimates generalization *within that captured UI style*, not to unrelated graphics or future UI changes.
- Offline replay is not live scanner validation. New EXP-018 capture logs and labelled quantities are needed to verify C# runtime shape extraction reproduces the Python prototype.
- The remaining ~23 flagged counts need a correction workflow or additional recognizer improvements before a full automatic GOOD export is safe for the live tracker.
- The ten OCR passes are still in place; performance optimization comes **after** independently verified accuracy.

**Next live gate:** after CI passes, run a short controlled batch of Character Development Items **and** a short ordinary Materials-only batch (capturing original 195×44 quantities), confirm problematic cases (Chaos Gear, Crystal Chunk, 5↔9, 7↔1), and verify the 23 estimated uncertain slots are surfaced as review-needed. Only then consider a full 558-item benchmark. Character roster remains deferred.
