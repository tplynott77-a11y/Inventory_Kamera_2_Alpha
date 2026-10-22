using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Tesseract;

namespace InventoryKamera
{
    /// <summary>
    /// V3.2's two-view five-scale weighted quantity OCR, using V4 controller crops.
    /// One colour-filtered inverted binary vote (weight 1) and one clean
    /// non-inverted grayscale vote (weight 2) are taken at each scale.
    /// </summary>
    internal sealed class MaterialQuantityVotingReader
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IOcrService ocrService;
        private readonly IImagePreprocessor preprocessor;

        internal MaterialQuantityVotingReader(IOcrService ocrService, IImagePreprocessor preprocessor)
        {
            this.ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            this.preprocessor = preprocessor ?? throw new ArgumentNullException(nameof(preprocessor));
        }

        internal int Read(Bitmap crop)
        {
            if (crop == null) throw new ArgumentNullException(nameof(crop));
            var votes = new MaterialQuantityVotes();
            for (double scale = 1.0; scale <= 3.0; scale += 0.5)
            {
                using (Bitmap scaled = GenshinProcesor.ResizeImage(
                    crop, (int)(crop.Width * scale), (int)(crop.Height * scale)))
                {
                    // Branch A: filtered, grayscale, inverted, thresholded.
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
                            votes.Record(raw, 1);
                            Logger.Debug("Material OCR filtered/inverted {0}x: '{1}'", scale, raw);
                        }
                    }

                    // Branch B: clean, non-inverted grayscale, twice the weight.
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
                        votes.Record(raw, 2);
                        Logger.Debug("Material OCR clean grayscale {0}x: '{1}'", scale, raw);
                    }
                }
            }
            if (votes.TryGetConsensus(out int result)) return result;
            Logger.Warn("Material OCR uncertain: {0}", votes.Summarize());
            return 0;
        }
    }

    /// <summary>
    /// Exact V3.2 voting policy: unique winning weighted vote and at least
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
