using System;
using Xunit;

namespace InventoryKamera.Tests
{
    public class ReleaseTagVersionTests
    {
        [Theory]
        [InlineData("v2.0.0-alpha", "2.0.0")]
        [InlineData("2.0.0-alpha.2", "2.0.0")]
        [InlineData("v2.0.0-beta+build.17", "2.0.0")]
        [InlineData("v2.0.0", "2.0.0")]
        [InlineData("v1.4.5", "1.4.5")]
        [InlineData("2.0.0.22379", "2.0.0.22379")]
        [InlineData("  v2.0.0-rc.1  ", "2.0.0")]
        public void TryParseReleaseTagVersion_AcceptsNumericCoreAndSemVerSuffixes(string tag, string expected)
        {
            Assert.True(AppInfo.TryParseReleaseTagVersion(tag, out Version actual));
            Assert.Equal(new Version(expected), actual);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("v2.0.0-")]
        [InlineData("0-")]
        [InlineData("not-a-version")]
        [InlineData("v2.0.0_alpha")]
        public void TryParseReleaseTagVersion_RejectsMalformedTagsWithoutThrowing(string tag)
        {
            Assert.False(AppInfo.TryParseReleaseTagVersion(tag, out Version actual));
            Assert.Null(actual);
        }
    }
}
