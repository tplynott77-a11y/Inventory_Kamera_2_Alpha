# EXP-023 — Live roster GOOD comparison (screenshot audit pending)

**Local export:** `genshinData_GOOD_2026_10_22_22_08.json` (user-supplied, character-only). **Raw archive:** `Experiment 23.zip` (~426 MB, user-supplied). **Status: PARTIAL RECOVERY / NOT IMPORT-SAFE.**

**Evidence limitation:** JSON was read directly and compared field-by-field against `genshinData_GOOD_2026_10_22_20_27.json` (EXP-021), `genshinData_GOOD_2026_10_22_21_15.json` (EXP-022), and `genshinData_GOOD_2026_10_15_22_33.json` (historical). The container image/ZIP processing service was unavailable during this assessment, so **the ZIP log entries and screenshots have NOT been independently inspected**. Do not claim screenshot-level 100% accuracy or identify specific OCR error causes without them.

## Counts and comparisons (independently recomputed from GOOD files)
- **71 characters**, identical exported key set to EXP-021 and EXP-022. Cryo Traveler is still absent from this GOOD; mannequin slots are not expected in playable GOOD.
- **63/71 characters have all three talents >0**; **8/71 are 0/0/0**: Kinich, Mavuika, Yaoyao, Collei, Chiori, Tighnari, YunJin, Gorou. No negative talent values (EXP-022 had Tartaglia auto -1).
- Relative to EXP-021: three previously-zero sets were recovered (**Razor, Sucrose, Dehya**); three zeros persist (**Collei, Chiori, YunJin**); five nonzero characters regressed to zeros (**Kinich, Mavuika, Yaoyao, Tighnari, Gorou**). The only other changed talent sets were **Kachina 11/1/1 -> 1/1/1** and **Kirara 1/1/4 -> 1/1/1**. All other character talent export objects match EXP-021. These are export comparisons, not raw UI OCR validation.
- **All 71 constellation levels identical to EXP-022**. Only two differences vs EXP-021: **Charlotte C3→C4** and **Kirara C4→C5**, matching the two EXP-021 screenshots that visibly showed those nodes active. Relative to Oct 15 GOOD there are three further increases: Wanderer C0→C1, Diluc C1→C2, Freminet C1→C2; these appeared in both EXP-021 and EXP-022 and may reflect real pulls. Therefore there is **no new constellation regression detectable from export comparisons**; fully screenshot-verifying all nodes remains pending.
- **Ascension-only difference vs EXP-022:** Sayu was recorded Ascension 0 in EXP-022 and Ascension 1 in EXP-023; EXP-021 also recorded Ascension 1. All 71 character levels match EXP-022. No evidence yet whether EXP-022's Sayu A0 was a temporary OCR error.
- User reported in-UI reads for Dahlia and Candace of **1/4/4**, which were reported correct to their in-game display. Both are recorded C6 and export **1/1/1** to GOOD after deducting C3 and C5 constellation-granted +3 talent levels: this can be **expected intended behavior**, not a contradiction. User provided this visual observation, not a verified extracted screenshot.
- Similarly, Kachina C3 exports 1/1/1 when a displayed Skill Lv4 is reduced by its C3 +3. Kirara C5 export is now 1/1/1 (EXP-021's wrong C4 left Burst at 4).
- EXP-023 fixes the catastrophic EXP-022 all-zero talent problem, but 8 zeros, missing Cryo Traveler and unreviewed OCR mean **not safe for live GOOD import**.

## Next diagnosis
1. When ZIP processing is available, inspect `Experiment 23.zip` log text for `header verification failed`, `could not determine.*talents`, `talents_unconfirmed`, `constellation and displayed talent levels disagree`, and scan position/timing for the eight zero characters. Distinguish identity verification vs talent OCR vs impossible-C3 guard (Yun Jin).
2. Screenshot-audit all 71 constellation results, especially C6-first reads and unexpectedly increasing historical constellations.
3. Verify the Cryo Traveler's captured OCR raw header and whether the existing `gryo`/`cryaq` correction is actually applied after name resolution. Farty Pants is a mannequin and not expected in playable roster.
4. Avoid modifying the preserved EXP-020 materials branch and hold off on another full scan or importing an incomplete GOOD until causes are diagnosed.
