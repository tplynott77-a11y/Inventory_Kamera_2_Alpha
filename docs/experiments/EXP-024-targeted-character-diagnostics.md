# EXP-024 — Diagnose missing talents without changing recognition

**Date prepared:** 2026-10-23. **Branch:** `feature/v4-hybrid`. **Status:** EXP-024 LIVE RUN ANALYSED; 70/71 SCANNED CHARACTERS' TALENTS RECOVERED; CANONICAL TRAVELER IDENTITY FIX COMMITTED, AWAITING LIVE CONFIRMATION.

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

## Live EXP-024 run, 2026-10-23 09:29–09:37 — logs + GOOD audit

**Files:** `InventoryKamera(1).log` (370 lines), `InventoryKamera.debug(1).log` (2281 lines), `genshinData_GOOD_2026_10_23_09_37.json`. Characters-only, 3840×2160, game data 7.1.

- **71 exported character records; 70 have three positive exported talents.** All eight EXP023 zeros are repaired: Kinich 7/8/8, Mavuika 6/6/6, Yaoyao 1/2/1, Collei 1/1/1, Chiori 1/1/1, Tighnari 1/1/1, Gorou 1/1/1, YunJin 1/1/1 (raw C3-boosted Burst Lv.4, properly discounted to Lv.1).
- **Nine `EXP024 HEADER_ALTERNATE_ACCEPT` successes**, including TravelerCryo's constellation verification; Faruzan constellation; YumemizukiMizuki, Kachina, Fischl, Chevreuse, Ningguang, Sucrose, Yanfei talents. Other previously failing headers worked on the primary path this run. **One `EXP024 HEADER_FAILURE`** for TravelerCryo at Talent phase; no `TALENT_FAILURE` and no `TALENT_BOOST_RETRY` messages.
- **Root cause of Traveler 0/0/0: canonical identity compared to variant export name.** At 09:36:06, primary reader correctly recognised `currentName=Traveler`, `currentElement=Cryo`, but the verifier expected `character.NameGOOD=TravelerCryo`, so exact string comparison failed. The alternative raw read did not get the exact custom display name and could not rescue the same screen. This is a **verification identity mapping bug**, not a missing Cryo form and not a demonstrated talent OCR issue. The Constellation verification had succeeded with an alternate OCR `Cryo / T_2the_homas77` at 09:30:32.
- **Mika is the single non-mannequin Attributes skip**, raw OCR `cryo./ mika a` after all 20 retries at 09:30:08. Kirara was independently recognised at 09:30:20 and passed C5/talents. The user subsequently reported a separate rerun where Mika passed; the rerun logs/GOOD are not part of this first EXP024 evidence set. Do not change broad OCR because of this isolated visual interference.
- Mannequin `Farty_Pants` also failed recognition as intended and must remain excluded. Tartaglia's off-party self-only +1 Normal Attack correction was applied at 09:37:15. All 71 constellation and 70 positive talent records were exported successfully, but no independent screenshot audit of every level is claimed.

### Focused post-run fix

Replace the Phase 2/3 verifier's direct `NameGOOD == recognizedName` check with a pure helper that accepts (a) an exact same name and matching element, or (b) canonical `Traveler` with a matching element whose expected GOOD key is exactly `Traveler` + that element. Do **not** accept any different element, wrong character or mannequin. The existing strict fallback remains unchanged. Add regression tests for TravelerCryo, wrong forms, Kirara/Mika, mannequin exclusion, and ordinary characters. **No changes to OCR thresholds, name matching, controller navigation, constellation reader, or Materials subsystem.**

**Validation still required:** the new build must scan and export nonzero Cryo Traveler talents while continuing to read Mika and the other 70 characters. Do not describe this correction as live-proven based only on successful CI.

## Follow-up live scan — 2026-10-23 10:03–10:12 (new EXP-024 evidence)

User supplied `InventoryKamera(2).log`, `InventoryKamera.debug(2).log`, and `genshinData_GOOD_2026_10_23_10_11.json`. Characters only, 3840×2160, game data 7.1.

- **72/72 expected playable characters recognised, 72 distinct GOOD keys**; only intentionally excluded mannequin `cryo / farty_pants` is logged unrecognised. Mika is captured C4 with base 1/1/1, and Kirara C5 with base 1/1/1, so the previously suspected Mika/Kirara cross-reading is not borne out by this export.
- **71/72 have all-positive talent levels**. The sole 0/0/0 is **Chongyun**, whose `EXP024 TALENT_FAILURE` logged `attempts=20, maxRows=2` and raw OCR with `Lv: 1` / `Lw. 1` variants. Do **not** treat this as a real 0 or assume a numerical value from the incomplete OCR; screenshot review/targeted parser work belongs to a separate follow-up.
- Previous EXP023 failures recovered: Kinich 7/8/8, Mavuika 6/6/6, Yaoyao 1/2/1, Collei 1/1/1, Chiori 1/1/1, Tighnari 1/1/1, Gorou 1/1/1, YunJin 1/1/1. `EXP024 HEADER_ALTERNATE_ACCEPT` showed correct headers for Chiori, Tighnari, Kirara, and others. YunJin OCR captured 1/1/4, then correctly subtracted C3 +3 Burst to export 1/1/1.
- **Confirmed Cryo Traveler database defect:** at 10:05:14 `TravelerCryo Constellation: 4`; at 10:10:38 `EXP024 TALENT_SUCCESS character=TravelerCryo ... scanned=5/5/9`, followed by `TravelerCryo: missing or malformed ConstellationOrder data. Talent levels were not adjusted`. GOOD incorrectly preserved 5/5/9; the expected unbuffed C4 GOOD is **5/5/6**. The existing builder's `playerElements` list excludes Cryo, and the per-element constellation mapping is missing/stale. This is not a misread name or talent.
- **Post-scan fix prepared:** pure `TryResolveConstellationTalentOrder` shared between early talent plausibility and final constellation scaling; Cryo Traveler's known C3 Burst/C5 Skill correction applied even when metadata is missing/wrong. `EnsureCryoTravelerMetadata` supplies `cryo` in memory on data reload and in future database updates, without replacing preserved orders for other forms. Unit tests cover C4 5/5/9 to 5/5/6, stale and wrong Cryo entries, repeat idempotence, other element preservation, and no false Cryo fallback for Hydro/Pyro. **Pending fresh-build CI and in-game validation**; the uploaded GOOD was made *before* this fix and remains incorrect for Traveler and Chongyun.

**Scope:** No alteration to EXP020 Materials archive, navigation, OCR thresholds, other character constellation orders, Weapons, or Artifacts.

## Chongyun screenshot-confirmed parser fix — 2026-10-23

The user supplied six EXP024 screenshots: `attributes.png`, `attributes_full_3840x2160.png`, `talents_unconfirmed.png`, `talents_unconfirmed_full_3840x2160.png`, and `constellation_greedy_5.png` / `constellation_greedy_6.png`.

- The 4K full-window screenshot confirms **Cryo / Chongyun** and the **Talents** sub-tab was genuinely selected. The crop clearly shows **Normal Lv.1 / Skill Lv.4 / Burst Lv.4**. This is NOT a roster-navigation error.
- Constellation screenshots show **C5 active, C6 locked**, consistent with logged Chongyun C5. At C5, the C3/C5 bonuses apply to the skill and burst respectively; target base GOOD is **1/1/1**, not 0/0/0.
- His logged *first* OCR frame already contained all three visible values, but one was transcribed as **`Lv: 1`**, whereas the production regex `[Ll][Vv]\\.?\\s*(\\d+)` accepted only an optional dot, not a colon. It therefore counted only two levels and exhausted all 20 retries. A later frame's `Lw. 1` is not accepted as verified evidence because it changes a letter.
- **Minimal source correction:** controller talent parser now recognises an optional **period OR colon** after `Lv`, preserving the same per-line first-match rule, the 1–15 range and existing `Flv.` behaviour (Qiqi). It does not guess digits or accept `Lw.`. The active scanner invokes the new pure `ParseControllerTalentLevels` helper, which has regression tests using Chongyun's *exact initial OCR* `Lv: 1 / Lv.4 / Lv.4`, plus negative and backwards-compatibility cases.
- This improvement should let Chongyun pass at the **first OCR attempt** given the recorded first frame; needs a **fresh in-game build and scan** to prove end-to-end correctness. No constellation, navigation, equipment, or Materials code was changed for Chongyun.

## Full-scan follow-up (2026-10-23 10:45–10:54) and named-character preview

User supplied `InventoryKamera(3).log`, `InventoryKamera.debug(3).log`, `genshinData_GOOD_2026_10_23_10_54.json` and four diagnostic images for Kinich/Chongyun.

**Results:** 72/72 distinct expected playable characters identified; 71/72 characters with valid positive talent values; the only zero is **Chongyun**. **Cryo Traveler C4 is fixed live**: raw OCR 5/5/9, GOOD **5/5/6**. YunJin C3 remains correctly 1/1/1 after raw 1/1/4. Mika C4, Kirara C5, Kinich 7/8/8, and the other previously missing talents are present. Farty Pants is the intentionally excluded mannequin and still generates the known non-playable warning.

**Two remaining verification warnings, now screenshot explained:**
- **Kinich constellation:** at 10:47:51, `EXP024 HEADER_FAILURE` expected Kinich, read **Electro / Keqing**, even though the forced full-window screenshot shows Kinich on the Constellation page with six locked nodes (C0). This may be transient UI/capture timing, not proof of persistent controller drift; his talents later read 7/8/8 correctly. Added two bounded fresh-frame attempts after primary and alternate OCR fail. Acceptance still requires matching expected name **and element**.
- **Chongyun talents:** at 10:53:51, `EXP024 HEADER_FAILURE` expected Cryo/Chongyun but raw header OCR was **`cryo / chongyun, 2`**. The forced screenshot shows correct Chongyun on the Talents page with visible **1/4/4**, and the earlier C5 screenshots corroborate C5. The colon-aware talent parser fix from earlier is **not tested by this run**, because the header gate skipped `ScanTalents` completely. Added a verification-only, tightly scoped tolerance for a trailing **comma + 1–2 digits** after an otherwise exact name+element; no fuzzy matching, no assumption that the cursor is correct. Expected GOOD **1/1/1** after C3/C5 subtraction, still needs a new live validation.

**New opt-in named-character scan** (EXPERIMENTAL, not live validated):
- Existing `All` or `Chars to Scan N` remains when the new named-target setting is empty.
- The main UI has a `Specific...` / `Selected: N` button that opens a multiline dialog. Input example `Chongyun, Kinich, TravelerCryo`; accepts line/comma/semicolon-separated **exact GOOD keys**, ignoring spaces/case only, no fuzzy choices.
- Named mode overrides the first-N limit. Phase 1 traverses the roster reading headers to locate requested targets, taking Attributes data for targets and the real first four party members. If all names found, phase 1 terminates early. Phases 2 and 3 use existing recorded shoulder-tap gaps and read **only selected characters**, starting from the correct physical slot and using the existing backward partial-pass path; a full lap supports late/missing names. Missing target names are explicitly reported, never substituted.
- Named mode corrects Tartaglia/Skirk buffs from **physical** first-four roster slots, rather than assuming chosen names are the active party. Only targets are emitted in the GOOD JSON. This is a *partial* file for testing, not a replacement for a complete account export.
- The UI setting is persisted and shown as `Selected: N` while active; clear its text in the dialog to return to normal mode. No changes to materials, weapons, artifacts, navigation constants, or the frozen EXP020 558/558 materials archive.

**Validation state:** compile/unit tests and Windows packaging pending final latest CI; **in-game named-mode test not yet run**.

## High-DPI UI correction — 2026-10-23

User provided a full screenshot of the 2.0.0-alpha WinForms main window showing **Chars to Scan: 5** and the `What to Scan` box severely crowded; the new `Specific...` option was not visibly clickable. Previous runtime button at `(200,224)`, `101×30` was too small/crowded under the fixed-size `320×296` box.

**UI-only correction:** button moved into the otherwise free right column beside Dev Items/Materials (`x135,y219,w167,h58`), rendered with a distinct, high-contrast background, two-line label and explicit `BringToFront`. Added a permanent second entry **Options → Scan Specific Characters...** with shortcut **Ctrl+Shift+C**, opening the same dialog even if future DPI layouts interfere with the button. Preserves the All/first-N mode and the exact named-target setting. No scanner/navigation/material/GOOD logic changed. Fresh-build CI and actual high-DPI click validation still required.

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
