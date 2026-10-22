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
