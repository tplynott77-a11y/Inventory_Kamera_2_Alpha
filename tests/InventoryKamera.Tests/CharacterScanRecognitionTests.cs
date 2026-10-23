using System;
using System.Drawing;
using System.Linq;
using Xunit;
using Newtonsoft.Json.Linq;

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
        [InlineData("Dendro / Kinich", "Dendro", "Kinich", true)]
        [InlineData("Geo / Chiori", "Geo", "Chiori", true)]
        [InlineData("Cryo / T_2the_homas77", "Cryo", "t_2the_homas77", true)]
        [InlineData("Cryo / Farty_Pants", "Cryo", "t_2the_homas77", false)]
        [InlineData("Dendro / Tighnari", "Dendro", "Kinich", false)]
        [InlineData("Geo / Kinich", "Dendro", "Kinich", false)]
        [InlineData("Anemo Yumemizuki\n/ Mizuki", "Anemo", "YumemizukiMizuki", true)]
        [InlineData("", "Dendro", "Kinich", false)]
        public void EXP024_AlternateHeaderMustExactlyMatchElementAndDisplayName(
            string raw, string element, string displayName, bool expected)
        {
            Assert.Equal(expected, CharacterScanRecognition.ExactHeaderMatches(raw, element, displayName));
        }

        [Theory]
        [InlineData("TravelerCryo", "Cryo", "Traveler", "Cryo", true)]
        [InlineData("TravelerCryo", "Cryo", "TravelerCryo", "Cryo", true)]
        [InlineData("TravelerPyro", "Pyro", "Traveler", "Pyro", true)]
        [InlineData("TravelerCryo", "Cryo", "Traveler", "Pyro", false)]
        [InlineData("TravelerPyro", "Pyro", "Traveler", "Cryo", false)]
        [InlineData("TravelerCryo", "Cryo", "Manequin2", "Cryo", false)]
        [InlineData("TravelerCryo", "Cryo", "Farty_Pants", "Cryo", false)]
        [InlineData("Kirara", "Dendro", "Mika", "Cryo", false)]
        [InlineData("Kirara", "Dendro", "Kirara", "Cryo", false)]
        [InlineData("Kirara", "Dendro", "Kirara", "Dendro", true)]
        [InlineData("Mika", "Cryo", "Mika", "Cryo", true)]
        [InlineData("TravelerCryo", "Cryo", null, "Cryo", false)]
        public void EXP024_VerificationRecognizesCanonicalTravelerButNeverWrongElementOrSlot(
            string expectedName, string expectedElement,
            string scannedName, string scannedElement, bool expected)
        {
            Assert.Equal(expected, CharacterScanRecognition.MatchesExpectedCharacter(
                expectedName, expectedElement, scannedName, scannedElement));
        }

        [Theory]
        [InlineData(null, "burst", "skill", true)]
        [InlineData("{}", "burst", "skill", true)]
        [InlineData("{'cryo':['burst','skill']}", "burst", "skill", false)]
        [InlineData("{'cryo':['skill','burst']}", "burst", "skill", true)]
        [InlineData("{'cryo':['burst','burst']}", "burst", "skill", true)]
        public void EXP024_CryoTravelerBoostsRepairMissingOrWrongOrder(
            string json, string expectedC3, string expectedC5, bool expectedRepair)
        {
            JToken stored = json == null ? null : JToken.Parse(json.Replace('\'', '"'));
            Assert.True(CharacterScanRecognition.TryResolveConstellationTalentOrder(
                "TravelerCryo", "Cryo", stored,
                out string c3, out string c5, out bool repaired));
            Assert.Equal(expectedC3, c3);
            Assert.Equal(expectedC5, c5);
            Assert.Equal(expectedRepair, repaired);
            Assert.False(CharacterScanRecognition.ImpossibleConstellationTalentCandidate(
                4, c3, c5, 5, 5, 9));
        }

        [Fact]
        public void EXP024_CryoTravelerBaseBurstIsSixForScreenshotConfirmedC4()
        {
            Assert.True(CharacterScanRecognition.TryResolveConstellationTalentOrder(
                "TravelerCryo", "Cryo", null,
                out string c3, out string c5, out _));
            var levels = new System.Collections.Generic.Dictionary<string, int>
            {
                ["auto"] = 5, ["skill"] = 5, ["burst"] = 9
            };
            levels[c3] -= 3;
            Assert.Equal(5, levels["auto"]);
            Assert.Equal(5, levels["skill"]);
            Assert.Equal(6, levels["burst"]);
            Assert.Equal("skill", c5);
        }

        [Fact]
        public void EXP024_TravelerDatabaseRepairPreservesOtherElementOrders()
        {
            var db = JObject.Parse(@"{
                'Element':['hydro','pyro'],
                'ConstellationOrder':{
                    'hydro':['skill','burst'],
                    'pyro':['skill','burst']
                }
            }".Replace('\'', '"'));
            var hydroBefore = db["ConstellationOrder"]["hydro"].DeepClone();
            var pyroBefore = db["ConstellationOrder"]["pyro"].DeepClone();
            Assert.True(CharacterScanRecognition.EnsureCryoTravelerMetadata(db));
            Assert.Equal("burst", (string)db["ConstellationOrder"]["cryo"][0]);
            Assert.Equal("skill", (string)db["ConstellationOrder"]["cryo"][1]);
            Assert.Equal(hydroBefore, db["ConstellationOrder"]["hydro"]);
            Assert.Equal(pyroBefore, db["ConstellationOrder"]["pyro"]);
            Assert.Equal(1, ((JArray)db["Element"]).Count(v => (string)v == "cryo"));
            Assert.False(CharacterScanRecognition.EnsureCryoTravelerMetadata(db));
        }

        [Theory]
        [InlineData("TravelerHydro","Hydro","{'hydro':['skill','burst']}","skill","burst")]
        [InlineData("TravelerPyro","Pyro","{'pyro':['burst','skill']}","burst","skill")]
        [InlineData("Kirara","Dendro","['skill','burst']","skill","burst")]
        public void EXP024_ExistingNonCryoOrdersUnchanged(
            string name, string element, string json, string expectedC3, string expectedC5)
        {
            Assert.True(CharacterScanRecognition.TryResolveConstellationTalentOrder(
                name, element, JToken.Parse(json.Replace('\'', '"')),
                out string c3, out string c5, out bool repaired));
            Assert.Equal(expectedC3, c3);
            Assert.Equal(expectedC5, c5);
            Assert.False(repaired);
        }

        [Fact]
        public void EXP024_MissingOtherTravelerFormDoesNotGetCryoDefault()
        {
            Assert.False(CharacterScanRecognition.TryResolveConstellationTalentOrder(
                "TravelerHydro", "Hydro", new JObject(),
                out _, out _, out bool repaired));
            Assert.False(repaired);
        }

        [Theory]
        [InlineData("Traveler", "Cryo", true)]
        [InlineData("Traveler", "Pyro", false)]
        [InlineData("Manequin2", "Cryo", false)]
        [InlineData("Farty_Pants", "Cryo", false)]
        [InlineData("Kinich", "Dendro", false)]
        public void EXP024_OnlyCanonicalCryoTravelerHasElementOverride(
            string canonicalName, string element, bool expected)
        {
            Assert.Equal(expected, CharacterScanRecognition.IsObservedCryoTraveler(canonicalName, element));
        }

        [Theory]
        [InlineData(3, "burst", "skill", 1, 1, 3, true)]
        [InlineData(3, "burst", "skill", 1, 1, 4, false)]
        [InlineData(6, "burst", "skill", 1, 3, 4, true)]
        [InlineData(6, "burst", "skill", 1, 4, 4, false)]
        [InlineData(0, "burst", "skill", 1, 1, 1, false)]
        [InlineData(3, null, null, 1, 1, 1, false)]
        public void EXP024_RetryImpossibleBoostedTalentInsteadOfExportingIt(
            int constellation, string c3, string c5,
            int normal, int skill, int burst, bool impossible)
        {
            Assert.Equal(impossible,
                CharacterScanRecognition.ImpossibleConstellationTalentCandidate(
                    constellation, c3, c5, normal, skill, burst));
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
