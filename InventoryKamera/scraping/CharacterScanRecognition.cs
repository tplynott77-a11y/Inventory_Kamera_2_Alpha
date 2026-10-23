using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

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
