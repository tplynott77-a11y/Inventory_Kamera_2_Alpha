using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Tesseract;

namespace InventoryKamera
{
    /// <summary>
    /// V4 two-view five-scale weighted quantity OCR using controller crops.
    /// EXP-017's 558 manually labelled 195x44 crops showed filtered/inverted
    /// OCR substantially outperforms clean grayscale OCR. Give filtered votes
    /// weight 3 and clean votes weight 1 at each of the five scales.
    /// This is an experimental improvement, not a complete accuracy fix.
    /// </summary>
    internal sealed class MaterialQuantityVotingReader
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IOcrService ocrService;
        private readonly IImagePreprocessor preprocessor;
        private readonly bool verifyByShape;

        internal MaterialQuantityVotingReader(IOcrService ocrService,
            IImagePreprocessor preprocessor, bool verifyByShape = true)
        {
            this.ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            this.preprocessor = preprocessor ?? throw new ArgumentNullException(nameof(preprocessor));
            this.verifyByShape = verifyByShape;
        }

        /// <summary>
        /// Do not silently accept a plausible but unverified weighted vote.
        /// A number must match an independent digit-shape reading and at
        /// least two of the ten original OCR reads. Zero means review needed.
        /// </summary>
        internal static int ConfirmByShape(int shapeValue, IEnumerable<string> rawOcr)
        {
            if (shapeValue <= 0) return 0;
            int supportingReads = 0;
            foreach (string raw in rawOcr)
                if (MaterialQuantityVotes.TryParseQuantity(raw, out int read) &&
                    read == shapeValue)
                    supportingReads++;
            return supportingReads >= 2 ? shapeValue : 0;
        }

        /// <summary>
        /// EXP-020: a conservative fallback for shapes with well-separated
        /// digit template scores when Tesseract consistently confuses 3/5
        /// with 9, 3/7 with 1, or drops the final zero of a ten.
        ///
        /// The bounds were replayed over complete EXP-017 and EXP-019 logs.
        /// If the shape or repeated compatible OCR signal is unclear, reject.
        /// This is deliberately limited to the verified 4K crop geometry.
        /// </summary>
        internal static int ConfirmWithFallback(MaterialShapeReading shape,
            IEnumerable<string> rawOcr, out bool fallbackUsed)
        {
            fallbackUsed = false;
            if (shape.Value <= 0) return 0;
            var readings = rawOcr.ToList();
            if (ConfirmByShape(shape.Value, readings) > 0)
                return shape.Value;

            if (!shape.GeometryValid || shape.MinimumMargin < 400 ||
                shape.MaximumDistance > 600)
                return 0;

            string expected = shape.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            int compatible = 0;
            foreach (string raw in readings)
            {
                if (CompatibleDigitConfusion(expected, raw))
                    compatible++;
            }
            if (compatible < 2) return 0;
            fallbackUsed = true;
            return shape.Value;
        }

        internal static bool CompatibleDigitConfusion(string expected, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return false;
            if (!MaterialQuantityVotes.TryParseQuantity(raw, out _))
                return false;
            string observed = raw.Trim().Replace(" ", "").Replace(",", "");

            // Clean OCR often drops the trailing zero of 10; this is
            // accepted only when independently strong glyphs show 10.
            if (expected.Length == observed.Length + 1 &&
                expected.EndsWith("0", StringComparison.Ordinal) &&
                expected.Substring(0, expected.Length - 1) == observed)
                return true;
            if (expected.Length != observed.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                char a = expected[i], b = observed[i];
                if (a == b) continue;
                bool commonMisread =
                    (a == '3' && (b == '9' || b == '1')) ||
                    (a == '5' && b == '9') ||
                    (a == '7' && b == '1');
                if (!commonMisread) return false;
            }
            return true;
        }

        internal int Read(Bitmap crop)
        {
            if (crop == null) throw new ArgumentNullException(nameof(crop));
            var votes = new MaterialQuantityVotes();
            var rawReads = new List<string>(10);
            for (double scale = 1.0; scale <= 3.0; scale += 0.5)
            {
                using (Bitmap scaled = GenshinProcesor.ResizeImage(
                    crop, (int)(crop.Width * scale), (int)(crop.Height * scale)))
                {
                    // Branch A: filtered, grayscale, inverted, thresholded (primary view).
                    using (Bitmap filtered = (Bitmap)scaled.Clone())
                    {
                        var dark = new IntRange(0, 150);
                        Bitmap working = filtered;
                        preprocessor.FilterColors(ref working, dark, dark, dark);
                        for (int x = 0; x < working.Width; x++)
                            for (int y = 0; y < (int)(working.Height * 0.25); y++)
                                working.SetPixel(x, y, Color.White);
                        using (Bitmap binary = preprocessor.ConvertToGrayscale(working))
                        {
                            Bitmap processed = binary;
                            preprocessor.SetInvert(ref processed);
                            preprocessor.SetThreshold(50, ref processed);
                            string raw = ocrService.AnalyzeText(
                                processed, PageSegMode.SingleWord, numbersOnly: true).Trim();
                            rawReads.Add(raw);
                            votes.Record(raw, 3);
                            Logger.Debug("Material OCR filtered/inverted {0}x: '{1}'", scale, raw);
                        }
                    }

                    // Branch B: clean, non-inverted grayscale, used as secondary evidence.
                    var rect = new Rectangle(
                        (int)(scaled.Width * 0.15),
                        (int)(scaled.Height * 0.25),
                        (int)(scaled.Width * 0.70),
                        scaled.Height - (int)(scaled.Height * 0.25));
                    using (Bitmap region = scaled.Clone(rect, scaled.PixelFormat))
                    using (Bitmap gray = preprocessor.ConvertToGrayscale(region))
                    {
                        string raw = ocrService.AnalyzeText(
                            gray, PageSegMode.SingleWord, numbersOnly: true).Trim();
                        rawReads.Add(raw);
                        votes.Record(raw, 1);
                        Logger.Debug("Material OCR clean grayscale {0}x: '{1}'", scale, raw);
                    }
                }
            }
            bool hasWeightedConsensus = votes.TryGetConsensus(out int weightedResult);
            if (!verifyByShape)
            {
                if (hasWeightedConsensus) return weightedResult;
                Logger.Warn("Material OCR uncertain: {0}", votes.Summarize());
                return 0;
            }

            MaterialShapeReading shape = MaterialDigitShapeReader.Analyze(crop);
            int confirmed = ConfirmWithFallback(shape, rawReads, out bool fallbackUsed);
            if (confirmed == 0)
                Logger.Warn("Material quantity REVIEW REQUIRED: shape={0}, weighted={1}, geometry={2}, margin={3:F0}, distance={4:F0}, votes={5}",
                    shape.Value, hasWeightedConsensus ? weightedResult : 0,
                    shape.GeometryValid, shape.MinimumMargin, shape.MaximumDistance,
                    votes.Summarize());
            else if (fallbackUsed)
                Logger.Info("Material quantity EXP020 high-confidence shape fallback: weighted={0} -> {1}, margin={2:F0}, distance={3:F0}",
                    hasWeightedConsensus ? weightedResult : 0, confirmed,
                    shape.MinimumMargin, shape.MaximumDistance);
            else if (confirmed != weightedResult)
                Logger.Info("Material quantity shape corrected weighted OCR: {0} -> {1}",
                    hasWeightedConsensus ? weightedResult : 0, confirmed);
            return confirmed;
        }
    }

    /// <summary>
    /// Voting policy: unique winning weighted vote and at least
    /// two distinct OCR readings. Unknown text is rejected, not coerced.
    /// </summary>
    internal sealed class MaterialQuantityVotes
    {
        private readonly Dictionary<int, int> weighted = new Dictionary<int, int>();
        private readonly Dictionary<int, int> reads = new Dictionary<int, int>();

        internal void Record(string text, int weight)
        {
            if (weight < 1) throw new ArgumentOutOfRangeException(nameof(weight));
            if (!TryParseQuantity(text, out int value)) return;
            weighted[value] = weighted.TryGetValue(value, out int w) ? w + weight : weight;
            reads[value] = reads.TryGetValue(value, out int n) ? n + 1 : 1;
        }

        internal bool TryGetConsensus(out int value)
        {
            value = 0;
            if (weighted.Count == 0) return false;
            var top = weighted.OrderByDescending(p => p.Value).First();
            int competing = weighted.Where(p => p.Key != top.Key)
                .Select(p => p.Value).DefaultIfEmpty(0).Max();
            if (reads[top.Key] < 2 || top.Value <= competing) return false;
            value = top.Key;
            return true;
        }

        internal string Summarize() => weighted.Count == 0
            ? "no valid readings"
            : string.Join(", ", weighted.Select(p =>
                p.Key + " (" + p.Value + " weighted, " + reads[p.Key] + " reads)"));

        internal static bool TryParseQuantity(string raw, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            string clean = raw.Trim().Replace(" ", "");
            if (!Regex.IsMatch(clean, @"^\d{1,3}(,\d{3})*$|^\d+$")) return false;
            return int.TryParse(clean.Replace(",", ""), out value) && value > 0;
        }
    }
}
