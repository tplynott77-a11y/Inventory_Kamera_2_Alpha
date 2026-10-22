using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace InventoryKamera
{
    /// <summary>
    /// Metadata for independently classified digits. This is a verification
    /// signal and never a proof of correctness on an unseen UI theme/resolution.
    /// </summary>
    internal readonly struct MaterialShapeReading
    {
        internal int Value { get; }
        internal double MinimumMargin { get; }
        internal double MaximumDistance { get; }
        internal bool GeometryValid { get; }

        internal MaterialShapeReading(int value, double minimumMargin,
            double maximumDistance, bool geometryValid)
        {
            Value = value;
            MinimumMargin = minimumMargin;
            MaximumDistance = maximumDistance;
            GeometryValid = geometryValid;
        }
    }

    /// <summary>
    /// Independent glyph-shape verification for 195x44 material quantity
    /// screenshots. Unknown image sizes/ambiguous geometry fail closed.
    /// EXP-020 detects touching adjacent digits (e.g. 677 with two touching 7s).
    /// </summary>
    internal static class MaterialDigitShapeReader
    {
        private const int Width = 195, Height = 44;
        private const int GlyphW = 8, GlyphH = 14;

        internal static int Read(Bitmap image) => Analyze(image).Value;

        internal static MaterialShapeReading Analyze(Bitmap image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (image.Width != Width || image.Height != Height) return default;

            bool[,] ink = new bool[Height, Width];
            int[] columnCounts = new int[Width];
            for (int y = 6; y < 37; y++)
                for (int x = 15; x < 175; x++)
                {
                    Color p = image.GetPixel(x, y);
                    if (p.B - p.R > 3 && p.G < 175 && p.R < 175 && p.B < 190)
                    {
                        ink[y, x] = true;
                        columnCounts[x]++;
                    }
                }

            var runs = new List<(int L, int R)>();
            int start = -1, last = -1;
            for (int x = 0; x < Width; x++)
            {
                if (columnCounts[x] < 2) continue;
                if (start < 0) start = x;
                else if (x - last >= 2)
                {
                    runs.Add((start, last + 1));
                    start = x;
                }
                last = x;
            }
            if (start >= 0) runs.Add((start, last + 1));
            if (runs.Count < 1 || runs.Count > 7) return default;

            // At 4K the actual digit silhouettes span 8-21 columns. A
            // 32-42-column run can be two touching digits that were merged
            // into one by x-projection (EXP-019 FirmArrowhead "677").
            // Try a midpoint split; low-quality halves still fail confidence.
            var separated = new List<(int L, int R)>();
            foreach (var run in runs)
            {
                int width = run.R - run.L;
                if (width >= 32 && width <= 42)
                {
                    int midpoint = run.L + width / 2;
                    separated.Add((run.L, midpoint));
                    separated.Add((midpoint, run.R));
                }
                else separated.Add(run);
            }
            if (separated.Count > 7) return default;

            bool validGeometry = true;
            double minimumMargin = double.PositiveInfinity;
            double maximumDistance = 0;
            var digits = new StringBuilder();

            foreach (var run in separated)
            {
                int top = Height, bottom = -1;
                for (int y = 0; y < Height; y++)
                    for (int x = run.L; x < run.R; x++)
                        if (ink[y, x])
                        {
                            top = Math.Min(top, y);
                            bottom = Math.Max(bottom, y);
                        }

                int w = run.R - run.L, h = bottom - top + 1;
                if (h < 6 || w < 2) return default;

                if (w < 7 || w > 23 || h < 19 || h > 31)
                    validGeometry = false;

                float[] glyph = Resize(ink, run.L, top, w, h);
                int bestDigit = -1;
                double bestSquared = double.PositiveInfinity;
                double secondSquared = double.PositiveInfinity;
                for (int digit = 0; digit < 10; digit++)
                {
                    double distance = 0;
                    byte[] reference = MaterialDigitTemplates.All[digit];
                    for (int i = 0; i < glyph.Length; i++)
                    {
                        double delta = glyph[i] - reference[i];
                        distance += delta * delta;
                    }
                    if (distance < bestSquared)
                    {
                        secondSquared = bestSquared;
                        bestSquared = distance;
                        bestDigit = digit;
                    }
                    else if (distance < secondSquared)
                        secondSquared = distance;
                }
                double best = Math.Sqrt(bestSquared);
                double margin = Math.Sqrt(secondSquared) - best;
                minimumMargin = Math.Min(minimumMargin, margin);
                maximumDistance = Math.Max(maximumDistance, best);
                digits.Append((char)('0' + bestDigit));
            }

            if (!int.TryParse(digits.ToString(), NumberStyles.None,
                CultureInfo.InvariantCulture, out int quantity) || quantity <= 0)
                return default;

            return new MaterialShapeReading(quantity, minimumMargin,
                maximumDistance, validGeometry);
        }

        /// <summary>Half-pixel bilinear resizing used by the 8x14 templates.</summary>
        private static float[] Resize(bool[,] ink, int left, int top,
            int width, int height)
        {
            var result = new float[GlyphW * GlyphH];
            for (int y = 0; y < GlyphH; y++)
            {
                double sy = Math.Clamp((y + 0.5) * height / GlyphH - 0.5, 0, height - 1);
                int y0 = (int)sy, y1 = Math.Min(y0 + 1, height - 1);
                double dy = sy - y0;
                for (int x = 0; x < GlyphW; x++)
                {
                    double sx = Math.Clamp((x + 0.5) * width / GlyphW - 0.5, 0, width - 1);
                    int x0 = (int)sx, x1 = Math.Min(x0 + 1, width - 1);
                    double dx = sx - x0;
                    double a = ink[top + y0, left + x0] ? 255 : 0;
                    double b = ink[top + y0, left + x1] ? 255 : 0;
                    double c = ink[top + y1, left + x0] ? 255 : 0;
                    double d = ink[top + y1, left + x1] ? 255 : 0;
                    result[y * GlyphW + x] = (float)(
                        (a * (1 - dx) + b * dx) * (1 - dy) +
                        (c * (1 - dx) + d * dx) * dy);
                }
            }
            return result;
        }
    }
}
