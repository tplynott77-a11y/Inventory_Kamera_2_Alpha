using System;
using System.Drawing;
using Xunit;

namespace InventoryKamera.Tests
{
    public class CharacterScanRecognitionTests
    {
        [Fact]
        public void EXP024_DiagnosticsPreserveRowsWithoutModifyingRawOcr()
        {
            string raw = "  Normal   Lv. 1\nSkill\tLv. 4\r\nBurst Lv. 4";
            Assert.Equal("Normal Lv. 1 | Skill Lv. 4 | Burst Lv. 4",
                CharacterScanRecognition.CompactOcrDiagnostic(raw));
            Assert.Equal("  Normal   Lv. 1\nSkill\tLv. 4\r\nBurst Lv. 4", raw);
        }

        [Fact]
        public void EXP024_DiagnosticsAreBoundedAndHandleEmptyOcr()
        {
            Assert.Equal("(empty)", CharacterScanRecognition.CompactOcrDiagnostic("  "));
            Assert.Equal("01234...", CharacterScanRecognition.CompactOcrDiagnostic("0123456789", 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CharacterScanRecognition.CompactOcrDiagnostic("a", 0));
        }

        [Theory]
        [InlineData("Traveler", "cryaq", "", "cryo")]
        [InlineData("Traveler", "gryo", "", "cryo")]
        [InlineData("Kaeya", "gryo", "", "")]
        [InlineData("Traveler", "cryo", "cryo", "cryo")]
        [InlineData("Kaeya", "cryaq", "", "")]
        [InlineData("Traveler", "hydro", "hydro", "hydro")]
        public void OnlyObservedTravelerElementTypoIsCorrected(
            string name, string raw, string recognized, string expected)
        {
            Assert.Equal(expected, CharacterScanRecognition.RepairTravelerElement(
                name, raw, recognized));
        }

        [Theory]
        [InlineData("Activated")]
        [InlineData(" ACTiVaTeD ")]
        [InlineData("Activated!")]
        public void ExactActivatedKeywordIsAccepted(string raw)
        {
            Assert.True(CharacterScanRecognition.SaysActivated(raw));
        }

        [Theory]
        [InlineData("Activate")]
        [InlineData("Activate!")]
        [InlineData("Deactivated")]
        [InlineData("")]
        public void LockedNodesOrUnrelatedTextAreNotAccepted(string raw)
        {
            Assert.False(CharacterScanRecognition.SaysActivated(raw));
        }

        [Fact]
        public void EXP021_GoldIsolationDoesNotMistakeBrightGreenBackgroundForText()
        {
            using var image = new Bitmap(4, 1);
            image.SetPixel(0, 0, Color.FromArgb(248, 175, 20)); // orange letter
            image.SetPixel(1, 0, Color.FromArgb(239, 231, 80)); // bright yellow/green
            image.SetPixel(2, 0, Color.FromArgb(113, 192, 202)); // blue-green background
            image.SetPixel(3, 0, Color.White);
            using Bitmap mask = CharacterScanRecognition.IsolateGoldConstellationText(image);
            Assert.Equal(Color.Black.ToArgb(), mask.GetPixel(0, 0).ToArgb());
            for (int x = 1; x < 4; x++)
                Assert.Equal(Color.White.ToArgb(), mask.GetPixel(x, 0).ToArgb());
        }

        [Fact]
        public void CombatTalentBandsExcludePassiveRows()
        {
            Rectangle auto = CharacterScanRecognition.TalentLevelBand(174, 959, 0);
            Rectangle skill = CharacterScanRecognition.TalentLevelBand(174, 959, 1);
            Rectangle burst = CharacterScanRecognition.TalentLevelBand(174, 959, 2);
            Assert.True(auto.Bottom < skill.Top);
            Assert.True(skill.Bottom < burst.Top);
            Assert.InRange(auto.Top, 30, 39);
            Assert.InRange(skill.Top, 180, 190);
            Assert.InRange(burst.Top, 330, 340);
            Assert.True(burst.Bottom < 480); // passives excluded
        }

        [Theory]
        [InlineData("Lv. 1", 1)]
        [InlineData("Cragbiter  Lv. 1", 1)]
        [InlineData("Lv. 10", 10)]
        [InlineData("LV 15", 15)]
        public void TalentRowCanParseSingleLegalLevel(string text, int expected)
        {
            Assert.True(CharacterScanRecognition.TrySingleTalentLevel(text, out int value));
            Assert.Equal(expected, value);
        }

        [Theory]
        [InlineData("Lv. 0")]
        [InlineData("Lv. 16")]
        [InlineData("Lv. 1 and Lv. 11")]
        [InlineData("Passive Talent")]
        [InlineData("")]
        public void InvalidOrAmbiguousCombatLevelsAreRejected(string text)
        {
            Assert.False(CharacterScanRecognition.TrySingleTalentLevel(text, out _));
        }

        [Theory]
        [InlineData(3, 11, true)]
        [InlineData(3, 7, false)]
        [InlineData(4, 11, false)]
        [InlineData(6, 13, false)]
        public void ImpossibleLowAscensionAutoIsFlagged(int ascension, int level, bool expected)
        {
            Assert.Equal(expected, CharacterScanRecognition.ImpossibleLowAscensionAuto(ascension, level));
        }

        [Fact]
        public void EXP021_KachinaDisagreementDoesNotSilentlyExportEleven()
        {
            Assert.False(CharacterScanRecognition.AgreeOnTalentLevel(
                "Lv. 11", "Lv. 1", out int rejected));
            Assert.Equal(0, rejected);
            Assert.True(CharacterScanRecognition.AgreeOnTalentLevel(
                "Lv. 1", "Lv. 1", out int level));
            Assert.Equal(1, level);
        }
    }
}
