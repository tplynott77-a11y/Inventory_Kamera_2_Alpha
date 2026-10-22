# Inventory Kamera V4 — Hybrid engineering branch

Status: **experimental, not validated in-game**  
Start: 2026-10-22  
Branch: `feature/v4-hybrid` (forked from `test/official-v2-alpha` at `ba202806c61abd896944df53ea650deeb51b42d7`).

## Intent

Make a personal, reliable 4K Genshin Impact scanner using:

- A modern .NET application and existing alpha controller/capture/worker infrastructure;
- Controller-first input (ViGEmBus already installed locally), not mouse-first merely for distribution convenience;
- The V3.2 stable material OCR's weighted-vote and digit-shape verifier **after** screenshot crop calibration;
- Fast alpha artifact scanning, with its **existing lock classifier retained unchanged initially**;
- Verified, self-correcting navigation that must reject uncertain inventory tabs instead of silently exporting the wrong category;
- Reliable character roster traversal / constellations / talents, using prior known-good scans as fixtures;
- Separate optional keyboard/mouse mode as a longer-term fallback, not mixed while a controller-driven scan is active.

## Benchmarks and ground truth

- V2 alpha artifacts: 285 entries scanned in **33.083 s**, 276 exported; background workers continued after navigation.
- V3.2 stable artifacts: 285 entries scanned in **110.776 s**, 276 exported.
- **276/276 identical non-lock artifact signatures**, including equipped character and all stats, across the two exports dated 2026-10-22. This is scanner agreement, not independent in-game ground truth.
- Locks: alpha reports 108 true, stable reports 4 true; alpha has some image-confirmed false negatives. **Do not change alpha locking in milestone 1**.
- V3.2 material baseline: two independently agreeing 558-item material exports at delay 0 and 1; approximately 422.7s / 486.7s, respectively.
- V2 alpha failed to cover 946 materials, exported only 25 materials and one character in the mixed run; quantity screenshot/output disagreements are confirmed.
- User observed the alpha interpreting Artifacts as Weapons when Genshin inventory opened on Artifacts; materials-only scanning must work from any starting tab.

## Milestones

1. **Branch isolation + modern build foundation** (in progress): .NET 10 SDK-style build and branch-only GitHub Actions; preserve stable / alpha branches.
2. **Controller navigation verification** (initial code committed, game testing outstanding):
   - OCR tab names with confidence filtering and repeated reads;
   - use the old phase position only as a hint;
   - validate the destination after switching;
   - recover by bounded tab sweeps if necessary;
   - fail closed and record a screenshot if target cannot be verified.
   - Future: add independent category-content recognition (e.g., verified billet icons, reference weapon names) to avoid stable-but-wrong OCR.
3. **Precision OCR transplant**: extract V3.2 quantity verifier as independent bitmap component; calibrate controller crop and test saved images before any live inventory import.
4. **Character-state repair**: separate button movement bugs from OCR failures; regression-test full roster and talent/constellation alignment.
5. **Full validation and performance gating**: compare outputs at identical filter settings; preserve full logs and screenshots; prevent incomplete/invalid runs from silently being treated as verified.

## Initial live test checklist (after CI passes)

- Controller manually selected in Genshin; alpha backend creates the virtual pad at scan time.
- At 3840x2160, request **Artifacts only** while inventory was last left on Weapons, Artifacts, and Materials.
- Request **Materials only** from each of those three starting tabs; first confirm tab correctness, not OCR quantity accuracy.
- Switch between selected inventory categories in a full run. Reject any case where an Artifacts card gets parsed as a Weapon card.
- On a deliberate navigation failure, verify scan aborts with `navigation/unverified_inventory_tab.png` rather than continuing into the wrong category.
- **Do not overwrite the live farming tracker or treat partial GOOD output as verified.**

## Scope and status caveats

This branch is a work-in-progress. Initial navigation verification is implemented but **not yet compiled or validated in-game** as of the first commit. The alpha's character navigation and material crop/OCR issues remain unresolved. The lock detector is intentionally unchanged.
