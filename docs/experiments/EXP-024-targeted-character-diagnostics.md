# EXP-024 — Diagnose missing talents without changing recognition

**Date prepared:** 2026-10-23. **Branch:** `feature/v4-hybrid`. **Status:** DIAGNOSTICS COMMITTED; LIVE EXP-024 NOT YET RUN.

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
