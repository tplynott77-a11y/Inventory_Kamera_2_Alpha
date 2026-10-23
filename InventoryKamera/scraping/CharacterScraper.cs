using InventoryKamera.game;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace InventoryKamera
{
    internal class CharacterScraper
	{
		private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		protected int NumOfCharToScan;

		private readonly IOcrService ocrService;
		private readonly IImagePreprocessor imagePreprocessor;
		private readonly IScanSettings scanSettings;
		private readonly IScanProgressReporter progressReporter;

        public CharacterScraper(IOcrService ocrService, IImagePreprocessor imagePreprocessor, IScanSettings scanSettings, IScanProgressReporter progressReporter)
		{
			this.ocrService = ocrService;
			this.imagePreprocessor = imagePreprocessor;
			this.scanSettings = scanSettings;
			this.progressReporter = progressReporter;
			NumOfCharToScan = scanSettings.NumOfCharToScan;
		}

		/// <summary>
		/// Childe (Tartaglia) passive buff fix -- his kit grants +1 auto-attack talent level to the
		/// first 4 party members once he's Ascension 4+, which the raw scanned talent level doesn't
		/// reflect. Used by the controller-driven batched path (<see cref="ScanCharacters"/>)
		/// since it only needs the final assembled roster, not how it was scanned.
		/// </summary>
		private void ApplyTartagliaFix(List<Character> Characters)
		{
			for (int i = 0; i < Characters.Count; i++)
			{
				if (Characters[i].NameGOOD.ToLower() == "tartaglia" && Characters[i].Ascension >= 4)
				{
					Logger.Info("Ascension 4+ Tartaglia found at position {0}.", i);
					if (i < 4)
					{
						for (int j = 0; j < Math.Min(4, Characters.Count); j++)
						{
							if (Characters[j].Talents.TryGetValue("auto", out int rawAuto) && rawAuto > 0)
                                Characters[j].Talents["auto"] -= 1;
							Logger.Info("Applied Tartaglia auto attack fix to {0} at position {1}.", Characters[j].NameGOOD, j);
						}
						break;
					}
					else
					{
						if (Characters[i].Talents.TryGetValue("auto", out int rawSelfAuto) && rawSelfAuto > 0)
                            Characters[i].Talents["auto"] -= 1;
						Logger.Info("Applied Tartaglia auto attack fix to self only.");
						break;
					}
				}
				else if (Characters[i].NameGOOD.ToLower() == "tartaglia") break;
			}
		}

		/// <summary>
		/// Skirk passive buff fix -- per user (2026-07-05), her kit grants +1 Skill talent level to
		/// the whole active team whenever that team (the first 4 scanned characters, matching
		/// <see cref="ApplyTartagliaFix"/>'s own team-position convention) is composed entirely of
		/// Hydro and Cryo characters, which the raw scanned talent level doesn't reflect. Unlike
		/// Tartaglia's fix, there's no ascension requirement, and there's no self-only fallback for an
		/// off-team Skirk -- her buff simply doesn't apply if she isn't one of the first 4. Stacks
		/// additively with any other active team buff on the same talent (only Tartaglia's exists
		/// today, and it targets a different talent, "auto", so there's no actual overlap yet).
		/// </summary>
		private void ApplySkirkFix(List<Character> Characters)
		{
			if (Characters.Count < 4) return;

			int skirkIndex = Characters.FindIndex(c => c.NameGOOD.ToLower() == "skirk");
			if (skirkIndex < 0 || skirkIndex >= 4) return;

			bool wholeTeamHydroOrCryo = Characters.Take(4).All(c =>
				c.Element != null && (c.Element.ToLower() == "hydro" || c.Element.ToLower() == "cryo"));

			if (!wholeTeamHydroOrCryo) return;

			Logger.Info("Skirk found on an all-Hydro/Cryo team -- applying team Skill buff correction.");
			for (int j = 0; j < 4; j++)
			{
				if (Characters[j].Talents.TryGetValue("skill", out int rawSkill) && rawSkill > 0)
                    Characters[j].Talents["skill"] -= 1;
				Logger.Info("Applied Skirk skill fix to {0} at position {1}.", Characters[j].NameGOOD, j);
			}
		}

		/// <summary>
        /// <summary>
        /// In named mode, adjust only targets actually in the first four
        /// PHYSICAL roster slots, not the first four chosen names. This keeps
        /// Childe/Skirk buffs correct for single-character debugging.
        /// </summary>
        private void ApplyTargetedPartyTalentFixes(
            List<Character> targets, List<Character> physicalParty)
        {
            bool completeParty = physicalParty.Count == 4;
            bool IsInParty(Character character) => completeParty &&
                physicalParty.Any(p => p.NameGOOD == character.NameGOOD);

            bool tartagliaOnTeam = completeParty &&
                physicalParty.Any(p => p.NameGOOD == "Tartaglia" && p.Ascension >= 4);
            bool skirkOnTeam = completeParty &&
                physicalParty.Any(p => p.NameGOOD == "Skirk") &&
                physicalParty.All(p => p.Element != null &&
                    (p.Element.Equals("Hydro", StringComparison.OrdinalIgnoreCase) ||
                     p.Element.Equals("Cryo", StringComparison.OrdinalIgnoreCase)));

            if (!completeParty)
                Logger.Warn("EXP024 NAMED_SCAN incomplete party: team buffs will not be inferred.");

            foreach (Character target in targets)
            {
                bool inParty = IsInParty(target);
                if (((tartagliaOnTeam && inParty) ||
                     (target.NameGOOD == "Tartaglia" && target.Ascension >= 4)) &&
                    target.Talents.TryGetValue("auto", out int rawAuto) && rawAuto > 1)
                {
                    target.Talents["auto"] -= 1;
                    Logger.Info("EXP024 NAMED_SCAN Tartaglia auto correction for {0}", target.NameGOOD);
                }

                if (skirkOnTeam && inParty &&
                    target.Talents.TryGetValue("skill", out int rawSkill) && rawSkill > 1)
                {
                    target.Talents["skill"] -= 1;
                    Logger.Info("EXP024 NAMED_SCAN Skirk skill correction for {0}", target.NameGOOD);
                }
            }
        }

		/// Controller-driven character scan (Phase 3 §6c), batched by sub-tab across the whole roster
		/// instead of per-character. Per user design decision (2026-07-05): switching the character
		/// screen's sub-tab (left-stick up/down; full order is Attributes, Weapons, Artifacts,
		/// Constellations, Talents, Profile) pays a real UI transition-animation cost, while advancing
		/// between characters (right shoulder button, mirroring how
		/// <see cref="InventoryScraper.SwitchToTab"/> cycles inventory tabs with LB/RB)
		/// does not. Interleaving sub-tabs per character would pay that animation cost repeatedly per
		/// character; this instead pays it exactly twice for the whole scan (Attributes to
		/// Constellations, a 3-step move past Weapons/Artifacts; Constellations to Talents, 1 step),
		/// independent of roster size -- at the cost of doing 3 full passes over the roster instead of
		/// 1. That trade favors batching since shoulder-tap advance is the cheap, already-fast-tuned
		/// primitive elsewhere (see <see cref="WeaponScraper.ScanWeapons"/>). Weapons and
		/// Artifacts sub-tabs are only transited through, never scanned, from this method (equipped
		/// gear is already covered by <see cref="WeaponScraper"/>/<see cref="ArtifactScraper"/>'s own
		/// controller scans).
		/// No dedicated rewind-to-start step exists between phases (per user, 2026-07-05): a full
		/// scan's cursor is already back on the first character after Phase 1's wraparound detection,
		/// so Phase 2/3 just read forward again (which wraps around for free, since count equals the
		/// whole roster). A partial scan's cursor instead sits one character past the last one
		/// scanned (no wraparound, since the roster has more characters left over) -- Phase 2 reads
		/// backward from there (<see cref="ScanRosterBackward"/>) to reach the same characters a
		/// rewind-then-forward pass would, in half the shoulder taps, which conveniently leaves the
		/// cursor back on the first character for Phase 3 to read forward
		/// (<see cref="ScanRosterForward"/>) same as the full-scan case.
		/// Constellation-node navigation and talent reading each get their own controller-native
		/// method (<see cref="ScanConstellations"/>/<see cref="ScanTalents"/>)
		/// rather than reusing the mouse path's per-node click loop.
		/// <paramref name="Characters"/> is scanned up to <see cref="NumOfCharToScan"/> entries (0 =
		/// whole roster).
		/// </summary>
		public void ScanCharacters(GameController controller, ref List<Character> Characters)
		{
            var requestedNames = CharacterScanRecognition.ParseRequestedCharacterNames(
                scanSettings.SpecificCharacterNames);
            bool targeted = requestedNames.Count > 0;
            int maxToScan = targeted ? 0 : NumOfCharToScan;
            if (targeted)
            {
                progressReporter.SetCharacter_Max(requestedNames.Count);
                Logger.Info("EXP024 NAMED_SCAN requested exact names: {0}",
                    string.Join(", ", requestedNames.OrderBy(v => v)));
            }
            else if (maxToScan != 0)
            {
                progressReporter.SetCharacter_Max(maxToScan);
            }
            progressReporter.ResetCharacterDisplay();

			EnterCharacterMenu(controller);

			// Per user (2026-07-05): the Character menu always opens on the Attributes sub-tab
			// regardless of what was open last time, so no reset-to-known-position step is needed
			// here. Also per user: unlike the pause-menu tab bar, this sub-tab control is
			// unbounded/circular (Up from Attributes wraps around to Profile, the last sub-tab, not
			// clamped) -- the clamped-control "over-shoot is harmless" idiom used elsewhere
			// (GameController.MashBack, an earlier version of this method) does NOT apply here and
			// would actively misnavigate.
			// --- Phase 1: Attributes (name, element, level) for the whole roster ---
            string firstName = null;
            string firstPhysicalName = null;
            int physicalSlot = 0;
            var scanned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Full (first four physical roster slots) party state is kept
            // independently of which characters were selected for export.
            // Otherwise Skirk/Childe could mistakenly see selected targets as
            // the first four party members.
            var physicalParty = new List<Character>();

			// True once the loop breaks via wraparound (cursor already sitting back on the first
			// character); false if it breaks via the scan-count cap or a cancel request (cursor
			// sitting one character past the last one scanned, since the roster has more characters
			// than were scanned so no wraparound happened). Drives Phase 2's read direction below.
			bool endedAtFirstCharacter = false;

			// Physical shoulder-tap gap between consecutive recorded characters -- NOT assumed to be
			// 1. Per user (2026-07-05, live-tested): a manequin slot (no constellation page to enter)
			// sitting between recorded characters caused Phase 2 to land on it and press confirm,
			// which closed the whole Character menu -- tracing back to Phase 2/3 assuming 1 shoulder
			// tap always equals 1 recorded character, when a manequin (or a duplicate-name repeat)
			// consumes a tap without being recorded into Characters. gapsBeforeEach[i] is the tap
			// count from Characters[i-1] (or the scan's starting position, for i==0, always 0) to
			// reach Characters[i] -- replayed exactly by ScanRosterForward/ScanRosterBackward below
			// instead of a uniform single-tap assumption.
			var gapsBeforeEach = new List<int>();
			int gapSinceLastRecorded = 0;
			int wrapGap = 0; // taps from the last recorded character back to the first, once wrapped
			int unrecognizedCount = 0; // non-manequin slots whose name/element couldn't be read

			while (true)
			{
				string name = null, element = null;
				string rawRead = ScanNameAndElement(ref name, ref element);

				bool isManequin = name == "Manequin1" || name == "Manequin2";
				bool hasValidNameAndElement = !isManequin && !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(element);

				if (hasValidNameAndElement)
				{
                    // The full-scan wrap marker is the first *recorded*
                    // character. In named mode it must be the first PHYSICAL
                    // character, since the first requested target could be
                    // fifty slots further into the roster.
                    string currentPhysicalName = name == "Traveler"
                        ? "Traveler" + element : name;
                    if (targeted && firstPhysicalName == null)
                        firstPhysicalName = currentPhysicalName;
                    bool wrapped = targeted
                        ? physicalSlot > 0 && string.Equals(
                            currentPhysicalName, firstPhysicalName, StringComparison.OrdinalIgnoreCase)
                        : Characters.Count > 0 && name == firstName;
                    if (wrapped)
                    {
                        // For a targeted subset, complete the circular gap from
                        // last requested target THROUGH the physical start back
                        // to the first requested target.
                        wrapGap = gapSinceLastRecorded +
                            (targeted && gapsBeforeEach.Count > 0 ? gapsBeforeEach[0] : 0);
                        endedAtFirstCharacter = true;
                        break;
                    }

                    string goodKey = name == "Traveler"
                        ? "Traveler" + element : name;
                    bool wanted = !targeted || requestedNames.Contains(
                        CharacterScanRecognition.NormalizeRequestedCharacterName(goodKey));
                    bool partySlot = targeted && physicalSlot < 4;

                    // Only expensive Attributes work for selected targets;
                    // collect the first four physical party members too so
                    // passive team corrections don't mistake a subset for a
                    // different team. Names alone are sufficient for others.
                    if (wanted || partySlot)
                    {
                        bool ascended = false;
                        int level = ScanLevel(ref ascended);
                        if (level == -1)
                        {
                            if (wanted) progressReporter.AddError($"Could not determine {name}'s level. Setting to 1.");
                            level = 1;
                            ascended = false;
                        }

                        if (partySlot)
                        {
                            physicalParty.Add(new Character
                            {
                                NameGOOD = name,
                                Element = element,
                                Level = level,
                                Ascended = ascended
                            });
                        }

                        if (wanted)
                        {
                            // Keep the exact existing GOOD identity and
                            // screenshot evidence for selected targets.
                            LogCharacterScreenshot(name, "attributes", NameElementRegion());
                            LogCharacterWindow(name, "attributes");

                            if (!scanned.Contains(goodKey))
                            {
                                var character = new Character
                                {
                                    NameGOOD = name,
                                    Element = element,
                                    Level = level,
                                    Ascended = ascended
                                };
                                Characters.Add(character);
                                gapsBeforeEach.Add(gapSinceLastRecorded);
                                gapSinceLastRecorded = 0;
                                scanned.Add(goodKey);
                                progressReporter.IncrementCharacterCount();
                                if (Characters.Count == 1) firstName = name;
                                Logger.Info("{0} attributes: element={1}, level={2}{3}",
                                    character.NameGOOD, character.Element, character.Level,
                                    character.Ascended ? "+" : "");
                            }
                            else
                            {
                                Logger.Info("Prevented {0} duplicate scan (controller)", goodKey);
                            }
                        }
                    }
                    else
                    {
                        Logger.Debug("EXP024 NAMED_SCAN skip attributes for non-target {0}", goodKey);
                    }
				}
				else if (!isManequin)
				{
					// Name and element are read from a single combined OCR string and always fail
					// together (ScanNameAndElement clears both on any parse/element-mismatch failure),
					// so this is one combined error, not two -- with the raw OCR text so the user can see
					// what was actually read.
					progressReporter.AddError($"Could not determine character element and name (\"{rawRead}\")");

					// Unrecognized, non-manequin slot: it never gets added to Characters, so it's
					// already skipped by the Constellations (Phase 2) and Talents (Phase 3) passes (both
					// only iterate recorded characters). Save one full-window screenshot so the user can
					// identify who was skipped -- it goes in the top-level characters folder (not a
					// per-character subfolder, since we have no name to file it under). Saved regardless
					// of LogScreenshots: an unrecognized character is a failure worth capturing.
					unrecognizedCount++;
					Logger.Warn("Unrecognized character (raw OCR: \"{0}\") -- skipping all passes and saving a full screenshot.", rawRead);
					Directory.CreateDirectory("./logging/characters");
					using (var screenshot = Navigation.CaptureWindow())
						screenshot.Save($"./logging/characters/unrecognized_{unrecognizedCount}.png");
				}

				controller.TapButton(Xbox360Button.RightShoulder, InventoryScraper.ScaledControllerDelay(80));
				Thread.Sleep(InventoryScraper.ScaledControllerDelay(100));
                gapSinceLastRecorded++;
                physicalSlot++;

                // Stop as soon as all exact requested names were seen (after
                // the first four party slots are known). The ordinary partial
                // backward-pass algorithm will now retrace the measured gaps.
                if (targeted && physicalSlot >= 4 &&
                    requestedNames.All(v => scanned.Contains(v)))
                {
                    Logger.Info("EXP024 NAMED_SCAN all {0} targets found after {1} physical slots.",
                        requestedNames.Count, physicalSlot);
                    break;
                }

                if (!targeted && maxToScan != 0 && Characters.Count >= maxToScan) break;
                // Prevent an unreadable wraparound name or an invalid target
                // from trapping the experimental named mode in an endless lap.
                if (targeted && physicalSlot >= 120)
                {
                    progressReporter.AddError("Named-character scan exceeded 120 roster slots without a verified wrap. Stopping safely.");
                    Logger.Warn("EXP024 NAMED_SCAN reached bounded 120-slot guard.");
                    break;
                }
				if (GameScanner.CancelRequested)
				{
					Logger.Info("Stopping character scan: cancel requested");
					break;
				}
			}

            if (targeted)
            {
                foreach (string missing in requestedNames.Except(scanned, StringComparer.OrdinalIgnoreCase).OrderBy(v => v))
                    progressReporter.AddError($"Named character not found: {missing}. The partial export excludes it.");
                Logger.Info("EXP024 NAMED_SCAN selected={0}/{1}: {2}",
                    Characters.Count, requestedNames.Count,
                    string.Join(", ", Characters.Select(c => c.NameGOOD)));
            }

            int count = Characters.Count;
            if (count == 0) return;

			// Per user (2026-07-05): a cancel request during Phase 1 was respected (the loop above
			// already checks it), but Phase 2/3 never checked it at all, so a cancel mid-scan
			// silently kept running the full Constellations/Talents passes over every character
			// already recorded instead of stopping. If Phase 1 itself was cancelled, skip straight to
			// the Tartaglia fix on whatever was scanned rather than starting Phase 2/3 at all.
			if (GameScanner.CancelRequested)
			{
				if (targeted)
				{
				    ApplyTargetedPartyTalentFixes(Characters, physicalParty);
				}
				else
				{
				    ApplyTartagliaFix(Characters);
				    ApplySkirkFix(Characters);
				}
				return;
			}

			// Local functions/lambdas below can't capture the `ref` parameter Characters directly
			// (CS1628) -- alias it to a plain local. Same List<Character> instance either way.
			List<Character> characterList = Characters;

			// Gap from the last recorded character to wherever the cursor now sits: back at the
			// first character (full scan, via wrapGap) or however many manequin/duplicate slots past
			// the last one scanned (partial scan or cancel, via whatever accrued since).
			int gapAfterLast = endedAtFirstCharacter ? wrapGap : gapSinceLastRecorded;

			// --- Phase 2: Constellations for the whole roster ---
			// Per user (2026-07-05): the full sub-tab order is Attributes, Weapons, Artifacts,
			// Constellations, Talents, Profile -- Constellations is 3 stops down from Attributes, not
			// 1 (Weapons and Artifacts sit between them).
			controller.Move(GameController.MenuDirection.Down, 3,
				holdMs: InventoryScraper.ScaledControllerDelay(150), settleMs: InventoryScraper.ScaledControllerDelay(150));

			void ScanConstellation(Character character)
			{
				// Verify the roster cursor is actually on this character before pressing B -- on a
				// manequin (no constellation page) B closes the whole Character menu and derails the
				// scan. See VerifyOnExpectedCharacter.
				if (!VerifyOnExpectedCharacter(character, "Constellation")) return;

				// Per user (2026-07-05): greedy (C6-first, read backward) mode only for 4-star
				// characters so far -- see IsFourStarCharacter/ScanConstellationsGreedy.
				character.Constellation = IsFourStarCharacter(character)
					? ScanConstellationsGreedy(controller, character)
					: ScanConstellations(controller, character);
				Logger.Info("{0} Constellation: {1}", character.NameGOOD, character.Constellation);
			}

			// Per user (2026-07-05): no rewind step -- a full scan's cursor is already sitting on the
			// first character (read forward, which returns to the start again via wrapGap, gap-aware
			// rather than assuming count taps), and a partial scan's cursor instead sits gapAfterLast
			// characters past the last one scanned, so reading backward from there reaches the same
			// characters a rewind-then-forward pass would, for far fewer shoulder taps.
            if (targeted)
            {
                bool constellationCompleted;
                if (endedAtFirstCharacter)
                {
                    AdvanceTargetedRoster(controller, Xbox360Button.RightShoulder, gapsBeforeEach[0]);
                    constellationCompleted = ScanNamedRosterForward(controller, characterList,
                        gapsBeforeEach, gapAfterLast, ScanConstellation);
                }
                else
                {
                    constellationCompleted = ScanNamedRosterBackward(controller, characterList,
                        gapsBeforeEach, gapAfterLast, ScanConstellation);
                }

                if (!constellationCompleted)
                {
                    Logger.Warn("EXP024 NAMED_SCAN constellation navigation incomplete: suppressing Talent pass.");
                    return;
                }
            }
            else if (endedAtFirstCharacter)
            {
                ScanRosterForward(controller, characterList, gapsBeforeEach, gapAfterLast, ScanConstellation);
            }
            else
                ScanRosterBackward(controller, characterList, gapsBeforeEach, gapAfterLast, ScanConstellation);

			if (GameScanner.CancelRequested)
			{
				if (targeted)
				{
				    ApplyTargetedPartyTalentFixes(Characters, physicalParty);
				}
				else
				{
				    ApplyTartagliaFix(Characters);
				    ApplySkirkFix(Characters);
				}
				return;
			}

			// --- Phase 3: Talents for the whole roster ---
			// Talents is immediately adjacent to Constellations in the sub-tab order, so this stays a
			// single step. Both Phase 2 branches above leave the cursor sitting back on the first
			// character (a full lap for the forward read, or the natural endpoint of the backward
			// read), so this phase can always read forward. No trailing gap needed (null) since this
			// is the last phase -- nothing follows that needs the cursor back at the start.
			controller.Move(GameController.MenuDirection.Down, 1,
				holdMs: InventoryScraper.ScaledControllerDelay(150), settleMs: InventoryScraper.ScaledControllerDelay(150));

            void ScanTargetTalents(Character character)
            {
                if (!VerifyOnExpectedCharacter(character, "Talent")) return;
                character.Talents = ScanTalents(character);
                Logger.Info("{0} Talents: {1}", character.NameGOOD,
                    "{" + string.Join(", ", character.Talents.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}");
                ApplyConstellationTalentScaling(character);
            }

            if (targeted)
            {
                if (!ScanNamedRosterForward(controller, characterList, gapsBeforeEach, null, ScanTargetTalents))
                {
                    Logger.Warn("EXP024 NAMED_SCAN talent navigation incomplete; preserving zero-valued unverified talents.");
                    return;
                }
            }
            else
            {
                ScanRosterForward(controller, characterList, gapsBeforeEach, null, ScanTargetTalents);
            }

			if (targeted)
			{
			    ApplyTargetedPartyTalentFixes(Characters, physicalParty);
			}
			else
			{
			    ApplyTartagliaFix(Characters);
			    ApplySkirkFix(Characters);
			}
		}

		/// <summary>
		/// Advances <paramref name="taps"/> times in one direction, one shoulder-button tap at a time
		/// (never a single multi-position jump), matching how <see cref="ScanCharacters"/>'s
		/// Phase 1 measured each gap the same way.
		/// </summary>
        /// <summary>
        /// EXP-024 named scans can cross dozens of character slots between
        /// selected targets. A quick shoulder tap may be dropped by the game,
        /// so a blind gap count alone cannot prove arrival. Keep ordinary full
        /// scans unchanged; use a more conservative rhythm for named scans.
        /// </summary>
        private void AdvanceTargetedRoster(GameController controller, Xbox360Button button, int taps)
        {
            for (int i = 0; i < taps; i++)
            {
                if (GameScanner.CancelRequested) return;
                controller.TapButton(button, InventoryScraper.ScaledControllerDelay(120));
                Thread.Sleep(InventoryScraper.ScaledControllerDelay(180));
            }
        }

        private bool HeaderConfirmsNamedTarget(Character expected)
        {
            Thread.Sleep(InventoryScraper.ScaledControllerDelay(250));
            string foundName = null, foundElement = null;
            ScanNameAndElement(ref foundName, ref foundElement, maxAttempts: 2);
            if (CharacterScanRecognition.MatchesExpectedCharacter(
                expected.NameGOOD, expected.Element, foundName, foundElement))
                return true;
            return TryVerifyExpectedHeaderWithAlternateOcr(expected, out _);
        }

        /// <summary>
        /// After a long jump, inspect the CURRENT roster slot before touching
        /// Constellations or Talents. If the game ignored one or more taps,
        /// search at most three slots each way. Never accept a wrong identity
        /// and never extrapolate further from a failed anchor.
        /// </summary>
        private bool TryReanchorNamedCharacter(
            GameController controller, Character expected, Xbox360Button intendedDirection)
        {
            if (HeaderConfirmsNamedTarget(expected)) return true;

            Xbox360Button opposite = intendedDirection == Xbox360Button.LeftShoulder
                ? Xbox360Button.RightShoulder : Xbox360Button.LeftShoulder;
            const int maxCorrection = 3;
            for (int offset = 1; offset <= maxCorrection; offset++)
            {
                if (GameScanner.CancelRequested) return false;
                AdvanceTargetedRoster(controller, intendedDirection, 1);
                if (HeaderConfirmsNamedTarget(expected))
                {
                    Logger.Warn("EXP024 NAMED_SCAN RESYNC expected={0} direction={1} extraTaps={2}",
                        expected.NameGOOD, intendedDirection, offset);
                    return true;
                }
            }

            // Return to the initially expected position before probing the
            // opposite direction. If all probes fail, abort this phase.
            AdvanceTargetedRoster(controller, opposite, maxCorrection);
            for (int offset = 1; offset <= maxCorrection; offset++)
            {
                if (GameScanner.CancelRequested) return false;
                AdvanceTargetedRoster(controller, opposite, 1);
                if (HeaderConfirmsNamedTarget(expected))
                {
                    Logger.Warn("EXP024 NAMED_SCAN RESYNC expected={0} direction={1} extraTaps={2}",
                        expected.NameGOOD, opposite, offset);
                    return true;
                }
            }

            progressReporter.AddError($"Named scan lost roster position near {expected.NameGOOD}; stopping instead of assigning wrong data.");
            Logger.Error("EXP024 NAMED_SCAN RESYNC_FAILED expected={0} after +/-{1} slots. Aborting remaining passes.",
                expected.NameGOOD, maxCorrection);
            return false;
        }

        private bool ScanNamedRosterBackward(
            GameController controller, List<Character> characters, List<int> gaps,
            int gapAfterLast, Action<Character> scanCharacter)
        {
            for (int i = characters.Count - 1; i >= 0; i--)
            {
                if (GameScanner.CancelRequested) return false;
                int gap = i == characters.Count - 1 ? gapAfterLast : gaps[i + 1];
                AdvanceTargetedRoster(controller, Xbox360Button.LeftShoulder, gap);
                if (!TryReanchorNamedCharacter(controller, characters[i], Xbox360Button.LeftShoulder))
                    return false;
                scanCharacter(characters[i]);
            }
            return true;
        }

        private bool ScanNamedRosterForward(
            GameController controller, List<Character> characters, List<int> gaps,
            int? gapAfterLast, Action<Character> scanCharacter)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (GameScanner.CancelRequested) return false;
                if (!TryReanchorNamedCharacter(controller, characters[i], Xbox360Button.RightShoulder))
                    return false;
                scanCharacter(characters[i]);
                int? gap = i < characters.Count - 1 ? gaps[i + 1] : gapAfterLast;
                if (gap.HasValue)
                    AdvanceTargetedRoster(controller, Xbox360Button.RightShoulder, gap.Value);
            }
            return true;
        }

		private void AdvanceRoster(GameController controller, Xbox360Button button, int taps)
		{
			for (int t = 0; t < taps; t++)
			{
				controller.TapButton(button, InventoryScraper.ScaledControllerDelay(80));
				Thread.Sleep(InventoryScraper.ScaledControllerDelay(100));
			}
		}

		/// <summary>
		/// Runs <paramref name="scanCharacter"/> once per character in <paramref name="characters"/>,
		/// assuming the cursor is already sitting on the first one, advancing forward (right shoulder)
		/// by the exact physical gap to the next character after each read -- <paramref
		/// name="gapsBeforeEach"/>[i] is the gap consumed to reach <paramref name="characters"/>[i]
		/// (see <see cref="ScanCharacters"/>'s Phase 1 for how it's measured), reused
		/// here to advance from character i to i+1. After the last character, advances by <paramref
		/// name="gapAfterLast"/> if given (used when the following phase also needs the cursor back
		/// at the start), or not at all if null (the last phase needs no such trailing move).
		/// </summary>
		private void ScanRosterForward(GameController controller, List<Character> characters, List<int> gapsBeforeEach, int? gapAfterLast, Action<Character> scanCharacter)
		{
			int count = characters.Count;
			for (int i = 0; i < count; i++)
			{
				scanCharacter(characters[i]);

				// Per user (2026-07-05): a cancel request during Phase 2/3 previously went
				// unchecked, silently finishing the whole roster pass instead of stopping.
				if (GameScanner.CancelRequested)
				{
					Logger.Info("Stopping character scan: cancel requested");
					break;
				}

				int? gap = i < count - 1 ? gapsBeforeEach[i + 1] : gapAfterLast;
				if (gap.HasValue) AdvanceRoster(controller, Xbox360Button.RightShoulder, gap.Value);
			}
		}

		/// <summary>
		/// Runs <paramref name="scanCharacter"/> once per character in <paramref name="characters"/>,
		/// last to first, assuming the cursor is sitting <paramref name="gapAfterLast"/> physical
		/// positions past the last character, advancing backward (left shoulder) by the exact gap
		/// before each read -- the mirror image of <see cref="ScanRosterForward"/>, reusing the same
		/// <paramref name="gapsBeforeEach"/> measurements since the physical distance between two
		/// characters doesn't depend on which direction it's crossed. Used for a partial scan, where
		/// reading backward from Phase 1's stopping point reaches the same characters a
		/// rewind-to-start-then-forward pass would, without the wasted rewind taps.
		/// </summary>
		private void ScanRosterBackward(GameController controller, List<Character> characters, List<int> gapsBeforeEach, int gapAfterLast, Action<Character> scanCharacter)
		{
			int count = characters.Count;
			for (int i = count - 1; i >= 0; i--)
			{
				int gap = i == count - 1 ? gapAfterLast : gapsBeforeEach[i + 1];
				AdvanceRoster(controller, Xbox360Button.LeftShoulder, gap);
				scanCharacter(characters[i]);

				// Per user (2026-07-05): a cancel request during Phase 2/3 previously went
				// unchecked, silently finishing the whole roster pass instead of stopping.
				if (GameScanner.CancelRequested)
				{
					Logger.Info("Stopping character scan: cancel requested");
					break;
				}
			}
		}

		/// <summary>
		/// Opens Genshin's pause menu and navigates the controller-mode tab bar to the Character
		/// screen (grid position [2,1] in the 4-wide x 5-tall tab grid, per MODERNIZATION_PLAN.md §6c,
		/// 2026-07-05), then confirms with B (Genshin's confirm/select button -- A backs out/cancels,
		/// confirmed 2026-07-05).
		/// Unlike the Inventory sub-tab row (which does remember its last-viewed tab, see
		/// <see cref="InventoryScraper.SwitchToTab"/>), the pause menu's own top-level tab bar resets
		/// to [0,0] every time it's opened fresh from gameplay -- so Move(Right,2) then Move(Down,1)
		/// from [0,0] is always correct here, provided this is entered from the unpaused free-roam
		/// state. The caller guarantees that: characters run in the same single controller session as
		/// the inventory phases (Phase 3 §6c, revised 2026-07-07 -- no more per-phase disconnect), and
		/// <c>InventoryKamera.GatherData</c> explicitly backs out to free-roam (MashBack + a generous
		/// settle wait) before this runs when an inventory phase preceded it. The leading Escape +
		/// MashBack below are a belt-and-suspenders reset on top of that.
		/// </summary>
		private void EnterCharacterMenu(GameController controller)
		{
			Navigation.sim.Keyboard.KeyPress(Navigation.escapeKey);
			Navigation.SystemWait(Navigation.Speed.UI);

			// Safety net (same idiom as GameController.ExitControllerMode): the previous
			// controller-driven phase's teardown already backs out of any nested menu, but
			// EnterControllerMode()'s A-press below assumes a clean free-roam state to avoid
			// triggering an unwanted in-game action instead of a scheme-switch nudge. Over-pressing
			// A here is harmless once already at the root (per MashBack's own doc comment).
			controller.MashBack();

			controller.EnterControllerMode();
			Thread.Sleep(InventoryScraper.ScaledControllerDelay(2000));
			controller.OpenMenu();
			Thread.Sleep(InventoryScraper.ScaledControllerDelay(2000));
			controller.Move(GameController.MenuDirection.Right, 2,
				holdMs: InventoryScraper.ScaledControllerDelay(300), settleMs: InventoryScraper.ScaledControllerDelay(300));
			controller.Move(GameController.MenuDirection.Down, 1,
				holdMs: InventoryScraper.ScaledControllerDelay(300), settleMs: InventoryScraper.ScaledControllerDelay(300));
			Thread.Sleep(InventoryScraper.ScaledControllerDelay(600));
			controller.TapButton(Xbox360Button.B, holdMs: InventoryScraper.ScaledControllerDelay(300));
			Thread.Sleep(Math.Max(3000, InventoryScraper.ScaledControllerDelay(2000)));
		}

		/// <summary>
		/// Re-reads the currently-selected slot's name/element header and returns whether it matches
		/// the character we expect to be scanning. Phases 2 (Constellations) and 3 (Talents) both
		/// replay Phase 1's shoulder-tap gaps to land back on each recorded character while skipping
		/// manequin slots; if that replay drifts even one slot (e.g. a constellation enter/exit not
		/// returning the roster cursor to the exact same position), this catches it before we act on
		/// the wrong slot -- pressing confirm on a manequin closes the whole Character menu, and reading
		/// a wrong slot records another character's constellation/talent data. Returns false (and logs)
		/// on any mismatch or unreadable slot, so the caller skips that character rather than corrupting
		/// it. Uses a small retry budget (fails fast on a wrong slot); the name/element header is shown
		/// on every Character sub-tab, so it reads the same region as Phase 1's Attributes read.
		/// </summary>
		private bool VerifyOnExpectedCharacter(Character character, string phaseLabel)
		{
			string currentName = null, currentElement = null;
			string rawHeaderOcr = ScanNameAndElement(ref currentName, ref currentElement, maxAttempts: 5);
            if (CharacterScanRecognition.MatchesExpectedCharacter(
                character.NameGOOD, character.Element, currentName, currentElement))
                return true;

            // EXP-024 observed genuine Kinich/Chiori talent screens whose
            // readable headers were rejected by the primary 110-threshold OCR.
            // The fallback is only for verification, and only an EXACT element
            // plus on-screen display-name match can unlock the talent read.
            string alternateOcr;
            if (TryVerifyExpectedHeaderWithAlternateOcr(character, out alternateOcr))
            {
                Logger.Info("EXP024 HEADER_ALTERNATE_ACCEPT expected={0} element={1} rawOCR=\"{2}\"",
                    character.NameGOOD, character.Element, alternateOcr);
                return true;
            }

            // EXP-024: Kinich's failed constellation screenshot shows Kinich,
            // but the earlier identity read still saw the previous Keqing.
            // A bounded fresh-frame retry tolerates late game transitions;
            // never trust roster position or accept a mismatched character.
            for (int refresh = 0; refresh < 2; refresh++)
            {
                Thread.Sleep(InventoryScraper.ScaledControllerDelay(250));
                string retryName = null, retryElement = null;
                string retryRaw = ScanNameAndElement(ref retryName, ref retryElement, maxAttempts: 1);
                if (CharacterScanRecognition.MatchesExpectedCharacter(
                        character.NameGOOD, character.Element, retryName, retryElement) ||
                    TryVerifyExpectedHeaderWithAlternateOcr(character, out alternateOcr))
                {
                    Logger.Info("EXP024 HEADER_REFRESH_ACCEPT expected={0} element={1} retry={2} rawOCR=\"{3}\"",
                        character.NameGOOD, character.Element, refresh + 1,
                        CharacterScanRecognition.CompactOcrDiagnostic(retryRaw));
                    return true;
                }

                currentName = retryName;
                currentElement = retryElement;
                rawHeaderOcr = retryRaw;
            }

			Logger.Warn("EXP024 HEADER_FAILURE phase={0} expected={1} actual={2} expectedElement={3} actualElement={4} rawOCR=\"{5}\" -- skipping character.",
                phaseLabel, character.NameGOOD, currentName ?? "(unreadable)", character.Element,
                currentElement ?? "(unreadable)", CharacterScanRecognition.CompactOcrDiagnostic(rawHeaderOcr));
            // EXP-021: six characters silently acquired zero talents here.
            // Preserve the actual screen even with normal screenshot logging off.
            LogCharacterScreenshot(character.NameGOOD, $"diagnostics/{phaseLabel}_verification_failed", NameElementRegion(), force: true);
            LogCharacterWindow(character.NameGOOD, $"diagnostics/{phaseLabel}_verification_failed", force: true);
            progressReporter.AddError($"{phaseLabel} header verification failed for {character.NameGOOD}; check diagnostic screenshots.");
            return false;
		}

        /// <summary>
        /// EXP-024 verification-only rescue for readable headers rejected by
        /// the legacy 110-threshold text recognizer. A different character
        /// MUST NOT be accepted by fuzzy matching, substring lookup, or by
        /// merely trusting the controller's roster position.
        /// </summary>
        private bool TryVerifyExpectedHeaderWithAlternateOcr(Character character, out string evidence)
        {
            evidence = "(empty)";
            string lookupKey = character.NameGOOD.Contains("Traveler")
                ? "traveler" : character.NameGOOD.ToLowerInvariant();
            string displayName = character.NameGOOD;
            if (GenshinProcesor.Characters.TryGetValue(lookupKey, out var record) &&
                record["CustomName"] != null)
                displayName = (string)record["CustomName"];

            using (Bitmap original = Navigation.CaptureRegion(NameElementRegion()))
            {
                foreach (int threshold in new[] { 175, 210 })
                {
                    Bitmap processed = imagePreprocessor.ConvertToGrayscale(original);
                    try
                    {
                        imagePreprocessor.SetThreshold(threshold, ref processed);
                        imagePreprocessor.SetInvert(ref processed);
                        using (Bitmap scaled = GenshinProcesor.ResizeImage(processed,
                                   processed.Width * 2, processed.Height * 2))
                        {
                            foreach (var mode in new[] { Tesseract.PageSegMode.SingleLine, Tesseract.PageSegMode.Auto })
                            {
                                string raw = ocrService.AnalyzeText(scaled, mode)?.Trim() ?? "";
                                evidence = CharacterScanRecognition.CompactOcrDiagnostic(raw);
                                if (CharacterScanRecognition.ExactHeaderMatches(raw, character.Element, displayName))
                                    return true;
                            }
                        }
                    }
                    finally
                    {
                        processed?.Dispose();
                    }
                }
            }
            return false;
        }

		/// <summary>
		/// The always-visible name/element header region on the Character screen. Shared by
		/// <see cref="ScanNameAndElement"/>'s OCR read and Phase 1's per-character attributes
		/// screenshot. Measured (2026-07-05) with <c>ui/CoordinatePickerForm.cs</c>.
		/// </summary>
		private static Rectangle NameElementRegion() => new RECT(
			Left:   (int)( 0.0941 * Navigation.GetWidth() ),
			Top:    (int)( 0.0430 * Navigation.GetHeight() ),
			Right:  (int)( 0.2442 * Navigation.GetWidth() ),
			Bottom: (int)( 0.0920 * Navigation.GetHeight() ));

		/// <summary>
		/// Saves a screenshot of just <paramref name="region"/> (the section actually being scanned,
		/// not the whole window) under a per-character logging folder, gated on
		/// <see cref="scanSettings"/>'s LogScreenshots setting -- shared by every controller-path
		/// capture point (<see cref="ScanCharacters"/>'s Attributes read,
		/// <see cref="ScanConstellations"/> per node, <see cref="ScanTalents"/>). <paramref
		/// name="relativeName"/> may contain '/' to nest into a subfolder (e.g.
		/// <c>"constellations/constellation_1"</c>).
		/// </summary>
		private void LogCharacterScreenshot(string characterName, string relativeName, Rectangle region, bool force = false)
		{
			if (!force && !scanSettings.LogScreenshots) return;

			string path = $"./logging/characters/{characterName}/{relativeName}.png";
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (var screenshot = Navigation.CaptureRegion(region))
				screenshot.Save(path);
		}

		/// <summary>
		/// Saves a full-window screenshot (the whole game window, not just the scanned region) next to
		/// the region crop that <see cref="LogCharacterScreenshot"/> writes, gated on the same
		/// LogScreenshots setting. Purpose is coordinate discovery: every character-scan region in this
		/// class is expressed as a window-size fraction, and re-deriving those fractions for a new
		/// aspect ratio (e.g. 16:10) needs a full-window capture of each screen to measure against. The
		/// resolution is baked into the filename so captures from different resolutions/aspect ratios
		/// sit side by side instead of overwriting each other. <paramref name="relativeName"/> may
		/// contain '/' to nest into a subfolder, same as <see cref="LogCharacterScreenshot"/>.
		/// </summary>
		private void LogCharacterWindow(string characterName, string relativeName, bool force = false)
        {
            if (!force && !scanSettings.LogScreenshots) return;

			string path = $"./logging/characters/{characterName}/{relativeName}_full_{Navigation.GetWidth()}x{Navigation.GetHeight()}.png";
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (var screenshot = Navigation.CaptureWindow())
				screenshot.Save(path);
		}

		/// <summary>
		/// Applies the constellation-3/5 talent-level discount (Genshin auto-grants a talent level
		/// via certain constellations, which the raw scanned talent level doesn't reflect). Used by
		/// the controller-driven batched path (<see cref="ScanCharacters"/>), since it
		/// only needs the assembled name/element/constellation/talents, not how they were scanned.
		/// </summary>
		private void ApplyConstellationTalentScaling(Character character)
		{
            // A previous phase may have failed identity or talent OCR; zero is
            // our review-required sentinel, not a genuine in-game talent.
            // Never turn missing values into negative GOOD levels by applying
            // C3/C5 talent bonuses to a failed read.
            if (character.Talents == null ||
                character.Talents.Values.Any(level => level < 1 || level > 15))
            {
                Logger.Warn("Skipping talent constellation correction for {0}: levels are unverified.",
                    character.NameGOOD);
                return;
            }

			// Pyro Traveler's own constellation scan can't be trusted (per user, 2026-07-05): the
			// secondary constellations that grant +3 to a talent are visually embedded in the same 6
			// nodes as the base unlocks, so ScanConstellations can misreport a
			// lower-constellation Pyro Traveler as C6. Per user: default to assuming C0 and infer the
			// real constellation instead from the raw scanned talent levels -- a Skill level of 11+
			// is only reachable if the C3-equivalent +3 Skill bonus is active, and a Burst level of
			// 11+ only if the C5-equivalent +3 Burst bonus is active (unlocks are sequential, so
			// Burst>=11 implies the Skill bonus is active too). This replaces, rather than
			// supplements, the generic scanned-Constellation-based discount below for this character.
			if (character.NameGOOD.Contains("Traveler") && character.Element?.ToLower() == "pyro")
			{
				int constellation = 0;

				if (character.Talents.TryGetValue("skill", out int skillLevel) && skillLevel >= 11)
				{
					constellation = 3;
					character.Talents["skill"] -= 3;
					Logger.Info("{0}: scanned Skill level {1} implies at least C3 -- adjusted to {2}.",
						character.NameGOOD, skillLevel, character.Talents["skill"]);
				}

				if (character.Talents.TryGetValue("burst", out int burstLevel) && burstLevel >= 11)
				{
					constellation = 5;
					character.Talents["burst"] -= 3;
					Logger.Info("{0}: scanned Burst level {1} implies at least C5 -- adjusted to {2}.",
						character.NameGOOD, burstLevel, character.Talents["burst"]);
				}

				Logger.Info("{0} (Pyro): constellation corrected from scanned {1} to inferred {2}.",
					character.NameGOOD, character.Constellation, constellation);
				character.Constellation = constellation;
				return;
			}

			if (character.Constellation < 3) return;

            string lookupKey = character.NameGOOD.Contains("Traveler")
                ? "traveler" : character.NameGOOD.ToLowerInvariant();
            GenshinProcesor.Characters.TryGetValue(lookupKey, out var characterData);
            JToken storedOrder = characterData?["ConstellationOrder"];

            if (!CharacterScanRecognition.TryResolveConstellationTalentOrder(
                    character.NameGOOD, character.Element, storedOrder,
                    out string talentLeveledAtConst3, out string talentLeveledAtConst5,
                    out bool repairedCryoOrder))
            {
                progressReporter.AddError($"{character.NameGOOD}: missing or malformed ConstellationOrder data. Talent levels were not adjusted for constellation bonuses.");
                return;
            }

            if (repairedCryoOrder)
                Logger.Warn("EXP024 TRAVELER_CRYO_ORDER_REPAIRED: database missing/wrong Cryo mapping; C3=burst, C5=skill.");

            bool impossibleC3 = character.Constellation >= 3 &&
                (!character.Talents.TryGetValue(talentLeveledAtConst3, out int boostedAtC3) || boostedAtC3 < 4);
            bool impossibleC5 = character.Constellation >= 5 &&
                (!character.Talents.TryGetValue(talentLeveledAtConst5, out int boostedAtC5) || boostedAtC5 < 4);
            if (impossibleC3 || impossibleC5)
            {
                progressReporter.AddError($"{character.NameGOOD}: constellation and displayed talent levels disagree; review screenshots.");
                character.Talents["auto"] = 0;
                character.Talents["skill"] = 0;
                character.Talents["burst"] = 0;
                return;
            }

            if (character.Constellation >= 3)
            {
                Logger.Info("{0} constellation 3+, adjusting scanned {1} level",
                    character.NameGOOD, talentLeveledAtConst3);
                character.Talents[talentLeveledAtConst3] -= 3;
            }

            if (character.Constellation >= 5)
            {
                Logger.Info("{0} constellation 5+, adjusting scanned {1} level",
                    character.NameGOOD, talentLeveledAtConst5);
                character.Talents[talentLeveledAtConst5] -= 3;
            }
        }

		public static string ScanMainCharacterName(IOcrService ocrService, IImagePreprocessor imagePreprocessor, IScanProgressReporter progressReporter)
		{
			var xReference = 1280.0;
			var yReference = 720.0;
			if (Navigation.GetAspectRatio() == new Size(8, 5))
			{
				yReference = 800.0;
			}

			RECT region = new RECT(
				Left:   (int)(185 / xReference * Navigation.GetWidth()),
				Top:    (int)(26  / yReference * Navigation.GetHeight()),
				Right:  (int)(460 / xReference * Navigation.GetWidth()),
				Bottom: (int)(60  / yReference * Navigation.GetHeight()));

			Bitmap nameBitmap = Navigation.CaptureRegion(region);

			//Image Operations
			GenshinProcesor.SetGamma(0.2, 0.2, 0.2, ref nameBitmap);
			imagePreprocessor.SetInvert(ref nameBitmap);
			Bitmap n = imagePreprocessor.ConvertToGrayscale(nameBitmap);

			progressReporter.SetNavigation_Image(nameBitmap);

			string text = ocrService.AnalyzeText(n).Trim();
			if (text != "")
			{
				// Only keep a-Z and 0-9
				text = Regex.Replace(text, @"[\W_]", string.Empty).ToLower();

				// Only keep text up until first space
				text = Regex.Replace(text, @"\s+\w*", string.Empty);

			}
			else
			{
				progressReporter.AddError(text);
			}
			n.Dispose();
			nameBitmap.Dispose();
			return text;
		}

		/// <summary>
		/// Reads the always-visible name/element region on the Attributes sub-tab. Region re-measured
		/// (2026-07-05) with <c>ui/CoordinatePickerForm.cs</c> against a two-line wrapped name.
		/// <paramref name="maxAttempts"/> caps the retry loop -- Phase 1's roster read uses the default
		/// 20 (a genuine character should read within that), while the Phase 2 constellation guard
		/// passes a smaller budget so it fails fast on a manequin/wrong slot instead of burning the full
		/// 20 retries before skipping. Returns the raw OCR text from the final attempt (name and
		/// element are read from one combined OCR string, so callers can surface it in a failure
		/// message to show what was actually read).
		/// </summary>
		private string ScanNameAndElement(ref string name, ref string element, int maxAttempts = 20)
		{
			int attempts = 0; // reduced from 75 per user (2026-07-05) -- 75 retries at Speed.Fast was too slow when parsing genuinely fails
			Rectangle region = NameElementRegion();
			string rawText = "";

			do
			{
				using (Bitmap bm = Navigation.CaptureRegion(region))
				{
					Bitmap n = imagePreprocessor.ConvertToGrayscale(bm);
					imagePreprocessor.SetThreshold(110, ref n);
					imagePreprocessor.SetInvert(ref n);

					n = GenshinProcesor.ResizeImage(n, n.Width * 2, n.Height * 2);
					string block = ocrService.AnalyzeText(n, Tesseract.PageSegMode.Auto).ToLower().Trim();
					string line = ocrService.AnalyzeText(n, Tesseract.PageSegMode.SingleLine).ToLower().Trim();

					// Characters with wrapped names will not have a slash
					string nameAndElement = line.Contains("/") ? line : block;
					rawText = nameAndElement;

					if (nameAndElement.Contains("/"))
					{
						var split = nameAndElement.Split('/');

						// Search for element and character name in block

						// Long name characters might look like
						// <Element>   <First Name>
						// /           <Last Name>
						// Per user (2026-07-05, live screenshot): the wrapped case's first line holds
						// BOTH the element and the first word of the name (e.g. "Anemo Yumemizuki"),
						// which the element-extraction below already isolated correctly, but the name
						// search previously used only the text after "/" ("Mizuki"), silently dropping
						// "Yumemizuki" -- fixed by carrying the leftover first-line text forward into
						// the name search instead of discarding it.
						string namePart1 = "";
                        string rawElementToken = split[0].Trim().Split(' ')[0];
                        if (!split[0].Contains(" "))
						{
							element = GenshinProcesor.FindElementByName(split[0].Trim());
						}
						else
						{
							var firstLineWords = split[0].Split(new[] { ' ' }, 2);
							element = GenshinProcesor.FindElementByName(firstLineWords[0].Trim());
							if (firstLineWords.Length > 1) namePart1 = firstLineWords[1];
						}

						// Find character based on the leftover first-line text (if any) plus the
						// string after /. Long name characters might search by their last name only
						// but it'll still work in the non-wrapped case (namePart1 stays empty).
						name = GenshinProcesor.FindClosestCharacterName(Regex.Replace(namePart1 + split[1], @"[\W]", string.Empty));
                        // Constrained EXP-021 correction: only an identified Traveler
                        // may map the observed element token "cryaq" back to Cryo.
                        element = CharacterScanRecognition.RepairTravelerElement(name, rawElementToken, element);
                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(element) ||
                            !GenshinProcesor.CharacterMatchesElement(name, element))
                        {
                            name = "";
                            element = "";
                        }
                    }
					n.Dispose();

					if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(element))
					{
						Logger.Debug("Scanned character name as {0} with element {1}", name, element);
                        progressReporter.SetCharacter_NameAndElement(bm, name, element);
						return rawText;
					}
					else
                    {
                        // Per-attempt retry: don't dump a screenshot here -- it used to litter the
                        // top-level ./logging/characters folder with hash-named images (one per failed
                        // attempt, ungated). A single full-window screenshot is saved once by the Phase 1
                        // caller if the character stays unrecognized after all retries (unrecognized_N.png).
                        Logger.Debug("Could not parse character name/element (attempt {0}/{1}). Retrying...", attempts+1, maxAttempts);
                    }
				}
				attempts++;
				Navigation.SystemWait(200f);
			} while ( attempts < maxAttempts );
			name = null;
			element = null;
			return rawText;
		}

		/// <summary>
		/// Reads the level/ascension region on the Attributes sub-tab. 16:9 region measured
		/// (2026-07-05), 16:10 Y position measured (2026-07-13, 1680x1050), both with
		/// <c>ui/CoordinatePickerForm.cs</c>. Expressed as position + size: the crop's x/width/height are
		/// shared across aspect ratios, so only the y position branches -- the window is letterboxed
		/// vertically, but the horizontal axis and the box's dimensions don't change.
		/// </summary>
		private int ScanLevel(ref bool ascended)
		{
            int attempt = 0;

			// Shared x/width/height; only the y position shifts by aspect ratio (see doc comment).
			Rectangle region = new Rectangle(
				x:      (int)( 0.7626 * Navigation.GetWidth() ),
				y:      (int)( (Navigation.IsNormal ? 0.1895 : 0.1695) * Navigation.GetHeight() ),   // 16:9 : 16:10
				width:  (int)( 0.1209 * Navigation.GetWidth() ),
				height: (int)( 0.0352 * Navigation.GetHeight() ));

			do
			{
				Bitmap bm = Navigation.CaptureRegion(region);

				bm = GenshinProcesor.ResizeImage(bm, bm.Width * 2, bm.Height * 2);
				Bitmap n = imagePreprocessor.ConvertToGrayscale(bm);
				imagePreprocessor.SetInvert(ref n);
				// Bug fix (2026-07-05): contrast was being applied to `bm` (the display-only copy
				// passed to progressReporter.SetCharacter_Level below), not `n` (what's actually fed
				// to Tesseract) -- the contrast boost never reached the OCR input at all, likely
				// contributing to level misreads.
				imagePreprocessor.SetContrast(30.0, ref n);

				string text = ocrService.AnalyzeText(n).Trim();
				Logger.Debug("Scanned character level as {0}", text);

				text = Regex.Replace(text, @"(?![0-9/]).", string.Empty);
				Logger.Debug("Filtered scanned text to {0}", text);
				if (text.Contains("/"))
				{
					var values = text.Split('/');
                    if (int.TryParse(values[0], out int level) && int.TryParse(values[1], out int maxLevel))
                    {
                        maxLevel = (int)Math.Round(maxLevel / 10.0, MidpointRounding.AwayFromZero) * 10;
                        ascended = 20 <= level && level < maxLevel;
                        progressReporter.SetCharacter_Level(bm, level, maxLevel);
                        n.Dispose();
                        bm.Dispose();
                        Logger.Debug("Parsed character level as {0}", level);
                        return level;
                    }
				}
				Logger.Debug("Failed to parse character level and ascension from {0} (text), retrying", text);

				attempt++;

                n.Dispose();
                bm.Dispose();
                Navigation.SystemWait(Navigation.Speed.Fast);
			} while (attempt < 20); // reduced from 50 per user (2026-07-05), matching ScanNameAndElement's cap

			return -1;
		}

		/// <summary>
		/// Controller-mode constellation scan. Per user (2026-07-05, live-tested), the interaction
		/// model here is entirely different from the mouse path's per-node click + color-sample: you
		/// press confirm (B) once to enter the constellation detail view (which opens on the first
		/// constellation already focused), then a single left-stick Down step advances focus to the
		/// next constellation -- the same fixed on-screen region always shows whichever constellation
		/// is currently focused, so there is no per-node capture-region math like the mouse path
		/// needed. Unlock state is read via OCR for the literal word "Activated" (a locked
		/// constellation instead prompts "Activate", per user) rather than the mouse path's
		/// white-background color sample. Exits via cancel (A) once done or once a locked
		/// constellation is found. Region measured (2026-07-05) with <c>ui/CoordinatePickerForm.cs</c>.
		/// </summary>
		private int ScanConstellations(GameController controller, Character character)
		{
			Rectangle activatedRegion = new RECT(
				Left:   (int)( 0.1574 * Navigation.GetWidth() ),
				Top:    (int)( 0.8323 * Navigation.GetHeight() ),
				Right:  (int)( 0.2241 * Navigation.GetWidth() ),
				Bottom: (int)( 0.8777 * Navigation.GetHeight() ));

			int constellation;

			controller.TapButton(Xbox360Button.B, InventoryScraper.ScaledControllerDelay(300));
			Thread.Sleep(InventoryScraper.ScaledControllerDelay(600)); // set to 600 per user (2026-07-05)

			Bitmap constellationShot = null;
			for (constellation = 0; constellation < 6; constellation++)
			{
				if (constellation > 0)
				{
					// Per user (2026-07-05): settleMs already sleeps after the stick releases
					// (GameController.MoveStep), so a separate Thread.Sleep on top of it was a
					// redundant double-wait -- folded into settleMs directly instead.
					controller.MoveStep(GameController.MenuDirection.Down,
						holdMs: InventoryScraper.ScaledControllerDelay(100), settleMs: InventoryScraper.ScaledControllerDelay(400));
				}

				LogCharacterScreenshot(character.NameGOOD, $"constellations/constellation_{constellation + 1}", activatedRegion);
				LogCharacterWindow(character.NameGOOD, $"constellations/constellation_{constellation + 1}");

				bool activated = ReadConstellationActivated(activatedRegion);

				// Capture the constellation region to show in the UI: keep the last ACTIVATED node,
				// falling back to the first node examined so a C0 character still shows something.
				if (activated || constellationShot == null)
				{
					constellationShot?.Dispose();
					constellationShot = Navigation.CaptureRegion(activatedRegion);
				}

				if (!activated) break;
			}

			controller.TapButton(Xbox360Button.A, InventoryScraper.ScaledControllerDelay(300));
			Thread.Sleep(InventoryScraper.ScaledControllerDelay(250)); // lowered from 400 per user (2026-07-05)

			progressReporter.SetCharacter_Constellation(constellationShot, constellation);
			constellationShot?.Dispose();
			return constellation;
		}

		/// <summary>
		/// Reads the currently-focused constellation node's unlock state, retrying up to 3 times
		/// (re-capturing a fresh screenshot each time, not just re-OCRing the same bitmap -- a miss is
		/// more likely the node-move animation not having settled yet than a deterministic misread) --
		/// see <see cref="ScanConstellations"/>'s doc comment history for why. Reads via
		/// OCR for the literal word "Activated" (a locked constellation instead prompts "Activate").
		/// Preprocessing is gamma+invert+grayscale (2026-07-05): a live screenshot showed "Activated"
		/// as bold orange/gold text on a blue-green gradient background, the same pattern that broke
		/// contrast-based preprocessing for the weapon card nameplate (see
		/// WeaponScraper/ScanMainCharacterName's doc comments); gamma correction is the proven fix for
		/// that exact text/background combination in this codebase. Shared by
		/// <see cref="ScanConstellations"/> and <see cref="ScanConstellationsGreedy"/>.
		/// </summary>
		private bool ReadConstellationActivated(Rectangle activatedRegion)
        {
            for (int readAttempt = 0; readAttempt < 3; readAttempt++)
            {
                if (readAttempt > 0) Navigation.SystemWait(200f);

                using (Bitmap screenshot = Navigation.CaptureRegion(activatedRegion))
                {
                    // EXP-022: isolate the golden label from bright and animated
                    // backgrounds. Charlotte C4 and Kirara C5 are known regressions.
                    using (Bitmap isolated = CharacterScanRecognition.IsolateGoldConstellationText(screenshot))
                    using (Bitmap enlarged = GenshinProcesor.ResizeImage(
                        isolated, isolated.Width * 3, isolated.Height * 3))
                    {
                        string goldText = ocrService.AnalyzeText(enlarged,
                            Tesseract.PageSegMode.SingleLine).Trim();
                        Logger.Debug("Constellation gold-isolated OCR attempt {0}: '{1}'",
                            readAttempt + 1, goldText);
                        if (CharacterScanRecognition.SaysActivated(goldText)) return true;
                    }

                    // Keep the older gamma-based algorithm as a secondary view.
                    Bitmap legacy = (Bitmap)screenshot.Clone();
                    try
                    {
                        GenshinProcesor.SetGamma(0.2, 0.2, 0.2, ref legacy);
                        imagePreprocessor.SetInvert(ref legacy);
                        using (Bitmap gray = imagePreprocessor.ConvertToGrayscale(legacy))
                        {
                            string legacyText = ocrService.AnalyzeText(gray,
                                Tesseract.PageSegMode.SingleBlock).Trim();
                            Logger.Debug("Constellation gamma OCR attempt {0}: '{1}'",
                                readAttempt + 1, legacyText);
                            if (CharacterScanRecognition.SaysActivated(legacyText)) return true;
                        }
                    }
                    finally { legacy.Dispose(); }
                }
            }
            return false;
        }

		/// <summary>
		/// Greedy variant of <see cref="ScanConstellations"/>: instead of walking C1 to
		/// C6 forward and stopping at the first lock, jumps straight to C6 and reads backward. Per
		/// user (2026-07-05): the constellation list is an unbounded/circular scroll (same as the
		/// Character screen's own sub-tab row), so a single Up move from the C1 focus that confirming
		/// (B) opens on wraps straight to C6 instead of needing 5 Down moves to walk there. Since
		/// unlocks are always sequential (C6 unlocked implies C1-C5 are too), if C6 reads Activated
		/// the whole scan is done in a single read instead of up to six. Per user, only used for
		/// 4-star characters for now (<see cref="IsFourStarCharacter"/>) since those are commonly
		/// fully-conned, making the C6-first check likely to pay off; 5-stars stay on the forward scan
		/// where a full-con check failing fast (locked at C1) is the more common case.
		/// NOT YET LIVE-VERIFIED: the circular-scroll-wraps-in-one-Up-move assumption for the
		/// constellation list specifically (confirmed only for the Character screen's own sub-tab row
		/// so far).
		/// </summary>
		private int ScanConstellationsGreedy(GameController controller, Character character)
		{
			Rectangle activatedRegion = new RECT(
				Left:   (int)( 0.1574 * Navigation.GetWidth() ),
				Top:    (int)( 0.8323 * Navigation.GetHeight() ),
				Right:  (int)( 0.2241 * Navigation.GetWidth() ),
				Bottom: (int)( 0.8777 * Navigation.GetHeight() ));

			controller.TapButton(Xbox360Button.B, InventoryScraper.ScaledControllerDelay(300));
			Thread.Sleep(InventoryScraper.ScaledControllerDelay(600));

			controller.MoveStep(GameController.MenuDirection.Up,
				holdMs: InventoryScraper.ScaledControllerDelay(100), settleMs: InventoryScraper.ScaledControllerDelay(400));

			Bitmap constellationShot = null;
			int constellation = 0;
			for (int node = 5; node >= 0; node--)
			{
				LogCharacterScreenshot(character.NameGOOD, $"constellations/constellation_greedy_{node + 1}", activatedRegion);

				bool activated = ReadConstellationActivated(activatedRegion);

				// Capture the constellation region for the UI: the activated node found, or the first
				// node examined as a fallback if none are activated (C0).
				if (activated || constellationShot == null)
				{
					constellationShot?.Dispose();
					constellationShot = Navigation.CaptureRegion(activatedRegion);
				}

				if (activated)
				{
					constellation = node + 1;
					break;
				}

				if (node > 0)
				{
					controller.MoveStep(GameController.MenuDirection.Up,
						holdMs: InventoryScraper.ScaledControllerDelay(100), settleMs: InventoryScraper.ScaledControllerDelay(400));
				}
			}

			controller.TapButton(Xbox360Button.A, InventoryScraper.ScaledControllerDelay(300));
			Thread.Sleep(InventoryScraper.ScaledControllerDelay(250));

			progressReporter.SetCharacter_Constellation(constellationShot, constellation);
			constellationShot?.Dispose();
			return constellation;
		}

		/// <summary>
		/// Whether <paramref name="character"/>'s database entry has Rarity == 4. Missing Rarity
		/// (e.g. a characters.json predating the 2026-07-05 DatabaseManager change that added it, not
		/// yet refreshed via Update Database) is treated as "unknown" (false) rather than assumed, so
		/// callers fall back to the safe non-greedy scan instead of guessing.
		/// </summary>
		private bool IsFourStarCharacter(Character character)
		{
			string lookupKey = character.NameGOOD.Contains("Traveler") ? "traveler" : character.NameGOOD.ToLower();
			return GenshinProcesor.Characters.TryGetValue(lookupKey, out var data)
				&& data["Rarity"] != null
				&& data["Rarity"].ToObject<int>() == 4;
		}

		/// <summary>
		/// Controller-mode talent scan. Per user (2026-07-05, live-tested), the Talents sub-tab
		/// already displays every talent's level simultaneously as "Lv. XX" rows (plus other
		/// descriptive text sharing the same region) -- no per-icon click/capture loop is needed the
		/// way the mouse path requires. Parses every "Lv. XX" match out of the single captured region,
		/// in on-screen top-to-bottom order, and assigns the first three as auto/skill/burst.
		/// Per user (2026-07-05): the mouse path's Mona/Ayaka movement-talent row skip does not apply
		/// here -- removed after a live test got stuck retrying (found 3 "Lv. XX" rows, wanted 4)
		/// rather than just taking the 3 that were actually there. Retries (matching the mouse path's
		/// per-icon retry loop) until at least 3 "Lv. XX" rows are found. Region measured (2026-07-05)
		/// with <c>ui/CoordinatePickerForm.cs</c>.
		/// </summary>
		private Dictionary<string, int> ScanTalents(Character character)
		{
			var talents = new Dictionary<string, int>
			{
				{ "auto" , 0 },
				{ "skill", 0 },
				{ "burst", 0 }
			};

			Rectangle region = new RECT(
				Left:   (int)( 0.8290 * Navigation.GetWidth() ),
				Top:    (int)( 0.1248 * Navigation.GetHeight() ),
				Right:  (int)( 0.8743 * Navigation.GetWidth() ),
				Bottom: (int)( 0.5687 * Navigation.GetHeight() ));

            // Resolve C3/C5 metadata before OCR so impossible reads are retried
            // rather than accepted and zeroed afterward (EXP-023 YunJin Lv.3
            // versus screenshot-confirmed C3-boosted Burst Lv.4).
            string c3Talent = null, c5Talent = null;
            if (character.Constellation >= 3 &&
                !(character.NameGOOD.Contains("Traveler") &&
                  string.Equals(character.Element, "Pyro", StringComparison.OrdinalIgnoreCase)))
            {
                string key = character.NameGOOD.Contains("Traveler")
                    ? "traveler" : character.NameGOOD.ToLowerInvariant();
                GenshinProcesor.Characters.TryGetValue(key, out var metadata);
                // Same resolver as final GOOD correction, even with stale 7.1 data.
                CharacterScanRecognition.TryResolveConstellationTalentOrder(
                    character.NameGOOD, character.Element, metadata?["ConstellationOrder"],
                    out c3Talent, out c5Talent, out _);
            }

			const int requiredRows = 3;
			int attempt = 0;
            // EXP-024: evidence-only telemetry to distinguish OCR rows missing,
            // impossible-level rejection and phase header verification failures.
            int mostRowsSeen = 0;
            int impossibleAutoRejects = 0;
            int impossibleBoostRejects = 0;
            string firstRawOcr = null, lastRawOcr = null;

			while (attempt < 20) // reduced from 50 per user (2026-07-05), matching ScanNameAndElement/ScanLevel's cap
			{
				Bitmap bm = Navigation.CaptureRegion(region);
                // EXP-023: show the crop in Kamera's UI before OCR, even on failure.
                if (attempt == 0)
                {
                    for (int slot = 0; slot < 3; slot++)
                        progressReporter.SetCharacter_Talent(bm, "unverified", slot);
                }
				Bitmap resized = GenshinProcesor.ResizeImage(bm, bm.Width * 2, bm.Height * 2);
				Bitmap n = imagePreprocessor.ConvertToGrayscale(resized);
				imagePreprocessor.SetContrast(60, ref n);
				imagePreprocessor.SetInvert(ref n);

				string rawTalentOcr = ocrService.AnalyzeText(n, Tesseract.PageSegMode.SingleBlock).Trim();
                string diagnostic = CharacterScanRecognition.CompactOcrDiagnostic(rawTalentOcr);
                if (attempt == 0) firstRawOcr = diagnostic;
                lastRawOcr = diagnostic;
                // EXP-024: accepts the observed "Lv: 1" mispunctuation from
                // Chongyun without relaxing the level range or guessing digits.
                var levels = CharacterScanRecognition.ParseControllerTalentLevels(rawTalentOcr);

				mostRowsSeen = Math.Max(mostRowsSeen, levels.Count);
                if (levels.Count >= requiredRows &&
                    CharacterScanRecognition.ImpossibleLowAscensionAuto(character.Ascension, levels[0]))
                {
                    impossibleAutoRejects++;
                    Logger.Warn("EXP023 {0}: rejected impossible Normal Attack Lv.{1} at ascension {2}.",
                        character.NameGOOD, levels[0], character.Ascension);
                    // Preserve the first suspicious frame for diagnosis. Retries
                    // may read the correct level after animations settle.
                    if (attempt == 0)
                    {
                        LogCharacterScreenshot(character.NameGOOD, "talents_suspect", region, force: true);
                        LogCharacterWindow(character.NameGOOD, "talents_suspect", force: true);
                    }
                }
                else if (levels.Count >= requiredRows &&
                    CharacterScanRecognition.ImpossibleConstellationTalentCandidate(
                        character.Constellation, c3Talent, c5Talent,
                        levels[0], levels[1], levels[2]))
                {
                    impossibleBoostRejects++;
                    Logger.Warn("EXP024 TALENT_BOOST_RETRY character={0} constellation={1} OCR={2}/{3}/{4} c3={5} c5={6}",
                        character.NameGOOD, character.Constellation, levels[0], levels[1], levels[2],
                        c3Talent ?? "(unknown)", c5Talent ?? "(unknown)");
                }
                else if (levels.Count >= requiredRows)
                {
                    talents["auto"] = levels[0];
					talents["skill"] = levels[1];
					talents["burst"] = levels[2];
                    Logger.Info("EXP024 TALENT_SUCCESS character={0} attempt={1} rows={2} scanned={3}/{4}/{5} rawOCR=\"{6}\"",
                        character.NameGOOD, attempt + 1, levels.Count, levels[0], levels[1], levels[2], diagnostic);

					progressReporter.SetCharacter_Talent(bm, talents["auto"].ToString(), 0);
					progressReporter.SetCharacter_Talent(bm, talents["skill"].ToString(), 1);
					progressReporter.SetCharacter_Talent(bm, talents["burst"].ToString(), 2);
					LogCharacterScreenshot(character.NameGOOD, "talents", region);
					LogCharacterWindow(character.NameGOOD, "talents");

					n.Dispose();
					bm.Dispose();
					break;
				}

				Logger.Debug("Controller talent scan: found {0}/{1} \"Lv. XX\" rows, retrying", levels.Count, requiredRows);
				n.Dispose();
				bm.Dispose();
				attempt++;
				Navigation.SystemWait(Navigation.Speed.Fast);
			}

			// The loop only sets all three talents together (on a successful read) then breaks; if it
			// exhausted every attempt without that, they're still -1. Surface it (previously silent --
			// the character just came back with -1/-1/-1 talents) and force-save the talent region so
			// the failure is diagnosable even with LogScreenshots off.
			if (talents["auto"] < 1 || talents["skill"] < 1 || talents["burst"] < 1)
			{
				Logger.Warn("EXP024 TALENT_FAILURE character={0} attempts={1} maxRows={2} impossibleAutoRejects={3} impossibleBoostRejects={4} firstOCR=\"{5}\" lastOCR=\"{6}\"",
                    character.NameGOOD, attempt, mostRowsSeen, impossibleAutoRejects, impossibleBoostRejects, firstRawOcr ?? "(not captured)", lastRawOcr ?? "(not captured)");
                progressReporter.AddError($"EXP024 could not read {character.NameGOOD}'s combat talents: zero is a review sentinel, not an in-game level.");
				LogCharacterScreenshot(character.NameGOOD, "talents_unconfirmed", region, force: true);
                LogCharacterWindow(character.NameGOOD, "talents_unconfirmed", force: true);
			}

			return talents;
		}
	}
}