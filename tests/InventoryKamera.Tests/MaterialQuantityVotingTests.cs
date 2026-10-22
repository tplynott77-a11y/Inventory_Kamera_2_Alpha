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
        public void TwoActualReadsRequiredEvenWhenGrayscaleHasDoubleWeight()
        {
            var votes = new MaterialQuantityVotes();
            votes.Record("6", 2);
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
            // Every pair: filtered then clean grayscale. The first filtered
            // result is wrong but parsable; repeated clean grayscale wins.
            var ocr = new QueueOcr("4", "6", "4", "6", "4", "6", "4", "6", "4", "6");
            var reader = new MaterialQuantityVotingReader(ocr, new ImageProcessor());
            using (var image = new Bitmap(195, 44))
                Assert.Equal(6, reader.Read(image));
            Assert.Equal(10, ocr.Calls);
            Assert.All(ocr.Modes, m => Assert.Equal(PageSegMode.SingleWord, m));
            Assert.All(ocr.NumericModes, m => Assert.True(m));
        }

        [Fact]
        public void SingleParsableReadIsInsufficient()
        {
            var ocr = new QueueOcr("9", "", "", "", "", "", "", "", "", "");
            var reader = new MaterialQuantityVotingReader(ocr, new ImageProcessor());
            using (var image = new Bitmap(195, 44))
                Assert.Equal(0, reader.Read(image));
            Assert.Equal(10, ocr.Calls);
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
