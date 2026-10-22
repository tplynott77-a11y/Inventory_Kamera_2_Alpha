using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace InventoryKamera
{
    /// <summary>
    /// An independent glyph-shape verification signal for EXP-017's 195x44
    /// material quantity crops. Does not replace Tesseract: quantities require
    /// corroborating OCR readings before the scanner accepts them.
    /// Returns zero for unvalidated sizes/invalid or unreadable glyphs.
    /// </summary>
    internal static class MaterialDigitShapeReader
    {
        private const int Width = 195, Height = 44;
        private const int GlyphW = 8, GlyphH = 14;

        internal static int Read(Bitmap image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (image.Width != Width || image.Height != Height) return 0;

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

            // Separate digits by columns containing no ink. Vertical
            // contacts occasionally cause under-segmentation: reject those
            // rather than guess the number of digits.
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
            if (runs.Count < 1 || runs.Count > 7) return 0;

            var digits = new StringBuilder();
            foreach (var run in runs)
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
                if (h < 6 || w < 2) continue;
                var glyph = Resize(ink, run.L, top, w, h);
                int bestDigit = 0;
                double bestDistance = double.PositiveInfinity;
                for (int digit = 0; digit <= 9; digit++)
                {
                    double distance = 0;
                    byte[] reference = MaterialDigitTemplates.All[digit];
                    for (int i = 0; i < glyph.Length; i++)
                    {
                        double delta = glyph[i] - reference[i];
                        distance += delta * delta;
                    }
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestDigit = digit;
                    }
                }
                digits.Append((char)('0' + bestDigit));
            }
            return int.TryParse(digits.ToString(), NumberStyles.None,
                       CultureInfo.InvariantCulture, out int result) && result > 0
                ? result : 0;
        }

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
