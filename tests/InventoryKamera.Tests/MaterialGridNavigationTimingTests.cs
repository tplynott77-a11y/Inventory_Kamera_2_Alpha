using Xunit;

namespace InventoryKamera.Tests
{
    public class MaterialGridNavigationTimingTests
    {
        [Fact]
        public void NormalGridStepUsesShortPulseAndLongRelease()
        {
            var timing = MaterialScraper.MaterialGridStepTiming(1.0);
            Assert.Equal(45, timing.HoldMs);
            Assert.Equal(250, timing.SettleMs);
            Assert.True(timing.SettleMs > timing.HoldMs * 4);
        }

        [Theory]
        [InlineData(0.5, 20, 25)]
        [InlineData(1.5, 120, 150)]
        public void FastAndSlowModesRetainOriginalTimings(double delay, int hold, int settle)
        {
            var timing = MaterialScraper.MaterialGridStepTiming(delay);
            Assert.Equal(hold, timing.HoldMs);
            Assert.Equal(settle, timing.SettleMs);
        }
    }
}
