# EXP-024 — Diagnose missing talents without changing recognition

**Date prepared:** 2026-10-23. **Branch:** `feature/v4-hybrid`. **Status:** SCREENSHOT-DRIVEN FIX CANDIDATES COMMITTED; LIVE EXP-024 NOT YET RUN.

## Baseline (EXP-023)
- The user-provided `genshinData_GOOD_2026_10_22_22_08.json` contains 71 character records, of which 63 have three positive talents.
- Eight have all three talents zero: **Kinich, Mavuika, Yaoyao, Collei, Chiori, Tighnari, YunJin, Gorou**.
- Missing Cryo Traveler; a custom-named mannequin is intentionally excluded.
- All 71 constellation levels were identical between EXP-022 and EXP-023; known corrected examples **Charlotte C4** and **Kirara C5** remain correct. Screenshot-by-screenshot audit pending.
- Do not import this character GOOD into the trusted account database.

## EXP-023 text-log triage — completed 2026-10-23

The user supplied `InventoryKamera.log` (299 lines) and `InventoryKamera.debug.log` (2177 lines) for the 2026-10-22 22:00–22:08 Characters-only EXP-023 run. The logs decisively **separate the eight zero records into two categories**:

| Character | Log observation | Diagnostic classification |
| --- | --- | --- |
| Kinich | 22:07:41 — Talent header expected Kinich but unreadable after 5 attempts | HEADER_FAILURE |
| Mavuika | 22:07:45 — Talent header expected Mavuika but unreadable after 5 attempts | HEADER_FAILURE |
| Yaoyao | 22:08:04 — Talent header expected Yaoyao but unreadable after 5 attempts | HEADER_FAILURE |
| Collei | 22:08:10 — Talent header expected Collei but unreadable after 5 attempts | HEADER_FAILURE |
| Chiori | 22:08:13 — Talent header expected Chiori but unreadable after 5 attempts | HEADER_FAILURE |
| Tighnari | 22:08:15 — Talent header expected Tighnari but unreadable after 5 attempts | HEADER_FAILURE |
| YunJin | 22:08:17–19 — Header read successfully. Combat OCR retried after 1–2 detected rows, then reported 1/1/3; `constellation and displayed talent levels disagree` | CONSTELLATION_TALENT_INCONSISTENCY (not header failure) |
| Gorou | 22:08:20 — Talent header expected Gorou but unreadable after 5 attempts | HEADER_FAILURE |

**Key conclusions:** Seven missed talents were not passed to `ScanTalents` at all; their zeroed outputs were caused by the conservative identity gate, not demonstrated talent-number OCR failure. A shared OCR / phase-state cause is possible but cannot be chosen without examining the forced `diagnostics/Talent_verification_failed` screenshots. YunJin passed identity, then had a raw displayed Burst Lv3 inconsistent with her C3 +3 modifier; do not force-correct an unverified value.

**Traveler is a separate reproducible identity-mapping problem:** the run loaded custom Traveler name `t_2the_homas77`, and the skipped-slot raw OCR read `cryo / t_2the_homas77` exactly (22:01:04). Thus the element was **not** misread as gryo/cryaq this time, and the user-defined name is present in the OCR string. It failed to resolve/validate a canonical playable Cryo Traveler despite those tokens. Check the recognized name and `CharacterMatchesElement` path and the `unrecognized_1.png` screen before changing anything. `farty_pants` is an intentionally excluded mannequin.

**Next screenshot request:** one or two `Talent_verification_failed` crops/full-window screenshots from the seven header failures (ideally Kinich and Chiori), YunJin's `talents.png` and `talents_full_3840x2160.png` if available, and `unrecognized_1.png`. The text logs have already been provided: **do not ask user to search or resend them**. All new EXP-024 diagnostic logging remains not yet exercised live.

## Evidence-first priority: inspect EXP-023 archive before another live scan

EXP-023's `Experiment 23.zip` (~426 MB) was uploaded, but the binary/archive-processing environment was unavailable at audit time. This limitation means the **cause of each zero is still unknown**. Extract and share a much smaller ZIP containing:
1. **The scan's text log(s)** from `logging/` covering the EXP-023 timestamps.
2. For the eight named characters, the files under `logging/characters/<NAME>/diagnostics/Talent_verification_failed*` **if present**, and `logging/characters/<NAME>/talents_unconfirmed*` **if present**. Preserve names and directory structure. Some will have one kind of failure image but not both.
3. `logging/characters/unrecognized_*.png` plus associated logs, to establish what appeared on the Cryo Traveler screen.
4. Optional constellation screenshots for **Charlotte, Kirara, Wanderer, Diluc, Freminet** if the prior screenshot archive is available; avoid changing constellation OCR solely on export discrepancies.
5. No need to re-upload every success screenshot or the full 426 MB archive.

Use the logs to label **each of the eight cases** as: `HEADER_FAILURE` (phase identity OCR/navigation), `TALENT_OCR_FAILURE` (fewer than three `Lv.` rows after retries), `LEVEL_SANITY_REJECT` (impossible low-ascension normal), or `OTHER` (e.g. wrong/corrupt UI position). If logs do not distinguish these, record **UNDETERMINED** and inspect screenshots before making fixes.

## New EXP-024 code instrumentation

**No recognition, navigation, constellation, or material-quantity logic changed.** Only logs/tests:
- On failed identity verification, write a structured `EXP024 HEADER_FAILURE` warning with expected/actual name and element plus bounded raw OCR from the last retry. Keep forced full-window and header captures.
- On each successfully accepted three-row talent read, write `EXP024 TALENT_SUCCESS` with character, attempt number, three detected levels and bounded raw OCR.
- On exhaustive talent-read failure, write `EXP024 TALENT_FAILURE` with attempt count, maximum number of candidate rows seen, impossible-low-ascension rejection count, and first/last raw OCR reads. Keep forced full-window and talent crops.
- Compact OCR diagnostics preserve original row boundaries as ` | `, cap the log field at 440 characters and are **never used to accept/reject an OCR result**. Formatter has regression unit tests.

## Screenshot inspection and fixes prepared — 2026-10-23

The user provided seven EXP-023 images: `unrecognized_1.png`, `unrecognized_2.png`, two name-header verification crops, two matching full-window Talents screenshots (Kinich, Chiori), and `talents.png` from Yun Jin.

### Direct visual facts

- `unrecognized_1.png`: the header is plainly **Cryo / T_2the_homas77** and the avatar is actual male Traveler, level 90/90. This is **not** missing on-screen text.
- `unrecognized_2.png`: **Cryo / Farty_Pants** is the named mannequin. Its separate exclusion is expected; do not map it to Traveler.
- Kinich's failure crop/full screen: **Dendro / Kinich** is legible; the **Talents** sub-tab is actively selected and combat talents display **7 / 8 / 8**.
- Chiori's failure crop/full screen: **Geo / Chiori** is legible; the **Talents** sub-tab is actively selected and combat talents display **1 / 1 / 1**.
- Yun Jin's cropped combat talent rows display **Normal 1 / Skill 1 / Burst 4 (blue, C3 boosted)**. The log's earlier accepted **1 / 1 / 3** is wrong and should be retried; never silently coerce 3 to 4.

**Interpretation:** The Kinich/Chiori screenshots rule out navigational mismatch in these instances. The seven `HEADER_FAILURE`s are likely caused by the existing fixed 110-threshold OCR / strict slash-based header parser on colorful talent backgrounds. The precise failure mechanism cannot be proved from screenshots alone; the EXP-024 raw OCR diagnostic logger will give the missing discrimination. Traveler's raw screenshot and log confirm its custom name/element, so validation against the local `characters.json` may reject Cryo if the element list is stale.

### Candidate source changes (all must still pass a live run)

1. **Exact-match header rescue, only in Phase 2/3 verification:** after the normal five-attempt check fails, separately OCR the header with thresholds 175/210 (SingleLine and Auto) and accept it **only if the cleaned full header exactly equals expected element + on-screen display name**. Custom names are resolved from the current `Characters` database. This does not accept a wrong character solely because the roster cursor should have been on them.
2. **Cryo Traveler recognition:** allow `CharacterMatchesElement("Traveler", "Cryo")` as a **screenshot-confirmed narrowly scoped exception** for a stale version-7.1 database, while leaving mannequin and other-character element validation intact and null-safe.
3. **C3/C5 talent candidate guard:** lookup each character's `ConstellationOrder`. If the scanned raw level of a talent that is supposed to have +3 is below 4, **retry the OCR** rather than accepting, subtracting three, or inventing a repaired level. Handles Yun Jin's mistaken Burst Lv.3 without hardcoding Burst=4. Pyro Traveler's unusual constellation inference stays excluded from this check.
4. **Regression unit tests:** enforce exact header identity, Traveler-only Cryo exception, and C3/C5 candidate acceptance/rejection based on the original screenshots.

**Scope:** The EXP-020 materials archive, Materials code, Artifact/Weapon scanners, and constellation recognition are unchanged. These are candidate improvements, not proven live fixes. The EXP-024 evidence log remains enabled.

## If new live evidence is needed

1. Build from `feature/v4-hybrid` **after CI succeeds**, not from the EXP-020 archived branch.
2. Use **3840 × 2160**, in-game free-roam, **Characters only**, controller **Normal** speed, **Log Screenshots ON**, and no experimental Fast mode.
3. If a small targeted sample can be set up by selecting the troublesome characters in the roster without breaking cursor assumptions, prefer that. Otherwise run one complete roster pass **only if EXP-023 evidence is insufficient**; do not force a fourth 8-minute scan needlessly.
4. Keep the new run's text log and crop evidence, GOOD export and branch SHA together.
5. Examine the `EXP024 HEADER_FAILURE`, `EXP024 TALENT_FAILURE`, `EXP024 TALENT_SUCCESS` messages and map them to specific screenshots. No blanket OCR rewrite.

## Pass/fail criteria

- At minimum, every targeted previously-zero character must have *visually verified* nonzero combat talents; no “0” or negative talent exported as a real level.
- Cryo Traveler must be properly recognised **by element and character identity**; no unconstrained fuzzy matching to hide OCR mistakes.
- Charlotte C4 / Kirara C5 and other constellation reads must remain screenshot-correct.
- All talent levels need to be checked **after** GOOD's C3/C5 +3 adjustment and team passive correction (Dahlia and Candace C6 correctly display 1/4/4 but export 1/1/1).
- Never overwrite the verified materials snapshot (`archive/exp020-materials-validated-2026-10-22`, commit `42dfe6a5b24c7b1941fbc69504b0266851fda03d`).
- EXP-024 remains **PENDING / diagnostic** until actual screenshot evidence and a follow-up GOOD export confirm the behaviour.
