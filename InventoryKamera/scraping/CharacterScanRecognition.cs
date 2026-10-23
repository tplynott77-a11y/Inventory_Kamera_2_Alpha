using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace InventoryKamera
{
    /// <summary>
    /// Small, testable character-scan recognition rules. No material-scan code
    /// depends on this class. EXP-022 uses only observed UI evidence.
    /// </summary>
    internal static class CharacterScanRecognition
    {
        internal static string RepairTravelerElement(string name, string rawElement, string recognizedElement)
        {
            // EXP-021/022: Cryo Traveler was read as "cryaq" and "gryo".
            // Accept only these observed spellings, only for independently
            // recognized Traveler. No fuzzy matching for other characters.
            if (string.Equals(name, "Traveler", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(rawElement?.Trim(), "cryaq", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(rawElement?.Trim(), "gryo", StringComparison.OrdinalIgnoreCase)))
                return "cryo";
            return recognizedElement;
        }

        /// <summary>
        /// Low-ascension Normal Attacks have a strict upper bound; even
        /// allowing Childe's +1 passive, an A3 (or lower) character cannot
        /// display a genuine Normal Attack Lv. 11. EXP-021 Kachina was
        /// incorrectly accepted as Lv. 11 at ascension 3, despite screenshot Lv. 1.
        /// Higher ascensions are left to ordinary OCR verification.
        /// </summary>
        internal static bool ImpossibleLowAscensionAuto(int ascension, int rawAuto)
        {
            return ascension >= 0 && ascension <= 3 && rawAuto > 7;
        }

        /// <summary>
        /// EXP-024 diagnostic-only formatting: preserve OCR row boundaries in a
        /// bounded single-line log field. Never influences recognition decisions.
        /// </summary>
        internal static string CompactOcrDiagnostic(string raw, int limit = 440)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "(empty)";
            string compact = Regex.Replace(raw.Replace("\r", ""), @"[ \t]+", " ")
                .Replace("\n", " | ").Trim();
            if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit));
            return compact.Length <= limit ? compact : compact.Substring(0, limit) + "...";
        }

        /// <summary>
        /// EXP-024: Only a complete OCR header that precisely matches the expected
        /// element AND display name may rescue a failed primary identity read.
        /// This is intentionally stricter than the roster's fuzzy first-pass
        /// parser: stripping separators/whitespace is safe, guessing names is not.
        /// </summary>
        internal static bool ExactHeaderMatches(string raw, string expectedElement, string expectedDisplayName)
        {
            if (string.IsNullOrWhiteSpace(raw) ||
                string.IsNullOrWhiteSpace(expectedElement) ||
                string.IsNullOrWhiteSpace(expectedDisplayName)) return false;

            string Normalize(string value) =>
                Regex.Replace(value, @"[\W_]", "", RegexOptions.CultureInvariant).ToLowerInvariant();

            return Normalize(raw) == Normalize(expectedElement) + Normalize(expectedDisplayName);
        }

        /// <summary>
        /// Screenshot-confirmed in EXP-024: the current game allows a Cryo
        /// Traveler. A cached characters.json may not yet include that form.
        /// Do not relax element validation for anyone else or for mannequins.
        /// </summary>
        /// <summary>
        /// EXP-024 log-confirmed identity mismatch: ScanNameAndElement returns
        /// canonical "Traveler", while GOOD stores the current form as
        /// "TravelerCryo" (or Traveler + another element). Do not spend extra
        /// OCR attempts or reject an already-validated canonical identity.
        /// Element agreement is mandatory, including for regular characters.
        /// </summary>
        internal static bool MatchesExpectedCharacter(
            string expectedGoodName, string expectedElement,
            string recognizedName, string recognizedElement)
        {
            if (string.IsNullOrWhiteSpace(expectedGoodName) ||
                string.IsNullOrWhiteSpace(expectedElement) ||
                string.IsNullOrWhiteSpace(recognizedName) ||
                string.IsNullOrWhiteSpace(recognizedElement) ||
                !string.Equals(expectedElement, recognizedElement, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(expectedGoodName, recognizedName, StringComparison.Ordinal))
                return true;

            return string.Equals(recognizedName, "Traveler", StringComparison.Ordinal) &&
                string.Equals(expectedGoodName, "Traveler" + expectedElement,
                    StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsObservedCryoTraveler(string canonicalName, string element) =>
            string.Equals(canonicalName, "Traveler", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(element, "Cryo", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// EXP-024: before accepting a talent OCR candidate, reject a claimed
        /// C3/C5 boosted talent below display Lv.4. This does NOT infer the
        /// correct value; it only requests another OCR attempt.
        /// </summary>
        /// <summary>
        /// EXP-024 Chongyun: whole-block OCR sometimes substitutes a colon for
        /// the period in "Lv. 1" ("Lv: 1"). Keep the original controller
        /// reader's per-line, first-match and 1-15 constraints; allow only
        /// that observed punctuation variant, not guesses such as "Lw.".
        /// </summary>
        internal static List<int> ParseControllerTalentLevels(string rawOcr)
        {
            var levels = new List<int>();
            if (string.IsNullOrWhiteSpace(rawOcr)) return levels;
            foreach (string line in rawOcr.Split('\n'))
            {
                Match match = Regex.Match(line, @"[Ll][Vv][.:]?\s*(\d+)",
                    RegexOptions.CultureInvariant);
                if (match.Success &&
                    int.TryParse(match.Groups[1].Value, out int level) &&
                    level >= 1 && level <= 15)
                    levels.Add(level);
            }
            return levels;
        }

        internal static bool ImpossibleConstellationTalentCandidate(
            int constellation, string c3Talent, string c5Talent, int normal, int skill, int burst)
        {
            int TalentLevel(string key) =>
                key == "auto" ? normal : key == "skill" ? skill : key == "burst" ? burst : 0;

            return (constellation >= 3 && !string.IsNullOrWhiteSpace(c3Talent) &&
                       TalentLevel(c3Talent) < 4) ||
                   (constellation >= 5 && !string.IsNullOrWhiteSpace(c5Talent) &&
                       TalentLevel(c5Talent) < 4);
        }

        /// <summary>
        /// EXP-024 follow-up: Cryo Traveler's C3 boosts Burst, C5 boosts Skill.
        /// Ensure cached and rebuilt character data contain the Cryo entry
        /// without discarding the manually preserved orders for other elements.
        /// </summary>
        internal static bool EnsureCryoTravelerMetadata(JObject traveler)
        {
            if (traveler == null) return false;
            bool changed = false;

            if (traveler["Element"] is JArray elements)
            {
                bool hasCryo = false;
                foreach (JToken value in elements)
                    if (string.Equals((string)value, "cryo", StringComparison.OrdinalIgnoreCase))
                        hasCryo = true;
                if (!hasCryo)
                {
                    elements.Add("cryo");
                    changed = true;
                }
            }

            if (traveler["ConstellationOrder"] == null)
            {
                traveler["ConstellationOrder"] = new JObject();
                changed = true;
            }

            if (traveler["ConstellationOrder"] is JObject orders)
            {
                if (!TryReadTalentOrder(orders["cryo"], out string c3, out string c5) ||
                    c3 != "burst" || c5 != "skill")
                {
                    orders["cryo"] = new JArray("burst", "skill");
                    changed = true;
                }
            }
            // Do not overwrite a malformed legacy flat order: the runtime
            // resolver below still safely repairs Cryo without deleting
            // other Traveler elements' mappings.
            return changed;
        }

        /// <summary>
        /// Shared by the pre-OCR C3/C5 plausibility check and the final GOOD
        /// correction, so they never disagree about which talent is boosted.
        /// A missing/malformed Cryo Traveler entry is repaired from the
        /// screenshot-confirmed mapping, NOT from guessed OCR talent values.
        /// </summary>
        internal static bool TryResolveConstellationTalentOrder(
            string goodName, string element, JToken storedOrder,
            out string c3Talent, out string c5Talent, out bool usedCryoRepair)
        {
            c3Talent = null;
            c5Talent = null;
            usedCryoRepair = false;

            bool isTraveler = !string.IsNullOrWhiteSpace(element) &&
                string.Equals(goodName, "Traveler" + element, StringComparison.OrdinalIgnoreCase);
            JToken selectedOrder = isTraveler
                ? (storedOrder as JObject)?[element.ToLowerInvariant()]
                : storedOrder;

            if (isTraveler &&
                string.Equals(element, "Cryo", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadTalentOrder(selectedOrder, out string foundC3, out string foundC5) ||
                    foundC3 != "burst" || foundC5 != "skill")
                    usedCryoRepair = true;

                c3Talent = "burst";
                c5Talent = "skill";
                return true;
            }

            return TryReadTalentOrder(selectedOrder, out c3Talent, out c5Talent);
        }

        private static bool TryReadTalentOrder(JToken token, out string c3Talent, out string c5Talent)
        {
            c3Talent = null;
            c5Talent = null;
            if (!(token is JArray pair) || pair.Count < 2 ||
                pair[0].Type != JTokenType.String || pair[1].Type != JTokenType.String)
                return false;

            string c3 = ((string)pair[0]).ToLowerInvariant();
            string c5 = ((string)pair[1]).ToLowerInvariant();
            bool IsTalent(string name) => name == "auto" || name == "skill" || name == "burst";
            if (!IsTalent(c3) || !IsTalent(c5) || c3 == c5) return false;
            c3Talent = c3;
            c5Talent = c5;
            return true;
        }

        internal static bool SaysActivated(string text) =>
            !string.IsNullOrWhiteSpace(text) &&
            Regex.IsMatch(text, @"\bactivated\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// A pure colour-discrimination pass measured against all saved
        /// EXP-021 bright-node examples: orange/gold letters have red notably
        /// above green, and green notably above blue. White and blue-green
        /// gradients are rejected. Result is black text on white background.
        /// Not an OCR answer by itself: Tesseract must still read "Activated".
        /// </summary>
        internal static Bitmap IsolateGoldConstellationText(Bitmap crop)
        {
            if (crop == null) throw new ArgumentNullException(nameof(crop));
            var result = new Bitmap(crop.Width, crop.Height);
            for (int y = 0; y < crop.Height; y++)
                for (int x = 0; x < crop.Width; x++)
                {
                    Color p = crop.GetPixel(x, y);
                    bool gold = p.R > 160 && p.G > 90 &&
                                p.R - p.G > 30 && p.G - p.B > 40;
                    result.SetPixel(x, y, gold ? Color.Black : Color.White);
                }
            return result;
        }

        /// <summary>
        /// These three vertical bands contain only the NORMAL, SKILL and
        /// BURST "Lv." labels in EXP-021's 174x959 screenshot crops.
        /// Passive/ascension rows start below the third band.
        /// Calculating fractions from the captured region preserves scaling.
        /// </summary>
        internal static Rectangle TalentLevelBand(int width, int height, int index)
        {
            if (width < 60 || height < 300) throw new ArgumentOutOfRangeException(nameof(height));
            double[] begins = { 0.035, 0.192, 0.349 };
            if (index < 0 || index >= begins.Length) throw new ArgumentOutOfRangeException(nameof(index));
            int top = (int)(height * begins[index]);
            int bottom = Math.Min(height, (int)(height * (begins[index] + 0.125)));
            // Trim away the left-aligned talent name (the cropped 4K
            // screenshots show Lv. text consistently in the right 76%).
            // This stops partial names from contaminating a SingleLine OCR.
            int left = (int)(width * 0.24);
            return new Rectangle(left, top, width - left, bottom - top);
        }

        internal static bool TrySingleTalentLevel(string text, out int level)
        {
            level = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            MatchCollection hits = Regex.Matches(text,
                @"\bLv\s*\.?\s*(\d{1,2})\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (hits.Count != 1 || !int.TryParse(hits[0].Groups[1].Value, out int value) ||
                value < 1 || value > 15) return false;
            level = value;
            return true;
        }

        internal static bool AgreeOnTalentLevel(string first, string second, out int level)
        {
            level = 0;
            return TrySingleTalentLevel(first, out int a) &&
                   TrySingleTalentLevel(second, out int b) &&
                   a == b && (level = a) > 0;
        }
    }
}
