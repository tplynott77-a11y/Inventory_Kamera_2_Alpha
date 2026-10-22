using System;
using System.Collections.Generic;
using System.Drawing;
using Tesseract;
using Xunit;

namespace InventoryKamera.Tests
{
    public class MaterialQuantityVotingTests
    {
        [Theory]
        [InlineData("6", 6)]
        [InlineData("1,109", 1109)]
        [InlineData("11102", 11102)]
        [InlineData(" 42 ", 42)]
        public void ValidPositiveNumbersAreRecognized(string raw, int expected)
        {
            Assert.True(MaterialQuantityVotes.TryParseQuantity(raw, out int parsed));
            Assert.Equal(expected, parsed);
        }

        [Theory]
        [InlineData("")]
        [InlineData("M")]
        [InlineData("4A")]
        [InlineData("1,10")]
        [InlineData("0")]
        [InlineData("2-")]
        [InlineData("1.2")]
        public void MalformedNumbersAreRejectedInsteadOfCleaned(string raw)
        {
            Assert.False(MaterialQuantityVotes.TryParseQuantity(raw, out int value));
            Assert.Equal(0, value);
        }

        [Fact]
        public void TwoActualReadsRequiredEvenForPrimaryFilteredView()
        {
            var votes = new MaterialQuantityVotes();
            votes.Record("6", 3);
            Assert.False(votes.TryGetConsensus(out _));
            votes.Record("6", 1);
            Assert.True(votes.TryGetConsensus(out int answer));
            Assert.Equal(6, answer);
        }

        [Fact]
        public void EqualWeightedVotesAreRejected()
        {
            var votes = new MaterialQuantityVotes();
            votes.Record("4", 1);
            votes.Record("5", 2);
            votes.Record("4", 2);
            votes.Record("5", 1);
            Assert.False(votes.TryGetConsensus(out _));
        }

        [Fact]
        public void MalformedTextCannotCreateAConsensus()
        {
            var votes = new MaterialQuantityVotes();
            votes.Record("M", 2);
            votes.Record("1-", 1);
            votes.Record("4", 1);
            Assert.False(votes.TryGetConsensus(out _));
        }

        [Fact]
        public void BothPreprocessingViewsRunAtEveryScaleWithoutEarlyExit()
        {
            // Every pair: filtered then clean grayscale. The filtered
            // view is the more accurate source on actual V4 controller crops.
            var ocr = new QueueOcr("4", "6", "4", "6", "4", "6", "4", "6", "4", "6");
            var reader = new MaterialQuantityVotingReader(ocr, new ImageProcessor(), verifyByShape: false);
            using (var image = new Bitmap(195, 44))
                Assert.Equal(4, reader.Read(image));
            Assert.Equal(10, ocr.Calls);
            Assert.All(ocr.Modes, m => Assert.Equal(PageSegMode.SingleWord, m));
            Assert.All(ocr.NumericModes, m => Assert.True(m));
        }

        [Fact]
        public void EXP017_CrystalChunk_TrustsFiveCorrectFilteredVotes()
        {
            // Screenshot: 25. The grayscale model consistently read 29.
            var ocr = new QueueOcr("25", "29", "25", "29", "25", "29",
                "25", "29", "25", "29");
            var reader = new MaterialQuantityVotingReader(ocr, new ImageProcessor(), verifyByShape: false);
            using var crop = new Bitmap(195, 44);
            Assert.Equal(25, reader.Read(crop));
        }

        [Fact]
        public void EXP017_RawMeat_DoesNotFavorConsistentlyWrongGrayscale()
        {
            // Screenshot: 704. The secondary view read 104 tenaciously.
            var ocr = new QueueOcr("704", "104", "704", "104", "704", "104",
                "704", "104", "704", "104");
            var reader = new MaterialQuantityVotingReader(ocr, new ImageProcessor(), verifyByShape: false);
            using var crop = new Bitmap(195, 44);
            Assert.Equal(704, reader.Read(crop));
        }

        [Fact]
        public void EXP017_ChaosGear_RejectsGrayscaleInsertedDigits()
        {
            // Screenshot: 356; grayscale fabricated 3906/396, while the
            // filtered view recovered 356 at three of five scales.
            var ocr = new QueueOcr("", "3906", "", "396", "356", "3906",
                "356", "396", "356", "3906");
            var reader = new MaterialQuantityVotingReader(ocr, new ImageProcessor(), verifyByShape: false);
            using var crop = new Bitmap(195, 44);
            Assert.Equal(356, reader.Read(crop));
        }

        [Fact]
        public void SingleParsableReadIsInsufficient()
        {
            var ocr = new QueueOcr("9", "", "", "", "", "", "", "", "", "");
            var reader = new MaterialQuantityVotingReader(ocr, new ImageProcessor(), verifyByShape: false);
            using (var image = new Bitmap(195, 44))
                Assert.Equal(0, reader.Read(image));
            Assert.Equal(10, ocr.Calls);
        }

        [Fact]
        public void ShapeCanOverrideIncorrectWeightedOcrWithTwoIndependentReads()
        {
            Assert.Equal(25, MaterialQuantityVotingReader.ConfirmByShape(
                25, new[] { "29", "29", "29", "25", "29", "29", "25" }));
            Assert.Equal(704, MaterialQuantityVotingReader.ConfirmByShape(
                704, new[] { "104", "704", "104", "704" }));
        }

        [Fact]
        public void ShapeDisagreementOrSingleSupportingReadRequiresReview()
        {
            Assert.Equal(0, MaterialQuantityVotingReader.ConfirmByShape(
                356, new[] { "3906", "396", "3906", "356" }));
            Assert.Equal(0, MaterialQuantityVotingReader.ConfirmByShape(
                0, new[] { "356", "356", "356" }));
            Assert.Equal(0, MaterialQuantityVotingReader.ConfirmByShape(
                5, new[] { "9", "9", "9", "9" }));
        }

        [Fact]
        public void DefaultReaderWillNotExportUnverifiedDigits()
        {
            var ocr = new QueueOcr("25", "25", "25", "25", "25",
                "25", "25", "25", "25", "25");
            var reader = new MaterialQuantityVotingReader(ocr, new ImageProcessor());
            using var blank = new Bitmap(195, 44);
            Assert.Equal(0, reader.Read(blank));
            Assert.Equal(10, ocr.Calls);
        }

        [Fact]
        public void UnvalidatedCropSizesFailClosed()
        {
            using var crop = new Bitmap(98, 22);
            Assert.Equal(0, MaterialDigitShapeReader.Read(crop));
        }

        private sealed class QueueOcr : IOcrService
        {
            private readonly Queue<string> values;
            public int Calls { get; private set; }
            public List<PageSegMode> Modes { get; } = new List<PageSegMode>();
            public List<bool> NumericModes { get; } = new List<bool>();

            public QueueOcr(params string[] values) => this.values = new Queue<string>(values);
            public void Restart() { }
            public string AnalyzeText(Bitmap bitmap,
                PageSegMode pageMode = PageSegMode.SingleLine, bool numbersOnly = false)
            {
                Calls++;
                Modes.Add(pageMode);
                NumericModes.Add(numbersOnly);
                return values.Dequeue();
            }
            public (string Text, float Confidence) AnalyzeTextWithConfidence(Bitmap bitmap,
                PageSegMode pageMode = PageSegMode.SingleLine, bool numbersOnly = false)
                => (AnalyzeText(bitmap, pageMode, numbersOnly), 1f);
        }
    }
}
